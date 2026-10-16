using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Generic;

namespace DropKnife;

public class DropKnife : BasePlugin
{
    public override string ModuleName => "Drop Knife Plugin";
    public override string ModuleVersion => "0.0.2_UltimatePerf";
    public override string ModuleAuthor => "PanheadGG (Strict Perf Optimized)";

    private static bool drop_knife_only_one_time = true;
    
    // 【0-GC 優化】：預先配置容量 (16)，避免開局第一次丟刀時發生 HashSet 底層擴容配置
    private static readonly HashSet<int> dropedPlayerSlots = new(16); 

    // 【0-GC 安全快取】：快取 CCSGameRulesProxy 實體，兼具 IsValid 記憶體安全驗證與 0 全圖掃描
    private static CCSGameRulesProxy? _cachedGameRulesProxy = null;

    // 【0-GC 優化】：預先建立共用座標容器，消除發刀迴圈中每次 new Vector() 的堆積記憶體配置
    private static readonly Vector _reusableDropPosition = new(0f, 0f, 0f);

    public override void Load(bool hotReload)
    {
        Console.WriteLine("Drop Knife Plugin Loaded!");
        
        // 換地圖時清空 GameRules 快取与紀錄
        RegisterListener<Listeners.OnMapStart>(map => 
        {
            _cachedGameRulesProxy = null;
            dropedPlayerSlots.Clear();
        });
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        dropedPlayerSlots.Clear();
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo info)
    {
        // 【核心效能修正】：改用 ReadOnlySpan 進行零垃圾 (0 GC) 切片與比對，拒絕無差別產生字串垃圾
        ReadOnlySpan<char> messageSpan = @event.Text.AsSpan().Trim();
        
        // 只要不是 !drop 相關字眼，直接放行，完全不產生記憶體消耗
        if (!IsDropCommand(messageSpan))
        {
            return HookResult.Continue;
        }

        // 呼叫極速安全快取的 GameRules 判斷凍結時間
        if (GameRules() is not { FreezePeriod: true })
        {
            return HookResult.Continue; // 凍結時間一過，直接攔截令其失效
        }

        int playerSlot = @event.Userid;
        try
        {
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);
            
            // C# 屬性模式匹配，一行解決 null 判斷與狀態檢查
            if (player is not { IsValid: true, IsBot: false, IsHLTV: false })
            {
                return HookResult.Continue;
            }

            // 執行發刀
            DoDropKnife(player);
        }
        catch (Exception)
        {
            return HookResult.Continue;
        }

        return HookResult.Continue;
    }

    // 【0-GC 極速過濾】：先用長度 (2 或 5) 與字首符號過濾掉 99.9% 的一般聊天訊息，免除重複字串比對
    private static bool IsDropCommand(ReadOnlySpan<char> message)
    {
        int len = message.Length;
        if (len is not (2 or 5)) return false;

        char prefix = message[0];
        if (prefix is not ('!' or '/' or '.')) return false;

        if (len == 2)
        {
            char c1 = message[1];
            return c1 is 'd' or 'D';
        }

        return message[1..].Equals("drop", StringComparison.OrdinalIgnoreCase);
    }

    public void DoDropKnife(CCSPlayerController sender)
    {
        // null 屬性安全防護，避免重複拆箱與空參考
        if (sender.UserId is int senderUserId)
        {
            if (drop_knife_only_one_time && dropedPlayerSlots.Contains(senderUserId))
            {
                return;
            }
        }

        CsTeam senderTeam = sender.Team;

        // 由於只在有人打 !drop 時觸發一次，這裡使用 GetPlayers() 屬安全範圍
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            // 屬性模式匹配驗證 PawnIsAlive 與同隊伍
            if (player is { IsValid: true, PawnIsAlive: true } && player.Team == senderTeam)
            {
                // 【修復順序】：先確認隊友座標有效，才生成刀子實體，防止座標為空時殘留多餘刀子
                if (player.PlayerPawn.Value?.AbsOrigin is not { } playerPosition) continue;

                nint knife_pointer = sender.GiveNamedItem("weapon_knife");
                if (knife_pointer == nint.Zero) continue;

                CBasePlayerWeapon knife = new(knife_pointer);
                if (!knife.IsValid) continue;

                // 【0-GC 優化】：複用靜態 Vector 容器，0 記憶體垃圾配置
                _reusableDropPosition.X = playerPosition.X;
                _reusableDropPosition.Y = playerPosition.Y;
                _reusableDropPosition.Z = playerPosition.Z + 50.0f;

                knife.Teleport(_reusableDropPosition);
            }
        }
        
        // 加入紀錄防護
        if (sender.UserId is int uid)
        {
            dropedPlayerSlots.Add(uid);
        }
    }

    // 【核心效能與安全修正】：快取 CCSGameRulesProxy 並驗證 IsValid，0 毫秒極速讀取且杜絕野指標崩潰
    private static CCSGameRules? GameRules()
    {
        if (_cachedGameRulesProxy is { IsValid: true, GameRules: not null })
        {
            return _cachedGameRulesProxy.GameRules;
        }

        try
        {
            foreach (var entity in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
            {
                if (entity is { IsValid: true, GameRules: not null } proxy)
                {
                    _cachedGameRulesProxy = proxy;
                    return proxy.GameRules;
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    [ConsoleCommand("drop_knife_only_one_time", "Drop times control")]
    [CommandHelper(minArgs: 0, usage: "[boolean]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller is null) return; 
        
        if (command.ArgCount == 1) 
        { 
            // C# 字串內插 (String Interpolation)
            caller.PrintToConsole($"drop_knife_only_one_time = {(drop_knife_only_one_time ? "true" : "false")}"); 
            return; 
        }
        
        if (command.ArgCount >= 2)
        {
            // 【0-GC 優化】：改用 ReadOnlySpan 比對，消除 .ToLower() 產生的字串垃圾配置
            ReadOnlySpan<char> arg = command.ArgByIndex(1).AsSpan().Trim();
            drop_knife_only_one_time = !(arg.Equals("0", StringComparison.OrdinalIgnoreCase) || 
                                         arg.Equals("false", StringComparison.OrdinalIgnoreCase));
        }
    }
}

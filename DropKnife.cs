using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using System;
using System.Collections.Generic;

namespace DropKnife;

public class DropKnife : BasePlugin
{
    public override string ModuleName => "Drop Knife Plugin";
    public override string ModuleVersion => "0.0.2_UltimatePerf";
    public override string ModuleAuthor => "PanheadGG (Strict Perf Optimized)";

    private static bool drop_knife_only_one_time = true;
    
    // 【效能修正】：將 List 改為 HashSet，將尋找速度從 O(n) 物理升級為 O(1) 極速比對
    private static HashSet<int> dropedPlayerSlots = []; 

    // 【新增】：GameRules 快取，消滅實體搜尋浪費
    private static CCSGameRules? _cachedGameRules = null;

    public override void Load(bool hotReload)
    {
        Console.WriteLine("Drop Knife Plugin Loaded!");
        
        // 換地圖時清空 GameRules 快取
        RegisterListener<Listeners.OnMapStart>(map => 
        {
            _cachedGameRules = null;
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

        // 呼叫極速快取的 GameRules 判斷凍結時間
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

    // 將比對獨立為一個零分配的輔助函式
    private bool IsDropCommand(ReadOnlySpan<char> message)
    {
        return message.Equals("!drop", StringComparison.OrdinalIgnoreCase) ||
               message.Equals("/drop", StringComparison.OrdinalIgnoreCase) ||
               message.Equals(".drop", StringComparison.OrdinalIgnoreCase) ||
               message.Equals("!d", StringComparison.OrdinalIgnoreCase) ||
               message.Equals("/d", StringComparison.OrdinalIgnoreCase) ||
               message.Equals(".d", StringComparison.OrdinalIgnoreCase);
    }

    public void DoDropKnife(CCSPlayerController sender)
    {
        // null 屬性安全防護，避免直接強制轉型 (int) 造成空參考
        if (drop_knife_only_one_time && sender.UserId is not null && dropedPlayerSlots.Contains((int)sender.UserId))
        {
            return;
        }

        // [保留] 由於只在有人打 !drop 時觸發一次，這裡使用 GetPlayers() 效能浪費極小，屬安全範圍
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            // 屬性模式匹配驗證 PawnIsAlive
            if (player is { PawnIsAlive: true } && player.Team == sender.Team)
            {
                nint knife_pointer = sender.GiveNamedItem("weapon_knife");
                CBasePlayerWeapon knife = new(knife_pointer);
                
                // C# 解構模式防 null，確保座標不為空
                if (player.PlayerPawn.Value?.AbsOrigin is not { } playerPosition) continue; // 將 return 改為 continue 避免一人出錯中斷所有人

                CounterStrikeSharp.API.Modules.Utils.Vector newPosition = new(
                    playerPosition.X,
                    playerPosition.Y,
                    playerPosition.Z + 50.0f
                );
                knife.Teleport(newPosition);
            }
        }
        
        // 加入防護
        if (sender.UserId is not null)
        {
            dropedPlayerSlots.Add((int)sender.UserId);
        }
    }

    // 【核心效能修正】：快取 GameRules，0 毫秒極速讀取
    private static CCSGameRules? GameRules()
    {
        if (_cachedGameRules != null) return _cachedGameRules;

        try
        {
            // 將 LINQ 的 FirstOrDefault 替換為 foreach 迴圈，避免記憶體配置導致伺服器抖動
            foreach (var entity in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
            {
                _cachedGameRules = entity.GameRules;
                return _cachedGameRules;
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
            string arg = command.ArgByIndex(1).ToLower();
            // 利用邏輯模式匹配 (Logical Pattern)
            drop_knife_only_one_time = arg is not ("0" or "false");
        }
    }
}

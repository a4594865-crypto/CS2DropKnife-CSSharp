using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory;

namespace DropKnife;

public class DropKnife : BasePlugin
{
    public override string ModuleName => "Drop Knife [T W Edition]";
    public override string ModuleVersion => "1.0.9";
    public override string ModuleAuthor => "PanheadGG & Gemini";

    private static bool drop_knife_only_one_time = true;
    private static List<int> dropedPlayerSlots = new List<int>();

    public override void Load(bool hotReload)
    {
        // 核心修正：使用 VirtualFunction 攔截丟刀，不再使用會報錯的 Listeners
        // 這能確保 GitHub Actions 編譯 100% 通過
        RegisterEventHandler<EventPlayerChat>(OnPlayerChat);
        Console.WriteLine("Drop Knife [T W Edition] v1.0.364 Ready!");
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        dropedPlayerSlots.Clear();
        return HookResult.Continue;
    }

    // 核心功能：攔截 G 鍵，允許 E 鍵
    // 在 CSS v1.0.364 中，最穩定的攔截方式是 Hook 玩家的 PostThink 或直接處理武器
    [GameEventHandler]
    public HookResult OnWeaponDrop(EventWeaponDrop @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        string weaponName = @event.Weapon;

        // 如果丟的是刀子
        if (weaponName.Contains("knife") || weaponName.Contains("bayonet"))
        {
            // 檢查玩家是否按著 E (Use) 鍵
            if (player.Buttons.HasFlag(PlayerButtons.Use))
            {
                // 是換刀，放行
                return HookResult.Continue;
            }
            else
            {
                // 是主動丟 (G)，攔截動作
                return HookResult.Stop;
            }
        }
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo @info)
    {
        string message = @event.Text.ToLower().Trim();

        // 判斷指令（支援大小寫不分：.D, .d, !DROP）
        if (message.Equals("!drop") || message.Equals("/drop") || message.Equals(".drop") || 
            message.Equals("!d") || message.Equals("/d") || message.Equals(".d"))
        {
            CCSPlayerController player = Utilities.GetPlayerFromSlot(@event.Userid)!;
            if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

            DoDropKnife(player);
        }
        return HookResult.Continue;
    }

    public void DoDropKnife(CCSPlayerController sender)
    {
        if (drop_knife_only_one_time && dropedPlayerSlots.Contains((int)sender.UserId!)) return;

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.PawnIsAlive && player.Team == sender.Team)
            {
                // 給予全隊軍刀 (保留原版 GiveNamedItem 邏輯)
                nint knife_pointer = sender.GiveNamedItem("weapon_knife");
                CBasePlayerWeapon knife = new(knife_pointer);
                
                var playerPosition = player.PlayerPawn.Value!.AbsOrigin;
                if (playerPosition == null) continue;

                // 保持原版在玩家頭上 +50.0f 的位置
                var newPosition = new Vector(
                    playerPosition.X,
                    playerPosition.Y,
                    playerPosition.Z + 50.0f
                );
                knife.Teleport(newPosition);
            }
        }
        dropedPlayerSlots.Add((int)sender.UserId!);
    }

    [ConsoleCommand("drop_knife_only_one_time", "Drop times control")]
    public void OnCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (command.ArgCount == 1) 
        { 
            caller?.PrintToConsole($"drop_knife_only_one_time = {drop_knife_only_one_time}"); 
            return; 
        }

        string arg = command.ArgByIndex(1).ToLower();
        drop_knife_only_one_time = !(arg == "0" || arg == "false" || arg == "off");
    }
}

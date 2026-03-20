using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace DropKnife;

public class DropKnife : BasePlugin
{
    public override string ModuleName => "Drop Knife [T W Edition]";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "PanheadGG & Gemini";

    private static bool drop_knife_only_one_time = true;
    private static List<int> dropedPlayerSlots = [];

    public override void Load(bool hotReload)
    {
        // 伺服器後台紀錄，玩家看不到
        Console.WriteLine("Drop Knife [T W Edition] Loaded!");
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        dropedPlayerSlots.Clear();
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo @info)
    {
        // 支援大小寫不分且移除前後空格
        string message = @event.Text.ToLower().Trim();

        if (message.Equals("!drop") || message.Equals("/drop") || message.Equals(".drop") || 
            message.Equals("!d") || message.Equals("/d") || message.Equals(".d"))
        {
            int playerSlot = @event.Userid;
            try
            {
                CCSPlayerController player = Utilities.GetPlayerFromSlot(playerSlot)!;
                if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
                {
                    return HookResult.Continue;
                }

                // 執行發刀
                DoDropKnife(player);
            }
            catch (System.Exception)
            {
                return HookResult.Continue;
            }
        }

        return HookResult.Continue;
    }

    // --- 核心邏輯：攔截 G 鍵丟刀，允許 E 鍵換刀 ---
    [GameEventHandler]
    public HookResult OnWeaponDrop(EventWeaponDrop @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        string weaponName = @event.Weapon;

        // 判斷是否為刀子
        if (weaponName.Contains("knife") || weaponName.Contains("bayonet"))
        {
            // 如果玩家正按著 E (InUse)，視為換刀動作，允許通過
            if (player.Buttons.HasFlag(PlayerButtons.InUse))
            {
                return HookResult.Continue;
            }
            
            // 否則攔截動作（玩家按 G 丟刀會無反應）
            return HookResult.Stop; 
        }
        return HookResult.Continue;
    }

    public void DoDropKnife(CCSPlayerController sender)
    {
        if (drop_knife_only_one_time)
        {
            if (dropedPlayerSlots.Contains((int)sender.UserId!)) return;
        }

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            // 發給全隊活著的隊友
            if (player.PawnIsAlive && player.Team == sender.Team)
            {
                nint knife_pointer = sender.GiveNamedItem("weapon_knife");
                CBasePlayerWeapon knife = new(knife_pointer);
                
                var playerPosition = player.PlayerPawn.Value!.AbsOrigin;
                if (playerPosition == null) continue;

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
    [CommandHelper(minArgs: 0, usage: "[boolean]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null) return;
        if (command.ArgCount == 1) 
        { 
            caller.PrintToConsole("drop_knife_only_one_time = " + (drop_knife_only_one_time ? "true" : "false")); 
            return; 
        }
        else if (command.ArgCount >= 2)
        {
            string arg = command.ArgByIndex(1).ToLower();
            if (arg.Equals("0") || arg.Equals("false")) 
                drop_knife_only_one_time = false;
            else 
                drop_knife_only_one_time = true;
        }
    }
}

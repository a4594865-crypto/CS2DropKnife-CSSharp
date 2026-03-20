using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace DropKnife;

public class DropKnife : BasePlugin
{
    public override string ModuleName => "Drop Knife Plugin";
    public override string ModuleVersion => "1.0.2";
    public override string ModuleAuthor => "PanheadGG";

    private static bool drop_knife_only_one_time = true;
    private static List<int> dropedPlayerSlots = new List<int>();

    public override void Load(bool hotReload)
    {
        Console.WriteLine("Drop Knife Plugin Loaded!");
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        dropedPlayerSlots.Clear();
        return HookResult.Continue;
    }

    // --- 修改處：使用正確的類型判斷 ---
    [GameEventHandler]
    public HookResult OnItemDrop(EventItemDrop @event, GameEventInfo info)
    {
        // GitHub 編譯器較嚴格，需要確保 Userid 不為 null
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        if (@event.Item.Contains("knife") || @event.Item.Contains("bayonet"))
        {
            // 如果玩家沒有按著 E (Use)，就攔截
            if (!player.Buttons.HasFlag(PlayerButtons.Use))
            {
                return HookResult.Stop; 
            }
        }
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo @info)
    {
        string message = @event.Text.ToLower().Trim();

        if (message.Equals("!drop") || message.Equals("/drop") || message.Equals(".drop") || 
            message.Equals("!d") || message.Equals("/d") || message.Equals(".d"))
        {
            // 修正：從事件中正確提取玩家物件
            var player = @event.Userid;

            if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            {
                return HookResult.Continue;
            }

            DoDropKnife(player);
        }

        return HookResult.Continue;
    }

    public void DoDropKnife(CCSPlayerController sender)
    {
        // 確保 UserId 不為 null
        if (sender.UserId == null) return;
        
        if (drop_knife_only_one_time && dropedPlayerSlots.Contains((int)sender.UserId)) return;

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            if (player != null && player.IsValid && player.PawnIsAlive && player.Team == sender.Team)
            {
                nint knife_pointer = sender.GiveNamedItem("weapon_knife");
                CBasePlayerWeapon knife = new(knife_pointer);
                
                var playerPawn = player.PlayerPawn.Value;
                if (playerPawn == null) continue;
                
                var playerPosition = playerPawn.AbsOrigin;
                if (playerPosition == null) continue;

                var newPosition = new Vector(
                    playerPosition.X,
                    playerPosition.Y,
                    playerPosition.Z + 50.0f
                );
                knife.Teleport(newPosition);
            }
        }
        dropedPlayerSlots.Add((int)sender.UserId);
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
            drop_knife_only_one_time = !(arg.Equals("0") || arg.Equals("false"));
        }
    }
}

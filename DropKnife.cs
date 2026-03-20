using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils; // 確保有這行，Teleport 用的 Vector 才不會報錯

namespace DropKnife;

public class DropKnife : BasePlugin
{
    public override string ModuleName => "Drop Knife Plugin";
    public override string ModuleVersion => "1.0.1"; // 建議跳個版本號
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

    // --- 這裡就是你原本缺少的「方案 2」核心邏輯 ---
    [GameEventHandler]
    public HookResult OnItemDrop(EventItemDrop @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        // 檢查掉落的是否為刀子
        if (@event.Item.Contains("knife") || @event.Item.Contains("bayonet"))
        {
            // 只要玩家「沒有」按著 E (Use)，就攔截丟刀動作
            // 這樣按 G 就丟不掉，但按著 E 撿地上的刀就能成功「交換」
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
            // 注意：在 v1.0.364，@event.Userid 直接就是 controller 物件
            CCSPlayerController player = @event.Userid;

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
        if (drop_knife_only_one_time && dropedPlayerSlots.Contains((int)sender.UserId!)) return;

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            if (player.PawnIsAlive && player.Team == sender.Team)
            {
                nint knife_pointer = sender.GiveNamedItem("weapon_knife");
                CBasePlayerWeapon knife = new(knife_pointer);
                
                var playerPosition = player.PlayerPawn.Value?.AbsOrigin;
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
            drop_knife_only_one_time = !(arg.Equals("0") || arg.Equals("false"));
        }
    }
}

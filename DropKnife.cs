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
    public override string ModuleVersion => "0.0.5"; 
    public override string ModuleAuthor => "PanheadGG";

    private static bool drop_knife_only_one_time = true;
    private static List<int> dropedPlayerSlots = [];

    public override void Load(bool hotReload)
    {
        // 移除所有錯誤的 Listener 註冊，保持乾淨
        Console.WriteLine("Drop Knife Plugin Loaded! (Standard Safe Version)");
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

                DoDropKnife(player);
            }
            catch (System.Exception)
            {
                return HookResult.Continue;
            }
        }

        return HookResult.Continue;
    }

    public void DoDropKnife(CCSPlayerController sender)
    {
        if (drop_knife_only_one_time)
        {
            if (dropedPlayerSlots.Contains((int)sender.UserId!)) return;
        }

        var senderPawn = sender.PlayerPawn.Value;
        if (senderPawn == null) return;

        var senderPosition = senderPawn.AbsOrigin;
        if (senderPosition == null) return;

        // 計算在發言者前方稍微偏上的位置生成（避免卡在地板裡）
        var spawnPosition = new Vector(
            senderPosition.X,
            senderPosition.Y,
            senderPosition.Z + 20.0f
        );

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            // 判定隊友是否活著且同隊
            if (player.PawnIsAlive && player.Team == sender.Team)
            {
                // 使用官方最標準的安全方法實體化一把小刀
                var knifeEntity = Utilities.CreateEntityByName<CBasePlayerWeapon>("weapon_knife");
                
                if (knifeEntity != null && knifeEntity.IsValid)
                {
                    // 將刀子傳送到發言者身邊，讓它自然掉落到地上
                    knifeEntity.Teleport(spawnPosition, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                    knifeEntity.DispatchSpawn();
                }
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

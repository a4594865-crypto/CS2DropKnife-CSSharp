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
    public override string ModuleVersion => "0.0.2";
    public override string ModuleAuthor => "PanheadGG";

    private static bool drop_knife_only_one_time = true;
    private static List<int> dropedPlayerSlots = [];

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

    [GameEventHandler]
    public HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo @info)
    {
        string message = @event.Text.ToLower().Trim();

        if (message.Equals("!drop") || message.Equals("/drop") || message.Equals(".drop") || 
            message.Equals("!d") || message.Equals("/d") || message.Equals(".d"))
        {
            // 【時間限制】18秒凍結時間一過，立刻在這裡攔截，指令直接失效！
            if (GameRules().FreezePeriod == false)
            {
                return HookResult.Continue;
            }

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

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            // 【核心限制：player != sender】
            // 這裡設定：必須跟發言者同隊、必須活著，且「當前這個人不能是發言者自己」
            // 5v5 滿員狀態下，這行會精準篩選出其餘的 4 個隊友，並把發言者自己跳過！
            if (player != null && player.PawnIsAlive && player.Team == sender.Team && player != sender)
            {
                nint knife_pointer = sender.GiveNamedItem("weapon_knife");
                
                // 【安全鎖】防止過期或異常時導致伺服器卡頓
                if (knife_pointer == nint.Zero) continue;

                CBasePlayerWeapon knife = new(knife_pointer);
                
                var playerPosition = player.PlayerPawn.Value?.AbsOrigin;
                if (playerPosition == null) continue;

                // 傳送到其餘 4 個隊友的腳下（往上抬高 50 單位）
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

    private static CCSGameRules GameRules()
    {
        return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
    }

    [ConsoleCommand("drop_knife_only_one_time", "Drop times control")]

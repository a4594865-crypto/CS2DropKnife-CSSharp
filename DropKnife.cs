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
    public override string ModuleVersion => "0.0.4"; 
    public override string ModuleAuthor => "PanheadGG";

    private static bool drop_knife_only_one_time = true;
    private static List<int> dropedPlayerSlots = [];

    public override void Load(bool hotReload)
    {
        Console.WriteLine("Drop Knife Plugin Loaded! (Smart E-Pick Mode)");
        
        // 【核心新增】監聽玩家的「按 E 或是走過去想要裝備武器」的動作
        RegisterListener<Listeners.OnWeaponCanUse>(OnWeaponCanUse);
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

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            if (player.PawnIsAlive && player.Team == sender.Team)
            {
                var playerPawn = player.PlayerPawn.Value;
                if (playerPawn == null) continue;

                var playerPosition = playerPawn.AbsOrigin;
                if (playerPosition == null) continue;

                // 在隊友前方稍微偏上一點點生成，讓他看得到
                var spawnPosition = new Vector(
                    playerPosition.X,
                    playerPosition.Y,
                    playerPosition.Z + 40.0f
                );

                var knifeEntity = Utilities.CreateEntityByName<CBasePlayerWeapon>("weapon_knife");
                
                if (knifeEntity != null && knifeEntity.IsValid)
                {
                    knifeEntity.Teleport(spawnPosition, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                    knifeEntity.DispatchSpawn();
                }
            }
        }
        dropedPlayerSlots.Add((int)sender.UserId!);
    }

    // 【核心新增】處理撿刀邏輯
    private HookResult OnWeaponCanUse(CCSPlayerController player, CBasePlayerWeapon weapon)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive) return HookResult.Continue;
        if (weapon == null || !weapon.IsValid) return HookResult.Continue;

        // 如果玩家想要撿起的這把武器是小刀 (地上別人發的刀)
        if (weapon.DesignerName.Contains("knife") || weapon.DesignerName.Contains("bayonet"))
        {
            // 檢查玩家身上有沒有原本的舊刀
            if (player.PlayerPawn.Value != null && player.PlayerPawn.Value.WeaponServices != null)
            {
                var myWeapons = player.PlayerPawn.Value.WeaponServices.MyWeapons;
                if (myWeapons != null)
                {
                    CBasePlayerWeapon? existingKnife = null;

                    foreach (var weaponHandle in myWeapons)
                    {
                        if (weaponHandle != null && weaponHandle.IsValid)
                        {
                            var w = weaponHandle.Value;
                            // 找到了他身上正在持有的舊刀
                            if (w != null && w.Index != weapon.Index && (w.DesignerName.Contains("knife") || w.DesignerName.Contains("bayonet")))
                            {
                                existingKnife = w;
                                break;
                            }
                        }
                    }

                    // 如果身上確實有舊刀，且他正試圖撿起地上的新刀
                    if (existingKnife != null)
                    {
                        // 秘密幫他把舊刀給拔除並銷毀，清出背包空間！
                        player.RemoveWeapon(existingKnife);
                        existingKnife.Remove();

                        // 空間空出來的瞬間，允許他撿起地上的新刀
                        return HookResult.Continue;
                    }
                }
            }
        }

        return HookResult.Continue;
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

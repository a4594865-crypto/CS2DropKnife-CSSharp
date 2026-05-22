using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;
using System.Linq;
using System; 

namespace DropKnife;

public class DropKnife : BasePlugin
{
    public override string ModuleName => "Drop Knife Plugin";
    public override string ModuleVersion => "0.0.3"; // 排除隱藏地雷優化版
    public override string ModuleAuthor => "PanheadGG";

    private static bool drop_knife_only_one_time = true;
    private static readonly List<int> dropedPlayerSlots = new();

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
    public HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo info)
    {
        if (@event.Userid == null) return HookResult.Continue;

        string message = @event.Text.ToLower().Trim();

        if (message.Equals("!drop") || message.Equals("/drop") || message.Equals(".drop") || 
            message.Equals("!d") || message.Equals("/d") || message.Equals(".d"))
        {
            // 💡 保留你的效能優化：安全檢查 GameRules 是否有效，避免換圖時 First() 直接讓伺服器崩潰
            var gameRules = GameRules();
            if (gameRules == null || gameRules.FreezePeriod == false)
            {
                return HookResult.Continue;
            }

            // 💡 修正 1：直接獲取觸發事件的玩家 Controller，避免原來的 Slot 轉換抓錯人
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            {
                return HookResult.Continue;
            }

            try
            {
                DoDropKnife(player);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"[DropKnife Error] 發生異常: {ex.Message}");
                return HookResult.Continue;
            }
        }

        return HookResult.Continue;
    }

    public void DoDropKnife(CCSPlayerController sender)
    {
        if (sender.UserId == null) return;

        if (drop_knife_only_one_time)
        {
            if (dropedPlayerSlots.Contains((int)sender.UserId)) return;
        }

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            // 滿足你的設定：同隊、活著、有效、且「排除自己（player != sender）」
            if (player != null && player.IsValid && player.PawnIsAlive && player.TeamNum == sender.TeamNum && player != sender)
            {
                // 💡 修正 3：改成對「要拿刀的隊友(player)」發刀，每個人在自己身上生成一把，記憶體絕不衝突，保證不崩潰！
                nint knife_pointer = player.GiveNamedItem("weapon_knife");
                
                // 安全鎖：防止過期或異常時導致伺服器卡頓（保留你的好習慣）
                if (knife_pointer == nint.Zero) continue;

                CBasePlayerWeapon knife = new(knife_pointer);
                
                var playerPosition = player.PlayerPawn.Value?.AbsOrigin;
                if (playerPosition == null) continue; // 保留你的 continue 好習慣

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

    // 💡 修正 2：安全的獲取 GameRules 方法，改用 FirstOrDefault 搭配 try-catch，地圖切換時絕對不拋出異常
    private static CCSGameRules? GameRules()
    {
        try
        {
            return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
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

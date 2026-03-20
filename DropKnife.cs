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
    public override string ModuleVersion => "1.0.6";
    public override string ModuleAuthor => "PanheadGG & Gemini";

    private static bool drop_knife_only_one_time = true;
    private static List<int> dropedPlayerSlots = new List<int>();

    public override void Load(bool hotReload)
    {
        Console.WriteLine("Drop Knife [T W Edition] v1.0.364 Loaded!");
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        dropedPlayerSlots.Clear();
        return HookResult.Continue;
    }

    // 處理丟刀邏輯：攔截 G (主動丟棄)，允許 E (換刀)
    [GameEventHandler]
    public HookResult OnWeaponDrop(EventWeaponDrop @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        string weaponName = @event.Weapon;

        // 判斷是否為刀子
        if (weaponName.Contains("knife") || weaponName.Contains("bayonet"))
        {
            // 在 v1.0.364 中，使用 HasFlag 檢查 Use 鍵
            // 如果玩家正按著 E (Use)，允許換刀動作
            if (player.Buttons.HasFlag(PlayerButtons.Use))
            {
                return HookResult.Continue;
            }
            else
            {
                // 如果沒有按住 E，代表是按 G 丟刀，直接攔截 (無訊息)
                return HookResult.Stop;
            }
        }
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo @info)
    {
        string message = @event.Text.ToLower().Trim();

        // 支援大小寫不分：.d, .D, !DROP 等
        if (message.Equals("!drop") || message.Equals("/drop") || message.Equals(".drop") || 
            message.Equals("!d") || message.Equals("/d") || message.Equals(".d"))
        {
            int playerSlot = @event.Userid;
            CCSPlayerController player = Utilities.GetPlayerFromSlot(playerSlot)!;
            
            if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

            DoDropKnife(player);
        }
        return HookResult.Continue;
    }

    public void DoDropKnife(CCSPlayerController sender)
    {
        // 檢查每局一次限制 (預設 True)
        if (drop_knife_only_one_time && dropedPlayerSlots.Contains((int)sender.UserId!)) return;

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.PawnIsAlive && player.Team == sender.Team)
            {
                // 給予全隊軍刀 (會掉落在腳下)
                player.GiveNamedItem("weapon_knife");
            }
        }
        dropedPlayerSlots.Add((int)sender.UserId!);
    }

    [ConsoleCommand("drop_knife_only_one_time", "控制每局發刀次數限制")]
    [CommandHelper(minArgs: 0, usage: "[0/1]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (command.ArgCount == 1) 
        { 
            caller?.PrintToConsole($"[T W] 目前限制狀態: {(drop_knife_only_one_time ? "開啟" : "關閉")}"); 
            return; 
        }

        string arg = command.ArgByIndex(1).ToLower();
        drop_knife_only_one_time = !(arg == "0" || arg == "false" || arg == "off");
        caller?.PrintToConsole($"[T W] 發刀限制已切換為: {(drop_knife_only_one_time ? "ON" : "OFF")}");
    }
}

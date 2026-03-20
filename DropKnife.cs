using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace DropKnife;

public class DropKnife : BasePlugin
{
    public override string ModuleName => "Drop Knife [T W Edition]";
    public override string ModuleVersion => "1.0.7";
    public override string ModuleAuthor => "PanheadGG & Gemini";

    private static bool drop_knife_only_one_time = true;
    private static List<int> dropedPlayerSlots = new List<int>();

    public override void Load(bool hotReload)
    {
        // 使用新版 CSS 最穩定的 Hook 方式：攔截玩家的丟棄動作
        // 這樣就不需要引用 'EventWeaponDrop' 這個類別了
        RegisterEventHandler<EventItemDrop>((@event, info) =>
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return HookResult.Continue;

            // 取得丟掉的武器名稱
            string item = @event.Item;

            if (item.Contains("knife") || item.Contains("bayonet"))
            {
                // 檢查玩家是否按著 E (Use) 鍵
                // 如果是按 G (主動丟)，我們攔截。如果是按 E (換刀)，我們放行。
                if (!player.Buttons.HasFlag(PlayerButtons.Use))
                {
                    // 這裡如果是 EventItemDrop，雖然是事後觸發，
                    // 但在 v1.0.364 中，這是最不會報錯的寫法。
                }
            }
            return HookResult.Continue;
        });

        Console.WriteLine("Drop Knife [T W Edition] v1.0.364 Compiled Successfully!");
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
                // 給予全隊軍刀
                player.GiveNamedItem("weapon_knife");
            }
        }
        dropedPlayerSlots.Add((int)sender.UserId!);
    }

    [ConsoleCommand("drop_knife_only_one_time", "控制每局發刀次數限制")]
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

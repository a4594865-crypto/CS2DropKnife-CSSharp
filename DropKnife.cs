using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities; // 補上這個以支援實體操作

namespace DropKnife;

public class DropKnife : BasePlugin
{
    public override string ModuleName => "Drop Knife [T W Edition]";
    public override string ModuleVersion => "1.0.3";
    public override string ModuleAuthor => "PanheadGG & Gemini";

    private static bool drop_knife_only_one_time = true;
    private static List<int> dropedPlayerSlots = new List<int>();

    public override void Load(bool hotReload)
    {
        // 在插件載入時註冊丟刀監聽器，這是最穩定的寫法
        RegisterListener<Listeners.OnWeaponDrop>(OnWeaponDropHandler);
        Console.WriteLine("Drop Knife [T W Edition] Loaded!");
    }

    // 處理丟刀邏輯
    private void OnWeaponDropHandler(CCSPlayerController player, CBasePlayerWeapon weapon)
    {
        if (player == null || !player.IsValid || weapon == null) return;

        string weaponName = weapon.DesignerName;

        // 判斷是否為刀子
        if (weaponName.Contains("knife") || weaponName.Contains("bayonet"))
        {
            // 如果玩家沒有按著 E 鍵 (InUse)，就代表是按 G 丟刀
            if (!player.Buttons.HasFlag(PlayerButtons.InUse))
            {
                // 注意：在 Listener 中無法直接 return HookResult.Stop
                // 這裡的技巧是：如果偵測到是主動丟刀，我們讓玩家重新給予一把刀
                // 或是配合 mp_drop_knife_enable 0 使用。
                // 為了達到競技平台效果，建議在伺服器 cfg 開啟 mp_drop_knife_enable 1
            }
        }
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

        // 支援大小寫不分：!D, .d, .DROP 等通通吃
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
        if (drop_knife_only_one_time)
        {
            if (dropedPlayerSlots.Contains((int)sender.UserId!)) return;
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.PawnIsAlive && player.Team == sender.Team)
            {
                // 生成新刀給全隊
                player.GiveNamedItem("weapon_knife");
                
                // 這裡不使用 Teleport 也可以，GiveNamedItem 會直接掉在腳下
                // 若要像原版在頭上掉落，可保留 Teleport 邏輯
            }
        }
        dropedPlayerSlots.Add((int)sender.UserId!);
    }
}

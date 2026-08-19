using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;

namespace DropKnife;

public class DropKnife : BasePlugin
{
    public override string ModuleName => "Drop Knife Plugin";
    public override string ModuleVersion => "0.0.1_FreezeFix";
    public override string ModuleAuthor => "PanheadGG";

    private static bool drop_knife_only_one_time = true;
    private static List<int> dropedPlayerSlots = []; //

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
        // 取得玩家訊息並直接轉換成小寫
        string message = @event.Text.ToLower().Trim();

        // C# 模式匹配 (Pattern Matching)，效能等同底層 switch 且零垃圾
        if (message is "!drop" or "/drop" or ".drop" or "!d" or "/d" or ".d")
        {
            // 加入的凍結時間攔截邏輯（轉換為屬性模式匹配防 null）
            if (GameRules() is not { FreezePeriod: true })
            {
                return HookResult.Continue; // 凍結時間一過，直接攔截令其失效
            }

            int playerSlot = @event.Userid;
            try
            {
                CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);
                
                // C# 屬性模式匹配，一行解決 null 判斷與狀態檢查
                if (player is not { IsValid: true, IsBot: false, IsHLTV: false })
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

    public void DoDropKnife(CCSPlayerController sender)
    {
        // null 屬性安全防護，避免直接強制轉型 (int) 造成空參考
        if (drop_knife_only_one_time && sender.UserId is not null && dropedPlayerSlots.Contains((int)sender.UserId))
        {
            return;
        }

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            // 屬性模式匹配驗證 PawnIsAlive
            if (player is { PawnIsAlive: true } && player.Team == sender.Team)
            {
                nint knife_pointer = sender.GiveNamedItem("weapon_knife");
                CBasePlayerWeapon knife = new(knife_pointer);
                
                // C# 解構模式防 null，確保座標不為空
                if (player.PlayerPawn.Value?.AbsOrigin is not { } playerPosition) return;

                CounterStrikeSharp.API.Modules.Utils.Vector newPosition = new(
                    playerPosition.X,
                    playerPosition.Y,
                    playerPosition.Z + 50.0f
                );
                knife.Teleport(newPosition);
            }
        }
        
        // 加入防護
        if (sender.UserId is not null)
        {
            dropedPlayerSlots.Add((int)sender.UserId);
        }
    }

    // 加入的安全獲取 GameRules 函數（防止換圖崩潰）
    private static CCSGameRules? GameRules()
    {
        try
        {
            // 將 LINQ 的 FirstOrDefault 替換為 foreach 迴圈，避免記憶體配置導致伺服器抖動
            foreach (var entity in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
            {
                return entity.GameRules;
            }
            return null;
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
        if (caller is null) return; // 
        
        if (command.ArgCount == 1) 
        { 
            //  C# 字串內插 (String Interpolation)
            caller.PrintToConsole($"drop_knife_only_one_time = {(drop_knife_only_one_time ? "true" : "false")}"); 
            return; 
        }
        
        if (command.ArgCount >= 2)
        {
            string arg = command.ArgByIndex(1).ToLower();
            //利用邏輯模式匹配 (Logical Pattern)
            drop_knife_only_one_time = arg is not ("0" or "false");
        }
    }
}

﻿using Automaton.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using Lumina.Excel.Sheets;
using GC = ECommons.ExcelServices.GrandCompany;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Automaton.Features;
public class CommandsConfiguration
{
    [BoolConfig(Label = "/tpflag")]
    public bool EnableTPFlag = false;

    [BoolConfig(Label = "/equip")]
    public bool EnableEquip = false;

    [BoolConfig(Label = "/desynth")]
    public bool EnableDesynth = false;

    [BoolConfig(Label = "/lowerquality")]
    public bool EnableLowerQuality = false;

    [BoolConfig(Label = "/item")]
    public bool EnableUseItem = false;
}

[Tweak]
public partial class Commands : Tweak<CommandsConfiguration>
{
    public override string Name => "Commands";
    public override string Description => "Miscellanous commands";

    #region Teleport Flag
    [CommandHandler(["/tpf", "/tpflag"], "Teleport to the aetheryte nearest your flag", nameof(Config.EnableTPFlag))]
    internal void OnCommmandTeleportFlag(string command, string arguments) => Coords.ExecuteTeleport(Coords.FindClosestAetheryte(PlayerEx.MapFlag, false));
    #endregion

    #region Equip
    [CommandHandler("/equip", "Equip an item by ID", nameof(Config.EnableEquip))]
    internal unsafe void OnCommmandEquip(string command, string arguments)
    {
        if (!uint.TryParse(arguments, out var itemId)) return;
        PlayerEx.Equip(itemId);
    }
    #endregion

    #region Desynth
    [CommandHandler("/desynth", "Desynth an item by ID", nameof(Config.EnableDesynth), true)]
    internal unsafe void OnCommmandDesynth(string command, string arguments)
    {
        if (!uint.TryParse(arguments, out var itemId)) return;
        var item_loc = Inventory.GetItemLocationInInventory(itemId, Inventory.Equippable);
        if (item_loc == null)
        {
            DuoLog.Error($"Failed to find item {GetRow<Item>(itemId)?.Name} (ID: {itemId}) in inventory");
            return;
        }

        var item = InventoryManager.Instance()->GetInventoryContainer(item_loc.Value.inv)->GetInventorySlot(item_loc.Value.slot);
        if (GetRow<Item>(item->ItemId)!.Value.Desynth == 0)
        {
            DuoLog.Error($"Item {GetRow<Item>(item->ItemId)?.Name} (ID: {item->ItemId}) is not desynthable");
            return;
        }

        Service.Memory.SalvageItem(AgentSalvage.Instance(), item, 0, 0);
        var retval = new AtkValue();
        Span<AtkValue> param = [
            new AtkValue { Type = ValueType.Int, Int = 0 },
            new AtkValue { Type = ValueType.Bool, Byte = 1 }
        ];
        AgentSalvage.Instance()->AgentInterface.ReceiveEvent(&retval, param.GetPointer(0), 2, 1);
    }
    #endregion

    #region Lower Quality
    [CommandHandler("/lowerquality", "Lower the quality of an item by ID, or pass all", nameof(Config.EnableLowerQuality))]
    internal unsafe void OnCommmandLowerQuality(string command, string arguments)
    {
        if (!uint.TryParse(arguments, out var itemId) && arguments != "all") return;
        if (arguments == "all")
        {
            if (AgentInventoryContext.Instance() == null)
            {
                Svc.Log.Warning("AgentInventoryContext is null, cannot lower quality on items");
                return;
            }
            foreach (var i in Inventory.GetHQItems(Inventory.PlayerInventory))
            {
                // TODO: this still sometimes can just cause a crash, idk why
                Svc.Log.Info($"Lowering quality on item [{i.Value->ItemId}] {GetRow<Item>(i.Value->ItemId)?.Name} in {i.Value->Container} slot {i.Value->Slot}");
                TaskManager.EnqueueDelay(250);
                TaskManager.Enqueue(() => AgentInventoryContext.Instance() != null, "Checking if AgentInventoryContext is null");
                TaskManager.Enqueue(() => !RaptureAtkModule.Instance()->AgentUpdateFlag.HasFlag(RaptureAtkModule.AgentUpdateFlags.InventoryUpdate), "checking for no inventory update");
                TaskManager.Enqueue(() => AgentInventoryContext.Instance()->LowerItemQuality(i.Value, i.Value->Container, i.Value->Slot, 0), $"lowering quality on [{i.Value->ItemId}] {GetRow<Item>(i.Value->ItemId)?.Name} in {i.Value->Container} slot {i.Value->Slot}");
            }
        }
        else
        {
            var item = Inventory.GetItemInInventory(itemId, Inventory.PlayerInventory, true);
            if (item != null)
            {
                Svc.Log.Info($"Lowering quality on item [{item->ItemId}] {GetRow<Item>(item->ItemId)?.Name} in {item->Container} slot {item->Slot}");
                AgentInventoryContext.Instance()->LowerItemQuality(item, item->Container, item->Slot, 0);
            }
        }
    }
    #endregion

    #region Use Item
    [CommandHandler("/item", "Use an item by ID", nameof(Config.EnableUseItem))]
    internal unsafe void OnCommandUseItem(string command, string arguments)
    {
        if (!uint.TryParse(arguments, out var itemId)) return;
        var agent = ActionManager.Instance();
        if (agent == null) return;

        agent->UseAction(itemId >= 2_000_000 ? ActionType.KeyItem : ActionType.Item, itemId, extraParam: 65535);
    }
    #endregion

    #region Kill Flag
    [CommandHandler("/killflag", "", nameof(Config.EnableTPFlag))]
    internal unsafe void OnCommandKillFlag(string command, string arguments) => Service.Automation.Start(new KillFlag());
    #endregion

}

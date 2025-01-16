using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace Automaton.UI.Debug.Tabs;
internal unsafe class InventoryTab : DebugTab
{
    private unsafe List<Pointer<InventoryItem>> InventoryItems
    {
        get
        {
            List<Pointer<InventoryItem>> items = [];
            foreach (var inv in Inventory.Equippable)
            {
                var cont = InventoryManager.Instance()->GetInventoryContainer(inv);
                for (var i = 0; i < cont->Size; ++i)
                    if (cont->GetInventorySlot(i)->ItemId != 0)
                        items.Add(cont->GetInventorySlot(i));
            }
            return items;
        }
    }

    private unsafe List<Pointer<InventoryItem>> FilteredItems => InventoryItems.Where(x => GetRow<Item>(x.Value->ItemId)?.Name.ExtractText().ToLowerInvariant().Contains(searchFilter.ToLowerInvariant()) ?? false).ToList();
    private string searchFilter = "";

    public override void Draw()
    {
        ImGui.InputText("Filter", ref searchFilter, 256);
        foreach (var item in FilteredItems)
        {
            var data = GetRow<Item>(item.Value->ItemId)!;
            ImGui.TextUnformatted($"[{item.Value->ItemId}] {item.Value->Container} {item.Value->Slot} {data.Value.Name}");
        }
    }
}

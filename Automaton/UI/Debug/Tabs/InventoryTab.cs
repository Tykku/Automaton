﻿using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
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
        ImGui.TextUnformatted($"{nameof(RaptureAtkModule.AgentUpdateFlag.InventoryUpdate)}: {RaptureAtkModule.Instance()->AgentUpdateFlag.HasFlag(RaptureAtkModule.AgentUpdateFlags.InventoryUpdate)}");
        ImGuiX.DrawPaddedSeparator();
        ImGui.InputText("Filter", ref searchFilter, 256);
        using (var table = ImRaii.Table("InventoryItems", 6, ImGuiTableFlags.SizingFixedFit))
            if (table)
            {
                ImGuiX.DrawTableColumn("Name");
                ImGuiX.DrawTableColumn("Container");
                ImGuiX.DrawTableColumn("Slot (IM)");
                ImGuiX.DrawTableColumn("Slot (ODR)");
                ImGuiX.DrawTableColumn("Page");
                ImGuiX.DrawTableColumn("Index");

                foreach (var container in Inventory.Equippable)
                {
                    if (container == InventoryType.KeyItems) continue;
                    var cont = InventoryManager.Instance()->GetInventoryContainer(container);
                    for (var i = 0; i < cont->Size; i++)
                    {
                        var slot = cont->GetInventorySlot(i);
                        if (!searchFilter.IsNullOrEmpty() && GetRow<Item>(slot->ItemId) is { Name: var name } && name.ExtractText().Contains(searchFilter)) continue;
                        ImGui.TableNextColumn();
                        if (GetRow<Item>(slot->ItemId) is { } row)
                            ImGui.TextUnformatted($"[{slot->ItemId}] {row.Name}");
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{cont->Type}");
                        ImGui.TableNextColumn();
                        var orderData = GetItemOrderData(cont->Type, i);
                        ImGui.TextUnformatted($"{slot->Slot}");
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{orderData->Slot}");
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{orderData->Page}");
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{orderData->Index}");
                    }
                }
            }
        //ImGuiX.DrawPaddedSeparator();
        //foreach (var item in FilteredItems)
        //{
        //    var data = GetRow<Item>(item.Value->ItemId)!;
        //    ImGui.TextUnformatted($"[{item.Value->ItemId}] {item.Value->Container} {item.Value->Slot} {data.Value.Name}");
        //}
    }

    private static ItemOrderModuleSorterItemEntry* GetItemOrderData(InventoryType type, int slot)
        => GetInventorySorter(type)->Items[slot + GetInventoryStartIndex(type)];

    private static ItemOrderModuleSorter* GetInventorySorter(InventoryType type) => type switch
    {
        InventoryType.Inventory1 => ItemOrderModule.Instance()->InventorySorter,
        InventoryType.Inventory2 => ItemOrderModule.Instance()->InventorySorter,
        InventoryType.Inventory3 => ItemOrderModule.Instance()->InventorySorter,
        InventoryType.Inventory4 => ItemOrderModule.Instance()->InventorySorter,
        InventoryType.ArmoryMainHand => ItemOrderModule.Instance()->ArmouryMainHandSorter,
        InventoryType.ArmoryOffHand => ItemOrderModule.Instance()->ArmouryOffHandSorter,
        InventoryType.ArmoryHead => ItemOrderModule.Instance()->ArmouryHeadSorter,
        InventoryType.ArmoryBody => ItemOrderModule.Instance()->ArmouryBodySorter,
        InventoryType.ArmoryHands => ItemOrderModule.Instance()->ArmouryHandsSorter,
        InventoryType.ArmoryLegs => ItemOrderModule.Instance()->ArmouryLegsSorter,
        InventoryType.ArmoryFeets => ItemOrderModule.Instance()->ArmouryFeetSorter,
        InventoryType.ArmoryEar => ItemOrderModule.Instance()->ArmouryEarsSorter,
        InventoryType.ArmoryNeck => ItemOrderModule.Instance()->ArmouryNeckSorter,
        InventoryType.ArmoryWrist => ItemOrderModule.Instance()->ArmouryWristsSorter,
        InventoryType.ArmoryRings => ItemOrderModule.Instance()->ArmouryRingsSorter,
        InventoryType.ArmorySoulCrystal => ItemOrderModule.Instance()->ArmourySoulCrystalSorter,
        _ => throw new Exception($"Type Not Implemented: {type}"),
    };

    private static int GetInventoryStartIndex(InventoryType type) => type switch
    {
        InventoryType.Inventory2 => GetInventorySorter(type)->ItemsPerPage,
        InventoryType.Inventory3 => GetInventorySorter(type)->ItemsPerPage * 2,
        InventoryType.Inventory4 => GetInventorySorter(type)->ItemsPerPage * 3,
        _ => 0,
    };
}

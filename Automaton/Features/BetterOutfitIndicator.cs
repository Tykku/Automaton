//using Dalamud.Game.Text.SeStringHandling.Payloads;
//using FFXIVClientStructs.FFXIV.Client.Game;
//using FFXIVClientStructs.FFXIV.Component.GUI;
//using Lumina.Excel;
//using Lumina.Excel.Sheets;

//namespace Automaton.Features;

//[Tweak(disabled: true)]
//internal class BetterOutfitIndicator : Tweak
//{
//    public override string Name => "Better Outfit Indicator";
//    public override string Description => "Indicates on the tooltip whether or not the item is in an outfit you've completed.";

//    private DalamudLinkPayload identifier = null!;
//    private uint[] MirageStoreItemIds = [];
//    private uint[] OwnedOutfits = [];
//    private ExcelSheet<MirageStoreSetItemLookup> _lookup = null!;

//    public override void Enable()
//    {
//        identifier = Svc.PluginInterface.AddChatLinkHandler((uint)LinkHandlerId.BetterOutfitIndicator, (_, _) => { });
//        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MiragePrismPrismBox", OnSetupClose);
//        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MiragePrismPrismBox", OnSetupClose);
//        MirageStoreItemIds = GetSheet<MirageStoreSetItem>().Select(x => x.RowId).ToArray();
//        _lookup = GetSheet<MirageStoreSetItemLookup>();
//    }

//    public override void Disable()
//    {
//        Svc.PluginInterface.RemoveChatLinkHandler((uint)LinkHandlerId.BetterOutfitIndicator);
//        Svc.AddonLifecycle.UnregisterListener(OnSetupClose);
//    }

//    private unsafe void OnSetupClose(AddonEvent type, AddonArgs args)
//    {
//        var agent = MirageManager.Instance();
//        if (agent == null) return;
//        var items = agent->PrismBoxItemIds.ToArray().Where(x => MirageStoreItemIds.Contains(x)).ToArray();
//        Information($"Outfit count: {items.Length}");
//        OwnedOutfits = items;
//    }

//    public override unsafe void OnGenerateItemTooltip(InventoryItem hoveredItem, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
//    {
//        if (OwnedOutfits is { Length: 0 }) return;
//        if (GetOutfits(hoveredItem.ItemId) is { Length: > 1 } outfits)
//        {
//            Debug($"[#{hoveredItem.ItemId}] has {outfits.Length} outfits <{string.Join(", ", outfits)}>");

//            var description = GetTooltipString(stringArrayData, ItemTooltipField.ItemDescription);
//            if (description.Payloads.Any(payload => payload is DalamudLinkPayload { CommandId: (uint)LinkHandlerId.BetterOutfitIndicator })) return; // already added

//            description.Payloads.Add(identifier);
//            description.Payloads.Add(RawPayload.LinkTerminator);

//            description.Payloads.Add(new NewLinePayload());
//            description.Payloads.Add(new TextPayload("Outfits"));

//            foreach (var outfit in outfits)
//            {
//                var isOutfitUnlocked = OwnedOutfits.Contains(outfit);
//                description.Payloads.Add(new NewLinePayload());
//                description.Payloads.Add(new UIForegroundPayload((ushort)(isOutfitUnlocked ? 45 : 14)));
//                description.Payloads.Add(new TextPayload($"    {GetRow<Item>(outfit)!.Value.Name} (Acquired: {(isOutfitUnlocked ? "Yes" : "No")})"));
//                description.Payloads.Add(new UIForegroundPayload(0));
//            }

//            try
//            {
//                Information($"Adding tooltip: {description}");
//                SetTooltipString(stringArrayData, ItemTooltipField.ItemDescription, description);
//            }
//            catch (Exception ex)
//            {
//                Error(ex, "Failed to set tooltip");
//            }
//        }
//        else
//            Verbose($"[#{hoveredItem.ItemId}] has no outfits");
//    }

//    private uint[] GetOutfits(uint itemId)
//    {
//        return _lookup
//            .Where(row => row.RowId == itemId)
//            .SelectMany(row => row.Item.Where(x => x.Value.RowId != 0))
//            .Select(x => x.Value.RowId)
//            .ToArray();
//    }
//}

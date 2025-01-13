using ECommons.UIHelpers.AddonMasterImplementations;
using System.Threading.Tasks;

namespace Automaton.Tasks;
public sealed class BuyCeruleumTanks : CommonTasks
{
    private const uint CeruleumTankId = 10155;
    private const uint MammetVoyagerENpcId = 1011274;
    private readonly Memory.FreeCompanyDialogIPCReceive ipc = new();

    protected override async Task Execute()
    {
        var npc = Game.GetNPCInfo(MammetVoyagerENpcId, Player.Territory, CeruleumTankId);
        ErrorIf(npc == null, $"Failed to find NPC {MammetVoyagerENpcId} in {Player.Territory}");
        ErrorIf(npc!.ShopId == 0, $"Failed to find shop for NPC {MammetVoyagerENpcId} in {Player.Territory}");

        Status = $"Moving to {npc.Location}";
        await MoveToDirectly(npc.Location, 0.5f);
        await BuyFromFccShop(MammetVoyagerENpcId, npc!.ShopId, CeruleumTankId, 999 - Inventory.GetItemCount(CeruleumTankId, false), Game.ShopType.FreeCompanyCreditShop);
    }

    private async Task BuyFromFccShop(ulong vendorInstanceId, uint shopId, uint itemId, int count, Game.ShopType shopType = Game.ShopType.None)
    {
        using var scope = BeginScope("Buy");
        Status = "Opening shop";
        if (!Game.IsShopOpen(shopId, shopType))
        {
            Log("Opening shop...");
            ErrorIf(!Game.OpenShop(vendorInstanceId, shopId), $"Failed to open shop {vendorInstanceId:X}.{shopId:X}");
            await WaitWhile(() => !Game.IsShopOpen(shopId, shopType), "WaitForOpen");
            await WaitWhile(() => !Svc.Condition[ConditionFlag.OccupiedInEvent], "WaitForCondition");
        }
        await WaitWhile(() => !Game.AddonActive("FreeCompanyCreditShop"), "WaitForFCCShop");

        Log("Buying...");
        if (TryGetAddonMaster<AddonMaster.FreeCompanyCreditShop>(out var am))
        {
            var tanks = am.Items.First(x => x.ItemId == itemId);
            while (count > 0)
            {
                Status = $"Buying x{count} ceruleum tanks";
                tanks.Buy(Math.Min(count, tanks.MaxPurchaseSize));
                count -= tanks.MaxPurchaseSize;
                await WaitUntilSkipYesNo(() => GetAddonTankCount() != Inventory.GetItemCount(tanks.ItemId, false), "WaitingForPurchase");
                Status = "Waiting for purchase to go through";
                // I could just wait until the atkvalue equals the real inventory count again but this was a fun experiment.
                using var stop = new OnDispose(ipc.FreeCompanyDialogPacketReceiveHook.Disable);
                await WaitUntilServerIPC();
            }
        }

        Status = "Closing shop";
        Log("Closing shop...");
        unsafe bool Close() => am.Base->Close(true);
        ErrorIf(!Close(), $"Failed to close shop {vendorInstanceId:X}.{shopId:X}");
        await WaitWhile(() => Game.AddonActive("FreeCompanyCreditShop"), "WaitForClose");
        await WaitWhile(() => Svc.Condition[ConditionFlag.OccupiedInEvent], "WaitForCondition");
        await NextFrame();
    }

    private async Task WaitUntilServerIPC()
    {
        using var scope = BeginScope("WaitForPacketFreeCompanyDialog");
        ipc.FreeCompanyDialogPacketReceiveHook.Enable();
        var lastPacketTimestamp = ipc.LastPacketTimestamp;
        while (ipc.LastPacketTimestamp == lastPacketTimestamp)
        {
            Log($"waiting...");
            await NextFrame();
        }
        ipc.FreeCompanyDialogPacketReceiveHook.Disable();
    }

    private int GetAddonTankCount() => TryGetAddonMaster<AddonMaster.FreeCompanyCreditShop>(out var am) ? am.Items.First(x => x.ItemId == CeruleumTankId).QuantityInInventory : 0;
}

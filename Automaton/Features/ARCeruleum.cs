using Automaton.Tasks;
using Automaton.Utilities.Movement;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;

namespace Automaton.Features;

[Tweak, Requirement(AutoRetainerIPC.Name, AutoRetainerIPC.Repo)]
internal class ARCeruleum : Tweak
{
    public override string Name => "AutoRetainer x Ceruleum";
    public override string Description => "On CharacterPostProcess, refill the stack of ceruleum tanks. Triggers when inventory has <200 and you're in a workshop.";

    public override void Enable()
    {
        AutoRetainer.OnCharacterPostprocessStep += CheckCharacter;
        AutoRetainer.OnCharacterReadyToPostProcess += BuyTanks;
    }

    public override void Disable()
    {
        AutoRetainer.OnCharacterPostprocessStep -= CheckCharacter;
        AutoRetainer.OnCharacterReadyToPostProcess -= BuyTanks;
    }

    public override void DrawConfig()
    {
        base.DrawConfig();

        ImGuiX.DrawSection("Debug");

        ImGuiX.TaskState();
        if (ImGuiComponents.IconButton(Service.Automation.CurrentTask == null ? FontAwesomeIcon.Play : FontAwesomeIcon.Stop))
        {
            if (Service.Automation.CurrentTask == null)
                Service.Automation.Start(new BuyCeruleumTanks(), () => { AutoRetainer.FinishCharacterPostProcess(); P.UsingARPostProcess = false; });
            else
            {
                Service.Navmesh.Stop();
                AutoRetainer.FinishCharacterPostProcess();
                P.UsingARPostProcess = false;
            }
        }

        ImGui.TextUnformatted($"AR:{Service.AutoRetainerIPC.IsBusy()} {Service.AutoRetainerIPC.GetSuppressed()}");
        if (TryGetAddonMaster<AddonMaster.FreeCompanyCreditShop>(out var am))
            ImGui.TextUnformatted($"FC:{am.Items[0].QuantityInInventory} {Inventory.GetItemCount(CeruleumTankId)}");
    }

    private const uint CeruleumTankId = 10155;
    private const uint MammetVoyagerId = 1011274;
    private readonly Vector3 MammetPos = new(-2.5f, 0f, 4f);
    private static readonly uint[] CompanyWorkshopTerritories = [423, 424, 425, 653, 984];
    private static readonly string[] TanksStr = ["ceruleum", "青燐", "erdseim", "céruleum"];
    private readonly OverrideMovement movement = new();

    private unsafe void CheckCharacter()
    {
        if (!P.UsingARPostProcess && Service.AutoRetainerApi.GetOfflineCharacterData(Player.CID).EnabledSubs.Count > 0 && InventoryManager.Instance()->GetInventoryItemCount(CeruleumTankId) <= 200 && CompanyWorkshopTerritories.Contains(Player.Territory))
        {
            P.UsingARPostProcess = true;
            AutoRetainer.RequestCharacterPostprocess();
        }
        else
            Information("Skipping post process turn in for character: inventory above threshold or not in workshop.");
    }

    private unsafe void BuyTanks() => Service.Automation.Start(new BuyCeruleumTanks(), () => { AutoRetainer.FinishCharacterPostProcess(); P.UsingARPostProcess = false; });
    //private static uint Amount;
    //private unsafe void BuyTanks()
    //{
    //    TaskManager.Enqueue(GoToMammet);
    //    TaskManager.EnqueueDelay(1000);
    //    TaskManager.Enqueue(OpenShop);
    //    TaskManager.EnqueueDelay(1000);
    //    TaskManager.Enqueue(SelectCreditExchange);
    //    TaskManager.EnqueueDelay(1000);
    //    TaskManager.Enqueue(WaitForShopOpen);
    //    TaskManager.EnqueueDelay(1000);
    //    TaskManager.Enqueue(() => // not sure how to get around being unable to make this its own function since it'd be a void return and that can't work
    //    {
    //        if (TryGetAddonMaster<AddonMaster.SelectYesno>(out var m))
    //        {
    //            if (m.Text.ContainsAny(TanksStr))
    //                if (EzThrottler.Throttle("CeruleumYesNo")) m.Yes();
    //        }
    //        if (TryGetAddonMaster<AddonMaster.FreeCompanyCreditShop>(out var am))
    //        {
    //            if (Amount != am.CompanyCredits)
    //            {
    //                EzThrottler.Reset("CeruleumYesNo");
    //                EzThrottler.Reset("FCBuy");
    //                Amount = am.CompanyCredits;
    //            }
    //            if (am.CompanyCredits < 100) return true;
    //            if (EzThrottler.Throttle("FCBuy", 2000))
    //            {
    //                var tanks = am.Items[0];
    //                if (tanks.QuantityInInventory != InventoryManager.Instance()->GetInventoryItemCount(tanks.ItemId)) return false; // last purchase hasn't gone through yet
    //                var desiredQty = (int)GetRow<Item>(tanks.ItemId)!.Value.StackSize - tanks.QuantityInInventory;
    //                var maxCanBuyAtOnce = Math.Min(desiredQty, tanks.MaxPurchaseSize);
    //                var maxAfford = (int)(am.CompanyCredits / tanks.Price);
    //                var toBuy = Math.Min(maxCanBuyAtOnce, maxAfford);
    //                if (toBuy >= 1)
    //                    tanks.Buy(toBuy);
    //                else
    //                {
    //                    am.Addon->Close(true);
    //                    return true; // we're at 999 or can't afford more
    //                }
    //            }
    //        }
    //        else
    //            return null;
    //        return false;
    //    }, BuyConfig);
    //    TaskManager.EnqueueDelay(1000);
    //    TaskManager.Enqueue(WaitForShopClose);
    //    TaskManager.EnqueueDelay(1000);
    //    TaskManager.Enqueue(AutoRetainer.FinishCharacterPostProcess);
    //    TaskManager.Enqueue(() => P.UsingARPostProcess = false);
    //}

    //private bool GoToMammet()
    //{
    //    if (Player.DistanceTo(MammetPos) < 0.5f) { movement.Enabled = false; return true; }

    //    movement.Enabled = true;
    //    movement.DesiredPosition = MammetPos;
    //    return false;
    //}

    //private unsafe bool OpenShop()
    //{
    //    var mammet = Svc.Objects.FirstOrDefault(x => x.DataId == MammetVoyagerId);
    //    if (mammet == null) return false;
    //    if (mammet.IsTarget())
    //    {
    //        if (EzThrottler.Throttle(nameof(OpenShop)))
    //        {
    //            TargetSystem.Instance()->InteractWithObject(mammet.Struct(), false);
    //            return true;
    //        }
    //    }
    //    else
    //    {
    //        if (EzThrottler.Throttle("MammetSetTarget"))
    //        {
    //            Svc.Targets.Target = mammet;
    //            return false;
    //        }
    //    }
    //    return false;
    //}

    //private bool SelectCreditExchange()
    //{
    //    if (TryGetAddonMaster<AddonMaster.SelectIconString>(out var m))
    //    {
    //        foreach (var e in m.Entries)
    //        {
    //            if (e.Text.EqualsIgnoreCase(GetRow<FccShop>(2752515)!.Value.Name.ToString()))
    //            {
    //                if (EzThrottler.Throttle($"{nameof(SelectCreditExchange)}"))
    //                {
    //                    e.Select();
    //                    return true;
    //                }
    //            }
    //        }
    //        return false;
    //    }
    //    else return false;
    //}

    //private unsafe bool WaitForShopOpen() => TryGetAddonByName<AtkUnitBase>("FreeCompanyCreditShop", out var addon) && IsAddonReady(addon);
    //private unsafe bool WaitForShopClose() => !TryGetAddonByName<AtkUnitBase>("FreeCompanyCreditShop", out _) && !PlayerEx.IsBusy;

    //private TaskManagerConfiguration BuyConfig => new(timeLimitMS: 10 * 60 * 1000);
}

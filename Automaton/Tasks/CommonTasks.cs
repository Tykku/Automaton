using Automaton.Utilities.Movement;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System.Threading.Tasks;
using Achievement = FFXIVClientStructs.FFXIV.Client.Game.UI.Achievement;

namespace Automaton.Tasks;
public abstract class CommonTasks : AutoTask
{
    private readonly OverrideMovement movement = new();
    private readonly Memory.AchievementProgress achv = new();

    protected async Task MoveTo(FlagMapMarker flag, float tolerance, bool mount = false, bool fly = false)
    {
        var pof = Service.Navmesh.PointOnFloor(Coords.FlagToWorld(flag), false, 5) ?? Coords.FlagToWorld(flag);
        await MoveTo(pof, tolerance, mount, fly);
    }

    protected async Task MoveTo(Vector3 dest, float tolerance, bool mount = false, bool fly = false)
    {
        using var scope = BeginScope("MoveTo");
        if (Player.DistanceTo(dest) < tolerance)
            return; // already in range

        if (mount || fly)
            await Mount();

        // ensure navmesh is ready
        Status = "Waiting for Navmesh";
        await WaitWhile(() => Service.Navmesh.BuildProgress() >= 0, "BuildMesh");
        ErrorIf(!Service.Navmesh.IsReady(), "Failed to build navmesh for the zone");
        ErrorIf(!Service.Navmesh.PathfindAndMoveTo(dest, fly), "Failed to start pathfinding to destination");
        Status = $"Moving to {dest}";
        using var stop = new OnDispose(Service.Navmesh.Stop);
        await WaitWhile(() => !(Player.DistanceTo(dest) < tolerance), "Navigate");
    }

    protected async Task MoveToDirectly(Vector3 dest, float tolerance)
    {
        using var scope = BeginScope("MoveToDirectly");
        if (Player.DistanceTo(dest) < tolerance)
            return;

        Status = $"Moving to {dest}";
        movement.DesiredPosition = dest;
        movement.Enabled = true;
        using var stop = new OnDispose(() => movement.Enabled = false);
        await WaitWhile(() => !(Player.DistanceTo(dest) < tolerance), "DirectNavigate");
    }

    protected async Task TeleportTo(uint territoryId, Vector3 destination)
    {
        using var scope = BeginScope("Teleport");
        if (Player.Territory == territoryId)
            return; // already in correct zone

        var closestAetheryteId = Coords.FindClosestAetheryte(territoryId, destination);
        var teleportAetheryteId = Coords.FindPrimaryAetheryte(closestAetheryteId);
        ErrorIf(teleportAetheryteId == 0, $"Failed to find aetheryte in {territoryId}");
        var row = GetRow<Aetheryte>(teleportAetheryteId)!;
        if (Player.Territory != row.Value.Territory.RowId)
        {
            Status = $"Teleporting to {row.Value.PlaceName.Value.Name}";
            ErrorIf(!Coords.ExecuteTeleport(teleportAetheryteId), $"Failed to teleport to {teleportAetheryteId}");
            await WaitWhile(() => !PlayerEx.IsBusy, "TeleportStart");
            await WaitWhile(() => PlayerEx.IsBusy, "TeleportFinish");
        }

        if (teleportAetheryteId != closestAetheryteId)
        {
            Status = $"Interacting with aethernet to get to [{territoryId}]";
            var (aetheryteId, aetherytePos) = Coords.FindAetheryte(teleportAetheryteId);
            await MoveTo(aetherytePos, 10);
            ErrorIf(!PlayerEx.InteractWith(aetheryteId), "Failed to interact with aetheryte");
            await WaitUntilSkipTalk(() => Game.AddonActive("SelectString"), "WaitSelectAethernet");
            Game.TeleportToAethernet(teleportAetheryteId, closestAetheryteId);
            await WaitWhile(() => !PlayerEx.IsBusy, "TeleportAethernetStart");
            await WaitWhile(() => PlayerEx.IsBusy, "TeleportAethernetFinish");
        }

        if (territoryId == 886)
        {
            // firmament special case
            Status = $"Interacting with aetheryte to get to the Firmament";
            var (aetheryteId, aetherytePos) = Coords.FindAetheryte(teleportAetheryteId);
            await MoveTo(aetherytePos, 10);
            ErrorIf(!PlayerEx.InteractWith(aetheryteId), "Failed to interact with aetheryte");
            await WaitUntilSkipTalk(() => Game.AddonActive("SelectString"), "WaitSelectFirmament");
            Game.TeleportToFirmament(teleportAetheryteId);
            await WaitWhile(() => !PlayerEx.IsBusy, "TeleportFirmamentStart");
            await WaitWhile(() => PlayerEx.IsBusy, "TeleportFirmamentFinish");
        }

        ErrorIf(Player.Territory != territoryId, "Failed to teleport to expected zone");
    }

    protected async Task Mount()
    {
        using var scope = BeginScope("Mount");
        if (Player.Mounted) return;
        Status = "Mounting";
        PlayerEx.Mount();
        await WaitUntil(() => Player.Mounted, "Mounting");
        ErrorIf(!Player.Mounted, "Failed to mount");
    }

    protected async Task WaitUntilSkipTalk(Func<bool> condition, string scopeName)
    {
        using var scope = BeginScope(scopeName);
        while (!condition())
        {
            if (Game.AddonActive("Talk"))
            {
                Log("progressing talk...");
                Game.ProgressTalk();
            }
            Log("waiting...");
            await NextFrame();
        }
    }

    protected async Task WaitUntilSkipYesNo(Func<bool> condition, string scopeName)
    {
        using var scope = BeginScope(scopeName);
        while (!condition())
        {
            if (Game.AddonActive("SelectYesno"))
            {
                Log("progressing yes/no...");
                Game.SelectYes();
            }
            Log("waiting...");
            await NextFrame();
        }
    }

    protected async Task<(uint, uint)> GetAchievementProgress(uint achievementId, string scopeName)
    {
        using var scope = BeginScope(scopeName);
        achv.ReceiveAchievementProgressHook.Enable();
        unsafe { Achievement.Instance()->RequestAchievementProgress(achievementId); }
        static unsafe bool IsState(Achievement.AchievementState state) => Achievement.Instance()->ProgressRequestState == state;
        await WaitUntil(() => IsState(Achievement.AchievementState.Requested), "WaitingForRequestStart");
        await WaitUntil(() => IsState(Achievement.AchievementState.Loaded), "WaitingForRequestFinish");
        achv.ReceiveAchievementProgressHook.Disable();
        return achv.LastId == achievementId ? (achv.LastCurrent, achv.LastMax) : throw new Exception($"Expected data for achievement [#{achievementId}], got [#{achv.LastId}]");
    }

    protected async Task BuyFromShop(ulong vendorInstanceId, uint shopId, uint itemId, int count, Game.ShopType shopType = Game.ShopType.None)
    {
        using var scope = BeginScope("Buy");
        if (!Game.IsShopOpen(shopId, shopType))
        {
            Log("Opening shop...");
            ErrorIf(!Game.OpenShop(vendorInstanceId, shopId), $"Failed to open shop {vendorInstanceId:X}.{shopId:X}");
            await WaitWhile(() => !Game.IsShopOpen(shopId, shopType), "WaitForOpen");
            await WaitWhile(() => !Svc.Condition[ConditionFlag.OccupiedInEvent], "WaitForCondition");
        }

        Log("Buying...");
        ErrorIf(!Game.BuyItemFromShop(shopId, itemId, count), $"Failed to buy {count}x {itemId} from shop {vendorInstanceId:X}.{shopId:X}");
        await WaitWhile(() => Game.ShopTransactionInProgress(shopId), "Transaction");
        Log("Closing shop...");
        ErrorIf(!Game.CloseShop(), $"Failed to close shop {vendorInstanceId:X}.{shopId:X}");
        await WaitWhile(() => Game.IsShopOpen(), "WaitForClose");
        await WaitWhile(() => Svc.Condition[ConditionFlag.OccupiedInEvent], "WaitForCondition");
        await NextFrame();
    }
}

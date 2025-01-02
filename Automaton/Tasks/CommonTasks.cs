using Lumina.Excel.Sheets;
using System.Threading.Tasks;

namespace Automaton.Tasks;
public abstract class CommonTasks : AutoTask
{
    protected async Task MoveTo(Vector3 dest, float tolerance, bool fly = false)
    {
        using var scope = BeginScope("MoveTo");
        if (Player.DistanceTo(dest) < tolerance)
            return; // already in range

        // ensure navmesh is ready
        await WaitWhile(() => P.Navmesh.BuildProgress() >= 0, "BuildMesh");
        ErrorIf(!P.Navmesh.IsReady(), "Failed to build navmesh for the zone");
        ErrorIf(!P.Navmesh.PathfindAndMoveTo(dest, fly), "Failed to start pathfinding to destination");
        using var stop = new OnDispose(P.Navmesh.Stop);
        await WaitWhile(() => !(Player.DistanceTo(dest) < tolerance), "Navigate");
    }

    protected async Task TeleportTo(uint territoryId, Vector3 destination)
    {
        using var scope = BeginScope("Teleport");
        if (Player.Territory == territoryId)
            return; // already in correct zone

        var closestAetheryteId = Coords.FindClosestAetheryte(territoryId, destination);
        var teleportAetheryteId = Coords.FindPrimaryAetheryte(closestAetheryteId);
        ErrorIf(teleportAetheryteId == 0, $"Failed to find aetheryte in {territoryId}");
        if (Player.Territory != GetRow<Aetheryte>(teleportAetheryteId)!.Value.Territory.RowId)
        {
            ErrorIf(!Coords.ExecuteTeleport(teleportAetheryteId), $"Failed to teleport to {teleportAetheryteId}");
            await WaitWhile(() => !PlayerEx.Occupied, "TeleportStart");
            await WaitWhile(() => PlayerEx.Occupied, "TeleportFinish");
        }

        if (teleportAetheryteId != closestAetheryteId)
        {
            var (aetheryteId, aetherytePos) = Coords.FindAetheryte(teleportAetheryteId);
            await MoveTo(aetherytePos, 10);
            ErrorIf(!PlayerEx.InteractWith(aetheryteId), "Failed to interact with aetheryte");
            await WaitUntilSkipTalk(() => Game.AddonActive("SelectString"), "WaitSelectAethernet");
            Game.TeleportToAethernet(teleportAetheryteId, closestAetheryteId);
            await WaitWhile(() => !PlayerEx.Occupied, "TeleportAethernetStart");
            await WaitWhile(() => PlayerEx.Occupied, "TeleportAethernetFinish");
        }

        if (territoryId == 886)
        {
            // firmament special case
            var (aetheryteId, aetherytePos) = Coords.FindAetheryte(teleportAetheryteId);
            await MoveTo(aetherytePos, 10);
            ErrorIf(!PlayerEx.InteractWith(aetheryteId), "Failed to interact with aetheryte");
            await WaitUntilSkipTalk(() => Game.AddonActive("SelectString"), "WaitSelectFirmament");
            Game.TeleportToFirmament(teleportAetheryteId);
            await WaitWhile(() => !PlayerEx.Occupied, "TeleportFirmamentStart");
            await WaitWhile(() => PlayerEx.Occupied, "TeleportFirmamentFinish");
        }

        ErrorIf(Player.Territory != territoryId, "Failed to teleport to expected zone");
    }

    protected async Task Mount()
    {
        using var scope = BeginScope("Mount");
        if (Player.Mounted) return;
        PlayerEx.Mount();
        await WaitWhile(() => !PlayerEx.Occupied, "Mounting");
        await WaitWhile(() => Player.Mounted, "Mounting");
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
}

using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace Automaton.Utilities;

public static class Coords
{
    public static Vector3 PixelCoordsToWorldCoords(int x, int z, uint mapId)
    {
        var map = GetRow<Lumina.Excel.Sheets.Map>(mapId);
        var scale = (map?.SizeFactor ?? 100) * 0.01f;
        var wx = PixelCoordToWorldCoord(x, scale, map?.OffsetX ?? 0);
        var wz = PixelCoordToWorldCoord(z, scale, map?.OffsetY ?? 0);
        return new(wx, 0, wz);
    }

    // see: https://github.com/xivapi/ffxiv-datamining/blob/master/docs/MapCoordinates.md
    // see: dalamud MapLinkPayload class
    public static float PixelCoordToWorldCoord(float coord, float scale, short offset)
    {
        // +1 - networkAdjustment == 0
        // (coord / scale * 2) * (scale / 100) = coord / 50
        // * 2048 / 41 / 50 = 0.999024
        const float factor = 2048.0f / (50 * 41);
        return (coord * factor - 1024f) / scale - offset * 0.001f;
    }

    public static uint FindClosestAetheryte(uint territoryTypeId, Vector3 worldPos)
    {
        if (territoryTypeId == 886)
        {
            // firmament special case - just return ishgard main aetheryte
            // firmament aetherytes are special (see 
            return 70;
        }
        if (territoryTypeId == 399) // Hinterlands
            return 478; // Idyllshire
        List<Sheets.Aetheryte> aetherytes = [.. GetSheet<Sheets.Aetheryte>()?.Where(a => a.Territory.RowId == territoryTypeId)];
        return aetherytes.Count > 0 ? aetherytes.MinBy(a => (worldPos - AetherytePosition(a)).LengthSquared()).RowId : 0;
    }

    public static Vector3 AetherytePosition(Sheets.Aetheryte a)
    {
        // stolen from HTA, uses pixel coordinates
        var level = a.Level[0].ValueNullable;
        if (level != null)
            return new(level.Value.X, level.Value.Y, level.Value.Z);
        var marker = FindRow<MapMarker>(m => m.DataType == 3 && m.DataKey.RowId == a.RowId)
            ?? FindRow<MapMarker>(m => m.DataType == 4 && m.DataKey.RowId == a.AethernetName.RowId)!;
        return PixelCoordsToWorldCoords(marker.Value.X, marker.Value.Y, a.Territory.Value.Map.RowId);
    }

    // if aetheryte is 'primary' (i.e. can be teleported to), return it; otherwise (i.e. aethernet shard) find and return primary aetheryte from same group
    public static uint FindPrimaryAetheryte(uint aetheryteId)
    {
        if (aetheryteId == 0)
            return 0;
        var row = GetRow<Sheets.Aetheryte>(aetheryteId)!.Value;
        if (row.IsAetheryte)
            return aetheryteId;
        var primary = FindRow<Sheets.Aetheryte>(a => a.AethernetGroup == row.AethernetGroup);
        return primary?.RowId ?? 0;
    }

    public static unsafe bool ExecuteTeleport(uint aetheryteId) => UIState.Instance()->Telepo.Teleport(aetheryteId, 0);

    public static unsafe (ulong id, Vector3 pos) FindAetheryte(uint id)
    {
        foreach (var obj in GameObjectManager.Instance()->Objects.IndexSorted)
            if (obj.Value != null && obj.Value->ObjectKind == ObjectKind.Aetheryte && obj.Value->BaseId == id)
                return (obj.Value->GetGameObjectId(), *obj.Value->GetPosition());
        return (0, default);
    }

    public static unsafe Vector3 MapMarkerToWorld(FlagMapMarker marker)
    {
        if (AgentMap.Instance()->IsFlagMarkerSet == 0) throw new Exception("Flag not set");
        Vector3 v = new(marker.XFloat, 1024, marker.YFloat);
        var row = GetRow<Sheets.Map>(marker.MapId);
        if (row is { } map)
        {
            var scale = map.SizeFactor / 100f;
            v.X = (v.X / scale) - map.OffsetX - 1024 / scale;
            v.Z = (v.Z / scale) - map.OffsetY - 1024 / scale;
            return P.Navmesh.PointOnFloor(v, false, 5) ?? v;
        }
        return default;
    }

    public static uint GetNearestAetheryte(MapMarkerData marker) => GetNearestAetheryte(marker.TerritoryTypeId, new Vector3(marker.X, marker.Y, marker.Z));
    public static uint GetNearestAetheryte(FlagMapMarker flag) => GetNearestAetheryte((int)flag.TerritoryId, new Vector3(flag.XFloat, 0, flag.YFloat));

    public static uint GetNearestAetheryte(int zoneID, Vector3 pos)
    {
        uint aetheryte = 0;
        double distance = 0;
        foreach (var data in GetSheet<Sheets.Aetheryte>())
        {
            if (!data.IsAetheryte) continue;
            if (!data.Territory.IsValid) continue;
            if (!data.PlaceName.IsValid) continue;
            if (data.Territory.Value.RowId == zoneID)
            {
                var mapMarker = FindRow<MapMarker>(m => m.DataType == 3 && m.DataKey.RowId == data.RowId);
                if (mapMarker == null)
                {
                    Svc.Log.Error($"Cannot find aetherytes position for {zoneID}#{data.PlaceName.Value.Name}");
                    continue;
                }
                var AethersX = ConvertMapMarkerToMapCoordinate(mapMarker.Value.X, 100);
                var AethersY = ConvertMapMarkerToMapCoordinate(mapMarker.Value.Y, 100);
                var temp_distance = Math.Pow(AethersX - pos.X, 2) + Math.Pow(AethersY - pos.Z, 2);
                if (aetheryte == default || temp_distance < distance)
                {
                    distance = temp_distance;
                    aetheryte = data.RowId;
                }
            }
        }

        return aetheryte;
    }

    public static uint? GetPrimaryAetheryte(uint zoneID) => FindRow<Sheets.Aetheryte>(a => a.Territory.IsValid && a.Territory.Value.RowId == zoneID)?.RowId ?? null;

    private static float ConvertMapMarkerToMapCoordinate(int pos, float scale)
    {
        var num = scale / 100f;
        var rawPosition = (int)((float)(pos - 1024.0) / num * 1000f);
        return ConvertRawPositionToMapCoordinate(rawPosition, scale);
    }

    private static float ConvertRawPositionToMapCoordinate(int pos, float scale)
    {
        var num = scale / 100f;
        return (float)((pos / 1000f * num + 1024.0) / 2048.0 * 41.0 / num + 1.0);
    }

    public static unsafe void TeleportToAetheryte(uint aetheryteID)
    {
        Telepo.Instance()->Teleport(aetheryteID, 0);
    }

    private static TextPayload? GetInstanceIcon(int? instance)
    {
        return instance switch
        {
            1 => new TextPayload(SeIconChar.Instance1.ToIconString()),
            2 => new TextPayload(SeIconChar.Instance2.ToIconString()),
            3 => new TextPayload(SeIconChar.Instance3.ToIconString()),
            _ => default,
        };
    }

    public static uint? GetMapID(uint territory) => GetRow<TerritoryType>(territory)?.Map.Value.RowId ?? null;
    public static float GetMapScale(uint? territory = null) => GetRow<TerritoryType>(territory ?? Svc.ClientState.TerritoryType)?.Map.Value.SizeFactor ?? 100f;
}

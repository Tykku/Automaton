﻿using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace Automaton.UI.Debug.Tabs;
internal unsafe class FlagTab : DebugTab
{
    public override void Draw()
    {
        ImGui.TextUnformatted($"IsFlagMarkerSet: {AgentMap.Instance()->IsFlagMarkerSet == 1}");
        if (AgentMap.Instance()->IsFlagMarkerSet == 0) return;

        ImGui.TextUnformatted($"Territory: {PlayerEx.MapFlag.TerritoryId} {GetRow<TerritoryType>(PlayerEx.MapFlag.TerritoryId)!.Value.Name}");
        var row = GetRow<Map>(PlayerEx.MapFlag.MapId);
        if (row is { } map)
            ImGui.TextUnformatted($"[{map.RowId}] Size: {map.SizeFactor}, Offset: {map.OffsetX}, {map.OffsetY} Territory: {map.TerritoryType.Value.Name}");

        ImGui.TextUnformatted($"Map Position: {new Vector2(PlayerEx.MapFlag.XFloat, PlayerEx.MapFlag.YFloat)}");

        var pos = Coords.MapMarkerToWorld(PlayerEx.MapFlag);
        ImGui.TextUnformatted($"World Position: {pos}");

        var territory = PlayerEx.MapFlag.TerritoryId;
        var closest = Coords.FindClosestAetheryte(territory, pos);
        var aetherytes = FindRows<Aetheryte>(x => x.Territory.RowId == territory).OrderBy(a => (pos - Coords.AetherytePosition(a)).LengthSquared());

        foreach (var aetheryte in aetherytes)
        {
            ImGui.TextUnformatted($"[{aetheryte.RowId}]");
            ImGui.Indent();
            ImGui.TextUnformatted($"PlaceName: {aetheryte.PlaceName.Value.Name}");
            ImGui.TextUnformatted($"AethernetName: {aetheryte.AethernetName.Value.Name}");
            ImGui.TextUnformatted($"Position: {Coords.AetherytePosition(aetheryte)}");
            ImGui.TextUnformatted($"Dist: {(pos - Coords.AetherytePosition(aetheryte)).LengthSquared()}");
            ImGui.Unindent();
        }
    }
}

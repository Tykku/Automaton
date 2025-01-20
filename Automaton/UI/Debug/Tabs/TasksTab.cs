using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;

namespace Automaton.UI.Debug.Tabs;
internal unsafe class TasksTab : DebugTab
{
    public override void Draw()
    {
        using (ImRaii.Disabled(!Service.Automation.Running))
            if (ImGui.Button("Stop current task"))
                Service.Automation.Stop();
        ImGuiX.TaskState();

        if (AgentMap.Instance()->IsFlagMarkerSet != 0)
        {
            var closest = Coords.FindClosestAetheryte(PlayerEx.MapFlag.TerritoryId, PlayerEx.MapFlag.ToVector3());
            ImGui.TextUnformatted($"{closest}");
            ImGui.TextUnformatted($"{Coords.FindPrimaryAetheryte(closest)}");
        }
    }
}

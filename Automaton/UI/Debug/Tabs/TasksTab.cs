using Automaton.Tasks;
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
            ImGui.TextUnformatted($"{Coords.FindPrimaryAetheryte(closest ?? 0)}");
        }

        if (ImGui.Button($"dwd"))
        {
            Service.Automation.Start(new FateGrind(C.Tweaks.DateWithDestiny));
        }

        //ImGui.TextUnformatted($"Fate Count: {fg.AvailableFates.Count()}");
        //foreach (var fate in fg.AvailableFates)
        //{
        //    ImGui.TextUnformatted($"{fate.Stringify()}");
        //}
    }
}

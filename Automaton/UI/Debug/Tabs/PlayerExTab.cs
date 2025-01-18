using ImGuiNET;

namespace Automaton.UI.Debug.Tabs;
internal unsafe class PlayerExTab : DebugTab
{
    public override void Draw()
    {
        var pi = typeof(PlayerEx).GetProperties();
        foreach (var p in pi)
        {
            try
            {
                ImGui.TextColored(new Vector4(0.2f, 0.6f, 0.4f, 1), $"{p.Name}: ");
                ImGui.SameLine();
                ImGui.TextDisabled($"{p.GetValue(typeof(PlayerEx))}");
            }
            catch (Exception e)
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"[ERROR] {e.Message}");
            }
        }
    }
}

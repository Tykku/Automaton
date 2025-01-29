using Automaton.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ImGuiNET;

namespace Automaton.Features;

public class ARTurnInConfiguration
{
    [IntConfig(DefaultValue = 50, Min = 0, Max = 140)]
    public int InventoryFreeSlotThreshold = 50;

    public List<ulong> ExcludedCharacters = [];
}

[Tweak, Requirement(NavmeshIPC.Name, NavmeshIPC.Repo), Requirement(AutoRetainerIPC.Name, AutoRetainerIPC.Repo), Requirement(DeliverooIPC.Name, DeliverooIPC.Repo), Requirement(LifestreamIPC.Name, LifestreamIPC.Repo)]
internal class ARTurnIn : Tweak<ARTurnInConfiguration>
{
    public override string Name => "AutoRetainer x Deliveroo";
    public override string Description => "On CharacterPostProcess, automatically go to your grand company and turn in your gear when inventory is below a certain threshold.";

    public override void Enable()
    {
        AutoRetainer.OnCharacterPostprocessStep += CheckCharacter;
        AutoRetainer.OnCharacterReadyToPostProcess += TurnIn;
    }

    public override void Disable()
    {
        AutoRetainer.OnCharacterPostprocessStep -= CheckCharacter;
        AutoRetainer.OnCharacterReadyToPostProcess -= TurnIn;
    }

    public override void DrawConfig()
    {
        base.DrawConfig();

        if (!Config.ExcludedCharacters.Contains(Svc.ClientState.LocalContentId))
        {
            if (ImGui.Button("Exclude Current Character"))
                Config.ExcludedCharacters.Add(Svc.ClientState.LocalContentId);
        }
        else
        {
            if (ImGui.Button("Remove Character Exclusion"))
                Config.ExcludedCharacters.Remove(Svc.ClientState.LocalContentId);
        }

        ImGuiX.DrawSection("Debug");

        ImGuiX.TaskState();
        if (ImGuiComponents.IconButton(Service.Automation.CurrentTask == null ? FontAwesomeIcon.Play : FontAwesomeIcon.Stop))
        {
            if (Service.Automation.CurrentTask == null)
                Service.Automation.Start(new AutoDeliveroo(), () => { AutoRetainer.FinishCharacterPostProcess(); P.UsingARPostProcess = false; });
            else
            {
                Service.Automation.Stop();
                AutoRetainer.FinishCharacterPostProcess();
                P.UsingARPostProcess = false;
            }
        }
    }

    private void CheckCharacter()
    {
        if (Config.ExcludedCharacters.Any(x => x == Svc.ClientState.LocalContentId))
            Log("Skipping post process turn in for character: character excluded.");
        else
        {
            if (!P.UsingARPostProcess && Service.AutoRetainerIPC.GetInventoryFreeSlotCount() <= Config.InventoryFreeSlotThreshold)
            {
                P.UsingARPostProcess = true;
                AutoRetainer.RequestCharacterPostprocess();
            }
            else
                Log("Skipping post process turn in for character: inventory above threshold.");
        }
    }

    private void TurnIn() => Service.Automation.Start(new AutoDeliveroo(), () => { AutoRetainer.FinishCharacterPostProcess(); P.UsingARPostProcess = false; });
}

using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.Sheets;
using System.Threading.Tasks;

namespace Automaton.Tasks;
public sealed class KillFlag : CommonTasks
{
    protected override async Task Execute()
    {
        Status = $"Teleporting to {GetRow<TerritoryType>(PlayerEx.MapFlag.TerritoryId)!.Value.PlaceName.Value.Name}";
        await TeleportTo(PlayerEx.MapFlag.TerritoryId, PlayerEx.MapFlag.ToVector3());
        Status = "Mounting";
        await Mount();
        Status = "Moving To";
        await MoveTo(PlayerEx.MapFlag, 5, true, true);
        await Kill();
    }

    private async Task Kill()
    {
        if (Svc.Objects.FirstOrDefault(o => o is IBattleNpc mob && mob.IsHunt(), null) is { } target)
        {
            Svc.Targets.Target = target;
            Service.BossMod.SetActive("VBM Default");
            await TargetDead(target);
            Service.BossMod.ClearActive();
        }
    }

    private async Task TargetDead(DGameObject target)
    {
        while (target != null && !target.IsDead)
            await NextFrame(30);
    }
}

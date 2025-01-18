using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation;
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
        Chat.Instance.SendMessage("/vnav flyflag");
        //await FlyFlag();
        //await Kill();
    }

    private async Task FlyFlag()
    {
        Chat.Instance.SendMessage("/vnav flyflag");
        await WaitUntil(() => P.Navmesh.IsRunning(), "Starting");
        await WaitUntil(() => !P.Navmesh.IsRunning(), "Stopping");
    }

    private async Task Kill()
    {
        var target = Svc.Objects.FirstOrDefault(o => o is IBattleNpc mob && mob.IsHunt(), null);
        if (target == null) return;
    }
}

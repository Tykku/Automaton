using ECommons.Automation;
using System.Threading.Tasks;

namespace Automaton.Tasks;
public sealed class KillFlag : CommonTasks
{
    protected override async Task Execute()
    {
        Status = "Teleporting";
        await TeleportTo(PlayerEx.MapFlag.TerritoryId, PlayerEx.MapFlag.ToVector3());
        Status = "Mounting";
        await Mount();
        Status = "Moving To";
        Chat.Instance.SendMessage("/vnav flyflag");
        //await FlyFlag();
    }

    private async Task FlyFlag()
    {
        Chat.Instance.SendMessage("/vnav flyflag");
        await WaitUntil(() => P.Navmesh.IsRunning(), "Starting");
        await WaitUntil(() => !P.Navmesh.IsRunning(), "Stopping");
    }
}

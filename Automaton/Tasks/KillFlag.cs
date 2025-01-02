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
        await MoveTo(PlayerEx.MapFlag.ToVector3(), 5, true);
    }
}

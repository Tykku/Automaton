using Dalamud.Game.Inventory;
using System.Threading.Tasks;

namespace Automaton.Tasks;
public sealed class RetrieveAllMateria(GameInventoryItem item) : CommonTasks
{
    protected override async Task Execute()
    {
        Status = $"Retrieving Materia";
        var materias = item.Materia.ToArray().Where(x => x != 0);
        foreach (var materia in materias)
        {
            Service.Memory.MaterializeAction(item, MaterializeEventId.Retrieve);
            await WaitUntilThenFalse(() => Svc.Condition[ConditionFlag.Occupied39], "RetrievingMateria");
        }
    }
}

using Dalamud.Game.Inventory;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
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
            Retrieve();
            await WaitUntilThenFalse(() => Svc.Condition[ConditionFlag.Occupied39], "RetrivingMateria");
        }
    }

    private unsafe void Retrieve()
    {
        try
        {
            var _item = (InventoryItem*)item.Address;
            P.Memory.RetrieveMateria?.Invoke(EventFramework.Instance(), 0x390001, _item->Container, _item->Slot, 0);
        }
        catch (Exception e) { e.Log(); }
    }
}

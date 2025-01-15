using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Threading.Tasks;
using Dalamud.Game.Inventory;

namespace Automaton.Tasks;
//public sealed class LoopMelding(GameInventoryItem item) : CommonTasks
//{
//    private static readonly uint GettingTooAttachedVII = 1905;
//    protected override async Task Execute()
//    {
//        Status = $"Getting Achievement Progress";
//        var (current, max) = await GetAchievementProgress(GettingTooAttachedVII, $"GetProgress{nameof(GettingTooAttachedVII)}");
//        while (current < max)
//        {
//            Status = $"Melding [{current}/{max}]";
//            Meld();
//            await WaitUntilThenFalse(() => Svc.Condition[ConditionFlag.Occupied39], "Melding");

//            Status = $"Retrieving [{current}/{max}]";
//            Retrieve(MaterializeEventId.Retrieve);
//            await WaitUntilThenFalse(() => Svc.Condition[ConditionFlag.Occupied39], "Retrieving");
//            current++;
//        }
//    }

//    private unsafe void Retrieve(MaterializeEventId eventId)
//    {
//        try
//        {
//            var _item = (InventoryItem*)item.Address;
//            P.Memory.RetrieveMateria?.Invoke(EventFramework.Instance(), (int)eventId, _item->Container, _item->Slot, 0);
//        }
//        catch (Exception e) { e.Log(); }
//    }
//}

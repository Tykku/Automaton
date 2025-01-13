using Automaton.Features;
using Dalamud.Game.ClientState.Fates;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System.Threading.Tasks;
using FateState = Dalamud.Game.ClientState.Fates.FateState;

namespace Automaton.Tasks;
public sealed class FateGrind(DateWithDestiny tweak) : CommonTasks
{
    // TODO:
    // auto detect yokai event, set yokai mode accordingly
    private static Vector3 TargetPos;
    private ushort nextFateId;
    private byte fateMaxLevel;
    private unsafe bool InFate => FateManager.Instance()->CurrentFate != null;

    private ushort FateID
    {
        get; set
        {
            if (field != value)
                SyncFate(value);
            field = value;
        }
    }

    protected override async Task Execute()
    {
        while (true)
        {
            Status = "Swapping Zones";
            if (Svc.Fates.Count == 0)
            {
                if (tweak.Config.SwapZones)
                    await SwapZones();
                else
                    return;
            }

            if (InFate)
            {
                unsafe
                {
                    fateMaxLevel = FateManager.Instance()->CurrentFate->MaxLevel;
                    FateID = FateManager.Instance()->CurrentFate->FateId;
                }
            }
            else
                FateID = 0;

            if (!InFate)
            {
                var nextFate = GetFates().FirstOrDefault();
                if (nextFate is not null && Svc.Condition[ConditionFlag.InFlight] && !P.Navmesh.PathfindInProgress())
                {
                    nextFateId = nextFate.FateId;
                    TargetPos = GetRandomPointInFate(nextFateId);
                    Status = $"Moving to [{nextFateId}] @ {TargetPos}";
                    await MoveTo(TargetPos, 5, true, true);
                }
            }
            await NextFrame();
        }
    }

    private async Task SwapZones()
    {
        // if we're achievement farming, find the next zone where the achievement isn't completed, otherwise, pick a random zone within the same expac
        // if we're yokai farming, find the next zone where the yokai isn't completed
        uint zone = 0;
        await TeleportTo(zone, default);
    }
    private bool FateConditions(IFate f) => f.GameData.Value.Rule == 1 && f.State != FateState.Preparation && f.Duration <= tweak.Config.MaxDuration && f.Progress <= tweak.Config.MaxProgress && f.TimeRemaining > tweak.Config.MinTimeRemaining && !tweak.Config.blacklist.Contains(f.FateId);

    private IOrderedEnumerable<IFate> GetFates() => Svc.Fates.Where(FateConditions)
        .OrderByDescending(x => tweak.Config.PrioritizeBonusFates && x.HasBonus && (!tweak.Config.BonusWhenTwist || Player.Status.FirstOrDefault(x => DateWithDestiny.TwistOfFateStatusIDs.Contains(x.StatusId)) != null))
        .ThenByDescending(x => tweak.Config.PrioritizeStartedFates && x.Progress > 0)
        .ThenBy(f => Vector3.Distance(PlayerEx.Position, f.Position));

    private unsafe Vector3 GetRandomPointInFate(ushort fateID)
    {
        var fate = FateManager.Instance()->GetFateById(fateID);
        var angle = new Random().NextDouble() * 2 * Math.PI;
        // Get a random point in a circle within half its radius
        var randomPoint = new Vector3((float)(fate->Location.X + fate->Radius / 2 * Math.Cos(angle)), fate->Location.Y, (float)(fate->Location.Z + fate->Radius / 2 * Math.Sin(angle)));
        var point = P.Navmesh.NearestPoint(randomPoint, 5, 5);
        return (Vector3)(point == null ? fate->Location : point);
    }

    private unsafe void SyncFate(ushort value)
    {
        if (value != 0 && PlayerState.Instance()->IsLevelSynced == 0)
        {
            if (Player.Level > fateMaxLevel)
                ECommons.Automation.Chat.Instance.SendMessage("/lsync");
        }
    }
}

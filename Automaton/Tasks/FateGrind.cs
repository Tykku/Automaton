using Automaton.Features;
using Dalamud.Game.ClientState.Fates;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System.Threading.Tasks;
using FateState = Dalamud.Game.ClientState.Fates.FateState;

namespace Automaton.Tasks;
public sealed class FateGrind(DateWithDestinyConfiguration config) : CommonTasks
{
    // TODO:
    // auto detect yokai event, set yokai mode accordingly
    private static Vector3 TargetPos;
    private ushort nextFateId;
    private byte fateMaxLevel;
    private unsafe bool InFate => FateManager.Instance()->CurrentFate != null;
    public unsafe IOrderedEnumerable<IFate> AvailableFates => Svc.Fates.Where(FateConditions).OrderByDescending(f => f.Progress).ThenByDescending(f => f.HasBonus).ThenBy(f => Player.DistanceTo(f.Position));

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
            if (Svc.Condition[ConditionFlag.InCombat])
            {
                Status = "Waiting for combat to end";
                await NextFrame(30);
            }

            if (InFate)
            {
                unsafe
                {
                    Status = "Syncing to Fate";
                    fateMaxLevel = FateManager.Instance()->CurrentFate->MaxLevel;
                    FateID = FateManager.Instance()->CurrentFate->FateId;
                    Service.BossMod.SetActive("AI");
                }
            }
            else
            {
                FateID = 0;
                Service.BossMod.ClearActive();
            }

            if (!InFate)
            {
                var nextFate = AvailableFates.FirstOrDefault();
                if (nextFate is not null)
                {
                    nextFateId = nextFate.FateId;
                    TargetPos = GetRandomPointInFate(nextFateId);
                    await MoveTo(TargetPos, 5, true, true);
                }
            }

            if (!AvailableFates.Any())
            {
                if (config.SwapZones)
                    await SwapZones();
                else
                    await NextFrame();
            }

            await NextFrame();
        }
    }

    private async Task SwapZones()
    {
        // if we're achievement farming, find the next zone where the achievement isn't completed, otherwise, pick a random zone within the same expac
        // if we're yokai farming, find the next zone where the yokai isn't completed
        var zoneId = GetNextAchievementZone() is { } zone ? zone : GetRandomSameExpacZone();
        await TeleportTo(zoneId, default);
    }

    private unsafe uint? GetNextAchievementZone()
    {
        var agent = AgentFateProgress.Instance();
        if (agent == null) return null;
        // prioritise zones in the same expac as current area
        var currentTabIndex = Array.FindIndex(agent->Tabs.ToArray(), tab => tab.Zones.ToArray().Any(zone => Player.Territory == zone.TerritoryTypeId));

        if (currentTabIndex != -1 && currentTabIndex < agent->Tabs.Length - 1)
        {
            // get zone in expac that needs fates
            var nullableZone = agent->Tabs[currentTabIndex].Zones.ToArray().FirstOrNull(zone => zone.NeededFates - zone.FateProgress > 0);
            return nullableZone is { } zone ? zone.TerritoryTypeId : null;
        }
        else
        {
            // get zone from any shared fate expac that needs fates
            var nullableZone = agent->Tabs.ToArray().SelectMany(tab => tab.Zones.ToArray()).FirstOrNull(zone => zone.NeededFates - zone.FateProgress > 0);
            return nullableZone is { } zone ? zone.TerritoryTypeId : null;
        }
    }

    private uint GetRandomSameExpacZone()
    {
        var rows = FindRows<TerritoryType>(x => x.ExVersion.RowId == GetRow<TerritoryType>(Player.Territory)!.Value.ExVersion.RowId);
        return rows[new Random().Next(rows.Length)].RowId;
    }

    private bool FateConditions(IFate f) => f.GameData.Value.Rule == 1 && f.State != FateState.Preparation && f.Duration <= config.MaxDuration && f.Progress <= config.MaxProgress && f.TimeRemaining > config.MinTimeRemaining && !config.blacklist.Contains(f.FateId);

    public IOrderedEnumerable<IFate> GetFates() => Svc.Fates.Where(FateConditions)
        .OrderByDescending(x => config.PrioritizeBonusFates && x.HasBonus && (!config.BonusWhenTwist || Player.Status.FirstOrDefault(x => DateWithDestiny.TwistOfFateStatusIDs.Contains(x.StatusId)) != null))
        .ThenByDescending(x => config.PrioritizeStartedFates && x.Progress > 0)
        .ThenBy(f => Vector3.Distance(PlayerEx.Position, f.Position));

    private unsafe Vector3 GetRandomPointInFate(ushort fateID)
    {
        var fate = FateManager.Instance()->GetFateById(fateID);
        var angle = new Random().NextDouble() * 2 * Math.PI;
        // Get a random point in a circle within half its radius
        var randomPoint = new Vector3((float)(fate->Location.X + fate->Radius / 2 * Math.Cos(angle)), fate->Location.Y, (float)(fate->Location.Z + fate->Radius / 2 * Math.Sin(angle)));
        var point = Service.Navmesh.NearestPoint(randomPoint, 5, 5);
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

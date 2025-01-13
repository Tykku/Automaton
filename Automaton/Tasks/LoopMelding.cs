using System.Threading.Tasks;

namespace Automaton.Tasks;
public sealed class LoopMelding : CommonTasks
{
    private static readonly uint GettingTooAttachedVII = 1905;
    protected override async Task Execute()
    {
        var (current, max) = await GetAchievementProgress(GettingTooAttachedVII, $"GetProgress{nameof(GettingTooAttachedVII)}");
        while (current < max)
        {
            current++;
        }
    }
}

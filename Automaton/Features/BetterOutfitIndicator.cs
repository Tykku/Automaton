using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Automaton.Features;

[Tweak(disabled: true)]
internal class BetterOutfitIndicator : Tweak
{
    public override string Name => "Better Outfit Indicator";
    public override string Description => "Indicates on the tooltip whether or not the item is in an outfit you've completed.";
    private DalamudLinkPayload payload;

    public override void Enable() => payload = Svc.PluginInterface.AddChatLinkHandler((uint)LinkHandlerId.BetterOutfitIndicator, (_, _) => { });
    public override void Disable() => Svc.PluginInterface.RemoveChatLinkHandler((uint)LinkHandlerId.BetterOutfitIndicator);
}

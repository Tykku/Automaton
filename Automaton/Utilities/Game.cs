using FFXIVClientStructs.FFXIV.Client.Network;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace Automaton.Utilities;
public unsafe class Game
{
    public static AtkUnitBase* GetAddonByName(string name) => RaptureAtkUnitManager.Instance()->GetAddonByName(name);
    public static bool AddonActive(string name) => AddonActive(GetAddonByName(name));
    public static bool AddonActive(AtkUnitBase* addon) => addon != null && addon->IsVisible && addon->IsReady;

    public static void ProgressTalk()
    {
        var addon = GetAddonByName("Talk");
        if (addon != null && addon->IsReady)
        {
            var evt = new AtkEvent() { Listener = &addon->AtkEventListener, Target = &AtkStage.Instance()->AtkEventTarget };
            var data = new AtkEventData();
            addon->ReceiveEvent(AtkEventType.MouseClick, 0, &evt, &data);
        }
    }

    public static void TeleportToAethernet(uint currentAetheryte, uint destinationAetheryte)
    {
        Span<uint> payload = [4, destinationAetheryte];
        PacketDispatcher.SendEventCompletePacket(0x50000 | currentAetheryte, 0, 0, payload.GetPointer(0), (byte)payload.Length, null);
    }

    public static void TeleportToFirmament(uint currentAetheryte)
    {
        Span<uint> payload = [9];
        PacketDispatcher.SendEventCompletePacket(0x50000 | currentAetheryte, 0, 0, payload.GetPointer(0), (byte)payload.Length, null);
    }
}

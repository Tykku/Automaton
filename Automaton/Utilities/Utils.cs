using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Textures.TextureWraps;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using ImGuiNET;

namespace Automaton.Utilities;
public static class Utils
{
    public static IDalamudTextureWrap? GetIcon(uint iconId) => iconId != 0 ? Svc.Texture?.GetFromGameIcon(iconId).GetWrapOrEmpty() : null;

    public static bool HasPlugin(string name) => DalamudReflector.TryGetDalamudPlugin(name, out _, false, true);

    public static unsafe bool IsClickingInGameWorld()
        => !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow)
        && !ImGui.GetIO().WantCaptureMouse
        && AtkStage.Instance()->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList.Count == 0
        && Framework.Instance()->Cursor->ActiveCursorType == 0;

    public static Vector3 RotatePoint(float cx, float cy, float angle, Vector3 p)
    {
        if (angle == 0f) return p;
        var s = (float)Math.Sin(angle);
        var c = (float)Math.Cos(angle);

        // translate point back to origin:
        p.X -= cx;
        p.Z -= cy;

        // rotate point
        var xnew = p.X * c - p.Z * s;
        var ynew = p.X * s + p.Z * c;

        // translate point back:
        p.X = xnew + cx;
        p.Z = ynew + cy;
        return p;
    }

    public static unsafe Structs.AgentMJICraftSchedule* Agent = (Structs.AgentMJICraftSchedule*)AgentModule.Instance()->GetAgentByInternalId(AgentId.MJICraftSchedule);
    public static unsafe Structs.AgentMJICraftSchedule.AgentData* AgentData => Agent != null ? Agent->Data : null;

    public static unsafe void SetRestCycles(uint mask)
    {
        Svc.Log.Debug($"Setting rest: {mask:X}");
        AgentData->NewRestCycles = mask;
        SynthesizeEvent(5, [new() { Type = AtkValueType.Int, Int = 0 }]);
    }

    private static unsafe void SynthesizeEvent(ulong eventKind, Span<AtkValue> args)
    {
        var eventData = stackalloc int[] { 0, 0, 0 };
        Agent->AgentInterface.ReceiveEvent((AtkValue*)eventData, args.GetPointer(0), (uint)args.Length, eventKind);
    }

    public static bool KeybindIsPressed(string name)
    {
        var key = KeybindToKey(name);
        if (!key.HasValue || !Svc.KeyState.IsVirtualKeyValid((int)key)) return false;
        return Svc.KeyState.GetRawValue((int)key) != 0 || IsKeyPressed((int)key);
    }

    public static void ResetKeybind(string name)
    {
        var key = KeybindToKey(name);
        if (!key.HasValue || !Svc.KeyState.IsVirtualKeyValid((int)key)) return;
        Svc.KeyState.SetRawValue((int)key, 0);
    }

    public static unsafe VirtualKey? KeybindToKey(string name)
    {
        VirtualKey? key = null;
        var keybind = new UIInputData.Keybind();
        var keyName = Utf8String.FromString(name);
        var inputData = UIInputData.Instance();
        inputData->GetKeybind(keyName, &keybind);
        List<List<nint>?> availableKeys = [GetKeysToPress(keybind.Key, keybind.Modifier), GetKeysToPress(keybind.AltKey, keybind.AltModifier)];
        var realKeys = availableKeys.Where(x => x != null).Select(x => x!).MinBy(x => x.Count);
        key = (VirtualKey?)realKeys?.FirstOrDefault();
        return key == null ? null : key;
    }

    public static List<nint>? GetKeysToPress(SeVirtualKey key, ModifierFlag modifier)
    {
        List<nint> keys = [];
        if (modifier.HasFlag(ModifierFlag.Ctrl))
            keys.Add(0x11); // VK_CONTROL
        if (modifier.HasFlag(ModifierFlag.Shift))
            keys.Add(0x10); // VK_SHIFT
        if (modifier.HasFlag(ModifierFlag.Alt))
            keys.Add(0x12); // VK_MENU

        nint mappedKey = (nint)key;
        if (mappedKey == 0)
            return null;

        keys.Add(mappedKey);
        return keys;
    }
}

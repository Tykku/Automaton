using ECommons.EzIpcManager;

namespace Automaton.IPC;

#nullable disable
#pragma warning disable CS8632
public class BossModIPC
{
    public const string Name = "BossMod";
    public const string Repo = "https://puni.sh/api/repository/veyn";
    public BossModIPC() => EzIPC.Init(this, Name);
    public static bool Installed => Utils.HasPlugin(Name);

    [EzIPC("Presets.%m", true)] public readonly Func<string, string?> Get;
    [EzIPC("Presets.%m", true)] public readonly Func<string, bool, bool> Create;
    [EzIPC("Presets.%m", true)] public readonly Func<string, bool> Delete;
    [EzIPC("Presets.%m", true)] public readonly Func<string> GetActive;
    [EzIPC("Presets.%m", true)] public readonly Func<string, bool> SetActive;
    [EzIPC("Presets.%m", true)] public readonly Func<bool> ClearActive;
    [EzIPC("Presets.%m", true)] public readonly Func<bool> GetForceDisabled;
    [EzIPC("Presets.%m", true)] public readonly Func<bool> SetForceDisabled;

    [EzIPC("AI.%m", true)] public readonly Action<string> SetPreset;
    [EzIPC("AI.%m", true)] public readonly Func<string> GetPreset;
}

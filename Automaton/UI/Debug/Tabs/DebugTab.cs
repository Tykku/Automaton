using System.Text.RegularExpressions;

namespace Automaton.UI.Debug.Tabs;
public interface IDebugTab : IDrawableTab;

public interface IDrawableTab
{
    string Title { get; }
    string InternalName { get; }
    bool DrawInChild { get; }
    bool IsEnabled { get; }
    bool IsPinnable { get; }
    bool CanPopOut { get; }
    void Draw();
}

public abstract partial class DebugTab : IDebugTab
{
    private string? _title = null;
    public virtual string Title => _title ??= NameRegex().Replace(TabRegex().Replace(GetType().Name, ""), "$1 $2");
    public virtual bool IsEnabled => true;
    public virtual bool IsPinnable => true;
    public virtual bool CanPopOut => true;
    public virtual bool DrawInChild => true;
    public virtual string InternalName => GetType().Name;

    [GeneratedRegex("Tab$")]
    private static partial Regex TabRegex();

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex NameRegex();

    public virtual void Draw() { }

    public bool Equals(IDebugTab? other)
    {
        return other?.Title == _title;
    }
}

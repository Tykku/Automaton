namespace Automaton.UI.Debug.Tabs;
internal class LogTab : DebugTab
{
    public override void Draw()
    {
        InternalLog.PrintImgui();
    }
}

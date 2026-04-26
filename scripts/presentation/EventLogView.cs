using AgeOfHorizons.Core;
using Godot;
using System.Linq;

public partial class EventLogView : RichTextLabel
{
    public void Render(GameState state)
    {
        Text = "[b]Event Log[/b]\n" + string.Join("\n", state.EventLog.Entries.TakeLast(8));
    }
}

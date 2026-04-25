using AgeOfHorizons.Core;
using Godot;
using System.Linq;

public partial class EventLogView : Label
{
    public void Render(GameState state)
    {
        Text = string.Join("\n", state.EventLog.Entries.TakeLast(6));
    }
}

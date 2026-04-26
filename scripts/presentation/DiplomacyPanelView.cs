using AgeOfHorizons.Core;
using Godot;
using System.Linq;

public partial class DiplomacyPanelView : RichTextLabel
{
    public void Render(GameState state)
    {
        var p = state.ActivePlayer;
        var lines = state.Players.Select(other => $"{other.Name}: {DiplomacySystem.GetRelation(p, other)}");
        Text = "[b]Diplomacy[/b]\n" + string.Join("\n", lines);
    }
}
using Godot;
public partial class DiplomacyPanelView : Control { }

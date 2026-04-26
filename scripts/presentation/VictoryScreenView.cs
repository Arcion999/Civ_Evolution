using AgeOfHorizons.Core;
using Godot;

public partial class VictoryScreenView : Label
{
    public void Render(GameState state)
    {
        Visible = !string.IsNullOrWhiteSpace(state.VictoryWinner);
        Text = state.VictoryWinner ?? string.Empty;
    }
}

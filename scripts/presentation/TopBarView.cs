using AgeOfHorizons.Core;
using Godot;

public partial class TopBarView : Label
{
    public void Render(GameState state)
    {
        Text = $"Turn {state.Turn} | Active: {state.ActivePlayer.Name} | Research: {state.ActivePlayer.CurrentResearchTechId}";
    }
}

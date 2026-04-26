using AgeOfHorizons.Core;
using Godot;

public partial class TopBarView : RichTextLabel
{
    public void Render(GameState state)
    {
        var p = state.ActivePlayer;
        Text = $"[b]Turn {state.Turn}[/b]  |  [b]{p.Name}[/b]  |  Era: {p.Era}  |  Gold: {p.Gold}  |  Science: {p.Science}  |  Culture: {p.Culture}  |  Influence: {p.Influence}  |  Research: {p.CurrentResearchTechId}";
public partial class TopBarView : Label
{
    public void Render(GameState state)
    {
        Text = $"Turn {state.Turn} | Active: {state.ActivePlayer.Name} | Research: {state.ActivePlayer.CurrentResearchTechId}";
    }
}

using AgeOfHorizons.Core;
using Godot;

public partial class TechPanelView : RichTextLabel
{
    public void Render(GameState state)
    {
        var p = state.ActivePlayer;
        Text = $"[b]Research[/b]\nCurrent: {p.CurrentResearchTechId}\nStored Science: {p.Science}\nKnown Techs: {p.ResearchedTechs.Count}";
    }
}

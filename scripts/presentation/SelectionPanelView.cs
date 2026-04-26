using Godot;

public partial class SelectionPanelView : Label
{
    public void Render(GameSceneController controller)
    {
        if (controller.SelectedUnit != null)
            Text = $"Selected Unit: {controller.SelectedUnit.UnitDefId} @ {controller.SelectedUnit.Coord}\nShift+Click: Move";
        else if (controller.SelectedTile != null)
            Text = $"Tile: {controller.SelectedTile}";
        else
            Text = "Nothing selected";
    }
}

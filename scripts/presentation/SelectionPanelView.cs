using Godot;

public partial class SelectionPanelView : RichTextLabel
public partial class SelectionPanelView : Label
{
    public void Render(GameSceneController controller)
    {
        if (controller.SelectedUnit != null)
        {
            var u = controller.SelectedUnit;
            Text = $"[b]Unit[/b]: {u.UnitDefId}\nMoves: {u.MovesRemaining} | Actions: {u.ActionPointsRemaining} | HP: {u.Health}\nActions: Shift+Click Move, [Found City] if Settler";
            return;
        }

        if (controller.SelectedTile != null)
        {
            Text = $"[b]Tile[/b]: {controller.SelectedTile}";
            return;
        }

        Text = "[b]Selection[/b]: nothing selected";
            Text = $"Selected Unit: {controller.SelectedUnit.UnitDefId} @ {controller.SelectedUnit.Coord}\nShift+Click: Move";
        else if (controller.SelectedTile != null)
            Text = $"Tile: {controller.SelectedTile}";
        else
            Text = "Nothing selected";
    }
}

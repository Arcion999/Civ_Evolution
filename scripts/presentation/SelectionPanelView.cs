using Godot;

public partial class SelectionPanelView : RichTextLabel
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
    }
}

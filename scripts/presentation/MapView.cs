using AgeOfHorizons.Core;
using Godot;
using System.Linq;

public partial class MapView : Node2D
{
    private const float HexSize = 24f;
    private GameSceneController _controller = null!;

    public void Render(GameState state, GameSceneController controller)
    {
        _controller = controller;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Main.State == null) return;
        var player = Main.State.ActivePlayer;
        foreach (var tile in Main.State.Grid.Tiles.Values)
        {
            var p = AxialToPixel(tile.Coord);
            var visible = player.VisibleTiles.Contains(tile.Coord.ToString());
            var color = tile.TerrainId switch
            {
                "water" => new Color("2d5f94"),
                "forest" => new Color("3e7f4f"),
                "ridge" => new Color("7a6f66"),
                _ => new Color("8fb86a")
            };
            if (!visible) color = color.Darkened(0.7f);
            DrawCircle(p, HexSize * 0.8f, color);

            if (Main.State.Cities.Any(c => c.Coord.Equals(tile.Coord)))
                DrawCircle(p, 6, new Color("ffd166"));
            if (Main.State.Units.Any(u => !u.Consumed && u.Coord.Equals(tile.Coord)))
                DrawCircle(p + new Vector2(8, -8), 5, new Color("ef476f"));
            if (_controller.SelectedTile.HasValue && _controller.SelectedTile.Value.Equals(tile.Coord))
                DrawArc(p, HexSize, 0, Mathf.Tau, 24, Colors.White, 2);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            var world = GetGlobalMousePosition();
            var coord = PixelToAxial(world);
            if (Main.State.Grid.Contains(coord))
            {
                if (_controller.SelectedUnit != null && Input.IsKeyPressed(Key.Shift))
                    _controller.MoveSelectedUnit(coord);
                else
                    _controller.SelectTile(coord);
            }
        }
    }

    private static Vector2 AxialToPixel(HexCoord c)
        => new(HexSize * (1.5f * c.Q) + 50, HexSize * (Mathf.Sqrt(3) / 2f * c.Q + Mathf.Sqrt(3) * c.R) + 50);

    private static HexCoord PixelToAxial(Vector2 p)
    {
        var q = (2f / 3f * (p.X - 50)) / HexSize;
        var r = ((-1f / 3f * (p.X - 50)) + (Mathf.Sqrt(3) / 3f * (p.Y - 50))) / HexSize;
        return new HexCoord(Mathf.RoundToInt(q), Mathf.RoundToInt(r));
    }
}

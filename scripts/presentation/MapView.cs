using AgeOfHorizons.Core;
using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class MapView : Node2D
{
    private const float HexSize = 36f;
    private readonly Dictionary<string, Texture2D> _terrainTextures = new();
    private readonly Dictionary<string, Texture2D> _unitTextures = new();
    private readonly Dictionary<string, Texture2D> _resourceTextures = new();
    private GameSceneController _controller = null!;

    public override void _Ready()
    {
        BuildTextureSet(_terrainTextures, 64, 64, new Dictionary<string, Color>
        {
            ["plains"] = new Color("82aa5a"), ["grassland"] = new Color("5ea052"), ["forest"] = new Color("356e3d"),
            ["hills"] = new Color("96764f"), ["mountain"] = new Color("707680"), ["desert"] = new Color("c4ae64"),
            ["tundra"] = new Color("b2c6d1"), ["water"] = new Color("3c64aa")
        });

        BuildTextureSet(_unitTextures, 32, 32, new Dictionary<string, Color>
        {
            ["settler"] = new Color("f0d778"), ["scout"] = new Color("d16f78"), ["warrior"] = new Color("9f4a4a"),
            ["worker"] = new Color("77a9d8"), ["builder"] = new Color("63b7b7")
        });

        BuildTextureSet(_resourceTextures, 20, 20, new Dictionary<string, Color>
        {
            ["stone"] = new Color("8c8c8c"), ["wood"] = new Color("7a5a3d"), ["wheat"] = new Color("f0cf57"), ["fish"] = new Color("5e96cf"),
            ["copper"] = new Color("b57542"), ["iron"] = new Color("9095a2"), ["bauxite"] = new Color("cd6c5a")
        });
    }

    private static void BuildTextureSet(Dictionary<string, Texture2D> target, int w, int h, Dictionary<string, Color> colors)
    {
        foreach (var kv in colors)
            target[kv.Key] = BuildFlatTexture(w, h, kv.Value);
    }

    private static Texture2D BuildFlatTexture(int width, int height, Color color)
    {
        var image = Image.Create(width, height, false, Image.Format.Rgba8);
        image.Fill(color);
        return ImageTexture.CreateFromImage(image);
    }

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
            var tex = _terrainTextures.GetValueOrDefault(tile.TerrainId, _terrainTextures["plains"]);
            var rect = new Rect2(p - new Vector2(HexSize / 2f, HexSize / 2f), new Vector2(HexSize, HexSize));
            DrawTextureRect(tex, rect, false, visible ? Colors.White : new Color(0.22f, 0.22f, 0.24f, 0.9f));
            DrawArc(p, HexSize * 0.54f, 0, Mathf.Tau, 20, new Color("1b1f24"), 1.5f);

            if (tile.ResourceId != null && _resourceTextures.TryGetValue(tile.ResourceId, out var resourceTex))
                DrawTexture(resourceTex, p + new Vector2(-8, 8));

            var city = Main.State.Cities.FirstOrDefault(c => c.Coord.Equals(tile.Coord));
            if (city != null)
                DrawCircle(p, 7, new Color("ffd166"));

            var unit = Main.State.Units.FirstOrDefault(u => !u.Consumed && u.Coord.Equals(tile.Coord));
            if (unit != null && _unitTextures.TryGetValue(unit.UnitDefId, out var unitTex))
                DrawTexture(unitTex, p + new Vector2(-16, -26));

            if (_controller.SelectedTile.HasValue && _controller.SelectedTile.Value.Equals(tile.Coord))
                DrawArc(p, HexSize * 0.58f, 0, Mathf.Tau, 24, new Color("ffe066"), 3);
        }

        DrawMiniMap();
    }

    private void DrawMiniMap()
    {
        var origin = new Vector2(1060, 520);
        DrawRect(new Rect2(origin - new Vector2(6, 6), new Vector2(190, 110)), new Color("20242b"));
        foreach (var tile in Main.State.Grid.Tiles.Values)
        {
            var c = tile.TerrainId == "water" ? new Color("356a9f") : new Color("72965f");
            var p = origin + new Vector2(tile.Coord.Q * 4, tile.Coord.R * 3);
            DrawRect(new Rect2(p, new Vector2(3, 2)), c);
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

    public static Vector2 AxialToPixel(HexCoord c)
        => new(HexSize * (1.5f * c.Q) + 80, HexSize * (Mathf.Sqrt(3) / 2f * c.Q + Mathf.Sqrt(3) * c.R) + 80);

    public static HexCoord PixelToAxial(Vector2 p)
    {
        var q = (2f / 3f * (p.X - 80)) / HexSize;
        var r = ((-1f / 3f * (p.X - 80)) + (Mathf.Sqrt(3) / 3f * (p.Y - 80))) / HexSize;
        return new HexCoord(Mathf.RoundToInt(q), Mathf.RoundToInt(r));
    }
}

using AgeOfHorizons.Core;
using Godot;

public partial class Main : Node
{
    public static GameConfig Config = null!;
    public static GameState State = null!;

    public override void _Ready()
    {
        Config = GameBootstrap.LoadConfig(ProjectSettings.GlobalizePath("res://"));
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }
}

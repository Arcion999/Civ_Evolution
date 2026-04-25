using AgeOfHorizons.Core;
using Godot;

public partial class MainMenuController : Control
{
    public void OnNewGamePressed()
    {
        Main.State = GameBootstrap.NewGame(Main.Config);
        GetTree().ChangeSceneToFile("res://scenes/GameScene.tscn");
    }

    public void OnQuitPressed() => GetTree().Quit();
}

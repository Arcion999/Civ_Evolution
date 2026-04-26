using AgeOfHorizons.Core;
using Godot;

public partial class MainMenuController : Control
{
    public void OnNewGamePressed()
    {
        Main.State = GameBootstrap.NewGame(Main.Config);
        GetTree().ChangeSceneToFile("res://scenes/GameScene.tscn");
    }

    public void OnLoadGamePressed()
    {
        var path = ProjectSettings.GlobalizePath("user://savegame.json");
        if (!System.IO.File.Exists(path)) return;
        Main.State = SaveLoadSystem.Load(path);
        GetTree().ChangeSceneToFile("res://scenes/GameScene.tscn");
    }

    public void OnOptionsPressed()
    {
        var popup = GetNode<AcceptDialog>("OptionsDialog");
        popup.DialogText = "Options are minimal in this prototype.\nUse WASD + wheel + middle-drag camera controls.";
        popup.PopupCentered();
    }

    public void OnQuitPressed() => GetTree().Quit();
}

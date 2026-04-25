using Godot;

public partial class CameraController : Camera2D
{
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.IsMouseButtonPressed(MouseButton.Middle))
            Position -= motion.Relative / Zoom;

        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp) Zoom *= 1.1f;
            if (mb.ButtonIndex == MouseButton.WheelDown) Zoom *= 0.9f;
        }
    }
}

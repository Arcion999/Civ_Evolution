using Godot;

public partial class CameraController : Camera2D
{
    [Export] public float PanSpeed = 850f;
    [Export] public float MinZoom = 0.45f;
    [Export] public float MaxZoom = 2.2f;

    private float _targetZoom = 1f;

    public override void _Ready()
    {
        Zoom = Vector2.One;
        _targetZoom = 1f;
    }

    public override void _Process(double delta)
    {
        var d = (float)delta;
        var move = Vector2.Zero;
        if (Input.IsKeyPressed(Key.W)) move.Y -= 1;
        if (Input.IsKeyPressed(Key.S)) move.Y += 1;
        if (Input.IsKeyPressed(Key.A)) move.X -= 1;
        if (Input.IsKeyPressed(Key.D)) move.X += 1;
        if (move != Vector2.Zero)
            Position += move.Normalized() * PanSpeed * d / Zoom.X;

        var z = Mathf.Lerp(Zoom.X, _targetZoom, 8f * d);
        Zoom = new Vector2(z, z);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.IsMouseButtonPressed(MouseButton.Middle))
            Position -= motion.Relative / Zoom;

        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp) _targetZoom = Mathf.Clamp(_targetZoom * 1.08f, MinZoom, MaxZoom);
            if (mb.ButtonIndex == MouseButton.WheelDown) _targetZoom = Mathf.Clamp(_targetZoom * 0.92f, MinZoom, MaxZoom);
        }
    }
}

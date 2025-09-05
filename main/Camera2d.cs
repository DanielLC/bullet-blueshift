using Godot;

public partial class Camera2d : Camera2D
{
    Vector2 oldSize = new Vector2(-1,-1);
    public override void _Process(double delta)
    {
        Vector2 size = GetViewport().GetVisibleRect().Size;
        if (size == oldSize)
            return;
        oldSize = size;
        Zoom = new Vector2(size.X, size.Y);
    }
}

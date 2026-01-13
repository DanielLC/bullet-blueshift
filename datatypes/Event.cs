using Godot;
using System;

public class Event
{
    public float x;
    public float y;
    public float t;

    public Event(float x, float y, float t) {
        this.x = x;
        this.y = y;
        this.t = t;
    }

    public Event(Vector4 v) {
        x = v.X;
        y = v.Y;
        t = v.Z;
    }

    public Vector4 ToVector() {
        return new Vector4(x, y, t, 1);
    }

    public PointOfReference GetTranslation() {
        var matrix = new Projection(
            new Vector4(1f, 0f, 0f, 0f),
            new Vector4(0f, 1f, 0f, 0f),
            new Vector4(0f, 0f, 1f, 0f),
            new Vector4(x, y, t, 1f)
        );

        var invMatrix = new Projection(
            new Vector4(1f, 0f, 0f, 0f),
            new Vector4(0f, 1f, 0f, 0f),
            new Vector4(0f, 0f, 1f, 0f),
            new Vector4(-x, -y, -t, 1f)
        );

        return new PointOfReference(matrix, invMatrix);
    }
    
    //If the proper distance is real, they're spacelike, and this returns 0.
	//They're not actually equal, but neither is after the other.
    //Otherwise, they're timelike. Return -1 if self is earlier, or 1 if e is earlier.
    public int Compare(Event e) {
        var dx = x - e.x;
        var dy = y - e.y;
        var dt = t - e.t;
        var proper_distance_squared = dx*dx + dy*dy - dt*dt;
        if (proper_distance_squared > 0) {
            return 0;
        } else {
            return Mathf.Sign(dt);
        }
    }

    public static bool operator >(Event left, Event right)
    {
        return left.Compare(right) > 0;
    }

    public static bool operator <(Event left, Event right)
    {
        return left.Compare(right) < 0;
    }

    public static float operator *(Event e, Vector4 coevent)
    {
        return e.x * coevent.X + e.y * coevent.Y + e.t * coevent.Z + coevent.W;
    }

    public override string ToString() {
        return $"{x}, {y}, {t}";
    }
}
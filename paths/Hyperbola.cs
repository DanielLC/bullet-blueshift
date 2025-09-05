using Godot;
using System;

public partial class Hyperbola : Path
{
    private readonly float accel;

    public Hyperbola(float accel)
    {
        this.accel = accel;
    }

    private float Time(float s)
    {
        return Mathf.Sinh(accel * s) / accel;
    }

    private float Time2(float t)
    {
        return Mathf.Asinh(accel * t) / accel;
    }

    public override Event Event(float s)
    {
        return new Event(0, Mathf.Cosh(accel * s) / accel, Mathf.Sinh(accel * s) / accel);
    }

    private Event Position2(float t)
    {
        return new Event(0, Mathf.Sqrt(t * t + 1 / (accel * accel)), t);
    }

    //Velocity(s) is tanh(accel*s), if you're wondering.
    private Velocity Velocity2(float t)
    {
        return new Velocity(0, t / Mathf.Sqrt(t * t + 1 / (accel * accel)));
    }

    public override PointOfReference PointOfReferenceAtTime(float s)
    {
        var t = Time(s);
        return new PointOfReference(Position2(t), Velocity2(t));
    }

    private float FindConeIntersectionT(Event e, float sign)
    {
        if (e.t <= -e.y)
        {
            GD.Print("e.t <= -e.y");
            return float.NaN;
        }
        var u = e.y;
        var v = e.x * e.x + e.y * e.y - e.t * e.t + 1 / (accel * accel);
        var a = e.t * e.t - u * u;
        var b = v * e.t;
        var c = v * v / 4 - u * u / (accel * accel);
        var d = b * b - 4 * a * c;
        // If the hyperbola doesn't intersect with the past lightcone
        // (which is entirely possible, if the object has been accelerating away from you for all eternity),
        // there's nothing to see, so return null.
        /*GD.Print($"u = {u}");
        GD.Print($"v = {v}");
        GD.Print($"a = {a}");
        GD.Print($"b = {b}");
        GD.Print($"c = {c}");*/
        if (d < 0)
        {
            GD.Print($"d = {d} < 0");
            //return float.NaN;
            return -b / (2 * a);
        }
        var t = (-b + sign * Mathf.Sqrt(d)) / (2 * a);
        return t;
    }

    public override PointOfReference SeenFromRest(Event e)
    {
        var t = FindConeIntersectionT(e, +1);
        if (float.IsNaN(t))
            return null;
        return PointOfReferenceAtTime(Time2(t));
    }

    public override Event See(Event e)
    {
        var t = FindConeIntersectionT(e, -1);
        if (float.IsNaN(t))
            return null;
        return Position2(t);
    }

    public override Event ToLocalSpacetime(Event e) {
        if (e.y <= 0)
            return null;
        var speed = e.t / e.y;
        if (Mathf.Abs(speed) > 1)
            return null;
        var v = new Velocity(0, -speed);
        var lateral = new Event(0, -1 / accel, 0).GetTranslation() * (v.GetLorentz() * e);
        return new Event(lateral.x, lateral.y, Mathf.Atanh(speed) / accel);
    }

    public override Velocity GetVelocity(Event e) {
        if (e.y <= 0)
            return null;
        var speed = e.t / e.y;
        if (Mathf.Abs(speed) > 1)
            return null;
        return new Velocity(0, speed);
    }

    public override float GetAccel() {
        return accel;
    }

    public override string ToString() {
        return "Hyperbola";
    }
}
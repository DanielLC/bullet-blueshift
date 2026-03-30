using Godot;
using System;

public partial class Transform : Path
{
    private Path path;
    public PointOfReference pathTransform;
    public float rotation;

    //I could add versions of this where you pass in fewer things. I'll just see what I need.
    public Transform(Path path, PointOfReference pathTransform, float initialRotation)
    {
        //This should combine Transforms, but I couldn't get it to work, and I ended up modifying how I do this so I don't need it.
        if (path is Transform)
            throw new InvalidOperationException("Path cannot be a Transform.");
        this.path = path;
        this.pathTransform = pathTransform;
        this.rotation = initialRotation;
        //GD.Print($"rotationSpeed: {rotationSpeed}, initialRotation: {initialRotation}");
    }

    public Transform(Path path, PointOfReference pathTransform) : this(path, pathTransform, 0) { }

    public override PointOfReference PointOfReferenceAtTime(float s)
    {
        return pathTransform * path.PointOfReferenceAtTime(s) * PointOfReference.FromRotation(-rotation);
    }

    public override Event Event(float s)
    {
        return pathTransform * path.Event(s);
    }

    //Doesn't include rotation. That shouldn't matter, but if I ever do try to include it, this needs to be fixed.
    public override (PointOfReference, float) SeenFromRest(Event e)
    {
        (var seen0, float t) = path.SeenFromRest(pathTransform.Inverse() * e);
        if (seen0 == null)
            return (null, t);
        else
            return (pathTransform * seen0, t);
        //return pathTransform * seen0 * PointOfReference.FromRotation(-(initialRotation + time * rotationSpeed));
    }

    public override Event See(Event e)
    {
        var see0 = path.See(pathTransform.Inverse() * e);
        if (see0 == null)
            return null;
        else
            return pathTransform * see0;
    }

    public override Event ToLocalSpacetime(Event e)
    {
        return PointOfReference.FromRotation(rotation) * path.ToLocalSpacetime(pathTransform.Inverse() * e);
    }

    public override Velocity GetVelocity(Event e)
    {
        var v = path.GetVelocity(pathTransform.Inverse() * e);
        if (v == null)
            return null;
        return pathTransform * v;
    }

    public override float GetAccel()
    {
        return path.GetAccel();
    }

    public override string ToString()
    {
        return "Transform: " + path.ToString();
    }
}
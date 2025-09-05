using Godot;
using System;

public partial class Transform : Path
{
    private Path path;
    public PointOfReference pathTransform;
    public PointOfReference spriteTransform;

    //I could add versions of this where you pass in fewer things. I'll just see what I need.
    public Transform(Path path, PointOfReference pathTransform, PointOfReference spriteTransform)
    {
        //This should combine Transforms, but I couldn't get it to work, and I ended up modifying how I do this so I don't need it.
        if (path is Transform)
            throw new InvalidOperationException("Path cannot be a Transform.");
        this.path = path;
        this.pathTransform = pathTransform;
        this.spriteTransform = spriteTransform;
    }

    public Transform(Path path, PointOfReference pathTransform) : this(path, pathTransform, PointOfReference.IDENTITY) { }

    public override PointOfReference PointOfReferenceAtTime(float s)
    {
        return pathTransform * path.PointOfReferenceAtTime(s) * spriteTransform.Inverse();
    }

    //TODO:Is this right? I feel like spriteTransform should be involved.
    public override Event Event(float s)
    {
        return pathTransform * path.Event(s);
    }

    public override PointOfReference SeenFromRest(Event e)
    {
        var seen0 = path.SeenFromRest(pathTransform.Inverse() * e);
        if (seen0 == null)
            return null;
        else
            return pathTransform * seen0 * spriteTransform.Inverse();
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
        return spriteTransform * path.ToLocalSpacetime(pathTransform.Inverse() * e);
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
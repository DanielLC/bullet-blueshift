using Godot;
using System;

public partial class Emitter : Entity
{
	private float time;
	// TODO: Overload GetEndPOR() to multiply by rotation. Then you can rotate Emitters to change what direction spawned bullets go.
	// private PointOfReference rotation = PointOfReference.IDENTITY;
	override public float Time => time;
	// path.PointOfReferenceAtTime() will return null if the Entity doesn't exist anymore. I'd add code to check for it, but what's it gonna do, return null?
	override public PointOfReference GetEndPOR()
	{
		return Path.PointOfReferenceAtTime(time);
	}
	override public void AddAcceleration(float accel, float radians, float time)
	{
		this.time += time;
		// GD.Print("Emitter.AddAcceleration: ", this.time);
	}

	override public void Extend(float time)
	{
		this.time += time;
		//GD.Print("Emitter.Extend: ", this.time);
	}
	
	override public void UpdateEmitters(float t)
	{
		//GD.Print("Emitter.UpdateEmitters");
		BaseEntity.UpdateEmitters(t);
	}

	override public void NewEmitter(int instructionPointer, Godot.Collections.Array variables)
	{
		//GD.Print("Emitter.NewEmitter");
		BaseEntity.NewEmitter(instructionPointer, variables);
	}

	public Emitter(Entity parent)
	{
		Path = parent.Path;
		BaseEntity = parent.BaseEntity;
		parent.AddChild(this);		//Note: I'm not sure about this. Maybe the Plyer should be the parent. Especially if I want Emitters to have sprites.
	}

    public override void SetCollisions(uint layers, uint collidesWith)
    {
        BaseEntity.SetCollisions(layers, collidesWith);
    }

}

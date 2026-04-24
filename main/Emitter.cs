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
		return path.PointOfReferenceAtTime(time);
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
		baseEntity.UpdateEmitters(t);
	}

	override public void NewEmitter(int instructionPointer, Godot.Collections.Array variables)
	{
		//GD.Print("Emitter.NewEmitter");
		baseEntity.NewEmitter(instructionPointer, variables);
	}

	public Emitter(Entity parent)
	{
		path = parent.path;
		baseEntity = parent.baseEntity;
	}
}

using Godot;
using System;

public partial class Emitter : Entity
{
	private float time;
	override public float Time => time;
	override public void AddAcceleration(float accel, float radians, float time)
	{
		this.time += time;
		// GD.Print("Emitter.AddAcceleration: ", this.time);
	}

	override public void Extend(float time)
	{
		this.time += time;
	}
	
	override public void UpdateEmitters()
	{
		baseEntity.UpdateEmitters();
	}

	public Emitter(Entity parent)
	{
		path = parent.path;
		baseEntity = parent.baseEntity;
	}
}

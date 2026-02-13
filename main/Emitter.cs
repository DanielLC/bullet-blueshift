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
		//GD.Print("Emitter.Extend: ", this.time);
	}
	
	override public void UpdateEmitters()
	{
		//GD.Print("Emitter.UpdateEmitters");
		baseEntity.UpdateEmitters();
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

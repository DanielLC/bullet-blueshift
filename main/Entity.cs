using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;

public partial class Entity : Node2D
{
	public Compound path;
	private Player player;
	private ShaderMaterial shader;
	public float size;
	private readonly EmitterHeap emitters = new();
	public Entity baseEntity;
	virtual public float Time => path.EndTime;
	private static readonly Dictionary<string, Texture2D> textureCache = [];
	//private float nextEmitterTime = float.PositiveInfinity;

	public void Translate(Event e)
	{
		path.Translate(e);
	}

	virtual public void NewEmitter(int instructionPointer, Godot.Collections.Array variables)
	{
		//TODO: Check for too many emitters.
		Emitter emitter = new(this);
		//GD.Print("Entity.NewEmitter: ", instructionPointer, "/", Script.instructions.Count, ", ", emitter.GetInstanceId());
		ScriptVM script = new(emitter, instructionPointer, variables);
		// These next two lines should be a little less efficient but a lot simpler than this big thing that comes after.
		// But they don't work and I can't figure out why.
		// Maybe the longer part doesn't work either and I just haven't noticed yet.
		//emitters.Push(0, script);
		//UpdateEmitters();

		for (int _ = 0; _ < 256; ++_)
		{
			//GD.Print("Entity.NewEmitter ", emitter.Time);
			if (!script.Run())
			{
				// GD.Print("Entity.NewEmitter: Emmitter ", emitter.GetInstanceId(), ", died immediately (why?)");
				// If it hits the end before anything happened, destroy it.
				emitter.QueueFree();
				// GD.Print("Entity.NewEmitter: Is the emitter being deleted? ", emitter.IsQueuedForDeletion());
				return;
			}
			if (emitter.Time > 0)
			{
				emitters.Push(emitter.Time, script);
				return;
			}
		}
		Player.Error(emitters.Peek().emitter.InstructionPointer, "Emitter got into an infinite loop containing this line on creation.");
	}

	virtual public void UpdateEmitters()
	{
		//GD.Print("Entity.UpdateEmitters");
		if (emitters.IsEmpty)
			return;
		for (int _ = 0; _ < 256; ++_)
		{
			(float time, ScriptVM emitter) = emitters.Peek();
			// GD.Print("Entity.UpdateEmitters: " + path.EndTime + " " + time);
			// BUG: I think the problem here is that I'm looking at the end of the path. I should be looking at the current time show on the screen. I end up spawinging bullets in the future, which the game isn't set up to handle.
			// That's also going to be a problem for spawning enemies some distance away.
			if (path.EndTime < time)
				return;
			if (emitter.Run())
			{
				// If it's still running, update the time.
				// It would be faster to keep looping until the time changes, but then that would make the infinite loop checker more complicated.
				// I can also use emitter.timeToPause.
				emitters.UpdateTop(emitter.entity.Time);
			}
			else
			{
				// If it's not running anymore, destroy the emitter.
				emitters.Pop();
				emitter.entity.QueueFree();
			}
			//GD.Print("Entity.UpdateEmitters: ", emitter.entity.Time);
			if (emitters.IsEmpty)
				return;
		}
		Player.Error(emitters.Peek().emitter.InstructionPointer, "Code entered an infinite loop containing this line.");
	}

	public PointOfReference PointOfReferenceAtTime(float s)
	{
		return path.PointOfReferenceAtTime(s);
	}

	public void Initialize(Player player, float size, PointOfReference pointOfReference, float rotationSpeed)
	{
		baseEntity = this;
		this.player = player;
		//Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
		var sprite = GetNode<MeshInstance2D>("MeshInstance2D");
		if (sprite != null && sprite.Material is ShaderMaterial mat)
		{
			shader = (Godot.ShaderMaterial)sprite.Material.Duplicate();
			sprite.Material = shader;
			shader.SetShaderParameter("size", size);
			shader.SetShaderParameter("scale", new Vector2(1f, 1f)); //TODO: This shouldn't be hardcoded.
			shader.SetShaderParameter("spin_speed", rotationSpeed);
		}
		sprite.Scale = new Vector2(size, size) * 4;
		this.size = size;
		path = new Compound(shader, pointOfReference, rotationSpeed);
	}

	public void Initialize(Player player, float size, float rotationSpeed)
	{
		Initialize(player, size, PointOfReference.IDENTITY, rotationSpeed);
	}
	//Note: This can be used to pre-load a texture.
	public static Texture2D GetTexture(string path, int lineNumber)
	{
		if (!textureCache.TryGetValue(path, out var texture))
		{
			var absPath = "res://".PathJoin(path);
			texture = (Texture2D)GD.Load(absPath);
			if (texture == null)
			{
				var fullPath = ProjectSettings.GlobalizePath(absPath);
				//TODO: Give the full error.
				//It needs the line number too. Should I pass that in?
				Player.Error(lineNumber, $"No texture found at'{fullPath}'.");
			}
			textureCache.Add(path, texture);
		}
		return texture;
	}

	// Maybe I should just make this work on Emitters. There are better ways to make ships with multiple spinning sprites, but until I add that, this is easy.
	public void SetParameters(string spritePath, float size, float rotationSpeed, int lineNumber)
	{
		var sprite = GetNode<MeshInstance2D>("MeshInstance2D");
		if (sprite != null && sprite.Material is ShaderMaterial mat)
		{
			shader.SetShaderParameter("texture_sampler", GetTexture(spritePath, lineNumber));
			shader.SetShaderParameter("size", size);
			shader.SetShaderParameter("spin_speed", rotationSpeed);
			sprite.Scale = new Vector2(size, size) * 4;
			this.size = size;
			path.rotationSpeed = rotationSpeed;
		}
		else
		{
			Player.Error(lineNumber, "SetParameters() called on an Emitter. Emitters don't have sprites.");
		}
	}

	public void Update()
	{
		if (path.CheckIfAllInPast(player.por.GetEvent(), size, shader))
		{
			QueueFree();
			return;
		}
		PointOfReference relativePOR = path.Seen(player.por);
		if (relativePOR == null)
		{
			//For now I'll just move it out of view.
			Position = new Vector2(9999, 9999);
			return; //TODO: Make it invisible or something.
		}
		Event pos = relativePOR.GetEvent();
		Velocity velocity = relativePOR.GetVelocity();
		Position = new Vector2(pos.x, pos.y);
		//sprite.Modulate = GetColor(GetDoppler(relativePOR));
		if (shader != null)
		{
			UpdateShader();
		}
	}

	private void UpdateShader()
	{
		shader.SetShaderParameter("por", player.por.xform);
		shader.SetShaderParameter("por_inverse", player.por.inverse);
		shader.SetShaderParameter("observer_position", Position);
		//shader.SetShaderParameter("scale", 0.001f); //TODO: This shouldn't be hardcoded.
	}

	private float GetDoppler(PointOfReference por)
	{
		var e = por.GetEvent();
		if (e.x == 0 && e.y == 0)
		{
			return 1;
		}
		var v = por.GetVelocity();
		return 1 / (v.t * (1 + (v.x * e.x + v.y * e.y) / Mathf.Sqrt(e.x * e.x + e.y * e.y)));
	}

	//Returns a number from 0 to 1 for values of t from 0 to 1.
	//Ideally something with a derivative of 0 at 0 and 1.
	//I'm using cosine.
	private float Interpolate(float t)
	{
		return (1 - Mathf.Cos(t * (float)Math.PI)) / 2;
	}

	// public Color GetColor(float ratio)
	// {
	// 	float originalWavelength = Mathf.Sqrt(2);
	// 	float originalIntensity = 1;
	// 	float[] rgb = { 0f, 0f, 0f };
	// 	var shiftedWavelength = Mathf.Log(originalWavelength * ratio) / Mathf.Log(2) * 5 - 1;
	// 	var shiftedIntensity = originalIntensity; //I should probably divide by ratio
	// 	var lower = Mathf.FloorToInt(shiftedWavelength);
	// 	var higher = lower + 1;
	// 	float t = shiftedWavelength - lower;
	// 	if (0 <= lower && lower < 3)
	// 		rgb[lower] = Interpolate(1 - t) * shiftedIntensity;
	// 	if (0 <= higher && higher < 3)
	// 		rgb[higher] = Interpolate(t) * shiftedIntensity;
	// 	else if (higher == 3)
	// 		rgb[0] = Interpolate(2 * t) * shiftedIntensity / 2;
	// 	return new Color(rgb[0], rgb[1], rgb[2]);
	// }

	virtual public void AddAcceleration(float accel, float radians, float time)
	{
		path.AddAcceleration(accel, radians, time);
		UpdateEmitters();
	}

	virtual public void Extend(float time)
	{
		path.Extend(time);
		UpdateEmitters();
	}
	public Event GetEnd()
	{
		return path.GetEnd();
	}
	public PointOfReference GetEndPOR()
	{
		return path.GetEndPOR();
	}
}

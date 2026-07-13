using Godot;
using System;
using System.Collections.Generic;

public partial class Entity : Node2D
{
	public Compound Path;
	private Player player;
	private ShaderMaterial shader;
	public float Size;
	private readonly EmitterHeap emitters = new();
	public Entity BaseEntity;
	virtual public float Time => Path.EndTime;
	private static readonly Dictionary<string, Texture2D> textureCache = [];
	//private static readonly PackedScene ExplosionScene = (PackedScene)ResourceLoader.Load("res://main/Explosion.tscn");
	private Area2D area;
	private CircleShape2D shape;
	public ScriptContext scriptContext;
	public float hp;

	/*public Explosion Explode()
	{
		var explosion = (Explosion)ExplosionScene.Instantiate();
		explosion.Initialize(player, Path.GetEndPOR());
		player.AddChild(explosion);
		return explosion;
	}*/

	public void Translate(Event e)
	{
		Path.Translate(e);
	}

	virtual public void NewEmitter(int instructionPointer, Godot.Collections.Array variables)
	{
		//TODO: Check for too many emitters.
		//Emitter is a node. This is not the proper way to construct it.
		Emitter emitter = new(this, Time);
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
		Player.RuntimeError(emitters.Peek().emitter.InstructionPointer, "Emitter got into an infinite loop containing this line on creation.");
	}

	virtual public void UpdateEmitters(float t)
	{
		//GD.Print("Entity.UpdateEmitters");
		if (emitters.IsEmpty)
			return;
		for (int _ = 0; _ < 256; ++_)
		{
			(float time, ScriptVM emitter) = emitters.Peek();
			 // GD.Print("Entity.UpdateEmitters: ", t, ", ", time);
			if (t < time + 0.0001f)
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
				GD.Print("Entity.UpdateEmitters: Emitter destroyed");
				emitters.Pop();
				emitter.entity.QueueFree();
			}
			if (emitters.IsEmpty)
				return;
		}
		Player.RuntimeError(emitters.Peek().emitter.InstructionPointer, "Code entered an infinite loop containing this line.");
	}

	public PointOfReference PointOfReferenceAtTime(float s)
	{
		return Path.PointOfReferenceAtTime(s);
	}

	public void Initialize(Player player, float size, PointOfReference pointOfReference, float rotationSpeed, bool isPlayer = false)
	{
		SetPosition(player.por.Inverse() * pointOfReference);
		BaseEntity = this;
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
		Size = size;
		//GD.Print("Entity.Initialize: " + isPlayer);
		if (isPlayer)
			Path = new PlayerCompound(shader, pointOfReference, rotationSpeed);
		else
			Path = new Compound(shader, pointOfReference, rotationSpeed);
		area = GetNode<Area2D>("Area2D");
		area.AreaEntered += OnAreaEntered;
		var collision = area.GetNode<CollisionShape2D>("CollisionShape2D");
		collision.Shape = collision.Shape.Duplicate() as Shape2D;
		shape = collision.Shape as CircleShape2D;
		shape.Radius = size;
	}

	public void Initialize(Player player, float size, float rotationSpeed)
	{
		Initialize(player, size, PointOfReference.IDENTITY, rotationSpeed);
	}
	//Note: This can be used to pre-load a texture.
	public static Texture2D GetTexture(string path, int instructionPointer)
	{
		if (!textureCache.TryGetValue(path, out var texture))
		{
			var absPath = "res://".PathJoin(path);
			texture = (Texture2D)GD.Load(absPath);
			if (texture == null)
			{
				var fullPath = ProjectSettings.GlobalizePath(absPath);
				// TODO: Give the full error.
				// It needs the line number too. Should I pass that in?
				// Looks like GD.Load() is just throwing an exception.
				Player.RuntimeError(instructionPointer, $"No texture found at'{fullPath}'.");
			}
			textureCache.Add(path, texture);
		}
		return texture;
	}

	// Maybe I should just make this work on Emitters. There are better ways to make ships with multiple spinning sprites, but until I add that, this is easy.
	public void SetParameters(string spritePath, float size, float rotationSpeed, int instructionPointer)
	{
		var sprite = GetNode<MeshInstance2D>("MeshInstance2D");
		if (sprite != null && sprite.Material is ShaderMaterial mat)
		{
			shader.SetShaderParameter("texture_sampler", GetTexture(spritePath, instructionPointer));
			shader.SetShaderParameter("size", size);
			shader.SetShaderParameter("spin_speed", rotationSpeed);
			sprite.Scale = new Vector2(size, size) * 4;
			Size = size;
			Path.rotationSpeed = rotationSpeed;
			shape.Radius = size;
		}
		else
		{
			Player.RuntimeError(instructionPointer, "SetParameters() called on an Emitter. Emitters don't have sprites.");
		}
	}

	public void Update()
	{
		if (Path.CheckIfAllInPast(player.por.GetEvent(), Size))
		{
			QueueFree();
			return;
		}
		(var relativePOR, float t) = Path.Seen(player.por);
		if (player.explosion != null && this != player.playerEntity && player.explosion.Contains(player.por * relativePOR.GetEvent()))
		{
			Die();
		}
		SetPosition(relativePOR);
		//sprite.Modulate = GetColor(GetDoppler(relativePOR));
		if (shader != null)
		{
			UpdateShader();
		}
		UpdateEmitters(t);
	}

	private void SetPosition(PointOfReference relativePOR)
	{
		if (relativePOR == null)
		{
			//For now I'll just move it out of view.
			Position = new Vector2(9999, 9999);
			return; //TODO: Make it invisible or something.
		}
		Event pos = relativePOR.GetEvent();
		Position = new Vector2(pos.x, pos.y);
	}

	private void UpdateShader()
	{
		shader.SetShaderParameter("por", player.por.xform);
		shader.SetShaderParameter("por_inverse", player.por.inverse);
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
		Path.AddAcceleration(accel, radians, time);
	}

	virtual public void Extend(float time)
	{
		Path.Extend(time);
	}

	virtual public void Rest(float time)
	{
		Path.Rest(time);
	}
	virtual public void Boost(Velocity v)
	{
		Path.Boost(v);
	}
	virtual public new void Rotate(float radians)
	{
		Path.Rotate(radians);
	}
	public Event GetEnd()
	{
		return Path.GetEnd();
	}
	virtual public PointOfReference GetEndPOR()
	{
		return Path.GetEndPOR();
	}

	public const uint Ally = 1u;
	public const uint Enemy = 2u;
	public const uint EnemyBullet = 4u;
	public const uint AllyBullet = 8u;
	private void OnAreaEntered(Area2D otherArea)
	{
		var otherShape = (CircleShape2D)area.GetNode<CollisionShape2D>("CollisionShape2D").Shape;
		// If it's an ally or enemy, decrease the hp and then die.
		if ((area.CollisionLayer & 0b11) != 0)
		{
			Entity other = otherArea.GetParent<Entity>();
			hp -= other.hp;
			if (hp <= 0)
			{
				// GD.Print($"{area.CollisionLayer == Ally}");
				if (area.CollisionLayer == Ally)
				{
					if(player.explosion == null)
						player.Explode();
				}
				else
				{
					Die();
				}
			}
		}
		else
		{
			Die();
		}
	}
	public void Die()
	{
		Path.Die();
		// TODO: The object shouldn't be destroyed instantly. It should stick around as it's destroyed.
		QueueFree();
	}
	virtual public void SetCollisions(uint layers, uint collidesWith)
	{
		area.CollisionLayer = layers;
		area.CollisionMask = collidesWith;
	}
}

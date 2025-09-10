using Godot;
using System;

public partial class Entity : Node2D
{
	private Compound path;
	private Player player;
	private ShaderMaterial shader;
	private float size;

	public void Initialize(Player player, float size, PointOfReference pointOfReference, float rotationSpeed)
	{
		GD.Print("Entity spawned.");
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

	public void Update()
	{
		if (path.CheckIfAllInPast(player.por.GetEvent(), size, shader))
		{
			GD.Print("Entity Removed");
			QueueFree();
			return;
		}
		PointOfReference relativePOR = path.Seen(player.por);
		if (relativePOR == null)
		{
			GD.Print("Entity Invisible");
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

	public Color GetColor(float ratio)
	{
		float originalWavelength = Mathf.Sqrt(2);
		float originalIntensity = 1;
		float[] rgb = { 0f, 0f, 0f };
		var shiftedWavelength = Mathf.Log(originalWavelength * ratio) / Mathf.Log(2) * 5 - 1;
		var shiftedIntensity = originalIntensity; //I should probably divide by ratio
		var lower = Mathf.FloorToInt(shiftedWavelength);
		var higher = lower + 1;
		float t = shiftedWavelength - lower;
		if (0 <= lower && lower < 3)
			rgb[lower] = Interpolate(1 - t) * shiftedIntensity;
		if (0 <= higher && higher < 3)
			rgb[higher] = Interpolate(t) * shiftedIntensity;
		else if (higher == 3)
			rgb[0] = Interpolate(2 * t) * shiftedIntensity / 2;
		return new Color(rgb[0], rgb[1], rgb[2]);
	}

	public void AddAcceleration(float accel, float radians, float time)
	{
		path.AddAcceleration(accel, radians, time);
	}
}

using Godot;
using System;

public partial class Explosion : Node2D
{
    private Player player;
    private ShaderMaterial shader;
    private Event position;
    public float size;

    override public void _Ready()
    {
        GD.Print("Explosion._Ready");
    }

    public void Initialize(Player player, float size, float brightness, PointOfReference pointOfReference)
    {
        this.player = player;
        position = pointOfReference.GetEvent();
        var sprite = GetNode<MeshInstance2D>("MeshInstance2D");
        if (sprite != null && sprite.Material is ShaderMaterial mat)
        {
            shader = (Godot.ShaderMaterial)sprite.Material.Duplicate();
            sprite.Material = shader;
            shader.SetShaderParameter("radius", size);
            shader.SetShaderParameter("brightness", brightness);
            shader.SetShaderParameter("event", position.ToVector());
            shader.SetShaderParameter("velocity", pointOfReference.GetVelocity().ToVector());
        }
        else
        {
            GD.Print("Explosion.Initialize: Shader not found.");
            GD.Print("Sprite: ", sprite);
            GD.Print("Material: ", sprite.Material);
            GD.Print("Material Type: ", sprite.Material.GetClass());
        }
        sprite.Scale = new Vector2(size, size) * 4;
        this.size = size;
        GD.Print("Explosion.Initialize");
        _Process(0);
    }

    // TODO: Make sure it destroys the explosion eventually.
    override public void _Process(double delta)
    {
        UpdateShader();
        var relPos = player.por.Inverse() * position;
        Position = new Vector2(relPos.x, relPos.y);
        // GD.Print("Explosion._Process: ", Position);
    }
    private void UpdateShader()
    {
        shader.SetShaderParameter("por", player.por.xform);
        shader.SetShaderParameter("por_inverse", player.por.inverse);
    }
}

using Godot;
using System;

public partial class Explosion : Node2D
{
    private Player player;
    private ShaderMaterial shader;
    private PointOfReference por;
    public float size;
    public float speed;

    override public void _Ready()
    {
        GD.Print("Explosion._Ready");
    }

    public void Initialize(Player player, PointOfReference pointOfReference)
    {
        this.player = player;
        por = pointOfReference;
        var sprite = GetNode<MeshInstance2D>("MeshInstance2D");
        if (sprite != null && sprite.Material is ShaderMaterial mat)
        {
            shader = (Godot.ShaderMaterial)sprite.Material.Duplicate();
            sprite.Material = shader;
            //shader.SetShaderParameter("radius", size);
            //shader.SetShaderParameter("brightness", brightness);
            shader.SetShaderParameter("event", por.GetEvent().ToVector());
            //shader.SetShaderParameter("velocity", pointOfReference.GetVelocity().ToVector());
            shader.SetShaderParameter("frequency", 4f);
        }
        else
        {
            GD.Print("Explosion.Initialize: Shader not found.");
            GD.Print("Sprite: ", sprite);
            GD.Print("Material: ", sprite.Material);
            GD.Print("Material Type: ", sprite.Material.GetClass());
        }
        sprite.Scale = new Vector2(1, 1) * 4;
        GD.Print("Explosion.Initialize");
        _Process(0);
        speed = 0.7f;
    }

    // Test this.
    public bool Contains(Event e)
    {
        e = por.Inverse() * e;
        return e.t * e.t * speed * speed > e.x * e.x + e.y * e.y;
    }

    // TODO: Make sure it destroys the explosion eventually.
    override public void _Process(double delta)
    {
        UpdateShader();
        // var relPos = player.por.Inverse() * por.GetEvent();
        // Position = new Vector2(relPos.x, relPos.y);
        // GD.Print("Explosion._Process: ", Position);
    }
    private void UpdateShader()
    {
        shader.SetShaderParameter("por", player.por.xform);
        shader.SetShaderParameter("por_inverse", player.por.inverse);
    }
}

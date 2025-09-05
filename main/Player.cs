using Godot;
using System;

public partial class Player : Node2D
{
    public const float ACCELERATION = 0.3f;
    //public const float SCALE = 1f;

    public PointOfReference por;
    private static readonly PackedScene EntityScene = (PackedScene) ResourceLoader.Load("res://main/Entity.tscn");

    public Player()
    {
        por = PointOfReference.IDENTITY;
        // Spawn entities here. At least until I add in scripting.
        var enemy = SpawnEntity();
        enemy.AddAcceleration(0.1f, -(float)Math.PI, 0.5f);
        enemy.AddAcceleration(0.1f, -0.5f * (float)Math.PI, 0.5f);
        for (int i = 0; i < 20; ++i)
        {
            enemy.AddAcceleration(0.1f, i * 0.5f * (float)Math.PI, 0.5f);
            //enemy.AddAcceleration(0.1f, 0, 0.2f);
        }

        /*Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
        sprite.ZIndex = 1;
        sprite.Scale *= 0.001f;*/

        //Benchmark();
    }

    private void Benchmark()
    {
        var accel = 0.1f;
        var t = 0.3f;
        var enemy = SpawnEntity(0.25f);
        enemy.AddAcceleration(accel * 2, -(float)Math.PI, t);
        enemy.AddAcceleration(accel / 2, -(float)Math.PI / 2, t);
        for (int i = 0; i < 20; ++i)
            enemy.AddAcceleration(accel, i/2*(float)Math.PI, t);

        var golden_angle = (3 - Mathf.Sqrt(5)) * (float)Math.PI;
        var radians = 0f;
        for (int i = 0; i < 1000; ++i) {
            var bullet = SpawnEntity(0.01f);
            bullet.AddAcceleration(accel, radians, 10);
            radians = (radians + golden_angle) % (2*(float)Math.PI);
        }
    }
    
	public override void _Process(double delta)
    {
        GD.Print($"FPS: {1 / delta}");
        float deltaF = (float)delta;
        //var viewportSize = GetViewportRect().Size;
        //Position = viewportSize / 2;
        //Scale = Mathf.Min(viewportSize.X, viewportSize.Y) * SCALE * Vector2.One;
        var x = Input.GetActionStrength("ui_right") - Input.GetActionStrength("ui_left");
        var y = Input.GetActionStrength("ui_down") - Input.GetActionStrength("ui_up");
        var dir = new Vector2(x, y).Normalized() * deltaF * ACCELERATION;
        var v = Velocity.FromRapidity(dir.X, dir.Y);
        //por *= new PointOfReference(new Event(0, 0, deltaF), v);
        por *= v.GetLorentz();
        por *= new Event(0, 0, deltaF).GetTranslation();
        //I'm pretty sure this does it in the opposite order I'd like
        //(it moves you forward in time, then accelerates, so it's less responsive),
        //but ultimately I'd like to change it to proper hyperbolic motion.
        //por = por.times(PointOfReference.from_event_and_velocity(Event.new(0,0,delta), Velocity.from_rapidity(dir.x, dir.y)))
        //por = PointOfReference.from_event_and_velocity(Event.new(0,0,delta), Velocity.from_rapidity(dir.x, dir.y)).times(por)
        //por = por.Accelerate(v)
        //por = por.Wait(delta)
        por.RecalculateInverse();   //I probably don't have to do this every time, but I'll do it for now.

        foreach (var child in GetChildren())
        {
            if (child is Entity entity)
            {
                entity.Update();
            }
        }
    }

    private Entity SpawnEntity(float size) {
        Entity entity = (Entity) EntityScene.Instantiate();
        entity.Initialize(this, size);
        AddChild(entity);
        return entity;
    }

    private Entity SpawnEntity() {
        return SpawnEntity(0.1f);
    }
}
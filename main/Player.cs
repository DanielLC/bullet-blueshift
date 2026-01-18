using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class Player : Node2D
{
    public const float ACCELERATION = 0.5f;
    public const float PLAYER_SIZE = 0.05f;
    public const float PLAYER_ROTATION = 0f;
    private List<Tuple<Event, ScriptVM>> events = [];
    //public const float SCALE = 1f;

    public PointOfReference por;
    private static readonly PackedScene EntityScene = (PackedScene)ResourceLoader.Load("res://main/Entity.tscn");
    //private Entity entity;    //This was representing the player. The game is not ready for it and it's probably overkill.
    private static Player instance;

    public Player()
    {
        instance = this;
    }

    public override void _Ready()
    {
        try
        {
            Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
            //sprite.ZIndex = 1;
            sprite.Scale /= 128;

            por = PointOfReference.IDENTITY;
            //entity = SpawnEntity(PLAYER_SIZE, PLAYER_ROTATION);

            ScriptedLevel();
            //BasicLevel();
            //Benchmark();
            //TestLevel();

        }
        catch (Exception e)
        {
            Debug.WriteLine("ERROR!");
            GD.PushError(e.ToString());
            GetTree().Paused = true; // or GetTree().Quit();
        }
    }

    private void ScriptedLevel()
    {
        new ScriptCompiler().ParseFile("script.txt");
        var enemy = SpawnEntity(0.1f, 0f, new Event(0, 0.2f, 0.3f).GetTranslation());
        ScriptVM script = new ScriptVM(enemy);
        var e = script.Run();
        if (e != null)
            events.Add(new Tuple<Event, ScriptVM>(e, script));
    }

    private void TestLevel()
    {
        var accel = 0.1f;
        var t = 1f;
        var enemy = SpawnEntity(0.25f, 1f);
        enemy.AddAcceleration(accel / 2, -(float)Math.PI, t);
        enemy.AddAcceleration(accel / 2, -(float)Math.PI / 2, t);
        for (int i = 0; i < 20; ++i)
            enemy.AddAcceleration(accel, i * 0.5f * (float)Math.PI, t);

        //PointOfReference por = new PointOfReference(new Event(0.2f, 0, 2), new Velocity(0, 0.2f));
        //PointOfReference por = enemy.PointOfReferenceAtTime(2f).GetEvent().GetTranslation();
        for (var i = 0; i < 100; ++i)
        {
            var bullet = SpawnEntity(0.01f, 1f, enemy.PointOfReferenceAtTime(0.1f * i).GetEvent().GetTranslation());
            bullet.AddAcceleration(0, 0, 10);
        }
    }

    private void Benchmark()
    {
        var accel = 0.1f;
        var t = 0.3f;
        var enemy = SpawnEntity(0.25f, 1f);
        enemy.AddAcceleration(accel / 2, -(float)Math.PI, t);
        enemy.AddAcceleration(accel / 2, -(float)Math.PI / 2, t);
        for (int i = 0; i < 20; ++i)
            enemy.AddAcceleration(accel, i * 0.5f * (float)Math.PI, t);

        var golden_angle = (3 - Mathf.Sqrt(5)) * (float)Math.PI;
        var radians = 0f;
        for (int i = 0; i < 1000; ++i)
        {
            var bullet = SpawnEntity(0.01f, 1f);
            bullet.AddAcceleration(accel, radians, 10);
            radians = (radians + golden_angle) % (2 * (float)Math.PI);
        }
    }

    private void BasicLevel()
    {
        var accel = 0.1f;
        var t = 1f;
        var enemy = SpawnEntity(0.15f, 1f, new Event(0, -0.3f, -0.1f).GetTranslation());
        enemy.AddAcceleration(accel / 2, -(float)Math.PI, t);
        enemy.AddAcceleration(accel / 2, -(float)Math.PI / 2, t);
        for (int i = 0; i < 20; ++i)
            enemy.AddAcceleration(accel, i * 0.25f * (float)Math.PI, t);

        var angle_delta_delta = 0.1f;
        var angle_delta = 0f;
        var cur_angle = 0f;
        for (int i = 0; i < 100; ++i)
        {
            var bullet = SpawnEntity(0.01f, 1f, enemy.PointOfReferenceAtTime(i * 0.1f));
            bullet.AddAcceleration(accel, cur_angle, 10);
            cur_angle = (cur_angle + angle_delta) % (2 * (float)Math.PI);
            angle_delta = (angle_delta + angle_delta_delta) % (2 * (float)Math.PI);
        }
    }

    public override void _Process(double delta)
    {
        try
        {
            //GD.Print($"FPS: {1 / delta}");
            float deltaF = (float)delta;
            //var viewportSize = GetViewportRect().Size;
            //Position = viewportSize / 2;
            //Scale = Mathf.Min(viewportSize.X, viewportSize.Y) * SCALE * Vector2.One;
            var x = Input.GetActionStrength("ui_right") - Input.GetActionStrength("ui_left");
            var y = Input.GetActionStrength("ui_down") - Input.GetActionStrength("ui_up");
            //entity.AddAcceleration(Mathf.Sqrt(x*x + y*y) * ACCELERATION, Mathf.Atan2(-x, y), deltaF);
            //por = entity.GetEndPOR();
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

            var curEvent = por.GetEvent();
            for (int i = 0; i < events.Count;)
            {
                if (events[i].Item1 < curEvent)
                {
                    var newEvent = events[i].Item2.Run();
                    if (newEvent == null)
                    {
                        // If it's done, just move the last one here.
                        events[i] = events[^1];
                        events.RemoveAt(events.Count - 1);
                    }
                    else
                    {
                        // Otherwise, use the new position.
                        events[i] = new Tuple<Event, ScriptVM>(newEvent, events[i].Item2);
                    }
                    continue;
                }
                // Only increment i if the event has not updated. Otherwise you'll need to check it again.
                // And you'll need to check if i < events.Count, since that might have changed, which is why I'm continuing the for loop instead of using a while.
                ++i;
            }

            foreach (var child in GetChildren())
            {
                if (child is Entity entity)
                {
                    entity.Update();
                }
            }
        }
        catch (Exception e)
        {
            GD.PushError(e.ToString());
            GetTree().Paused = true; // or GetTree().Quit();
        }
    }

    public static Entity SpawnEntity(float size, float rotationSpeed, PointOfReference pointOfReference)
    {
        Entity entity = (Entity)EntityScene.Instantiate();
        entity.Initialize(instance, size, pointOfReference, rotationSpeed);
        instance.AddChild(entity);
        return entity;
    }

    private Entity SpawnEntity(float size, float rotationSpeed)
    {
        return SpawnEntity(size, rotationSpeed, PointOfReference.IDENTITY);
    }

    private Entity SpawnEntity()
    {
        return SpawnEntity(0.1f, 1f, PointOfReference.IDENTITY);
    }
}
using Godot;
using System;
using System.Diagnostics;

public partial class ScriptContext : RefCounted
{
    private static RandomNumberGenerator rng = new();
    private Entity entity;
    private ScriptVM scriptVM;
    private const float DEG_TO_RAD = Mathf.Pi / 180;
    public ScriptContext(ScriptVM scriptVM)
    {
        this.scriptVM = scriptVM;
        entity = scriptVM.entity;
    }

    public void explode(float size, float brightness)
    {
        entity.Explode(size, brightness);
    }
    public void displace(float r, float degrees, float time)
    {
        entity.Translate(new Event(r * sin(degrees), -r * cos(degrees), time));
    }
    public void displace(float r, float degrees)
    {
        entity.Translate(new Event(r * sin(degrees), -r * cos(degrees), -r));
    }
    public void setParameters(string sprite, float size, float rotationSpeed)
    {
        entity.SetParameters(sprite, size, rotationSpeed, scriptVM.InstructionPointer);
    }
    public void rotate(float degrees)
    {
        entity.Path.Rotate(degrees * DEG_TO_RAD);
    }
    // This part is just ScriptContext. Emitters can't accelerate.
    public void accelerate(float acceleration, float degrees, float time)
    {
        entity.AddAcceleration(acceleration, (degrees + 180) * DEG_TO_RAD, time);
        scriptVM.timeToPause = true;
    }
    public void wait(float time)
    {
        entity.Extend(time);
        scriptVM.timeToPause = true;
    }
    public void nextFrame()
    {
        scriptVM.nextFrame = true;
    }
    // Here's some generally useful functions copied from the script in my old program.
    public float accelerationFromDistanceAndTime(float distance, float time)
    {
        return 2 / (time * time / distance - distance);
    }

    public float speedFromAccelerationAndTime(float acceleration, float time)
    {
        return 1 / sqrt(1 + 1 / (acceleration * acceleration * time * time));
    }

    public float gamma(float v)
    {
        return 1 / sqrt(1 - v * v);
    }
    // Everything here should be in an Emitter class that ScriptContext extends.
    // TODO: Modify this to also show it on the screen.
    public void print(params object[] args)
    {
        GD.Print(string.Concat(args));
    }

    public bool actionPressed(string action)
    {
        return Input.IsActionPressed(action);
    }
    public bool keyPressed(string keyName)
    {
        var key = OS.FindKeycodeFromString(keyName);
        return Input.IsKeyPressed(key);
    }
    public float wasdDirection()
    {
        float x = 0;
        float y = 0;
        if (Input.IsPhysicalKeyPressed(Key.W)) y += 1;
        if (Input.IsPhysicalKeyPressed(Key.A)) x -= 1;
        if (Input.IsPhysicalKeyPressed(Key.S)) y -= 1;
        if (Input.IsPhysicalKeyPressed(Key.D)) x += 1;
        // I think there might be something keeping it from reacting right to positive infinity?
        return angleTo(x, y);
    }
    // It might be better to combine these and add a way to read tuples into two variables.
    public float mouseDirection()
    {
        Vector2 mouse;
        try
        {
            mouse = entity.GetViewport().GetMousePosition();
        }
        catch (Exception e)
        {
            GD.Print("ScriptContext.mouseDirection: ", e.Message);
            return float.PositiveInfinity;
        }
        Vector2 center = entity.GetViewportRect().Size * 0.5f;
        Vector2 relative = mouse - center;
        var angle = angleTo(relative.X, -relative.Y);
        return angle;
    }
    // If I have this one, I really should normalize it with screen size.
    public float mouseDistance()
    {
        Vector2 mouse = entity.GetViewport().GetMousePosition();
        Vector2 center = entity.GetViewportRect().Size * 0.5f;
        Vector2 relative = mouse - center;
        return relative.Length();
    }

    /*public void setLayer(int layer)
    {
        entity.Area.CollisionLayer |= 1u << layer;
    }
    public void clearLayer(int layer)
    {
        entity.Area.CollisionLayer &= ~(1u << layer);
    }
    public void clearLayers()
    {
        entity.Area.CollisionLayer = 0u;
    }
    public void setMask(int layer)
    {
        entity.Area.CollisionMask |= 1u << layer;
    }
    public void clearMask(int layer)
    {
        entity.Area.CollisionMask &= ~(1u << layer);
    }
    public void clearMasks()
    {
        entity.Area.CollisionMask = 0u;
    }*/

    public void setAlly(float hp)
    {
        entity.SetCollisions(Entity.Ally, Entity.EnemyBullet);
        entity.hp = hp;
    }
    public void setEnemyBullet(float hp)
    {
        entity.SetCollisions(Entity.EnemyBullet, Entity.Ally);
        entity.hp = hp;
    }
    public void setEnemy(float hp)
    {
        entity.SetCollisions(Entity.Enemy, Entity.AllyBullet);
        entity.hp = hp;
    }
    public void setAllyBullet(float hp)
    {
        entity.SetCollisions(Entity.AllyBullet, Entity.Enemy);
        entity.hp = hp;
    }

    public void setAlly() => setAlly(0);
    public void setEnemyBullet() => setEnemyBullet(0);
    public void setEnemy() => setEnemy(0);
    public void setAllyBullet() => setAllyBullet(0);
    public float getHP() => entity.hp;
    public void setHP(float hp) => entity.hp = hp;
    public void die() => entity.Die();

    // UIElement stuff
    public UIElement panel(float x, float y, float width, float height) => new UIElement(x, y, width, height);
    public void setText(UIElement panel, string text) => panel.Text = text;
    public void setColor(UIElement panel, string color) => panel.Background = color;
    public void setSprite(UIElement panel, string sprite) => panel.SetSprite(sprite, scriptVM.InstructionPointer);
    public void destroyPanel(UIElement panel) => panel.QueueFree();

    // Math constants
    public float e = Mathf.E;
    public float pi = Mathf.Pi;
    public float goldenAngle = 137.50776405003785f; // 180 * (3 - Mathf.Sqrt(5));
    public float nan = float.NaN;
    // Math functions
    public bool isNaN(float x) => float.IsNaN(x);
    public float sqrt(float x) => Mathf.Sqrt(x);
    public float exp(float x) => Mathf.Exp(x);
    public float pow(float x, float y) => Mathf.Pow(x, y);
    public float log(float x) => Mathf.Log(x);
    public float log(float x, float b) => Mathf.Log(x) / Mathf.Log(b);
    public float abs(float x) => Mathf.Abs(x);
    public float floor(float x) => Mathf.Floor(x);
    public float ceil(float x) => Mathf.Ceil(x);
    public float round(float x) => Mathf.Round(x);
    public float mod(float a, float b) => a % b;
    // Trig
    public float sin(float x) => Mathf.Sin(x * DEG_TO_RAD);
    public float cos(float x) => Mathf.Cos(x * DEG_TO_RAD);
    public float tan(float x) => Mathf.Tan(x * DEG_TO_RAD);
    public float asin(float x) => 180 / pi * Mathf.Asin(x);
    public float acos(float x) => 180 / pi * Mathf.Acos(x);
    public float atan(float x) => 180 / pi * Mathf.Atan(x);
    public float angleTo(float x, float y)
    {
        if (x == 0 && y == 0)
            return float.NaN;
        else
            return 180 / pi * Mathf.Atan2(x, y);
    }
    // Hyperbolic trig
    public float sinh(float x) => Mathf.Sinh(x);
    public float cosh(float x) => Mathf.Cosh(x);
    public float tanh(float x) => Mathf.Tanh(x);
    public float asinh(float x) => Mathf.Asinh(x);
    public float acosh(float x) => Mathf.Acosh(x);
    public float atanh(float x) => Mathf.Atanh(x);
    // Random functions
    public float random() => rng.Randf();
    // Random functions
    public float random(float max) => rng.Randf() * max;
    // Random functions
    public float random(float min, float max) => rng.RandfRange(min, max);
    public int randInt(int min, int max) => rng.RandiRange(min, max);
    public float normal(float mean, float deviation) => rng.Randfn(mean, deviation);
}

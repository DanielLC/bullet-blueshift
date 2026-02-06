using Godot;
using System;

public partial class ScriptContext : RefCounted
{
    private static RandomNumberGenerator rng = new RandomNumberGenerator();
    private Entity entity;
    private ScriptVM scriptVM;
    //This part is just ScriptContext. Emitters can't accelerate.
    public ScriptContext(ScriptVM scriptVM)
    {
        this.scriptVM = scriptVM;
        this.entity = scriptVM.entity;
    }
    public void accelerate(float acceleration, float angle, float time)
    {
        entity.AddAcceleration(acceleration, angle * Mathf.Pi/180, time);
        scriptVM.timeToPause = true;
    }
    public void wait(float time)
    {
        entity.Extend(time);
        scriptVM.timeToPause = true;
    }
    //Everything here should be in an Emitter class that ScriptContext extends.
    //TODO: Modify this to also show it on the screen.
    public void print(params object[] args)
    {
        GD.Print(string.Concat(args));
    }
    // Math constants
    public float e = Mathf.E;
    public float pi = MathF.PI;
    public float goldenAngle = 180 * (3 - Mathf.Sqrt(5));
    // Math functions
    public float sqrt(float x) => Mathf.Sqrt(x);
    public float exp(float x) => Mathf.Exp(x);
    public float pow(float x, float y) => Mathf.Pow(x, y);
    public float log(float x) => Mathf.Log(x);
    public float log(float x, float b) => Mathf.Log(x) / Mathf.Log(b);
    public float abs(float x) => Mathf.Abs(x);
    public float floor(float x) => Mathf.Floor(x);
    public float ceil(float x) => Mathf.Ceil(x);
    public float round(float x) => Mathf.Round(x);
    // Trig
    public float sin(float x) => Mathf.Sin(x * Mathf.Pi/180);
    public float cos(float x) => Mathf.Cos(x * Mathf.Pi/180);
    public float tan(float x) => Mathf.Tan(x * Mathf.Pi/180);
    public float asin(float x) => 180 / Mathf.Pi * Mathf.Asin(x);
    public float acos(float x) => 180 / Mathf.Pi * Mathf.Acos(x);
    public float atan(float x) => 180 / Mathf.Pi * Mathf.Atan(x);
    public float atan2(float y, float x) => 180 / Mathf.Pi * Mathf.Atan2(y, x);
    // Hyperbolic trig
    public float sinh(float x) => Mathf.Sinh(x);
    public float cosh(float x) => Mathf.Cosh(x);
    public float tanh(float x) => Mathf.Tanh(x);
    public float asinh(float x) => Mathf.Asinh(x);
    public float acosh(float x) => Mathf.Acosh(x);
    public float atanh(float x) => Mathf.Atanh(x);
    // Random functions
    public float random() => rng.Randf();
    public int randInt(int min, int max) => rng.RandiRange(min, max);
    public float normal(float mean, float deviation) => rng.Randfn(mean, deviation);
}

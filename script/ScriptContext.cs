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
        print("Accelerating in the script");
        entity.AddAcceleration(acceleration, angle * Mathf.Pi/180, time);
        scriptVM.timeToPause = true;
    }
    public void spawn()
    {
        
    }
    //Everything here should be in an Emitter class that ScriptContext extends.
    //TODO: Modify this to also show it on the screen.
    public void print(params object[] args)
    {
        GD.Print(string.Concat(args));
    }
    // Math constants
    public float e = (float)Math.E;
    public float pi = (float)Math.PI;
    // Math functions
    public float Sqrt(float x) => (float)Math.Sqrt(x);
    public float Exp(float x) => (float)Math.Exp(x);
    public float Pow(float x, float y) => (float)Math.Pow(x, y);
    public float Log(float x) => (float)Math.Log(x);
    public float Log(float x, float b) => (float)Math.Log(x, b);
    public float Sin(float x) => (float)Math.Sin(x);
    public float Cos(float x) => (float)Math.Cos(x);
    public float Tan(float x) => (float)Math.Tan(x);
    public float Asin(float x) => (float)Math.Asin(x);
    public float Acos(float x) => (float)Math.Acos(x);
    public float Atan(float x) => (float)Math.Atan(x);
    public float Atan2(float y, float x) => (float)Math.Atan2(y, x);
    public float Sinh(float x) => (float)Math.Sinh(x);
    public float Cosh(float x) => (float)Math.Cosh(x);
    public float Tanh(float x) => (float)Math.Tanh(x);
    public float Asinh(float x) => (float)Math.Asinh(x);
    public float Acosh(float x) => (float)Math.Acosh(x);
    public float Atanh(float x) => (float)Math.Atanh(x);
    // Random functions
    public float Random() => rng.Randf();
    public int RandiRange(int min, int max) => rng.RandiRange(min, max);
    public float Normal(float mean, float deviation) => rng.Randfn(mean, deviation);
}

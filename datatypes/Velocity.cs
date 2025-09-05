using Godot;
using System;

public partial class Velocity : Node
{
    public float x;
    public float y;
    public float t;
    
    //Velocity is stored as a vector representing the path through spacetime after one second from its frame of reference.
    //Entering three values will treat it as that vector.
    //Entering two will treat it as x and y components of velocity, and calculate the vector.
    public Velocity(float x, float y) {
		var gamma = 1/Mathf.Sqrt(1-(x*x+y*y));
		this.x = gamma*x;
		this.y = gamma*y;
		this.t = gamma;
    }

    public Velocity(float x, float y, float t) {
		this.x = x;
		this.y = y;
		this.t = t;
    }

    public Velocity(Vector4 v) {
		this.x = v.X;
		this.y = v.Y;
		this.t = v.Z;
    }

    public static Velocity FromRapidity(float x, float y) {
        var magnitude = Mathf.Sqrt(x*x + y*y);
        if(magnitude == 0)
            return new Velocity(0, 0, 1);
        var speed = Mathf.Tanh(magnitude);
        var multiplier = speed/magnitude;
        return new Velocity(x*multiplier, y*multiplier);
    }

    public Vector4 ToVector() {
        return new Vector4(x, y, t, 0);
    }

    public PointOfReference GetLorentz() {
        var k = 1/(t+1);
        var matrix = new Projection(
            new Vector4(1+k*x*x,	k*x*y,		x,	0),
            new Vector4(k*x*y,		1+k*y*y,	y,	0),
            new Vector4(x,			y,			t,	0),
            new Vector4(0,			0,			0,	1)
        );
        var invMatrix = new Projection(
            new Vector4(1+k*x*x,	k*x*y,		-x,	0),
            new Vector4(k*x*y,		1+k*y*y,	-y,	0),
            new Vector4(-x,			-y,			t,	0),
            new Vector4(0,			0,			0,	1)
        );
        return new PointOfReference(matrix, invMatrix);
    }

    public override string ToString() {
        return $"{x}, {y}, {t}";
    }
}
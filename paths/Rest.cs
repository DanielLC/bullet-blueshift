using Godot;
using System;

public partial class Rest : Path
{
	public override PointOfReference PointOfReferenceAtTime(float s) {
		return new Event(0, 0, s).GetTranslation();
	}

	public override Event Event(float s) {
		return new Event(0, 0, s);
	}

	public override (PointOfReference, float) SeenFromRest(Event e) {
		var tt = e.t - Mathf.Sqrt(e.x*e.x + e.y*e.y);
		return (new Event(0, 0, tt).GetTranslation(), tt);
	}
	
	public override Event See(Event e) {
		var tt = e.t + Mathf.Sqrt(e.x*e.x + e.y*e.y);
		return new Event(0, 0, tt);
	}

	//Was this one not used? I'm apparently not overriding anything.
	/*public override PointOfReference GetPointOfReference(Event e) {
		return e.GetTranslation();
	}*/

	public override Event ToLocalSpacetime(Event e) {
		return e;
	}

	public override Velocity GetVelocity(Event e) {
		return new Velocity(0, 0, 1);
	}

	public override float GetAccel() {
		return 0;
	}
}

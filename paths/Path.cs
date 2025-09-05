using Godot;
using System;

public abstract partial class Path : Node
{
	/// <summary>
	/// Returns the point of reference at proper time s from when the path started.
	/// </summary>
	/// <param name="s">Proper time.</param>
	/// <returns>The point of reference at proper time s from when the path started.</returns>
	abstract public PointOfReference PointOfReferenceAtTime(float s);

	/* ## @param s Proper time.
	## @return The event after that much time passed from the reference
	## frame of the observer moving along this path. */
	virtual public Event Event(float s) {
		var por = PointOfReferenceAtTime(s);
		if(por == null) {
			return null;
		} else {
			return por.GetEvent();
		}
	}

	/* ## @param e Event where you look from.
	## @return The point of reference where the path crosses
	## the past lightcone of that event, given in absolute coordinates. */
	abstract public PointOfReference SeenFromRest(Event e);

	/* ## @param e Event to be seen.
	## @return The point where the path crosses the future lightcone of
	## that event, given in absolute coordinates. */
	abstract public Event See(Event e);


	/* ## @param por Point of reference where you look from.
	## @return The point of reference where the path crosses
	## the future lightcone of that event, given relative to
	## the point of reference `por`. */
	public PointOfReference Seen(PointOfReference por) {
		var seen0 = SeenFromRest(por.GetEvent());
		if (seen0 == null)
		{
			return null;
		}
		else
		{
			return por.Inverse() * seen0;
		}
	}

	abstract public Event ToLocalSpacetime(Event e);

	abstract public Velocity GetVelocity(Event e);

	/* ## @param e	Event from the reference frame of the object.
	## @return	Point of reference that represents the rest frame of the event,
	## along with the speed the object is moving.
	## This is the inverse of `to_local_spacetime`, except it returns a PointOfReference. */
	public PointOfReference ToWorldSpacetime(Event e) {
		var wait = PointOfReferenceAtTime(e.t);
		// If this is a Compound, e.t could be before the object spawned or after it despawns, in which case this returns null.
		// But it shouldn't. Maybe some of the Entity is visible.
		if(wait == null) {
			return null;
		}
		var displacement = new Event(e.x, e.y, 0);
		return displacement.GetTranslation() * wait;
	}

	//Not implemented yet.
	//abstract public void AbsoluteTransform();

	//abstract public void UpdateShader(ShaderMaterial shader);

	public virtual float GetAccel() {
		throw new NotImplementedException(
			"'GetAccel()' only implemented in Rest and Hyperbola, which have constant acceleration"
		);
	}
}

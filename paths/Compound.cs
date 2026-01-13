using Godot;
using System;
using System.Collections.Generic;

public partial class Compound : Path
{
	private List<Transform> components;
	//I'm thinking maybe all these lists should be stored inside Transform.
	private List<float> times;
	private List<Event> events;
	private List<Vector4> coevents;
	private List<PointOfReference> endPORs;
	private ShaderMaterial shader;
	private float rotationSpeed;
	public bool dead;

	public Compound(ShaderMaterial shader, PointOfReference start, float rotationSpeed)
	{
		this.shader = shader;
		components = [];
		endPORs = [start];
		times = [0];
		events = [start.GetEvent()];
		coevents = [GetCoevent(start)];
		dead = false;
		this.rotationSpeed = rotationSpeed;
	}

	public Compound(ShaderMaterial shader, float rotationSpeed) : this(shader, PointOfReference.IDENTITY, rotationSpeed) { }

	//TODO: This probably has some off by one errors.
	//And really, it should be looking for just the visible components.
	public void UpdateShader()
	{
		if(components.Count >= 64)
        {
			throw new Exception("Too many components for shader. Fix it so it's not trying to show all these components at once.");
        }
		shader.SetShaderParameter("segment_count", components.Count + 1);
		shader.SetShaderParameter("coevents", coevents.ToArray());
		var pathTransforms = new float[components.Count * 16];
		var pathTransformInverses = new float[components.Count * 16];
		var rotations = new float[components.Count];
		var accels = new float[components.Count];
		for (int i = 0; i < components.Count; ++i)
		{
			var component = components[i];
			component.pathTransform.PackXform(pathTransforms, i);
			component.pathTransform.PackInverse(pathTransformInverses, i);
			rotations[i] = times[i] * rotationSpeed + component.rotation;
			accels[i] = component.GetAccel();
		}
		shader.SetShaderParameter("path_transform", pathTransforms);
		shader.SetShaderParameter("path_transform_inverse", pathTransformInverses);
		shader.SetShaderParameter("rads", rotations);
		shader.SetShaderParameter("accels", accels);
		//DumpShaderParams(shader);
	}

	private void DumpShaderParams()
	{
		GD.Print("Compound.cs: Dumping Shader Params");
		string[] parameters = ["por", "por_inverse", "observer_position", "scale", "size", "segment_count", "coevents", "path_transform", "path_transform_inverse", "sprite_transform", "accels"];
		foreach (var name in parameters)
		{
			var value = shader.GetShaderParameter(name);
			GD.Print($"{name} = {value}");
		}
		GD.Print();
	}
	//TODO: Make Entity call this so it only has to find the component once.
	public int GetComponentIndex(float s)
	{
		if (s < 0)
		{
			//It's before the object existed
			return -1;
		}
		for (int i = 1; i < times.Count; ++i)
		{
			if (s < times[i])
			{
				return i-1;
			}
		}
		//It's after the object disappeared
		return -1;
	}
	public PointOfReference PointOfReferenceAtTime(float s, int i)
	{
		if (i < 0 || i >= components.Count)
			return null;
		return components[i].PointOfReferenceAtTime(s - times[i]);
	}

	public Event Event(float s, int i)
	{
		if (i < 0)
			return null;
		return components[i].Event(s - times[i]);
	}

	public override PointOfReference PointOfReferenceAtTime(float s)
	{
		int i = GetComponentIndex(s);
		return PointOfReferenceAtTime(s, i);
	}

	public override Event Event(float s)
	{
		int i = GetComponentIndex(s);
		return Event(s, i);
	}

	//Checks if every part of the ship at the end of the first component is in the past of e.
	//Automatically removes the first component if it is. And if there's nothing left, tells the Entity to destroy itself.
	public bool CheckIfAllInPast(Event e, float r, ShaderMaterial shader)
	{
		if (components.Count == 0)
		{
			return true;
		}
		var relativeEvent = endPORs[1].Inverse() * e;
		float tMinusR = relativeEvent.t - r;
		//GD.Print(tMinusR);
		//GD.Print(Mathf.Sqrt(relativeEvent.x * relativeEvent.x + relativeEvent.y * relativeEvent.y));
		//GD.Print();
		if (tMinusR > 0 && tMinusR * tMinusR > relativeEvent.x * relativeEvent.x + relativeEvent.y * relativeEvent.y)
		{
			if (components.Count == 0)
			{
				//The Entity parenting this will be removed. No point in doing anything with components etc.
				return true;
			}
			//It's entirely in the past and the first segment can safely be removed.
			components.RemoveAt(0);
			times.RemoveAt(0);
			events.RemoveAt(0);
			coevents.RemoveAt(0);
			endPORs.RemoveAt(0);
			GD.Print("Component removed");
			//I may want to loop instead of having it just end here in case more than one component disappears, but it's probably not worth it.
			UpdateShader();
			return false;
		}
		return false;
	}

	public override PointOfReference SeenFromRest(Event e)
	{
		if (components.Count == 0)
			return null;
		//If the object hasn't been created yet, there's nothing to draw.
		if (events[0] > e)
		{
			// TODO: I should probably make this use a Rest if it's still close enough that part of it is visible.
			return components[0].SeenFromRest(e);
			// return null
		}
		//Otherwise, go through paths one by one and find the one that sees you.
		//In the shader, this should be a binary search, and only look at the components that it could reasonably be.
		//But here, if I'm building the path all the way from the player's past light cone to future one, this is generally going to be at the beginning.
		//Assuming I start erasing them if they're no longer visible. I suppose I should probably do that here?
		for (int i = 1; i < components.Count; ++i)
		{
			if (events[i] > e)
			{
				return components[i - 1].SeenFromRest(e);
			}
		}
		return components[^1].SeenFromRest(e);
	}

	public override Event See(Event e)
	{
		if (components.Count == 0)
			return null;
		if (events[0] > e)
			return null;
		for (int i = 1; i < components.Count; ++i)
			if (events[i] > e)
				return components[i - 1].See(e);
		return components[^1].See(e);
	}

	public float GetT(int i, Event e)
	{
		return e * coevents[i];
	}
	//Gets the component that sees e as the present

	public int GetComponentIndex(Event e)
	{
		if (GetT(0, e) < 0)
			return -1;
		for (int i = 1; i < coevents.Count; ++i)
			if (GetT(0, e) < 0)
				return i;
		return -1;
	}

	public override Event ToLocalSpacetime(Event e)
	{
		int i = GetComponentIndex(e);
		if (i < 0)
			return null;
		Event lateral = components[i - 1].ToLocalSpacetime(e);
		return new Event(lateral.x, lateral.y, lateral.t + times[i - 1]);
	}

	public override Velocity GetVelocity(Event e)
	{
		int i = GetComponentIndex(e);
		if (i < 0)
			return null;
		return components[i - 1].GetVelocity(e);
	}

	public static Vector4 GetCoevent(PointOfReference m)
	{
		return new Vector4(m.inverse.X.Z, m.inverse.Y.Z, m.inverse.Z.Z, m.inverse.W.Z);
	}

	public Event AddAcceleration(float accel, float radians, float time)
	{
		Path path;
		PointOfReference translate;
		if (accel == 0)
		{
			path = new Rest();
			translate = PointOfReference.IDENTITY;
		}
		else
		{
			path = new Hyperbola(accel);
			translate = new Event(0, -1 / accel, 0).GetTranslation();
		}
		var rotate = PointOfReference.FromRotation(radians);
		var transform = new Transform(path, endPORs[^1] * rotate * translate, radians);
		PointOfReference end = transform.PointOfReferenceAtTime(time);
		components.Add(transform);
		times.Add(time + times[^1]);
		events.Add(end.GetEvent());
		coevents.Add(GetCoevent(end));
		endPORs.Add(end);
		UpdateShader();
		return events[^1];
	}

	public void Extend(float time)
	{
		if (dead)
			return;
		if (components.Count == 0)
		{
			components.Add(new Transform(new Rest(), endPORs[0]));
			times.Add(time);
			var newEvent = new Event(0, 0, time);
			events[0] = newEvent;
			endPORs[0] = newEvent.GetTranslation();
			coevents[0] = GetCoevent(endPORs[0]);
		}
		else
		{
			times[^1] += time;
			endPORs[^1] = components[^1].PointOfReferenceAtTime(times[^1]);
			events[^1] = endPORs[^1].GetEvent();
			coevents[^1] = GetCoevent(endPORs[^1]);
		}
	}

	public void Die(Event e)
	{
		var v = GetVelocity(e);
		endPORs[^1] = new PointOfReference(e, v);
		events[^1] = endPORs[^1].GetEvent();
		coevents[^1] = GetCoevent(endPORs[^1]);
		dead = true;
	}

	public Event GetEnd()
	{
		return events[^1];
	}

	public PointOfReference GetEndPOR()
	{
		return endPORs[^1];
	}
	public override string ToString()
	{
		return "Compound";
	}
}
/*

	#public void absoluteTransform(PointOfReference transformation) {
		#if(paths.size() == 0) {
			#end = end.times(transformation);
			#return;
		#}
		#
		#for(int i=0; i<paths.size(); ++i) {
			#paths.set(i,new Transform(paths.get(i), transformation));
		#}
		#end = transformation.times(end);
		#Matrix m = transformation.getMatrix().inverse();
		#for(int i=0; i<coevents.size(); ++i) {
			#coevents.set(i,coevents.get(i).times(m));
		#}
	#}

#I don't think I need this. If I bring it back, I need _events.
#func transform(transformation: PointOfReference) -> void:
	#if len(_components) == 0:
		#end = end.times(transformation)
		#_coevents[0] = end._inverse.z
		#return
	#var initial: PointOfReference = _components[0].pathTransforms
	#var change := initial.times(transformation.times(initial.inverse()))
	#for i in range(len(_components)):
		#_components[i] = Transform.new(_components[i], change)
	#end = change.times(end)
	#var m := change._inverse
	#for i in range(len(_coevents)):
		#_coevents[i] = _coevents[i].times(m)
*/
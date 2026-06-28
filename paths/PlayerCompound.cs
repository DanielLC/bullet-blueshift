using Godot;
using System;

public partial class PlayerCompound(ShaderMaterial shader, PointOfReference start, float rotationSpeed) : Compound(shader, start, rotationSpeed)
{
    private int invisibleComponents;
    public override void UpdateShader()
	{
		int visibleComponents = components.Count - invisibleComponents;
		if (visibleComponents >= 64)
		{
			throw new Exception("Too many components for shader. Fix it so it's not trying to show all these components at once.");
		}
		shader.SetShaderParameter("segment_count", visibleComponents + 1);
		var visibleCoevents = new Vector4[coevents.Count - invisibleComponents];
		coevents.CopyTo(invisibleComponents, visibleCoevents, 0, visibleCoevents.Length);
		shader.SetShaderParameter("coevents", visibleCoevents);
		var pathTransforms = new float[visibleComponents * 16];
		var pathTransformInverses = new float[visibleComponents * 16];
		var rotations = new float[visibleComponents];
		var accels = new float[visibleComponents];
		for (int i = 0; i < visibleComponents; ++i)
		{
			var component = components[i + invisibleComponents];
			component.pathTransform.PackXform(pathTransforms, i);
			component.pathTransform.PackInverse(pathTransformInverses, i);
			rotations[i] = times[i + invisibleComponents] * rotationSpeed + component.rotation;
			accels[i] = component.GetAccel();
		}
		shader.SetShaderParameter("path_transform", pathTransforms);
		shader.SetShaderParameter("path_transform_inverse", pathTransformInverses);
		shader.SetShaderParameter("rads", rotations);
		shader.SetShaderParameter("accels", accels);
		//DumpShaderParams(shader);
	}

    // Make sure if it only removes it if it's sufficiently far in the past.
    public override bool CheckIfAllInPast(Event e, float r)
	{
		if (components.Count == 0)
		{
			return true;
		}

		// First, remove any far enough into the past that they can be removed completely.
		float invisiblePastTime = 1f;
		var relativeEvent = endPORs[1].Inverse() * e;
		float tMinusR = relativeEvent.t - r - invisiblePastTime;
		while (tMinusR > 0 && tMinusR * tMinusR > relativeEvent.x * relativeEvent.x + relativeEvent.y * relativeEvent.y)
		{
			if (components.Count == 1)
			{
				// The Entity parenting this will be removed. No point in doing anything with components etc.
				return true;
			}
			// It's entirely in the past and the first segment can safely be removed.
			components.RemoveAt(0);
			times.RemoveAt(0);
			events.RemoveAt(0);
			coevents.RemoveAt(0);
			endPORs.RemoveAt(0);
			--invisibleComponents;
			
			relativeEvent = endPORs[1].Inverse() * e;
			tMinusR = relativeEvent.t - r - invisiblePastTime;
		}

		// Next, check if any are no longer visible.
		relativeEvent = endPORs[invisibleComponents + 1].Inverse() * e;
		tMinusR = relativeEvent.t - r;
		while (tMinusR > 0 && tMinusR * tMinusR > relativeEvent.x * relativeEvent.x + relativeEvent.y * relativeEvent.y)
		{
			// Make another component invisible.
			++invisibleComponents;
			
			relativeEvent = endPORs[invisibleComponents + 1].Inverse() * e;
			tMinusR = relativeEvent.t - r;
		}
		return false;
	}

}

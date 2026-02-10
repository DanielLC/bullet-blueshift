using Godot;
using System;

public class PointOfReference
{
    // Note about Projection: the Vectors used to make it are column vectors,
    // meaning the matrices are all transposed relative to how they're actually being written.
    // It also automatically transposes vectors during multiplication, so v*M is not the same as M*v.
    // It's the same as v^T*M.
    public Projection xform;
    public Projection inverse;
    // Should be a constant, but it's apparently not a constant expression. Just don't change it.
    public static PointOfReference IDENTITY;
    //const float[] PACKED_NAN = {NAN,NAN,NAN,NAN,NAN,NAN,NAN,NAN,NAN,NAN,NAN,NAN,NAN,NAN,NAN,NAN};
    static PointOfReference()
    {
        Projection identity = new(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(0, 0, 0, 1)
        );
        IDENTITY = new PointOfReference(identity, identity);
    }

    public PointOfReference(Projection xform, Projection inverse)
    {
        this.xform = xform;
        this.inverse = inverse;
    }

    // This could probably be optimized by having it build the matrix itself.
    // TODO: And definitely don't just make it create a PointOfReference and then copy it to itself.
    public PointOfReference(Event e, Velocity v) {
        var m = e.GetTranslation() * v.GetLorentz();
        xform = m.xform;
        inverse = m.inverse;
    }

    public Velocity GetVelocity() {
        return new Velocity(xform.Z);
    }

    public Event GetEvent() {
        return new Event(xform.W);
    }
    /*
    func wait(t: float) -> PointOfReference:
	var matrix := Projection(
		_xform[0],
		_xform[1],
		_xform[2],
		_xform[3] + t*_xform.z
	)
	var invMatrix := Projection(
		_xform[0],
		_xform[1],
		_xform[2],
		_xform[3] - t*_xform.z
	)
	return new(matrix, invMatrix)
    */

    public static PointOfReference operator*(PointOfReference a, PointOfReference b) {
        return new PointOfReference(a.xform * b.xform, b.inverse * a.inverse);
    }

    public static Event operator*(PointOfReference por, Event e) {
        return new Event(por.xform * e.ToVector());
    }

    public static Velocity operator*(PointOfReference por, Velocity v) {
        return new Velocity(por.xform * v.ToVector());
    }

    /*
    func accelerate(v: Velocity) -> PointOfReference:
	return self.times(v.get_lorentz())
    */

    public static PointOfReference FromRotation(float radians) {
        var c = Mathf.Cos(radians);
        var s = Mathf.Sin(radians);
        var matrix = new Projection(
            new Vector4(c,	s,	0,	0),
            new Vector4(-s,	c,	0,	0),
            new Vector4(0,	0,	1,	0),
            new Vector4(0,	0,	0,	1)
        );
        var inv_matrix = new Projection(
            new Vector4(c,	-s,	0,	0),
            new Vector4(s,	c,	0,	0),
            new Vector4(0,	0,	1,	0),
            new Vector4(0,	0,	0,	1)
        );
        return new PointOfReference(matrix, inv_matrix);
    }
    /*
	#public static PointOfReference getXMirror() {
		#Jama.Matrix matrix = new Jama.Matrix(new double[][] {
				#{-1,0,	0,	0},
				#{0,	1,	0,	0},
				#{0,	0,	1,	0},
				#{0,	0,	0,	1}
		#});
		#return new PointOfReference(matrix);
	#}
	#
	#public static PointOfReference getYMirror() {
		#Jama.Matrix matrix = new Jama.Matrix(new double[][] {
				#{1,	0,	0,	0},
				#{0,	-1,	0,	0},
				#{0,	0,	1,	0},
				#{0,	0,	0,	1}
		#});
		#return new PointOfReference(matrix);
	#}
	#
	#public Jama.Matrix getMatrix() {
		#return matrix;
	#}
	#
    */

    public override string ToString() {
        return $"{xform.X}\n{xform.Y}\n{xform.Z}\n{xform.W}\n";
    }

    public float TestInverse() {
        Projection shouldBeIdentity = xform * inverse;
        return
            shouldBeIdentity.X.DistanceSquaredTo(new Vector4(1,0,0,0)) +
            shouldBeIdentity.Y.DistanceSquaredTo(new Vector4(0,1,0,0)) +
            shouldBeIdentity.Z.DistanceSquaredTo(new Vector4(0,0,1,0)) +
            shouldBeIdentity.W.DistanceSquaredTo(new Vector4(0,0,0,1));

    }

    public void RecalculateInverse() {
        inverse = xform.Inverse();
    }

    public PointOfReference Inverse() {
        return new PointOfReference(inverse, xform);
    }

    private void PackArray(Projection m, float[] array, int i) {
        i *= 16;
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                array[i++] = m[row][col];
            }
        }
    }

    public void PackXform(float[] array, int i) {
        PackArray(xform, array, i);
    }

    public void PackInverse(float[] array, int i) {
        PackArray(inverse, array, i);
    }

    public void PackNaN(float[] array, int i) {
        for(int j = 0; j < 16; ++j) {
            array[j] = float.NaN;
        }
    }
}
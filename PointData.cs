using MathNet.Numerics.LinearAlgebra;

namespace HelmertLSA
{
    public class Point3D
    {
        public string Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public Vector<float> Vector => Vector<float>.Build.Dense(new float[] { X, Y, Z });

        // Constructor to initialize all properties
        public Point3D(string id, float x, float y, float z)
        {
            Id = id;
            X = x;
            Y = y;
            Z = z;
        }

        // Constructor to initialize from a vector
        public Point3D(string id, Vector<float> v)
        {
            Id = id;
            X = v[0];
            Y = v[1];
            Z = v[2];
        }
    }
}
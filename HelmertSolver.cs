using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HelmertLSA
{
    public class HelmertSolver
    {
        public class Result
        {
            public float Scale { get; set; }
            public Matrix<float> Rotation { get; set; }
            public Vector<float> Translation { get; set; }

            public Result()
            {
                Scale = 1.0f;
                Rotation = Matrix<float>.Build.DenseIdentity(3);
                Translation = Vector<float>.Build.Dense(3);
            }

            public Vector<float> Transform(Vector<float> point)
            {
                return Scale * Rotation * point + Translation;
            }
        }

        public Result Solve(List<Vector<float>> source, List<Vector<float>> target)
        {
            if (source == null || target == null)
                throw new ArgumentNullException("Source and target point lists cannot be null.");

            if (source.Count != target.Count)
                throw new ArgumentException("Source and target point lists must have the same number of elements.");

            int n = source.Count;

            // Compute centroids
            var centroidSource = ComputeCentroid(source);
            var centroidTarget = ComputeCentroid(target);

            // Center the points
            var centeredSource = source.Select(p => p - centroidSource).ToList();
            var centeredTarget = target.Select(p => p - centroidTarget).ToList();

            // Compute covariance matrix H
            var H = Matrix<float>.Build.Dense(3, 3);
            for (int i = 0; i < n; i++)
            {
                H += centeredSource[i].ToColumnMatrix() * centeredTarget[i].ToRowMatrix();
            }

            // Perform Singular Value Decomposition (SVD)
            var svd = H.Svd();
            var U = svd.U;
            var VT = svd.VT;
            var R = VT.Transpose() * U.Transpose();

            // Ensure a proper rotation (determinant = 1)
            if (R.Determinant() < 0)
            {
                var diag = Matrix<float>.Build.DenseIdentity(3);
                diag[2, 2] = -1;
                R = VT.Transpose() * diag * U.Transpose();
            }

            // Compute scale
            float scaleNumerator = 0;
            float scaleDenominator = 0;
            for (int i = 0; i < n; i++)
            {
                var rotated = R * centeredSource[i];
                scaleNumerator += rotated.DotProduct(centeredTarget[i]);
                scaleDenominator += centeredSource[i].DotProduct(centeredSource[i]);
            }

            float scale = scaleNumerator / scaleDenominator;

            // Compute translation
            var translation = centroidTarget - scale * R * centroidSource;

            return new Result
            {
                Scale = scale,
                Rotation = R,
                Translation = translation
            };
        }

        private Vector<float> ComputeCentroid(List<Vector<float>> points)
        {
            if (points == null || points.Count == 0)
                throw new ArgumentException("Point list cannot be null or empty.");

            var sum = Vector<float>.Build.Dense(points[0].Count);
            foreach (var point in points)
            {
                sum += point;
            }

            return sum / points.Count;
        }

        public List<Vector<float>> ComputeResiduals(List<Vector<float>> source, List<Vector<float>> target, Result transformation)
        {
            if (source == null || target == null)
                throw new ArgumentNullException("Source and target point lists cannot be null.");

            if (source.Count != target.Count)
                throw new ArgumentException("Source and target point lists must have the same number of elements.");

            var residuals = new List<Vector<float>>();

            for (int i = 0; i < source.Count; i++)
            {
                var transformedPoint = transformation.Transform(source[i]);
                var residual = target[i] - transformedPoint;
                residuals.Add(residual);
            }

            return residuals;
        }
    }
}
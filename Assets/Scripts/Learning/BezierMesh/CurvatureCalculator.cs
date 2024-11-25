using UnityEngine;
using System.Numerics;

namespace BezierMesh
{
    public struct Matrix2x2
    {
        public float M11, M12, M21, M22;

        public Matrix2x2(float m11, float m12, float m21, float m22)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M21 = m21;
            this.M22 = m22;
        }
    }

    public class CurvatureCalculator
    {
        public static (float[,], float[,]) ComputeGradient(float[,] Z, float dx, float dy)
        {
            int width = Z.GetLength(0);
            int height = Z.GetLength(1);
            float[,] dZdx = new float[width, height];
            float[,] dZdy = new float[width, height];

            for (int i = 1; i < width - 1; i++)
            {
                for (int j = 1; j < height - 1; j++)
                {
                    dZdx[i, j] = (Z[i + 1, j] - Z[i - 1, j]) / (2 * dx);
                    dZdy[i, j] = (Z[i, j + 1] - Z[i, j - 1]) / (2 * dy);
                }
            }

            return (dZdx, dZdy);
        }

        public static (float[,], float[,], float[,]) ComputeHessian(float[,] Z, float dx, float dy)
        {
            int width = Z.GetLength(0);
            int height = Z.GetLength(1);
            float[,] d2Zdx2 = new float[width, height];
            float[,] d2Zdy2 = new float[width, height];
            float[,] d2Zdxdy = new float[width, height];

            for (int i = 1; i < width - 1; i++)
            {
                for (int j = 1; j < height - 1; j++)
                {
                    d2Zdx2[i, j] = (Z[i + 1, j] - 2 * Z[i, j] + Z[i - 1, j]) / (dx * dx);
                    d2Zdy2[i, j] = (Z[i, j + 1] - 2 * Z[i, j] + Z[i, j - 1]) / (dy * dy);
                    d2Zdxdy[i, j] = (Z[i + 1, j + 1] - Z[i - 1, j + 1] - Z[i + 1, j - 1] + Z[i - 1, j - 1]) / (4 * dx * dy);
                }
            }

            return (d2Zdx2, d2Zdy2, d2Zdxdy);
        }

        public static (float[,], float[,]) Curvature(float[,] Z, float dx, float dy)
        {
            var (d2Zdx2, d2Zdy2, d2Zdxdy) = ComputeHessian(Z, dx, dy);
            int width = Z.GetLength(0);
            int height = Z.GetLength(1);

            float[,] GaussianCurvature = new float[width, height];
            float[,] MeanCurvature = new float[width, height];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Matrix2x2 hessian = new Matrix2x2(d2Zdx2[i, j], d2Zdxdy[i, j], d2Zdxdy[i, j], d2Zdy2[i, j]);
                    var eigenvalues = Eigenvalues2x2(hessian);

                    GaussianCurvature[i, j] = eigenvalues.Item1 * eigenvalues.Item2;
                    MeanCurvature[i, j] = 0.5f * (eigenvalues.Item1 + eigenvalues.Item2);
                }
            }

            return (GaussianCurvature, MeanCurvature);
        }

        private static (float, float) Eigenvalues2x2(Matrix2x2 matrix)
        {
            float trace = matrix.M11 + matrix.M22;
            float determinant = matrix.M11 * matrix.M22 - matrix.M21 * matrix.M12;
            float discriminant = trace * trace - 4 * determinant;

            if (discriminant < 0)
            {
                // Return some default values or handle the case as appropriate.
                // For this example, I'm returning (0, 0) when the discriminant is negative.
                return (0, 0);
            }

            float root = Mathf.Sqrt(discriminant);

            float eigenvalue1 = 0.5f * (trace + root);
            float eigenvalue2 = 0.5f * (trace - root);

            return (eigenvalue1, eigenvalue2);
        }
    }
}

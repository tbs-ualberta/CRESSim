using System;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;


namespace BezierMesh
{
    public class BezierMeshGeneratorWithCurvatureLimit
    {
        private int u_cells;
        private int w_cells;
        private float x_start;
        private float x_end;
        private float y_start;
        private float y_end;
        private int u_ctrl_pts;
        private int w_ctrl_pts;
        private float max_deformation;
        private bool random_ctrl_pts;
        private float curv_treshold;

        public BezierMeshGeneratorWithCurvatureLimit(int u_cells = 12, int w_cells = 10, float x_start = -2, float x_end = 2, float y_start = -2, float y_end = 2, int u_ctrl_pts = 3, int w_ctrl_pts = 3, float max_deformation = 2, bool random_ctrl_pts = false, float curv_treshold = 0.6f)
        {
            this.u_cells = u_cells + 1;
            this.w_cells = w_cells + 1;
            this.x_start = x_start;
            this.x_end = x_end;
            this.y_start = y_start;
            this.y_end = y_end;
            this.u_ctrl_pts = u_ctrl_pts;
            this.w_ctrl_pts = w_ctrl_pts;
            this.max_deformation = max_deformation;
            this.random_ctrl_pts = random_ctrl_pts;
            this.curv_treshold = curv_treshold;

            // Add assertion on u_ctrl_pts, w_ctrl_pts > 3
        }

        private float[,] RandCtrlPts(int low, int high)
        {
            float[,] pnts = new float[u_ctrl_pts, u_ctrl_pts];
            for (int i = 0; i < u_ctrl_pts; i++)
            {
                for (int j = 0; j < u_ctrl_pts; j++)
                {
                    pnts[i, j] = UnityEngine.Random.Range(low, high) / 2.0f;
                }
            }
            return pnts;
        }

        public (float[,], float[,], float[,], float[], float[]) CtrlPts()
        {
            float[] a = Linspace(x_start, x_end, u_ctrl_pts);
            float[] b = Linspace(y_start, y_end, w_ctrl_pts);
            float[,] x = new float[u_ctrl_pts, w_ctrl_pts];
            float[,] y = new float[u_ctrl_pts, w_ctrl_pts];
            float[,] z = new float[u_ctrl_pts, u_ctrl_pts];

            if (random_ctrl_pts)
            {
                // Set random seed if necessary
                for (int i = 0; i < u_ctrl_pts; i++)
                {
                    for (int j = 0; j < w_ctrl_pts; j++)
                    {
                        x[i, j] = a[i];
                        y[i, j] = b[j];
                        if (i > 0 && i < u_ctrl_pts - 1 && j > 0 && j < w_ctrl_pts - 1)
                        {
                            z[i, j] = UnityEngine.Random.Range(-1f, 0.5f);
                        }
                    }
                }
            }
            else
            {
                // The rest of the logic for non-random control points...
                // This part needs further implementation based on the original logic.
            }

            return (x, y, z, a, b);
        }

        private int Binomial(int n, int i)
        {
            return Factorial(n) / (Factorial(i) * Factorial(n - i));
        }

        private int Factorial(int number)
        {
            int result = 1;
            for (int i = 1; i <= number; i++)
            {
                result *= i;
            }
            return result;
        }

        private float Bernstein(int n, int i, float direction)
        {
            return Binomial(n, i) * Mathf.Pow(direction, i) * Mathf.Pow(1 - direction, n - i);
        }

        // Stub for curvature function. This needs actual implementation.
        private (float[,], float[,]) CalculateCurvature(float[,] Z)
        {
            float[,] G = new float[Z.GetLength(0), Z.GetLength(1)];
            float[,] H = new float[Z.GetLength(0), Z.GetLength(1)];
            var (_, _, _, a, b) = CtrlPts();
            float da = a[1] - a[0];
            float db = b[1] - b[0];
            (G, H) = CurvatureCalculator.Curvature(Z, da, db);
            return (G, H);
        }

        public (float[,], float[,], float[,]) GenerateGrid()
        {
            float curv = 100.0f;
            float[,] X = new float[this.u_cells, this.w_cells];
            float[,] Y = new float[this.u_cells, this.w_cells];
            float[,] Z = new float[this.u_cells, this.w_cells];

            while (curv > curv_treshold)
            {
                var (x, y, z, _, _) = CtrlPts();
                int u_ctrl_pts = x.GetLength(0);
                int w_ctrl_pts = x.GetLength(1);
                int n = u_ctrl_pts - 1;
                int m = w_ctrl_pts - 1;
                float[] U = Linspace(0, 1, this.u_cells);
                float[] W = Linspace(0, 1, this.w_cells);
                Array.Clear(X, 0, X.Length);
                Array.Clear(Y, 0, Y.Length);
                Array.Clear(Z, 0, Z.Length);

                for (int u_index = 0; u_index < u_ctrl_pts; u_index++)
                {
                    for (int w_index = 0; w_index < w_ctrl_pts; w_index++)
                    {
                        for (int k = 0; k < this.u_cells; k++)
                        {
                            for (int l = 0; l < this.w_cells; l++)
                            {
                                float u_basis = Bernstein(n, u_index, U[k]);
                                float w_basis = Bernstein(m, w_index, W[l]);

                                X[k, l] += u_basis * w_basis * x[u_index, w_index];
                                Y[k, l] += u_basis * w_basis * y[u_index, w_index];
                                Z[k, l] += u_basis * w_basis * z[u_index, w_index];
                            }
                        }
                    }
                }

                float maxAbsZ = float.MinValue;
                for (int i = 0; i < u_cells; i++)
                {
                    for (int j = 0; j < w_cells; j++)
                    {
                        maxAbsZ = Mathf.Max(maxAbsZ, Mathf.Abs(Z[i, j]));
                    }
                }

                for (int i = 1; i < u_cells - 1; i++)
                {
                    for (int j = 1; j < w_cells - 1; j++)
                    {
                        Z[i, j] = max_deformation * Z[i, j] / maxAbsZ;
                    }
                }

                var (Gauss_curv, _) = CalculateCurvature(Z);
                curv = Mathf.Max(Gauss_curv.Cast<float>().Max(), Mathf.Abs(Gauss_curv.Cast<float>().Min()));

                //if (float.IsNaN(curv))
                //{
                //    // if NaN continue the loop
                //    continue;
                //}

                if (ContainsNaN(Z))
                {
                    // if NaN continue the loop
                    curv = 100;
                    continue;
                }

            }
            if (ContainsNaN(Z))
            {
                Debug.LogError("NaN found in Z before returning from GenerateGrid.");
            }
            return (X, Y, Z);
        }


        // Additional utility function
        private float[] Linspace(float start, float end, int num)
        {
            float[] result = new float[num];
            for (int i = 0; i < num; i++)
            {
                result[i] = Mathf.Lerp(start, end, (float)i / (num - 1));
            }
            return result;
        }

        bool ContainsNaN(float[,] arr)
        {
            for (int i = 0; i < arr.GetLength(0); i++)
            {
                for (int j = 0; j < arr.GetLength(1); j++)
                {
                    if (float.IsNaN(arr[i, j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static Mesh CreateMeshFromGrid(float[,] x, float[,] y, float[,] z)
        {
            if (x.GetLength(0) != z.GetLength(0) || x.GetLength(1) != z.GetLength(1) || y.GetLength(0) != z.GetLength(0) || y.GetLength(1) != z.GetLength(1))
            {
                throw new Exception("Unable to resolve x and y variables");
            }

            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[z.GetLength(0) * z.GetLength(1)];
            int[] triangles = new int[(z.GetLength(0) - 1) * (z.GetLength(1) - 1) * 6];

            int vertIndex = 0;
            int triIndex = 0;

            for (int i = 0; i < z.GetLength(0); i++)
            {
                for (int j = 0; j < z.GetLength(1); j++)
                {
                    vertices[vertIndex] = new Vector3(x[i, j], y[i, j], z[i, j]);
                    vertIndex++;

                    if (i < z.GetLength(0) - 1 && j < z.GetLength(1) - 1)
                    {
                        triangles[triIndex] = i * z.GetLength(1) + j;
                        triangles[triIndex + 1] = (i + 1) * z.GetLength(1) + j + 1;
                        triangles[triIndex + 2] = i * z.GetLength(1) + j + 1;
                        triangles[triIndex + 3] = (i + 1) * z.GetLength(1) + j + 1;
                        triangles[triIndex + 4] = i * z.GetLength(1) + j;
                        triangles[triIndex + 5] = (i + 1) * z.GetLength(1) + j;
                        triIndex += 6;
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
using BezierMesh;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
using UnityEngine;

[CreateAssetMenu(fileName = "TissueMeshGenerator", menuName = "Learning/Blood Suction/Tissue Mesh Generator", order = 1)]
public class TissueMeshGenerator : ScriptableObject
{
    public int NumControlPoints
    {
        get { return m_numControlPts; }
        set { m_numControlPts = value; }
    }

    public Mesh GenerateMesh()
    {
        Mesh copiedMesh = new Mesh()
        {
            name = m_referenceMesh.name + " Copy",
            vertices = m_referenceMesh.vertices,
            triangles = m_referenceMesh.triangles,
            normals = m_referenceMesh.normals,
            tangents = m_referenceMesh.tangents,
            bounds = m_referenceMesh.bounds,
            uv = m_referenceMesh.uv
        };

        // Generate Bezier surface mesh
        BezierMeshGeneratorWithCurvatureLimit bezierMeshGenerator = new BezierMeshGeneratorWithCurvatureLimit(
            u_cells: 25,
            w_cells: 25,
            x_start: copiedMesh.bounds.min.x,
            x_end: copiedMesh.bounds.max.x,
            y_start: copiedMesh.bounds.min.z,
            y_end: copiedMesh.bounds.max.z,
            u_ctrl_pts: m_numControlPts,
            w_ctrl_pts: m_numControlPts,
            max_deformation: 0.5f,
            random_ctrl_pts: true,
            curv_treshold: 5
        );

        float[,] X, Y, Z;
        (X, Y, Z) = bezierMeshGenerator.GenerateGrid();

        int[] surfaceIndices = FindMeshTopSurfaceIndices(copiedMesh);
        int[] surfaceInnerIndices = FindMeshTopSurfaceInnerIndices(copiedMesh, surfaceIndices);

        // Modify the copied mesh surface
        float minX = X.Cast<float>().Min();
        float maxX = X.Cast<float>().Max();
        float minY = Y.Cast<float>().Min();
        float maxY = Y.Cast<float>().Max();

        float rangeX = maxX - minX;
        float rangeY = maxY - minY;

        int sizeX = X.GetLength(0);
        int sizeY = Y.GetLength(1);

        Vector3[] vertices = copiedMesh.vertices;
        foreach (int idx in surfaceInnerIndices)
        {
            int indexX = (int)((vertices[idx].x - minX) / rangeX * (sizeX - 1));
            int indexY = (int)((vertices[idx].z - minY) / rangeY * (sizeY - 1));

            // safety guarantee
            indexX = Mathf.Max(0, Mathf.Min(indexX, sizeX - 1));
            indexY = Mathf.Max(0, Mathf.Min(indexY, sizeY - 1));

            vertices[idx].y += Z[indexX, indexY];
        }
        copiedMesh.SetVertices(vertices);
        copiedMesh.RecalculateBounds();
        copiedMesh.RecalculateNormals();
        copiedMesh.RecalculateTangents();

        return copiedMesh;
    }

    static private int[] FindMeshTopSurfaceIndices(Mesh mesh)
    {
        List<int> indices = new List<int>();
        Vector3[] vertices = mesh.vertices;
        float MaxHeightY = -float.MaxValue;
        float eps = 1e-6f;
        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].y > MaxHeightY + eps)
            {
                MaxHeightY = vertices[i].y;
                indices.Clear();
                indices.Add(i);
            }
            else if (Mathf.Abs(vertices[i].y - MaxHeightY) < eps)
            {
                indices.Add(i);
            }
        }
        return indices.ToArray();
    }


    static private int[] FindMeshTopSurfaceInnerIndices(Mesh mesh, int[] surfaceIndices)
    {
        List<int> indices = new List<int>();
        Vector3[] vertices = mesh.vertices;
        float eps = 1e-6f;
        Bounds newBounds = new Bounds();
        Vector3 newMinBound = new Vector3()
        {
            x = mesh.bounds.min.x + eps,
            y = mesh.bounds.max.y,
            z = mesh.bounds.min.z + eps,
        };
        Vector3 newMaxBound = new Vector3()
        {
            x = mesh.bounds.max.x - eps,
            y = mesh.bounds.max.y,
            z = mesh.bounds.max.z - eps,
        };
        newBounds.SetMinMax(newMinBound, newMaxBound);

        foreach (int i in surfaceIndices)
        {
            if (newBounds.Contains(vertices[i]))
            {
                indices.Add(i);
            }
        }
        return indices.ToArray();
    }

    [SerializeField]
    private Mesh m_referenceMesh;
    [SerializeField]
    private int m_numControlPts;
}

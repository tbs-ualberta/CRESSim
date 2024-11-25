using System;
using UnityEngine;
using UnityEngine.UI;

public class MapMeshProcessor : MonoBehaviour
{
    #region Properties
    public float[] DiscretizedMapBuffer
    {
        get { return m_discretizedMapBuffer; }
    }

    public float[] DiscretizedMapFineBuffer
    {
        get { return m_discretizedMapFineBuffer; }
    }

    public int GridSize
    {
        get { return m_gridSize; }
        set { m_gridSize = value; }
    }

    public Vector3 MaxHeightLocation
    {
        get { return new Vector3(m_maxHeightMeshLocation[0] * m_scale.x, m_maxHeight * m_scale.z, -m_maxHeightMeshLocation[1] * m_scale.y); }
    }

    #endregion

    #region Messages

    void Start()
    {
        m_scale = transform.localScale;
        m_discretizedMapBuffer = new float[m_gridSize * m_gridSize];
        UpdateMesh();
        UpdateMapGrid();
    }

    #endregion

    #region Methods

    public void UpdateMesh()
    {
        m_currentMesh = GetComponent<MeshFilter>().mesh;
    }

    public void UpdateMesh(Mesh mesh)
    {
        m_currentMesh = mesh;
    }

    /// <summary>
    /// Update the discretized map buffers
    /// </summary>
    public void UpdateMapGrid()
    {
        m_maxHeight = -10;
        int[] triangles = m_currentMesh.triangles;
        int pixelSize = m_fineGridSize + 1;
        if (m_discretizedMapFineBuffer is null)
        {
            m_discretizedMapFineBuffer = new float[pixelSize * pixelSize];
        }
        // Loop over the triangles
        for (int t = 0; t < triangles.Length / 3; t++)
        {
            Vector3 a = m_currentMesh.vertices[m_currentMesh.triangles[t * 3]];
            Vector3 b = m_currentMesh.vertices[m_currentMesh.triangles[t * 3 + 1]];
            Vector3 c = m_currentMesh.vertices[m_currentMesh.triangles[t * 3 + 2]];
            // assume that X-Y plane is the map region and Z is the height
            float[] p1 = MapMeshPointToGrid(a, m_fineGridSize);
            float[] p2 = MapMeshPointToGrid(b, m_fineGridSize);
            float[] p3 = MapMeshPointToGrid(c, m_fineGridSize);

            // no negative values. int is same as floor
            int x_min = (int)Mathf.Min(p1[0], p2[0], p3[0]);
            int x_max = (int)Mathf.Ceil(Mathf.Max(p1[0], p2[0], p3[0]));
            int y_min = (int)Mathf.Min(p1[1], p2[1], p3[1]);
            int y_max = (int)Mathf.Ceil(Mathf.Max(p1[1], p2[1], p3[1]));
            if (x_max > m_fineGridSize) x_max = m_fineGridSize;
            if (y_max > m_fineGridSize) y_max = m_fineGridSize;

            for (int i = x_min; i <= x_max; i++)
            {
                for (int j = y_min; j <= y_max; j++)
                {
                    float[] gridPoint = new float[2] { i, j };
                    if (WithinTriangle(gridPoint, p1, p2, p3))
                    {
                        // Unmap grid point (int) to the original mesh coordinate
                        float[] meshPoint;
                        meshPoint = UnmapGridLocationToMeshXY(gridPoint);
                        float height = PlaneInterpolation(a, b, c, meshPoint[0], meshPoint[1]);
                        m_discretizedMapFineBuffer[i + j * pixelSize] = height;
                        if (height > m_maxHeight)
                        {
                            m_maxHeight = height;
                            m_maxHeightMeshLocation = meshPoint;
                        }
                    }
                }
            }
        }
        UpdateDownsampledMapBuffer();
    }

    /// <summary>
    /// Get the height of the map at (x, z) in the current Map GameObject coordinate.
    /// Assume that the mesh is -90 rotated around X axis, and the mesh origin coincides with the origin of GameObject.transform
    /// </summary>
    /// <param name="x">X position in the coordinate of the Map</param>
    /// <param name="z">Z position in the coordinate of the Map</param>
    /// <returns></returns>
    public float GetMapHeight(float x, float z)
    {
        float[] gridLocation = MapMeshPointToGrid(x / m_scale.x, -z / m_scale.y, m_fineGridSize);
        // Naive flooring
        int u = (int)gridLocation[0];
        int v = (int)gridLocation[1];
        // Boundary check
        if (u < 0 || v < 0 || u > m_fineGridSize || v > m_fineGridSize)
        {
            return float.NaN;
        }
        return m_discretizedMapFineBuffer[u + v * (m_fineGridSize + 1)] * m_scale.y;
    }

    public void DrawHeightMapInRawImage(bool useFineBuffer = false)
    {
        float[] buffer;
        int pixelSize;
        if (useFineBuffer)
        {
            buffer = m_discretizedMapFineBuffer;
            pixelSize = m_fineGridSize + 1;
        }
        else
        {
            buffer = m_discretizedMapBuffer;
            pixelSize = m_gridSize;
        }
        float maxHeight = Mathf.Max(buffer);
        float minHeight = Mathf.Min(buffer);
        Texture2D texture = new Texture2D(pixelSize, pixelSize);
        for (int i = 0; i < pixelSize; i++)
        {
            for (int j = 0; j < pixelSize; j++)
            {
                float gray = (-minHeight + buffer[i + j * pixelSize]) / (maxHeight - minHeight);
                texture.SetPixel(pixelSize - j - 1, pixelSize - i - 1, new Color(gray, gray, gray));
            }
        }
        texture.Apply();
        GameObject gameObjectRawImage = GameObject.Find("RawImage");
        RawImage rawImage = gameObjectRawImage.GetComponent<RawImage>();
        rawImage.texture = texture;
    }

    #endregion

    #region Private

    private void UpdateDownsampledMapBuffer()
    {
        if (m_discretizedMapBuffer is null)
        {
            m_discretizedMapBuffer = new float[m_gridSize * m_gridSize];
        }

        int factor = m_fineGridSize / m_gridSize;

        for (int x = 0; x < m_gridSize; x++)
        {
            for (int y = 0; y < m_gridSize; y++)
            {
                float sum = 0f;

                // Calculate the average value for this patch in the original image
                for (int i = 0; i < factor; i++)
                {
                    for (int j = 0; j < factor; j++)
                    {
                        int originalX = x * factor + i;
                        int originalY = y * factor + j;

                        sum += m_discretizedMapFineBuffer[originalX + originalY * (m_fineGridSize + 1)];
                    }
                }

                m_discretizedMapBuffer[x + m_gridSize * y] = sum / (factor * factor);
            }
        }
    }

    private float[] MapMeshPointToGrid(Vector3 p, int gridSize)
    {
        float[] gridLocation = new float[2];
        // Assume the origin is at the center of the mesh
        gridLocation[0] = (p.x / m_meshSize + 0.5f) * gridSize;
        gridLocation[1] = (p.y / m_meshSize + 0.5f) * gridSize;
        return gridLocation;
    }

    private float[] MapMeshPointToGrid(float x, float y, int gridSize)
    {
        float[] gridLocation = new float[2];
        // Assume the origin is at the center of the mesh
        gridLocation[0] = (x / m_meshSize + 0.5f) * gridSize;
        gridLocation[1] = (y / m_meshSize + 0.5f) * gridSize;
        return gridLocation;
    }

    private float[] UnmapGridLocationToMeshXY(float[] gridPoint)
    {
        float[] meshLocation = new float[2];
        meshLocation[0] = (gridPoint[0] / m_fineGridSize - 0.5f) * m_meshSize;
        meshLocation[1] = (gridPoint[1] / m_fineGridSize - 0.5f) * m_meshSize;
        return meshLocation;
    }

    private bool WithinTriangle(float[] p, float[] p1, float[] p2, float[] p3)
    {
        // Calculate area of the main triangle
        float mainTriangleArea = TriangleArea(p1, p2, p3);

        // Calculate areas of sub-triangles
        float triangle1Area = TriangleArea(p, p1, p2);
        float triangle2Area = TriangleArea(p, p2, p3);
        float triangle3Area = TriangleArea(p, p3, p1);

        // Tolerance
        float tolerance = 0.0001f;

        // Check if sum of sub-triangles' areas is approximately equal to main triangle's area
        return Math.Abs(mainTriangleArea - (triangle1Area + triangle2Area + triangle3Area)) <= tolerance;
    }

    private float TriangleArea(float[] a, float[] b, float[] c)
    {
        return Mathf.Abs(a[0] * (b[1] - c[1]) + b[0] * (c[1] - a[1]) + c[0] * (a[1] - b[1])) / 2.0f;
    }

    public static float PlaneInterpolation(Vector3 a, Vector3 b, Vector3 c, float x, float y)
    {
        Plane plane = new Plane(a, b, c);

        // Calculate the vectors relative to point a
        Vector3 ab = b - a;
        Vector3 ac = c - a;

        // Calculate the normal of the plane
        Vector3 normal = plane.normal;
        float A = normal.x;
        float B = normal.y;
        float C = normal.z;

        // Calculate the constant D
        float D = -Vector3.Dot(normal, a);

        // Substitute x and y into the plane equation to solve for z
        float z = (-D - A * x - B * y) / C;

        return z;
    }


    [NonSerialized]
    float[] m_discretizedMapBuffer;
    [NonSerialized]
    float[] m_discretizedMapFineBuffer;
    [NonSerialized]
    Mesh m_currentMesh;
    [NonSerialized]
    Vector3 m_scale;
    [NonSerialized]
    float m_meshSize = 4.0f;
    [NonSerialized]
    int m_fineGridSize = 100;
    [NonSerialized]
    float m_maxHeight;
    [NonSerialized]
    float[] m_maxHeightMeshLocation;

    [SerializeField]
    int m_gridSize;

    #endregion
}

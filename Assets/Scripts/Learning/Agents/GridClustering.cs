/// By OpenAI o1-preview, 2024-09-23

using System.Collections.Generic;
using UnityEngine;

public class GridClustering
{
    public float maxDistance = 0.2f; // Maximum allowed distance
    public List<Vector3> points;     // List of points to cluster

    private Dictionary<Vector3Int, List<int>> grid; // Mapping from grid cell to point indices
    private int[] parent;                           // Union-Find parent array
    public List<List<int>> clusters;               // List of clusters, each cluster is a list of point indices
    public List<Vector3> centers;

    public void ComputeAll()
    {
        // Initialize the grid and parent array
        grid = new Dictionary<Vector3Int, List<int>>();
        parent = new int[points.Count];

        // Step 1: Assign points to grid cells
        AssignPointsToGrid();

        // Step 2: Build the graph
        BuildGraph();

        // Step 3: Find connected components (clusters)
        FindClusters();

        // Step 4: Calculate centers of clusters
        CalculateClusterCenters();
    }

    void AssignPointsToGrid()
    {
        float cellSize = maxDistance; // Cell size equal to maxDistance
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 point = points[i];

            // Compute grid cell indices
            int x = Mathf.FloorToInt(point.x / cellSize);
            int y = Mathf.FloorToInt(point.y / cellSize);
            int z = Mathf.FloorToInt(point.z / cellSize);
            Vector3Int cell = new Vector3Int(x, y, z);

            // Add point index to the cell
            if (!grid.ContainsKey(cell))
            {
                grid[cell] = new List<int>();
            }
            grid[cell].Add(i);

            // Initialize Union-Find parent
            parent[i] = i;
        }
    }

    void BuildGraph()
    {
        float maxDistanceSqr = maxDistance * maxDistance;
        float cellSize = maxDistance;

        // Directions to neighboring cells (including the cell itself)
        List<Vector3Int> neighborOffsets = GetNeighborOffsets();

        foreach (var cellEntry in grid)
        {
            Vector3Int cell = cellEntry.Key;
            List<int> cellPoints = cellEntry.Value;

            // Check neighboring cells
            foreach (Vector3Int offset in neighborOffsets)
            {
                Vector3Int neighborCell = cell + offset;

                if (!grid.ContainsKey(neighborCell))
                    continue;

                List<int> neighborPoints = grid[neighborCell];

                // Compare points in current cell with points in neighbor cell
                foreach (int idxA in cellPoints)
                {
                    foreach (int idxB in neighborPoints)
                    {
                        if (idxA >= idxB && cell == neighborCell)
                            continue; // Avoid duplicate checks

                        Vector3 pointA = points[idxA];
                        Vector3 pointB = points[idxB];

                        if ((pointA - pointB).sqrMagnitude <= maxDistanceSqr)
                        {
                            Union(idxA, idxB);
                        }
                    }
                }
            }
        }
    }

    List<Vector3Int> GetNeighborOffsets()
    {
        // Generate offsets for neighboring cells (3D)
        List<Vector3Int> offsets = new List<Vector3Int>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    offsets.Add(new Vector3Int(x, y, z));
                }
            }
        }
        return offsets;
    }

    int Find(int x)
    {
        // Path compression
        if (parent[x] != x)
            parent[x] = Find(parent[x]);
        return parent[x];
    }

    void Union(int x, int y)
    {
        int rootX = Find(x);
        int rootY = Find(y);

        if (rootX != rootY)
        {
            parent[rootY] = rootX;
        }
    }

    void FindClusters()
    {
        Dictionary<int, List<int>> clusterDict = new Dictionary<int, List<int>>();

        for (int i = 0; i < points.Count; i++)
        {
            int root = Find(i);

            if (!clusterDict.ContainsKey(root))
            {
                clusterDict[root] = new List<int>();
            }
            clusterDict[root].Add(i);
        }

        clusters = new List<List<int>>(clusterDict.Values);
    }

    // void CalculateClusterCenters()
    // {
    //     centers = new List<Vector3>(clusters.Count);
    //     for (int i = 0; i < clusters.Count; i++)
    //     {
    //         List<int> cluster = clusters[i];
    //         Vector3 sum = Vector3.zero;

    //         foreach (int idx in cluster)
    //         {
    //             sum += points[idx];
    //         }

    //         Vector3 center = sum / cluster.Count;
    //         centers.Add(center);
    //         Debug.Log($"Cluster {i}: Center = {center}, Points = {cluster.Count}");
    //     }
    // }

    void CalculateClusterCenters()
    {
        centers = new List<Vector3>(clusters.Count);
        for (int i = 0; i < clusters.Count; i++)
        {
            List<int> cluster = clusters[i];
            int medoidIndex = -1;
            float minTotalDistance = float.MaxValue;

            // Iterate over each point in the cluster to find the medoid
            foreach (int idxA in cluster)
            {
                Vector3 pointA = points[idxA];
                float totalDistance = 0f;

                // Compute total distance to all other points in the cluster
                foreach (int idxB in cluster)
                {
                    if (idxA == idxB) continue;
                    Vector3 pointB = points[idxB];
                    totalDistance += Vector3.Distance(pointA, pointB);
                }

                // Update medoid if a smaller total distance is found
                if (totalDistance < minTotalDistance)
                {
                    minTotalDistance = totalDistance;
                    medoidIndex = idxA;
                }
            }

            Vector3 medoid = points[medoidIndex];
            centers.Add(medoid);
        }
    }
}

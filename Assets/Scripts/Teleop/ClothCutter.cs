using PhysX5ForUnity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ClothCutter : MonoBehaviour
{
    public bool Cutting
    {
        get { return m_cutting; }
        set { m_cutting = value; }
    }

    void FixedUpdate()
    {
        if (m_cutting)
        {
            PxParticleSpring[] springs = m_actor.Springs;
            List<PxParticleSpring> listSprings = springs.ToList();
            Vector4[] particles = m_actor.ParticleData.PositionInvMass.ToArray();
            Plane cuttingPlane = new Plane(transform.forward, transform.position);
            bool shouldUpdateSpring = false;

            for (int i = listSprings.Count - 1; i >= 0; i--)
            {
                var sp = listSprings[i];
                Vector3 p0 = particles[sp.ind0];
                Vector3 p1 = particles[sp.ind1];
                float intersectionDistance;
                Ray springRay = new Ray(p0, p1 - p0);
                cuttingPlane.Raycast(springRay, out intersectionDistance);
                Vector3 intersectionPoint = springRay.GetPoint(intersectionDistance);
                if (intersectionDistance > 0 && intersectionDistance <= (p1 - p0).magnitude && IsPointInSector(intersectionPoint))
                {
                    int u1 = sp.ind0;
                    int u2 = sp.ind1;
                    // always ensure the first index is smaller
                    if (u1 > u2) (u1, u2) = (u2, u1);
                    m_affectedIndices.Add((u1, u2));
                    listSprings.Remove(sp);
                    shouldUpdateSpring = true;
                }
            }
            if (shouldUpdateSpring)
            {
                m_actor.SyncSetSprings(listSprings.ToArray());
                RemoveAffectedTriangles(m_clothMeshFilter.sharedMesh, m_affectedIndices);
            }
            m_cutting = false;
        }
    }

    private void RemoveAffectedTriangles(Mesh mesh, HashSet<(int, int)> affectedIndices)
    {
        bool IsEdgeAffected(int v1, int v2)
        {
            int u1 = m_actor.OriginalToUniqueMap[v1];
            int u2 = m_actor.OriginalToUniqueMap[v2];
            // ensure smaller index is first to handle undirected edges
            if (u1 > u2) (u1, u2) = (u2, u1);

            return affectedIndices.Contains((u1, u2));
        }

        int[] triangles = mesh.triangles;
        int[] newTriangles = new int[triangles.Length];
        int newTrianglesCount = 0;

        // loop through each triangle
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            // check how many edges of this triangle are affected
            int affectedEdgesCount = 0;
            if (IsEdgeAffected(v1, v2)) affectedEdgesCount++;
            if (IsEdgeAffected(v2, v3)) affectedEdgesCount++;
            if (IsEdgeAffected(v3, v1)) affectedEdgesCount++;

            // If less than two edges are affected, keep the triangle
            if (affectedEdgesCount < 2)
            {
                newTriangles[newTrianglesCount++] = v1;
                newTriangles[newTrianglesCount++] = v2;
                newTriangles[newTrianglesCount++] = v3;
            }
        }
        if (newTrianglesCount < newTriangles.Length)
        {
            Array.Resize(ref newTriangles, newTrianglesCount);
            mesh.triangles = newTriangles;
        }
    }

    public bool IsPointInSector(Vector3 point)
    {
        // check if within radius
        if ((transform.position - point).sqrMagnitude > m_sectorRadius * m_sectorRadius) return false;

        // check if within angle
        Vector3 toPoint = point - transform.position;
        float angle = Vector3.Angle(-transform.up, toPoint);
        return angle <= m_sectorAngle * 0.5f;
    }

    [SerializeField]
    private PhysxTriangleMeshClothActor m_actor;
    [SerializeField]
    private MeshFilter m_clothMeshFilter;
    [SerializeField]
    private float m_sectorRadius;
    [SerializeField]
    private float m_sectorAngle; // In degrees
    private bool m_cutting = false;
    private HashSet<(int, int)> m_affectedIndices = new HashSet<(int, int)>();
}

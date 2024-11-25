using PhysX5ForUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TestRandomTissueMeshGenerator : MonoBehaviour
{
    void Update()
    {
        m_time = Time.fixedTime;
        if (m_time > 3 && m_shouldGenerate)
        {
            m_shouldGenerate = false;
            m_meshGenerator.NumControlPoints = 11;
            Mesh mesh = m_meshGenerator.GenerateMesh();
            m_meshFilter.mesh = mesh;
            // m_actor.enabled = false;
            // m_triangleMeshGeometry.Mesh = mesh;
            // m_triangleMeshGeometry.Recreate();
            m_actor.ReferenceMesh = mesh;
            m_actor.Recreate();
            // m_actor.enabled = true;
        }
    }

    [SerializeField]
    MeshFilter m_meshFilter;
    [SerializeField]
    TissueMeshGenerator m_meshGenerator;
    [SerializeField]
    PhysxFEMSoftBodyActor m_actor;
    [SerializeField]
    PhysxTriangleMeshGeometry m_triangleMeshGeometry;
    float m_time = 0;
    bool m_shouldGenerate = true;
}

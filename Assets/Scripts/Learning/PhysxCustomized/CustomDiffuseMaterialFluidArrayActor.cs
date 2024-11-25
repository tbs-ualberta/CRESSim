using System.Collections;
using System.Collections.Generic;
using PhysX5ForUnity;
using UnityEngine;

public class CustomDiffuseMaterialFluidArrayActor : PhysxFluidArrayActor, ICustomFluidActor
{
    public float MaterialState
    {
        get { return m_materialState; }
    }

    public int ParticleMaterialChangeCount
    {
        get { return m_particleMaterialChangeCount; }
        set { m_particleMaterialChangeCount = value; }
    }
    
    [SerializeField]
    private float m_materialState;

    private int m_particleMaterialChangeCount;
}

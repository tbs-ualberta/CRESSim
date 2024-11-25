using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PhysX5ForUnity;
using UnityEngine;

public class FluidParticleRigidFilter : MonoBehaviour
{
    void Start()
    {
        for (int i = 0; i < m_fluidActor.NumParticles; ++i)
        {
            foreach (PhysxNativeGameObjectBase rigidActor in m_rigidActors)
            {
                m_fluidActor.AddParticleRigidFilter(rigidActor, i);
            }
        }
    }

    [SerializeField]
    protected PhysxFluidActor m_fluidActor;
    [SerializeField]
    protected PhysxNativeGameObjectBase[] m_rigidActors;
}

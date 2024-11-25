using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PhysX5ForUnity;
using UnityEngine;

public class TestParticleRigidFilter : MonoBehaviour
{
    void Start()
    {
        for (int i = 0; i < m_fluidActor.NumParticles; ++i)
        {
            m_fluidActor.AddParticleRigidFilter(m_rigidActor, i);
        }
    }

    void Update()
    {
        if (Time.fixedTime > 3 && m_shouldRemoveFilters)
        {
            foreach (ParticleRigidFilterPair pair in m_fluidActor.ParticleRigidFilterPairs.ToList())
            {
                m_fluidActor.RemoveParticleRigidFilter(pair);
            }
            m_shouldRemoveFilters = false;
        }
    }

    [SerializeField]
    PhysxFluidArrayActor m_fluidActor;
    [SerializeField]
    PhysxRigidActor m_rigidActor;

    bool m_shouldRemoveFilters = true;
}

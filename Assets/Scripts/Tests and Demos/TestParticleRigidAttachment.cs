using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PhysX5ForUnity;
using UnityEngine;

public class TestParticleRigidAttachment : MonoBehaviour
{
    void Start()
    {
        for (int i = 0; i < m_fluidActor.NumParticles; ++i)
        {
            Vector3 fixedLocalPosition = m_rigidActor.transform.InverseTransformPoint(m_fluidActor.ParticleData.PositionInvMass[i]);
            fixedLocalPosition.x *= m_rigidActor.transform.lossyScale.x;
            fixedLocalPosition.y *= m_rigidActor.transform.lossyScale.y;
            fixedLocalPosition.z *= m_rigidActor.transform.lossyScale.z;
            Physx.AttachParticleToRigidBody(m_fluidActor.NativeObjectPtr, i, m_rigidActor.NativeObjectPtr, ref fixedLocalPosition);
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

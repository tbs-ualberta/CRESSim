using System.Collections;
using System.Collections.Generic;
using PhysX5ForUnity;
using UnityEngine;

public class CustomDiffuseMaterialFluidSourceActor : PhysxFluidSourceActor, ICustomFluidActor
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

    public bool InEpisode
    {
        get { return m_inEpisode; }
        set { m_inEpisode = value; }
    }

    public bool IsActive
    {
        get { return m_isActive; }
        set { m_isActive = value; }
    }

    public int CompletionThreshold
    {
        get { return m_completionThreshold; }
        set { m_completionThreshold = value; }
    }

    public override void ResetObject()
    {
        base.ResetObject();
        m_inEpisode = false;
        m_isActive = false;
        m_lastParticleMaterialChangeCount = 0;
    }

    protected override void UpdateParticleData()
    {
        if (m_inEpisode && m_isActive)
        {
            base.UpdateParticleData();
            switch (m_agent.TaskLesson)
            {
                case 1:
                    m_agent.AddIrrigationReward(m_blood.ParticleMaterialChangeCount - m_lastParticleMaterialChangeCount);
                    m_lastParticleMaterialChangeCount = m_blood.ParticleMaterialChangeCount;
                    if (m_blood.ParticleMaterialChangeCount > m_materialChangeThreshold)
                    {
                        ChangeBloodMaterial();
                    }
                    if (m_blood.ParticleMaterialChangeCount >= m_completionThreshold)
                    {
                        m_agent.AddCompletionReward();
                        m_agent.EndEpisode();
                    }
                    // For evaluation, if the max possible number of particles changed, we can consider it successful but no completion reward added.
                    // if (m_agent.Evaluation && m_blood.ParticleMaterialChangeCount == m_agent.ActiveBloodIndices.Count)
                    // {
                    //     m_agent.AddCompletionReward();
                    //     m_agent.CummulativeReward -= 5;
                    //     m_agent.EndEpisode();
                    // }
                    break;
                case 0:
                    if (m_blood.ParticleMaterialChangeCount > m_materialChangeThreshold)
                    {
                        ChangeBloodMaterial();
                    }
                    break;
            }
            if (m_activeParticleIndices.Count == m_numParticles)
            {
                m_agent.EndEpisode();
            }
        }
        else
        {
            SyncParticleDataGet();
        }

        // Remove particles below the boundary
        bool belowBoundary = false;
        Vector4 teleportPosition = new Vector4(InactiveParticlePosition.x, InactiveParticlePosition.y, InactiveParticlePosition.z, 0f);
        List<int> particlesToRemove = new List<int>();

        foreach (int idx in m_activeParticleIndices)
        {
            Vector4 position = m_particleData.PositionInvMass[idx];
            // in case particle drops below the map
            if (position.y < -10.0f)
            {
                belowBoundary = true;
                m_particleData.SetParticle(idx, teleportPosition, true);
                particlesToRemove.Add(idx);
            }
        }

        if (belowBoundary)
        {
            foreach (int idx in particlesToRemove)
            {
                m_activeParticleIndices.Remove(idx);
            }
            m_particleData.SyncParticlesSet(false);
        }

        belowBoundary = false;
        particlesToRemove.Clear();

        foreach (int idx in m_blood.ActiveParticleIndices)
        {
            Vector4 position = m_blood.ParticleData.PositionInvMass[idx];
            // in case particle drops below the map
            if (position.y < -10.0f)
            {
                belowBoundary = true;
                m_blood.ParticleData.SetParticle(idx, teleportPosition, true);
                particlesToRemove.Add(idx);
            }
        }

        if (belowBoundary)
        {
            foreach (int idx in particlesToRemove)
            {
                m_blood.ActiveParticleIndices.Remove(idx);
            }
            m_blood.ParticleData.SyncParticlesSet(false);
        }
        // Debug.Log(m_agent.CummulativeReward);
    }

    private void ChangeBloodMaterial()
    {
        m_blood.PBDMaterial.Friction = m_pbdMaterial.Friction;
        m_blood.PBDMaterial.Viscosity = m_pbdMaterial.Viscosity;
        m_blood.PBDMaterial.SurfaceTension = m_pbdMaterial.SurfaceTension;
        m_blood.PBDMaterial.Cohesion = m_pbdMaterial.Cohesion;
    }

    [SerializeField]
    private float m_materialState;
    [SerializeField]
    private CustomDiffuseMaterialFluidArrayActor m_blood;
    [SerializeField]
    private IrrigationAgent m_agent;
    [SerializeField]
    private float m_materialChangeThreshold = 100;

    private int m_particleMaterialChangeCount;
    private bool m_inEpisode = false;
    private bool m_isActive = false;
    private int m_lastParticleMaterialChangeCount = 0;
    private int m_completionThreshold;
}

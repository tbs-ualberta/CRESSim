using PhysX5ForUnity;
using System;
using UnityEngine;

[ExecuteInEditMode]
[DisallowMultipleComponent]
[AddComponentMenu("Blood Simulation/Suction Actor")]
public class SuctionActor : MonoBehaviour
{
    #region Properties

    [SerializeField]
    public PhysxParticleActor pxParticleActor;

    public float Radius
    {
        get { return m_radius; }
        set { m_radius = value; }
    }

    public float ConeAngle
    {
        get { return m_coneAngle; }
        set { m_coneAngle = value; }
    }

    public float RemovalRadius
    {
        get { return m_removalRadius; }
        set { m_removalRadius = value; }
    }

    public float ForceScale
    {
        get { return m_forceScale; }
        set { m_forceScale = value; }
    }

    public bool IsActive
    {
        get { return m_isActive; }
        set { m_isActive = value; }
    }

    public bool InEpisode
    {
        get { return m_inEpisode; }
        set { m_inEpisode = value; }
    }

    #endregion

    #region Messages

    private void Start()
    {
        localCoordinatePosition = transform.position;
    }

    private void FixedUpdate()
    {
        UpdateParticles();
    }

    private void OnValidate()
    {
        // transform.localScale = new Vector3(m_radius * 2, m_radius * 2, m_radius * 2);
    }

    #endregion

    #region Private

    void UpdateParticles()
    {
        if (Application.isPlaying && m_inEpisode)
        {
            Vector3 forceOrigin = transform.position;
            Vector3 forceDirecion = -transform.up; // the - Y axis of the gameObject
            //int[] activeIndices = m_particleDataContainer.ActiveIndices;
            //int activeCount = m_particleDataContainer.ActiveCount;
            int removalIndexCount = 0;
            Vector4 removalTeleportPosition = new Vector4(5.0f + localCoordinatePosition.x, -5.0f + localCoordinatePosition.y, 0.0f + localCoordinatePosition.z, 0.0f);
            float dT = Time.fixedDeltaTime;
            for (int i = 0; i < pxParticleActor.ParticleData.NumParticles; ++i)
            {
                //int idx = activeIndices[i];
                int idx = i;
                Vector4 position = pxParticleActor.ParticleData.PositionInvMass[idx];
                Vector3 p = position;
                Vector3 delta = p - forceOrigin;

                // in case particle drops below the map
                if (position.y < -10.0f)
                {
                    pxParticleActor.ParticleData.SetParticle(idx, removalTeleportPosition, true);
                    continue;
                }

                float t = Vector3.Dot(delta, forceDirecion);
                float distanceToOrigin = delta.magnitude;
                if (distanceToOrigin > m_radius)
                {
                    // Early stopping
                    continue;
                }

                Vector3 c = Vector3.Cross(delta, forceDirecion);
                float norm_c = Mathf.Sqrt(Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2) + Mathf.Pow(c.z, 2));
                float a = Mathf.Atan2(norm_c, t) * 180 / 3.1415f;

                if (distanceToOrigin < m_removalRadius) // using distanceToOrigin is less realistic
                {
                    ++removalIndexCount;
                    pxParticleActor.ParticleData.SetParticle(idx, removalTeleportPosition, true);
                }


                if (a < m_coneAngle)
                {
                    Vector3 addedVelocity = -m_forceScale * dT * position.w * (delta.normalized / (distanceToOrigin + 0.0001f) - delta.normalized / (m_radius + 0.0001f));
                    Vector3 velocity = pxParticleActor.ParticleData.Velocity[idx];
                    pxParticleActor.ParticleData.SetVelocity(idx, velocity + addedVelocity, true);
                }
            }
            pxParticleActor.ParticleData.SyncParticlesSet(false);
        }
    }

    [SerializeField]
    float m_radius;
    [SerializeField]
    float m_removalRadius = 0.5f;
    [SerializeField]
    float m_coneAngle = 30.0f;
    [SerializeField]
    float m_forceScale = 1.0f;
    [SerializeField]
    bool m_isActive = false;
    // [SerializeField]
    // ParticleDataContainer m_particleDataContainer;
    [SerializeField]
    bool m_useMapHeightForRemoval;

    [NonSerialized]
    bool m_inEpisode = true;
    [NonSerialized]
    Vector3 localCoordinatePosition;


    #endregion
}

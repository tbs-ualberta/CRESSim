using PhysX5ForUnity;
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Learning/Suction Actor with Reward")]
public class SuctionActorWithReward : MonoBehaviour
{
    #region Properties

    public LinkedList<int> ActiveFluidIndices
    {
        get { return m_activeFluidIndices; }
        set { m_activeFluidIndices = value; }
    }

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

    public bool InEpisode
    {
        get { return m_inEpisode; }
        set { m_inEpisode = value; }
    }

    #region Public

    public void InitializeActiveIndices(List<int> indices)
    {
        m_activeFluidIndices = new LinkedList<int>(indices);
    }
    #endregion

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

    #endregion

    #region Private

    void UpdateParticles()
    {
        if (m_inEpisode)
        {
            Vector3 forceOrigin = transform.position;
            Vector3 forceDirecion = -transform.up; // the - Y axis of the gameObject
            //int[] activeIndices = m_particleDataContainer.ActiveIndices;
            //int activeCount = m_particleDataContainer.ActiveCount;
            int removalIndexCount = 0;
            Vector4 removalTeleportPosition = new Vector4(5.0f + localCoordinatePosition.x, -5.0f + localCoordinatePosition.y, -5.0f + localCoordinatePosition.z, 0.0f);
            float dT = Time.fixedDeltaTime;

            float currentHorizontalDistanceToFluid = float.MaxValue;


            for (LinkedListNode<int> it = m_activeFluidIndices.First; it != null;)
            {
                LinkedListNode<int> next = it.Next;
            // }
            // for (int i = 0; i < m_pxParticleActor.ParticleData.NumParticles; ++i)
            // {
                //int idx = activeIndices[i];
                int idx = it.Value;
                Vector4 position = m_pxParticleActor.ParticleData.PositionInvMass[idx];
                Vector3 p = position;
                Vector3 delta = p - forceOrigin;

                // in case particle drops below the map
                if (position.y < -10.0f)
                {
                    m_pxParticleActor.ParticleData.SetParticle(idx, removalTeleportPosition, true);
                    m_activeFluidIndices.Remove(it); // as a side effect it.Next == null
                    it = next;
                    continue;
                }

                float horizontalDistance = Mathf.Sqrt(HorizontalDistanceSquared(p, transform.position));
                if (horizontalDistance < currentHorizontalDistanceToFluid) currentHorizontalDistanceToFluid = horizontalDistance;

                float t = Vector3.Dot(delta, forceDirecion);
                float distanceToOrigin = delta.magnitude;
                if (distanceToOrigin > m_radius)
                {
                    // Early stopping
                    it = next;
                    continue;
                }

                Vector3 c = Vector3.Cross(delta, forceDirecion);
                float norm_c = Mathf.Sqrt(Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2) + Mathf.Pow(c.z, 2));
                float a = Mathf.Atan2(norm_c, t) * 180 / 3.1415f;

                if (distanceToOrigin < m_removalRadius) // using distanceToOrigin is less realistic
                {
                    ++removalIndexCount;
                    m_pxParticleActor.ParticleData.SetParticle(idx, removalTeleportPosition, true);
                    m_activeFluidIndices.Remove(it);
                }


                if (a < m_coneAngle)
                {
                    Vector3 addedVelocity = -m_forceScale * dT * position.w * (delta.normalized / (distanceToOrigin + 0.0001f) - delta.normalized / (m_radius + 0.0001f));
                    Vector3 velocity = m_pxParticleActor.ParticleData.Velocity[idx];
                    m_pxParticleActor.ParticleData.SetVelocity(idx, velocity + addedVelocity, true);
                }
                it = next;
            }
            m_agent.AddSuctionReward(removalIndexCount);
            m_pxParticleActor.ParticleData.SyncParticlesSet(false);

            // Some checks to prevent negative reward when finishing suctioning an area
            // I don't know why sometimes large values are preset. Simply check the magnitude.
            if (!m_isSuctionLastStep && removalIndexCount == 0 && Mathf.Abs(currentHorizontalDistanceToFluid) < 1e6 && Mathf.Abs(m_lastHorizontalDistanceToFluid) < 1e6)
            {
                m_agent.AddSuctionApproachingReward(currentHorizontalDistanceToFluid, m_lastHorizontalDistanceToFluid);
            }
            m_lastHorizontalDistanceToFluid = currentHorizontalDistanceToFluid;
            m_isSuctionLastStep = removalIndexCount > 0;
            if (m_activeFluidIndices.Count <= 3)
            {
                m_agent.AddCompletionReward();
                m_agent.EndEpisode();
            }
        }
        else
        {
            // if not in episode, always update the last distance
            m_lastHorizontalDistanceToFluid = GetToBloodHorizontalDistance();
        }
    }

    private static float HorizontalDistanceSquared(Vector3 a, Vector3 b)
    {
        Vector3 diff = a - b;
        return diff.x * diff.x + diff.z * diff.z;
    }

    private static float HorizontalDistanceSquared(Vector4 a, Vector3 b)
    {
        float diffX = a.x - b.x;
        float diffZ = a.z - b.z;
        return diffX * diffX + diffZ * diffZ;
    }

    private float GetToBloodHorizontalDistance()
    {
        float dist = float.MaxValue;
        Vector3 liquidSourcePosition = transform.position;
        for (LinkedListNode<int> it = m_activeFluidIndices.First; it != null;)
        {
            LinkedListNode<int> next = it.Next;
            int idx = it.Value;
            Vector4 p = m_pxParticleActor.ParticleData.PositionInvMass[idx];
            float d = Mathf.Sqrt(HorizontalDistanceSquared(p, liquidSourcePosition));
            if (d < dist) dist = d;
            it = next;
        }
        return dist;
    }

    [SerializeField]
    PhysxParticleActor m_pxParticleActor;
    [SerializeField]
    float m_radius;
    [SerializeField]
    float m_removalRadius = 0.5f;
    [SerializeField]
    float m_coneAngle = 30.0f;
    [SerializeField]
    float m_forceScale = 1.0f;
    [SerializeField]
    SuctionAgent m_agent;

    [NonSerialized]
    bool m_inEpisode = true;
    [NonSerialized]
    Vector3 localCoordinatePosition;

    private LinkedList<int> m_activeFluidIndices;
    private float m_lastHorizontalDistanceToFluid;
    private bool m_isSuctionLastStep = false;
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using PhysX5ForUnity;
using Unity.MLAgents;
using UnityEngine;

public class RobotEndEffectorPoseRewarder : MonoBehaviour
{
    void Start()
    {
        m_initialEETransformData = m_eeTransform.ToPxTransformData();
    }

    void FixedUpdate()
    {
        if (Application.isPlaying && m_agent.InEpisode)
        {
            float angle = Vector3.Angle(m_initialEETransformData.quaternion * Vector3.up, m_eeTransform.up);
            m_agent.AddEEPoseReward(angle);
        }
    }

    [SerializeField]
    private SuctionIrrigationAgentBase m_agent;
    [SerializeField]
    private Transform m_eeTransform;

    private PxTransformData m_initialEETransformData;
}

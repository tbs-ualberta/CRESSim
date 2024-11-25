using PhysX5ForUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class RobotForceRewarder : MonoBehaviour
{
    public bool InContact
    {
        get { return m_inContact; }
    }

    public float PenaltyScale
    {
        get { return -m_penaltyReward; }
        set { m_penaltyReward = -value; }
    }

    void Start()
    {
        m_sqrMaxForce = m_maxForce * m_maxForce;
        m_sqrMaxTorque = m_maxTorque * m_maxTorque;
    }

    void FixedUpdate()
    {
        if (Application.isPlaying && m_agent.InEpisode)
        {
            PxSpatialForceData f;
            Physx.GetRobotLinkIncomingForce(m_robot.NativeObjectPtr, 5, out f);
            if (f.force.sqrMagnitude > m_sqrMaxForce || f.torque.sqrMagnitude > m_sqrMaxTorque)
            {
// #if UNITY_EDITOR
//                 Debug.Log(f.force.magnitude);
//                 Debug.Log(f.torque.magnitude);
// #endif
                m_agent.AddSuctionIrrigationReward(m_penaltyReward);
                m_inContact = true;
                if (m_stopEpisodeImmediately)
                {
                    m_agent.EndEpisode();
                }
            }
            else
            {
                m_inContact = false;
            }
        }
    }

    [SerializeField]
    private PhysxArticulationRobot m_robot;
    [SerializeField]
    private SuctionIrrigationAgentBase m_agent;
    [SerializeField]
    private float m_maxForce;
    [SerializeField]
    private float m_maxTorque;
    [SerializeField]
    private bool m_stopEpisodeImmediately;
    

    [NonSerialized]
    private float m_sqrMaxForce;
    [NonSerialized]
    private float m_sqrMaxTorque;

    private bool m_inContact = false;
    private float m_penaltyReward;
}

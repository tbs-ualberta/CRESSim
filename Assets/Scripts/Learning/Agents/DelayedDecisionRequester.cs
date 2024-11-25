using PhysX5ForUnity;
using System;
using System.Reflection;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;


[AddComponentMenu("Blood Simulation/Delayed Decision Requester")]
public class DelayedDecisionRequester : DecisionRequester
{
    public int DelayedSteps
    {
        get { return m_delayedSteps; }
        set { m_delayedSteps = value; }
    }

    private void Start()
    {
        m_propertyInfo = new PropertyInfo[m_actors.Length];
        for (int i = 0; i < m_actors.Length; i++)
        {
            m_propertyInfo[i] = m_actors[i].GetType().GetProperty("InEpisode");
        }
    }

    protected override bool ShouldRequestDecision(DecisionRequestContext context)
    {
        // Ensures that the delay has ended and is at the decision step
        if (Agent.StepCount < m_delayedSteps) m_isDelayEnd = false;
        m_isDelayEnd = m_isDelayEnd || (Agent.StepCount >= m_delayedSteps && context.AcademyStepCount % DecisionPeriod == DecisionStep);
        if (!m_isDelayEnd)
        {
            for (int i = 0; i < m_actors.Length; i++)
            {
                m_propertyInfo[i].SetValue(m_actors[i], false);
            }
            m_agent.InEpisode = false;
            m_shouldInitialize = true;
        }
        else
        {
            for (int i = 0; i < m_actors.Length; i++)
            {
                m_propertyInfo[i].SetValue(m_actors[i], true);
            }
            m_agent.InEpisode = true;
            InitialFrameCameraSensorComponent initialFrameCameraSensorComponent = m_agent.GetComponent<InitialFrameCameraSensorComponent>();
            if (m_shouldInitialize && initialFrameCameraSensorComponent != null)
            {
                initialFrameCameraSensorComponent.UpdateSensorFrame();
                m_shouldInitialize = false;
            }
        }

        return context.AcademyStepCount % DecisionPeriod == DecisionStep && m_isDelayEnd;
    }

    protected override bool ShouldRequestAction(DecisionRequestContext context)
    {
        return TakeActionsBetweenDecisions && m_isDelayEnd;
    }

    [SerializeField]
    int m_delayedSteps;
    [SerializeField]
    MonoBehaviour[] m_actors;
    [SerializeField]
    SuctionIrrigationAgentBase m_agent;

    [NonSerialized]
    PropertyInfo[] m_propertyInfo;
    bool m_shouldInitialize = false;
    bool m_isDelayEnd = false;
}

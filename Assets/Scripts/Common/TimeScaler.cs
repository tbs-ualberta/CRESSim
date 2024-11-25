using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeScaler : MonoBehaviour
{
    void OnEnable()
    {
        m_fixedDeltaTime = Time.fixedDeltaTime;
        if (!m_scaleWaitingOnly)
        {
            Time.timeScale = m_timeScale;
            // Time.fixedDeltaTime = m_fixedDeltaTime * Time.timeScale;
        }
    }

    void FixedUpdate()
    {
        if (m_scaleWaitingOnly)
        {
            if (m_agent.InEpisode)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = m_fixedDeltaTime;
            }
            else
            {
                Time.timeScale = m_timeScale;
            }
        }
    }

    void OnDisable()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = m_fixedDeltaTime;
    }

    [SerializeField]
    private bool m_scaleWaitingOnly = false;
    [SerializeField]
    private float m_timeScale;
    [SerializeField]
    private SuctionIrrigationAgentBase m_agent;
    private float m_fixedDeltaTime;
}

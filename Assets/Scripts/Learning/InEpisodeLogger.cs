using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InEpisodeLogger : MonoBehaviour
{
    void FixedUpdate()
    {
        if (m_agent.InEpisode)
        {
            Debug.Log("In episode");
        }
        else
        {
            Debug.Log("Not in episode");
        }
    }

    [SerializeField]
    private SuctionIrrigationAgentBase m_agent;
}

using PhysX5ForUnity;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class SuctionAgent : SuctionIrrigationAgentBase
{
    protected override void Start()
    {
        base.Start();
        if (m_scriptedDemos)
        {
            m_demoTargetEEPose = m_demoEETooltip.ToPxTransformData();
            m_gridClustering = new GridClustering();
            m_gridClustering.maxDistance = m_bloodFluid.PBDParticleSystem.ParticleSpacing + 0.2f;
        }
    }
    protected override void GetCommonRandomParameters()
    {
        base.GetCommonRandomParameters();
        m_approachingRewardDistanceThreshold = m_randomParameterHelper.GetWithDefault("approaching_reward_distance_threshold", 0.2f);
        m_approachingRewardScale = m_randomParameterHelper.GetWithDefault("approaching_reward", 10f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // avoid undesired actions when not in the episode
        if (m_inEpisode)
        {
            float[] targetJointPositions = new float[5];

            for (int i = 0; i < 5; i++)
            {
                targetJointPositions[i] = m_robotController.JointPositionSetpoint[i];
                targetJointPositions[i] += actions.ContinuousActions[i] * m_actionScale[i];
            }
            m_robotController.DriveJoints(targetJointPositions);
        }
    }

    public void AddSuctionReward(int numParticlesSuctioned)
    {
        float r = numParticlesSuctioned * 0.03f;
        AddSuctionIrrigationReward(r);
    }

    public void AddSuctionApproachingReward(float currentDist, float lastDist)
    {
        if (m_approachingRewardScale > 0 && currentDist > m_approachingRewardDistanceThreshold)
        {
            AddSuctionIrrigationReward(m_approachingRewardScale * (lastDist - currentDist));
        }
    }

    protected override void RandomizeBlood()
    {
        base.RandomizeBlood();

        m_suctionActor.InitializeActiveIndices(m_activeBloodIndices);
        m_suctionActor.InEpisode = false;

        if (m_evaluation)
        {
            m_evalInitialBloodParticleNum = m_activeBloodIndices.Count;
        }
    }

    protected override void HeurisiticScriptedInput(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        float minDistEEToCenter = float.MaxValue;
        Vector3 nearestClusterCenter = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);


        List<Vector3> points = new List<Vector3>(m_suctionActor.ActiveFluidIndices.Count);
        foreach (int idx in m_suctionActor.ActiveFluidIndices)
        {
            points.Add(m_bloodFluid.ParticleData.PositionInvMass[idx]);
        }
        m_gridClustering.points = points;
        m_gridClustering.ComputeAll();

        int nearestClusterIndex = -1;

        for (int i = 0; i < m_gridClustering.clusters.Count; i++)
        {
            if (m_gridClustering.clusters[i].Count < 4) continue;
            Vector2 horizontalDirection = new Vector2();
            Vector3 center = m_gridClustering.centers[i];
            horizontalDirection.x = (center - m_demoEETooltip.position).x;
            horizontalDirection.y = (center - m_demoEETooltip.position).z;
            if (horizontalDirection.magnitude < minDistEEToCenter)
            {
                minDistEEToCenter = horizontalDirection.magnitude;
                nearestClusterIndex = i;
                nearestClusterCenter = center;
            }
        }

        if (nearestClusterIndex == -1) return;

        if (minDistEEToCenter < 0.1)
        {
            m_demoTargetEEPose.position.x = nearestClusterCenter.x;
            m_demoTargetEEPose.position.y = nearestClusterCenter.y + 0.1f;   // A hardcoded height above the blood.
            m_demoTargetEEPose.position.z = nearestClusterCenter.z;
        }
        else
        {
            m_demoTargetEEPose.position.x = nearestClusterCenter.x;
            m_demoTargetEEPose.position.y = nearestClusterCenter.y + 0.3f;   // A hardcoded height above the blood.
            m_demoTargetEEPose.position.z = nearestClusterCenter.z;
        }

        // Extended 6-element current jp setpoint
        float[] targetJointPositions = ((PSMSuctionIrrigator6DofControllerMod)m_robotController).GetExtendedJointPositionSetPoint(m_robotController.JointPositionSetpoint);
        bool success = m_robotController.Robot.InverseKinematics(targetJointPositions, m_demoTargetEEPose);
        if (success)
        {
            float[] currentJpSetpoint = m_robotController.JointPositionSetpoint;
            float actionScale = 5f;
            continuousActionsOut[0] = Mathf.Clamp(actionScale * (targetJointPositions[0] - currentJpSetpoint[0]), -1, 1);
            continuousActionsOut[1] = Mathf.Clamp(actionScale * (targetJointPositions[1] - currentJpSetpoint[1]), -1, 1);
            continuousActionsOut[2] = Mathf.Clamp(actionScale * (targetJointPositions[2] - currentJpSetpoint[2]), -1, 1);
            continuousActionsOut[3] = Mathf.Clamp(actionScale * (targetJointPositions[3] - currentJpSetpoint[3]), -1, 1);
            continuousActionsOut[4] = Mathf.Clamp(actionScale * (targetJointPositions[4] - currentJpSetpoint[4]), -1, 1);
        }
        else
        {
            continuousActionsOut[0] = 0;
            continuousActionsOut[1] = 0;
            continuousActionsOut[2] = 0;
            continuousActionsOut[3] = 0;
            continuousActionsOut[4] = 0;
        }
    }

    protected override void InitializeEvalRecorder()
    {
        base.InitializeEvalRecorder();
        if (m_evalRecorder != null)
        {
            m_evalRecorder.RecordLine(m_evalRecorderFileName, "reward,success,initial,final");
        }
    }

    protected override void RecordEval()
    {
        if (m_evalRecorder != null && m_suctionActor != null && m_suctionActor.ActiveFluidIndices != null)
        {
            m_evalRecorder.RecordLine(m_evalRecorderFileName, CummulativeReward.ToString() +
                "," + m_isSuccess.ToString() + "," +
                m_evalInitialBloodParticleNum + "," +
                m_suctionActor.ActiveFluidIndices.Count);
#if UNITY_EDITOR
            if (CompletedEpisodes >= m_numEvalEpisodes)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
#endif
        }
    }

    [SerializeField]
    protected SuctionActorWithReward m_suctionActor;
    [SerializeField]
    private Transform m_demoEETooltip;
    private float m_approachingRewardDistanceThreshold;
    private float m_approachingRewardScale;
    private PxTransformData m_demoTargetEEPose;
    private GridClustering m_gridClustering;
    private int m_evalInitialBloodParticleNum;
}

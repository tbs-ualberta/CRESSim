using System;
using System.Collections;
using System.Collections.Generic;
using PhysX5ForUnity;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;

public class IrrigationAgent : SuctionIrrigationAgentBase
{
    public int TaskLesson
    {
        get { return m_taskLesson; }
    }

    public override void OnEpisodeBegin()
    {
        // Debug.Log(CummulativeReward);
        base.OnEpisodeBegin();
        m_liquidSource.CompletionThreshold = m_completionThreshold;
        RandomizeLiquidSource();
        m_fluidRenderer.ResetMaterialStates(m_bloodFluid);
        m_fluidRenderer.ResetMaterialStates(m_liquidSource);
        m_liquidSource.InEpisode = false;
        m_liquidSource.IsActive = false;
        m_isIrrigating = false;
    }

    protected override void Start()
    {
        base.Start();
        if (m_scriptedDemos)
        {
            m_demoTargetEEPose = m_demoEETooltip.ToPxTransformData();
        }
    }

    private void AddIrrigationApproachingReward(float currentDist, float lastDist)
    {
        // Some hack to prevent weird large values possible because of physics
        if (m_approachingRewardScale > 0 && Mathf.Abs(currentDist) < 20 && Mathf.Abs(lastDist) < 20)
        {
            AddSuctionIrrigationReward(m_approachingRewardScale * (lastDist - currentDist));
        }
    }

    private void FixedUpdate()
    {
        // Debug.Log(CummulativeReward);
        switch (m_taskLesson)
        {
            case 0:
                if (m_inEpisode)
                {
                    // Continuous approaching reward
                    float currentDistance = GetLiquidSourceToBloodHorizontalDistance();

                    if (currentDistance < m_irrigationSwitchOnLocationPenaltyDistanceThreshold)
                    {
                        if (m_isIrrigating)
                        {
                            AddSuctionIrrigationReward(m_irrigationSwitchOnLocationReward);
                            // AddSuctionIrrigationReward(m_completionReward);
                            // EndEpisode();
                        }
                        else AddSuctionIrrigationReward(-m_irrigationSwitchOnLocationPenalty);
                    }
                    else
                    {
                        if (m_isIrrigating) AddSuctionIrrigationReward(-m_irrigationSwitchOnLocationPenalty);
                        else AddIrrigationApproachingReward(currentDistance, m_lastDistance);
                    }
                    m_lastDistance = currentDistance;
                }
                else
                {
                    m_lastDistance = GetLiquidSourceToBloodHorizontalDistance();
                }
                break;
            case 1:
                if (m_inEpisode)
                {
                    // Continuous approaching reward
                    float currentDistance = GetLiquidSourceToBloodHorizontalDistance();

                    if (currentDistance < m_irrigationSwitchOnLocationPenaltyDistanceThreshold)
                    {
                        if (m_isIrrigating) AddSuctionIrrigationReward(m_irrigationSwitchOnLocationReward);
                        else AddSuctionIrrigationReward(-m_irrigationSwitchOnLocationPenalty);
                    }
                    else
                    {
                        if (m_isIrrigating) AddSuctionIrrigationReward(-m_irrigationSwitchOnLocationPenalty);
                        else AddIrrigationApproachingReward(currentDistance, m_lastDistance);
                    }
                    m_lastDistance = currentDistance;
                }
                else
                {
                    m_lastDistance = GetLiquidSourceToBloodHorizontalDistance();
                }
                break;
        }
    }

    protected override void GetCommonRandomParameters()
    {
        // Task lessons
        // Lesson 0: Reward when moving closer; Reward and end episode when irrigate close to the location; Penalty when irrigates at other locations;
        // Lesson 1: The actual task.
        m_taskLesson = Mathf.RoundToInt(m_randomParameterHelper.GetWithDefault("task_lesson", 0));

        // Penalty scales
        m_actionPenaltyScale = m_randomParameterHelper.GetWithDefault("action_penalty", 0.02f);
        m_eePosePenaltyScale = m_randomParameterHelper.GetWithDefault("ee_pose_penalty", 0.0005f);
        m_eeForcePenaltyScale = m_randomParameterHelper.GetWithDefault("ee_force_penalty", 0.0f);
        m_irrigationSwitchPenaltyScale = m_randomParameterHelper.GetWithDefault("irrigation_switch_penalty", 0.0f);
        m_irrigationRewardScale = m_randomParameterHelper.GetWithDefault("irrigation_reward", 0.2f);
        m_irrigationPenaltyScale = m_randomParameterHelper.GetWithDefault("irrigation_penalty", 0.0f);
        m_completionReward = m_randomParameterHelper.GetWithDefault("completion_reward", 5.0f);
        m_disallowSwitchOff = m_randomParameterHelper.GetWithDefault("disallow_switch_off", 0.0f) > 0;
        m_irrigationSwitchOnLocationPenalty = m_randomParameterHelper.GetWithDefault("switch_on_location_penalty", 0.0f);
        m_irrigationSwitchOnLocationReward = m_randomParameterHelper.GetWithDefault("switch_on_location_reward", 0.05f);
        m_irrigationSwitchOnLocationPenaltyDistanceThreshold = m_randomParameterHelper.GetWithDefault("switch_on_location_penalty_distance_threshold", 0.2f);
        m_completionThreshold = Mathf.RoundToInt(m_randomParameterHelper.GetWithDefault("completion_threshold", 80f));
        m_approachingRewardScale = m_randomParameterHelper.GetWithDefault("approaching_reward", 10f);

        m_friction = m_randomParameterHelper.GetWithDefault("friction", 0.5f);
        m_viscocity = m_randomParameterHelper.GetWithDefault("viscocity", 5.0f);
        m_surfaceTension = m_randomParameterHelper.GetWithDefault("surface_tension", 0.5f);
        m_cohesion = m_randomParameterHelper.GetWithDefault("cohesion", 20.0f);
        m_bloodAmount = m_randomParameterHelper.GetWithDefault("blood_amount", 1.0f);
        m_numRandomInitialBloodRegions = Mathf.RoundToInt(m_randomParameterHelper.GetWithDefault("blood_regions", 1.0f));

        m_robotForceRewarder.PenaltyScale = m_eeForcePenaltyScale;

        m_liquidSourceFriction = m_randomParameterHelper.GetWithDefault("liquid_source_friction", 0.1f);
        m_liquidSourceViscocity = m_randomParameterHelper.GetWithDefault("liquid_source_viscocity", 5.0f);
        m_liquidSourceSurfaceTension = m_randomParameterHelper.GetWithDefault("liquid_source_surface_tension", 0.5f);
        m_liquidSourceCohesion = m_randomParameterHelper.GetWithDefault("liquid_source_cohesion", 5.0f);

    }

    protected override Color GetRandomBloodColor()
    {
        return UnityEngine.Random.ColorHSV(15f / 360f, 25f / 360f, 0.85f, 1.0f, 0.6f, 0.7f);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);
        sensor.AddObservation(m_isIrrigating);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (m_inEpisode)
        {
            float[] targetJointPositions = new float[5];

            for (int i = 0; i < 5; i++)
            {
                targetJointPositions[i] = m_robotController.JointPositionSetpoint[i];
                targetJointPositions[i] += actions.ContinuousActions[i] * m_actionScale[i];
            }
            m_robotController.DriveJoints(targetJointPositions);
            // Penalty when turning off
            if (m_isIrrigating && actions.DiscreteActions[0] == 0)
            {
                // Irrigation on/off switch penalty
                AddSuctionIrrigationReward(-m_irrigationSwitchPenaltyScale);
                if (m_irrigationSwitchOnLocationPenalty !=0 && !IsLiquidSourceCloseToBlood())
                {
                    AddSuctionIrrigationReward(-m_irrigationSwitchOnLocationPenalty);
                }
            }
            m_isIrrigating = actions.DiscreteActions[0] > 0 | (m_disallowSwitchOff && m_isIrrigating);
            m_liquidSource.IsActive = m_isIrrigating;
            if (m_isIrrigating)
            {
                AddSuctionIrrigationReward(-m_irrigationPenaltyScale);
            }
        }
    }

    public void AddIrrigationReward(int numParticlesAffected)
    {
        float r = numParticlesAffected * m_irrigationRewardScale;
        AddSuctionIrrigationReward(r);
    }

    private void RandomizeLiquidSource()
    {
        m_liquidSource.PBDMaterial.Friction = m_liquidSourceFriction;
        m_liquidSource.PBDMaterial.Viscosity = m_liquidSourceViscocity;
        m_liquidSource.PBDMaterial.SurfaceTension = m_liquidSourceSurfaceTension;
        m_liquidSource.PBDMaterial.Cohesion = m_liquidSourceCohesion;
        m_liquidSource.ResetObject();
        m_liquidSource.FluidColor = GetRandomLiquidSourceColor();
    }

    private Color GetRandomLiquidSourceColor()
    {
        return UnityEngine.Random.ColorHSV(00f / 360f, 40f / 360f, 0.8f, 1.0f, 0.9f, 1.0f);
    }

    private static float HorizontalDistanceSquared(Vector3 a, Vector3 b)
    {
        Vector3 diff = a - b;
        return diff.x * diff.x + diff.z * diff.z; // Ignoring the y component for horizontal distance
    }

    private bool IsLiquidSourceCloseToBlood()
    {
        Vector3 liquidSourcePosition = m_liquidSource.transform.position;
        float distanceThreshSquared = m_irrigationSwitchOnLocationPenaltyDistanceThreshold * m_irrigationSwitchOnLocationPenaltyDistanceThreshold;
        foreach (Vector3 p in m_bloodFluid.ParticleData.PositionInvMass)
        {
            if (HorizontalDistanceSquared(p, liquidSourcePosition) < distanceThreshSquared)
            {
                return true;
            }
        }
        return false;
    }

    private float GetLiquidSourceToBloodHorizontalDistance()
    {
        float dist = float.MaxValue;
        Vector3 liquidSourcePosition = m_liquidSource.transform.position;
        float distanceThreshSquared = m_irrigationSwitchOnLocationPenaltyDistanceThreshold * m_irrigationSwitchOnLocationPenaltyDistanceThreshold;
        foreach (Vector3 p in m_bloodFluid.ParticleData.PositionInvMass)
        {
            float d = Mathf.Sqrt(HorizontalDistanceSquared(p, liquidSourcePosition));
            if (d < dist) dist = d;
        }
        return dist;
    }

    protected override void HeurisiticKeyboardInput(in ActionBuffers actionsOut)
    {
        base.HeurisiticKeyboardInput(actionsOut);
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = Mathf.RoundToInt(Input.GetAxisRaw("Fire1"));
    }

    protected override void HeurisiticScriptedInput(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        float avgX = 0;
        float avgY = 0;
        float avgZ = 0;
        Vector3 p;
        foreach (int idx in m_activeBloodIndices)
        {
            p = m_bloodFluid.ParticleData.PositionInvMass[idx];

            avgX += p.x;
            avgY += p.y;
            avgZ += p.z;
        }
        avgX /= m_bloodFluid.ParticleData.NumParticles;
        avgY /= m_bloodFluid.ParticleData.NumParticles;
        avgZ /= m_bloodFluid.ParticleData.NumParticles;

        m_demoTargetEEPose.position.x = avgX;
        m_demoTargetEEPose.position.z = avgZ;
        m_demoTargetEEPose.position.y = avgY + 0.6f;  // A hardcoded height above the blood.
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

        var discreteActionsOut = actionsOut.DiscreteActions;
        float diffX = m_demoEETooltip.position.x - avgX;
        float diffZ = m_demoEETooltip.position.z - avgZ;
        if ((diffX * diffX + diffZ * diffZ) < 0.05f)
        {
            discreteActionsOut[0] = 1;
        }
        else
        {
            discreteActionsOut[0] = 0;
        }
    }

    protected override void HeurisiticMTMInput(in ActionBuffers actionsOut)
    {
        base.HeurisiticMTMInput(actionsOut);
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = GetComponent<MTMTeleoperationJoystick>().GripperClosed ? 1 : 0;
    }

    protected override void ResetRobot()
    {
        m_robotController.Robot.ResetObject();
        if (m_randomInitialRobotPose)
        {
            float j1, j2, j3, j4, j5;
            j1 = UnityEngine.Random.Range(0.0f, 0.3f);
            j2 = UnityEngine.Random.Range(-0.2f, 0.2f);
            j3 = UnityEngine.Random.Range(-4.2f, -3.9f);
            j4 = UnityEngine.Random.Range(-0.3f, 0.3f);
            j5 = UnityEngine.Random.Range(-0.3f, 0.3f);
            m_robotController.DriveJoints(new float[] { j1, j2, j3, j4, j5 });
        }
        else
        {
            m_robotController.DriveJoints(new float[] { 0, 0, -1.8f, 0, 0 });
        }
    }

    protected override void InitializeEvalRecorder()
    {
        base.InitializeEvalRecorder();
        if (m_evalRecorder != null)
        {
            m_evalRecorder.RecordLine(m_evalRecorderFileName, "reward,success");
        }
    }

    protected override void RecordEval()
    {
        if (m_evalRecorder != null)
        {
            m_evalRecorder.RecordLine(m_evalRecorderFileName, CummulativeReward.ToString() + "," + m_isSuccess.ToString());
#if UNITY_EDITOR
            if (CompletedEpisodes >= m_numEvalEpisodes)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
#endif
        }
    }

    [SerializeField]
    CustomDiffuseMaterialFluidSourceActor m_liquidSource;
    [SerializeField]
    CustomFluidDiffuseMaterialRenderer m_fluidRenderer;
    [SerializeField]
    private Transform m_demoEETooltip;

    private bool m_isIrrigating = false;
    private float m_irrigationPenaltyScale;
    private float m_irrigationSwitchPenaltyScale;

    private float m_liquidSourceFriction;
    private float m_liquidSourceViscocity;
    private float m_liquidSourceSurfaceTension;
    private float m_liquidSourceCohesion;
    private float m_irrigationRewardScale;
    private int m_completionThreshold;
    private bool m_disallowSwitchOff;
    private float m_irrigationSwitchOnLocationPenalty;
    private float m_irrigationSwitchOnLocationReward;
    private float m_irrigationSwitchOnLocationPenaltyDistanceThreshold;
    private int m_taskLesson;
    private float m_lastDistance;
    private float m_approachingRewardScale;

    private PxTransformData m_demoTargetEEPose;
}

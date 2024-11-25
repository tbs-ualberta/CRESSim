using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PhysX5ForUnity;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;

public class SuctionIrrigationAgentBase : Agent
{
    public bool InEpisode
    {
        get { return m_inEpisode; }
        set { m_inEpisode = value; }
    }

    public float CummulativeReward
    {
        get { return m_cummulativeReward; }
        set { m_cummulativeReward = value; }
    }

    public bool Evaluation
    {
        get { return m_evaluation; }
    }

    public List<int> ActiveBloodIndices
    {
        get { return m_activeBloodIndices; }
    }

    protected virtual void Start()
    {
        if (m_randomMaterials)
        {
            m_initialTableColor = m_meshRendererVisualTable.material.color;
            m_initialTissueColor = m_meshRendererTissue.material.color;
            m_initialRobotLink4Color = m_meshRendererRobotLink4.material.color;
        }
        if (m_evaluation)
        {
            UnityEngine.Random.InitState(m_evalSeed);
            m_randomParameterHelper.InitializeSampler(m_evalSeed);
        }
        InitializeEvalRecorder();
        if (m_randomLighting)
        {
            m_initialTransformLight = m_light.transform.ToPxTransformData();
        }
        if (m_randomCamera)
        {
            m_initialTransformCamera = m_camera.transform.ToPxTransformData();
        }
    }

    public override void OnEpisodeBegin()
    {
        // used with DelayedDecisionRequestor
        m_inEpisode = false;

        // for evaluation
        // Debug.Log(m_cummulativeReward);
        RecordEval();
        m_cummulativeReward = 0;
        m_isSuccess = false;

        m_randomParameterHelper.IsEval = m_evaluation;

        GetCommonRandomParameters();

        ResetRobot();
        ResetTissue();

        RandomizeBlood();
        RandomizeOtherVisual();

        // For collecting demonstration
        if (m_collectDemos)
        {
            PrepareDemo();
        }
    }

    protected virtual void PrepareDemo()
    {
        if (!m_scriptedDemos) GetComponent<MTMTeleoperationJoystick>().IsMTMInitialized = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (m_useSetpointObservation)
        {
            for (int i = 0; i < 5; i++)
            {
                sensor.AddObservation(m_robotController.JointPositionSetpoint[i]);
            }
        }
        else
        {
            for (int i = 0; i < 5; i++)
            {
                sensor.AddObservation(m_robotController.Robot.JointPositions[i]);
            }
        }
        if (m_contactObservation)
        {
            sensor.AddObservation(m_robotForceRewarder.InContact);
        }
    }

    public void AddSuctionIrrigationReward(float r)
    {
        m_cummulativeReward += r;
        AddReward(r);
    }

    public void AddActionPenalty(ActionBuffers actions)
    {
        float dx = actions.ContinuousActions[0];
        float dy = actions.ContinuousActions[1];
        float motionPenalty = Mathf.Sqrt(dx * dx + dy * dy);
        float r = -m_actionPenaltyScale * motionPenalty;
        AddSuctionIrrigationReward(r); // 0.04
    }

    public void AddEEPoseReward(float angle)
    {
        AddSuctionIrrigationReward(-m_eePosePenaltyScale * angle);
    }

    public void AddCompletionReward()
    {
        // Success
        m_isSuccess = true;
        AddSuctionIrrigationReward(m_completionReward);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (m_collectDemos)
        {
            if (m_scriptedDemos)
            {
                HeurisiticScriptedInput(actionsOut);
            }
            else
            {
                HeurisiticMTMInput(actionsOut);
            }
        }
        else
        {
            HeurisiticKeyboardInput(actionsOut);
        }
    }

    protected virtual void HeurisiticKeyboardInput(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxisRaw("Horizontal");
        continuousActionsOut[1] = Input.GetAxisRaw("Vertical");
        continuousActionsOut[2] = Input.GetAxisRaw("3rd axis");
        continuousActionsOut[3] = Input.GetAxisRaw("4th axis");
        continuousActionsOut[4] = Input.GetAxisRaw("5th axis");
    }

    protected virtual void HeurisiticScriptedInput(in ActionBuffers actionsOut)
    {
        
    }

    protected virtual void HeurisiticMTMInput(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        MTMTeleoperationJoystick joystick = GetComponent<MTMTeleoperationJoystick>();
        float[] jpDiff = joystick.PsmJointPosDiff;
        for (int i = 0; i < jpDiff.Length; i++)
        {
            continuousActionsOut[i] = Mathf.Clamp(joystick.PsmJointPosDiff[i], -1, 1);// / m_actionScale[0] * 0.5f;
        }
    }

    protected virtual void GetCommonRandomParameters()
    {
        // action penalty scale
        m_actionPenaltyScale = m_randomParameterHelper.GetWithDefault("action_penalty", 0.02f);
        m_eePosePenaltyScale = m_randomParameterHelper.GetWithDefault("ee_pose_penalty", 0.0005f);
        m_eeForcePenaltyScale = m_randomParameterHelper.GetWithDefault("ee_force_penalty", 0.0f);
        m_completionReward = m_randomParameterHelper.GetWithDefault("completion_reward", 5.0f);

        m_friction = m_randomParameterHelper.GetWithDefault("friction", 0.05f);
        //float density = m_randomParameterHelper.GetWithDefault("density", 1000.0f);
        m_viscocity = m_randomParameterHelper.GetWithDefault("viscocity", 5.0f);
        m_surfaceTension = m_randomParameterHelper.GetWithDefault("surface_tension", 0.5f);
        m_cohesion = m_randomParameterHelper.GetWithDefault("cohesion", 5.0f);
        m_bloodAmount = m_randomParameterHelper.GetWithDefault("blood_amount", 1.0f);
        m_numRandomInitialBloodRegions = (int)m_randomParameterHelper.GetWithDefault("blood_regions", 1.0f);

        m_robotForceRewarder.PenaltyScale = m_eeForcePenaltyScale;
    }

    protected virtual void ResetRobot()
    {
        m_robotController.Robot.ResetObject();
        if (m_randomInitialRobotPose)
        {
            float j1, j2, j3, j4, j5;
            j1 = UnityEngine.Random.Range(0.0f, 0.3f);
            j2 = UnityEngine.Random.Range(-0.2f, 0.2f);
            j3 = UnityEngine.Random.Range(-4.5f, -4.0f);
            j4 = UnityEngine.Random.Range(-0.3f, 0.3f);
            j5 = UnityEngine.Random.Range(-0.3f, 0.3f);
            m_robotController.DriveJoints(new float[] { j1, j2, j3, j4, j5 });
        }
        else
        {
            m_robotController.DriveJoints(new float[] { 0, 0, -1.8f, 0, 0 });
        }
    }

    protected virtual void ResetTissue()
    {
        m_meshGenerator.NumControlPoints = 11;
        Mesh mesh = m_meshGenerator.GenerateMesh();
        ((PhysxTriangleMeshGeometry)m_tissueShape.Geometry).Mesh = mesh;
        m_tissueShape.GetComponent<MeshFilter>().mesh = mesh;
        m_tissueShape.Geometry.Recreate();
        m_tissueShape.Recreate();
        m_tissue.Recreate();
    }

    protected virtual void RandomizeBlood()
    {
        m_bloodFluid.PBDMaterial.Friction = m_friction;
        m_bloodFluid.PBDMaterial.Viscosity = m_viscocity;
        m_bloodFluid.PBDMaterial.SurfaceTension = m_surfaceTension;
        m_bloodFluid.PBDMaterial.Cohesion = m_cohesion;
        m_bloodFluid.ResetObject();
        m_bloodFluid.ParticleData.SyncParticlesGet();

        // TODO: This is so ugly. Why don't use ActiveParticleIndices from the fluid actor?
        int startIdx = 0;
        m_activeBloodIndices.Clear();
        for (int i = 0; i < m_bloodFluid.ParticleData.NumParticles; i++)
        {
            m_activeBloodIndices.Add(i);
        }
        if (m_bloodAmount < 1.0f)
        {
            startIdx = RemoveFluidByProportion(1 - m_bloodAmount, m_activeBloodIndices);
        }
        if (m_randomInitialBloodLocation)
        {
            float minDistanceBetweenPositions = 1f;
            List<Vector3> generatedPositions = new List<Vector3>();
            for (int i = 0; i < m_numRandomInitialBloodRegions; i++)
            {
                int endIdx = m_activeBloodIndices.Last() + 1;
                if (i < m_numRandomInitialBloodRegions - 1)
                {
                    // minimum 100 particles per region. Some hack to insure each segment is larger than 50;
                    int randStart = startIdx + m_minRegionParticleNum;
                    int randEnd = endIdx - (m_numRandomInitialBloodRegions - i + 1) * m_minRegionParticleNum;
                    if (randStart >= randEnd) break;
                    endIdx = UnityEngine.Random.Range(randStart, randEnd);
                }

                // By GPT: ensure distance between each initial region
                // The min distance cannot [exceed / be close to] the max possible value. It causes infinite loops or takes a very long time
                Vector3 newLocalPosition;
                bool positionValid;

                do
                {
                    newLocalPosition = new Vector3
                    {
                        x = UnityEngine.Random.Range(-1.0f, 1.0f),
                        y = 0,
                        z = UnityEngine.Random.Range(-1.0f, 1.0f)
                    };

                    positionValid = true;

                    foreach (var pos in generatedPositions)
                    {
                        if (Vector3.Distance(newLocalPosition, pos) < minDistanceBetweenPositions)
                        {
                            positionValid = false;
                            break;
                        }
                    }
                } while (!positionValid);
                generatedPositions.Add(newLocalPosition);

                ReinitializeFluidPosition(newLocalPosition, startIdx, endIdx);
                startIdx = endIdx;
            }
        }
        m_bloodFluid.ParticleData.SyncParticlesSet(true);
    }

    protected virtual void RandomizeOtherVisual()
    {
        if (m_randomMaterials)
        {
            m_meshRendererVisualTable.material.SetColor("_Color", GenerateRandomColorAround(m_initialTableColor, 0.1f, 0.1f, 0.1f));
            m_meshRendererTissue.material.SetColor("_Color", GenerateRandomColorAround(m_initialTissueColor, 0.05f, 0.1f, 0.1f));
            m_meshRendererRobotLink4.material.SetColor("_Color", GenerateRandomColorAround(m_initialRobotLink4Color, 0.05f, 0.1f, 0.1f));
            m_bloodFluid.FluidColor = GetRandomBloodColor();
        }
        if (m_randomLighting)
        {
            m_light.transform.position = m_initialTransformLight.position;
            float rotDiffX = m_randomParameterHelper.GetWithDefault("light_rotation_diff_x", 0.0f);
            float rotDiffY = m_randomParameterHelper.GetWithDefault("light_rotation_diff_y", 0.0f);
            float rotDiffZ = m_randomParameterHelper.GetWithDefault("light_rotation_diff_z", 0.0f);
            float intensity = m_randomParameterHelper.GetWithDefault("light_intensity", 1.0f);
            float shadowStrength = m_randomParameterHelper.GetWithDefault("light_shadow_strength", 0.5f);
            Quaternion newRotation = new Quaternion()
            {
                eulerAngles = m_initialTransformLight.quaternion.eulerAngles + new Vector3(rotDiffX, rotDiffY, rotDiffZ)
            };
            m_light.transform.rotation = newRotation;

            m_light.shadowStrength = shadowStrength;
            m_light.intensity = intensity;
        }
        if (m_randomCamera)
        {
            float posDiffX = m_randomParameterHelper.GetWithDefault("camera_position_diff_x", 0.0f);
            float posDiffY = m_randomParameterHelper.GetWithDefault("camera_position_diff_y", 0.0f);
            float posDiffZ = m_randomParameterHelper.GetWithDefault("camera_position_diff_z", 0.0f);
            float rotDiffX = m_randomParameterHelper.GetWithDefault("camera_rotation_diff_x", 0.0f);
            float rotDiffY = m_randomParameterHelper.GetWithDefault("camera_rotation_diff_y", 0.0f);
            float rotDiffZ = m_randomParameterHelper.GetWithDefault("camera_rotation_diff_z", 0.0f);
            Vector3 newPosition = m_initialTransformCamera.position + new Vector3(posDiffX, posDiffY, posDiffZ);
            Quaternion newRotation = new Quaternion()
            {
                eulerAngles = m_initialTransformCamera.quaternion.eulerAngles + new Vector3(rotDiffX, rotDiffY, rotDiffZ)
            };
            m_camera.transform.position = newPosition;
            m_camera.transform.rotation = newRotation;
        }
    }

    protected virtual Color GetRandomBloodColor()
    {
        return UnityEngine.Random.ColorHSV(0f, 40f / 360f, 0.8f, 1.0f, 0.8f, 1.0f);
    }

    // Generates a random color that is within hueDiffRange, saturationDiffRange, and valueDiffRange of initialColor
    protected Color GenerateRandomColorAround(Color initialColor, float hueDiffRange, float saturationDiffRange, float valueDiffRange)
    {
        Color.RGBToHSV(initialColor, out float h, out float s, out float v);

        float newH = (h + UnityEngine.Random.Range(-hueDiffRange, hueDiffRange)) % 1.0f;
        if (newH < 0) newH += 1.0f;  // Ensure the hue stays within the 0-1 range

        float newS = Mathf.Clamp(s + UnityEngine.Random.Range(-saturationDiffRange, saturationDiffRange), 0.0f, 1.0f);
        float newV = Mathf.Clamp(v + UnityEngine.Random.Range(-valueDiffRange, valueDiffRange), 0.0f, 1.0f);
    
        return Color.HSVToRGB(newH, newS, newV);
    }

    protected int RemoveFluidByProportion(float p, List<int> activeIndices)
    {
        int removeUntilIndex = (int)(m_bloodFluid.NumParticles * p);
        Vector4 removalTeleportPosition = new Vector4(5.0f + transform.position.x, -5.0f + transform.position.y, -5.0f + transform.position.z, 0.0f);
        for (int i = 0; i < removeUntilIndex; ++i)
        {
            m_bloodFluid.ParticleData.SetParticle(i, removalTeleportPosition, true);
            activeIndices.Remove(i);
        }
        m_bloodFluid.ParticleData.SyncParticlesSet();
        return removeUntilIndex;
    }

    protected void ReinitializeFluidPosition(Vector3 startPosition, int startIdx, int endIdx)
    {
        ArraySegment<Vector4> positionInvMass = m_bloodFluid.ParticleData.PositionInvMass;
        float particleSpacing = m_bloodFluid.PBDParticleSystem.ParticleSpacing;
        int totalParticles = endIdx - startIdx;

        int dimXZ = Mathf.CeilToInt(Mathf.Pow(totalParticles, 1f / 3f));  // X and Z dimensions
        int dimY = Mathf.CeilToInt((float)totalParticles / (dimXZ * dimXZ));  // Y dimension

        startPosition = m_bloodFluid.transform.position + startPosition - new Vector3(0.5f * dimXZ * particleSpacing, 0.3f, 0.5f * dimXZ * particleSpacing);

        int currentIdx = startIdx;

        for (int y = 0; y < dimY; y++)
        {
            for (int z = 0; z < dimXZ; z++)
            {
                for (int x = 0; x < dimXZ; x++)
                {
                    if (currentIdx >= endIdx)
                        return;

                    Vector3 newPosition = startPosition + particleSpacing * new Vector3(x, y, z);
                    positionInvMass[currentIdx] = new Vector4(newPosition.x, newPosition.y, newPosition.z, positionInvMass[currentIdx].w);
                    m_bloodFluid.ParticleData.SetVelocity(currentIdx, Vector3.zero, false);

                    currentIdx++;
                }
            }
        }
    }

    protected virtual void InitializeEvalRecorder()
    {
        if (m_evalRecorder != null)
        {
            m_evalRecorder.InitializeFile(m_evalRecorderFileName);
        }
    }

    protected virtual void RecordEval()
    {

    }

    protected float m_friction;
    protected float m_viscocity;
    protected float m_surfaceTension;
    protected float m_cohesion;
    protected float m_actionPenaltyScale;
    private float m_cummulativeReward;
    protected bool m_inEpisode;
    protected PxTransformData m_initialTransformLight;
    protected PxTransformData m_initialTransformCamera;
    protected Color m_initialTableColor;
    protected Color m_initialRobotColor;
    protected Color m_initialTissueColor;
    protected Color m_initialRobotLink4Color;
    protected List<int> m_activeBloodIndices = new List<int>();
    protected bool m_isSuccess;


    [SerializeField]
    protected PSMControllerBase m_robotController;
    [SerializeField]
    protected PhysxFluidArrayActor m_bloodFluid;
    [SerializeField]
    protected PhysxActor m_tissue;
    [SerializeField]
    protected PhysxShape m_tissueShape;
    [SerializeField]
    protected float[] m_actionScale;
    [SerializeField]
    protected TissueMeshGenerator m_meshGenerator;
    [SerializeField]
    protected bool m_randomMaterials;
    [SerializeField]
    protected bool m_randomInitialRobotPose = false;
    [SerializeField, InspectorRange(0.0f, 1.0f)]
    protected float m_bloodAmount = 1.0f;
    [SerializeField]
    protected bool m_randomInitialBloodLocation;
    [SerializeField]
    protected int m_numRandomInitialBloodRegions = 1;
    [SerializeField]
    protected int m_minRegionParticleNum = 50;
    [SerializeField]
    protected bool m_randomLighting;
    [SerializeField]
    protected Light m_light;
    [SerializeField]
    protected bool m_randomCamera;
    [SerializeField]
    protected Camera m_camera;
    [SerializeField]
    protected MeshRenderer m_meshRendererVisualTable;
    [SerializeField]
    protected MeshRenderer m_meshRendererTissue;
    [SerializeField]
    protected MeshRenderer m_meshRendererRobotLink4;
    [SerializeField]
    protected bool m_collectDemos;
    [SerializeField]
    protected bool m_scriptedDemos;

    protected float m_eePosePenaltyScale;
    protected float m_eeForcePenaltyScale;

    [SerializeField]
    protected bool m_evaluation = false;
    [SerializeField]
    protected int m_evalSeed = 12345;
    [SerializeField]
    protected EvalRecorder m_evalRecorder;
    [SerializeField]
    protected string m_evalRecorderFileName;
    [SerializeField]
    protected float m_numEvalEpisodes = 50;
    [SerializeField]
    protected bool m_useSetpointObservation = false;
    [SerializeField]
    protected bool m_contactObservation = false;
    [SerializeField]
    protected RobotForceRewarder m_robotForceRewarder;
    [SerializeField]
    protected float m_completionReward;
    [SerializeField]
    protected RandomParameterHelper m_randomParameterHelper;

}

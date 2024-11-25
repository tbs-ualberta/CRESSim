using System.Collections;
using System.Collections.Generic;
using PhysX5ForUnity;
using UnityEngine;

public class MTMTeleoperationJoystick : MonoBehaviour
{
    public bool InEpisode
    {
        get { return m_inEpisode; }
        set { m_inEpisode = value; }
    }

    public bool IsMTMInitialized
    {
        get { return m_isMTMInitialized; }
        set
        {
            m_isMTMInitialized = value; 
            if (!value) m_isTeleoperating = false; // Reset teleoperating state
        }
    }

    public float[] PsmJointPosDiff
    {
        get { return m_psmJointPosDiff; }
        set { m_psmJointPosDiff = value; }
    }

    public bool GripperClosed
    {
        get { return m_MtmRos.GripperClosed; }
    }
    
    void Start()
    {
        m_psmMtmRotationOffset1 = m_psmBase.rotation * Quaternion.Inverse(m_rotationPsmMtm);
        m_psmMtmRotationOffset2 = Quaternion.Inverse(m_rotationEE);
    }

    void FixedUpdate()
    {
        if (!m_isMTMInitialized && Time.fixedTime > m_delayedStart) // Some hack to let ROS connector read the topic first
        {
            InitializeMTM();
            m_isMTMInitialized = true;
        }

        if (m_inEpisode && m_isTeleoperating)
        {
            m_tick++;
            if (m_tick >= m_updateFrequency)
            {
                m_tick = 0;
                TeleoperationMainLoop();
            }
        }
    }

    public void TeleoperationMainLoop()
    {
        PxTransformData mtmPoseCurrent = m_MtmRos.MeasuredCp;
        Vector3 diffPosition = mtmPoseCurrent.position - m_mtmPoseLast.position;

        // Based on current Cartesian setpoint, do FK once
        float[] jpForFk = ((PSMSuctionIrrigator6DofControllerMod)m_psmController).GetExtendedJointPositionSetPoint(m_psmController.JointPositionSetpoint);
        m_psmPoseTarget = m_psmController.Robot.ForwardKinematics(jpForFk).ToPxTransformData();

        m_psmPoseTarget.position += m_teleopScale *( m_psmMtmRotationOffset1 * diffPosition);
        m_psmPoseTarget.quaternion = RotationMTMToPSM(mtmPoseCurrent.quaternion) * Quaternion.Euler(-90, 90, 0); // To EE joint on self
        m_mtmPoseLast = mtmPoseCurrent;

        // Extended 6-element current jp setpoint
        m_psmTargetJointPos = ((PSMSuctionIrrigator6DofControllerMod)m_psmController).GetExtendedJointPositionSetPoint(m_psmController.JointPositionSetpoint);
        bool success = m_psmController.Robot.InverseKinematics(m_psmTargetJointPos, m_psmPoseTarget);
        if (success)
        {
            float[] currentJpSetpoint = m_psmController.JointPositionSetpoint;
            m_psmJointPosDiff[0] = m_actionScale * (m_psmTargetJointPos[0] - currentJpSetpoint[0]);
            m_psmJointPosDiff[1] = m_actionScale * (m_psmTargetJointPos[1] - currentJpSetpoint[1]);
            m_psmJointPosDiff[2] = m_actionScale * (m_psmTargetJointPos[2] - currentJpSetpoint[2]);
            m_psmJointPosDiff[3] = m_actionScale * (m_psmTargetJointPos[4] - currentJpSetpoint[3]);
            m_psmJointPosDiff[4] = m_actionScale * (m_psmTargetJointPos[5] - currentJpSetpoint[4]);
            
        }
        else
        {
            m_psmJointPosDiff[0] = 0;
            m_psmJointPosDiff[1] = 0;
            m_psmJointPosDiff[2] = 0;
            m_psmJointPosDiff[3] = 0;
            m_psmJointPosDiff[4] = 0;
        }
    }

    public Quaternion RotationMTMToPSM(Quaternion mtmRot)
    {
        return m_psmMtmRotationOffset1 * mtmRot * m_psmMtmRotationOffset2;
    }

    public void InitializeMTM()
    {
        StartCoroutine(InitializeMTMCoroutine());
    }

    public IEnumerator InitializeMTMCoroutine()
    {
        if (!m_MtmRos.OperatingState.IsHomed)
        {
            Debug.Log("MTM not homed.");
        }
        float[] jpForFk = ((PSMSuctionIrrigator6DofControllerMod)m_psmController).GetExtendedJointPositionSetPoint(m_psmController.JointPositionSetpoint);
        PxTransformData fk = m_psmController.Robot.ForwardKinematics(jpForFk).ToPxTransformData();
        fk.quaternion *= Quaternion.Inverse(Quaternion.Euler(-90, 90, 0)); // Tooltip pose
        m_MtmRos.AlignMTMWithPSM(
            fk,
            m_rotationPsmMtm,
            m_psmBase.rotation,
            m_rotationEE,
            3.0f,
            false);
        yield return StartCoroutine(m_MtmRos.WaitForBusyCoroutine(2.0f)); // Wait for first move to complete

        m_MtmRos.UseGravityCompensation(true);
        Vector3 posStiff = new Vector3(0f, 0f, 0f);
        Vector3 posDamping = new Vector3(-2f, -2f, -2f);
        Vector3 oriStiff = new Vector3(0f, 0f, 0f);
        Vector3 oriDamping = new Vector3(-0.001f, -0.001f, -0.001f);
        m_MtmRos.SetCartesianImpedanceGains(posStiff, posDamping, oriStiff, oriDamping);

        m_isTeleoperating = true;
        m_mtmPoseLast = m_MtmRos.MeasuredCp;
        m_psmPoseTarget = m_psmController.Robot.ForwardKinematics(jpForFk).ToPxTransformData();
        m_psmTargetJointPos = m_psmController.JointPositionSetpoint;
    }

    [SerializeField] private float m_teleopScale;
    [SerializeField] private MTMROSConnector m_MtmRos;
    [SerializeField] private Transform m_psmBase;
    [SerializeField] private Transform m_psmBaseJoint;
    [SerializeField] private Transform m_psmEETooltip;
    [SerializeField] private PSMControllerBase m_psmController;
    [SerializeField] private float m_actionScale = 200;
    [SerializeField] private int m_updateFrequency = 4;
    private Quaternion m_rotationPsmMtm = new Quaternion(0, 0.70710678f, 0, 0.70710678f);
    private Quaternion m_rotationEE = new Quaternion(0, 0.70710678f, 0, 0.70710678f);

    private bool m_isMTMInitialized = false;
    private bool m_isTeleoperating = false;
    private PxTransformData m_mtmPoseLast;
    private PxTransformData m_psmPoseTarget;
    private Quaternion m_psmMtmRotationOffset1;
    private Quaternion m_psmMtmRotationOffset2;
    private bool m_inEpisode = false;
    private float m_delayedStart = 0.1f;
    private float[] m_psmJointPosDiff = new float[5];
    private float[] m_psmTargetJointPos = new float[5];
    private int m_tick = 0;
}

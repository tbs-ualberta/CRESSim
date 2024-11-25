using System.Collections;
using PhysX5ForUnity;
using UnityEngine;

public abstract class MTMTeleoperationControllerBase : MonoBehaviour
{
    void Start()
    {
        m_psmMtmRotationOffset1 = m_psmBase.rotation * Quaternion.Inverse(m_rotationPsmMtm);
        m_psmMtmRotationOffset2 = Quaternion.Inverse(m_rotationEE);
    }

    void FixedUpdate()
    {
        if (!m_isPSMInitialized)
        {
            InitializePSM();
            m_isPSMInitialized = true;
        }
        if (Time.fixedTime > m_delayedStart)
        {
            if (!m_isMTMInitialized)
            {
                StartTeleoperation();
                m_isMTMInitialized = true;
            }
        }

        if (m_isTeleoperating)
        {
            TeleoperationMainLoop();
        }
    }

    protected virtual void InitializePSM()
    {
        m_psmController.DriveJoints(new float[] { 0f, 0f, -0.5f, 0, 0, 0});
    }

    protected virtual void TeleoperationMainLoop()
    {
        PxTransformData mtmPoseCurrent = m_MtmRos.MeasuredCp;
        Vector3 diffPosition = mtmPoseCurrent.position - m_mtmPoseLast.position;
        m_psmPoseTarget.position += m_teleopScale *( m_psmMtmRotationOffset1 * diffPosition);
        m_psmPoseTarget.quaternion = RotationMTMToPSM(mtmPoseCurrent.quaternion);
        m_psmController.DriveCartesianPose(m_psmPoseTarget);
        m_mtmPoseLast = mtmPoseCurrent;
    }

    public Quaternion RotationMTMToPSM(Quaternion mtmRot)
    {
        return m_psmMtmRotationOffset1 * mtmRot * m_psmMtmRotationOffset2;
    }

    public void StartTeleoperation()
    {
        StartCoroutine(StartTeleoperationCoroutine());
    }

    public IEnumerator StartTeleoperationCoroutine()
    {
        if (!m_MtmRos.OperatingState.IsHomed)
        {
            Debug.Log("MTM not homed.");
        }
        m_MtmRos.AlignMTMWithPSM(m_psmEETooltip.ToPxTransformData(), m_rotationPsmMtm, m_psmBase.rotation, m_rotationEE, 3.0f);
        yield return StartCoroutine(m_MtmRos.WaitForBusyCoroutine(6.0f)); // Wait for first move to complete

        m_MtmRos.UseGravityCompensation(true);
        Vector3 posStiff = new Vector3(0f, 0f, 0f);
        Vector3 posDamping = new Vector3(-2f, -2f, -2f);
        Vector3 oriStiff = new Vector3(0f, 0f, 0f);
        Vector3 oriDamping = new Vector3(-0.001f, -0.001f, -0.001f);
        m_MtmRos.SetCartesianImpedanceGains(posStiff, posDamping, oriStiff, oriDamping);

        m_isTeleoperating = true;
        m_mtmPoseLast = m_MtmRos.MeasuredCp;
        m_psmPoseTarget = m_psmEETooltip.ToPxTransformData();
    }

    [SerializeField] protected float m_teleopScale;
    [SerializeField] protected MTMROSConnector m_MtmRos;
    [SerializeField] protected Transform m_psmBase;
    [SerializeField] protected Transform m_psmEETooltip;
    [SerializeField] protected PSMControllerBase m_psmController;
    protected Quaternion m_rotationPsmMtm = new Quaternion(0, 0.70710678f, 0, 0.70710678f);
    protected Quaternion m_rotationEE = new Quaternion(0, 0.70710678f, 0, 0.70710678f);
    protected bool m_isPSMInitialized = false;
    protected float m_delayedStart = 4f;

    protected bool m_isMTMInitialized = false;
    protected bool m_isTeleoperating = false;
    protected PxTransformData m_mtmPoseLast;
    protected PxTransformData m_psmPoseTarget;
    protected Quaternion m_psmMtmRotationOffset1;
    protected Quaternion m_psmMtmRotationOffset2;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhysX5ForUnity;
using System;

[RequireComponent(typeof(PhysxArticulationRobot))]
public abstract class PSMControllerBase : MonoBehaviour
{

    public PhysxArticulationRobot Robot
    {
        get { return m_robot; }
    }

    public float[] JointPositionSetpoint
    {
        get { return m_jointPositionSetpoint; }
        set
        {
            DriveJoints(m_jointPositionSetpoint);
            m_jointPositionSetpoint = value;
        }
    }

    private void Start()
    {
        m_psmPitchEndTransform = m_psmPitchEnd.transform;
        m_psmPitchBottomTransform = m_psmPitchBottom.transform;
        m_psmPitchBottomTranslationFromPitchEnd = m_psmPitchEndTransform.position - m_psmPitchBottomTransform.position;

        m_psmPitchTopTransform = m_psmPitchTop.transform;
        m_psmPitchTopTranslationFromPitchEnd = m_psmPitchTopJoint.transform.position - m_psmPitchTopTransform.position;

        m_rotationTooltipToJoint = m_psmEETooltipJointOnSelf.localRotation;

        m_psmPitchBackInitialRotation = m_psmPitchBack.transform.localRotation;
        m_psmPitchBackJointRelative = m_psmPitchBackJoint.transform.InverseTransformPoint(m_psmPitchBack.transform.position);

        m_psmPitchFrontInitialRotation = m_psmPitchFront.transform.localRotation;
        m_psmPitchFrontJointRelative = m_psmPitchFrontJoint.transform.InverseTransformPoint(m_psmPitchFront.transform.position);
    }

    private void FixedUpdate()
    {
        if (m_alignVisualLinks)
        {
            AlignPitchBottomAndTop();
            AlignPitchBackAndFront(m_psmPitchBack, m_psmPitchBackJoint, m_psmPitchBackJointRelative, m_psmPitchBackInitialRotation);
            AlignPitchBackAndFront(m_psmPitchFront, m_psmPitchFrontJoint, m_psmPitchFrontJointRelative, m_psmPitchFrontInitialRotation);
        }
        if (m_forceKeepAlive && m_jointPositionSetpoint != null)
        {
            DriveJoints(m_jointPositionSetpoint);
        }
    }

    private void AlignPitchBottomAndTop()
    {
        m_psmPitchBottomTransform.position = m_psmPitchEndTransform.position - m_psmPitchBottomTranslationFromPitchEnd;
        m_psmPitchTopTransform.position = m_psmPitchTopJoint.transform.position - m_psmPitchTopTranslationFromPitchEnd;
    }

    private void AlignPitchBackAndFront(GameObject link, GameObject joint, Vector3 initialRelativePosition, Quaternion initialLocalRotation)
    {
        Vector3 currentRelative = joint.transform.InverseTransformPoint(link.transform.position);
        float diffRotation = Vector3.Angle(currentRelative, initialRelativePosition);
        if (currentRelative.z < initialRelativePosition.z) diffRotation = -diffRotation; // Invert rotation according to relative z
        Quaternion additionalRotation = Quaternion.Euler(0, diffRotation, 0);
        link.transform.localRotation = initialLocalRotation * additionalRotation;
    }

    public virtual void DriveJoints(float[] jointPositions)
    {
        if (jointPositions == null || jointPositions.Length != m_robot.NumJoints)
        {
            Debug.Log(jointPositions.Length);
            Debug.Log(m_robot.NumJoints);
            throw new ArgumentException("jointPositions must have the same number of elements with the drivable joint number.");
        }

        for (int i = 0; i < m_robot.NumJoints; i++)
        {
            // Allow some computational errors
            if (jointPositions[i] < m_robot.JointLimits[i].lower - 1e-4 || jointPositions[i] > m_robot.JointLimits[i].upper + 1e-4)
            {
                Debug.Log("Joint limits reached.");
                jointPositions[i] = Mathf.Clamp(jointPositions[i], m_robot.JointLimits[i].lower, m_robot.JointLimits[i].upper);
            }
        }
        Physx.DriveJoints(m_robot.NativeObjectPtr, ref jointPositions[0]);
        m_jointPositionSetpoint = jointPositions;
    }

    public abstract void DriveCartesianPose(PxTransformData t);

    [SerializeField]
    protected PhysxArticulationRobot m_robot;
    [SerializeField]
    protected Transform m_psmEETooltipJointOnSelf;
    [SerializeField]
    private bool m_forceKeepAlive;
    [SerializeField]
    private bool m_alignVisualLinks = false;
    [SerializeField]
    protected GameObject m_psmPitchEnd;
    [SerializeField]
    protected GameObject m_psmPitchBottom;
    [SerializeField]
    protected GameObject m_psmPitchTopJoint;
    [SerializeField]
    protected GameObject m_psmPitchTop;
    [SerializeField]
    protected GameObject m_psmPitchBack;
    [SerializeField]
    protected GameObject m_psmPitchBackJoint;
    [SerializeField]
    protected GameObject m_psmPitchFront;
    [SerializeField]
    protected GameObject m_psmPitchFrontJoint;

    private Transform m_psmPitchEndTransform;
    private Transform m_psmPitchBottomTransform;
    private Vector3 m_psmPitchBottomTranslationFromPitchEnd;
    private Transform m_psmPitchTopTransform;
    private Vector3 m_psmPitchTopTranslationFromPitchEnd;
    private Quaternion m_psmPitchBackInitialRotation;
    private Vector3 m_psmPitchBackJointRelative;
    private Quaternion m_psmPitchFrontInitialRotation;
    private Vector3 m_psmPitchFrontJointRelative;
    protected Quaternion m_rotationTooltipToJoint = new Quaternion(-0.5f, 0.5f, -0.5f, 0.5f);
    protected float[] m_jointPositionSetpoint;
}

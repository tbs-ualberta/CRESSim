using System;
using PhysX5ForUnity;
using UnityEngine;

public class LargeNeedleDriverTeleoperationController : MTMTeleoperationControllerBase
{
    protected override void InitializePSM()
    {
        m_psmController.DriveJoints(new float[] { -0.5f, 0f, -1.5f, 0, 0, 0, 0});
    }

    protected override void TeleoperationMainLoop()
    {
        PxTransformData mtmPoseCurrent = m_MtmRos.MeasuredCp;
        Vector3 diffPosition = mtmPoseCurrent.position - m_mtmPoseLast.position;
        m_psmPoseTarget.position += m_teleopScale *( m_psmMtmRotationOffset1 * diffPosition);
        m_psmPoseTarget.quaternion = RotationMTMToPSM(mtmPoseCurrent.quaternion);
        if (m_MtmRos.GripperClosed)
        {
            ((PSMLargeNeedleDriverController)m_psmController).DriveCartesianPose(m_psmPoseTarget, 0.15f);
            AttachParticle();
        }
        else
        {
            ((PSMLargeNeedleDriverController)m_psmController).DriveCartesianPose(m_psmPoseTarget, 0.5f);
            DetachParticle();
        }
        m_mtmPoseLast = mtmPoseCurrent;
    }

    void AttachParticle()
    {
        if (m_attachmentHandler < 0)
        {
            Vector4[] vertices = m_softActor.CollisionMeshData.positionInvMass;
            float minDistance = float.MaxValue;
            int graspedIdx = int.MaxValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 positionParticle = vertices[i];

                float d = (m_rigidActor.transform.position - positionParticle).sqrMagnitude;
                if (d < minDistance)
                {
                    minDistance = d;
                    graspedIdx = i;
                }
            }
            if (graspedIdx < vertices.Length && minDistance < 0.5)
            {
                Vector3 fixedLocalPosition = m_rigidActor.transform.InverseTransformPoint(vertices[graspedIdx]);
                fixedLocalPosition.x *= m_rigidActor.transform.lossyScale.x;
                fixedLocalPosition.y *= m_rigidActor.transform.lossyScale.y;
                fixedLocalPosition.z *= m_rigidActor.transform.lossyScale.z;
                IntPtr pxLinkPtr = m_robot.EELinkPtrs[2];
                m_attachmentHandler = Physx.AttachFEMSoftBodyVertexToRigidBody(m_softActor.NativeObjectPtr, m_rigidActor.NativeObjectPtr, graspedIdx, ref fixedLocalPosition);
            }
        }
    }

    void DetachParticle()
    {
        if (m_attachmentHandler >= 0)
        {
            IntPtr pxLinkPtr = m_robot.EELinkPtrs[2];
            Physx.DetachFEMSoftBodyVertexFromRigidBody(m_softActor.NativeObjectPtr, m_rigidActor.NativeObjectPtr, m_attachmentHandler);
            m_attachmentHandler = -1;
        }
    }

    [SerializeField]
    private PhysxFEMSoftBodyActor m_softActor;
    [SerializeField]
    private PhysxArticulationRobot m_robot;
    [SerializeField]
    private PhysxKinematicRigidActor m_rigidActor;
    private int m_attachmentHandler;
}

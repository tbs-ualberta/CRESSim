using System;
using PhysX5ForUnity;
using UnityEngine;

public class LargeNeedleDriverClothGraspingTeleoperationController : MTMTeleoperationControllerBase
{
    protected override void InitializePSM()
    {
        m_psmController.DriveJoints(new float[] { -0.8f, 0f, -1.5f, 0, 0, 0, 0 });
    }

    protected override void TeleoperationMainLoop()
    {
        PxTransformData mtmPoseCurrent = m_MtmRos.MeasuredCp;
        Vector3 diffPosition = mtmPoseCurrent.position - m_mtmPoseLast.position;
        m_psmPoseTarget.position += m_teleopScale * (m_psmMtmRotationOffset1 * diffPosition);
        m_psmPoseTarget.quaternion = RotationMTMToPSM(mtmPoseCurrent.quaternion);
        if (m_MtmRos.GripperClosed)
        {
            ((PSMLargeNeedleDriverController)m_psmController).DriveCartesianPose(m_psmPoseTarget, 0.1f);
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
        if (m_attachedParticle < 0)
        {
            Vector4[] particles = m_clothActor.ParticleData.PositionInvMass.ToArray();
            float minDistance = float.MaxValue;
            int graspedIdx = int.MaxValue;
            for (int i = 0; i < particles.Length; i++)
            {
                Vector3 positionParticle = particles[i];

                float d = (m_attachmentPoint.position - positionParticle).sqrMagnitude;
                if (d < minDistance)
                {
                    minDistance = d;
                    graspedIdx = i;
                }
            }
            if (graspedIdx < particles.Length && minDistance < 0.02)
            {
                Vector3 fixedLocalPosition = m_rigidActor.transform.InverseTransformPoint(particles[graspedIdx]);
                fixedLocalPosition.x = 0;
                fixedLocalPosition.z = 0;
                // fixedLocalPosition.x *= m_rigidActor.transform.lossyScale.x;
                fixedLocalPosition.y *= m_rigidActor.transform.lossyScale.y;
                // fixedLocalPosition.z *= m_rigidActor.transform.lossyScale.z;
                Physx.AttachParticleToRigidBody(m_clothActor.NativeObjectPtr, graspedIdx, m_rigidActor.NativeObjectPtr, ref fixedLocalPosition);
                m_attachedParticle = graspedIdx;
            }
        }
    }

    void DetachParticle()
    {
        if (m_attachedParticle >= 0)
        {
            Physx.DetachParticleFromRigidBody(m_clothActor.NativeObjectPtr, m_attachedParticle, m_rigidActor.NativeObjectPtr);
            m_attachedParticle = -1;
        }
    }

    [SerializeField]
    private PhysxTriangleMeshClothActor m_clothActor;
    [SerializeField]
    private PhysxArticulationRobot m_robot;
    [SerializeField]
    private Transform m_attachmentPoint;
    [SerializeField]
    private PhysxKinematicRigidActor m_rigidActor;
    private int m_attachedParticle = -1;
}

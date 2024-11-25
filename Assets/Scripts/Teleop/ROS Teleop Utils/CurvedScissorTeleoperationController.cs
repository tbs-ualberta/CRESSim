using System;
using PhysX5ForUnity;
using UnityEngine;

public class CurvedScissorTeleoperationController : MTMTeleoperationControllerBase
{
    protected override void InitializePSM()
    {
        m_psmController.DriveJoints(new float[] { 0.5f, 0f, -1.5f, 0, 0, 0, 0});
    }

    protected override void TeleoperationMainLoop()
    {
        PxTransformData mtmPoseCurrent = m_MtmRos.MeasuredCp;
        Vector3 diffPosition = mtmPoseCurrent.position - m_mtmPoseLast.position;
        m_psmPoseTarget.position += m_teleopScale *( m_psmMtmRotationOffset1 * diffPosition);
        m_psmPoseTarget.quaternion = RotationMTMToPSM(mtmPoseCurrent.quaternion);
        if (m_MtmRos.GripperClosed)
        {
            ((PSMLargeNeedleDriverController)m_psmController).DriveCartesianPose(m_psmPoseTarget, -0.1f);
            m_cutterBehavior.Cutting = true;
        }
        else
        {
            ((PSMLargeNeedleDriverController)m_psmController).DriveCartesianPose(m_psmPoseTarget, 0.5f);
            m_cutterBehavior.Cutting = false;
        }
        m_mtmPoseLast = mtmPoseCurrent;
    }

    [SerializeField]
    private PhysxArticulationRobot m_robot;
    [SerializeField]
    private ClothCutter m_cutterBehavior;
    private int m_attachmentHandler;
}

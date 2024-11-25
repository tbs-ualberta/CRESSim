using PhysX5ForUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// PSM with Suction Irrigator
/// </summary>
/// 
public class PSMSuctionIrrigator6DofController : PSMControllerBase
{
    public override void DriveJoints(float[] jointPositions)
    {
        if (jointPositions == null || jointPositions.Length != 6)
        {
            throw new ArgumentException("jointPositions must be an array of 6 floats.");
        }

        float[] extendedJointPositions = new float[7];
        Array.Copy(jointPositions, extendedJointPositions, 6);
        extendedJointPositions[6] = jointPositions[5];

        base.DriveJoints(extendedJointPositions);
    }

    public override void DriveCartesianPose(PxTransformData t)
    {
        float[] qInit = new float[6];
        for (int i = 0; i < 6; i++)
        {
            qInit[i] = m_robot.JointPositions[i];
        }

        t.quaternion = t.quaternion * m_rotationTooltipToJoint;
        bool result = Physx.GetRobotInverseKinematics(m_robot.NativeObjectPtr, ref qInit[0], ref t, 1e-3f, 100, 0.01f);
        if (!result)
        {
            float[] qActual = m_robot.JointPositions;
            string printStr = "q_actual: ";
            for (int i = 0; i < 6; i++)
            {
                printStr += qActual[i];
                printStr += ", ";
            }
            print(printStr);
            printStr = "q_solved: ";
            for (int i = 0; i < 6; i++)
            {
                printStr += qInit[i];
                printStr += ", ";
            }
            print(printStr);
            throw new Exception("IK Failed!");
        }

        bool shouldDriveJoints = true;
        for (int i = 0; i < 6; i++)
        {
            if (qInit[i] - m_robot.JointPositions[i] > 0.2)
            {
                shouldDriveJoints = false;
            }
        }
        if (shouldDriveJoints)
        {
            DriveJoints(qInit);
        }
    }
}

using PhysX5ForUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// PSM with Suction Irrigator 6 DoF for IK, actually controlled with 5 joint inputs
/// </summary>
/// 
public class PSMSuctionIrrigator6DofControllerMod : PSMControllerBase
{
    public override void DriveJoints(float[] jointPositions)
    {
        if (jointPositions == null || jointPositions.Length != 5)
        {
            throw new ArgumentException("jointPositions must be an array of 5 floats.");
        }

        float[] extendedJointPositions = GetExtendedJointPositionSetPoint(jointPositions);

        base.DriveJoints(extendedJointPositions);
        m_jointPositionSetpoint = jointPositions;
    }

    public float[] GetExtendedJointPositionSetPoint(float[] jointPositions)
    {
        float[] extendedJointPositions = new float[7];
        extendedJointPositions[0] = jointPositions[0];
        extendedJointPositions[1] = jointPositions[1];
        extendedJointPositions[2] = jointPositions[2];
        extendedJointPositions[3] = 0;
        extendedJointPositions[4] = jointPositions[3];
        extendedJointPositions[5] = jointPositions[4];
        extendedJointPositions[6] = jointPositions[4];
        return extendedJointPositions;
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

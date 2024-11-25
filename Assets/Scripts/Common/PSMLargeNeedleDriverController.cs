using PhysX5ForUnity;
using System;
using UnityEngine;

/// <summary>
/// PSM with large needle driver (and similar tools, e.g. curved scissor)
/// </summary>
/// 
public class PSMLargeNeedleDriverController : PSMControllerBase
{
    public override void DriveJoints(float[] jointPositions)
    {
        if (jointPositions == null || jointPositions.Length != 7)
        {
            throw new ArgumentException("jointPositions must be an array of 7 floats.");
        }

        float[] extendedJointPositions = new float[8];

        for (int i = 0; i < 5; i++)
        {
            extendedJointPositions[i] = jointPositions[i];
        }

        extendedJointPositions[7] = jointPositions[5];

        extendedJointPositions[5] = jointPositions[5] - jointPositions[6] / 2;
        extendedJointPositions[6] = jointPositions[5] + jointPositions[6] / 2;

        base.DriveJoints(extendedJointPositions);
    }

    public override void DriveCartesianPose(PxTransformData t)
    {
        DriveCartesianPose(t, 0);
    }

    public void DriveCartesianPose(PxTransformData t, float angleGrasper)
    {
        float[] qInit = new float[7];
        for (int i = 0; i < 5; i++)
        {
            qInit[i] = m_robot.JointPositions[i];
        }
        qInit[5] = m_robot.JointPositions[7];

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
        // set grasper angle and drive the joints
        qInit[6] = angleGrasper;
        DriveJoints(qInit);
    }

    [Obsolete("The method has never been properly tested.")]
    public void DriveCartesianPoseInEE(PxTransformData t, float angleGrasper)
    {
        float[] jointPositions = m_robot.JointPositions;

        Matrix4x4 fk;
        // Getting the end-effector pos by doing FK
        // TODO: this is inefficient; use the current EE tooltip pose instead
        Physx.GetRobotForwardKinematics(m_robot.NativeObjectPtr, ref jointPositions[0], out fk);

        Quaternion currentRotation = MatrixToRotation(fk);
        Vector3 currentPosition = MatrixToPosition(fk);


        // Now we will transform the current EE to the new pos
        Quaternion newRotation = currentRotation * t.quaternion;
        Vector3 newPosition = currentPosition + t.position;
        PxTransformData newPose = new PxTransformData(newPosition, newRotation);

        float[] qInit = new float[m_robot.NumJoints];

        float[] qActual = m_robot.JointPositions;

        bool result = Physx.GetRobotInverseKinematics(m_robot.NativeObjectPtr, ref qInit[0], ref newPose, 1e-3f, 100, 0.01f);
        if (!result)
        {
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

            printStr += "failure";
            print(printStr);
        }

        qInit[6] = angleGrasper / 2;
        qInit[7] = angleGrasper / 2;

        Physx.DriveJoints(m_robot.NativeObjectPtr, ref qInit[0]);

    }

    static Quaternion MatrixToRotation(Matrix4x4 m)
    {
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        Quaternion q = new Quaternion();
        q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
        q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
        q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
        q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
        q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
        q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
        q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
        return q;
    }

    static Vector3 MatrixToPosition(Matrix4x4 m)
    {
        Vector3 result = new Vector3();
        result.x = m[0, 3];
        result.y = m[1, 3];
        result.z = m[2, 3];
        return result;
    }
}

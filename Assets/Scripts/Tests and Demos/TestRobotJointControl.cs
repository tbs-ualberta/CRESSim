using PhysX5ForUnity;
using System;
using UnityEngine;

public class TestRobotJointControl : MonoBehaviour
{
    private void Start()
    {
        m_targets = new float[5];
        m_Jn = new float[m_robot.NumJoints, 6];
    }
    private void FixedUpdate()
    {
        if (m_reset && Time.fixedTime > 1.0f)
        {
            print("reset");
            m_robot.ResetObject();
            m_reset = false;
        }
        float[] jointPositions = m_robot.JointPositions;
        //for (int i = 0; i < m_robot.NumJoints; i++)
        //{
        //    print(jointPositions[i]);
        //}
        m_targets[0] -= 0.001f;
        m_targets[1] -= 0.001f;

        m_targets[2] -= 0.001f;

        m_targets[3] += 0.001f;
        m_targets[4] += 0.001f;
        // m_targets[5] -= 0.001f;
        // m_targets[6] += 0.0005f;
        // m_targets[7] += 0.0005f;

        //m_targets[0] = Mathf.Clamp(m_targets[0], -3.14f, 3.14f);
        //m_targets[1] = Mathf.Clamp(m_targets[1], -3.14f, 3.14f);
        //m_targets[2] = Mathf.Clamp(m_targets[2], -1.5f, 0f);
        //m_targets[3] = Mathf.Clamp(m_targets[3], -0.4f, 0.4f);

        //Physx.DriveJoints(m_robot.PxRobot, ref m_targets[0]);
        m_controller.DriveJoints(m_targets);
        Matrix4x4 fk;
        Physx.GetRobotForwardKinematics(m_robot.NativeObjectPtr, ref m_targets[0], out fk);
        ////print(fk);
        //m_eeFrame.transform.rotation = MatrixToRotation(fk);
        //m_eeFrame.transform.position = MatrixToPosition(fk);

        //Physx.GetRobotJacobianBody(m_robot.PxRobot, ref m_targets[0], ref m_Jn[0, 0], 6, 6);

        //Physx.GetRobotJacobianSpatial(m_robot.PxRobot, ref m_targets[0], ref m_Jn[0, 0], 6, 6);
        //string printStr;
        //for (int i=0; i<6; i++)
        //{
        //    printStr = "";
        //    printStr += i + ": ";
        //    printStr += m_Jn[i, 0];
        //    printStr += " ";
        //    printStr += m_Jn[i, 1];
        //    printStr += " ";
        //    printStr += m_Jn[i, 2];
        //    printStr += " ";
        //    printStr += m_Jn[i, 3];
        //    printStr += " ";
        //    printStr += m_Jn[i, 4];
        //    printStr += " ";
        //    printStr += m_Jn[i, 5];
        //    print(printStr);
        //    print("");
        //}

        float[] qInit = { 0f, 0f, 0f, 0f, 0f, 0f };
        //for (int i = 0; i < qInit.Length - 1; i++)
        //{
        //    qInit[i] = m_targets[i] + UnityEngine.Random.Range(-0.1f, 0.1f);
        //}
        //qInit[5] = m_targets[7] + UnityEngine.Random.Range(-0.1f, 0.1f);

        float[] qActual = m_robot.JointPositions;

        // bool result = Physx.GetRobotInverseKinematics(m_robot.PxRobot, ref qInit[0], PxTransformData.FromTransform(m_eeFrame.transform), 1e-3, 100, 0.01f);
        // if (!result)
        // {
        //     string printStr = "q_actual: ";
        //     for (int i = 0; i < 6; i++)
        //     {
        //         printStr += qActual[i];
        //         printStr += ", ";
        //     }
        //     print(printStr);
        //     printStr = "q_solved: ";
        //     for (int i = 0; i < 6; i++)
        //     {
        //         printStr += qInit[i];
        //         printStr += ", ";
        //     }

        //     printStr += "failure";
        //     print(printStr);
        // }

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

    [SerializeField]
    PhysxArticulationRobot m_robot;
    [SerializeField]
    Transform m_eeFrame;
    [SerializeField]
    PSMControllerBase m_controller;

    private float[] m_targets;
    private float[,] m_Jn;
    private bool m_reset = true;
}

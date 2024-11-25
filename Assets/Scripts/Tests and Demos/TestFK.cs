using System;
using System.Collections;
using System.Collections.Generic;
using PhysX5ForUnity;
using UnityEngine;

public class TestFK : MonoBehaviour
{
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
    
    void FixedUpdate()
    {
        float[] target;
        if (m_psmController.JointPositionSetpoint == null)
        {
            target = new float[5] { 0, 0, 0, 0, 0 };
        }
        else
        {
            target = m_psmController.JointPositionSetpoint;
            target[0] += 0.001f;
            target[1] += 0.001f;
            target[2] -= 0.1f;
            target[3] += 0.001f;
            target[4] += 0.001f;
        }
        m_psmController.DriveJoints(target);

        float[] extendedJointPositions = new float[6];
        extendedJointPositions[0] = target[0];
        extendedJointPositions[1] = target[1];
        extendedJointPositions[2] = target[2];
        extendedJointPositions[3] = 0;
        extendedJointPositions[4] = target[3];
        extendedJointPositions[5] = target[4];
        // extendedJointPositions[2] /= 20;
        Matrix4x4 t = m_psmController.Robot.ForwardKinematics(extendedJointPositions);
        Debug.Log("");
        Quaternion q = MatrixToRotation(t);
        Debug.Log(MatrixToPosition(t).ToString());
        Debug.Log(q.ToString());
        Debug.Log(m_psmEETooltip.position.ToString());
        Debug.Log(m_psmEETooltip.rotation.ToString());
        m_eeFkFrame.transform.rotation = q;
        m_eeFkFrame.transform.position = MatrixToPosition(t);
    }

    [SerializeField]
    private PSMControllerBase m_psmController;
    [SerializeField]
    private Transform m_psmEETooltip;
    [SerializeField]
    Transform m_eeFkFrame;
}

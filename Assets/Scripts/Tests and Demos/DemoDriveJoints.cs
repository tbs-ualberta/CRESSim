using PhysX5ForUnity;
using System;
using UnityEngine;

public class DemoDriveJoints : MonoBehaviour
{
    private void Start()
    {
        m_targets = new float[5];
        m_targets[2] = -2.0f;
    }

    private void FixedUpdate()
    {

        m_controller.DriveJoints(m_targets);

    }

    [SerializeField]
    PhysxArticulationRobot m_robot;
    [SerializeField]
    PSMControllerBase m_controller;

    private float[] m_targets;
}

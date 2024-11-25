using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhysX5ForUnity;

public class PSMCartisianUserControl : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float verticalSpeed = 5.0f;


    private void Start()
    {
        Transform existingTransform = m_robotEETooltip.transform; // reference to an existing transform
        m_transformEE.position = existingTransform.position;
        m_transformEE.quaternion = existingTransform.rotation;
    }

    void FixedUpdate()
    {
        // Get horizontal and vertical input from the left joystick
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Get input from the right trigger for up/down movement
        float triggerInput = Input.GetAxis("3rd axis"); // Change this to match your input settings


        // Calculate the movement vector in the horizontal plane
        Vector3 moveDirection = new Vector3(horizontalInput, triggerInput, verticalInput) * moveSpeed * Time.fixedDeltaTime;

        // Move the game object
        m_transformEE.position += moveDirection;

        m_controller.DriveCartesianPose(m_transformEE);
    }

    [SerializeField]
    PSMControllerBase m_controller;
    [SerializeField]
    private PhysxArticulationRobot m_robot;
    [SerializeField]
    GameObject m_robotEETooltip;

    private PxTransformData m_transformEE;
}

using PhysX5ForUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickRobotControl : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float verticalSpeed = 5.0f;


    private void Start()
    {
        m_targets = new float[m_robot.NumJoints];
    }

    // Update is called once per frame
    void Update()
    {
        // Get horizontal and vertical input from the left joystick
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Get input from the right trigger for up/down movement
        float triggerInput = Input.GetAxis("3rd axis"); // Change this to match your input settings


        // Calculate the movement vector in the horizontal plane
        Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput) * moveSpeed * Time.deltaTime;

        // Add vertical movement based on the trigger input
        moveDirection.y = triggerInput * verticalSpeed * Time.deltaTime;

        // Move the game object
        m_targets[0] += 0.1f*moveDirection.y;
        //m_targets[1] += moveDirection.y;
        m_targets[2] += moveDirection.y;
        Physx.DriveJoints(m_robot.NativeObjectPtr, ref m_targets[0]);
    }

    [SerializeField]
    private PhysxArticulationRobot m_robot;

    private float[] m_targets;
}

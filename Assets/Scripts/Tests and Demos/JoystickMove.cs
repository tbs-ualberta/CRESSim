using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickMove : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float verticalSpeed = 5.0f;


    private void Start()
    {

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
        transform.Translate(moveDirection, Space.World);
    }
}

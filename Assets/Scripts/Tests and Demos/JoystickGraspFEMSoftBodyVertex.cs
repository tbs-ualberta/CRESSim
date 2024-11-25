using PhysX5ForUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickGraspFEMSoftBodyVertex : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float verticalSpeed = 5.0f;
    public PhysxFEMSoftBodyActor actor;

    private Transform transformBlade1;
    private Transform transformBlade2;

    private bool isGrasping = false;

    private void Start()
    {
        transformBlade1 = gameObject.transform.Find("Scissor/Blade 1");
        transformBlade2 = gameObject.transform.Find("Scissor/Blade 2");

    }

    void Update()
    {
        // Get horizontal and vertical input from the left joystick
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Get input from the right trigger for up/down movement
        float triggerInput = Input.GetAxis("3rd axis"); // Change this to match your input settings

        bool shouldGrasp = Input.GetButton("Fire1");


        if (shouldGrasp && !isGrasping)
        {
            AttachParticle();
            isGrasping = true;
        }

        if (!shouldGrasp && isGrasping)
        {
            DetachParticle();
            isGrasping = false;
        }

        if (!shouldGrasp)
        {
            transformBlade1.localRotation = Quaternion.Euler(0f, 10f, 0f);
            transformBlade2.localRotation = Quaternion.Euler(0f, -10f, 180f);
        }
        else
        {
            transformBlade1.localRotation = Quaternion.Euler(0f, 0f, 0f);
            transformBlade2.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }

        // Calculate the movement vector in the horizontal plane
        Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput) * moveSpeed * Time.deltaTime;

        // Add vertical movement based on the trigger input
        moveDirection.y = triggerInput * verticalSpeed * Time.deltaTime;

        // Move the game object
        transform.Translate(moveDirection, Space.World);
    }

    void AttachParticle()
    {
        Vector4[] vertices = actor.CollisionMeshData.positionInvMass;
        float minDistance = float.MaxValue;
        int graspedIdx = int.MaxValue;
        for (int i=0; i< vertices.Length; i++)
        {
            Vector3 positionParticle = vertices[i];

            float d = (transformBlade1.position - positionParticle).sqrMagnitude;
            if (d < minDistance)
            {
                minDistance = d;
                graspedIdx = i;
            }
        }
        if (graspedIdx < vertices.Length && minDistance < 0.5)
        {
            Vector3 fixedLocalPosition = transformBlade1.InverseTransformPoint(vertices[graspedIdx]);
            fixedLocalPosition.x *= transformBlade1.lossyScale.x;
            fixedLocalPosition.y *= transformBlade1.lossyScale.y;
            fixedLocalPosition.z *= transformBlade1.lossyScale.z;
            PhysxKinematicRigidActor rigidActor = transformBlade1.GetComponent<PhysxKinematicRigidActor>();
            attachmentHandler = Physx.AttachFEMSoftBodyVertexToRigidBody(actor.NativeObjectPtr, rigidActor.NativeObjectPtr, graspedIdx, ref fixedLocalPosition);
        }
    }

    void DetachParticle()
    {
        //if (attachmentHandler > 0)
        {
            PhysxKinematicRigidActor rigidActor = transformBlade1.GetComponent<PhysxKinematicRigidActor>();
            Physx.DetachFEMSoftBodyVertexFromRigidBody(actor.NativeObjectPtr, rigidActor.NativeObjectPtr, attachmentHandler);
            attachmentHandler = -1;
        }
    }

    private int attachmentHandler;
}

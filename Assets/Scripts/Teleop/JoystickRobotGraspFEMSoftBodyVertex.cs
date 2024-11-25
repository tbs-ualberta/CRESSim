using PhysX5ForUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickRobotGraspFEMSoftBodyVertex : MonoBehaviour
{
    void Update()
    {
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
    }

    void AttachParticle()
    {
        Vector4[] vertices = m_softActor.CollisionMeshData.positionInvMass;
        float minDistance = float.MaxValue;
        int graspedIdx = int.MaxValue;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 positionParticle = vertices[i];

            float d = (m_robotEETransform.position - positionParticle).sqrMagnitude;
            if (d < minDistance)
            {
                minDistance = d;
                graspedIdx = i;
            }
        }
        if (graspedIdx < vertices.Length && minDistance < 0.5)
        {
            Vector3 fixedLocalPosition = m_robotEETransform.InverseTransformPoint(vertices[graspedIdx]);
            fixedLocalPosition.x *= m_robotEETransform.lossyScale.x;
            fixedLocalPosition.y *= m_robotEETransform.lossyScale.y;
            fixedLocalPosition.z *= m_robotEETransform.lossyScale.z;
            IntPtr pxLinkPtr = m_robot.EELinkPtrs[m_robotEEIdx];
            attachmentHandler = Physx.AttachFEMSoftBodyVertexToRigidBody(m_softActor.NativeObjectPtr, pxLinkPtr, graspedIdx, ref fixedLocalPosition);
        }
    }

    void DetachParticle()
    {
        IntPtr pxLinkPtr = m_robot.EELinkPtrs[m_robotEEIdx];
        Physx.DetachFEMSoftBodyVertexFromRigidBody(m_softActor.NativeObjectPtr, pxLinkPtr, attachmentHandler);
        attachmentHandler = -1;
    }

    [SerializeField]
    private PhysxFEMSoftBodyActor m_softActor;
    [SerializeField]
    private PhysxArticulationRobot m_robot;
    [SerializeField]
    private int m_robotEEIdx = 2;
    [SerializeField]
    private Transform m_robotEETransform;

    private bool isGrasping = false;
    private int attachmentHandler;
}

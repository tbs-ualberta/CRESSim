using System.Collections;
using System.Collections.Generic;
using PhysX5ForUnity;
using Unity.MLAgents;
using UnityEngine;

public class SuctionAgentSoftBody : SuctionAgent
{
    protected override void ResetTissue()
    {
        // If using soft body for tissue
        foreach (PhysxFEMSoftRigidOverlapAttachment attachment in m_softRigidAttachments)
        {
            attachment.enabled = false;
        }
        m_meshGenerator.NumControlPoints = 11;
        Mesh mesh = m_meshGenerator.GenerateMesh();
        ((PhysxFEMSoftBodyActor)m_tissue).ReferenceMesh = mesh;
        m_tissue.Recreate();
        foreach (PhysxFEMSoftRigidOverlapAttachment attachment in m_softRigidAttachments)
        {
            attachment.enabled = true;
        }
    }

    [SerializeField]
    private PhysxFEMSoftRigidOverlapAttachment[] m_softRigidAttachments;
}

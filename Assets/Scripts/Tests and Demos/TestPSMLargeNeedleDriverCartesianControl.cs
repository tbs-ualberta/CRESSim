using PhysX5ForUnity;
using UnityEngine;

public class TestPSMLargeNeedleDriverCartesianControl : MonoBehaviour
{
    private void Start()
    {
        m_targets = new float[7];
        m_lastTime = Time.fixedTime;
        // Create a new GameObject
        GameObject newObj = new GameObject("NewObject");

        // Now newObj.transform is your new Transform
        m_transformEE = newObj.transform;

        // Optionally, if you want to copy the properties of an existing transform
        Transform existingTransform = m_robotEETooltip.transform; // reference to an existing transform
        m_transformEE.position = existingTransform.position;
        m_transformEE.rotation = existingTransform.rotation;
        m_transformEE.localScale = existingTransform.localScale;
    }

    private void FixedUpdate()
    {
        if (Time.fixedTime > 2)
        {
            if (Time.fixedTime - m_lastTime > 0.2)
            {
                m_lastTime = Time.fixedTime;
                //transformEE.position += new Vector3(0.0f, 0.05f, 0.0f);
                m_transformEE.position += new Vector3(0.01f, 0.01f, 0.01f);
                //m_transformEE.rotation = m_transformEE.rotation * Quaternion.Euler(0.2f, 0.2f, 0.2f);
                m_controller.DriveCartesianPose(m_transformEE.ToPxTransformData(), 0f);
            }
        }
        else
        {
            m_transformEE.position = new Vector3(0.0f, -1.5f, 0.0f);
            m_controller.DriveCartesianPose(m_transformEE.ToPxTransformData(), 0);
        }
    }

    [SerializeField]
    PSMLargeNeedleDriverController m_controller;
    [SerializeField]
    GameObject m_robotEETooltip;

    private float[] m_targets;
    private float m_lastTime;
    private Transform m_transformEE;
}

using System.Collections;
using System.Collections.Generic;
using PhysX5ForUnity;
using UnityEngine;

public class TestSetFluidColor : MonoBehaviour
{
    void Update()
    {
        if (Time.fixedTime > 3 && m_shouldSetColor)
        {
            m_actor.FluidColor = new Color(1.0f, 0, 0);
            m_shouldSetColor = false;
        }
    }

    [SerializeField]
    private PhysxFluidArrayActor m_actor;
    private bool m_shouldSetColor = true;
}

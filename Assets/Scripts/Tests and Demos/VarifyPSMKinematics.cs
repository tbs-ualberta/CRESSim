using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VarifyPSMKinematics : MonoBehaviour
{
    void Start()
    {
        print(m_link3.transform.position);
        print(m_link5.transform.position);
    }

    [SerializeField]
    private GameObject m_link3;
    [SerializeField]
    private GameObject m_link5;
}

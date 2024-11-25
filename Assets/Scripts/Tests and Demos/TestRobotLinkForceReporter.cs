using PhysX5ForUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotLinkForceReporter : MonoBehaviour
{
    void Start()
    {
        
    }

    void FixedUpdate()
    {
        PxSpatialForceData f;
        Physx.GetRobotLinkIncomingForce(m_robot.NativeObjectPtr, 5, out f);
        print(f.torque);
        print(f.force);
    }

    [SerializeField]
    PhysxArticulationRobot m_robot;
}

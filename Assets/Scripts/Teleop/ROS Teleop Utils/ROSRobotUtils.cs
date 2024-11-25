
using UnityEngine;
using System.Runtime.InteropServices;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using PhysX5ForUnity;


namespace ROSRobotUtils
{
    public struct OperatingState
    {
        public string State;
        public bool IsHomed;
        public bool IsBusy;
    }

    public static class CoordinateSpaceExtensions
    {
        public static TransformMsg To<C>(this PxTransformData transform) where C : ICoordinateSpace, new()
        {
            return new TransformMsg(new Vector3<C>(transform.position), new Quaternion<C>(transform.quaternion));
        }
    }
}
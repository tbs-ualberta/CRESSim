using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using PhysX5ForUnity;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Sensor;
using RosMessageTypes.Crtk;
using System.Collections;
using ROSRobotUtils;
using RosMessageTypes.Std;
using RosMessageTypes.Cisst;

public class MTMROSConnector : MonoBehaviour
{
    public OperatingState OperatingState
    {
        get {return m_operatingState; }
    }

    public PxTransformData MeasuredCp
    {
        get { return m_measuredCp; }
    }

    public bool GripperClosed
    {
        get { return m_gripperClosed; }
    }

    public void MoveCp(PxTransformData t, float timeout = 2)
    {
        StartCoroutine(WaitAndMoveCp(t, timeout));
        m_operatingState.IsBusy = true;
    }

    public void MoveJp(double[] jp, float timeout = 2)
    {
        StartCoroutine(WaitAndMoveJp(jp, timeout));
        m_operatingState.IsBusy = true;
    }

    public void MoveJp(float[] jp)
    {
        double[] djp = new double[jp.Length];
        for (int i = 0; i < jp.Length; i++)
        {
            djp[i] = jp[i];
        }
        MoveJp(djp);
    }

    public void UseGravityCompensation(bool useGravityCompensation)
    {
        BoolMsg boolMsg = new BoolMsg(useGravityCompensation);
        m_rosConnection.Publish(TOPIC_USE_GRAVITY_COMPENSATION, boolMsg);
    }

    public void SetCartesianImpedanceGains(Vector3 posStiff, Vector3 posDamping, Vector3 oriStiff, Vector3 oriDamping)
    {
        double timeAsDouble = Time.timeAsDouble;
        uint seconds = (uint)timeAsDouble;
        double fractionalSeconds = timeAsDouble - seconds;
        uint nanoseconds = (uint)(fractionalSeconds * 1e9); // 1 second = 1e9 nanoseconds
        PrmCartesianImpedanceGainsMsg gainsMsg = new PrmCartesianImpedanceGainsMsg
        {
            header = new HeaderMsg
            {
                stamp = new TimeMsg(seconds, nanoseconds),
            },
            // Common settings
            // Always start from current position
            ForcePosition = m_measuredCpMsg.transform.translation,
            ForceOrientation = m_measuredCpMsg.transform.rotation,
            TorqueOrientation = m_measuredCpMsg.transform.rotation,
            
            // User settings
            PosStiffPos = new Vector3Msg(posStiff.x, posStiff.y, posStiff.z),
            PosStiffNeg = new Vector3Msg(posStiff.x, posStiff.y, posStiff.z),
            PosDampingPos = new Vector3Msg(posDamping.x, posDamping.y, posDamping.z),
            PosDampingNeg = new Vector3Msg(posDamping.x, posDamping.y, posDamping.z),
            OriStiffPos = new Vector3Msg(oriStiff.x, oriStiff.y, oriStiff.z),
            OriStiffNeg = new Vector3Msg(oriStiff.x, oriStiff.y, oriStiff.z),
            OriDampingPos = new Vector3Msg(oriDamping.x, oriDamping.y, oriDamping.z),
            OriDampingNeg = new Vector3Msg(oriDamping.x, oriDamping.y, oriDamping.z),
        };
        m_rosConnection.Publish(TOPIC_SET_CARTESIAN_IMPEDANCE_GAINS, gainsMsg);
    }

    public void AlignMTMWithPSM(PxTransformData psmPose, Quaternion rotationPsmMtm, Quaternion rotationPsmBase, Quaternion rotationPsmEE, float timeout = 2.0f, bool resetToHome = true)
    {
        StartCoroutine(AlignMTMWithPSMCoroutine(psmPose, rotationPsmMtm, rotationPsmBase, rotationPsmEE, timeout, resetToHome));
    }

    public IEnumerator WaitForBusyCoroutine(float timeout)
    {
        float startTime = Time.time;
        while (Time.time - startTime < timeout)
        {
            if (!m_operatingState.IsBusy)
            {
                yield break;
            }
            yield return new WaitForSeconds(0.01f);
        }
        // Timeout
        Debug.Log("Timeout reached while waiting for busy state.");
        yield break;
    }

    private IEnumerator AlignMTMWithPSMCoroutine(PxTransformData psmPose, Quaternion rotationPsmMtm, Quaternion rotationPsmBase, Quaternion rotationPsmEE, float timeout, bool resetToHome)
    {
        if (resetToHome)
        {
            // Move to zero joint positions
            MoveJp(new double[] { 0, 0, 0, 0, 0, 0, 0 }, timeout);
            yield return StartCoroutine(WaitForBusyCoroutine(timeout)); // Wait for first move to complete
        }

        // Calculate the aligned MTM pose
        psmPose.position = m_measuredCp.position;
        psmPose.quaternion = rotationPsmMtm * Quaternion.Inverse(rotationPsmBase) * psmPose.quaternion * rotationPsmEE;
        MoveCp(psmPose, timeout);
    }

    private IEnumerator WaitAndMoveCp(PxTransformData t, float timeout)
    {
        yield return StartCoroutine(WaitForBusyCoroutine(timeout)); // Wait for robot to be idle

        double timeAsDouble = Time.timeAsDouble;
        uint seconds = (uint)timeAsDouble;
        double fractionalSeconds = timeAsDouble - seconds;
        uint nanoseconds = (uint)(fractionalSeconds * 1e9); // 1 second = 1e9 nanoseconds
        TransformStampedMsg transformStampedMsg = new TransformStampedMsg
        {
            header = new HeaderMsg
            {
                stamp = new TimeMsg(seconds, nanoseconds),
            },
            transform = t.To<FLU>()
        };
        m_rosConnection.Publish(TOPIC_MOVE_CP, transformStampedMsg);
    }

    private IEnumerator WaitAndMoveJp(double[] jp, float timeout)
    {
        yield return StartCoroutine(WaitForBusyCoroutine(timeout)); // Wait for robot to be idle

        double timeAsDouble = Time.timeAsDouble;
        uint seconds = (uint)timeAsDouble;
        double fractionalSeconds = timeAsDouble - seconds;
        uint nanoseconds = (uint)(fractionalSeconds * 1e9); // 1 second = 1e9 nanoseconds
        JointStateMsg jointStateMsg = new JointStateMsg
        {
            header = new HeaderMsg
            {
                stamp = new TimeMsg(seconds, nanoseconds),
            },
            position = jp
        };
        m_rosConnection.Publish(TOPIC_MOVE_JP, jointStateMsg);
    }

    void Start()
    {
        TOPIC_OPERATING_STATE =  "/" + m_robotName + TOPIC_OPERATING_STATE;
        TOPIC_MEASURED_CP =  "/" + m_robotName + TOPIC_MEASURED_CP;
        TOPIC_MOVE_CP = "/" + m_robotName + TOPIC_MOVE_CP;
        TOPIC_MOVE_JP = "/" + m_robotName + TOPIC_MOVE_JP;

        TOPIC_USE_GRAVITY_COMPENSATION = "/" + m_robotName + TOPIC_USE_GRAVITY_COMPENSATION;
        TOPIC_SET_CARTESIAN_IMPEDANCE_GAINS = "/" + m_robotName + TOPIC_SET_CARTESIAN_IMPEDANCE_GAINS;
        TOPIC_LOCK_ORIENTATION = "/" + m_robotName + TOPIC_LOCK_ORIENTATION;
        TOPIC_UNLOCK_ORIENTATION = "/" + m_robotName + TOPIC_UNLOCK_ORIENTATION;

        TOPIC_GRIPPER_CLOSED = "/" + m_robotName + TOPIC_GRIPPER_CLOSED;

        m_rosConnection = ROSConnection.GetOrCreateInstance();
        m_rosConnection.Subscribe<Operating_stateMsg>(TOPIC_OPERATING_STATE, OperatingStateCallback);
        m_rosConnection.Subscribe<TransformStampedMsg>(TOPIC_MEASURED_CP, MeasuredCpCallback);
        m_rosConnection.RegisterPublisher<TransformStampedMsg>(TOPIC_MOVE_CP);
        m_rosConnection.RegisterPublisher<JointStateMsg>(TOPIC_MOVE_JP);

        m_rosConnection.RegisterPublisher<BoolMsg>(TOPIC_USE_GRAVITY_COMPENSATION);
        m_rosConnection.RegisterPublisher<PrmCartesianImpedanceGainsMsg>(TOPIC_SET_CARTESIAN_IMPEDANCE_GAINS);
        m_rosConnection.RegisterPublisher<BoolMsg>(TOPIC_UNLOCK_ORIENTATION);

        m_rosConnection.Subscribe<BoolMsg>(TOPIC_GRIPPER_CLOSED, GripperClosedCallback);
    }

    private void MeasuredCpCallback(TransformStampedMsg msg)
    {
        m_measuredCpMsg = msg;
        m_measuredCp.position = msg.transform.translation.From<FLU>();
        m_measuredCp.quaternion = msg.transform.rotation.From<FLU>();
    }

    private void OperatingStateCallback(Operating_stateMsg msg)
    {
        m_operatingState.State = msg.state;
        m_operatingState.IsHomed = msg.is_homed;
        m_operatingState.IsBusy = msg.is_busy;
    }

    private void GripperClosedCallback(BoolMsg msg)
    {
        m_gripperClosed = msg.data;
    }

    private ROSConnection m_rosConnection;

    private OperatingState m_operatingState;

    private PxTransformData m_measuredCp;

    private TransformStampedMsg m_measuredCpMsg;

    private bool m_gripperClosed;

    [SerializeField] private string m_robotName = "MTML";
    private string TOPIC_OPERATING_STATE = "/operating_state";
    private string TOPIC_MEASURED_CP = "/measured_cp";
    private string TOPIC_MOVE_JP = "/move_jp";
    private string TOPIC_MOVE_CP = "/move_cp";
    private string TOPIC_USE_GRAVITY_COMPENSATION = "/use_gravity_compensation";
    private string TOPIC_SET_CARTESIAN_IMPEDANCE_GAINS = "/set_cartesian_impedance_gains";
    private string TOPIC_LOCK_ORIENTATION = "/lock_orientation";
    private string TOPIC_UNLOCK_ORIENTATION = "/unlock_orientation";
    private string TOPIC_GRIPPER_CLOSED = "/gripper/closed";
}
//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;

namespace RosMessageTypes.Crtk
{
    [Serializable]
    public class Operating_stateMsg : Message
    {
        public const string k_RosMessageName = "crtk_msgs/operating_state";
        public override string RosMessageName => k_RosMessageName;

        // 
        //  See https://github.com/collaborative-robotics/documentation/wiki/Robot-API-status
        // 
        //  Standard states include DISABLED, ENABLED, PAUSED and FAULT
        // 
        public HeaderMsg header;
        public string state;
        public bool is_homed;
        public bool is_busy;

        public Operating_stateMsg()
        {
            this.header = new HeaderMsg();
            this.state = "";
            this.is_homed = false;
            this.is_busy = false;
        }

        public Operating_stateMsg(HeaderMsg header, string state, bool is_homed, bool is_busy)
        {
            this.header = header;
            this.state = state;
            this.is_homed = is_homed;
            this.is_busy = is_busy;
        }

        public static Operating_stateMsg Deserialize(MessageDeserializer deserializer) => new Operating_stateMsg(deserializer);

        private Operating_stateMsg(MessageDeserializer deserializer)
        {
            this.header = HeaderMsg.Deserialize(deserializer);
            deserializer.Read(out this.state);
            deserializer.Read(out this.is_homed);
            deserializer.Read(out this.is_busy);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.state);
            serializer.Write(this.is_homed);
            serializer.Write(this.is_busy);
        }

        public override string ToString()
        {
            return "Operating_stateMsg: " +
            "\nheader: " + header.ToString() +
            "\nstate: " + state.ToString() +
            "\nis_homed: " + is_homed.ToString() +
            "\nis_busy: " + is_busy.ToString();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize);
        }
    }
}
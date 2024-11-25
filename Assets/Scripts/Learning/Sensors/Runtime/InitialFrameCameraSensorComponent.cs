using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.MLAgents.Sensors
{
    /// <summary>
    /// A SensorComponent that creates a <see cref="InitialFrameCameraSensor"/>.
    /// </summary>
    [AddComponentMenu("ML Agents Extensions/Initial Frame Camera Sensor", (int)MenuGroup.Sensors)]
    public class InitialFrameCameraSensorComponent : SensorComponent, IDisposable
    {
        [HideInInspector, SerializeField, FormerlySerializedAs("camera")]
        Camera m_Camera;

        InitialFrameCameraSensor m_Sensor;

        /// <summary>
        /// Camera object that provides the data to the sensor.
        /// </summary>
        public Camera Camera
        {
            get { return m_Camera; }
            set { m_Camera = value; UpdateSensor(); }
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("sensorName")]
        string m_SensorName = "InitialFrameCameraSensor";

        /// <summary>
        /// Name of the generated <see cref="InitialFrameCameraSensor"/> object.
        /// Note that changing this at runtime does not affect how the Agent sorts the sensors.
        /// </summary>
        public string SensorName
        {
            get { return m_SensorName; }
            set { m_SensorName = value; }
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("width")]
        int m_Width = 84;

        /// <summary>
        /// Width of the generated observation.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public int Width
        {
            get { return m_Width; }
            set { m_Width = value; }
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("height")]
        int m_Height = 84;

        /// <summary>
        /// Height of the generated observation.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public int Height
        {
            get { return m_Height; }
            set { m_Height = value; }
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("grayscale")]
        bool m_Grayscale;

        /// <summary>
        /// Whether to generate grayscale images or color.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public bool Grayscale
        {
            get { return m_Grayscale; }
            set { m_Grayscale = value; }
        }

        [HideInInspector, SerializeField]
        ObservationType m_ObservationType;

        /// <summary>
        /// The type of the observation.
        /// </summary>
        public ObservationType ObservationType
        {
            get { return m_ObservationType; }
            set { m_ObservationType = value; UpdateSensor(); }
        }

        [HideInInspector, SerializeField]
        bool m_RuntimeCameraEnable;


        /// <summary>
        /// Controls the whether the camera sensor's attached camera
        /// is enabled during runtime. Overrides the camera object enabled status.
        /// Disabled for improved performance. Disabled by default.
        /// </summary>
        public bool RuntimeCameraEnable
        {
            get { return m_RuntimeCameraEnable; }
            set { m_RuntimeCameraEnable = value; UpdateSensor(); }
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("compression")]
        SensorCompressionType m_Compression = SensorCompressionType.PNG;

        /// <summary>
        /// The compression type to use for the sensor.
        /// </summary>
        public SensorCompressionType CompressionType
        {
            get { return m_Compression; }
            set { m_Compression = value; UpdateSensor(); }
        }

        void Start()
        {
            UpdateSensor();
        }

        /// <summary>
        /// Creates the <see cref="InitialFrameCameraSensor"/>
        /// </summary>
        /// <returns>The created <see cref="InitialFrameCameraSensor"/> object for this component.</returns>
        public override ISensor[] CreateSensors()
        {
            Dispose();
            m_Sensor = new InitialFrameCameraSensor(m_Camera, m_Width, m_Height, Grayscale, m_SensorName, m_Compression, m_ObservationType);

            return new ISensor[] { m_Sensor };
        }

        /// <summary>
        /// Update fields that are safe to change on the Sensor at runtime.
        /// </summary>
        public void UpdateSensor()
        {
            if (m_Sensor != null)
            {
                m_Sensor.Camera = m_Camera;
                m_Sensor.CompressionType = m_Compression;
                m_Sensor.Camera.enabled = m_RuntimeCameraEnable;
            }
        }

        /// <summary>
        /// Clean up the sensor created by CreateSensors().
        /// </summary>
        public void Dispose()
        {
            if (!ReferenceEquals(m_Sensor, null))
            {
                m_Sensor.Dispose();
                m_Sensor = null;
            }
        }

        /// <summary>
        /// Manually update the memorized initial frame
        /// </summary>
        public void UpdateSensorFrame()
        {
            m_Sensor.UpdateFrame();
        }
    }
}

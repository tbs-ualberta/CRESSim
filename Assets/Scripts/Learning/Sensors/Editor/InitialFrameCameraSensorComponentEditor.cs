using UnityEditor;
using Unity.MLAgents.Sensors;

namespace Unity.MLAgents.Editor
{
    [CustomEditor(typeof(InitialFrameCameraSensorComponent), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    internal class InitialFrameCameraSensorComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            // Drawing the InitialFrameCameraSensorComponent
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(so.FindProperty("m_Camera"), true);
            EditorGUI.BeginDisabledGroup(!EditorUtilities.CanUpdateModelProperties());
            {
                // These fields affect the sensor order or observation size,
                // So can't be changed at runtime.
                EditorGUILayout.PropertyField(so.FindProperty("m_SensorName"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_Width"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_Height"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_Grayscale"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_ObservationType"), true);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(so.FindProperty("m_RuntimeCameraEnable"), true);
            EditorGUILayout.PropertyField(so.FindProperty("m_Compression"), true);

            var requireSensorUpdate = EditorGUI.EndChangeCheck();
            so.ApplyModifiedProperties();

            if (requireSensorUpdate)
            {
                UpdateSensor();
            }
        }

        void UpdateSensor()
        {
            var sensorComponent = serializedObject.targetObject as InitialFrameCameraSensorComponent;
            sensorComponent?.UpdateSensor();
        }
    }
}
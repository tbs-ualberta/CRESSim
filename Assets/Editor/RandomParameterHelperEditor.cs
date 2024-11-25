using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using System.Collections.Generic;

[CustomEditor(typeof(RandomParameterHelper))]
public class RandomParameterHelperEditor : Editor
{
    public void OnEnable()
    {
        m_parameterArray = serializedObject.FindProperty("m_parameterArray");
        m_parameterNames = serializedObject.FindProperty("m_parameterNames");
        m_foldouts.Clear();
        while (m_foldouts.Count < m_parameterNames.arraySize)
        {
            m_foldouts.Add(false);
        }
    }

    public override void OnInspectorGUI()
    {
        RandomParameterHelper myComponent = (RandomParameterHelper)target;
        GUIContent m_nameLabelContent = new GUIContent("Name");

        EditorGUILayout.PropertyField(m_parameterNames);
        while (m_foldouts.Count < m_parameterNames.arraySize)
        {
            m_foldouts.Add(false);
        }
        // Display each element
        for (int i = 0; i < m_parameterArray.arraySize && i < m_parameterNames.arraySize; i++)
        {
            SerializedProperty paramArrayProperty =  m_parameterArray.GetArrayElementAtIndex(i);
            SerializedProperty paramNameProperty =  m_parameterNames.GetArrayElementAtIndex(i);

            // Foldout for each element
            string displayName = paramNameProperty.stringValue;
            if (displayName=="") displayName = "Element " + i.ToString();
            m_foldouts[i] = EditorGUILayout.Foldout(m_foldouts[i], displayName, true);

            // Show fields inside the foldout if unfolded
            if (m_foldouts[i])
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Element " + i, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(paramNameProperty, m_nameLabelContent);
                EditorGUILayout.PropertyField(paramArrayProperty.FindPropertyRelativeOrFail("Min"));
                EditorGUILayout.PropertyField(paramArrayProperty.FindPropertyRelativeOrFail("Max"));
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Remove Element"))
                {
                    m_parameterArray.DeleteArrayElementAtIndex(i);
                    m_parameterNames.DeleteArrayElementAtIndex(i);
                }
                if (GUILayout.Button("Add Element Above"))
                {
                    m_parameterArray.InsertArrayElementAtIndex(i);
                    m_parameterNames.InsertArrayElementAtIndex(i);
                    m_foldouts.Insert(i, false);
                }
            }
        }

        EditorGUILayout.PropertyField(m_parameterArray);

        // Save the changes back to the object
        if (GUI.changed)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }

    private SerializedProperty m_parameterArray;
    private SerializedProperty m_parameterNames;
    private List<bool> m_foldouts = new List<bool>();
}

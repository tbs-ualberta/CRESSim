using UnityEngine;
using UnityEditor;

public class TrainingAreaLayerEditorWindow : EditorWindow
{
    [MenuItem("Window/Training Area Layer Editor")]
    public static void ShowWindow()
    {
        GetWindow<TrainingAreaLayerEditorWindow>("Training Area Layer Editor");
    }

    private void OnGUI()
    {
        m_selectedGameObject = Selection.activeGameObject;
        if (m_selectedGameObject == null)
        {
            EditorGUILayout.LabelField("No GameObject selected.");
            return;
        }

        EditorGUILayout.LabelField("Select Layer:");
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName))
            {
                if (GUILayout.Button(layerName))
                {
                    // selectedGameObject.layer = i;
                    AssignLayer(m_selectedGameObject, i);
                }
            }
        }
    }

    private void OnSelectionChange()
    {
        m_selectedGameObject = Selection.activeGameObject;
    }

    public void OnInspectorUpdate()
    {
        Repaint();
    }

    static void AssignLayer(GameObject gameObject, int layer)
    {
        if (layer == -1) return;

        Transform[] allTransforms = gameObject.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            t.gameObject.layer = layer;
            EditorUtility.SetDirty(t.gameObject);
        }

        Light[] lights = gameObject.GetComponentsInChildren<Light>(true);
        foreach (Light light in lights)
        {
            light.cullingMask = 1 << layer;
            EditorUtility.SetDirty(light.gameObject);
        }

        // Transform visualTable = gameObject.transform.Find("Suction Area/Visual Table");
        // visualTable.gameObject.layer = 0;
    }

    private GameObject m_selectedGameObject = null;
}

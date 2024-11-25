using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DemoInputPausePlay : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
#if UNITY_EDITOR
            EditorApplication.isPaused = true;
#endif
        }
    }
}
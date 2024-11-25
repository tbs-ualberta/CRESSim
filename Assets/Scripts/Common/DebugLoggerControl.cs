using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLoggerControl : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_EDITOR
        Debug.unityLogger.logEnabled = m_logEnabledInEditor;
#else
        Debug.unityLogger.logEnabled = m_logEnabledInBuild;
#endif
    }

#pragma warning disable 0414
    [SerializeField]
    private bool m_logEnabledInEditor = true;
    [SerializeField]
    private bool m_logEnabledInBuild = false;
#pragma warning restore 0414
}

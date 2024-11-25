using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;

[Serializable]
public class ParameterNameMinMax
{
    public float Min = 0;
    public float Max = 0;
    [NonSerialized] // I don't know why serialization doesn't work when this string is serialized.
    public string Name = "defaultName";

    public ParameterNameMinMax(string name, float min, float max)
    {
        Name = name;
        Min = min;
        Max = max;
    }
}

[CreateAssetMenu(fileName = "RandomParameters", menuName = "Learning/Random Parameters", order = 0)]
public class RandomParameterHelper : ScriptableObject
{
    public bool IsEval
    {
        get { return m_isEval; }
        set { m_isEval = value; }
    }

    public void InitializeSampler(int seed)
    {
        m_sampler = new System.Random(seed);
    }

    public void OnEnable()
    {
        if (m_parameterArray == null) m_parameterArray = new ParameterNameMinMax[0];
        for (int i = 0; i < m_parameterArray.Length && i < m_parameterNames.Length; i++)
        {
            m_parameterArray[i].Name = m_parameterNames[i];
        }
        m_parameterList = m_parameterArray.ToList();
    }

    public void OnValidate()
    {
        if (m_parameterArray.Length > 0) m_parameterArray = m_parameterArray.Where(p => p != null).ToArray(); // Remove null entries

        if (m_parameterArray.Length != m_parameterNames.Length)
        {
            m_parameterArray = new ParameterNameMinMax[m_parameterNames.Length];
        }
        for (int i = 0; i < m_parameterArray.Length && i < m_parameterNames.Length; i++)
        {
            m_parameterArray[i].Name = m_parameterNames[i];
        }

        m_parameterList = m_parameterArray.ToList();
    }

    public float GetWithDefault(string name, float defaultValue)
    {
        if (m_isEval)
        {
            ParameterNameMinMax p = m_parameterList.Find(item => item.Name == name);
            if (p != null)
            {
                return p.Min + (float)m_sampler.NextDouble() * (p.Max - p.Min);
            }
            return defaultValue;
        }
        return Academy.Instance.EnvironmentParameters.GetWithDefault(name, defaultValue);
    }

    [SerializeField]
    private ParameterNameMinMax[] m_parameterArray;
    [SerializeField]
    private string[] m_parameterNames;

    private List<ParameterNameMinMax> m_parameterList;
    private bool m_isEval = false;
    private System.Random m_sampler = null;
}

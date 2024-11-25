using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EvalRecorder : MonoBehaviour
{
    public virtual void InitializeFile(string fileName)
    {
        using (StreamWriter writer = new StreamWriter(fileName, false))
        {

        }
    }

    public virtual void RecordLine(string fileName, string text)
    {
        using (StreamWriter writer = new StreamWriter(fileName, true))
        {
            writer.WriteLine(text);
        }
    }
}

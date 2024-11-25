using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RewardRecorder : EvalRecorder
{ 
    public void RecordReward(string fileName, float reward)
    {
        using (StreamWriter writer = new StreamWriter(fileName, true))
        {
            writer.WriteLine(reward);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICustomFluidActor
{
    public float MaterialState
    {
        get;
    }

    public int ParticleMaterialChangeCount
    {
        get;
        set;
    }
}

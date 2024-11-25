using System.Collections;
using System.Collections.Generic;
using PhysX5ForUnity;
using UnityEngine;

public class TestChangeFluidMaterial : MonoBehaviour
{
    void FixedUpdate()
    {
        if (shouldChange)
        {
            if (fluid.ParticleMaterialChangeCount > fluid.NumParticles * 0.8f)
            {
                originalFriction = fluid.PBDMaterial.Friction;
                originalCohesion = fluid.PBDMaterial.Cohesion;
                fluid.PBDMaterial.Friction = 0;
                fluid.PBDMaterial.Cohesion = 5;
                shouldChange = false;
            }
        }
    }

    void OnDisable()
    {
        fluid.PBDMaterial.Friction = originalFriction;
        fluid.PBDMaterial.Cohesion = originalCohesion;
    }

    [SerializeField]
    private CustomDiffuseMaterialFluidArrayActor fluid;

    private bool shouldChange = true;
    private float originalCohesion;
    private float originalFriction;
}

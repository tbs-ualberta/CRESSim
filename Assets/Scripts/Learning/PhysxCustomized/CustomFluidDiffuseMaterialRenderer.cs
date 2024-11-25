using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PhysX5ForUnity;
using UnityEngine;

public class CustomFluidDiffuseMaterialRenderer : PhysxPBDParticleSystemFluidRenderer
{
    public int[] ActorMaterialStateChangeParticleCounts
    {
        get { return m_actorMaterialStateChangeParticleCounts; }
    }

    public void ResetAllMaterialStates()
    {
        m_diffuseMaterialStateBuffer.SetData(m_diffuseMaterialInitialStates);
        m_diffuseMaterialNewStateBuffer.SetData(m_diffuseMaterialInitialStates);
        for (int i = 0; i < m_materialChangeActors.Count; i++)
        {
            m_materialChangeActors[i].ParticleMaterialChangeCount = 0;
        }
    }

    public void ResetMaterialStates(PhysxFluidActor actor)
    {
        m_diffuseMaterialStateBuffer.SetData(m_diffuseMaterialInitialStates, actor.ParticleData.IndexOffset, actor.ParticleData.IndexOffset, actor.NumParticles);
        m_diffuseMaterialNewStateBuffer.SetData(m_diffuseMaterialInitialStates, actor.ParticleData.IndexOffset, actor.ParticleData.IndexOffset, actor.NumParticles);
        ((ICustomFluidActor)actor).ParticleMaterialChangeCount = 0;
    }

    public override void AddActor(PhysxFluidActor actor)
    {
        base.AddActor(actor);
        m_actorDiffuseMaterialPropertyStates.Add(((ICustomFluidActor)actor).MaterialState);
    }

    public override void UpdateColorsBuffer()
    {
        base.UpdateColorsBuffer();
        m_newColorsBuffer.SetData(m_fluidColors.Take(m_numParticles).ToArray());
    }

    private void DiffuseColor()
    {
        ComputeBuffer oldColorBuffer;
        ComputeBuffer newColorBuffer;
        ComputeBuffer oldMaterialStateBuffer;
        ComputeBuffer newMaterialStateBuffer;
        if (m_swapBuffersFlag)
        {
            oldColorBuffer = m_colorsBuffer;
            newColorBuffer = m_newColorsBuffer;
            oldMaterialStateBuffer = m_diffuseMaterialStateBuffer;
            newMaterialStateBuffer = m_diffuseMaterialNewStateBuffer;
        }
        else
        {
            oldColorBuffer = m_newColorsBuffer;
            newColorBuffer = m_colorsBuffer;
            oldMaterialStateBuffer = m_diffuseMaterialNewStateBuffer;
            newMaterialStateBuffer = m_diffuseMaterialStateBuffer;
        }
        m_swapBuffersFlag = !m_swapBuffersFlag;

        // Clear the grid
        int kernel = m_colorDiffusionShader.FindKernel("ClearGrid");

        m_colorDiffusionShader.SetBuffer(kernel, "grid", m_diffuseColorGridBuffer);
        m_colorDiffusionShader.Dispatch(kernel, Mathf.CeilToInt(m_diffuseColorGridSize.x * m_diffuseColorGridSize.y * m_diffuseColorGridSize.z * m_diffuseColorMaxCellParticles / 1024.0f), 1, 1);

        // Add particles to the grid
        kernel = m_colorDiffusionShader.FindKernel("AddParticles");
        m_colorDiffusionShader.SetInt("indexCount", m_totalActiveIndicesCount);
        m_colorDiffusionShader.SetBuffer(kernel, "indices", m_indexBuffer);
        m_colorDiffusionShader.SetBuffer(kernel, "particles", m_particleBuffer);
        m_colorDiffusionShader.SetBuffer(kernel, "grid", m_diffuseColorGridBuffer);
        m_colorDiffusionShader.Dispatch(kernel, Mathf.CeilToInt(m_maxNumActiveIndices / 512.0f), 1, 1);

        // Diffuse colors and material states
        kernel = m_colorDiffusionShader.FindKernel("DiffuseColors");
        m_colorDiffusionShader.SetInt("indexCount", m_totalActiveIndicesCount);
        m_colorDiffusionShader.SetBuffer(kernel, "indices", m_indexBuffer);
        m_colorDiffusionShader.SetBuffer(kernel, "particles", m_particleBuffer);
        m_colorDiffusionShader.SetBuffer(kernel, "velocities", m_velocityBuffer);
        m_colorDiffusionShader.SetBuffer(kernel, "colors", oldColorBuffer);
        m_colorDiffusionShader.SetBuffer(kernel, "newColors", newColorBuffer);
        m_colorDiffusionShader.SetBuffer(kernel, "grid", m_diffuseColorGridBuffer);
        m_colorDiffusionShader.SetBuffer(kernel, "propertyStates", oldMaterialStateBuffer);
        m_colorDiffusionShader.SetBuffer(kernel, "newPropertyStates", newMaterialStateBuffer);
        m_colorDiffusionShader.Dispatch(kernel, Mathf.CeilToInt(m_maxNumActiveIndices / 512.0f), 1, 1);

        // Calculate particles
        for (int i = 0; i < m_materialChangeActors.Count; i++)
        {
            m_actorMaterialStateChangeParticleCounts[i] = 0;
        }
        m_diffuseMaterialCountBuffer.SetData(m_actorMaterialStateChangeParticleCounts);

        kernel = m_colorDiffusionShader.FindKernel("CountExceedingParticles");
        m_colorDiffusionShader.SetInt("totalLength", m_numParticles);
        m_colorDiffusionShader.SetFloat("threshold", m_materialChangeThreshold);
        m_colorDiffusionShader.SetInt("pairsCount", m_materialChangeActors.Count);
        m_colorDiffusionShader.SetBuffer(kernel, "propertyStates", oldMaterialStateBuffer);
        m_colorDiffusionShader.SetBuffer(kernel, "newPropertyStates", newMaterialStateBuffer);
        m_colorDiffusionShader.SetBuffer(kernel, "countBuffer", m_diffuseMaterialCountBuffer);
        m_colorDiffusionShader.SetBuffer(kernel, "startIndices", m_diffuseMaterialCountStartIndicesBuffer);
        m_colorDiffusionShader.SetBuffer(kernel, "endIndices", m_diffuseMaterialCountEndIndicesBuffer);
        m_colorDiffusionShader.Dispatch(kernel, Mathf.CeilToInt(m_numParticles / 1024.0f), 1, 1);

        m_diffuseMaterialCountBuffer.GetData(m_actorMaterialStateChangeParticleCounts);

        for (int i = 0; i < m_materialChangeActors.Count; i++)
        {
            m_materialChangeActors[i].ParticleMaterialChangeCount = m_actorMaterialStateChangeParticleCounts[i];
        }
    }

    protected override void CreateRenderResources()
    {
        base.CreateRenderResources();

        // for fluid mixing
        if (m_colorDiffusionShader)
        {
            CreateComputeBuffer(ref m_velocityBuffer, 16, m_numParticles);
            m_diffuseColorGridSize.x = Mathf.CeilToInt(m_diffuseColorGridRange.x / m_diffuseColorCellSize);
            m_diffuseColorGridSize.y = Mathf.CeilToInt(m_diffuseColorGridRange.y / m_diffuseColorCellSize);
            m_diffuseColorGridSize.z = Mathf.CeilToInt(m_diffuseColorGridRange.z / m_diffuseColorCellSize);
            Vector3 gridSize;
            gridSize.x = m_diffuseColorGridSize.x;
            gridSize.y = m_diffuseColorGridSize.y;
            gridSize.z = m_diffuseColorGridSize.z;
            CreateComputeBuffer(ref m_newColorsBuffer, sizeof(float) * 4, m_numParticles);
            m_newColorsBuffer.SetData(m_fluidColors.Take(m_numParticles).ToArray()); // this ensures the colors for inactive particles are still corrct and not overwritten
            CreateComputeBuffer(ref m_diffuseColorGridBuffer, sizeof(float) * 4, m_diffuseColorGridSize.x * m_diffuseColorGridSize.y * m_diffuseColorGridSize.z * m_diffuseColorMaxCellParticles);
            m_colorDiffusionShader.SetVector("gridSize", gridSize);
            m_colorDiffusionShader.SetFloat("maxCellParticles", m_diffuseColorMaxCellParticles);
            m_colorDiffusionShader.SetFloat("numParticles", m_numParticles);
            m_colorDiffusionShader.SetFloat("cellSize", m_diffuseColorCellSize);

            m_diffuseMaterialInitialStates = new float[m_numParticles];
            CreateComputeBuffer(ref m_diffuseMaterialStateBuffer, sizeof(float), m_numParticles);
            CreateComputeBuffer(ref m_diffuseMaterialNewStateBuffer, sizeof(float), m_numParticles);

            for (int i = 0; i < m_actors.Count; i++)
            {
                PhysxParticleActor actor = m_actors[i];
                int offset = actor.ParticleData.IndexOffset;
                int numParticles = actor.NumParticles;
                float state = m_actorDiffuseMaterialPropertyStates[i];
                if (Mathf.Approximately(state, 0))
                {
                    m_materialChangeActors.Add((ICustomFluidActor)actor);
                    m_actorMaterialStateChangeParticleStartIndices.Add(offset);
                    m_actorMaterialStateChangeParticleEndIndices.Add(offset + numParticles);
                }
                for (int j = offset; j < offset + numParticles; j++)
                {
                    m_diffuseMaterialInitialStates[j] = state;
                }
            }
            CreateComputeBuffer(ref m_diffuseMaterialCountBuffer, sizeof(int), m_materialChangeActors.Count);
            CreateComputeBuffer(ref m_diffuseMaterialCountStartIndicesBuffer, sizeof(int), m_materialChangeActors.Count);
            CreateComputeBuffer(ref m_diffuseMaterialCountEndIndicesBuffer, sizeof(int), m_materialChangeActors.Count);
            m_diffuseMaterialStateBuffer.SetData(m_diffuseMaterialInitialStates);
            m_diffuseMaterialNewStateBuffer.SetData(m_diffuseMaterialInitialStates);
            m_diffuseMaterialCountStartIndicesBuffer.SetData(m_actorMaterialStateChangeParticleStartIndices);
            m_diffuseMaterialCountEndIndicesBuffer.SetData(m_actorMaterialStateChangeParticleEndIndices);
            m_actorMaterialStateChangeParticleCounts = new int[m_materialChangeActors.Count];
        }
    }

    protected override void UpdateRenderResources()
    {
        base.UpdateRenderResources();
        m_velocityBuffer.SetData(m_pbdParticleSystem.SharedVelocity.Take(m_numParticles).ToArray());
        m_diffuseStep++;
        if (m_diffuseStep >= m_stepsPerDiffuse)
        {
            DiffuseColor();
            m_diffuseStep = 0;
        }
    }

    protected override void DestroyRenderResources()
    {
        base.DestroyRenderResources();

        if (m_newColorsBuffer != null) { m_newColorsBuffer.Release(); m_newColorsBuffer = null; }
        if (m_diffuseColorGridBuffer != null) { m_diffuseColorGridBuffer.Release(); m_diffuseColorGridBuffer = null; }
        if (m_diffuseMaterialStateBuffer != null) { m_diffuseMaterialStateBuffer.Release(); m_diffuseMaterialStateBuffer = null; }
        if (m_diffuseMaterialNewStateBuffer != null) { m_diffuseMaterialNewStateBuffer.Release(); m_diffuseMaterialNewStateBuffer = null; }
        if (m_diffuseMaterialCountBuffer != null) { m_diffuseMaterialCountBuffer.Release(); m_diffuseMaterialCountBuffer = null; }
        if (m_diffuseMaterialCountStartIndicesBuffer != null) { m_diffuseMaterialCountStartIndicesBuffer.Release(); m_diffuseMaterialCountStartIndicesBuffer = null; }
        if (m_diffuseMaterialCountEndIndicesBuffer != null) { m_diffuseMaterialCountEndIndicesBuffer.Release(); m_diffuseMaterialCountEndIndicesBuffer = null; }
        if (m_velocityBuffer != null) { m_velocityBuffer.Release(); m_velocityBuffer = null; }
    }

    [SerializeField]
    private Vector3 m_diffuseColorGridRange = new Vector3(10, 10, 10);
    [SerializeField]
    private float m_diffuseColorCellSize = 0.2f;
    [SerializeField]
    private int m_diffuseColorMaxCellParticles = 1;
    [SerializeField]
    private ComputeShader m_colorDiffusionShader;
    [SerializeField]
    private float m_materialChangeThreshold = 0.5f;
    [SerializeField]
    private int m_stepsPerDiffuse = 1;

    private Vector3Int m_diffuseColorGridSize;
    private ComputeBuffer m_newColorsBuffer, m_diffuseColorGridBuffer;
    private ComputeBuffer m_diffuseMaterialStateBuffer, m_diffuseMaterialNewStateBuffer, m_diffuseMaterialCountBuffer, m_diffuseMaterialCountStartIndicesBuffer, m_diffuseMaterialCountEndIndicesBuffer;
    private float[] m_diffuseMaterialInitialStates;
    private List<float> m_actorDiffuseMaterialPropertyStates = new List<float>();
    private List<int> m_actorMaterialStateChangeParticleStartIndices = new List<int>();
    private List<int> m_actorMaterialStateChangeParticleEndIndices = new List<int>();
    private int[] m_actorMaterialStateChangeParticleCounts;
    private List<ICustomFluidActor> m_materialChangeActors = new List<ICustomFluidActor>();
    private bool m_swapBuffersFlag = false;
    private int m_diffuseStep = 0;
    private ComputeBuffer m_velocityBuffer;
}

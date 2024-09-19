using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst.CompilerServices;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
[BurstCompile]
public class PheromoneManager : Singleton<PheromoneManager>
{
    public bool loadCenteredPheromones = true;
    
    
    public PheromoneData pheromoneData;
    private PheromoneData pheromoneDataBuffer;
    public GridData gridData;

    [Header("Gizmo Settings")]
    public bool drawGridGizmo = true;
    public bool drawIntensityGizmo = true;
    public float intensityGizmoRange = 100;
    

    // Start is called before the first frame update
    void Start()
    {
        //initialize pheromone map
        pheromoneData.pheromoneIntensity = new NativeArray<float>(gridData.gridWidth * gridData.gridHeight, Allocator.Persistent);
        if(loadCenteredPheromones){
            LoadCenteredPheromones();
        }
        else{
            //initialize pheromone map with 0
            for(int i = 0; i < pheromoneData.pheromoneIntensity.Length; i++){
                pheromoneData.pheromoneIntensity[i] = 0;
            }
        }
        pheromoneDataBuffer.pheromoneIntensity = new NativeArray<float>(gridData.gridWidth * gridData.gridHeight, Allocator.Persistent);
        NativeArray<float>.Copy(pheromoneData.pheromoneIntensity, pheromoneDataBuffer.pheromoneIntensity, pheromoneData.pheromoneIntensity.Length);
        GameManager.Instance.onPointTwoSecondTick.AddListener(CallPheromoneDecayJob);

    }
    private void LoadCenteredPheromones(){
        float maxDistance = math.distance(new float2(0, 0), new float2(gridData.gridWidth / 2, gridData.gridHeight / 2));

        //add a few pheromone sources, intensitiy higher in the middle
        for(int x = 0; x < gridData.gridWidth; x++)
        {
            for(int y = 0; y < gridData.gridHeight; y++)
            {
                float distance = math.distance(new float2(x, y), new float2(gridData.gridWidth / 2, gridData.gridHeight / 2));
                pheromoneData.pheromoneIntensity[x + y * gridData.gridWidth] = math.max(0, 1 - distance / maxDistance);
            }
        }
    }

    public void CallPheromoneDecayJob(){
        //decay existing pheromones
        PheromoneDecayJob decayJob = new PheromoneDecayJob();
        decayJob.pheromoneMap = pheromoneDataBuffer.pheromoneIntensity;
        decayJob.decayRate = pheromoneData.decayRate;
        decayJob.Schedule(pheromoneData.pheromoneIntensity.Length, 131072, Ants.Instance.antRotationJobHandle).Complete();
        //wait for ants to finish moving, then let them place pheromones
        Ants.Instance.antMovementJobHandle.Complete();
        AntPheromoneJob antPheromoneJob = new AntPheromoneJob();
        antPheromoneJob.gridData = gridData;
        antPheromoneJob.pheromoneMap = pheromoneDataBuffer.pheromoneIntensity;
        antPheromoneJob.pheromoneData = pheromoneData;
        NativeArray<float3> antPositions = new NativeArray<float3>(Ants.Instance.antData.antTransforms.length, Allocator.TempJob);
        for(int i = 0; i < Ants.Instance.antData.antTransforms.length; i++){
            antPositions[i] = Ants.Instance.antData.antTransforms[i].position;
        }
        antPheromoneJob.antPositions = antPositions;
        antPheromoneJob.Schedule().Complete();
        antPositions.Dispose();
        //copy the buffer to the actual pheromone data
        Ants.Instance.antRotationJobHandle.Complete();
        NativeArray<float>.Copy(pheromoneDataBuffer.pheromoneIntensity, pheromoneData.pheromoneIntensity, pheromoneData.pheromoneIntensity.Length);
    }

    /// <summary>
    /// Update pheromone map
    /// Does not take delta time, run at set intervals
    /// </summary>
    [BurstCompile]
    public struct PheromoneDecayJob : IJobParallelFor
    {
        public NativeArray<float> pheromoneMap;
        [ReadOnly]
        public float decayRate;

        public void Execute(int index)
        {
            // Apply the decay
            float intensity = pheromoneMap[index];
            Hint.Assume(intensity >= 0);
            Hint.Assume(intensity <= 1);

            if (intensity < 0.01f) 
            {
                pheromoneMap[index] = 0;
                return;
            }
            
            intensity *= 1 - decayRate;

            
            // Update the pheromone map
            pheromoneMap[index] = intensity;
        }
    }
    
    [BurstCompile]
    public struct AntPheromoneJob : IJob
    {
        [ReadOnly] public GridData gridData;
        public NativeArray<float> pheromoneMap;
        [ReadOnly] public NativeArray<float3> antPositions;
        [ReadOnly] public PheromoneData pheromoneData;

        public void Execute()
        {
            for(int i = 0; i < antPositions.Length; i++){
                // Get the ant's position
                float3 antPosition = antPositions[i];
                int x = (int)math.round(antPosition.x / gridData.gridResolution);
                int y = (int)math.round(antPosition.y / gridData.gridResolution);
                if(x < 0 || x >= gridData.gridWidth || y < 0 || y >= gridData.gridHeight)
                {
                    return;
                }
                // Get the pheromone intensity at the ant's position
                float intensity = pheromoneMap[x + y * gridData.gridWidth];
                // Update the pheromone intensity
                intensity += pheromoneData.increaseRate;
                intensity = math.min(1, intensity);
                pheromoneMap[x + y * gridData.gridWidth] = intensity;
            }
            
        }
    }




    // ---------------  GIZMO STUFF ---------------
    private void OnDrawGizmos() {
        if(drawGridGizmo){
            DrawGridGizmo();
        }
        if(drawIntensityGizmo){
            DrawIntensityGizmo();
        }
    }

    private void DrawGridGizmo(){
        //draw pheromone grid lines
        Gizmos.color = Color.grey;
        for(int x = 0; x < gridData.gridWidth; x++)
        {
            Gizmos.DrawLine(new Vector3(x * gridData.gridResolution, 0, 0), new Vector3(x * gridData.gridResolution, gridData.gridHeight * gridData.gridResolution, 0));
        }
        for(int y = 0; y < gridData.gridHeight; y++)
        {
            Gizmos.DrawLine(new Vector3(0, y * gridData.gridResolution, 0), new Vector3(gridData.gridWidth * gridData.gridResolution, y * gridData.gridResolution, 0));
        }
    }
    private void DrawIntensityGizmo(){
        // Get the camera's position
        Vector3 cameraPosition = Camera.main.transform.position;
        
        // Calculate the grid bounds based on the camera's position
        int minX = Mathf.Max(0, Mathf.FloorToInt((cameraPosition.x - gridData.gridResolution * intensityGizmoRange) / gridData.gridResolution));
        int maxX = Mathf.Min(gridData.gridWidth, Mathf.CeilToInt((cameraPosition.x + gridData.gridResolution * intensityGizmoRange) / gridData.gridResolution));
        int minY = Mathf.Max(0, Mathf.FloorToInt((cameraPosition.y - gridData.gridResolution * intensityGizmoRange) / gridData.gridResolution));
        int maxY = Mathf.Min(gridData.gridHeight, Mathf.CeilToInt((cameraPosition.y + gridData.gridResolution * intensityGizmoRange) / gridData.gridResolution));

        // If map is created, draw pheromone values within the camera's view
        if(pheromoneData.pheromoneIntensity.IsCreated)
        {
            for(int x = minX; x < maxX; x++)
            {
                for(int y = minY; y < maxY; y++)
                {
                    float intensity = pheromoneData.pheromoneIntensity[x + y * gridData.gridWidth];
                    if(intensity > 0)
                    {
                        // Color based on intensity
                        Gizmos.color = new Color(1,0,0, intensity);
                        Gizmos.DrawCube(new Vector3(x * gridData.gridResolution - gridData.gridResolution/2, y * gridData.gridResolution - gridData.gridResolution/2, 0), new Vector3(gridData.gridResolution, gridData.gridResolution, 0));
                    }
                }
            }
        }
    }
}
[System.Serializable]
[BurstCompile]

public struct PheromoneData{
    /// <summary>
    ///The actual pheromone value. Index with x + y * width
    /// </summary>
    public NativeArray<float> pheromoneIntensity;
    
    public float decayRate; // 0.1
    public float increaseRate; 
}
[System.Serializable]
[BurstCompile]
public struct GridData{
    public int gridWidth;
    public int gridHeight;
    public float gridResolution;
}

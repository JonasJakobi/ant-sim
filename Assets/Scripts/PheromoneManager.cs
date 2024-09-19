using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
[BurstCompile]
public class PheromoneManager : Singleton<PheromoneManager>
{
    public bool drawGridGizmo = true;
    public bool drawIntensityGizmo = true;
    
    public PheromoneData pheromoneData;
    public GridData gridData;
    

    // Start is called before the first frame update
    void Start()
    {
        //initialize pheromone map
        pheromoneData.pheromoneIntensity = new NativeArray<float>(gridData.gridWidth * gridData.gridHeight, Allocator.Persistent);
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



    [BurstCompile]
    public struct PheromoneDecayJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> pheromoneMap;
        [ReadOnly]
        public float decayRate;
        public void Execute(int index)
        {
            pheromoneMap[index] = math.max(0, pheromoneMap[index] - decayRate);
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
        //if map is created, draw pheromone values
        if(pheromoneData.pheromoneIntensity.IsCreated)
        {
            for(int x = 0; x < gridData.gridWidth; x++)
            {
                for(int y = 0; y < gridData.gridHeight; y++)
                {
                    float intensity = pheromoneData.pheromoneIntensity[x + y * gridData.gridWidth];
                    if(intensity > 0)
                    {
                        //color based on intensity
                        Gizmos.color = new Color(1, 1 - intensity, 1 - intensity, 1);
                        Gizmos.DrawCube(new Vector3(x * gridData.gridResolution - gridData.gridResolution/2, y * gridData.gridResolution- gridData.gridResolution/2, 0), new Vector3(gridData.gridResolution, gridData.gridResolution, 0));
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
}
[System.Serializable]
[BurstCompile]
public struct GridData{
    public int gridWidth;
    public int gridHeight;
    public float gridResolution;
}

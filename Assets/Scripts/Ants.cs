using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using System.Runtime.CompilerServices;
[BurstCompile]
public class Ants : MonoBehaviour
{   
    public int numAnts = 1000;
    public int randomNumberAmount = 50000;
    public AntStats antStats;

    public Vector2 xBounds = new Vector2(0.5f, 2);
    public Vector2 yBounds = new Vector2(0.5f, 2);
    public float maxAngleChange = 0.1f;
    public float minAngleChange = -0.1f;
    [SerializeField] private GameObject _antPrefab;

    private bool _spawned = false;
    private List<Transform> _antTransforms = new List<Transform>();
    private TransformAccessArray _antTransformAccessArray;
    public NativeArray<float> randomNumbers;
    

    //---------------  UNITY CALLBACKS ------
    void Start()
    {
        GenerateRandomNumbers();
        InitializeAnts();
    }

    void Update()
    {
        if(! _spawned){
            return;
        }
        CallMoveAntsJob();

    }
    //---------------  INTERNAL PRIVATE METHODS ------
    private void InitializeAnts(){
        //spawn numAnts at random positions in [-1,1] x and y with random angles (2d)
        for(int i = 0; i < numAnts; i++)
        {
            GameObject ant = Instantiate(_antPrefab, new Vector3(UnityEngine.Random.Range(xBounds.x , xBounds.y), UnityEngine.Random.Range(yBounds.x, yBounds.y), 0), Quaternion.identity);
            ant.transform.Rotate(0, 0, UnityEngine.Random.Range(0, 360));
            _antTransforms.Add(ant.transform);
        }
        _antTransformAccessArray = new TransformAccessArray(_antTransforms.ToArray());
        _spawned = true;
    }
    private void GenerateRandomNumbers(){
        randomNumbers = new NativeArray<float>(randomNumberAmount, Allocator.Persistent);
        for(int i = 0; i < randomNumberAmount; i++)
        {
            randomNumbers[i] = UnityEngine.Random.Range(minAngleChange, maxAngleChange);
        }
    }

    
    private void CallMoveAntsJob(){
        ControlAntsJob controlAntsJob = new ControlAntsJob();
        controlAntsJob.deltaTime = Time.deltaTime;
        controlAntsJob.antStats = antStats;
        controlAntsJob.frameCount = Time.frameCount;
        controlAntsJob.randomRotations = randomNumbers;
        controlAntsJob.gridData = PheromoneManager.Instance.gridData;
        controlAntsJob.pheromoneData = PheromoneManager.Instance.pheromoneData;
        JobHandle jobHandle = controlAntsJob.Schedule(_antTransformAccessArray);
    }

    // ------------------ JOBS ------
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
   // [BurstCompile]
    public struct ControlAntsJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float> randomRotations;
        [ReadOnly] public int frameCount;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public AntStats antStats;
        [ReadOnly] public PheromoneData pheromoneData;
        [ReadOnly] public GridData gridData;
        public void Execute(int index, TransformAccess transform)
        {
            //move forward
            transform.position += transform.localRotation * new float3(1,0,0) * antStats.speed * deltaTime;

            //Random rotation
            quaternion rotation = quaternion.Euler(0, 0, GetRandomAngle(index));
            transform.rotation = math.mul(transform.rotation, rotation);
        
            //rotate towards pheromone
            float frontLeftPheromone = GetPheromoneIntensity(transform.position, transform.rotation, math.PI / 4);  // 45 degrees left
            float frontRightPheromone = GetPheromoneIntensity(transform.position, transform.rotation, -math.PI / 4); // 45 degrees right

            if(frontLeftPheromone < frontRightPheromone){
                transform.rotation = math.mul(transform.rotation, quaternion.Euler(0, 0, -antStats.pheromonerotationSpeed * frontLeftPheromone * deltaTime));
            }
            else{
                transform.rotation = math.mul(transform.rotation, quaternion.Euler(0, 0, antStats.pheromonerotationSpeed * frontRightPheromone * deltaTime));
            }

            



        
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetGridIndex(float3 position){
            int gridX = (int)math.floor(position.x / gridData.gridResolution);
            int gridY = (int)math.floor(position.y / gridData.gridResolution);
            return gridX + gridY * gridData.gridWidth;
        } 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetRandomAngle(int index){
            //random rotation
            int mangledFrameCount = (frameCount * 13) ^ (frameCount >> 2) * index;
            float randomAngle = randomRotations[mangledFrameCount % randomRotations.Length] * antStats.rotationSpeed * deltaTime;
            return randomAngle;
        }
        // Get the pheromone intensity at the specified angle (forward, front-left, front-right)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetPheromoneIntensity(float3 position, quaternion rotation, float angleOffset)
        {
            // Get the ant's direction and apply the offset
            quaternion adjustedRotation = math.mul(rotation, quaternion.Euler(0, 0, angleOffset));
            float3 forwardDirection = math.mul(adjustedRotation, new float3(1, 0, 0));

            // Calculate the new position based on the direction
            float3 newPosition = position + forwardDirection * gridData.gridResolution; // Move one cell forward

            // Get the grid index at the new position
            int newGridIndex = GetGridIndex(newPosition);

            // Return the pheromone intensity at the new grid position
            return pheromoneData.pheromoneIntensity[newGridIndex];
        }

    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3((xBounds.x + xBounds.y) / 2, (yBounds.x + yBounds.y) / 2, 0), new Vector3(xBounds.y - xBounds.x, yBounds.y - yBounds.x, 0));
    
    }

}

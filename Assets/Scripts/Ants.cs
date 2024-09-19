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
public class Ants : Singleton<Ants>
{   
    public int numAnts = 1000;
    public int randomNumberAmount = 50000;
    public AntStats antStats;
    public AntData antData;

    public Vector2 xBounds = new Vector2(0.5f, 2);
    public Vector2 yBounds = new Vector2(0.5f, 2);
    public float maxAngleChange = 0.1f;
    public float minAngleChange = -0.1f;
    [SerializeField] private GameObject _antPrefab;

    private bool _spawned = false;
    private List<Transform> _antTransforms = new List<Transform>();
    private NativeArray<float> _randomNumbers;

    public JobHandle antRotationJobHandle;
    public JobHandle antMovementJobHandle;
    

    //---------------  UNITY CALLBACKS ------
    void Start()
    {
        GenerateRandomNumbers();
        InitializeAnts();
        GameManager.Instance.onPointOneSecondTick.AddListener(CallUpdateAntsRotationJob);
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
        antData.antTransforms = new TransformAccessArray(_antTransforms.ToArray());
        _spawned = true;
    }
    private void GenerateRandomNumbers(){
        _randomNumbers = new NativeArray<float>(randomNumberAmount, Allocator.Persistent);
        for(int i = 0; i < randomNumberAmount; i++)
        {
            _randomNumbers[i] = UnityEngine.Random.Range(minAngleChange, maxAngleChange);
        }
    }
    private void CallMoveAntsJob(){
        antRotationJobHandle.Complete();
        MoveAntsJob moveAntsJob = new MoveAntsJob();
        moveAntsJob.antStats = antStats;
        moveAntsJob.deltaTime = Time.deltaTime;
        moveAntsJob.gridData = PheromoneManager.Instance.gridData;
        antMovementJobHandle = moveAntsJob.Schedule(antData.antTransforms);
    }
    
    private void CallUpdateAntsRotationJob(){
        UpdateAntsRotationJob updateAntsRotationJob = new UpdateAntsRotationJob();
        updateAntsRotationJob.antStats = antStats;
        updateAntsRotationJob.frameCount = Time.frameCount;
        updateAntsRotationJob.randomRotations = _randomNumbers;
        updateAntsRotationJob.gridData = PheromoneManager.Instance.gridData;
        updateAntsRotationJob.pheromoneData = PheromoneManager.Instance.pheromoneData;
        antRotationJobHandle = updateAntsRotationJob.Schedule(antData.antTransforms);
    }

    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
   // [BurstCompile]
    public struct MoveAntsJob : IJobParallelForTransform
    {
        [ReadOnly] public GridData gridData;
        [ReadOnly] public AntStats antStats;
        [ReadOnly] public float deltaTime;
        public void Execute(int index, TransformAccess transform)
        {
            //move forward
            transform.position += transform.localRotation * new float3(1,0,0) * antStats.speed * deltaTime;
            //if out of bounds. loop around
            if(transform.position.x < 0){
                transform.position = new float3(gridData.gridWidth * gridData.gridResolution, transform.position.y, transform.position.z);
            }
            else if(transform.position.x > gridData.gridWidth * gridData.gridResolution){
                transform.position = new float3(0, transform.position.y, transform.position.z);
            }
            if(transform.position.y < 0){
                transform.position = new float3(transform.position.x, gridData.gridHeight * gridData.gridResolution, transform.position.z);
            }
            else if(transform.position.y > gridData.gridHeight * gridData.gridResolution){
                transform.position = new float3(transform.position.x, 0, transform.position.z);
            }

        }
    }
    // ------------------ JOBS ------
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
   // [BurstCompile]
    public struct UpdateAntsRotationJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float> randomRotations;
        [ReadOnly] public int frameCount;
        [ReadOnly] public AntStats antStats;
        [ReadOnly] public PheromoneData pheromoneData;
        [ReadOnly] public GridData gridData;
        public void Execute(int index, TransformAccess transform)
        {
            
            //Random rotation
            quaternion rotation = quaternion.Euler(0, 0, GetRandomAngle(index));
            transform.rotation = math.mul(transform.rotation, rotation);
        
            //rotate towards pheromone
            float frontPheromone = GetPheromoneIntensity(transform.position, transform.rotation, 0); // forward
            float frontLeftPheromone = GetPheromoneIntensity(transform.position, transform.rotation, math.PI / 4);  // 45 degrees left
            float frontRightPheromone = GetPheromoneIntensity(transform.position, transform.rotation, -math.PI / 4); // 45 degrees right

            if(frontRightPheromone > frontLeftPheromone && frontRightPheromone > frontPheromone){
                transform.rotation = math.mul(transform.rotation, quaternion.Euler(0, 0, -antStats.pheromonerotationSpeed * frontRightPheromone));
            }
            else if (frontLeftPheromone > frontRightPheromone && frontLeftPheromone > frontPheromone){
                transform.rotation = math.mul(transform.rotation, quaternion.Euler(0, 0, antStats.pheromonerotationSpeed * frontLeftPheromone));
            }

            



        
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetGridIndex(float3 position){
            int gridX = (int)math.round(position.x / gridData.gridResolution);
            int gridY = (int)math.round(position.y / gridData.gridResolution);
            if(gridX < 0 || gridX >= gridData.gridWidth || gridY < 0 || gridY >= gridData.gridHeight)
            {
                return -1;
            }
            return gridX + gridY * gridData.gridWidth;
        } 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetRandomAngle(int index){
            //random rotation
            int mangledFrameCount = (frameCount * 13) ^ (frameCount >> 2) * index;
            float randomAngle = randomRotations[mangledFrameCount % randomRotations.Length] * antStats.rotationSpeed;
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
            if(newGridIndex == -1){ // index of -1 means out of bounds
                return 0;
            }
            // Return the pheromone intensity at the new grid position
            return pheromoneData.pheromoneIntensity[newGridIndex];
        }

    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3((xBounds.x + xBounds.y) / 2, (yBounds.x + yBounds.y) / 2, 0), new Vector3(xBounds.y - xBounds.x, yBounds.y - yBounds.x, 0));
    
    }

}

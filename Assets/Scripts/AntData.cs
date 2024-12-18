using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
/// <summary>
/// Stats of all ants
/// </summary>
[System.Serializable]
[BurstCompile]
public struct AntStats{
    public float speed;
    public float rotationSpeed;
    public float pheromonerotationSpeed;

}

[System.Serializable]
[BurstCompile]
public struct AntData{
    public TransformAccessArray antTransforms;
}
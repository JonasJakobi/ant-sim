using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
[BurstCompile]
public class CollisionMapManager : Singleton<CollisionMapManager>
{

}
[System.Serializable]
[BurstCompile]

public struct CollisionMapData {
   
    public NativeArray<bool> isTerrain;
    
}

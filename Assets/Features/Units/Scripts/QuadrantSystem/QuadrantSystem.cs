using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(UnitLateUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct QuadrantSystem : ISystem
{
    private const int ZMultiplier = 1000;
    private const int CellSize = 10;

    private NativeParallelMultiHashMap<int, QuadrantData> playerMap;
    private NativeParallelMultiHashMap<int, QuadrantData> enemyMap;
    private EntityQuery playerQuery;
    private EntityQuery enemyQuery;
    
    public void OnCreate(ref SystemState state)
    {
        playerMap = new NativeParallelMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        enemyMap = new NativeParallelMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);


        playerQuery = SystemAPI.QueryBuilder()
            .WithAll<LocalTransform>()
            .WithAll<UnitAliveState>()
            .WithAll<PlayerTag>()
            .WithDisabled<UnitChargingCD>()
            .Build();
        
        enemyQuery = SystemAPI.QueryBuilder()
            .WithAll<LocalTransform>()
            .WithAll<UnitAliveState>()
            .WithAll<EnemyTag>()
            .WithDisabled<UnitChargingCD>()
            .Build();

        var e = state.EntityManager.CreateEntity(typeof(QuadrantMaps));
        state.EntityManager.SetComponentData(e, new QuadrantMaps()
        {
            PlayerMap = playerMap,
            EnemyMap = enemyMap,
            CellSize = CellSize,
            ZMultiplier = ZMultiplier
        });
    }
    
    public void OnUpdate(ref SystemState state)
    {
        ResetMap(ref playerMap, ref playerQuery);
        ResetMap(ref enemyMap, ref enemyQuery);
        
        ScheduleJob(ref state, ref playerQuery, ref playerMap);
        ScheduleJob(ref state, ref enemyQuery, ref enemyMap);
    }

    private void ScheduleJob(ref SystemState state, ref EntityQuery query, ref NativeParallelMultiHashMap<int, QuadrantData> map)
    {
        SetQuadrantDataHashMapJob job = new SetQuadrantDataHashMapJob
        {
            HashMap = map.AsParallelWriter()
        };
        state.Dependency = job.ScheduleParallel(query, state.Dependency);
        state.Dependency.Complete();
    }

    private void ResetMap(ref NativeParallelMultiHashMap<int, QuadrantData> map, ref EntityQuery query)
    {
        map.Clear();
        var estimatedUnitCount = query.CalculateEntityCount();
        if (map.Capacity < estimatedUnitCount)
        {
            map.Capacity = estimatedUnitCount;
        }
    }

    public void OnDestroy(ref SystemState state)
    {
        playerMap.Dispose();
        enemyMap.Dispose();
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct SetQuadrantDataHashMapJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, QuadrantData>.ParallelWriter HashMap;

        private void Execute(Entity entity, in LocalTransform transform)
        {
            int hashMapKey = (int) (math.floor(transform.Position.x / CellSize) + (ZMultiplier * math.floor(transform.Position.z / CellSize)));
            HashMap.Add(hashMapKey, new QuadrantData()
            {
                Entity = entity,
                Position = transform.Position
            });            
        }
    }
}

public struct QuadrantData
{
    public Entity Entity;
    public float3 Position;
}

public struct QuadrantMaps : IComponentData
{
    [ReadOnly] public int ZMultiplier;
    [ReadOnly] public int CellSize;
    
    [ReadOnly] public NativeParallelMultiHashMap<int, QuadrantData> PlayerMap;
    [ReadOnly] public NativeParallelMultiHashMap<int, QuadrantData> EnemyMap;
    
    public int GetPositionHashMapKey(float3 position)
    {
        return (int) (math.floor(position.x / CellSize) + (ZMultiplier * math.floor(position.z / CellSize)));
    }

    public NativeArray<int> GetNeighborHashMapKeys(int hashMapKey)
    {
        NativeArray<int> neighborKeys = new NativeArray<int>(9, Allocator.Temp);
        neighborKeys[0] = hashMapKey;
        neighborKeys[3] = hashMapKey + ZMultiplier;
        neighborKeys[4] = hashMapKey - ZMultiplier;
        neighborKeys[1] = hashMapKey + 1;
        neighborKeys[2] = hashMapKey - 1;
        neighborKeys[5] = hashMapKey + ZMultiplier + 1;
        neighborKeys[6] = hashMapKey + ZMultiplier - 1;
        neighborKeys[7] = hashMapKey - ZMultiplier + 1;
        neighborKeys[8] = hashMapKey - ZMultiplier - 1;
        return neighborKeys;
    }

    public NativeParallelMultiHashMap<int, QuadrantData> GetMap<T>() where T : IComponentData
    {
        if (typeof(T) == typeof(PlayerTag))
        {
            return PlayerMap;
        }
        else
        {
            return EnemyMap;
        }
    }
}
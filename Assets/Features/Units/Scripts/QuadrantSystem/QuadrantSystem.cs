using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct QuadrantSystem : ISystem
{
    private const int ZMultiplier = 1000;
    private const int CellSize = 10;

    private EntityQuery _active;
    
    public NativeParallelMultiHashMap<int, QuadrantData> playerMap;
    public NativeParallelMultiHashMap<int, QuadrantData> enemyMap;
    private EntityQuery playerQuery;
    private EntityQuery enemyQuery;

    public void OnCreate(ref SystemState state)
    {
        _active = SystemAPI.QueryBuilder()
            .WithDisabled<UnitChargingState>()
            .Build();
        
        playerMap = new NativeParallelMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        enemyMap = new NativeParallelMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        
        
        EntityQueryBuilder pbuilder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<LocalTransform>()
            .WithAll<UnitAliveState>()
            .WithAll<PlayerTag>()
            .WithDisabled<UnitChargingState>();
        
        EntityQueryBuilder ebuilder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<LocalTransform>()
            .WithAll<UnitAliveState>()
            .WithAll<EnemyTag>()
            .WithDisabled<UnitChargingState>();
        
        playerQuery = state.GetEntityQuery(in pbuilder);
        enemyQuery = state.GetEntityQuery(in ebuilder);

        var e = state.EntityManager.CreateEntity(typeof(QuadrantMaps));
        state.EntityManager.SetComponentData(e, new QuadrantMaps()
        {
            playerMap = playerMap,
            enemyMap = enemyMap,
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

        // var singleton = SystemAPI.GetSingletonRW<QuadrantMaps>();
        // singleton.ValueRW.playerMap = playerMap;
        // singleton.ValueRW.enemyMap = enemyMap;
    }

    private void ScheduleJob(ref SystemState state, ref EntityQuery query, ref NativeParallelMultiHashMap<int, QuadrantData> map)
    {
        SetQuadrantDataHashMapJob job = new SetQuadrantDataHashMapJob
        {
            HashMap = map.AsParallelWriter()
        };
        JobHandle jobHandle = job.ScheduleParallel(query, state.Dependency);
        jobHandle.Complete();
    }

    private void ResetMap(ref NativeParallelMultiHashMap<int, QuadrantData> map, ref EntityQuery query)
    {
        map.Clear();
        int estimatedUnitCount = query.CalculateEntityCount();
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
    private partial struct SetQuadrantDataHashMapJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, QuadrantData>.ParallelWriter HashMap;
        public void Execute(Entity entity, in LocalTransform transform)
        {
            int hashMapKey = (int) (math.floor(transform.Position.x / CellSize) + (ZMultiplier * math.floor(transform.Position.z / CellSize)));
            HashMap.Add(hashMapKey, new QuadrantData()
            {
                entity = entity,
                position = transform.Position
            });            
        }
    }
    
    private int GetPositionHashMapKey(float3 position)
    {
        return (int) (math.floor(position.x / CellSize) + (ZMultiplier * math.floor(position.z / CellSize)));
    }

    private int GetEntityCountInHashMap(NativeParallelMultiHashMap<int, QuadrantData> hashMap, int hashKey)
    {
        QuadrantData data;
        NativeParallelMultiHashMapIterator<int> iterator;
        int count = 0;
        if (hashMap.TryGetFirstValue(hashKey, out data, out iterator))
        {
            do
            {
                count++;
            } while (hashMap.TryGetNextValue(out data, ref iterator));
        }

        return count;
    }
}

public struct QuadrantData
{
    public Entity entity;
    public float3 position;
}

public struct QuadrantMaps : IComponentData
{
    [ReadOnly] public int ZMultiplier;
    [ReadOnly] public int CellSize;
    
    [ReadOnly] public NativeParallelMultiHashMap<int, QuadrantData> playerMap;
    [ReadOnly] public NativeParallelMultiHashMap<int, QuadrantData> enemyMap;
    
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
            return playerMap;
        }
        else
        {
            return enemyMap;
        }
    }
}
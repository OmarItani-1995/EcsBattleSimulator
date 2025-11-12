using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitPreUpdateSystemGroup))]
public partial struct TargetingSystem : ISystem 
{
    private EntityQuery playerQuery;
    private EntityQuery enemyQuery;
    private Random random;
    public void OnCreate(ref SystemState state)
    {
        random = Random.CreateFromIndex(0);
        playerQuery = new EntityQueryBuilder(Allocator.Persistent) 
            .WithAll<PlayerTag>()
            .WithAll<LocalTransform>()
            .WithAll<UnitAliveState>()
            .WithNone<UnitTargetCD>()
            .WithDisabled<UnitChargingState>()
            .Build(ref state);
        
        enemyQuery = new EntityQueryBuilder(Allocator.Persistent) 
            .WithAll<EnemyTag>()
            .WithAll<LocalTransform>()
            .WithAll<UnitAliveState>()
            .WithNone<UnitTargetCD>()
            .WithDisabled<UnitChargingState>()
            .Build(ref state);

        state.RequireForUpdate(playerQuery);
        state.RequireForUpdate(enemyQuery);
        state.RequireForUpdate<QuadrantMaps>();
        state.RequireForUpdate<UnitPreUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var quadrantMaps = SystemAPI.GetSingleton<QuadrantMaps>();
        var ecb = SystemAPI.GetSingleton<UnitPreUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        
        if (playerQuery.CalculateEntityCount() > 0)
        {
            FindTargets(playerQuery, ref quadrantMaps, ref quadrantMaps.enemyMap, commandBuffer, ref state);
        }

        if (enemyQuery.CalculateEntityCount() > 0)
        {
            FindTargets(enemyQuery, ref quadrantMaps, ref quadrantMaps.playerMap, commandBuffer, ref state);
        }
    }

    private void FindTargets(EntityQuery query, ref QuadrantMaps maps,
        ref NativeParallelMultiHashMap<int, QuadrantData> targets, EntityCommandBuffer.ParallelWriter ecb,
        ref SystemState state)
    {
        var frame = (uint)math.floor(SystemAPI.Time.ElapsedTime);
        var findTargetJob = new FindTargetJob
        {
            quadrantMaps = maps,
            targetsMap = targets,
            Frame = frame,
            Seed = random.NextUInt(),
            ecb = ecb,
        };
        
        state.Dependency = findTargetJob.ScheduleParallel(query, state.Dependency);
    }

    [BurstCompile]
    private partial struct FindTargetJob : IJobEntity
    {
        [ReadOnly] public QuadrantMaps quadrantMaps;
        [ReadOnly] public NativeParallelMultiHashMap<int, QuadrantData> targetsMap;
        [ReadOnly] public uint Frame;
        [ReadOnly] public uint Seed;
        
        public EntityCommandBuffer.ParallelWriter ecb;
        
        public void Execute([EntityIndexInQuery] int entityInQueryIndex, Entity entity, in LocalTransform transform)
        {
            int hashMapKey = quadrantMaps.GetPositionHashMapKey(transform.Position);
            NativeArray<int> neighborKeys = quadrantMaps.GetNeighborHashMapKeys(hashMapKey);
            NativeArray<QuadrantData> foundTargets = new NativeArray<QuadrantData>(10, Allocator.Temp);
            int foundCount = 0;
            for (int i = 0; i < neighborKeys.Length; i++)
            {
                FindClosestTarget(in transform, neighborKeys[i], ref foundTargets, ref foundCount);
                if (foundCount >= 5)
                {
                    break;
                }
            }

            if (foundCount > 0)
            {
                uint h = math.hash(new uint3((uint)entityInQueryIndex, (uint)Frame, Seed));
                int idx = (int)(h % (uint)foundCount);
                
                ecb.AddComponent(entityInQueryIndex, entity, new UnitTargetCD
                {
                    targetEntity = foundTargets[idx].entity,
                });

                if (idx % 2 == 0)
                {
                    ecb.AddComponent(entityInQueryIndex, foundTargets[idx].entity, new UnitTargetCD()
                    {
                        targetEntity = entity,
                    });
                }
            }
            
            neighborKeys.Dispose();
            foundTargets.Dispose();
        }

        private void FindClosestTarget(in LocalTransform transform, int hashMapKey, ref NativeArray<QuadrantData> foundTargets, ref int foundCount)
        {
            if (targetsMap.TryGetFirstValue(hashMapKey, out QuadrantData targetData,
                    out NativeParallelMultiHashMapIterator<int> iterator))
            {
                do
                {
                    // float distance = math.distance(transform.Position, targetData.position);
                    // if (closestDistance == 0 || distance < closestDistance)
                    // {
                    //     closestDistance = distance;
                    //     closestTarget = targetData;
                    // }
                    foundTargets[foundCount] = targetData;
                    foundCount = foundCount + 1;
                } while (foundCount < foundTargets.Length && targetsMap.TryGetNextValue(out targetData, ref iterator));
            }
        }
    }
}

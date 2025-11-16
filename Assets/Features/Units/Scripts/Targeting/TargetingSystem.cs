using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitPreUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct TargetingSystem : ISystem 
{
    private EntityQuery playerQuery;
    private EntityQuery enemyQuery;
    private Random random;
    public void OnCreate(ref SystemState state)
    {
        random = Random.CreateFromIndex(0);
        playerQuery = SystemAPI.QueryBuilder()
            .WithAll<PlayerTag>()
            .WithAll<LocalTransform>()
            .WithAll<UnitAliveState>()
            .WithDisabled<UnitTargetCD>()
            .WithDisabled<UnitChargingState>()
            .Build();
        
        enemyQuery = SystemAPI.QueryBuilder()
            .WithAll<EnemyTag>()
            .WithAll<LocalTransform>()
            .WithAll<UnitAliveState>()
            .WithDisabled<UnitTargetCD>()
            .WithDisabled<UnitChargingState>()
            .Build();

        // state.RequireForUpdate(playerQuery);
        // state.RequireForUpdate(enemyQuery);
        state.RequireForUpdate<QuadrantMaps>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var quadrantMaps = SystemAPI.GetSingleton<QuadrantMaps>();
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        
        FindTargets(playerQuery, ref quadrantMaps, ref quadrantMaps.EnemyMap, commandBuffer, ref state);
        FindTargets(enemyQuery, ref quadrantMaps, ref quadrantMaps.PlayerMap, commandBuffer, ref state);
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
    [StructLayout(LayoutKind.Auto)]
    private partial struct FindTargetJob : IJobEntity
    {
        [ReadOnly] public QuadrantMaps quadrantMaps;
        [ReadOnly] public NativeParallelMultiHashMap<int, QuadrantData> targetsMap;
        [ReadOnly] public uint Frame;
        [ReadOnly] public uint Seed;
        
        public EntityCommandBuffer.ParallelWriter ecb;

        private void Execute([EntityIndexInQuery] int index, Entity entity, in LocalTransform transform, ref UnitTargetCD target)
        {
            int hashMapKey = quadrantMaps.GetPositionHashMapKey(transform.Position);
            var neighborKeys = quadrantMaps.GetNeighborHashMapKeys(hashMapKey);
            var foundTargets = new NativeArray<QuadrantData>(10, Allocator.Temp);
            var foundCount = 0;
            foreach (var key in neighborKeys)
            {
                FindClosestTarget(key, ref foundTargets, ref foundCount);
                if (foundCount >= 5)
                {
                    break;
                }
            }

            if (foundCount > 0)
            {
                var h = math.hash(new uint3((uint)index, Frame, Seed));
                var idx = (int)(h % (uint)foundCount);

                target.TargetEntity = foundTargets[idx].Entity;
                ecb.SetComponentEnabled<UnitTargetCD>(index, entity, true);
                if (idx % 2 == 0)
                {
                    ecb.AddComponent(index, foundTargets[idx].Entity, new UnitTargetCD()
                    {
                        TargetEntity = entity,
                    });
                }
            }
            
            neighborKeys.Dispose();
            foundTargets.Dispose();
        }

        private void FindClosestTarget(int hashMapKey, ref NativeArray<QuadrantData> foundTargets, ref int foundCount)
        {
            if (!targetsMap.TryGetFirstValue(hashMapKey, out var targetData,
                    out var iterator)) return;
            do
            {
                foundTargets[foundCount] = targetData;
                foundCount = foundCount + 1;
            } while (foundCount < foundTargets.Length && targetsMap.TryGetNextValue(out targetData, ref iterator));
        }
    }
}

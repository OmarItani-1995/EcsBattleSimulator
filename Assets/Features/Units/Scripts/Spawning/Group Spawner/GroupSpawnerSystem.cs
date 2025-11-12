using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(WorldSystemGroup))]
public partial struct GroupSpawnerSystem : ISystem
{
    private EntityQuery _query;
    
    public void OnCreate(ref SystemState state)
    {
        _query = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<GroupSpawnerCD>()
            .WithAll<LocalTransform>()
            .Build(ref state);
        state.RequireForUpdate(_query);
        state.RequireForUpdate<WorldSystemEndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = SystemAPI.GetSingleton<WorldSystemEndSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        var job = new GroupSpawnerJob
        {
            DeltaTime = deltaTime,
            Ecb = commandBuffer,
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
    
    [BurstCompile]
    private partial struct GroupSpawnerJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter Ecb;
        public void Execute([ChunkIndexInQuery] int index, Entity entity, ref GroupSpawnerCD spawner, in LocalTransform transform)
        {
            spawner.SpawnTimer -= DeltaTime;
            if (spawner.SpawnTimer <= 0)
            {
                var newEntity = Ecb.Instantiate(index, spawner.groupToSpawn);
                Ecb.SetComponent(index, newEntity, transform);
                spawner.SpawnTimer = spawner.SpawnInterval;
            }
        }
    }
}

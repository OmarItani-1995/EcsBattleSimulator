using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
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
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        var job = new GroupSpawnerJob
        {
            DeltaTime = deltaTime,
            Ecb = ecb.AsParallelWriter(),
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
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

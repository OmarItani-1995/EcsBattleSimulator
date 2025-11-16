using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitLateUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct GroupSpawnerSystem : ISystem
{
    private EntityQuery _query;
    
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<GroupSpawnerCD>()
            .WithAll<LocalTransform>()
            .Build();
        state.RequireForUpdate(_query);
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        var job = new GroupSpawnerJob
        {
            DeltaTime = deltaTime,
            Ecb = commandBuffer,
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
    
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct GroupSpawnerJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int index, ref GroupSpawnerCD spawner, in LocalTransform transform)
        {
            spawner.SpawnTimer -= DeltaTime;
            if (!(spawner.SpawnTimer <= 0)) return;
            var newEntity = Ecb.Instantiate(index, spawner.groupToSpawn);
            Ecb.SetComponent(index, newEntity, transform);
            spawner.SpawnTimer = spawner.SpawnInterval;
        }
    }
}

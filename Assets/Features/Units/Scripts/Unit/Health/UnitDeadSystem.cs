using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitPreUpdateSystemGroup))]
public partial struct UnitDeadSystem : ISystem
{
    private EntityQuery _query;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitPreUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
        _query = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<UnitDeadCD>()
            .Build(ref state);
        state.RequireForUpdate(_query);
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<UnitPreUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        var job = new UnitDeadJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Ecb = commandBuffer
        };
        state.Dependency = job.Schedule(state.Dependency);
    }
    
    [BurstCompile]
    private partial struct UnitDeadJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter Ecb;        
        public void Execute([ChunkIndexInQuery] int index, Entity entity, ref UnitDeadCD unitDead)
        {
            unitDead.deathDuration -= DeltaTime;
            if (unitDead.deathDuration <= 0f)
            {
                Ecb.DestroyEntity(index, entity);
            }
        }
    }
}

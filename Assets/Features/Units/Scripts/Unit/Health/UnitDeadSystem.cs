using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial struct UnitDeadSystem : ISystem
{
    private EntityQuery _query;

    public void OnCreate(ref SystemState state)
    {
        _query = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<UnitDeadCD>()
            .Build(ref state);
        state.RequireForUpdate(_query);
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var job = new UnitDeadJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Ecb = ecb.AsParallelWriter()
        };
        state.Dependency = job.Schedule(state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
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

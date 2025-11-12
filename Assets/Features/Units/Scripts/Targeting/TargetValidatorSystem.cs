using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct TargetValidatorSystem : ISystem
{
    private EntityQuery _query;
    private ComponentLookup<UnitAliveState> _UnitAliveStateLookup;
    public void OnCreate(ref SystemState state)
    {
        _query = state.GetEntityQuery(typeof(UnitTargetCD));
        state.RequireForUpdate(_query);
        _UnitAliveStateLookup = state.GetComponentLookup<UnitAliveState>(true);
    }

    public void OnUpdate(ref SystemState state)
    {
        _UnitAliveStateLookup.Update(ref state);
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var job = new TargetValidatorJob
        {
            Ecb = ecb.AsParallelWriter(),
            AliveStateLookUp = _UnitAliveStateLookup
        };
        state.Dependency = job.Schedule(state.Dependency);
        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);  
        ecb.Dispose();
    }
    
    [BurstCompile]
    private partial struct TargetValidatorJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<UnitAliveState> AliveStateLookUp;
        public EntityCommandBuffer.ParallelWriter Ecb;
        
        public void Execute([ChunkIndexInQuery] int index, Entity entity, in UnitTargetCD targetComponent)
        {
            if (!AliveStateLookUp.IsComponentEnabled(targetComponent.targetEntity))
            {
                Ecb.RemoveComponent<UnitTargetCD>(index, entity);
            }
        }
    }
}

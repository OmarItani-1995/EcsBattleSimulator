using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitPreUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct TargetValidatorSystem : ISystem
{
    private EntityQuery _query;
    private ComponentLookup<UnitAliveState> _UnitAliveStateLookup;
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<UnitTargetCD>()
            .Build();
        state.RequireForUpdate(_query);
        _UnitAliveStateLookup = state.GetComponentLookup<UnitAliveState>(true);
        state.RequireForUpdate<UnitPreUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        _UnitAliveStateLookup.Update(ref state);
        var ecb = SystemAPI.GetSingleton<UnitPreUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        var job = new TargetValidatorJob
        {
            Ecb = commandBuffer,
            AliveStateLookUp = _UnitAliveStateLookup
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
    
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct TargetValidatorJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<UnitAliveState> AliveStateLookUp;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int index, Entity entity, in UnitTargetCD targetComponent)
        {
            if (!AliveStateLookUp.IsComponentEnabled(targetComponent.targetEntity))
            {
                Ecb.RemoveComponent<UnitTargetCD>(index, entity);
            }
        }
    }
}

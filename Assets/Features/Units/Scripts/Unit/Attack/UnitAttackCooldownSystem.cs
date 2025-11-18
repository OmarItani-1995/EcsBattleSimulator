using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitPreUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct UnitAttackCooldownSystem : ISystem
{
    private EntityQuery _query;
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<UnitAttackCooldownCD>()
            .WithAll<UnitAttackCooldownState>()
            .WithAll<AnimatorComponentData>()
            .Build();
        state.RequireForUpdate(_query);
        state.RequireForUpdate<UnitPreUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var ecbSingleton = SystemAPI.GetSingleton<UnitPreUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var job = new UnitAttackCooldownJob
        {
            DeltaTime = deltaTime,
            Ecb = ecb.AsParallelWriter(),
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
    
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct UnitAttackCooldownJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int index, Entity entity, ref UnitAttackCooldownCD cooldown, ref AnimatorComponentData animator)
        {
            cooldown.Elapsed += DeltaTime;
            if (!(cooldown.Elapsed >= cooldown.Duration)) return;
            cooldown.Elapsed = 0f;
            Ecb.SetComponentEnabled<UnitAttackCooldownState>(index, entity, false);

            animator.currentTick = 0;
        }
    }
}

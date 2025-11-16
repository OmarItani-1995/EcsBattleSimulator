using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitLateUpdateSystemGroup), OrderLast = true)]
[UpdateBefore(typeof(UnitLateUpdateEndSimulationEntityCommandBufferSystem))]
[StructLayout(LayoutKind.Auto)]
public partial struct UnitHealthSystem : ISystem
{
    private const float DeathDuration = 10f;
    private EntityQuery _query;
    private ComponentLookup<AnimatorComponentData> _animatorLookup;
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<UnitHealthCD>()
            .WithAll<UnitHitsTaken>()
            .WithAll<UnitAnimatorCD>()
            .WithAll<UnitAliveState>()
            .Build();
        state.RequireForUpdate(_query);
        _animatorLookup = state.GetComponentLookup<AnimatorComponentData>(true);
        state.RequireForUpdate<UnitLateUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        _animatorLookup.Update(ref state);
        var ecbSingleton = SystemAPI.GetSingleton<UnitLateUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var job = new UnitHealthJob
        {
            Ecb = ecb.AsParallelWriter(),
            AnimatorLookup = _animatorLookup,
            deathDuration = DeathDuration
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
    
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct UnitHealthJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<AnimatorComponentData> AnimatorLookup;
        [ReadOnly] public float deathDuration;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int index, Entity entity, ref UnitHealthCD health, 
            DynamicBuffer<UnitHitsTaken> hitsTaken, ref UnitAnimatorCD animatorHolder)
        {
            if (hitsTaken.Length == 0) return;
            
            for (var i = 0; i < hitsTaken.Length; i++)
            {
                health.CurrentHealth -= hitsTaken[i].HitAmount;
            }
            hitsTaken.Clear();

            if (health.CurrentHealth > 0) return;
            Ecb.SetComponentEnabled<UnitAliveState>(index, entity, false);
            Ecb.AddComponent(index, entity, new UnitDeadCD()
            {
                deathDuration = deathDuration
            });
            
            if (!AnimatorLookup.HasComponent(animatorHolder.AnimatorEntity)) return;
            var animator = AnimatorLookup[animatorHolder.AnimatorEntity];
            animator.currentClip = AnimationClipName.Charging_Die;
            animator.currentTick = 0;
            animator.loop = false;
            Ecb.SetComponent(index, animatorHolder.AnimatorEntity, animator);
        }
    }
}

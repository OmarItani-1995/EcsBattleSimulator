using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
public partial struct UnitHealthSystem : ISystem
{
    private const float deathDuration = 5f;
    private EntityQuery _query;
    private ComponentLookup<AnimatorComponentData> _animatorLookup;
    public void OnCreate(ref SystemState state)
    {
        _query = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<UnitHealthCD>()
            .WithAll<UnitHitsTaken>()
            .WithAll<UnitAnimatorCD>()
            .WithAll<UnitAliveState>()
            .Build(ref state);
        state.RequireForUpdate(_query);
        _animatorLookup = state.GetComponentLookup<AnimatorComponentData>(true);
    }

    public void OnUpdate(ref SystemState state)
    {
        _animatorLookup.Update(ref state);
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        var job = new UnitHealthJob
        {
            Ecb = ecb.AsParallelWriter(),
            AnimatorLookup = _animatorLookup,
            deathDuration = deathDuration
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    [BurstCompile]
    private partial struct UnitHealthJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<AnimatorComponentData> AnimatorLookup;
        [ReadOnly] public float deathDuration;
        public EntityCommandBuffer.ParallelWriter Ecb;
        
        public void Execute([ChunkIndexInQuery] int index, Entity entity, ref UnitHealthCD health, 
            DynamicBuffer<UnitHitsTaken> hitsTaken, ref UnitAnimatorCD animatorHolder)
        {
            if (hitsTaken.Length == 0) return;
            
            for (int i = 0; i < hitsTaken.Length; i++)
            {
                health.CurrentHealth -= hitsTaken[i].HitAmount;
            }
            hitsTaken.Clear();

            if (health.CurrentHealth <= 0)
            {
                Ecb.SetComponentEnabled<UnitAliveState>(index, entity, false);
                Ecb.AddComponent(index, entity, new UnitDeadCD()
                {
                    deathDuration = deathDuration
                });
                if (AnimatorLookup.HasComponent(animatorHolder.animatorEntity))
                {
                    var animator = AnimatorLookup[animatorHolder.animatorEntity];
                    animator.currentClip = AnimationClipName.Charging_Die;
                    animator.currentTick = 0;
                    animator.loop = false;
                    Ecb.SetComponent(index, animatorHolder.animatorEntity, animator);
                }
            }
        }
    }
}

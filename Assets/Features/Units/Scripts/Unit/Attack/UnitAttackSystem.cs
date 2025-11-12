using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
public partial struct UnitAttackSystem : ISystem
{
    private EntityQuery _query;
    private ComponentLookup<LocalTransform> _transformLookup;
    private ComponentLookup<UnitHealthCD> _healthLookup;
    private BufferLookup<UnitHitsTaken> _hitsTakenLookup;
    private ComponentLookup<AnimatorComponentData> _animatorLookup;
    public void OnCreate(ref SystemState state)
    {
        _query = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<UnitAttackCD>()
            .WithAll<UnitAliveState>()
            .WithAll<UnitAnimatorCD>()
            .WithAll<UnitTargetCD>()
            .Build(ref state);
        state.RequireForUpdate(_query);
        _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        _healthLookup = state.GetComponentLookup<UnitHealthCD>(false);
        _hitsTakenLookup = state.GetBufferLookup<UnitHitsTaken>(false);
        _animatorLookup = state.GetComponentLookup<AnimatorComponentData>(true);
    }

    public void OnUpdate(ref SystemState state)
    {
        _transformLookup.Update(ref state);
        _healthLookup.Update(ref state);
        _hitsTakenLookup.Update(ref state);
        _animatorLookup.Update(ref state);
        
        float deltaTime = SystemAPI.Time.DeltaTime;
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        var job = new UnitAttackJob
        {
            DeltaTime = deltaTime,
            Ecb = ecb.AsParallelWriter(),
            TransformLookup = _transformLookup,
            HealthLookup = _healthLookup,
            AnimatorLookup = _animatorLookup
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    private partial struct UnitAttackJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<UnitHealthCD> HealthLookup;
        [ReadOnly] public ComponentLookup<AnimatorComponentData> AnimatorLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute([ChunkIndexInQuery] int index, Entity entity, ref UnitAttackCD attack,
            in UnitTargetCD target, in UnitAnimatorCD animatorHolder)
        {
            attack.totalTime -= DeltaTime;
            if (!attack.didAttack && attack.totalTime <= attack.attackTime)
            {
                attack.didAttack = true;
                var transform = TransformLookup[entity];
                var targetTransform = TransformLookup[target.targetEntity];
                float distance = math.distance(transform.Position, targetTransform.Position);
                if (distance <= 2f)
                {
                    Ecb.AppendToBuffer<UnitHitsTaken>(index, target.targetEntity, new UnitHitsTaken
                    {
                        HitAmount = attack.attackDamage
                    });
                }
            }

            if (!(attack.totalTime <= 0f)) return;
            Ecb.RemoveComponent<UnitAttackCD>(index, entity);
            Ecb.RemoveComponent<UnitTargetCD>(index, entity);
            if (AnimatorLookup.HasComponent(animatorHolder.animatorEntity))
            {
                var animator = AnimatorLookup[animatorHolder.animatorEntity];
                animator.currentClip = AnimationClipName.Charing_Run;
                animator.currentTick = 0;
                animator.loop = true;
                Ecb.SetComponent(index, animatorHolder.animatorEntity, animator);
            }
            
        }
    }
}
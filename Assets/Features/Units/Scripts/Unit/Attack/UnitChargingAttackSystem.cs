using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitLateUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct UnitChargingAttackSystem : ISystem
{
    private EntityQuery _query;
    private ComponentLookup<LocalTransform> _transformLookup;
    private ComponentLookup<AnimatorComponentData> _animatorLookup;
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<UnitAttackCD>()
            .WithAll<UnitAliveState>()
            .WithAll<UnitAnimatorCD>()
            .WithAll<UnitTargetCD>()
            .Build();
        state.RequireForUpdate(_query);
        _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        _animatorLookup = state.GetComponentLookup<AnimatorComponentData>(true);
        state.RequireForUpdate<UnitLateUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        _transformLookup.Update(ref state);
        _animatorLookup.Update(ref state);
        
        var deltaTime = SystemAPI.Time.DeltaTime;
        var ecbSingleton = SystemAPI.GetSingleton<UnitLateUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var job = new UnitAttackJob
        {
            DeltaTime = deltaTime,
            Ecb = ecb.AsParallelWriter(),
            TransformLookup = _transformLookup,
            AnimatorLookup = _animatorLookup
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct UnitAttackJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<AnimatorComponentData> AnimatorLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int index, Entity entity, ref UnitAttackCD attack,
            in UnitTargetCD target, in UnitAnimatorCD animatorHolder)
        {
            attack.TotalTime -= DeltaTime;
            if (!attack.DidAttack && attack.TotalTime <= attack.AttackTime)
            {
                attack.DidAttack = true;
                var transform = TransformLookup[entity];
                var targetTransform = TransformLookup[target.targetEntity];
                float distance = math.distance(transform.Position, targetTransform.Position);
                if (distance <= 2f)
                {
                    Ecb.AppendToBuffer<UnitHitsTaken>(index, target.targetEntity, new UnitHitsTaken
                    {
                        HitAmount = attack.AttackDamage
                    });
                }
            }

            if (!(attack.TotalTime <= 0f)) return;
            Ecb.RemoveComponent<UnitAttackCD>(index, entity);
            Ecb.RemoveComponent<UnitTargetCD>(index, entity);
            
            if (!AnimatorLookup.HasComponent(animatorHolder.animatorEntity)) return;
            var animator = AnimatorLookup[animatorHolder.animatorEntity];
            animator.currentClip = AnimationClipName.Charing_Run;
            animator.currentTick = 0;
            animator.loop = true;
            Ecb.SetComponent(index, animatorHolder.animatorEntity, animator);
        }
    }
}
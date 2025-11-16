using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct UnitAttackCheckSystem : ISystem
{
    private EntityQuery _query;
    private ComponentLookup<LocalTransform> _transformLookup;
    private ComponentLookup<AnimatorComponentData> _animatorLookup;
    private ComponentLookup<UnitAliveState> _aliveStateLookup;
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<UnitTargetCD>()
            .WithAll<UnitAttackInfoSD>()
            .WithAll<UnitAnimatorCD>()
            .WithAll<LocalToWorld>()
            .WithAll<UnitAliveState>()
            .WithNone<UnitAttackCD>()
            .Build();
        state.RequireForUpdate(_query);
        _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        _animatorLookup = state.GetComponentLookup<AnimatorComponentData>(false);
        _aliveStateLookup = state.GetComponentLookup<UnitAliveState>(true);
        state.RequireForUpdate<UnitUpdateEndSImulationEntityCommandBufferSystem.Singleton>();
    }
    
    public void OnUpdate(ref SystemState state)
    {
        _transformLookup.Update(ref state);
        _animatorLookup.Update(ref state);
        _aliveStateLookup.Update(ref state);
        
        var ecbSingleton = SystemAPI.GetSingleton<UnitUpdateEndSImulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        
        var job = new AttachCheckJob
        {
            Ecb = ecb.AsParallelWriter(),
            TransformLookup = _transformLookup,
            AnimatorLookUp = _animatorLookup,
            AliveStateLookup = _aliveStateLookup
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
    
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct AttachCheckJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<AnimatorComponentData> AnimatorLookUp;
        [ReadOnly] public ComponentLookup<UnitAliveState> AliveStateLookup;

        private void Execute([ChunkIndexInQuery] int index, Entity entity, in UnitTargetCD target, in UnitAttackInfoSD attackInfo,
            in LocalToWorld transform, in UnitAnimatorCD animatorHolder)
        {
            float distance = math.distance(transform.Position, TransformLookup[target.targetEntity].Position);
            if (distance <= attackInfo.attackRange)
            {
                var animator = AnimatorLookUp[animatorHolder.AnimatorEntity];
                if (!AliveStateLookup.IsComponentEnabled(target.targetEntity))
                {
                    Ecb.RemoveComponent<UnitAttackCD>(index, entity);
                    Ecb.RemoveComponent<UnitTargetCD>(index, entity);
                    animator.currentClip = AnimationClipName.Charing_Run;
                    animator.currentTick = 0;
                    animator.loop = true;
                    Ecb.SetComponent(index, animatorHolder.AnimatorEntity, animator);
                    return;
                }
                animator.currentClip = attackInfo.attackAnimation;
                animator.currentTick = 0;
                animator.loop = false;
                
                Ecb.AddComponent(index, animatorHolder.AnimatorEntity, animator);
                
                Ecb.AddComponent(index, entity, new UnitAttackCD()
                {
                    AttackTime = attackInfo.attackTime,
                    TotalTime = attackInfo.attackTotalTime,
                    DidAttack = false,
                    AttackDamage = attackInfo.attackDamage
                });   
            }
        }
    }
}

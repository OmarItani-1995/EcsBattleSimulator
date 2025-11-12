using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
public partial struct UnitAnimatorInitializerSystem : ISystem
{
    private EntityQuery _query;
    private BufferLookup<Child> _childLookup;
    private ComponentLookup<AnimatorComponentData> _animatorLookup;
    
    public void OnCreate(ref SystemState state)
    {
        _query = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<UnitTag>()
            .WithNone<UnitAnimatorCD>()
            .Build(ref state);
        state.RequireForUpdate(_query);
        
        _childLookup = state.GetBufferLookup<Child>(true);
        _animatorLookup = state.GetComponentLookup<AnimatorComponentData>(true);
    }
    public void OnUpdate(ref SystemState state)
    {
        _childLookup.Update(ref state);
        _animatorLookup.Update(ref state);
        
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var job = new UnitAnimatorInitializerJob
        {
            Ecb = ecb.AsParallelWriter(),
            ChildLookup = _childLookup,
            AnimatorLookup = _animatorLookup
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    [BurstCompile]
    private partial struct UnitAnimatorInitializerJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public BufferLookup<Child> ChildLookup;
        [ReadOnly] public ComponentLookup<AnimatorComponentData> AnimatorLookup;
        
        public void Execute([ChunkIndexInQuery] int index, Entity entity)
        {
            if (!ChildLookup.HasBuffer(entity)) return;
            var children = ChildLookup[entity];
            for (int i = 0; i < children.Length; i++)
            {
                if (CheckRecursive(index, entity, children[i]))
                {
                    break;
                }
            }          
        }

        private bool CheckRecursive(int index, Entity entity, Child child)
        {
            Check(index, entity, child);
            if (ChildLookup.HasBuffer(child.Value))
            {
                var children = ChildLookup[child.Value];
                for (int i = 0; i < children.Length; i++)
                {
                    if (CheckRecursive(index, entity, children[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool Check(int index, Entity entity, Child child)
        {
            if (AnimatorLookup.HasComponent(child.Value))
            {
                Ecb.AddComponent(index, entity, new UnitAnimatorCD
                {
                    animatorEntity = child.Value,
                });
                return true;
            }

            return false;
        }
    }
}

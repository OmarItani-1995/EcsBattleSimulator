using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitPreUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct UnitAnimatorInitializerSystem : ISystem
{
    private EntityQuery _query;
    private BufferLookup<Child> _childLookup;
    private ComponentLookup<AnimatorComponentData> _animatorLookup;
    
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<UnitTag>()
            .WithNone<UnitAnimatorCD>()
            .Build();
        state.RequireForUpdate(_query);
        
        _childLookup = state.GetBufferLookup<Child>(true);
        _animatorLookup = state.GetComponentLookup<AnimatorComponentData>(true);
        state.RequireForUpdate<UnitPreUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
    }
    public void OnUpdate(ref SystemState state)
    {
        _childLookup.Update(ref state);
        _animatorLookup.Update(ref state);
        
        var ecbSingleton = SystemAPI.GetSingleton<UnitPreUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        
        var job = new UnitAnimatorInitializerJob
        {
            Ecb = ecb.AsParallelWriter(),
            ChildLookup = _childLookup,
            AnimatorLookup = _animatorLookup
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
    
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct UnitAnimatorInitializerJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<Child> ChildLookup;
        [ReadOnly] public ComponentLookup<AnimatorComponentData> AnimatorLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int index, Entity entity)
        {
            if (!ChildLookup.HasBuffer(entity)) return;
            var children = ChildLookup[entity];
            foreach (var child in children)
            {
                if (CheckRecursive(index, entity, child))
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

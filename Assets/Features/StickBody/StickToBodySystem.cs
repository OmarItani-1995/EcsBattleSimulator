using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitPreUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct StickToBodySystem : ISystem
{
    private EntityQuery _query;
    private EntityStorageInfoLookup _entityStorageInfoLookup;
    private ComponentLookup<LocalTransform> _transformLookup;
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<StickToBody>()
            .Build();
        state.RequireForUpdate(_query);
        _entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
        _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        state.RequireForUpdate<UnitPreUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        _transformLookup.Update(ref state);
        _entityStorageInfoLookup.Update(ref state);
        var ecb = SystemAPI.GetSingleton<UnitPreUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        var job = new StickToBodyJob
        {
            TransformLookup = _transformLookup,
            EntityStorageInfoLookup = _entityStorageInfoLookup,
            Ecb = commandBuffer
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct StickToBodyJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public EntityStorageInfoLookup EntityStorageInfoLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int index, Entity entity, in StickToBody stickToBody)
        {
            if (!EntityStorageInfoLookup.Exists(stickToBody.Target))
            {
                Ecb.DestroyEntity(index, entity);
                return;
            }

            var targetTransform = TransformLookup[stickToBody.Target];
            var transform = TransformLookup[entity];
            var newPosition = targetTransform.Position + stickToBody.offsetPosition;
            transform.Position = newPosition;
            Ecb.SetComponent(index, entity, transform);
        }
    }
}

using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(WorldSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct ArrowCollisionSystem : ISystem
{
    private EntityQuery _query;
    private ComponentLookup<LocalTransform> _localTransformLookup;
    private EntityStorageInfoLookup _entityStorageInfoLookup;
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<ArrowTag>()
            .WithAll<ArrowState>()
            .WithAll<CollisionBuffer>()
            .WithAll<PhysicsCollider>()
            .WithAll<PhysicsVelocity>()
            .WithAll<PhysicsMass>()
            .Build();
        state.RequireForUpdate(_query);
        _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
        _entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
        
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        _localTransformLookup.Update(ref state);
        _entityStorageInfoLookup.Update(ref state);
        
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        var job = new ArrowCollisionJob
        {
            Ecb = commandBuffer,
            TransformLookup = _localTransformLookup,
            EntityStorageInfoLookup = _entityStorageInfoLookup,
            PhysicsWorldIndex = new PhysicsWorldIndex(1)
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
    
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct ArrowCollisionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public EntityStorageInfoLookup EntityStorageInfoLookup;
        [ReadOnly] public PhysicsWorldIndex PhysicsWorldIndex;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int index, Entity entity, ref DynamicBuffer<CollisionBuffer> buffer, ref PhysicsVelocity velocity, ref PhysicsMass mass, ref PhysicsCollider collider)
        {
            if (buffer.Length == 0)
            {
                return;
            }

            var stickToBody = new StickToBody()
            {
                Target = buffer[0].Entity,
                offsetPosition = TransformLookup[entity].Position - TransformLookup[buffer[0].Entity].Position
            };

            if (EntityStorageInfoLookup.Exists(entity))
            {
                Ecb.AddComponent(index, entity, stickToBody);
                Ecb.SetComponentEnabled<ArrowState>(index, entity, false);
                Ecb.SetSharedComponent(index, entity, PhysicsWorldIndex);
                // Ecb.RemoveComponent<PhysicsVelocity>(index,entity);
                // Ecb.RemoveComponent<PhysicsCollider>(index, entity);
            }
        }
    }
}

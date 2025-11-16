using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct CollisionSystem : ISystem
{
    private ComponentLookup<UnitTag> unitLookUp;
    private BufferLookup<CollisionBuffer> collisionBufferType;
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        unitLookUp = state.GetComponentLookup<UnitTag>(true);
        collisionBufferType = state.GetBufferLookup<CollisionBuffer>();
    }

    public void OnUpdate(ref SystemState state)
    {
        collisionBufferType.Update(ref state);
        unitLookUp.Update(ref state);
        
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();
        
        var job = new CollisionJob
        {
            PhysicsWorld = physicsWorldSingleton.PhysicsWorld,
            CollisionBufferType = collisionBufferType,
            UnitLookUp = unitLookUp
        };
        state.Dependency = job.Schedule(simulation, state.Dependency);
    }

    [BurstCompile]
    private partial struct CollisionJob : ICollisionEventsJob
    {
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        [ReadOnly] public ComponentLookup<UnitTag> UnitLookUp;
        public BufferLookup<CollisionBuffer> CollisionBufferType;
        
        public void Execute(CollisionEvent collisionEvent)
        {
            var entityA = collisionEvent.EntityA;
            var entityB = collisionEvent.EntityB;
            
            if (UnitLookUp.HasComponent(entityA) && UnitLookUp.HasComponent(entityB))
            {
                return;
            }
            
            float3 collisionPoint = collisionEvent.CalculateDetails(ref PhysicsWorld).AverageContactPointPosition;
            AddCollision(entityA, entityB, collisionPoint);
            AddCollision(entityB, entityA, collisionPoint);
        }

        private void AddCollision(Entity entity, Entity otherEntity, float3 contactPoint)
        {
            if (!CollisionBufferType.HasBuffer(entity)) return;
            var buffer = CollisionBufferType[entity];
            buffer.Add(new CollisionBuffer
            {
                Entity = otherEntity,
                ContactPoint = contactPoint
            });
        }
    }
}


using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(BeforeFixedUpdateGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct FireRainSystem : ISystem
{
    private EntityQuery _query;
    private BufferLookup<UnitHitsTaken> _hitsTakenLookup;
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<FireRainTag>()
            .WithAll<LocalTransform>()
            .Build();
        state.RequireForUpdate(_query);
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        _hitsTakenLookup = state.GetBufferLookup<UnitHitsTaken>();

        var entity = state.EntityManager.CreateEntity(typeof(FireRainData));
        state.EntityManager.SetComponentData(entity, new FireRainData
        {
            Radius = 4f,
            ForceMagnitude = 150f,
            
        });
        state.RequireForUpdate<FireRainData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        _hitsTakenLookup.Update(ref state);
        
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        
        var data = SystemAPI.GetSingleton<FireRainData>();
        
        var filter = new CollisionFilter
        {
            BelongsTo = 1 << 9,
            CollidesWith = 1 << 7,
            GroupIndex = 0
        };
        var job = new FireRainJob
        {
            Ecb = ecb.AsParallelWriter(),
            PhysicsWorld = physicsWorld,
            CollisionFilter = filter,
            HitsTakenLookup = _hitsTakenLookup,
            FireRainData = data
        };
        
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
    
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct FireRainJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        [ReadOnly] public CollisionFilter CollisionFilter;
        [ReadOnly] public BufferLookup<UnitHitsTaken> HitsTakenLookup;
        [ReadOnly] public FireRainData FireRainData;
        private void Execute([ChunkIndexInQuery] int index, Entity entity, in LocalTransform transform)
        {
            var hitResults = new NativeList<DistanceHit>(Allocator.TempJob);
            
            if (PhysicsWorld.OverlapSphere(transform.Position, 4, ref hitResults, CollisionFilter) )
            {
                for (int i = 0; i < hitResults.Length; i++)
                {
                    DistanceHit hit = hitResults[i];
                    Entity hitEntity = PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                    if (HitsTakenLookup.HasBuffer(hitEntity))
                    {
                        Ecb.AppendToBuffer(index, hitEntity, new UnitHitsTaken()
                        {
                            HitAmount = 100
                        });
                        Ecb.AppendToBuffer(index, hitEntity, new ExplosionForceCD()
                        {
                            ForcePoint = transform.Position,
                            ForceMagnitude = FireRainData.ForceMagnitude,
                            Radius = FireRainData.Radius
                        });
                    }
                }
            }
            Ecb.DestroyEntity(index, entity);
            hitResults.Dispose();
        }
    }
}

public struct FireRainData : IComponentData
{
    public float Radius;
    public float ForceMagnitude;
}
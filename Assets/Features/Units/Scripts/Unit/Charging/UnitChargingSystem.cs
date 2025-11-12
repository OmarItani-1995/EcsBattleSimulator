using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[RequireMatchingQueriesForUpdate]
[BurstCompile]
[UpdateInGroup(typeof(UnitUpdateSystemGroup))]
public partial struct UnitChargingSystem : ISystem
{
    private const float StoppingMinZ = -10;
    private const float StoppingMaxZ = 10;
    
    private EntityQuery query;
    public void OnCreate(ref SystemState state)
    {
        query = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<UnitChargingState>()
            .WithAll<UnitChargingCD>()
            .WithAll<LocalTransform>()
            .Build(ref state);
        state.RequireForUpdate(query);
        state.RequireForUpdate<UnitUpdateEndSImulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<UnitUpdateEndSImulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var job = new UnitChargingJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Min = StoppingMinZ,
            Max = StoppingMaxZ,
            buffer = ecb.AsParallelWriter(),
            MaxDistance = 50f,
        };
        
        state.Dependency = job.ScheduleParallel(query, state.Dependency);
    }

    [BurstCompile]
    private partial struct UnitChargingJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float Min;
        [ReadOnly] public float Max;
        [ReadOnly] public float MaxDistance;
        
        public EntityCommandBuffer.ParallelWriter buffer;
        
        public void Execute([EntityIndexInQuery] int entityIndex, Entity entity, ref LocalTransform transform, in UnitChargingCD unitCharge)
        {
            if (unitCharge.MinSpeed == 0)
            {
                buffer.SetComponentEnabled<UnitChargingState>(entityIndex, entity, false);
                return;
            }

            float signedDeltaZ = transform.Position.z;
            float absDist = Mathf.Abs(signedDeltaZ);
            float t = 1- math.clamp(absDist / MaxDistance, 0, 1);
            float speed = math.lerp(unitCharge.MinSpeed, unitCharge.MaxSpeed, math.clamp(t * t, 0 , 1));
            transform.Position += unitCharge.Direction * speed * DeltaTime;
            if (transform.Position.z >= Min && transform.Position.z <= Max)
            {
                buffer.SetComponentEnabled<UnitChargingState>(entityIndex, entity, false);
            }
        }
    }
}

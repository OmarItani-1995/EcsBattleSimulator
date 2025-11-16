using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[RequireMatchingQueriesForUpdate]
[BurstCompile]
[UpdateInGroup(typeof(UnitUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct UnitChargingSystem : ISystem
{
    private const float StoppingMinZ = -10;
    private const float StoppingMaxZ = 10;
    
    private EntityQuery _query;
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<UnitChargingCD>()
            .WithAll<UnitAliveState>()
            .WithAll<LocalTransform>()
            .Build();
        state.RequireForUpdate(_query);
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
        
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct UnitChargingJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float Min;
        [ReadOnly] public float Max;
        [ReadOnly] public float MaxDistance;
        
        public EntityCommandBuffer.ParallelWriter buffer;

        private void Execute([EntityIndexInQuery] int entityIndex, Entity entity, ref LocalTransform transform, in UnitChargingCD unitCharge)
        {
            if (unitCharge.MinSpeed == 0)
            {
                buffer.SetComponentEnabled<UnitChargingCD>(entityIndex, entity, false);
                return;
            }

            var signedDeltaZ = transform.Position.z;
            var absDist = Mathf.Abs(signedDeltaZ);
            var t = 1- math.clamp(absDist / MaxDistance, 0, 1);
            var speed = math.lerp(unitCharge.MinSpeed, unitCharge.MaxSpeed, math.clamp(t * t, 0 , 1));
            transform.Position += unitCharge.Direction * speed * DeltaTime;
            if (transform.Position.z >= Min && transform.Position.z <= Max)
            {
                buffer.SetComponentEnabled<UnitChargingCD>(entityIndex, entity, false);
            }
        }
    }
}

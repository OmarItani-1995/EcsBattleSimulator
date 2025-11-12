using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct UnitDebuggerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<UnitTag>().WithEntityAccess())
        {
            if (transform.ValueRO.Position.x > -90f && transform.ValueRO.Position.x < 90f)
                continue;

            var unitAlive = SystemAPI.IsComponentEnabled<UnitAliveState>(entity);
            var unitCharging = SystemAPI.IsComponentEnabled<UnitChargingState>(entity);
            if (SystemAPI.HasComponent<UnitTargetCD>(entity))
            {
                var unitTarget = SystemAPI.GetComponent<UnitTargetCD>(entity);
            }

            var unitHealth = SystemAPI.GetComponent<UnitHealthCD>(entity);
        }
    }
}


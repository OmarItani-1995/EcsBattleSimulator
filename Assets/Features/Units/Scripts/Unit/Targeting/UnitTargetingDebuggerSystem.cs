using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
public partial struct UnitTargetingDebuggerSystem : ISystem
{
    private EntityQuery _active;
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = false;
        _active = SystemAPI.QueryBuilder()
            .WithAll<UnitTargetCD>()
            .Build();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (targetingDebug, transform) in SystemAPI.Query<RefRO<UnitTargetCD>, RefRO<LocalTransform>>().WithAll<PlayerTag>())
        {
            if (targetingDebug.ValueRO.targetEntity != Entity.Null)
            {
                var targetTransform = state.EntityManager.GetComponentData<LocalTransform>(targetingDebug.ValueRO.targetEntity);
                Debug.DrawLine(transform.ValueRO.Position, targetTransform.Position, Color.blue);
            }
        }
        
        foreach (var (targetingDebug, transform) in SystemAPI.Query<RefRO<UnitTargetCD>, RefRO<LocalTransform>>().WithAll<EnemyTag>())
        {
            if (targetingDebug.ValueRO.targetEntity != Entity.Null)
            {
                var targetTransform = state.EntityManager.GetComponentData<LocalTransform>(targetingDebug.ValueRO.targetEntity);
                Debug.DrawLine(transform.ValueRO.Position, targetTransform.Position, Color.red);
            }
        }
    }
}

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
public partial struct UnitAngularSystem : ISystem
{
    private EntityQuery _query;

    public void OnCreate(ref SystemState state)
    {
        _query = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<UnitTag>()
            .WithAll<PhysicsMass>()
            .WithAll<UnitAliveState>()
            .Build(ref state);
        
        state.RequireForUpdate(_query);
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var job = new UnitAngularJob { };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }

    [BurstCompile]
    private partial struct UnitAngularJob : IJobEntity
    {
        public void Execute(ref PhysicsMass mass)
        {
            var inv = mass.InverseInertia;
            inv.x = 0f;
            inv.z = 0f;
            mass.InverseInertia = inv;
        }
    }
}

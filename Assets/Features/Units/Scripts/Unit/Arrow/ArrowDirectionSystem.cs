using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(WorldSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct ArrowDirectionSystem : ISystem
{
    private EntityQuery _query;
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<ArrowTag>()
            .WithAll<LocalTransform>()
            .WithAll<PhysicsVelocity>()
            .Build();
        state.RequireForUpdate(_query);
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var job = new ArrowDirectionJob { };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
    
    [BurstCompile]
    private partial struct ArrowDirectionJob : IJobEntity
    {
        private void Execute(in PhysicsVelocity velocity, ref LocalTransform transform)
        {
            transform.Rotation = quaternion.LookRotationSafe(math.normalize(velocity.Linear), math.up());
        }
    }
}

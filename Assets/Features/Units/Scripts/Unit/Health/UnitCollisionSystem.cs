using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitPreUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct UnitCollisionSystem : ISystem
{
    private EntityQuery _query;
    
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<UnitAliveState>()
            .WithAll<CollisionBuffer>()
            .WithAll<UnitHitsTaken>()
            .Build();
        state.RequireForUpdate(_query);
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var job = new UnitCollisionJob();
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
    
    [BurstCompile]
    private partial struct UnitCollisionJob : IJobEntity
    {
        private static void Execute(ref DynamicBuffer<CollisionBuffer> collisionBuffer, ref DynamicBuffer<UnitHitsTaken> hitsTaken)
        {
            if (collisionBuffer.Length == 0) return;
            
            for (var i = 0; i < collisionBuffer.Length; i++)
            {
                hitsTaken.Add(new UnitHitsTaken { HitAmount = 100 });
            }
            collisionBuffer.Clear();
        }
    }
}

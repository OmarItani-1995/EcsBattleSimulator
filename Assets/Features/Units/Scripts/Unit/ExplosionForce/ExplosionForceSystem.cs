using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitLateUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct ExplosionForceSystem : ISystem
{
    private EntityQuery _query;
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<LocalTransform>()
            .WithAll<PhysicsVelocity>()
            .WithAll<PhysicsMass>()
            .WithAll<ExplosionForceCD>()
            .Build();

        state.RequireForUpdate(_query);
    }

    public void OnUpdate(ref SystemState state)
    {
        var job = new ExplosionForceJob();
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
    
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct ExplosionForceJob : IJobEntity
    {
        private void Execute([ChunkIndexInQuery] int index, Entity entity, in LocalTransform transform, ref DynamicBuffer<ExplosionForceCD> explosionBuffer, ref PhysicsVelocity velocity, in PhysicsMass mass)
        {
            for (int i = 0; i < explosionBuffer.Length; i++)
            {
                var position = transform.Position;
                var explosionPoint = explosionBuffer[i].ForcePoint;
                var forceMagnitude = explosionBuffer[i].ForceMagnitude;

                var direction = position - explosionPoint;
                var distance = math.length(direction);

                if (distance > 0f)
                    direction /= distance;
                else
                    direction = new float3(0, 1, 0); 

                const float upwardBoost = 3f;
                direction.y += upwardBoost;

                direction = math.normalize(direction);

                var falloff = math.saturate(1f - (distance / explosionBuffer[i].Radius));

                var impulse = direction * forceMagnitude * falloff;

                velocity.Linear += impulse * mass.InverseMass;
            }
            explosionBuffer.Clear();
        }
    }
}

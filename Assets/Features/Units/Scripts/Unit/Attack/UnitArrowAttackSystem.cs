using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitLateUpdateSystemGroup), OrderFirst = true)]
[StructLayout(LayoutKind.Auto)]
public partial struct UnitArrowAttackSystem : ISystem
{
    private EntityQuery _query;
    private Random random;
    public void OnCreate(ref SystemState state)
    {
        random = Random.CreateFromIndex(0);
        
        _query = SystemAPI.QueryBuilder()
            .WithAll<ArrowShootingCD>()
            .WithAll<UnitAttackCooldownCD>()
            .WithAll<LocalTransform>()
            .WithDisabled<UnitAttackCooldownState>()
            .Build();
        state.RequireForUpdate(_query);
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        
        float deltaTime = SystemAPI.Time.DeltaTime;
        var frame = (uint)math.floor(SystemAPI.Time.ElapsedTime);
        var job = new UnitArrowAttackJob
        {
            DeltaTime = deltaTime,
            Ecb = ecb.AsParallelWriter(),
            Frame = frame,
            Seed = random.NextUInt(),
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct UnitArrowAttackJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public uint Frame;
        [ReadOnly] public uint Seed;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int index, Entity entity, in LocalTransform transform, ref ArrowShootingCD shooting)
        {
            shooting.Elapsed += DeltaTime;
            if (shooting.Elapsed < shooting.AttackTime)
            {
                return;
            }
            
            shooting.Elapsed = 0f;
            var projectile = Ecb.Instantiate(index, shooting.Arrow);
            var projectileTransform = new LocalTransform()
            {
                Position = transform.Position + shooting.ShootingPosition,
                Rotation = shooting.ShootingRotation,
                Scale = 1f,
            };
            Ecb.SetComponent(index, projectile, projectileTransform);

            var h = math.hash(new uint3((uint)index, Frame, Seed));
            var extraForce = (int)(h % (uint)shooting.ForceRandomness);
            var direction = shooting.ShootingDirection;
            var force = new PhysicsVelocity()
            {
                Linear = direction * (shooting.Force + extraForce),
            };
            Ecb.SetComponent(index, projectile, force);
            Ecb.SetComponentEnabled<UnitAttackCooldownState>(index, entity, true);
        }
    }
}

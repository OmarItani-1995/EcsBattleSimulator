using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(TransformSystemGroup))]
public partial struct NavMeshAgentSystem : ISystem
{
    private EntityQuery query;
    private ComponentLookup<LocalTransform> _transformLookup;
    
    public void OnCreate(ref SystemState state)
    {
        query = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<NavMeshAgentCD>()
            .WithAll<UnitTargetCD>()
            .WithAll<LocalTransform>()
            .WithAll<UnitAliveState>()
            .WithNone<UnitAttackCD>()
            .Build(ref state);
        
        state.RequireForUpdate(query);    
        _transformLookup = state.GetComponentLookup<LocalTransform>(true);
    }

    public void OnUpdate(ref SystemState state)
    {
        _transformLookup.Update(ref state);
        
        float deltaTime = SystemAPI.Time.DeltaTime;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        var job = new NavMeshAgentJob
        {
            DeltaTime = deltaTime,
            TransformLookup = _transformLookup,
            Ecb = ecb.AsParallelWriter()
        };
        state.Dependency = job.ScheduleParallel(query, state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    private partial struct NavMeshAgentJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;
        public void Execute([ChunkIndexInQuery] int index, Entity entity, in NavMeshAgentCD agent, in UnitTargetCD target)
        {
            var targetUnitPosition = TransformLookup[target.targetEntity].Position;
            var transform = TransformLookup[entity];
            float3 direction = targetUnitPosition - transform.Position;
            float3 targetPosition = targetUnitPosition - math.normalize(direction) * 0.8f;
            direction.y = 0;
            transform.Rotation = quaternion.LookRotation(math.normalize(direction), math.up());
            if (math.distance(transform.Position, targetPosition) > 0.2f)
            {
                transform.Position += math.normalize(direction) * DeltaTime * agent.moveSpeed;
            }

            Ecb.AddComponent(index, entity, transform);
        }
    }
}


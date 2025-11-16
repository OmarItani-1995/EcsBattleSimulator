using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct NavMeshAgentSystem : ISystem
{
    private EntityQuery _query;
    private ComponentLookup<LocalTransform> _transformLookup;
    
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<NavMeshAgentCD>()
            .WithAll<UnitTargetCD>()
            .WithAll<LocalTransform>()
            .WithAll<UnitAliveState>()
            .WithDisabled<UnitAttackCD>()
            .Build();
        
        state.RequireForUpdate(_query);    
        _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        state.RequireForUpdate<UnitUpdateEndSImulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        _transformLookup.Update(ref state);
        
        var ecbSingleton = SystemAPI.GetSingleton<UnitUpdateEndSImulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        
        var deltaTime = SystemAPI.Time.DeltaTime;
        var job = new NavMeshAgentJob
        {
            DeltaTime = deltaTime,
            TransformLookup = _transformLookup,
            Ecb = ecb.AsParallelWriter()
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct NavMeshAgentJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int index, Entity entity, in NavMeshAgentCD agent, in UnitTargetCD target)
        {
            var targetUnitPosition = TransformLookup[target.TargetEntity].Position;
            var transform = TransformLookup[entity];
            var direction = targetUnitPosition - transform.Position;
            var targetPosition = targetUnitPosition - math.normalize(direction) * 0.8f;
            direction.y = 0;
            transform.Rotation = quaternion.LookRotation(math.normalize(direction), math.up());
            if (math.distance(transform.Position, targetPosition) > 0.2f)
            {
                transform.Position += math.normalize(direction) * DeltaTime * agent.moveSpeed;
            }
            
            Ecb.SetComponent(index, entity, transform);
        }
    }
}


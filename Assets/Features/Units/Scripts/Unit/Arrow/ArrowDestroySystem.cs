using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(UnitLateUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct ArrowDestroySystem : ISystem
{
    private EntityQuery _query;
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<ArrowDestroyCooldownComponentData>()
            .WithAll<ArrowTag>()
            .WithDisabled<ArrowState>()
            .Build();
        state.RequireForUpdate(_query);
        state.RequireForUpdate<UnitLateUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<UnitLateUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        
        float deltaTime = SystemAPI.Time.DeltaTime;
        var job = new ArrowDestroyJob
        {
            DeltaTime = deltaTime,
            Ecb = ecb.AsParallelWriter(),
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
    
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct ArrowDestroyJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int index, Entity entity, ref ArrowDestroyCooldownComponentData destroyCooldown)
        {
            destroyCooldown.Cooldown -= DeltaTime;
            if (destroyCooldown.Cooldown <= 0f)
            {
                Ecb.DestroyEntity(index, entity);
            }
        }
    }
}

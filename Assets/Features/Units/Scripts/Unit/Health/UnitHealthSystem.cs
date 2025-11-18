using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(UnitLateUpdateSystemGroup), OrderLast = true)]
[UpdateBefore(typeof(UnitLateUpdateEndSimulationEntityCommandBufferSystem))]
[StructLayout(LayoutKind.Auto)]
public partial struct UnitHealthSystem : ISystem
{
    private EntityQuery _query;
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<UnitHealthCD>()
            .WithAll<UnitHitsTaken>()
            .WithAll<AnimatorComponentData>()
            .WithAll<UnitAliveState>()
            .WithAll<PhysicsMass>()
            .Build();
        state.RequireForUpdate(_query);
        state.RequireForUpdate<UnitLateUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<UnitLateUpdateEndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var job = new UnitHealthJob
        {
            Ecb = ecb.AsParallelWriter(),
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
    
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct UnitHealthJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int index, Entity entity, ref UnitHealthCD health, 
            DynamicBuffer<UnitHitsTaken> hitsTaken, ref AnimatorComponentData animator, ref PhysicsMass mass)
        {
            if (hitsTaken.Length == 0) return;
            
            for (var i = 0; i < hitsTaken.Length; i++)
            {
                health.CurrentHealth -= hitsTaken[i].HitAmount;
            }
            hitsTaken.Clear();

            if (health.CurrentHealth > 0) return;
            Ecb.SetComponentEnabled<UnitAliveState>(index, entity, false);
            
            var inv = mass.InverseInertia;
            inv.x = 1f;
            inv.z = 1f;
            mass.InverseInertia = inv;
            
            animator.currentClip = AnimationClipName.Charging_Die;
            animator.currentTick = 0;
            animator.loop = false;
        }
    }
}

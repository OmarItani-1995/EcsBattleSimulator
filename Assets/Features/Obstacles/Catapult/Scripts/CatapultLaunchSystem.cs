using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[RequireMatchingQueriesForUpdate]
public partial struct CatapultLaunchSystem : ISystem
{
    private EntityQuery _query;
    private ComponentLookup<LocalTransform> _transformLookup;
    public void OnCreate(ref SystemState state)
    {
        _query = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<CatapultCD>()
            .Build(ref state);

        state.RequireForUpdate(_query);
        _transformLookup = state.GetComponentLookup<LocalTransform>(true);
    }

    public void OnUpdate(ref SystemState state)
    {
        _transformLookup.Update(ref state);
        float deltaTime = SystemAPI.Time.DeltaTime;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var job = new CatapultLaunchJob
        {
            DeltaTime = deltaTime,
            Ecb = ecb.AsParallelWriter(),
            TransformLookup = _transformLookup
        };
        state.Dependency = job.Schedule(_query, state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    [BurstCompile]
    private partial struct CatapultLaunchJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;
        
        public void Execute([ChunkIndexInQuery] int index, Entity entity, ref CatapultCD catapult)
        {
            if (catapult.CurrentReload > 0f)
            {
                catapult.CurrentReload -= DeltaTime;
                return;
            }
            
            Entity projectile = Ecb.Instantiate(index, catapult.ProjectileEntity);
            
            LocalTransform launchTransform = TransformLookup[catapult.LaunchPointEntity];
            Ecb.SetComponent(index, projectile, new LocalTransform
            {
                Position = launchTransform.Position,
                Rotation = launchTransform.Rotation,
                Scale = 1f
            });

            catapult.CurrentReload = catapult.ReloadTime;
        }
    }
}

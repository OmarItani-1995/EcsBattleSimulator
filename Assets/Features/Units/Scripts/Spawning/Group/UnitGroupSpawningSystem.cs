using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[RequireMatchingQueriesForUpdate]
[BurstCompile]
[UpdateInGroup(typeof(UnitLateUpdateSystemGroup))]
[StructLayout(LayoutKind.Auto)]
public partial struct UnitGroupSpawningSystem : ISystem
{
    private EntityQuery _query;
    
    public void OnCreate(ref SystemState state)
    {
        _query = SystemAPI.QueryBuilder()
            .WithAll<UnitGroupSpawningCD>()
            .WithAll<LocalTransform>()
            .WithAll<UnitGroupSpawningState>()
            .Build();
        
        state.RequireForUpdate(_query);
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        var job = new UnitGroupSpawningJob
        {
            Ecb = commandBuffer
        };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
    
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct UnitGroupSpawningJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([EntityIndexInQuery] int index, Entity entity, in LocalTransform transform, in UnitGroupSpawningCD unitGroup)
        {
            var entities = new NativeArray<Entity>(unitGroup.Rows * unitGroup.Columns, Allocator.Temp);
            Ecb.Instantiate(index, unitGroup.Prefab, entities);
            
            float spacing = unitGroup.Spacing;
            int columns = unitGroup.Columns;
            int rows = unitGroup.Rows;
            
            float width = (columns - 1) * spacing;
            float height = (rows - 1) * spacing;
            float3 topLeft = transform.Position - new float3(width / 2, 0, height / 2);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    float3 spawnPos = topLeft + new float3(j * spacing, 0f, i * spacing);
                    Ecb.SetComponent(index, entities[i * columns + j], new LocalTransform
                    {
                        Position = spawnPos,
                        Rotation = transform.Rotation,
                        Scale = 1f
                    });
                }
            }
            
            Ecb.SetComponentEnabled<UnitGroupSpawningState>(index, entity, false);
            entities.Dispose();
        }
    }
}

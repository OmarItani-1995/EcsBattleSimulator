using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[RequireMatchingQueriesForUpdate]
[BurstCompile]
public partial struct UnitGroupSpawningSystem : ISystem
{
    private EntityQuery _active;
    
    public void OnCreate(ref SystemState state)
    {
        _active = SystemAPI.QueryBuilder()
            .WithAll<UnitGroupSpawningCD, LocalTransform, UnitGroupSpawningState>()
            .Build();
        
        state.RequireForUpdate(_active);
    }

    public void OnUpdate(ref SystemState state)
    {

        foreach (var (unitGroup, transform,unitState, entity) in SystemAPI
                     .Query<RefRO<UnitGroupSpawningCD>, RefRO<LocalTransform>, RefRO<UnitGroupSpawningState>>().WithEntityAccess())
        {
            var entities = new NativeArray<Entity>(unitGroup.ValueRO.Rows * unitGroup.ValueRO.Columns, Allocator.Temp);
            state.EntityManager.Instantiate(unitGroup.ValueRO.Prefab, entities);
            
            float spacing = unitGroup.ValueRO.Spacing;
            int columns = unitGroup.ValueRO.Columns;
            int rows = unitGroup.ValueRO.Rows;
            
            float width = (columns - 1) * spacing;
            float height = (rows - 1) * spacing;
            float3 topLeft = transform.ValueRO.Position - new float3(width / 2, 0, height / 2);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    float3 spawnPos = topLeft + new float3(j * spacing, 0f, i * spacing);
                    state.EntityManager.SetComponentData(entities[i * columns + j], new LocalTransform
                    {
                        Position = spawnPos,
                        Rotation = transform.ValueRO.Rotation,
                        Scale = 1f
                    });
                }
            }
            
            SystemAPI.SetComponentEnabled<UnitGroupSpawningState>(entity, false);
            entities.Dispose();
        }
    }
}

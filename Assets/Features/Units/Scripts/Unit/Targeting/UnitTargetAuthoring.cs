using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class UnitTargetAuthoring : MonoBehaviour
{
    class Baker : Baker<UnitTargetAuthoring>
    {
        public override void Bake(UnitTargetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitTargetCD()
            {
                TargetEntity = Entity.Null
            });
            SetComponentEnabled<UnitTargetCD>(entity, false);
        }
    }
}
public struct UnitTargetCD : IComponentData, IEnableableComponent
{
    public Entity TargetEntity;
}

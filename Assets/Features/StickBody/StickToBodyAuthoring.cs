using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class StickToBodyAuthoring : MonoBehaviour
{
    class Baker : Baker<StickToBodyAuthoring>
    {
        public override void Bake(StickToBodyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new StickToBody()
            {
                Target = Entity.Null,
                OffsetPosition = float3.zero
            });
            SetComponentEnabled<StickToBody>(entity, false);
        }
    }
}
public struct StickToBody : IComponentData, IEnableableComponent
{
    public Entity Target;
    public float3 OffsetPosition;
}

using Unity.Entities;
using UnityEngine;

public class ArrowStateAuthoring : MonoBehaviour
{
    public class ArrowStateAuthoringBaker : Baker<ArrowStateAuthoring>
    {
        public override void Bake(ArrowStateAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ArrowState());
            SetComponentEnabled<ArrowState>(entity, true);
        }
    }
}

public struct ArrowState : IComponentData, IEnableableComponent
{
}

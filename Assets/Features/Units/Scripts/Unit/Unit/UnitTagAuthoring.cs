using Unity.Entities;
using UnityEngine;

public class UnitTagAuthoring : MonoBehaviour
{
    public class UnitTagAuthoringBaker : Baker<UnitTagAuthoring>
    {
        public override void Bake(UnitTagAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitTag());
        }
    }
}

public struct UnitTag : IComponentData
{
}

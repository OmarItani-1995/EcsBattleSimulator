using Unity.Entities;
using UnityEngine;

public class ArrowTagAuthoring : MonoBehaviour
{
    public class ArrowTagAuthoringBaker : Baker<ArrowTagAuthoring>
    {
        public override void Bake(ArrowTagAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ArrowTag());
        }
    }
}

public struct ArrowTag : IComponentData
{
    
}

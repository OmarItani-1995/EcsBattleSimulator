using Unity.Entities;
using UnityEngine;

public class FireRainTagAuthoring : MonoBehaviour
{
    public class FireRainTagAuthoringBaker : Baker<FireRainTagAuthoring>
    {
        public override void Bake(FireRainTagAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new FireRainTag());
        }
    }
}

public struct FireRainTag : IComponentData
{
}

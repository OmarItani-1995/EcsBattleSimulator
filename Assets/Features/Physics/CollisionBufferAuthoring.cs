using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CollisionBufferAuthoring : MonoBehaviour
{
    public class Baker : Baker<CollisionBufferAuthoring>
    {
        public override void Bake(CollisionBufferAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddBuffer<CollisionBuffer>(entity);
        }
    }
}

public struct CollisionBuffer : IBufferElementData
{
    public Entity Entity;
    public float3 ContactPoint;
}
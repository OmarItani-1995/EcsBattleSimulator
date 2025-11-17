using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ExplosionForceAuthoring : MonoBehaviour
{
    public class ExplosionForceAuthoringBaker : Baker<ExplosionForceAuthoring>
    {
        public override void Bake(ExplosionForceAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddBuffer<ExplosionForceCD>(entity);
        }
    }
}

public struct ExplosionForceCD : IBufferElementData
{
    public float ForceMagnitude;
    public float3 ForcePoint;
    public float Radius;
}

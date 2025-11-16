using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct StickToBody : IComponentData
{
    public Entity Target;
    public float3 offsetPosition;
}

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct ExplosionForceCD : IComponentData
{
    public float ForceMagnitude;
    public float3 ForcePoint;
}

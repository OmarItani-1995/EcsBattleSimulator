using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct UnitTargetCD : IComponentData
{
    public Entity targetEntity;
}

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class UnitChargingAuthoring : MonoBehaviour
{
    public float MinSpeed;
    public float MaxSpeed;
    public Vector3 Direction;
    public class UnitChargingAuthoringBaker : Baker<UnitChargingAuthoring>
    {
        public override void Bake(UnitChargingAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitChargingCD()
            {
                MinSpeed = authoring.MinSpeed,
                MaxSpeed = authoring.MaxSpeed,
                Direction = authoring.Direction,
            });
        }
    }
}

public struct UnitChargingCD : IComponentData
{
    public float MinSpeed;
    public float MaxSpeed;
    public float3 Direction;
}

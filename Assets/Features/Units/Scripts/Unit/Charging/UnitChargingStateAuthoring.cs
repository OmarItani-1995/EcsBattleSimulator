using Unity.Entities;
using UnityEngine;

public class UnitChargingStateAuthoring : MonoBehaviour
{
    public class UnitChargingStateAuthoringBaker : Baker<UnitChargingStateAuthoring>
    {
        public override void Bake(UnitChargingStateAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitChargingState());
            SetComponentEnabled<UnitChargingState>(entity, true);
        }
    }
}

public struct UnitChargingState : IComponentData, IEnableableComponent
{
}

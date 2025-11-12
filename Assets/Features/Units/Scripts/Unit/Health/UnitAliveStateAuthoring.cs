using Unity.Entities;
using UnityEngine;

public class UnitAliveStateAuthoring : MonoBehaviour
{
    public class UnitAliveStateAuthoringBaker : Baker<UnitAliveStateAuthoring>
    {
        public override void Bake(UnitAliveStateAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitAliveState());
            SetComponentEnabled<UnitAliveState>(entity, true);
        }
    }
}

public struct UnitAliveState : IComponentData, IEnableableComponent
{
}

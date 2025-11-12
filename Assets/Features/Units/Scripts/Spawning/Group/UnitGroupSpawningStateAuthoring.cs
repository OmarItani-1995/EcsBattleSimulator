using Unity.Entities;
using UnityEngine;

public class UnitGroupSpawningStateAuthoring : MonoBehaviour
{
    public class UnitGroupStateSpawningAuthoringBaker : Baker<UnitGroupSpawningStateAuthoring>
    {
        public override void Bake(UnitGroupSpawningStateAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitGroupSpawningState());
            SetComponentEnabled<UnitGroupSpawningState>(entity, true);
        }
    }
}

public struct UnitGroupSpawningState : IComponentData, IEnableableComponent
{
    
}

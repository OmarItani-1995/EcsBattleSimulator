using Unity.Entities;
using UnityEngine;


public class UnitHealthAuthoring : MonoBehaviour
{
    public int StartingHealth;
    public class UnitHealthAuthoringBaker : Baker<UnitHealthAuthoring>
    {
        public override void Bake(UnitHealthAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitHealthCD()
            {
                CurrentHealth = authoring.StartingHealth
            });
            AddBuffer<UnitHitsTaken>(entity);
        }
    }
}

public struct UnitHealthCD : IComponentData
{
    public int CurrentHealth;
}

public struct UnitHitsTaken : IBufferElementData
{
    public int HitAmount;
}

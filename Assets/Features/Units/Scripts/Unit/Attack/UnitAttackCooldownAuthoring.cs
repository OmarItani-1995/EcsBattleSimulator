using Unity.Entities;
using UnityEngine;

public class UnitAttackCooldownAuthoring : MonoBehaviour
{
    public float duration;
    
    public class UnitAttackCooldownAuthoringBaker : Baker<UnitAttackCooldownAuthoring>
    {
        public override void Bake(UnitAttackCooldownAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitAttackCooldownCD()
            {
                Duration = authoring.duration
            });
            AddComponent(entity, new UnitAttackCooldownState());
            SetComponentEnabled<UnitAttackCooldownState>(entity,false);
        }
    }
}

public struct UnitAttackCooldownCD : IComponentData
{
    public float Duration;
    public float Elapsed;
}

public struct UnitAttackCooldownState : IComponentData, IEnableableComponent
{
}
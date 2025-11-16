using Unity.Entities;
using UnityEngine;

public class UnitAttackAuthoring : MonoBehaviour
{
    public class UnitAttackAuthoringBaker : Baker<UnitAttackAuthoring>
    {
        public override void Bake(UnitAttackAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitAttackCD());
            SetComponentEnabled<UnitAttackCD>(entity, false);
        }
    }
}

public struct UnitAttackCD : IComponentData, IEnableableComponent
{
    public float TotalTime;
    public float AttackTime;
    public bool DidAttack;
    public int AttackDamage;
}

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
        }
    }
}

public struct UnitAttackCD : IComponentData
{
    public float totalTime;
    public float attackTime;
    public bool didAttack;
    public int attackDamage;
}

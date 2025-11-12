using Unity.Entities;
using UnityEngine;

public class UnitAttackInfoAuthoring : MonoBehaviour
{
    public float attackRange = 2f;
    public int attackDamage = 100;
    public float attackTotalTime;
    public float attackTime;
    
    public AnimationClipName attackAnimation = AnimationClipName.Charging_Attack;
    
    public class UnitAttackInfoAuthoringBaker : Baker<UnitAttackInfoAuthoring>
    {
        public override void Bake(UnitAttackInfoAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddSharedComponent(entity, new UnitAttackInfoSD()
            {
                attackDamage = authoring.attackDamage,
                attackRange = authoring.attackRange,
                attackAnimation = authoring.attackAnimation,
                attackTime = authoring.attackTime,
                attackTotalTime = authoring.attackTotalTime
            });
        }
    }
}

public struct UnitAttackInfoSD : ISharedComponentData
{
    public float attackTotalTime;
    public float attackTime;
    public float attackRange;
    public int attackDamage;
    public AnimationClipName attackAnimation;
}

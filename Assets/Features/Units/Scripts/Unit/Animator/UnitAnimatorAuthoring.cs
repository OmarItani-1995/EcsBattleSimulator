using Unity.Entities;
using UnityEngine;

public class UnitAnimatorAuthoring : MonoBehaviour
{
    public class UnitAnimatorAuthoringBaker : Baker<UnitAnimatorAuthoring>
    {
        public override void Bake(UnitAnimatorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitAnimatorCD()
            {
                AnimatorEntity = Entity.Null
            });
            SetComponentEnabled<UnitAnimatorCD>(entity, false);
        }
    }
}

public struct UnitAnimatorCD : IComponentData, IEnableableComponent
{
    public Entity AnimatorEntity;
}

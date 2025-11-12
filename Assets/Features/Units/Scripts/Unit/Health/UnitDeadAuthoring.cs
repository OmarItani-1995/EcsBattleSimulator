using Unity.Entities;
using UnityEngine;

public class UnitDeadAuthoring : MonoBehaviour
{
    public float deathDuration = 5f;
    public class UnitDeadAuthoringBaker : Baker<UnitDeadAuthoring>
    {
        public override void Bake(UnitDeadAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitDeadCD()
            {
                deathDuration = authoring.deathDuration
            });
        }
    }
}

public struct UnitDeadCD : IComponentData
{
    public float deathDuration;
}

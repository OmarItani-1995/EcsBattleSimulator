using Unity.Entities;
using UnityEngine;

public class ArrowDestroyCooldownAuthoring : MonoBehaviour
{
    public float Cooldown;
    public class ArrowDestroyCooldownAuthoringBaker : Baker<ArrowDestroyCooldownAuthoring>
    {
        public override void Bake(ArrowDestroyCooldownAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ArrowDestroyCooldownComponentData()
            {
                Cooldown = authoring.Cooldown
            });
        }
    }
}

public struct ArrowDestroyCooldownComponentData : IComponentData
{
    public float Cooldown;
}

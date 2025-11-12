using Unity.Entities;
using UnityEngine;

public class UnitGroupSpawningAuthoring : MonoBehaviour
{
    public int Rows;
    public int Columns;
    public float Spacing;
    public GameObject Prefab;
    public class UnitGroupSpawningAuthoringBaker : Baker<UnitGroupSpawningAuthoring>
    {
        public override void Bake(UnitGroupSpawningAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.WorldSpace);
            AddComponent(entity, new UnitGroupSpawningCD()
            {
                Rows = authoring.Rows,
                Columns = authoring.Columns,
                Spacing = authoring.Spacing,
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}

public struct UnitGroupSpawningCD : IComponentData
{
    public int Rows;
    public int Columns;
    public float Spacing;
    public Entity Prefab;
}

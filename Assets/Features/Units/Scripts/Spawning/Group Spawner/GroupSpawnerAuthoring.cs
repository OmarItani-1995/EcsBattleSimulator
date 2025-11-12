using Unity.Entities;
using UnityEngine;

public class GroupSpawnerAuthoring : MonoBehaviour
{
    public GameObject groupToSpawn;
    public float SpawnInterval;
    public class GroupSpawnerAuthoringBaker : Baker<GroupSpawnerAuthoring>
    {
        public override void Bake(GroupSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new GroupSpawnerCD()
            {
                groupToSpawn = GetEntity(authoring.groupToSpawn, TransformUsageFlags.Dynamic),
                SpawnInterval = authoring.SpawnInterval,
                SpawnTimer = 0f,
            });
        }
    }
}

public struct GroupSpawnerCD : IComponentData
{
    public Entity groupToSpawn;
    public float SpawnInterval;
    public float SpawnTimer;
}

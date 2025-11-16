using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class FireRainManager : MonoBehaviour
{
    [SerializeField] private List<FireRain> fireRainPrefabs;
    
    private EntityManager _entityManager;
    private EntityArchetype _fireRainArchetype;
    
    void Start()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _fireRainArchetype = _entityManager.CreateArchetype(
            typeof(FireRainTag),
            typeof(LocalTransform)
        );

        InitializeFireRainPrefabs();
    }

    private void InitializeFireRainPrefabs()
    {
        foreach (var prefab in fireRainPrefabs)
        {
            prefab.PlayParticle(OnSpawnEntity);
        }
    }

    void OnSpawnEntity(Vector3 position)
    {
        Entity entity = _entityManager.CreateEntity(_fireRainArchetype);
        _entityManager.SetComponentData(entity, new LocalTransform
        {
            Position = position,
            Rotation = Quaternion.identity,
            Scale = 1f
        });
    }
}

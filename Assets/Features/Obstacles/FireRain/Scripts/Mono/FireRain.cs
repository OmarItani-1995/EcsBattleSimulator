using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class FireRain : MonoBehaviour
{
    public ParticleSystem effect;
    public GameObject explosionPrefab;
    
    private List<ParticleCollisionEvent> _collisionEvents = new List<ParticleCollisionEvent>();
    private Action<Vector3> _spawnExplosion;

    private EntityManager _entityManager;
    void Start()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }
    
    public void PlayParticle(Action<Vector3> spawnExplosion)
    {
        _spawnExplosion = spawnExplosion;
        effect.Play();
    }
    private void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = effect.GetCollisionEvents(other, _collisionEvents);
        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 collisionPoint = _collisionEvents[i].intersection;
            _spawnExplosion?.Invoke(collisionPoint);
        }
    }
}

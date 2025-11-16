using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ArrowShootingAuthoring : MonoBehaviour
{
    public GameObject arrowPrefab;
    public Transform shootingPoint;
    public Vector3 shootingPosition;
    public Quaternion shootingRotation;
    public Vector3 shootingDirection;
    
    public float attackTime;
    public float force;
    public float forceRandomness;
    public class ArrowShootingAuthoringBaker : Baker<ArrowShootingAuthoring>
    {
        public override void Bake(ArrowShootingAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ArrowShootingCD()
            {
                Arrow = GetEntity(authoring.arrowPrefab, TransformUsageFlags.Dynamic),
                ShootingPosition = authoring.shootingPosition,
                ShootingRotation = authoring.shootingRotation,
                ShootingDirection = authoring.shootingDirection,
                AttackTime = authoring.attackTime,
                Force = authoring.force,
                ForceRandomness = authoring.forceRandomness,
            });
        }
    }

    [ContextMenu("Bake Data")]
    private void Co_BakeData()
    {
        shootingPosition = shootingPoint.localPosition;
        shootingRotation = shootingPoint.rotation;
        shootingDirection = shootingPoint.forward;
    }
}

public struct ArrowShootingCD : IComponentData
{
    public Entity Arrow;
    public float3 ShootingPosition;
    public quaternion ShootingRotation;
    public float3 ShootingDirection;
    public float AttackTime;
    public float Elapsed;

    public float Force;
    public float ForceRandomness;
}

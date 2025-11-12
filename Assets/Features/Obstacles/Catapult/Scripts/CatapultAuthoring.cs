using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

public class CatapultAuthoring : MonoBehaviour
{
    public GameObject projectileEntity;
    public Transform launchPointEntity;
    public float launchForce;
    public float reloadTime;
    public float currentReload;
    public class CatapultAuthoringBaker : Baker<CatapultAuthoring>
    {
        public override void Bake(CatapultAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CatapultCD()
            {
                ProjectileEntity = GetEntity(authoring.projectileEntity),
                LaunchPointEntity = GetEntity(authoring.launchPointEntity, TransformUsageFlags.Dynamic),
                LaunchForce = authoring.launchForce,
                ReloadTime = authoring.reloadTime,
                CurrentReload = authoring.currentReload
            });
        }
    }
}

public struct CatapultCD : IComponentData
{
    public Entity ProjectileEntity;
    public Entity LaunchPointEntity;
    public float LaunchForce;
    public float ReloadTime;
    public float CurrentReload;
}

using Lib.EcsParticle;
using Lib.EcsParticle.StartPosition;
using Lib.EcsParticle.StartRotation;
using Lib.EcsParticle.StartSize;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

public class EcsParticleAuthoring : MonoBehaviour
{
    public float duration;
    public bool loop;
    public float lifeTime;
    
    [SerializeReference] public EcsParticleStartSize startSize = new EcsParticleStartSizeConstant();
    [SerializeReference] public EcsParticleStartPosition startPosition = new EcsParticleStartPositionConstant();
    [SerializeReference] public EcsParticleStartRotation startRotation = new EcsParticleStartRotationConstant();
    
    public class EcsParticleAuthoringBaker : Baker<EcsParticleAuthoring>
    {
        public override void Bake(EcsParticleAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.ManualOverride);
            AddComponent(entity, new EcsParticle()
            {
                Duration = authoring.duration,
                Loop = authoring.loop,
                LifeTime = authoring.lifeTime
            });
            AddComponent<EcsParticleState>(entity, new EcsParticleState());
            SetComponentEnabled<EcsParticleState>(entity, true);
            
            AddComponent(entity, authoring.startSize.GetSize());
            AddComponent(entity, authoring.startPosition.GetPosition());
            AddComponent(entity, authoring.startRotation.GetRotation());
        }
    }
}

public struct EcsParticle : IComponentData
{
    public float Duration;
    public bool Loop;
    public float LifeTime;
}

public struct EcsParticleState : IComponentData, IEnableableComponent
{
}


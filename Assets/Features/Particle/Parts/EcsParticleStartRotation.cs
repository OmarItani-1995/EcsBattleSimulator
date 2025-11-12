using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Lib.EcsParticle
{
    [System.Serializable]
    public abstract class EcsParticleStartRotation
    {
        public abstract EcsParticleStartRotationData GetRotation();
    }
    
    public struct EcsParticleStartRotationData : IComponentData
    {
        public float3 MinRotation;
        public float3 MaxRotation;
    }
}

namespace Lib.EcsParticle.StartRotation
{
    [System.Serializable]
    public class EcsParticleStartRotationConstant : EcsParticleStartRotation
    {
        public float3 rotation = float3.zero;
        
        public override EcsParticleStartRotationData GetRotation()
        {
            return new EcsParticleStartRotationData()
            {
                MaxRotation = rotation,
                MinRotation = rotation
            };
        }
    }
    
    [System.Serializable]
    public class EcsParticleStartRotationRandomBetweenTwoConstants : EcsParticleStartRotation
    {
        public float3 minRotation = new float3(-180f, -180f, -180f);
        public float3 maxRotation = new float3(180f, 180f, 180f);
        
        public override EcsParticleStartRotationData GetRotation()
        {
            return new EcsParticleStartRotationData()
            {
                MaxRotation = maxRotation,
                MinRotation = minRotation
            };
        }
    }
}
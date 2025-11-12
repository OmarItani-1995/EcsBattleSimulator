using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Lib.EcsParticle
{
    [System.Serializable]
    public abstract class EcsParticleStartPosition
    {
        public EcsParticlePositionSpace positionSpace = EcsParticlePositionSpace.World;
        
        public abstract EcsParticleStartPositionData GetPosition();
    }

    public struct EcsParticleStartPositionData : IComponentData
    {
        public EcsParticlePositionSpace Space;
        public float3 MinPosition;
        public float3 MaxPosition;
    }

    public enum EcsParticlePositionSpace
    {
        World, 
        Parent
    }
}

namespace Lib.EcsParticle.StartPosition
{
    [System.Serializable]
    public class EcsParticleStartPositionConstant : EcsParticleStartPosition
    {
        public float3 position = float3.zero;

        public override EcsParticleStartPositionData GetPosition()
        {
            return new EcsParticleStartPositionData()
            {
                Space = positionSpace,
                MinPosition = position,
                MaxPosition = position
            };
        }

        [System.Serializable]
        public class EcsParticleStartPositionRandomBetweenTwoConstants : EcsParticleStartPosition
        {
            public float3 minPosition = new float3(-1f, -1f, -1f);
            public float3 maxPosition = new float3(1f, 1f, 1f);

            public override EcsParticleStartPositionData GetPosition()
            {
                return new EcsParticleStartPositionData()
                {
                    Space = positionSpace,
                    MinPosition = minPosition,
                    MaxPosition = maxPosition
                };
            }
        }
    }
}
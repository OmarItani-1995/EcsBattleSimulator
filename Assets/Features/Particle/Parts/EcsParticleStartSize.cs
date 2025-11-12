using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Lib.EcsParticle 
{
    [System.Serializable]
    public abstract class EcsParticleStartSize
    {
        public abstract EcsParticleStartSizeData GetSize();
    }

    public struct EcsParticleStartSizeData : IComponentData
    {
        public float minSize;
        public float maxSize;
    }
}

namespace Lib.EcsParticle.StartSize
{
    [System.Serializable]
    public class EcsParticleStartSizeConstant : EcsParticleStartSize
    {
        public float size = 1f;
        public override EcsParticleStartSizeData GetSize()
        {
            return new EcsParticleStartSizeData()
            {
                maxSize = size,
                minSize = size
            };
        }
    }

    [System.Serializable]
    public class EcsParticleStartSizeRandomBetweenTwoConstants : EcsParticleStartSize
    {
        public float minSize = 0.5f;
        public float maxSize = 1.5f;
        public override EcsParticleStartSizeData GetSize()
        {
            return new EcsParticleStartSizeData()
            {
                maxSize = maxSize,
                minSize = minSize
            };
        }
    }
}

using Unity.Entities;
using UnityEngine;

public class AnimatorAuthoring : MonoBehaviour
{
    public AnimationClipName startingClip;
    public bool startingLoop;
    public class AnimatorAuthoringBaker : Baker<AnimatorAuthoring>
    {
        public override void Bake(AnimatorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AnimatorComponentData()
            {
                currentClip = authoring.startingClip,
                loop = authoring.startingLoop,
            });
        }
    }
}

public struct AnimatorComponentData : IComponentData
{
    public AnimationClipName currentClip;
    public bool loop;
    
    public int currentTick;
    public double lastTickTime;
}


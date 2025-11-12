using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class NavMeshAgentAuthoring : MonoBehaviour
{
    public float MoveSpeed;
    public class NavMeshAgentAuthoringBaker : Baker<NavMeshAgentAuthoring>
    {
        public override void Bake(NavMeshAgentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new NavMeshAgentCD()
            {
                moveSpeed = authoring.MoveSpeed,
            });
            AddBuffer<NavMeshWaypointBuffer>(entity);
        }
    }
}

public struct NavMeshAgentCD : IComponentData
{
    public float moveSpeed;
}

public struct NavMeshWaypointBuffer : IBufferElementData
{
    public float3 position;
}

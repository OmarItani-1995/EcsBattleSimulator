using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

[BurstCompile]
public partial class AnimationClipSystem : SystemBase
{
    private NativeHashMap<int, BlobAssetReference<AnimationClipData>> _clipData;
    private EntityQuery _query;
    protected override void OnStartRunning()
    {
        var entitiesGraphicsSystem = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
        var originalClips = Resources.LoadAll<AnimatedMeshScriptableObject>("");
        int totalClipCounts = 0;
        foreach (var clip in originalClips)
        {
            totalClipCounts += clip.Animations.Count;
        }

        _clipData = new NativeHashMap<int, BlobAssetReference<AnimationClipData>>(totalClipCounts, Allocator.Persistent);

        
        foreach (var clipHolder in originalClips)
        {
            foreach (var animation in clipHolder.Animations)
            {
                var blobBuilder = new BlobBuilder(Allocator.Temp);
                ref var root = ref blobBuilder.ConstructRoot<AnimationClipData>();
                var meshIDs = blobBuilder.Allocate(ref root.MeshIndices, animation.Meshes.Count);
                for (var k = 0; k < animation.Meshes.Count; k++)
                {
                    var mesh = animation.Meshes[k];
                    meshIDs[k] = entitiesGraphicsSystem.RegisterMesh(mesh);
                }
                root.ClipName = animation.ClipName;
                root.AnimationFPS = clipHolder.AnimationFPS;
                var blob = blobBuilder.CreateBlobAssetReference<AnimationClipData>(Allocator.Persistent);
                blobBuilder.Dispose();
                _clipData.Add((int) animation.ClipName, blob);
            }
        }

        _query = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<AnimatorComponentData>()
            .WithAll<MaterialMeshInfo>()
            .Build(this);
    }

    protected override void OnDestroy()
    {
        foreach (var kvp in _clipData)
        {
            kvp.Value.Dispose();
        }
        _clipData.Dispose();
    }

    protected override void OnUpdate()
    {
        double time = SystemAPI.Time.ElapsedTime;

        var job = new AnimationClipJob
        {
            time = time,
            clipData = _clipData
        };
        this.Dependency = job.ScheduleParallel(_query, this.Dependency);
    }
    
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    private partial struct AnimationClipJob : IJobEntity
    {
        [ReadOnly] public double time;
        [ReadOnly] public NativeHashMap<int, BlobAssetReference<AnimationClipData>> clipData;

        private void Execute(ref AnimatorComponentData animator, ref MaterialMeshInfo materialMesh)
        {
            var currentClip = clipData[(int)animator.currentClip];
            materialMesh.MeshID = currentClip.Value.MeshIndices[animator.currentTick];
            
            if (time >= animator.lastTickTime + 1f / (currentClip.Value.AnimationFPS))
            {
                if (animator.currentTick+1 >= currentClip.Value.MeshIndices.Length)
                {
                    if (animator.loop)
                    {
                        animator.currentTick = 0;
                    }
                }
                else
                { 
                    animator.currentTick++;
                }
                animator.lastTickTime = time;
            }
        }
    }
}

public struct AnimationClipData
{
    public int AnimationFPS;
    public AnimationClipName ClipName;
    public BlobArray<BatchMeshID> MeshIndices;
}

public enum AnimationClipName
{
    None = 0,
    Charing_Run = 1, 
    Charging_Die = 2,
    Charging_Attack = 3,
    Standing_ShootArrow = 4,
    Idle = 5,
}

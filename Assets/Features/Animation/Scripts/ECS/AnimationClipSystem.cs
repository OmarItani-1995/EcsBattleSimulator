using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

[BurstCompile]
public partial class AnimationClipSystem : SystemBase
{
    public NativeHashMap<int, BlobAssetReference<AnimationClipData>> clipData;
    private EntityQuery _query;
    protected override void OnStartRunning()
    {
        var entitiesGraphicsSystem = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
        var originalClips = Resources.LoadAll<AnimatedMeshScriptableObject>("");
        int totalClipCounts = 0;
        for (int i = 0; i < originalClips.Length; i++)
        {
            totalClipCounts += originalClips[i].Animations.Count;
        }

        clipData = new NativeHashMap<int, BlobAssetReference<AnimationClipData>>(totalClipCounts, Allocator.Persistent);

        
        for (int i = 0; i < originalClips.Length; i++)
        {
            var clipHolder = originalClips[i];
            for (int j = 0; j < clipHolder.Animations.Count; j++)
            {
                var animation = clipHolder.Animations[j];
                var blobBuilder = new BlobBuilder(Allocator.Temp);
                ref var root = ref blobBuilder.ConstructRoot<AnimationClipData>();
                var meshIDs = blobBuilder.Allocate(ref root.meshIndices, animation.Meshes.Count);
                for (int k = 0; k < animation.Meshes.Count; k++)
                {
                    var mesh = animation.Meshes[k];
                    meshIDs[k] = entitiesGraphicsSystem.RegisterMesh(mesh);
                }
                root.clipName = animation.ClipName;
                root.animationFPS = originalClips[i].AnimationFPS;
                var blob = blobBuilder.CreateBlobAssetReference<AnimationClipData>(Allocator.Persistent);
                blobBuilder.Dispose();
                clipData.Add((int) animation.ClipName, blob);
            }
        }

        _query = new EntityQueryBuilder(Allocator.Persistent)
            .WithAll<AnimatorComponentData>()
            .WithAll<MaterialMeshInfo>()
            .Build(this);
    }

    protected override void OnDestroy()
    {
        clipData.Dispose();
    }

    protected override void OnUpdate()
    {
        double time = SystemAPI.Time.ElapsedTime;

        var job = new AnimationClipJob
        {
            time = time,
            clipData = clipData
        };
        this.Dependency = job.ScheduleParallel(_query, this.Dependency);
    }
    
    [BurstCompile]
    private partial struct AnimationClipJob : IJobEntity
    {
        [ReadOnly] public double time;
        [ReadOnly] public NativeHashMap<int, BlobAssetReference<AnimationClipData>> clipData;
        public void Execute(ref AnimatorComponentData animator, ref MaterialMeshInfo materialMesh)
        {
            var currentClip = clipData[(int)animator.currentClip];
            materialMesh.MeshID = currentClip.Value.meshIndices[animator.currentTick];
            
            if (time >= animator.lastTickTime + 1f / (currentClip.Value.animationFPS))
            {
                if (animator.currentTick+1 >= currentClip.Value.meshIndices.Length)
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
    public int animationFPS;
    public AnimationClipName clipName;
    public BlobArray<BatchMeshID> meshIndices;
}

public enum AnimationClipName
{
    None = 0,
    Charing_Run = 1, 
    Charging_Die = 2,
    Charging_Attack = 3,
}

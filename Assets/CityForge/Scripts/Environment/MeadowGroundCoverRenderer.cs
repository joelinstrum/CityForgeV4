using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge
{
    // Isolated showcase renderer. Existing region generation and meadow scenes keep their renderer.
    [ExecuteAlways] public sealed class MeadowGroundCoverRenderer : MonoBehaviour
    {
        public NativeMeadowBatch[] batches;
        public Camera presentationCamera;
        public CameraSystem.CameraMotionQuality navigationQuality;
        public bool adaptiveDensity = true;
        [Range(.1f,1f)] public float lod0Density = 1f;
        [Range(.1f,1f)] public float closestZoomDensity = 1f;
        public float closestOrthographicThreshold;
        // Runtime tiles opt in; approved authored reference batching stays unchanged.
        public bool combineCompatibleBatches;
        NativeMeadowBatch[][] mergedLevels,mergedCloseLevels;
        [NonSerialized] public int submittedInstances, submittedBatches;
        [NonSerialized] public double submissionMilliseconds;
        [NonSerialized] public long submissionAllocatedBytes;
        Matrix4x4[][][] levels, closeLevels;

        NativeMeadowBatch[] cachedBatches;
        float cachedDensity,cachedCloseDensity;bool cachedCombine;
        public int DensityCacheBuilds {get;private set;}
        bool CacheValid=>levels!=null&&ReferenceEquals(cachedBatches,batches)&&cachedDensity==lod0Density&&cachedCloseDensity==closestZoomDensity&&cachedCombine==combineCompatibleBatches;
        void OnEnable() { if(!CacheValid)RebuildDensityCache(); }
        public void InvalidateDensityCache(){cachedBatches=null;}
        public void RebuildDensityCache(){var work=BuildDensityCache();while(work.MoveNext()){} }
        public IEnumerator BuildDensityCache()
        {
            if (batches == null) { levels = closeLevels = null; mergedLevels=mergedCloseLevels=null; yield break; }
            cachedBatches=batches;cachedDensity=lod0Density;cachedCloseDensity=closestZoomDensity;cachedCombine=combineCompatibleBatches;DensityCacheBuilds++;
            levels = new Matrix4x4[batches.Length][][];
            for (int b = 0; b < batches.Length; b++)
            {
                yield return null;
                var source = batches[b].matrices;
                if (source == null) { levels[b] = new Matrix4x4[4][]; continue; }
                levels[b] = new Matrix4x4[4][];
                if(source.Length > 0 && lod0Density < .999f) {
                    int count=Mathf.Max(1,Mathf.RoundToInt(source.Length*lod0Density));
                    var reduced=new Matrix4x4[count];
                    for(int i=0;i<count;i++) reduced[i]=source[Mathf.Min(source.Length-1,Mathf.FloorToInt(i/lod0Density))];
                    source=reduced;
                }
                levels[b][0] = source;
                for (int level = 1; level < 4; level++)
                {
                    int stride = 1 << level;
                    var cache = new Matrix4x4[(source.Length + stride - 1) / stride];
                    for (int i = 0; i < cache.Length; i++) cache[i] = source[i * stride];
                    levels[b][level] = cache;
                }
            }
            closeLevels=new Matrix4x4[batches.Length][][];
            for(int b=0;b<batches.Length;b++) {
                yield return null;
                closeLevels[b]=new Matrix4x4[4][];
                for(int level=0;level<4;level++) {
                    var source=levels[b][level];
                    if(source==null || source.Length==0 || closestZoomDensity>=.999f){closeLevels[b][level]=source;continue;}
                    int count=Mathf.Max(1,Mathf.RoundToInt(source.Length*closestZoomDensity));
                    var reduced=new Matrix4x4[count];
                    for(int i=0;i<count;i++)reduced[i]=source[Mathf.Min(source.Length-1,Mathf.FloorToInt(i/closestZoomDensity))];
                    closeLevels[b][level]=reduced;
                }
            }
            mergedLevels=mergedCloseLevels=null;
            if(combineCompatibleBatches){
                mergedLevels=new NativeMeadowBatch[4][];mergedCloseLevels=new NativeMeadowBatch[4][];
                for(int level=0;level<4;level++){yield return null;mergedLevels[level]=MergeLevel(levels,level);yield return null;mergedCloseLevels[level]=MergeLevel(closeLevels,level);}
            }
        }
        NativeMeadowBatch[] MergeLevel(Matrix4x4[][][] density,int level)
        {
            var groups=new Dictionary<(Mesh,Material,int,Bounds),List<Matrix4x4>>();
            for(int i=0;i<batches.Length;i++){
                var b=batches[i];var matrices=density[i][level];if(matrices==null||matrices.Length==0)continue;
                var key=(b.mesh,b.material,b.submesh,b.bounds);
                if(!groups.TryGetValue(key,out var list))groups[key]=list=new List<Matrix4x4>();
                list.AddRange(matrices);
            }
            var result=new List<NativeMeadowBatch>();
            foreach(var group in groups)for(int offset=0;offset<group.Value.Count;offset+=511){
                // 511 also accommodates Unity's default world-to-object instance data.
                int count=Mathf.Min(511,group.Value.Count-offset);var matrices=new Matrix4x4[count];group.Value.CopyTo(offset,matrices,0,count);
                result.Add(new NativeMeadowBatch{mesh=group.Key.Item1,material=group.Key.Item2,submesh=group.Key.Item3,bounds=group.Key.Item4,matrices=matrices});
            }
            return result.ToArray();
        }
        void LateUpdate()
        {
            if(navigationQuality&&navigationQuality.NavigationWorkSuspended){submittedInstances=submittedBatches=0;submissionMilliseconds=0;submissionAllocatedBytes=0;return;}
            if (batches == null || !presentationCamera) return;
            if(!CacheValid)RebuildDensityCache();
            long start = Stopwatch.GetTimestamp(), allocation = GC.GetAllocatedBytesForCurrentThread();
            submittedInstances = submittedBatches = 0;
            var camera = presentationCamera;
            var origin = camera.transform.position;
            bool orthographic = camera.orthographic;
            var nav=camera.GetComponent<CameraSystem.MeadowShowcaseNavigation>();
            bool close=nav ? nav.ClosestTwoZoomLevels : orthographic && closestOrthographicThreshold>0 && camera.orthographicSize<=closestOrthographicThreshold;
            int aerialLevel = camera.orthographicSize > 220 ? 3 : camera.orthographicSize > 145 ? 2 : camera.orthographicSize > 90 ? 1 : 0;
            if(!adaptiveDensity)aerialLevel=0;
            // Merge AFTER thinning, retaining exactly the same matrices at every LOD.
            // Bounds remain per tile and submissions remain visible to reflection cameras.
            var drawBatches=combineCompatibleBatches&&orthographic?(close?mergedCloseLevels[aerialLevel]:mergedLevels[aerialLevel]):batches;
            for (int i = 0; i < drawBatches.Length; i++)
            {
                var batch = drawBatches[i];
                if (!batch.mesh || !batch.material || batch.matrices == null || batch.matrices.Length == 0) continue;
                int level = 0;
                if (adaptiveDensity)
                {
                    if (orthographic) level = aerialLevel;
                    else
                    {
                        var delta = batch.bounds.center - origin;
                        float distance = new Vector2(delta.x, delta.z).magnitude;
                        if (distance > 650f) continue;
                        level = distance > 350 ? 3 : distance > 220 ? 2 : distance > 125 ? 1 : 0;
                    }
                }
                var matrices = combineCompatibleBatches&&orthographic?batch.matrices:close ? closeLevels[i][level] : levels[i][level];
                if (matrices == null) continue;
                // Leave per-camera frustum culling to Unity so Crest reflections retain their own visibility.
                var parameters = new RenderParams(batch.material) { worldBounds = batch.bounds,
                    shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true };
                Graphics.RenderMeshInstanced(parameters, batch.mesh, batch.submesh, matrices);
                submittedInstances += matrices.Length; submittedBatches++;
            }
            submissionMilliseconds = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            submissionAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocation;
        }
    }
}

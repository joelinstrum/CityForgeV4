using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge
{
    /// <summary>Spatial instancing for static forests, with individual near views and inexpensive distant shadows.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class SpatialForestRenderer : MonoBehaviour
    {
        public Camera viewCamera;
        public bool instanced = true;
        public float minimumInstancedViewSize = 200;
        public bool simplifiedDistantShadows;
        public Mesh[] shadowSources = Array.Empty<Mesh>(), shadowReplacements = Array.Empty<Mesh>();
        public int TreeCount => trees.Count;
        public int SubmittedInstances { get; private set; }
        public int SubmittedBatches { get; private set; }
        public int VisibleTrees { get; private set; }
        public int SelectionRebuilds { get; private set; }
        public double SubmissionMilliseconds { get; private set; }
        public long SubmissionAllocatedBytes { get; private set; }
        const int Capacity = 511;
        const float CellSide = 128;
        sealed class Part { public Mesh mesh; public Material material; public int submesh; public Matrix4x4 matrix; public Bounds bounds; public ShadowCastingMode shadows; public Mesh shadowMesh; }
        sealed class Level { public float threshold; public Part[] parts; }
        sealed class Tree { public Transform root;public Vector3 reference; public float size; public Bounds bounds; public Level[] levels; public int x,z; }
        sealed class Group {
            public Mesh mesh; public Material material; public int submesh, count, stamp;
            public ShadowCastingMode shadows; public Bounds bounds;
            public readonly List<Matrix4x4[]> chunks = new();
        }
        readonly List<Tree> trees = new();
        readonly List<Renderer> originals = new();
        readonly List<bool> originalEnabled = new();
        readonly List<LODGroup> lodGroups = new();
        readonly List<bool> lodEnabled = new();
        readonly Dictionary<(Mesh,Material,int,int,int,ShadowCastingMode),Group> groups = new();
        readonly List<Group> active = new();
        readonly Plane[] planes = new Plane[6];
        bool captured, applied;
        int stamp;
        bool selectionValid, cachedOrtho, cachedSimpleShadows;
        Vector3 cachedPosition; Quaternion cachedRotation;
        float cachedSize, cachedFov, cachedBias;
        int cachedVisible;
        static Bounds WorldBounds(Bounds local,Matrix4x4 matrix)
        {
            var x=matrix.MultiplyVector(new Vector3(local.extents.x,0,0));
            var y=matrix.MultiplyVector(new Vector3(0,local.extents.y,0));
            var z=matrix.MultiplyVector(new Vector3(0,0,local.extents.z));
            return new Bounds(matrix.MultiplyPoint3x4(local.center),2*new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z)));
        }
        public void Capture(bool prepareInstancedView=false)
        {
            if (captured) return;
            CaptureLods(GetComponentsInChildren<LODGroup>(true));captured=true;
            if(prepareInstancedView&&instanced&&viewCamera&&viewCamera.orthographicSize>=minimumInstancedViewSize)
                SetOriginals(true);
        }
        void CaptureLods(IEnumerable<LODGroup> sources)
        {
            var shadowMap=new Dictionary<Mesh,Mesh>();
            for(int i=0;i<Mathf.Min(shadowSources.Length,shadowReplacements.Length);i++)
                if(shadowSources[i]&&shadowReplacements[i])shadowMap[shadowSources[i]]=shadowReplacements[i];
            foreach (var lod in sources) {
                var entry = new Tree { root=lod.transform,reference = lod.transform.TransformPoint(lod.localReferencePoint),
                    size = lod.size * Mathf.Max(Mathf.Abs(lod.transform.lossyScale.x),Mathf.Abs(lod.transform.lossyScale.y),Mathf.Abs(lod.transform.lossyScale.z)),
                    x = Mathf.FloorToInt(lod.transform.position.x / CellSide), z = Mathf.FloorToInt(lod.transform.position.z / CellSide) };
                var source = lod.GetLODs(); entry.levels = new Level[source.Length]; bool hasBounds=false;
                for (int level=0;level<source.Length;level++) {
                    var parts = new List<Part>();
                    foreach (var r in source[level].renderers) {
                        var filter = r.GetComponent<MeshFilter>(); if (!filter || !filter.sharedMesh) continue;
                        originals.Add(r); originalEnabled.Add(r.enabled);
                        var worldBounds=WorldBounds(filter.sharedMesh.bounds,r.localToWorldMatrix);
                        if (level==0) { if (!hasBounds) {entry.bounds=worldBounds;hasBounds=true;} else entry.bounds.Encapsulate(worldBounds); }
                        var materials = r.sharedMaterials;
                        for (int sub=0;sub<filter.sharedMesh.subMeshCount;sub++) {
                            var mat=materials[Mathf.Min(sub,materials.Length-1)];
                            if (!mat.enableInstancing) throw new InvalidOperationException("Tree material must support instancing: "+mat.name);
                            shadowMap.TryGetValue(filter.sharedMesh,out var shadowMesh);
                            parts.Add(new Part {mesh=filter.sharedMesh,shadowMesh=shadowMesh?shadowMesh:filter.sharedMesh,material=mat,submesh=sub,matrix=r.localToWorldMatrix,bounds=worldBounds,shadows=r.shadowCastingMode});
                        }
                    }
                    entry.levels[level]=new Level {threshold=source[level].screenRelativeTransitionHeight,parts=parts.ToArray()};
                }
                trees.Add(entry); lodGroups.Add(lod); lodEnabled.Add(lod.enabled);
            }
        }
        public void Append(IReadOnlyList<Transform> roots)
        {
            int renderStart=originals.Count,lodStart=lodGroups.Count;
            foreach(var root in roots)CaptureLods(root.GetComponentsInChildren<LODGroup>(true));
            if(applied){for(int i=renderStart;i<originals.Count;i++)if(originals[i])originals[i].enabled=false;for(int i=lodStart;i<lodGroups.Count;i++)if(lodGroups[i])lodGroups[i].enabled=false;}
            selectionValid=false;active.Clear();
        }
        public int RemoveWithin(Vector2 center,float radius)=>RemoveWithin(new[]{center},radius);
        public int RemoveWithin(IReadOnlyList<Vector2> centers,float radius)
        {
            float radius2=radius*radius;
            return RemoveTrees(point => World.ForestRemovalValidation.InBrush(point, centers, radius2));
        }
        public int RemoveTrees(System.Predicate<Vector2> contains)
        {
            int removed=0;
            for(int i=trees.Count-1;i>=0;i--){var tree=trees[i];if(!tree.root)continue;var p=tree.root.position;
                if(!contains(new Vector2(p.x,p.z)))continue;
                tree.root.gameObject.SetActive(false);Destroy(tree.root.gameObject);trees.RemoveAt(i);removed++;
            }
            if(removed>0){selectionValid=false;active.Clear();SubmittedInstances=SubmittedBatches=VisibleTrees=0;}
            return removed;
        }
        void SetOriginals(bool hide)
        {
            for(int i=0;i<lodGroups.Count;i++)if(lodGroups[i])lodGroups[i].enabled=hide?false:lodEnabled[i];
            for(int i=0;i<originals.Count;i++)if(originals[i])originals[i].enabled=hide?false:originalEnabled[i];
            applied=hide;
        }
        void Queue(Tree tree,Part part,bool visible,bool shadowPass=false)
        {
            // Let Unity cull bounded spatial groups for each camera, including reflections.
            var shadow=simplifiedDistantShadows?(shadowPass?ShadowCastingMode.ShadowsOnly:ShadowCastingMode.Off):part.shadows;
            var mesh=shadowPass?part.shadowMesh:part.mesh;
            var key=(mesh,part.material,part.submesh,tree.x,tree.z,shadow);
            if(!groups.TryGetValue(key,out var group)) {
                group=new Group {mesh=mesh,material=part.material,submesh=part.submesh,shadows=shadow};groups.Add(key,group);
            }
            if(group.stamp!=stamp){group.stamp=stamp;group.count=0;group.bounds=part.bounds;active.Add(group);}else group.bounds.Encapsulate(part.bounds);
            int chunk=group.count/Capacity,offset=group.count%Capacity;
            if(chunk==group.chunks.Count)group.chunks.Add(new Matrix4x4[Capacity]);
            group.chunks[chunk][offset]=part.matrix;group.count++;
        }
        void LateUpdate()
        {
            if(!viewCamera)return;
            Capture();bool useInstancing=instanced && viewCamera.orthographicSize>=minimumInstancedViewSize;
            if(applied!=useInstancing)SetOriginals(useInstancing);
            SubmittedInstances=SubmittedBatches=VisibleTrees=0;SubmissionAllocatedBytes=0;SubmissionMilliseconds=0;
            if(!useInstancing)return;
            long start=System.Diagnostics.Stopwatch.GetTimestamp(),allocated=GC.GetAllocatedBytesForCurrentThread();
            // Orthographic LOD coverage depends on view size, not position or heading.
            // Unity still culls the spatial bounds separately for every camera.
            bool perspectiveMoved = !viewCamera.orthographic && (cachedPosition!=viewCamera.transform.position || cachedRotation!=viewCamera.transform.rotation);
            bool rebuild = !selectionValid || perspectiveMoved
                || cachedSize!=viewCamera.orthographicSize || (!viewCamera.orthographic && cachedFov!=viewCamera.fieldOfView)
                || cachedOrtho!=viewCamera.orthographic || cachedBias!=QualitySettings.lodBias || cachedSimpleShadows!=simplifiedDistantShadows;
            if(rebuild) {
            SelectionRebuilds++;
            stamp++;active.Clear();GeometryUtility.CalculateFrustumPlanes(viewCamera,planes);
            float tangent=2*Mathf.Tan(viewCamera.fieldOfView*.5f*Mathf.Deg2Rad);
            foreach(var tree in trees) {
                float denominator=viewCamera.orthographic?2*viewCamera.orthographicSize:Mathf.Max(.01f,Vector3.Distance(viewCamera.transform.position,tree.reference))*tangent;
                float coverage=tree.size/denominator*QualitySettings.lodBias;
                int selected=-1;for(int i=0;i<tree.levels.Length;i++)if(coverage>=tree.levels[i].threshold){selected=i;break;}
                if(selected<0)continue;
                bool visible=GeometryUtility.TestPlanesAABB(planes,tree.bounds);if(visible)VisibleTrees++;
                foreach(var part in tree.levels[selected].parts) { Queue(tree,part,visible); if(simplifiedDistantShadows&&part.shadows!=ShadowCastingMode.Off)Queue(tree,part,visible,true); }
            }
            cachedPosition=viewCamera.transform.position;cachedRotation=viewCamera.transform.rotation;
            cachedSize=viewCamera.orthographicSize;cachedFov=viewCamera.fieldOfView;cachedOrtho=viewCamera.orthographic;cachedBias=QualitySettings.lodBias;
            cachedVisible=VisibleTrees;cachedSimpleShadows=simplifiedDistantShadows;selectionValid=true;
            } else VisibleTrees=cachedVisible;
            foreach(var group in active) {
                var parameters=new RenderParams(group.material){worldBounds=group.bounds,shadowCastingMode=group.shadows,receiveShadows=true};
                for(int startIndex=0;startIndex<group.count;startIndex+=Capacity) {
                    int count=Mathf.Min(Capacity,group.count-startIndex);
                    Graphics.RenderMeshInstanced(parameters,group.mesh,group.submesh,group.chunks[startIndex/Capacity],count);
                    SubmittedInstances+=count;SubmittedBatches++;
                }
            }
            SubmissionAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            SubmissionMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;
        }
        void OnEnable()
        {
            if(!captured)return;
            // Seasonal hiding also disables Unity's LODGroups. Re-establish the
            // selected backend even if the previous applied flag already agrees;
            // toggling through the other backend forces native LODs to re-register.
            bool useInstancing=instanced&&viewCamera&&viewCamera.orthographicSize>=minimumInstancedViewSize;
            SetOriginals(!useInstancing);SetOriginals(useInstancing);
            selectionValid=false;
        }
        void OnDisable(){if(applied)SetOriginals(false);selectionValid=false;SubmittedInstances=SubmittedBatches=VisibleTrees=0;}
        void OnDestroy(){if(applied)SetOriginals(false);}
    }
}

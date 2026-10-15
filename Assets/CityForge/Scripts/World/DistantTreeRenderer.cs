using System.Collections.Generic;
using CityForge.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World
{
    /// <summary>Zoom 4/5: merge camera-facing tree images into one draw mesh per variant.</summary>
    public sealed class DistantTreeRenderer : MonoBehaviour
    {
        private sealed class Batch { public Mesh mesh; public MeshRenderer renderer; public List<Vector3> vertices=new(); public List<Vector2> uv=new(); public List<int> indices=new(); }
        private readonly Dictionary<Material,Batch> batches=new();
        private readonly Dictionary<Material,Batch> shadowBatches=new();
        private PrototypeRegionView view;
        private CityForgeCameraController cameraControl;
        private Camera camera;
        private readonly List<TreeImpostor> trees=new();
        private int lastCount=-1,lastDistrict=-2;
        private Quaternion lastRotation;
        private bool wasFar;
        private int lastZoom=-1;
        private ForestPresentationSettings settings;
        public int QuadTreeCount {get;private set;}
        public int FarTreeCount {get;private set;}
        public bool IsFar {get;private set;}
        public int BatchCount => batches.Count;
        public int FarTriangleCount => QuadTreeCount*4;

        private void Start()
        {
            view=GetComponent<PrototypeRegionView>();
            settings=ForestPresentationSettings.Load();
            cameraControl=GetComponentInChildren<CityForgeCameraController>();camera=cameraControl.GetComponent<Camera>();
        }
        private void LateUpdate() { Refresh(); }
        public void Refresh()
        {
            if(view==null) return;
            var far=cameraControl.ZoomLevel>=4 && !GetComponent<RegionPreviewDiagnostics>().IsEnabled;
            var changed=lastCount!=view.TreeCount || lastDistrict!=view.SelectedDistrict || lastRotation!=camera.transform.rotation || far!=wasFar || lastZoom!=cameraControl.ZoomLevel;
            if(!changed) return;
            // Atlas shadow proxies have a denser silhouette than layered leaf meshes.
            // Compensate at the representation switch to keep their ground contrast close.
            var sun=transform.Find("RegionSun").GetComponent<Light>();
            sun.shadowStrength=far ? .50f : .64f;
            if(lastCount!=view.TreeCount)
            {
                trees.Clear();trees.AddRange(transform.Find("RandomTrees").GetComponentsInChildren<TreeImpostor>(true));
                if(trees.Count!=view.TreeCount) {Debug.LogError("Bake Distant Trees before testing far tree rendering.");return;}
            }
            foreach(var tree in trees)
            {
                // Fixed game zoom levels select exactly one mesh tier. Disable
                // automatic screen-size selection to avoid overlapping LODs.
                var lod=tree.GetComponent<LODGroup>();lod.enabled=false;
                var levels=lod.GetLODs();
                var selected=settings.MeshTier(cameraControl.ZoomLevel,levels.Length);
                for(var level=0;level<levels.Length;level++)
                    foreach(var renderer in levels[level].renderers) renderer.enabled=!far && level==selected;

            }
            Clear(batches,far);Clear(shadowBatches,far);
            FarTreeCount=0;QuadTreeCount=0;
            if(far)
            {
                var right=camera.transform.right;var up=camera.transform.up;
                foreach(var tree in trees)
                {
                    if(!tree.gameObject.activeSelf) continue;
                    FarTreeCount++;
                    var center=tree.transform.TransformPoint(tree.center);var half=tree.span*tree.transform.lossyScale.y*.5f;
                    AddQuad(GetBatch(batches,tree.material,false),center,half,right,up,camera.transform.eulerAngles.y-tree.transform.eulerAngles.y);
                    var light=transform.Find("RegionSun");
                    AddQuad(GetBatch(shadowBatches,tree.material,true),center,half,light.right,light.up,light.eulerAngles.y-tree.transform.eulerAngles.y);
                    QuadTreeCount++;
                }
                Upload(batches);Upload(shadowBatches);
            }
            IsFar=far;wasFar=far;lastCount=view.TreeCount;lastDistrict=view.SelectedDistrict;lastRotation=camera.transform.rotation;lastZoom=cameraControl.ZoomLevel;
        }
        private static void Clear(Dictionary<Material,Batch> source,bool visible)
        {foreach(var batch in source.Values) {batch.renderer.enabled=visible;batch.vertices.Clear();batch.uv.Clear();batch.indices.Clear();}}
        private Batch GetBatch(Dictionary<Material,Batch> source,Material material,bool shadow)
        {
            if(source.TryGetValue(material,out var batch)) return batch;
            var root=new GameObject((shadow ? "Distant tree shadows / " : "Distant trees / ")+material.name);root.transform.SetParent(transform,false);
            batch=new Batch {mesh=new Mesh {name="Batched tree silhouettes",indexFormat=IndexFormat.UInt32},renderer=root.AddComponent<MeshRenderer>()};
            root.AddComponent<MeshFilter>().sharedMesh=batch.mesh;batch.renderer.sharedMaterial=material;
            batch.renderer.shadowCastingMode=shadow ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.Off;
            source.Add(material,batch);return batch;
        }
        private static void AddQuad(Batch batch,Vector3 center,float half,Vector3 right,Vector3 up,float azimuth)
        {
            var index=batch.vertices.Count;
            batch.vertices.Add(center-right*half-up*half);batch.vertices.Add(center+right*half-up*half);
            batch.vertices.Add(center+right*half+up*half);batch.vertices.Add(center-right*half+up*half);
            var angle=Mathf.RoundToInt(Mathf.Repeat(azimuth,360)/45)%8;
            var u0=angle/8f;var u1=(angle+1)/8f;
            batch.uv.Add(new Vector2(u0,0));batch.uv.Add(new Vector2(u1,0));batch.uv.Add(new Vector2(u1,1));batch.uv.Add(new Vector2(u0,1));
            batch.indices.AddRange(new[]{index,index+2,index+1,index,index+3,index+2});
        }
        private static void Upload(Dictionary<Material,Batch> source)
        {
            foreach(var batch in source.Values)
            {batch.mesh.Clear();batch.mesh.SetVertices(batch.vertices);batch.mesh.SetUVs(0,batch.uv);batch.mesh.SetTriangles(batch.indices,0);batch.mesh.RecalculateBounds();}
        }
        private void OnDestroy()
        {foreach(var batch in batches.Values) Destroy(batch.mesh);foreach(var batch in shadowBatches.Values) Destroy(batch.mesh);}
    }
}

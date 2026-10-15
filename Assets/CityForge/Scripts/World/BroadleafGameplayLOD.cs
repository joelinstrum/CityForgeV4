using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using CityForge.CameraSystem;
namespace CityForge.World
{
 /// <summary>Discrete game zoom tiers. Shared derived meshes, single far batch, original source retained for controls.</summary>
 public sealed class BroadleafGameplayLOD:MonoBehaviour
 {
  sealed class DrawBatch{public Mesh mesh;public Material material;public int submesh,count;public Matrix4x4[] matrices=new Matrix4x4[1023];}
  sealed class Part{public DrawBatch batch;public Matrix4x4 matrix;}
  readonly Dictionary<(Mesh,Material,int),DrawBatch> drawBatches=new();Bounds forestBounds;
  sealed class Tree{public Transform root;public Renderer[] source;public Renderer[][] tiers;public int[] triangles;public Bounds bounds;public bool visible=true;public List<Part>[] parts=new[]{new List<Part>(),new List<Part>(),new List<Part>()};}
  readonly Dictionary<Material,Material> runtimeMaterials=new();readonly List<Tree> trees=new();readonly List<GameObject> derived=new();
  BroadleafGameplayLODLibrary library;CityForgeCameraController rig;Camera camera;Mesh batchMesh;MeshRenderer batch;
  BroadleafCanopyCluster cluster;Vector3 lastCameraPosition;float lastCameraSize;bool useLOD=true;int lastLevel=-1;Quaternion lastRotation;
  public bool ShowPreviewControls;
  void OnGUI(){if(!ShowPreviewControls || rig==null)return;GUI.Box(new Rect(24,Screen.height-110,320,80),"FOREST PREVIEW · "+trees.Count+" trees");GUI.Label(new Rect(36,Screen.height-82,155,25),"Zoom "+rig.ZoomLevel+" / 5");if(GUI.Button(new Rect(210,Screen.height-83,50,30),"+"))rig.SetZoomLevel(rig.ZoomLevel-1);if(GUI.Button(new Rect(270,Screen.height-83,50,30),"−"))rig.SetZoomLevel(rig.ZoomLevel+1);}
  public double LastSwitchMs{get;private set;}public int ActiveTriangles{get;private set;}
  public void Initialize(CityForgeCameraController cameraRig,BroadleafGameplayLODLibrary data,BroadleafCanopyCluster clusterData)
  {
   rig=cameraRig;camera=rig.GetComponent<Camera>();library=data;cluster=clusterData;
   var roots=new List<Transform>();foreach(Transform root in transform)roots.Add(root);
   foreach(var root in roots)
   {
    var t=new Tree{root=root,source=root.GetComponentsInChildren<Renderer>(),tiers=new Renderer[3][],triangles=new int[3]};
    t.bounds=t.source[0].bounds;foreach(var r in t.source)t.bounds.Encapsulate(r.bounds);t.bounds.Expand(20);
    for(int i=0;i<3;i++){var go=Instantiate(library.meshTiers[i],root);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one;go.name="GameplayLOD"+i;t.tiers[i]=go.GetComponentsInChildren<Renderer>();derived.Add(go);foreach(var r in t.tiers[i])t.triangles[i]+=r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;}
    foreach(var rs in t.tiers)foreach(var r in rs){var slots=r.sharedMaterials;for(int j=0;j<slots.Length;j++){var original=slots[j];if(!runtimeMaterials.TryGetValue(original,out var m)){m=new Material(original);m.enableInstancing=true;if(!library.honorAuthoredBarkMaterial && m.shader.name=="Universal Render Pipeline/Lit" && original.name.IndexOf("Bark",System.StringComparison.OrdinalIgnoreCase)>=0){m.SetColor("_BaseColor",new Color(.26f,.235f,.205f,1).gamma);m.SetColor("_EmissionColor",new Color(.10f,.092f,.082f,1));m.EnableKeyword("_EMISSION");m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;}if(library.honorAuthoredBarkMaterial && m.shader.name=="Universal Render Pipeline/Lit" && m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent>0){m.EnableKeyword("_EMISSION");m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;}runtimeMaterials.Add(original,m);}slots[j]=m;}r.sharedMaterials=slots;}
    for(int i=0;i<3;i++)foreach(var r in t.tiers[i]){var mesh=r.GetComponent<MeshFilter>().sharedMesh;var mats=r.sharedMaterials;for(int sub=0;sub<mesh.subMeshCount;sub++){var key=(mesh,mats[sub],sub);if(!drawBatches.TryGetValue(key,out var b)){b=new DrawBatch{mesh=mesh,material=mats[sub],submesh=sub,matrices=new Matrix4x4[Mathf.Max(1023,roots.Count)]};drawBatches.Add(key,b);}t.parts[i].Add(new Part{batch=b,matrix=r.transform.localToWorldMatrix});}}
    if(trees.Count==0)forestBounds=t.bounds;else forestBounds.Encapsulate(t.bounds);trees.Add(t);
   }
   if(cluster!=null && (trees.Count!=320 || Vector3.Distance(forestBounds.center,cluster.center)>.10f)){UnityEngine.Debug.LogWarning("Cluster bake does not match this stand; using per-tree far impostors until it is rebaked.");cluster=null;}
   var holder=new GameObject("Batched canopy LOD3–4");holder.transform.SetParent(transform,false);derived.Add(holder);batch=holder.AddComponent<MeshRenderer>();batch.sharedMaterial=library.impostorMaterial;batch.shadowCastingMode=ShadowCastingMode.Off;batch.receiveShadows=false;batchMesh=new Mesh{name="Broadleaf canopy batch",indexFormat=IndexFormat.UInt32};holder.AddComponent<MeshFilter>().sharedMesh=batchMesh;
   rig.ZoomChanged+=Apply;Apply(rig.ZoomLevel);
  }
  public void RefreshPresentation()=>Apply(rig.ZoomLevel);
  public void SetLODEnabled(bool enabled){useLOD=enabled;Apply(rig.ZoomLevel);}
  void LateUpdate(){if(!useLOD)return;if(rig.ZoomLevel>=4){if(camera.transform.rotation!=lastRotation)RebuildCanopy();}else{if(camera.transform.position!=lastCameraPosition || camera.orthographicSize!=lastCameraSize)Apply(rig.ZoomLevel);RenderInstances();}}
  public void RenderInstances(){if(!useLOD || rig.ZoomLevel>=4)return;foreach(var b in drawBatches.Values)if(b.count>0){var rp=new RenderParams(b.material){camera=camera,worldBounds=forestBounds,shadowCastingMode=ShadowCastingMode.TwoSided,receiveShadows=true};for(int start=0;start<b.count;start+=1023)Graphics.RenderMeshInstanced(rp,b.mesh,b.submesh,b.matrices,Mathf.Min(1023,b.count-start),start);}}


  void Apply(int level)
  {
   var watch=Stopwatch.StartNew();int tier=Mathf.Clamp(level-1,0,2);ActiveTriangles=0;
   foreach(var b in drawBatches.Values)b.count=0;
   var planes=GeometryUtility.CalculateFrustumPlanes(camera);lastCameraPosition=camera.transform.position;lastCameraSize=camera.orthographicSize;
   foreach(var tree in trees)
   {
    tree.visible=GeometryUtility.TestPlanesAABB(planes,tree.bounds);
    foreach(var r in tree.source)r.enabled=!useLOD;
    for(int i=0;i<3;i++)foreach(var r in tree.tiers[i]){r.enabled=false;}
    if(useLOD && level<=3 && tree.visible){ActiveTriangles+=tree.triangles[tier];foreach(var part in tree.parts[tier])part.batch.matrices[part.batch.count++]=part.matrix;}
   }
   batch.enabled=useLOD && level>=4;
   if(batch.enabled){if(lastRotation!=camera.transform.rotation || lastLevel!=level)RebuildCanopy();ActiveTriangles=level==5 && cluster!=null?2:trees.Count*2;}
   lastLevel=level;watch.Stop();LastSwitchMs=watch.Elapsed.TotalMilliseconds;
  }
  void RebuildCanopy()
  {
   var verts=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();var right=camera.transform.right;var up=camera.transform.up;
   int view=Mathf.RoundToInt(Mathf.Repeat(camera.transform.eulerAngles.y-45,360)/90)%4;
   if(rig.ZoomLevel==5 && cluster!=null)
   {
    batch.sharedMaterial=cluster.material;var center=cluster.center-camera.transform.forward*100;float half=cluster.span*.5f;
    verts.Add(transform.InverseTransformPoint(center-right*half-up*half));verts.Add(transform.InverseTransformPoint(center+right*half-up*half));verts.Add(transform.InverseTransformPoint(center+right*half+up*half));verts.Add(transform.InverseTransformPoint(center-right*half+up*half));float u0=view/4f,u1=(view+1)/4f;uv.Add(new Vector2(u0,0));uv.Add(new Vector2(u1,0));uv.Add(new Vector2(u1,1));uv.Add(new Vector2(u0,1));indices.AddRange(new[]{0,2,1,0,3,2});
   }
   else {batch.sharedMaterial=library.impostorMaterial;foreach(var tree in trees)
   {
    var center=tree.root.TransformPoint(library.center);float half=library.span*tree.root.lossyScale.y*.5f;int n=verts.Count;
    verts.Add(transform.InverseTransformPoint(center-right*half-up*half));verts.Add(transform.InverseTransformPoint(center+right*half-up*half));verts.Add(transform.InverseTransformPoint(center+right*half+up*half));verts.Add(transform.InverseTransformPoint(center-right*half+up*half));
    int angle=Mathf.RoundToInt(Mathf.Repeat(tree.root.eulerAngles.y,360)/45)%8;float u0=angle/8f,u1=(angle+1)/8f;float v0=view/4f,v1=(view+1)/4f;
    uv.Add(new Vector2(u0,v0));uv.Add(new Vector2(u1,v0));uv.Add(new Vector2(u1,v1));uv.Add(new Vector2(u0,v1));indices.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
   }
   }
   batchMesh.Clear();batchMesh.SetVertices(verts);batchMesh.SetUVs(0,uv);batchMesh.SetTriangles(indices,0);batchMesh.RecalculateBounds();lastRotation=camera.transform.rotation;
  }
  void OnDestroy(){if(rig!=null)rig.ZoomChanged-=Apply;foreach(var tree in trees)foreach(var r in tree.source)if(r!=null)r.enabled=true;foreach(var go in derived)if(go!=null)Destroy(go);if(batchMesh!=null)Destroy(batchMesh);foreach(var m in runtimeMaterials.Values)if(m!=null)Destroy(m);}
 }
}

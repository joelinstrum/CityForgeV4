using System;using System.Collections.Generic;using UnityEngine;
namespace CityForge.World {
/// <summary>Local bluegill stocking trial: looping rig swims and conservative, checked open-water paths.</summary>
public sealed class StreamFishStudy:MonoBehaviour {
 sealed class Fish {public Transform root;public SkinnedMeshRenderer body;public Vector2 home,target,start;public float speed,wait,swimDepth;}
 readonly List<Fish> fish=new();MeadowDistrictLandscape land;StreamRockStudy clusters;IndividualStreamRockScatter stones;System.Random random;bool enlarged;CityForge.CameraSystem.CityForgeCameraController cameraRig;
 public bool FishVisible {get;private set;}
 public int ActiveFishCount {get {int n=0;foreach(var f in fish)if(f.root.gameObject.activeInHierarchy)n++;return n;}}
 public int OnscreenFishCount {get {int n=0;foreach(var f in fish)if(f.root.gameObject.activeInHierarchy&&f.body.isVisible)n++;return n;}}
 public int FishCount=>fish.Count;public int MovedCount {get {int count=0;foreach(var f in fish)if(Vector2.Distance(f.start,new Vector2(f.root.position.x,f.root.position.z))>.08f)count++;return count;}}
 public int InvalidCount {get {int n=0;foreach(var f in fish)if(!Suitable(new Vector2(f.root.position.x,f.root.position.z)))n++;return n;}}
 public float MinimumSurfaceClearance {get {float v=100;foreach(var f in fish)v=Mathf.Min(v,12-f.root.position.y);return v;}}
 float R()=>(float)random.NextDouble();
 public void Initialize(MeadowDistrictLandscape source){
  land=source;enlarged=StreamRockTestRegion.ReadableFish(source.View);clusters=land.GetComponent<StreamRockStudy>();stones=land.GetComponent<IndividualStreamRockScatter>();random=new System.Random(land.View.Data.Seed^164031);
  var prefab=Resources.Load<GameObject>("StreamFish/BluegillFish");if(!prefab)throw new InvalidOperationException("Prepare private bluegill prefab first.");
  foreach(float z in new[]{95f,165f,235f,305f,375f,435f})for(int i=0;i<8;i++)for(int attempt=0;attempt<300;attempt++){
   float zz=z+(R()-.5f)*24;var p=new Vector2(StreamRockTestRegion.Center(zz)+(R()*2-1)*StreamRockTestRegion.HalfWidth(zz)*.48f,zz);
   if(!Suitable(p)||fish.Exists(f=>Vector2.Distance(f.home,p)<.4f))continue;
   var go=Instantiate(prefab,transform);go.name="Bluegill "+(fish.Count+1);float length=Mathf.Lerp(.24f,.32f,R())*(enlarged?1.5f:1);float swimDepth=enlarged?Mathf.Max(.18f,length*.42f+.035f):.24f;go.transform.localScale=Vector3.one*length;go.transform.position=new Vector3(p.x,SwimHeight(p,swimDepth),p.y);go.transform.rotation=Quaternion.Euler(0,R()*360,0);
   var animator=go.GetComponentInChildren<Animator>();animator.cullingMode=AnimatorCullingMode.CullCompletely;animator.speed=Mathf.Lerp(.8f,1.2f,R());animator.Play(0,0,R());
   fish.Add(new Fish{root=go.transform,body=go.GetComponentInChildren<SkinnedMeshRenderer>(),home=p,target=p,start=p,speed=Mathf.Lerp(.13f,.25f,R()),wait=R(),swimDepth=swimDepth});break;
  }
  cameraRig=land.View.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>();
  if(!cameraRig)throw new InvalidOperationException("Fish visibility requires district camera.");
  cameraRig.ZoomChanged+=ZoomChanged;ZoomChanged(cameraRig.ZoomLevel);
 }
 void ZoomChanged(int level){FishVisible=level==0;foreach(var f in fish)f.root.gameObject.SetActive(FishVisible);enabled=FishVisible;}
 public void Shutdown(){if(cameraRig)cameraRig.ZoomChanged-=ZoomChanged;cameraRig=null;enabled=false;FishVisible=false;foreach(var f in fish)if(f.root){f.root.gameObject.SetActive(false);Destroy(f.root.gameObject);}fish.Clear();}
 void OnDestroy(){Shutdown();}
 public bool Suitable(Vector2 p){return land&&land.Contains(p)&&12-land.Height(p)>.44f&&Mathf.Abs(p.x-StreamRockTestRegion.Center(p.y))<StreamRockTestRegion.HalfWidth(p.y)*.72f&&!clusters.OverlapsCluster(p,enlarged?.39f:.26f)&&stones.BedShelf(p)<.05f;}
 float SwimHeight(Vector2 p,float depth)=>Mathf.Max(12-depth,land.Height(p)+.16f);
 bool ClearPath(Vector2 a,Vector2 b){int n=Mathf.CeilToInt(Vector2.Distance(a,b)/.12f);for(int i=0;i<=n;i++)if(!Suitable(Vector2.Lerp(a,b,i/(float)Mathf.Max(1,n))))return false;return true;}
 void Update(){
  if(!land)return;float dt=Mathf.Min(Time.deltaTime,.1f);
  foreach(var f in fish){if(!f.body.isVisible)continue;var p=new Vector2(f.root.position.x,f.root.position.z);
   if(Vector2.Distance(p,f.target)<.06f){f.wait-=dt;if(f.wait>0)continue;f.wait=1+R()*2;for(int i=0;i<25;i++){var target=f.home+new Vector2((R()*2-1)*1.3f,(R()*2-1)*2);if(Vector2.Distance(p,target)>.2f&&ClearPath(p,target)){f.target=target;break;}}}
   var next=Vector2.MoveTowards(p,f.target,f.speed*dt);if(!ClearPath(p,next)){f.target=p;continue;}
   var direction=new Vector3(f.target.x-p.x,0,f.target.y-p.y);if(direction.sqrMagnitude>.0001f)f.root.rotation=Quaternion.Slerp(f.root.rotation,Quaternion.LookRotation(direction),dt*4);
   f.root.position=new Vector3(next.x,SwimHeight(next,f.swimDepth),next.y);
  }
 }
}
}

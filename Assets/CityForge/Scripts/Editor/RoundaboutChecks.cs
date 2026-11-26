#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;
using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.UI;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
using CityForge.World;using CityForge.UI;using CityForge.Lots;
public static class RoundaboutChecks {
 static readonly string Out=Path.GetFullPath("Library/RoadShapeChecks")+Path.DirectorySeparatorChar;
 static readonly BindingFlags I=BindingFlags.Instance|BindingFlags.NonPublic, S=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 static List<string> checks=new();
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add("PASS "+label);}
 static Vector2[] Guide(Vector2 center,int width,bool big)=>(Vector2[])typeof(PrototypeRegionView).GetMethod("RoundaboutGuide",S).Invoke(null,new object[]{center,1024f,width,big});
 class Surface:PointWireSurface {public bool Edit=true;public Collider Ground;public override bool Available=>true;public override bool CanEdit=>Edit;public override Collider Collider=>Ground;public override bool Contains(Vector2 p)=>Mathf.Abs(p.x)<200&&Mathf.Abs(p.y)<200;public override float Height(Vector2 p)=>0;}
 public static void Run(){Directory.CreateDirectory(Out);try{
 File.Delete(Out+"Roundabout-Error.txt");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
 foreach(int width in new[]{3,4,12,20})foreach(bool big in new[]{false,true}){
 var guide=Guide(new Vector2(1.1f,1.8f),width,big);float radius=(float)typeof(PrototypeRegionView).GetMethod("RoundaboutRadius",S).Invoke(null,new object[]{width,big});
 Check(guide.Length==48&&guide.All(p=>Mathf.Abs(Vector2.Distance(p,new Vector2(2,2))-radius)<.001f),$"Circular grid-centred guide width{width} large{big}");
 Check(guide.Zip(Guide(new Vector2(1.9f,1.1f),width,big),(a,b)=>a==b).All(x=>x),"Stable within a grid cell");
 Check(typeof(PrototypeRegionView).GetMethod("RoadGuideProblem",S).Invoke(null,new object[]{guide,true,512f})==null,"Saved road guide constraints");
 var record=new DistrictRoadRecord{district=0,surfaceId="dirt",points=guide,width=width,closed=true,curved=true,gridAligned=false};
 var saved=JsonUtility.FromJson<DistrictRoadRecord>(JsonUtility.ToJson(record));
 if(width!=20) typeof(PrototypeRegionView).GetMethod("ValidateDistrictRoads",S).Invoke(null,new object[]{new[]{saved},64,1024f});
 Check(saved.closed&&saved.curved&&!saved.gridAligned&&saved.points.SequenceEqual(guide),"Native road serialization retains circle and empty centre");
 var mesh=Mesh(guide,width);Check(mesh.vertexCount>0&&mesh.vertices.All(p=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)),"Native finite road ribbon");
 Check(mesh.vertices.All(p=>new Vector2(p.x-2,p.z-2).magnitude>radius-width*.54f),"No paving across island");UnityEngine.Object.DestroyImmediate(mesh);
 }
 var assembly=typeof(PrototypeRegionView).Assembly;var circle=new DistrictRoadRecord{points=Guide(Vector2.zero,4,false),width=4,surfaceId="weathered-concrete-street",closed=true,curved=true};
 var east=new DistrictRoadRecord{points=new[]{circle.points[0],circle.points[0]+Vector2.right*30},width=4,surfaceId=circle.surfaceId};var west=new DistrictRoadRecord{points=new[]{circle.points[24],circle.points[24]-Vector2.right*30},width=4,surfaceId=circle.surfaceId};
 circle.GetType().GetField("junctionCorners",I).SetValue(circle,assembly.GetType("CityForge.World.ConnectedRoadCorners").GetMethod("Find",S).Invoke(null,new object[]{circle,new[]{circle,east,west}}));
 var line=(IReadOnlyList<Vector2>)typeof(PrototypeRegionView).GetMethod("RoadCenterline",S).Invoke(null,new object[]{circle});
 object[] args={new[]{(line.ToArray(),true),(east.points,false),(west.points,false)},east.points[1],west.points[1],.1f,.1f,null,null};
 Check((bool)assembly.GetType("CityForge.World.FreightRoadGraph").GetMethod("TryRoute",S).Invoke(null,args),"Native transport graph connects approach roads through ring");
 Check(((Vector2[])args[5]).All(p=>Vector2.Distance(p,new Vector2(2,2))>11.8f),"Transport route follows ring without cutting island");
 var host=new GameObject("Stamp interaction fixture");var wire=host.AddComponent<PointWireTool>();
 var camera=new GameObject("Camera",typeof(Camera)).GetComponent<Camera>();var canvas=new GameObject("HUD",typeof(Canvas),typeof(CanvasScaler)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
 var ground=new GameObject("Ground",typeof(BoxCollider)).GetComponent<BoxCollider>();ground.enabled=false;
 var surface=new Surface{Ground=ground};wire.Initialize(camera,canvas.transform,surface,Color.cyan);wire.DrawingHint="Roundabout";
 int commits=0;bool reject=false;wire.StampCompletionRequested=p=>{commits++;return reject?"Blocked site":null;};
 var place=typeof(PointWireTool).GetMethod("TryPlaceStamp",I);Func<Vector2[],bool> stamp=p=>(bool)place.Invoke(wire,new object[]{p});var valid=Guide(Vector2.zero,4,false);
 Check(!stamp(valid)&&commits==0,"Inactive tool never commits");wire.Arm();Check(ground.enabled,"Arm acquires disabled terrain collider");
 Check(stamp(valid)&&stamp(valid)&&commits==2&&wire.Armed&&wire.Points.Count==0,"Repeated stamps do not create freehand markers");
 reject=true;Check(!stamp(valid)&&commits==3&&wire.Armed,"Rejected commit retains stamp");
 Check(!stamp(Guide(new Vector2(198,0),4,false))&&commits==3,"Whole guide containment blocks district edge");
 valid[0]=new Vector2(float.NaN,0);Check(!stamp(valid)&&commits==3,"Nonfinite shape rejected");
 surface.Edit=false;Check(!stamp(Guide(Vector2.zero,4,false))&&commits==3,"Consumer save/loading eligibility blocks mutation");surface.Edit=true;
 wire.Cancel();Check(!wire.Armed&&!ground.enabled&&!stamp(Guide(Vector2.zero,4,false)),"Cancel restores collider and disables stamps");
 // Test the actual road adapter's mode transitions, not a duplicate shortcut model.
 var view=host.AddComponent<PrototypeRegionView>();view.enabled=false;typeof(PrototypeRegionView).GetField("roadWire",I).SetValue(view,wire);
 var button=new GameObject("Finish",typeof(RectTransform),typeof(Button));var text=new GameObject("Text",typeof(RectTransform),typeof(Text));text.transform.SetParent(button.transform);typeof(PrototypeRegionView).GetField("finishRoadButton",I).SetValue(view,button.GetComponent<Button>());
 typeof(PrototypeRegionView).GetMethod("SetRoundaboutStamp",I).Invoke(view,new object[]{true});Check(wire.StampPaths!=null&&wire.StampPathsCompletionRequested!=null&&wire.Points.Count==0,"R mode installs actual adapter callbacks");
 typeof(PrototypeRegionView).GetMethod("SetRoundaboutStamp",I).Invoke(view,new object[]{false});Check(wire.StampPaths==null&&wire.StampPathsCompletionRequested==null&&wire.Armed,"R/Escape returns to ordinary armed road drawing");wire.Cancel();
 Render();File.WriteAllLines(Out+"Roundabout-Checks.txt",checks);EditorApplication.Exit(0);
 }catch(Exception e){File.WriteAllText(Out+"Roundabout-Error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
 static Mesh Mesh(Vector2[] points,int width)=>(Mesh)typeof(LotPathDefinition).Assembly.GetType("CityForge.Lots.LotPathSurface").GetMethod("BuildMesh",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{new LotPathDefinition{points=points,width=width,closed=true,curved=true},new Vector2(256,256),.1f,null,0,0f,null});
 static void Render(){
 var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetColor("_BaseColor",new Color(.55f,.60f,.65f));
 foreach(var pair in new[]{(new Vector2(-34,0),false),(new Vector2(36,0),true)}){var obj=new GameObject(pair.Item2?"Large roundabout":"Small roundabout",typeof(MeshFilter),typeof(MeshRenderer));obj.GetComponent<MeshFilter>().sharedMesh=Mesh(Guide(pair.Item1,4,pair.Item2),4);obj.GetComponent<MeshRenderer>().sharedMaterial=mat;}
 var grid=new Material(mat);grid.SetColor("_BaseColor",new Color(.12f,.20f,.22f));for(int x=-76;x<=80;x+=4){Line(new Vector3(x,0,-40),new Vector3(x,0,44));Line(new Vector3(-76,0,x),new Vector3(80,0,x));}
 var cam=new GameObject("Native roundabout geometry",typeof(Camera)).GetComponent<Camera>();cam.gameObject.AddComponent<UniversalAdditionalCameraData>();cam.orthographic=true;cam.orthographicSize=44;cam.transform.position=new Vector3(2,200,2);cam.transform.rotation=Quaternion.Euler(90,0,0);cam.farClipPlane=400;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.035f,.045f,.05f);
 var rt=new RenderTexture(1600,900,24);rt.Create();cam.targetTexture=rt;RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Out+"Native-Roundabouts.png",tex.EncodeToPNG());
 foreach(var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))mr.enabled=false;
 var hud=new GameObject("Ghost proof HUD",typeof(Canvas)).GetComponent<Canvas>();hud.renderMode=RenderMode.ScreenSpaceCamera;hud.worldCamera=cam;hud.planeDistance=1;
 Canvas.ForceUpdateCanvases();
 foreach(var pair in new[]{(new Vector2(-34,0),false),(new Vector2(36,0),true)}){
 var obj=new GameObject("Actual PointWire translucent stamp");var tool=obj.AddComponent<PointWireTool>();var ground=obj.AddComponent<BoxCollider>();tool.Initialize(cam,hud.transform,new Surface{Ground=ground},Color.cyan);tool.RibbonWidth=4;tool.RibbonFillColor=new Color(.5f,.52f,.55f);tool.Arm();
 var preview=(List<Vector3>)typeof(PointWireTool).GetField("preview",I).GetValue(tool);typeof(PointWireTool).GetMethod("BuildGuide",I).Invoke(tool,new object[]{preview,Guide(pair.Item1,4,pair.Item2),true});typeof(PointWireTool).GetMethod("LateUpdate",I).Invoke(tool,null);
 }
 Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Out+"Translucent-Stamp-Preview.png",tex.EncodeToPNG());
 RenderTexture.active=null;rt.Release();
 void Line(Vector3 a,Vector3 b){var l=new GameObject("4m grid",typeof(LineRenderer)).GetComponent<LineRenderer>();l.sharedMaterial=grid;l.startWidth=l.endWidth=.1f;l.positionCount=2;l.SetPositions(new[]{a,b});}
 }
}
#endif

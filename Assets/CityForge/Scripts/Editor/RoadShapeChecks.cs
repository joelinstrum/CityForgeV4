#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;
using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.UI;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
using CityForge.World;using CityForge.UI;using CityForge.Lots;
public static class RoadShapeChecks {
 static readonly string Out=Path.GetFullPath("Library/RoadShapeChecks")+Path.DirectorySeparatorChar;
 static readonly BindingFlags I=BindingFlags.Instance|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 static readonly Type Shape=typeof(PrototypeRegionView).GetNestedType("RoadStampShape",BindingFlags.NonPublic);
 static List<string> checks=new();
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add("PASS "+label);}
 static PointWireStampPath[] Guides(string shape,Vector2 center,int width,bool big,int turns)=>(PointWireStampPath[])typeof(PrototypeRegionView).GetMethod("RoadShapeGuides",S).Invoke(null,new object[]{Enum.Parse(Shape,shape),center,1024f,width,big,turns});
 class Surface:PointWireSurface {public bool Edit=true;public Collider Ground;public override bool Available=>true;public override bool CanEdit=>Edit;public override Collider Collider=>Ground;public override bool Contains(Vector2 p)=>Mathf.Abs(p.x)<200&&Mathf.Abs(p.y)<200;public override float Height(Vector2 p)=>0;}
 static string[] Names={"Roundabout","D","T","X","S","Intersection"};
 public static void Run(){Directory.CreateDirectory(Out);File.Delete(Out+"Shape-Error.txt");try{
 EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
 foreach(string name in Names)foreach(int width in new[]{3,4,12,20})foreach(bool big in new[]{false,true})foreach(int turns in new[]{0,1,2,3}){
 var paths=Guides(name,new Vector2(1,1),width,big,turns);string tag=$"{name} width{width} large{big} turn{turns}";
 Check(paths.Length==(name=="T"||name=="X"||name=="Intersection"?2:1),tag+" independent path count");
 Check(paths.All(p=>p.Closed==(name=="Roundabout"||name=="D")),tag+" no accidental closure of open shapes");
 foreach(var path in paths){Check(typeof(PrototypeRegionView).GetMethod("RoadGuideProblem",S).Invoke(null,new object[]{path.Points,path.Closed,512f})==null,tag+" valid bounded guide");
 var record=new DistrictRoadRecord{district=0,surfaceId="dirt",points=path.Points.ToArray(),width=width,closed=path.Closed,curved=name=="Roundabout"||name=="D",gridAligned=false};
 var saved=JsonUtility.FromJson<DistrictRoadRecord>(JsonUtility.ToJson(record));if(width!=20) typeof(PrototypeRegionView).GetMethod("ValidateDistrictRoads",S).Invoke(null,new object[]{new[]{saved},64,1024f});Check(saved.points.SequenceEqual(record.points)&&saved.closed==record.closed,tag+" native persistence");
 var mesh=Mesh(path,width,record.curved);Check(mesh.vertexCount>0&&mesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),tag+" finite native ribbon");UnityEngine.Object.DestroyImmediate(mesh);
 }
 var next=Guides(name,new Vector2(1.9f,1.1f),width,big,turns);Check(paths.Zip(next,(a,b)=>a.Points.SequenceEqual(b.Points)).All(v=>v),tag+" stable grid centre");
 }
 var s=Guides("S",Vector2.zero,4,false,0)[0].Points;Check(s.Zip(s.Skip(1),(a,b)=>b.x>a.x).All(v=>v),"S is monotonic with no hairpin");Check(Mathf.Abs((s[1]-s[0]).normalized.y)<.05f&&Mathf.Abs((s[^1]-s[^2]).normalized.y)<.05f,"S end tangents connect gently to straight roads");
 foreach(string name in new[]{"T","X","Intersection"}){
 var paths=Guides(name,Vector2.zero,4,false,0);var roads=paths.Select(p=>(p.Points.ToArray(),p.Closed)).ToArray();
 var endpoints=paths.SelectMany(p=>new[]{p.Points[0],p.Points[^1]}).Distinct().ToArray();foreach(var from in endpoints)foreach(var to in endpoints.Where(v=>v!=from)){
 object[] args={roads,from,to,.1f,.1f,null,null};Check((bool)typeof(PrototypeRegionView).Assembly.GetType("CityForge.World.FreightRoadGraph").GetMethod("TryRoute",S).Invoke(null,args),name+" native connectivity every arm pair");}
 }
 var host=new GameObject("Composite stamp fixture");var wire=host.AddComponent<PointWireTool>();var camera=new GameObject("Camera",typeof(Camera)).GetComponent<Camera>();var canvas=new GameObject("HUD",typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var collider=host.AddComponent<BoxCollider>();collider.enabled=false;var surface=new Surface{Ground=collider};wire.Initialize(camera,canvas.transform,surface,Color.cyan);int commits=0;bool reject=false;
 wire.StampPathsCompletionRequested=p=>{commits++;return reject?"Blocked arm":null;};var place=typeof(PointWireTool).GetMethod("TryPlaceStampPaths",I);Func<PointWireStampPath[],bool> stamp=p=>(bool)place.Invoke(wire,new object[]{p});var valid=Guides("T",Vector2.zero,4,false,0);
 Check(!stamp(valid)&&commits==0,"Inactive multi-path tool never commits");wire.Arm();Check(stamp(valid)&&stamp(valid)&&commits==2&&wire.Points.Count==0,"Repeat entire junction once per click without freehand points");
 Check(!stamp(new[]{valid[0],new PointWireStampPath(new[]{Vector2.zero,new Vector2(201,0)},false)})&&commits==2,"Invalid second arm rejects entire shape before callback");
 Check(!stamp(new[]{new PointWireStampPath(new[]{Vector2.zero,Vector2.one},true)})&&commits==2,"Closed path minimum differs from open path minimum");reject=true;Check(!stamp(valid)&&commits==3&&wire.Armed,"Domain rejection retains multi-path preview");surface.Edit=false;Check(!stamp(valid)&&commits==3,"Consumer save/loading guard blocks shape");surface.Edit=true;
 wire.Cancel();Check(!wire.Armed&&!collider.enabled,"Cancel restores collider and stamp input ownership");
 var view=host.AddComponent<PrototypeRegionView>();view.enabled=false;typeof(PrototypeRegionView).GetField("roadWire",I).SetValue(view,wire);var button=new GameObject("Finish",typeof(RectTransform),typeof(Button));var text=new GameObject("Text",typeof(RectTransform),typeof(Text));text.transform.SetParent(button.transform);typeof(PrototypeRegionView).GetField("finishRoadButton",I).SetValue(view,button.GetComponent<Button>());
 foreach(string name in Names){typeof(PrototypeRegionView).GetMethod("SetRoadStamp",I).Invoke(view,new object[]{Enum.Parse(Shape,name)});Check(wire.StampPaths!=null&&wire.StampPathsCompletionRequested!=null&&wire.StampGuide==null&&wire.Points.Count==0,"Actual adapter switches cleanly to "+name);}
 typeof(PrototypeRegionView).GetMethod("SetRoadStamp",I).Invoke(view,new object[]{Enum.Parse(Shape,"None")});Check(wire.StampPaths==null&&wire.StampPathsCompletionRequested==null&&wire.Armed,"Escape/same-key returns to ordinary drawing");wire.Cancel();
 Render();File.WriteAllLines(Out+"Shape-Checks.txt",checks);EditorApplication.Exit(0);
 }catch(Exception e){File.WriteAllText(Out+"Shape-Error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
 static Mesh Mesh(PointWireStampPath path,int width,bool curved)=>(Mesh)typeof(LotPathDefinition).Assembly.GetType("CityForge.Lots.LotPathSurface").GetMethod("BuildMesh",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{new LotPathDefinition{points=path.Points.ToArray(),width=width,closed=path.Closed,curved=curved},new Vector2(256,256),.1f,null,0,0f,null});
 static void Render(){
 var cam=new GameObject("Native stamp preview",typeof(Camera)).GetComponent<Camera>();cam.gameObject.AddComponent<UniversalAdditionalCameraData>();cam.orthographic=true;cam.orthographicSize=85;cam.transform.position=new Vector3(2,300,2);cam.transform.rotation=Quaternion.Euler(90,0,0);cam.farClipPlane=500;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.035f,.045f,.05f);var rt=new RenderTexture(1800,1200,24);rt.Create();cam.targetTexture=rt;
 var grid=new Material(Shader.Find("Universal Render Pipeline/Unlit"));grid.SetColor("_BaseColor",new Color(.12f,.20f,.22f));for(int x=-128;x<=128;x+=4){Line(new Vector3(x,0,-84),new Vector3(x,0,84));Line(new Vector3(-128,0,x),new Vector3(128,0,x));}
 var hud=new GameObject("Ghost proof HUD",typeof(Canvas)).GetComponent<Canvas>();hud.renderMode=RenderMode.ScreenSpaceCamera;hud.worldCamera=cam;hud.planeDistance=1;Canvas.ForceUpdateCanvases();
 for(int n=0;n<Names.Length;n++){
 var center=new Vector2((n%3-1)*80,(n<3?1:-1)*40);var paths=Guides(Names[n],center,4,false,0);
 var obj=new GameObject(Names[n]+" actual translucent preview");var tool=obj.AddComponent<PointWireTool>();var ground=obj.AddComponent<BoxCollider>();tool.Initialize(cam,hud.transform,new Surface{Ground=ground},Color.cyan);tool.DrawingHint="";tool.Label="";tool.RibbonWidth=4;tool.RibbonFillColor=new Color(.5f,.52f,.55f);tool.StampPaths=p=>paths;tool.Arm();typeof(PointWireTool).GetField("hint",I).GetValue(tool).GetType().GetProperty("text").SetValue(typeof(PointWireTool).GetField("hint",I).GetValue(tool),"");
 var previews=(List<List<Vector3>>)typeof(PointWireTool).GetField("stampPreviews",I).GetValue(tool);foreach(var path in paths){var preview=new List<Vector3>();typeof(PointWireTool).GetMethod("BuildGuide",I).Invoke(tool,new object[]{preview,path.Points,path.Closed});previews.Add(preview);}typeof(PointWireTool).GetMethod("LateUpdate",I).Invoke(tool,null);
 var label=new GameObject("Label",typeof(RectTransform),typeof(Text)).GetComponent<Text>();label.transform.SetParent(hud.transform,false);label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=28;label.alignment=TextAnchor.MiddleCenter;label.color=Color.white;label.text=new[]{"r · Roundabout","d · D loop","t · T junction","x · Diagonal crossing","s · Gentle S-curve","i · Intersection"}[n];label.rectTransform.sizeDelta=new Vector2(530,40);label.rectTransform.anchoredPosition=new Vector2((n%3-1)*565,n<3?470:-90);
 }
 Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1800,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1800,1200),0,0);tex.Apply();File.WriteAllBytes(Out+"Shape-Previews.png",tex.EncodeToPNG());RenderTexture.active=null;rt.Release();
 void Line(Vector3 a,Vector3 b){var l=new GameObject("4m grid",typeof(LineRenderer)).GetComponent<LineRenderer>();l.sharedMaterial=grid;l.startWidth=l.endWidth=.1f;l.positionCount=2;l.SetPositions(new[]{a,b});}
 }
}
#endif

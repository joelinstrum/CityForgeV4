#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Collections;
using System.IO;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
namespace CityForge.World {
public static class LenientParkPathChecks {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static void Need(bool b,string m){if(!b)throw new Exception(m);}
 sealed class Surface:PointWireSurface {
 internal Terrain terrain;public override bool Available=>true;public override bool CanEdit=>true;
 public override Collider Collider=>terrain.GetComponent<TerrainCollider>();public override bool Contains(Vector2 p)=>true;public override float Height(Vector2 p)=>32;
 }
 public static void Run(){
 var root=new GameObject("Disposable path regression");var data=new TerrainData{heightmapResolution=33,size=new Vector3(512,64,512)};var h=new float[33,33];for(int z=0;z<33;z++)for(int x=0;x<33;x++)h[z,x]=.5f;data.SetHeights(0,0,h);
 var ground=Terrain.CreateTerrainGameObject(data);ground.transform.SetParent(root.transform);var view=root.AddComponent<PrototypeRegionView>();Set(view,"terrain",ground.GetComponent<Terrain>());Set(view,"<Waterless>k__BackingField",true);Set(view,"<SelectedDistrict>k__BackingField",3);
 var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);wall.transform.position=new Vector3(20,33,20);wall.transform.localScale=new Vector3(8,2,8);
 try {
 var buildings=(IList)view.GetType().GetField("roadBuildings",F).GetValue(view);var type=view.GetType().GetNestedType("RoadBuildingFootprint",BindingFlags.NonPublic);buildings.Add(Activator.CreateInstance(type,new object[]{wall.transform,new Vector2(8,8)}));
 var points=new[]{new Vector2(10,20),new Vector2(30,20)};var path=new LotPathDefinition{points=points,width=2,materialId=LotPathMaterial.ResidentialFootpath};
 var mesh=LotPathSurface.BuildMesh(path,Vector2.one,.09f,p=>32);
 Need((string)Call(view,"RoadSiteProblem",mesh)!=null,"Fixture did not reproduce building clearance veto");UnityEngine.Object.DestroyImmediate(mesh);
 Need(Call(view,"ParkPathSiteProblem",path)==null,"Existing building envelope blocked ground path");
 var canvas=new GameObject("Disposable path HUD",typeof(RectTransform),typeof(Canvas));canvas.transform.SetParent(root.transform);var cam=new GameObject("Disposable camera",typeof(Camera));cam.transform.SetParent(root.transform);
 var wire=root.AddComponent<PointWireTool>();wire.Initialize(cam.GetComponent<Camera>(),canvas.transform,new Surface{terrain=ground.GetComponent<Terrain>()},Color.yellow);wire.OpenPath=true;wire.FinishOverUi=true;
 wire.CompletionRequested=p=>(string)Call(view,"CreateParkPath",p,false,path.materialId,2,false);
 wire.Arm();Need(wire.PlaceMarker(points[0]),"First marker rejected");Need(!wire.CompletePath()&&wire.Points.Count==1,"Invalid completion discarded sketch");Need(wire.PlaceMarker(points[1]),"Second marker rejected");Need(wire.CompletePath(),"Enter completion rejected path through existing lot envelope");
 Need(view.CaptureParkPaths().Length==1,"Completion did not persist actual path record");
 var record=view.CaptureParkPaths()[0];var json=JsonUtility.ToJson(record);var restored=JsonUtility.FromJson<DistrictParkPathRecord>(json);PrototypeRegionView.ValidateParkPaths(new[]{restored},4,view.MapSide);Need(restored.path.points.Length==2,"Path serialization lost markers");
 var pathRoot=(GameObject)view.GetType().GetField("parkPathRoot",F).GetValue(view);var filter=pathRoot.GetComponentInChildren<MeshFilter>();Need(filter.sharedMesh.vertexCount>0,"Committed path has no rendered geometry");Need(filter.sharedMesh.bounds.min.y>32.08f,"Path buried beneath lot overlays");
 var mat=filter.GetComponent<MeshRenderer>().sharedMaterial;if(mat.shader.name=="CityForge/Lot Path Blend")Need(mat.renderQueue>DistrictLotSurfaces.PathRenderQueue,"Player paving hidden behind authored paths");

 Set(view,"<Waterless>k__BackingField",false);ground.transform.position=Vector3.down*40;Need(Call(view,"ParkPathSiteProblem",path)!=null,"Underwater path accepted");ground.transform.position=Vector3.zero;Set(view,"<Waterless>k__BackingField",true);
 path.points=new[]{new Vector2(-10,20),new Vector2(30,20)};Need(Call(view,"ParkPathSiteProblem",path)!=null,"Cross-district route accepted");
 Call(view,"UndoParkPath");Need(view.CaptureParkPaths().Length==0,"Path undo did not remove record");
 // Exercise the Lot Editor's actual Enter/Finish handler without opening a user lot.
 var panel=root.AddComponent<CityForge.UI.LotEditorPanel>();var guide=(PointWirePath)panel.GetType().GetField("groundPath",F).GetValue(panel);var session=(LotEditorSession)panel.GetType().GetField("session",F).GetValue(panel);
 const string recentKey="CityForge.Lots.RecentPathMaterials";bool hadRecent=PlayerPrefs.HasKey(recentKey);string previousRecent=PlayerPrefs.GetString(recentKey);
 try {
 guide.TryAdd(Vector2.zero);Call(panel,"FinishGroundPath",false,true);Need(guide.Points.Count==1&&session.Lot.paths.Count==0,"Lot editor Enter discarded incomplete sketch");guide.TryAdd(new Vector2(4,0));Call(panel,"FinishGroundPath",false,true);Need(session.Lot.paths.Count==1&&!guide.Finished&&guide.Points.Count==0,"Lot editor Enter did not commit actual path");Need(session.CanUndo,"Lot editor path creation lost undo");session.Undo();Need(session.Lot.paths.Count==0,"Lot editor Enter path undo failed");
 }finally{if(hadRecent)PlayerPrefs.SetString(recentKey,previousRecent);else PlayerPrefs.DeleteKey(recentKey);}
 Debug.Log("LENIENT PARK PATH CHECKS PASS: former building-envelope veto reproduced, actual Enter completion creates visible path, one-marker sketch retained, serialization and undo pass, dry-ground/district containment retained; Lot Editor Enter creates a real path with undo.");
 }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(data);}
 }
}}
#endif

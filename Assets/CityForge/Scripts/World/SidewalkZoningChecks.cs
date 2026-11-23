#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Collections;
using CityForge.World;
using CityForge.Lots;
using UnityEngine;
public static class SidewalkZoningChecks
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
 static void Need(bool b,string m){if(!b)throw new Exception(m);}
 public static void Run(){
 var root=new GameObject("Disposable sidewalk zoning regression");
 var view=root.AddComponent<PrototypeRegionView>();
 var filter=root.AddComponent<MeshFilter>();
 var cells=new RectInt(20,20,8,2);
 var min=(Vector2)Call(view,"ZoneCorner",cells.min);
 var max=(Vector2)Call(view,"ZoneCorner",cells.max);
 var roads=(IDictionary)view.GetType().GetField("districtRoadMeshes",F).GetValue(view);
 try {
 foreach(int width in new[]{4,8}) {
 var road=new DistrictRoadRecord{width=width,surfaceId="street-cobblestone",points=new[]{new Vector2(min.x-20,min.y-width*.5f+1.5f),new Vector2(max.x+20,min.y-width*.5f+1.5f)}};
 var mesh=LotPathSurface.BuildMesh(new LotPathDefinition{points=road.points,width=width},Vector2.one,0);
 filter.sharedMesh=mesh;roads.Add(road,filter);
 var parcel=new DistrictLotRecord{center=(min+max)*.5f,definition=new LotDefinition{width=cells.width,length=cells.height}};
 Need((bool)Call(view,"LotOverlapsDistrictRoad",parcel,0f,0f),"Fixture must reproduce grid tile clipping pavement");
 Need(!(bool)Call(view,"ZoneOverlapsDistrictRoad",cells),"Sidewalk/curb edge cells rejected");
 object[] args={cells,6,TownZoneFacing.None,null,false,TownZoneFacing.None,false,false,true};
 Need(Call(view,"ResidentialFrontageProblem",args)==null,"Valid curb-edge zoning frontage rejected");
 // A row centered inside pavement must still fail.
 var roadCells=new RectInt(cells.x,cells.y-1,cells.width,1);
 Need((bool)Call(view,"ZoneOverlapsDistrictRoad",roadCells),"Road-centered cells accepted");
 roads.Clear();UnityEngine.Object.DestroyImmediate(mesh);
 }
 CityForge.Editor.PreparedGroundChecks.Run();
 CityForge.Editor.InteractionPerformanceChecks.Run();
 Debug.Log("SIDEWALK ZONING CHECKS PASS: curb-straddling grid cells accepted, frontage accepted, 4m/8m paved roads protected, strict lot/building collision retained.");
 }finally{roads.Clear();UnityEngine.Object.DestroyImmediate(root);}
 }
}
#endif

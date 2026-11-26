#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEditor;using CityForge.World;
public static class WideRoadChecks {
 static readonly BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;static List<string> checks=new();static Type V=typeof(PrototypeRegionView);
 static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,F).Invoke(o,args);
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add("PASS "+text);}
 public static void Run(){string output=Path.GetFullPath("Library/TransportChecks/WideRoads");Directory.CreateDirectory(output);File.Delete(Path.Combine(output,"Error.txt"));try{
 var go=new GameObject("Wide road native checks");var view=go.AddComponent<PrototypeRegionView>();view.enabled=false;
 var boulevard=new DistrictRoadRecord{district=0,surfaceId="art-deco-divided-boulevard",width=20,streetTreeStyle="city-tree-small",points=new[]{new Vector2(20,100),new Vector2(220,100)}};
 var avenue=new DistrictRoadRecord{district=0,surfaceId="art-deco-concrete-avenue",width=12,points=new[]{new Vector2(120,20),new Vector2(120,220)}};
 Check((int)V.GetMethod("DefaultRoadWidth",F).Invoke(null,new object[]{boulevard.surfaceId})==20,"Boulevard placement width20m");Check((int)V.GetMethod("DefaultRoadWidth",F).Invoke(null,new object[]{avenue.surfaceId})==12,"Avenue placement width12m");
 var records=JsonUtility.FromJson<RoadPack>(JsonUtility.ToJson(new RoadPack{roads=new[]{boulevard,avenue}})).roads;V.GetMethod("ValidateDistrictRoads",F).Invoke(null,new object[]{records,4,1024f});Check(records[0].width==20&&records[1].width==12,"Wide native records survive serialization and validation");
 Call(view,"RestoreDistrictRoads",(object)records);var saved=(List<DistrictRoadRecord>)V.GetField("districtRoads",F).GetValue(view);Check(saved[0].width==20&&saved[1].width==12,"Restore does not collapse sidewalk roads to4m");
 var type=V.Assembly.GetType("CityForge.World.StreetTreePalette");var plan=type.GetMethod("Plan",F);
 var median=((IEnumerable)plan.Invoke(null,new object[]{boulevard,boulevard.points,(Func<Vector2,bool>)(p=>true)})).Cast<object>().ToArray();var curb=((IEnumerable)plan.Invoke(null,new object[]{new DistrictRoadRecord{surfaceId=avenue.surfaceId,width=12,streetTreeStyle="city-tree-small"},avenue.points,(Func<Vector2,bool>)(p=>true)})).Cast<object>().ToArray();
 Vector2 Point(object p)=>(Vector2)p.GetType().GetField("Point",F).GetValue(p);
 Check(median.Length>10&&median.All(p=>Mathf.Abs(Point(p).y-100)<.001f),"Boulevard trees use one middle row");Check(curb.Any(p=>Point(p).x<120)&&curb.Any(p=>Point(p).x>120),"Avenue trees retain two curbside rows");
 var meshes=(IDictionary)V.GetField("districtRoadMeshes",F).GetValue(view);meshes.Add(boulevard,new GameObject("Road").AddComponent<MeshFilter>());
 var lane=(Vector2[])Call(view,"MotorBoulevardCenterline",boulevard);Check(lane[0]==boulevard.points[0]&&lane[^1]==boulevard.points[^1],"Vehicle lane preserves endpoint connections");Check(lane.Any(p=>Mathf.Abs(p.y-100)>5.9f),"Vehicle lane follows paved carriageway rather than median");
 var vehicleRoutes=(IEnumerable<(Vector2[] Points,bool Closed)>)Call(view,"VehicleRoadRoutes");Check(vehicleRoutes.Any(r=>r.Points.Any(p=>Mathf.Abs(p.y-100)>5.9f)),"Production vehicle route selection uses paved carriageway alignment");
 meshes.Add(avenue,new GameObject("Cross road").AddComponent<MeshFilter>());lane=(Vector2[])Call(view,"MotorBoulevardCenterline",boulevard);Check(lane.Where(p=>Mathf.Abs(p.x-120)<7).All(p=>Mathf.Abs(p.y-100)<.01f),"Vehicle lane rejoins intersection opening");
 var loop=new DistrictRoadRecord{surfaceId=boulevard.surfaceId,width=20,closed=true,points=new[]{new Vector2(20,20),new Vector2(80,20),new Vector2(80,80),new Vector2(20,80)}};Check(((Vector2[])Call(view,"MotorBoulevardCenterline",loop)).Length>80,"Closed boulevard lane remains continuous");
 var data=new TerrainData{heightmapResolution=33,size=new Vector3(256,100,256)};var terrain=Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();V.GetField("terrain",F).SetValue(view,terrain);
 var generated=new List<(string label,Mesh mesh)>();Call(view,"PresentBoulevardMedians",new[]{boulevard,avenue},(Action<string,Mesh,Material>)((name,mesh,mat)=>generated.Add((name,mesh))));
 Check(generated.Any(p=>p.label=="Boulevard planted median")&&generated.Any(p=>p.label=="Boulevard median curb"),"Native grass and curb meshes generated");
 Check(generated.All(p=>p.mesh.vertexCount>0&&p.mesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z))),"Median meshes are finite");
 Check(generated.Where(p=>p.label=="Boulevard planted median").SelectMany(p=>p.mesh.vertices).All(p=>Mathf.Abs(p.x-120)>17),"Median leaves crossing open");
 File.WriteAllLines(Path.Combine(output,"Checks.txt"),checks);EditorApplication.Exit(0);
 }catch(Exception e){File.WriteAllText(Path.Combine(output,"Error.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
 [Serializable]class RoadPack{public DistrictRoadRecord[] roads;}
}
#endif

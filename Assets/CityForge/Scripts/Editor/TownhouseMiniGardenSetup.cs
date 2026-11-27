#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEngine;
namespace CityForge.EditorTools {
 internal static class TownhouseMiniGardenSetup {
 const string Buildings="Assets/CityForge/Resources/Lots/RebuiltBuildings/",Components="Assets/CityForge/PreparedComponents/BrownstoneWhiteFlowers/",Shared="Assets/CityForge/Resources/Lots/TownhouseGardens/";
 const string Marker="Townhouse mini garden";
 internal sealed class Bed {
  internal Rect Area;internal float Soil;internal int Columns,Rows,Flowers;internal float FlowerScale;internal bool Pad,Border;
  internal Bed(float x0,float x1,float z0,float z1,float soil,int columns,int rows,int flowers,float flowerScale,bool pad=false,bool border=false){Area=Rect.MinMaxRect(x0,z0,x1,z1);Soil=soil;Columns=columns;Rows=rows;Flowers=flowers;FlowerScale=flowerScale;Pad=pad;Border=border;}
 }
 internal sealed class Plan {internal string Folder,Edge;internal Bed[] Beds;internal Plan(string folder,string edge,params Bed[] beds){Folder=folder;Edge=edge;Beds=beds;}}
 internal static readonly Plan[] Plans={
  new Plan("ResidentialDeco01","CFV005_CF_Foundation_Sandstone_Dark.mat",new Bed(-3.4f,.8f,7.4f,9.3f,.15f,5,3,3,1.35f,true,true)),
  new Plan("SandstoneArtDecoTownhouse01","TH-Foundation-CF_Foundation_Sandstone.mat",new Bed(-3.6f,-1.4f,7.4f,9.3f,.15f,3,3,2,1.35f,true,true),new Bed(1.4f,3.6f,7.4f,9.3f,.15f,3,3,2,1.35f,true,true)),
  new Plan("LimestoneTownhouse",null,new Bed(-3.08f,-1.72f,8.4f,9.12f,.527f,5,2,1,1.1f),new Bed(1.72f,3.08f,8.4f,9.12f,.527f,5,2,1,1.1f)),
  new Plan("BrickTownhouse02","BT2_dfa54c72771242.mat",new Bed(-2.86f,.50f,7.80f,9.35f,.15f,4,3,2,1.25f,true),new Bed(-.45f,.50f,6.93f,7.80f,.15f,2,2,0,1.25f,true)),
  new Plan("SteppedBrickTownhouse01",null,new Bed(-3.35f,-.25f,6.045f,8.395f,.23f,4,3,4,1.4f))
 };
 [MenuItem("CityForge/Buildings/Install Townhouse Mini Gardens")]
 internal static void Install(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Install native gardens outside Play mode.");Directory.CreateDirectory(Shared);AssetDatabase.Refresh();foreach(var plan in Plans)InstallOne(plan.Folder);AssetDatabase.SaveAssets();}
 internal static void InstallOne(string folder){
  var plan=Plans.Single(p=>p.Folder==folder);Directory.CreateDirectory(Shared);Directory.CreateDirectory(Buildings+folder+"/MiniGarden");AssetDatabase.Refresh();
  var leaves=MaterialCopy(Buildings+"SteppedBrickTownhouse01/GardenFlowers/Garden-Leaves.mat",Shared+"Leaves.mat");
  var soil=MaterialCopy(Buildings+"SteppedBrickTownhouse01/GardenFlowers/Garden-Soil.mat",Shared+"Soil.mat");
  Material edge=plan.Edge==null?null:MaterialCopy(Buildings+folder+"/"+plan.Edge,Buildings+folder+"/MiniGarden/Edging.mat");
  var leafPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Components+"Shrub-Far.prefab");var leafInstance=UnityEngine.Object.Instantiate(leafPrefab);
  var leafPieces=new List<CombineInstance>();Mesh leaf=null;
  try{
   foreach(var f in leafInstance.GetComponentsInChildren<MeshFilter>(true)){var mats=f.GetComponent<Renderer>().sharedMaterials;for(int sub=0;sub<f.sharedMesh.subMeshCount;sub++)if(mats[sub].name=="CF_WhiteFlowers_Leaves")leafPieces.Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=sub,transform=leafInstance.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});}
   leaf=new Mesh{name="Prepared low-poly leaves"};leaf.CombineMeshes(leafPieces.ToArray(),true,true);Normalize(leaf);if(leaf.GetIndexCount(0)/3!=120)throw new InvalidDataException("Changed prepared leaf topology");
   foreach(string tier in new[]{"Near","Medium"}){
    string path=Buildings+folder+"/"+tier+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
    try{
     foreach(string name in new[]{Marker,"White flowering garden"}){var old=root.transform.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);}
     if(folder=="SteppedBrickTownhouse01")ClearVolume(root,folder,tier,new Bounds(new Vector3(-.90f,.77f,7.675f),new Vector3(1.38f,1.18f,.43f)),null);
     if(folder=="LimestoneTownhouse")ClearVolume(root,folder,tier,new Bounds(new Vector3(0,.75f,8.775f),new Vector3(6.6f,.8f,1.05f)),"LTH-Green__Building");
     if(folder=="BrickTownhouse02")ClearVolume(root,folder,tier,new Bounds(new Vector3(-1.65f,.70f,7.9f),new Vector3(2.95f,1.5f,.26f)),"BT2_3030e10ca4f609");
     if(folder=="BrickTownhouse02")ClearVolume(root,folder,tier,new Bounds(new Vector3(-1.65f,.09f,7.9f),new Vector3(2.56f,.184f,.254f)),null);
     var garden=new GameObject(Marker);garden.transform.SetParent(root.transform,false);
     var green=new List<CombineInstance>();var whites=new List<Matrix4x4>();var pads=new List<CombineInstance>();var edging=new List<CombineInstance>();
     var cubeObject=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=cubeObject.GetComponent<MeshFilter>().sharedMesh;
     try{
      int index=0;
      foreach(var bed in plan.Beds){
       float radius=Mathf.Sqrt(leaf.bounds.extents.x*leaf.bounds.extents.x+leaf.bounds.extents.z*leaf.bounds.extents.z);
       float scale=Mathf.Min(1.4f,Mathf.Min(bed.Area.width,bed.Area.height)*.9f/(2*radius));float margin=radius*scale+.015f;
       for(int z=0;z<bed.Rows;z++)for(int x=0;x<bed.Columns;x++){
        float px=bed.Columns==1?bed.Area.center.x:Mathf.Lerp(bed.Area.xMin+margin,bed.Area.xMax-margin,(float)x/(bed.Columns-1));
        float pz=bed.Rows==1?bed.Area.center.y:Mathf.Lerp(bed.Area.yMin+margin,bed.Area.yMax-margin,(float)z/(bed.Rows-1));
        green.Add(new CombineInstance{mesh=leaf,subMeshIndex=0,transform=Matrix4x4.TRS(new Vector3(px,bed.Soil-.015f,pz),Quaternion.Euler(0,(index++*57)%360,0),Vector3.one*scale)});
       }
       var points=bed.Flowers==0?new Vector2[0]:bed.Flowers==1?new[]{new Vector2(.5f,.5f)}:bed.Flowers==2?new[]{new Vector2(.28f,.35f),new Vector2(.72f,.65f)}:bed.Flowers==3?new[]{new Vector2(.18f,.35f),new Vector2(.5f,.7f),new Vector2(.82f,.35f)}:new[]{new Vector2(.2f,.25f),new Vector2(.8f,.25f),new Vector2(.25f,.75f),new Vector2(.75f,.75f)};
       foreach(var point in points)whites.Add(Matrix4x4.TRS(new Vector3(Mathf.Lerp(bed.Area.xMin+.22f,bed.Area.xMax-.22f,point.x),bed.Soil+leaf.bounds.size.y*scale*.60f,Mathf.Lerp(bed.Area.yMin+.22f,bed.Area.yMax-.22f,point.y)),Quaternion.Euler(0,(index++*71)%360,0),Vector3.one*bed.FlowerScale));
       void Box(List<CombineInstance> list,Vector3 pos,Vector3 size){list.Add(new CombineInstance{mesh=cube,subMeshIndex=0,transform=Matrix4x4.TRS(pos,Quaternion.identity,size)});}
       if(bed.Pad)Box(pads,new Vector3(bed.Area.center.x,bed.Soil-.035f,bed.Area.center.y),new Vector3(bed.Area.width,.07f,bed.Area.height));
       if(bed.Border){float h=bed.Soil+.03f;foreach(float x in new[]{bed.Area.xMin-.04f,bed.Area.xMax+.04f})Box(edging,new Vector3(x,h*.5f,bed.Area.center.y),new Vector3(.08f,h,bed.Area.height+.16f));foreach(float z in new[]{bed.Area.yMin-.04f,bed.Area.yMax+.04f})Box(edging,new Vector3(bed.Area.center.x,h*.5f,z),new Vector3(bed.Area.width,h,.08f));}
      }
      if(folder=="BrickTownhouse02"){
       // An open L-shaped bed meets the bay foundation and stair cheek; edging remains only on the street and outer side.
       edging.Add(new CombineInstance{mesh=cube,subMeshIndex=0,transform=Matrix4x4.TRS(new Vector3(-1.25f,.09f,9.39f),Quaternion.identity,new Vector3(3.58f,.18f,.08f))});
       edging.Add(new CombineInstance{mesh=cube,subMeshIndex=0,transform=Matrix4x4.TRS(new Vector3(-3f,.09f,8.6115f),Quaternion.identity,new Vector3(.08f,.18f,1.637f))});
      }
      SavePart(garden,"Green planting",green,leaves,folder,tier);if(pads.Count>0)SavePart(garden,"Warm soil",pads,soil,folder,tier);if(edging.Count>0)SavePart(garden,"Stone edging",edging,edge,folder,tier);
     }finally{UnityEngine.Object.DestroyImmediate(cubeObject);}
     var flowerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Components+"BloomCluster-"+(tier=="Near"?"Medium":"Far")+".prefab");var flower=UnityEngine.Object.Instantiate(flowerPrefab);
     try{
      var filters=flower.GetComponentsInChildren<MeshFilter>(true);var materials=filters.SelectMany(f=>f.GetComponent<Renderer>().sharedMaterials).Distinct().ToArray();
      foreach(var mat in materials){var pieces=new List<CombineInstance>();foreach(var transform in whites)foreach(var f in filters){var mats=f.GetComponent<Renderer>().sharedMaterials;for(int sub=0;sub<mats.Length;sub++)if(mats[sub]==mat)pieces.Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=sub,transform=transform*flower.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});}SavePart(garden,"White flower accents "+mat.name,pieces,mat,folder,tier);}
     }finally{UnityEngine.Object.DestroyImmediate(flower);}
     TownhouseGardenFenceSetup.Install(garden,folder,tier);
     // Existing Limestone and Stepped soil stays in its authored container with the reviewed warm-earth finish.
     foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){
      if(renderer.transform.IsChildOf(garden.transform))continue;var mats=renderer.sharedMaterials;for(int i=0;i<mats.Length;i++)if(mats[i]&&(mats[i].name=="LTH-Soil__Shared"||mats[i].name=="Garden-Soil"))mats[i]=soil;renderer.sharedMaterials=mats;
     }
     PrefabUtility.SaveAsPrefabAsset(root,path);
    }finally{PrefabUtility.UnloadPrefabContents(root);}
   }
  }finally{if(leaf)UnityEngine.Object.DestroyImmediate(leaf);UnityEngine.Object.DestroyImmediate(leafInstance);}
  AssetDatabase.SaveAssets();
 }
 static Material MaterialCopy(string source,string path){var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;var original=AssetDatabase.LoadAssetAtPath<Material>(source);if(!original)throw new InvalidDataException("Missing garden material "+source);m=new Material(original){name=Path.GetFileNameWithoutExtension(path)};AssetDatabase.CreateAsset(m,path);return m;}
 static void Normalize(Mesh mesh){mesh.RecalculateBounds();var b=mesh.bounds;mesh.vertices=mesh.vertices.Select(v=>new Vector3(v.x-b.center.x,v.y-b.min.y,v.z-b.center.z)).ToArray();mesh.RecalculateBounds();}
 static void SavePart(GameObject parent,string name,List<CombineInstance> pieces,Material material,string folder,string tier){if(pieces.Count==0)return;var mesh=new Mesh{name=name};mesh.CombineMeshes(pieces.ToArray(),true,true);mesh.RecalculateBounds();string safe=string.Concat(name.Select(c=>char.IsLetterOrDigit(c)?c:'-'));string path=Buildings+folder+"/MiniGarden/"+tier+"-"+safe+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,path);saved=mesh;}var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent.transform,false);obj.GetComponent<MeshFilter>().sharedMesh=saved;obj.GetComponent<MeshRenderer>().sharedMaterial=material;}
 static void ClearVolume(GameObject root,string folder,string tier,Bounds volume,string onlyMaterial){
  foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true)){
   var source=filter.sharedMesh;if(!source)continue;var vertices=source.vertices;var matrix=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;var materials=filter.GetComponent<Renderer>().sharedMaterials;var lists=new List<int[]>();int removed=0;
   for(int sub=0;sub<source.subMeshCount;sub++){var indices=source.GetTriangles(sub);var keep=new List<int>();bool allowed=onlyMaterial==null||materials[sub].name==onlyMaterial;
    for(int i=0;i<indices.Length;i+=3){if(allowed&&Enumerable.Range(0,3).All(k=>volume.Contains(matrix.MultiplyPoint3x4(vertices[indices[i+k]]))))removed++;else{keep.Add(indices[i]);keep.Add(indices[i+1]);keep.Add(indices[i+2]);}}lists.Add(keep.ToArray());}
   if(removed==0)continue;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id);var mesh=UnityEngine.Object.Instantiate(source);for(int sub=0;sub<lists.Count;sub++)mesh.SetTriangles(lists[sub],sub,false);mesh.RecalculateBounds();string path=Buildings+folder+"/MiniGarden/"+tier+"-Cleared-"+guid+"-"+id+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);filter.sharedMesh=saved;}else{AssetDatabase.CreateAsset(mesh,path);filter.sharedMesh=mesh;}
  }
 }
 }
}
#endif

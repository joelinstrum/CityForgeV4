#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace CityForge.EditorTools {
 // Native garden adapter: reuses the approved Brick Townhouse 2 iron parts without changing its authoring kit.
 internal static class TownhouseGardenFenceSetup {
  const string Shared="Assets/CityForge/Resources/Lots/TownhouseGardens/";
  const string Buildings="Assets/CityForge/Resources/Lots/RebuiltBuildings/";
  [Serializable] sealed class Source {public Part[] parts;public float bayWidth,railLength;}
  [Serializable] sealed class Part {public string name;public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles;}
  internal static bool Applies(string folder)=>folder=="ResidentialDeco01"||folder=="SandstoneArtDecoTownhouse01"||folder=="BrickTownhouse02";
  internal static void Install(GameObject garden,string folder,string tier) {
   if(!Applies(folder))return;
   var json=AssetDatabase.LoadAssetAtPath<TextAsset>(Shared+"IronFenceSource.json");
   if(!json)throw new InvalidDataException("Missing prepared native iron fence components.");
   var source=JsonUtility.FromJson<Source>(json.text);
   var meshes=new Dictionary<string,Mesh>();
   foreach(var part in source.parts){var mesh=new Mesh{name="Approved iron "+part.name};mesh.vertices=part.vertices;mesh.normals=part.normals;mesh.uv=part.uv;mesh.triangles=part.triangles;mesh.RecalculateBounds();meshes.Add(part.name,mesh);}
   var iron=AssetDatabase.LoadAssetAtPath<Material>(Shared+"Iron.mat");
   if(!iron){var original=AssetDatabase.LoadAssetAtPath<Material>(Buildings+"BrickTownhouse02/BT2_3030e10ca4f609.mat");if(!original)throw new InvalidDataException("Missing original iron material");iron=new Material(original){name="Townhouse garden iron"};AssetDatabase.CreateAsset(iron,Shared+"Iron.mat");}
   var pieces=new List<CombineInstance>();
   try {
    var plan=TownhouseMiniGardenSetup.Plans.Single(p=>p.Folder==folder);
    foreach(var bed in folder=="BrickTownhouse02"?plan.Beds.Take(1):plan.Beds){
     var area=bed.Area;area=Rect.MinMaxRect(area.xMin-.04f,area.yMin-.04f,area.xMax+.04f,area.yMax+.04f);
     // The brick bay occupies the old tiny enclosure. Extend its existing side fence forward and relocate its front rail.
     bool extended=folder=="BrickTownhouse02";
     if(extended)area=Rect.MinMaxRect(-3f,7.833f,.50f,bed.Area.yMax+.04f);
     var corners=new[]{new Vector3(area.xMin,0,area.yMin),new Vector3(area.xMax,0,area.yMin),new Vector3(area.xMax,0,area.yMax),new Vector3(area.xMin,0,area.yMax)};
     for(int corner=0;corner<4;corner++)if(!extended||corner!=1)Add("Post",Matrix4x4.TRS(corners[corner],Quaternion.identity,Vector3.one));
     for(int edge=extended?2:0;edge<4;edge++)Segment(corners[edge],corners[(edge+1)%4]);
    }
    var combined=new Mesh{name="Front garden iron enclosure"};combined.CombineMeshes(pieces.ToArray(),true,true);combined.RecalculateBounds();
    string path=Buildings+folder+"/MiniGarden/"+tier+"-Iron-enclosure.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
    if(saved){EditorUtility.CopySerialized(combined,saved);UnityEngine.Object.DestroyImmediate(combined);}else{AssetDatabase.CreateAsset(combined,path);saved=combined;}
    var obj=new GameObject("Iron enclosure",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(garden.transform,false);obj.GetComponent<MeshFilter>().sharedMesh=saved;obj.GetComponent<MeshRenderer>().sharedMaterial=iron;
   } finally {foreach(var mesh in meshes.Values)UnityEngine.Object.DestroyImmediate(mesh);}
   void Add(string part,Matrix4x4 transform)=>pieces.Add(new CombineInstance{mesh=meshes[part],subMeshIndex=0,transform=transform});
   void Segment(Vector3 start,Vector3 end){
    var delta=end-start;float length=delta.magnitude;var rotation=Quaternion.Euler(0,-Mathf.Atan2(delta.z,delta.x)*Mathf.Rad2Deg,0);var direction=delta/length;
    Add("Rails",Matrix4x4.TRS(start,rotation,new Vector3(length/source.railLength,1,1)));
    int bays=Mathf.Max(1,Mathf.FloorToInt((length-.30f)/source.bayWidth));float inset=(length-bays*source.bayWidth)*.5f;
    for(int i=0;i<=bays;i++){var position=start+direction*(inset+i*source.bayWidth);Add("Picket",Matrix4x4.TRS(position,rotation,Vector3.one));if(i<bays)Add("Arch",Matrix4x4.TRS(position,rotation,Vector3.one));}
   }
  }
 }
}
#endif

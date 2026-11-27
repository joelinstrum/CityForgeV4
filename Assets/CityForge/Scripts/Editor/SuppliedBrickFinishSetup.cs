#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEngine;using CityForge.Lots;
namespace CityForge.EditorTools {
 internal static class SuppliedBrickFinishSetup {
 const string Root="Assets/CityForge/Resources/Lots/",Folder=Root+"SuppliedBrickFinishes/";
 static readonly (string id,string file,float rows)[] Sources={
 ("blue","Weathered Blue-Gray Brick Wall-2.png",17),("yellow","Weathered Golden Brick Wall Texture-3.png",20),("sand","Weathered Beige Brick Wall Texture-4.png",15.3f),("white","Weathered White Brick Wall Texture-1.png",19.3f),("red","Weathered Colonial Red Brickwork.png",20),("slate","Aged Slate Blue Painted Brick Wall.png",19)};
 [MenuItem("CityForge/Buildings/Install Supplied Brick Finishes")]
 internal static void Install(){
 if(EditorApplication.isPlaying)throw new InvalidOperationException("Install brick finishes outside Play mode.");
 var finishes=new Dictionary<string,BuildingWallFinish>();var lines=new List<string>();
 var packed=new Texture2D(2,2,TextureFormat.RGBA32,false,true);packed.SetPixels(Enumerable.Repeat(new Color(0,0,0,.18f),4).ToArray());packed.Apply();string packedPath=Folder+"Matte-Surface.png";File.WriteAllBytes(packedPath,packed.EncodeToPNG());UnityEngine.Object.DestroyImmediate(packed);AssetDatabase.ImportAsset(packedPath,ImportAssetOptions.ForceSynchronousImport);Import(packedPath,false,false);
 foreach(var src in Sources){
  string path=Folder+src.file;Import(path,true,false);var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels();int w=texture.width,h=texture.height;var normals=new Color[pixels.Length];Color mean=Color.clear;
  float Height(int x,int y){var c=pixels[((y+h)%h)*w+(x+w)%w].linear;return c.r*.2126f+c.g*.7152f+c.b*.0722f;}
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){int i=y*w+x;var n=new Vector3((Height(x-2,y)-Height(x+2,y))*2f,(Height(x,y-2)-Height(x,y+2))*2f,1).normalized;normals[i]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);if(i%16==0)mean+=pixels[i].linear;}
  mean/=Mathf.Ceil(pixels.Length/16f);mean.a=1;
  var normal=new Texture2D(w,h,TextureFormat.RGB24,false,true);normal.SetPixels(normals);normal.Apply();string normalPath=Folder+src.id+"-Normal.png";File.WriteAllBytes(normalPath,normal.EncodeToPNG());UnityEngine.Object.DestroyImmediate(normal);AssetDatabase.ImportAsset(normalPath,ImportAssetOptions.ForceSynchronousImport);Import(normalPath,false,true);
  finishes[src.id]=new BuildingWallFinish{id=src.id,albedo=texture,calmAlbedo=texture,normal=AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath),packedSurface=AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath),meanColor=mean,customSurface=true};
 }
 foreach(string family in new[]{"ResidentialDeco01","BrickTownhouse02","SteppedBrickTownhouse01"})foreach(string tier in new[]{"Near","Medium"}){
  string path=Root+"RebuiltBuildings/"+family+"/"+tier+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
  try{
   var profile=root.GetComponentInChildren<BuildingTintProfile>(true);profile.wallLabel="Brick finish";
   foreach(var binding in profile.bindings.Where(b=>b.region=="walls")){
    var metres=Measure(root,binding.material);lines.Add(family+" "+tier+" "+binding.material.name+" metres/UV="+metres);
    binding.finishes=Sources.Select(s=>{var f=finishes[s.id];return new BuildingWallFinish{id=f.id,albedo=f.albedo,calmAlbedo=f.calmAlbedo,normal=f.normal,packedSurface=f.packedSurface,meanColor=f.meanColor,customSurface=true,coursesPerTile=s.rows,uvTransform=new Vector4(metres.x/1.35f,metres.y/(s.rows*.075f),0,0)};}).ToArray();
   }
   PrefabUtility.SaveAsPrefabAsset(root,path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 AssetDatabase.SaveAssets();Debug.Log("Supplied brick UV mapping:\n"+string.Join("\n",lines));
 }
 static void Import(string path,bool srgb,bool normal){var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=srgb;importer.isReadable=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
 static Vector2 Measure(GameObject root,Material material){var horizontal=new List<float>();var vertical=new List<float>();
 foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true)){
  var renderer=filter.GetComponent<Renderer>();var mesh=filter.sharedMesh;if(!renderer||!mesh)continue;var vertices=mesh.vertices;var uv=mesh.uv;var matrix=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;if(uv.Length!=vertices.Length)continue;
  for(int sub=0;sub<mesh.subMeshCount;sub++)if(renderer.sharedMaterials[sub]==material){var triangles=mesh.GetTriangles(sub);for(int i=0;i<triangles.Length;i+=3)for(int edge=0;edge<3;edge++){
   int a=triangles[i+edge],b=triangles[i+(edge+1)%3];var delta=matrix.MultiplyPoint3x4(vertices[b])-matrix.MultiplyPoint3x4(vertices[a]);var tex=uv[b]-uv[a];float length=delta.magnitude;
   if(length<.01f)continue;if(Mathf.Abs(tex.y)<.0001f&&Mathf.Abs(tex.x)>.0001f&&Mathf.Abs(delta.y)<.001f)horizontal.Add(length/Mathf.Abs(tex.x));
   if(Mathf.Abs(tex.x)<.0001f&&Mathf.Abs(tex.y)>.0001f&&Mathf.Abs(delta.y)>length*.99f)vertical.Add(length/Mathf.Abs(tex.y));
  }}
 }
 float Median(List<float> values){if(values.Count==0)throw new InvalidDataException("Cannot measure brick UV scale: "+material.name);values.Sort();return values[values.Count/2];}
 return new Vector2(Median(horizontal),Median(vertical));
 }
 }
}
#endif

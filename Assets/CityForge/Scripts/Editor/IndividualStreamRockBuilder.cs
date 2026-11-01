#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using CityForge.World;
namespace CityForge.Editor {
public static class IndividualStreamRockBuilder {
 const string Root="Assets/CityForge/Resources/IndividualStreamRocks";
 public static void Build(){
  if(EditorApplication.isPlaying||File.Exists(Root+"/Stones.asset"))throw new InvalidOperationException("Prepare once in Edit mode; preserve existing output.");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  foreach(var file in new[]{"RocksNew_Color.jpg","RocksNew_Normal.jpg","RocksNew_Roughness.jpg"}){
   var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/"+file);importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.anisoLevel=4;importer.sRGBTexture=file.Contains("Color");
   if(file.Contains("Normal"))importer.textureType=TextureImporterType.NormalMap;
   importer.isReadable=file.Contains("Roughness");importer.SaveAndReimport();
  }
  var rough=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/RocksNew_Roughness.jpg");var pixels=rough.GetPixels32();for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(0,0,0,(byte)(255-pixels[i].r));
  var mask=new Texture2D(rough.width,rough.height,TextureFormat.RGBA32,false,true);mask.SetPixels32(pixels);mask.Apply();File.WriteAllBytes(Root+"/Smoothness.png",mask.EncodeToPNG());UnityEngine.Object.DestroyImmediate(mask);
  AssetDatabase.ImportAsset(Root+"/Smoothness.png",ImportAssetOptions.ForceSynchronousImport);var maskImporter=(TextureImporter)AssetImporter.GetAtPath(Root+"/Smoothness.png");maskImporter.sRGBTexture=false;maskImporter.maxTextureSize=2048;maskImporter.mipmapEnabled=true;maskImporter.SaveAndReimport();
  var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Individual river stones",enableInstancing=true};material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/RocksNew_Color.jpg"));material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/RocksNew_Normal.jpg"));material.SetFloat("_BumpScale",.65f);material.EnableKeyword("_NORMALMAP");material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Smoothness.png"));material.EnableKeyword("_METALLICSPECGLOSSMAP");material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.5f);AssetDatabase.CreateAsset(material,Root+"/Stones.mat");
  var path=Root+"/IndividualStones.fbx";var modelImporter=(ModelImporter)AssetImporter.GetAtPath(path);modelImporter.materialImportMode=ModelImporterMaterialImportMode.None;modelImporter.importAnimation=false;modelImporter.importCameras=false;modelImporter.importLights=false;modelImporter.SaveAndReimport();
  var filters=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshFilter>().OrderBy(f=>f.name).ToArray();if(filters.Length!=10)throw new InvalidOperationException("Expected ten individual stone meshes.");
  var assets=ScriptableObject.CreateInstance<RiverBedRockAssets>();assets.meshes=new Mesh[10];assets.materials=Enumerable.Repeat(material,10).ToArray();assets.mossy=new bool[10];
  for(int i=0;i<filters.Length;i++){
   var filter=filters[i];var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);var matrix=filter.transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;var vertices=mesh.vertices;var normals=mesh.normals;
   for(int j=0;j<vertices.Length;j++){vertices[j]=matrix.MultiplyPoint3x4(vertices[j]);normals[j]=normalMatrix.MultiplyVector(normals[j]).normalized;}mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();
   float scale=1/Mathf.Max(mesh.bounds.size.x,mesh.bounds.size.z);var offset=new Vector3(mesh.bounds.center.x,mesh.bounds.min.y,mesh.bounds.center.z);for(int j=0;j<vertices.Length;j++)vertices[j]=(vertices[j]-offset)*scale;
   mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.name=$"Individual river stone {i+1}";assets.meshes[i]=mesh;AssetDatabase.CreateAsset(mesh,Root+$"/Stone{i:00}.asset");
  }
  AssetDatabase.CreateAsset(assets,Root+"/Stones.asset");AssetDatabase.SaveAssets();
 }
}
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using CityForge.World;
namespace CityForge.Editor {
public static class StreamRockAssetBuilder {
 const string Root="Assets/CityForge/Resources/StreamRocks";
 public static void Build(){
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Prepare rocks in Edit mode.");
  if(File.Exists(Root+"/Rocks.asset"))throw new InvalidOperationException("Rock assets already prepared; preserve before rebuilding.");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  foreach(string file in new[]{"riverRockDiffuse.png","riverRockNormal.png"}){
   var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/"+file);
   importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.anisoLevel=4;
   importer.sRGBTexture=file.Contains("Diffuse");if(file.Contains("Normal"))importer.textureType=TextureImporterType.NormalMap;
   importer.SaveAndReimport();
  }
  string path=Root+"/RiverRockPatch.fbx";
  var modelImporter=(ModelImporter)AssetImporter.GetAtPath(path);
  modelImporter.materialImportMode=ModelImporterMaterialImportMode.None;modelImporter.importAnimation=false;modelImporter.importCameras=false;modelImporter.importLights=false;modelImporter.SaveAndReimport();
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var filter=model.GetComponentInChildren<MeshFilter>();
  var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name="Purchased riverbed scan — 8m patch";
  var matrix=filter.transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;var vertices=mesh.vertices;var normals=mesh.normals;
  for(int i=0;i<vertices.Length;i++){vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);normals[i]=normalMatrix.MultiplyVector(normals[i]).normalized;}
  mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();mesh.RecalculateTangents();
  // Use metres regardless of the FBX importer's unit conversion.
  float scale=7.84f/Mathf.Max(mesh.bounds.size.x,mesh.bounds.size.z);var center=mesh.bounds.center;float bottom=mesh.bounds.min.y;
  vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-new Vector3(center.x,bottom,center.z))*scale;
  mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,Root+"/Patch.asset");
  var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Scanned stream rocks",enableInstancing=true};
  material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/riverRockDiffuse.png"));
  material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/riverRockNormal.png"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.75f);material.SetFloat("_Smoothness",.16f);material.SetFloat("_Metallic",0);
  AssetDatabase.CreateAsset(material,Root+"/Rocks.mat");
  var assets=ScriptableObject.CreateInstance<RiverBedRockAssets>();assets.meshes=new[]{mesh};assets.materials=new[]{material};assets.mossy=new[]{false};AssetDatabase.CreateAsset(assets,Root+"/Rocks.asset");AssetDatabase.SaveAssets();
 }
}
}
#endif

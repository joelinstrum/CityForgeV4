using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using CityForge.World;
namespace CityForge.Editor
{
 public static class DenseForestCloseLODBuilder
 {
  const string Root="Assets/CityForge/Art/Trees/DenseForestTrees/CloseLOD0/";
  static T Save<T>(T value,string path) where T:UnityEngine.Object {value.name=Path.GetFileNameWithoutExtension(path);var existing=AssetDatabase.LoadAssetAtPath<T>(path);if(existing){EditorUtility.CopySerialized(value,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(value);return existing;}AssetDatabase.CreateAsset(value,path);return value;}
  [MenuItem("CityForge/Dense Forest Trees/Build Close LOD0 Treatment")]
  public static void Build(){
   AssetDatabase.Refresh();
   foreach(var name in new[]{"Hardwood_Albedo.png","Hardwood_Normal.png","Hardwood_Roughness.png","Hardwood_MetalSmooth.png"}){var imp=(TextureImporter)AssetImporter.GetAtPath(Root+name);imp.textureType=name.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;imp.sRGBTexture=name.Contains("Albedo");imp.mipmapEnabled=true;imp.anisoLevel=8;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();}
   var bark=new Material(Shader.Find("Universal Render Pipeline/Lit"));bark.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Hardwood_Albedo.png"));bark.SetColor("_BaseColor",Color.white);bark.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Hardwood_Normal.png"));bark.SetFloat("_BumpScale",.65f);bark.EnableKeyword("_NORMALMAP");bark.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Hardwood_MetalSmooth.png"));bark.EnableKeyword("_METALLICSPECGLOSSMAP");bark.SetFloat("_Smoothness",1f);bark.SetColor("_EmissionColor",new Color(.16f,.145f,.125f));bark.EnableKeyword("_EMISSION");bark.enableInstancing=true;bark=Save(bark,Root+"MatureHardwood.mat");
   var source=AssetDatabase.LoadAssetAtPath<DenseForestTreeFamily>("Assets/CityForge/Resources/DenseForestTrees.asset");var family=UnityEngine.Object.Instantiate(source);
   family.springSummer=(BroadleafGameplayLODLibrary[])source.springSummer.Clone();family.fall=(BroadleafGameplayLODLibrary[])source.fall.Clone();family.winter=(BroadleafGameplayLODLibrary[])source.winter.Clone();
   for(int season=0;season<3;season++)for(int v=0;v<4;v++){
    var original=source.Libraries((DenseForestSeason)season)[v];var label=DenseForestFamilyBuilder.Names[v]+"_"+new[]{"Summer","Fall","Winter"}[season];var folder=Root+label+"/";var path=folder+"LOD0.fbx";
    var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.isReadable=true;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.importCameras=false;imp.importLights=false;imp.importAnimation=false;imp.SaveAndReimport();
    var stage=new GameObject("Close LOD0 import");var tree=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),stage.transform);
    try{
     foreach(var r in tree.GetComponentsInChildren<MeshRenderer>()){bool leaf=r.name.ToLowerInvariant().Contains("foliage"),snow=r.name.ToLowerInvariant().Contains("snow");var mats=r.sharedMaterials;for(int j=0;j<mats.Length;j++)mats[j]=leaf?AssetDatabase.LoadAssetAtPath<Material>(DenseForestFamilyBuilder.Art+(season==1?"FallLeaf":"SummerLeaf")+Mathf.Min(j,2)+".mat"):snow?AssetDatabase.LoadAssetAtPath<Material>(DenseForestFamilyBuilder.Art+"BranchSnow.mat"):bark;r.sharedMaterials=mats;}
     if(season!=2){var g=stage.AddComponent<FrozenCrownLighting>();g.Initialize(AssetDatabase.LoadAssetAtPath<TextAsset>(DenseForestFamilyBuilder.Art+label+"/LOD0_Guides.bytes"));if(g.MissingVertices!=0)throw new Exception("Close guides unmatched");}
     foreach(var r in tree.GetComponentsInChildren<MeshRenderer>())if(r.name.ToLowerInvariant().Contains("foliage")){var mats=r.sharedMaterials;for(int j=0;j<mats.Length;j++){var mat=new Material(mats[j]);mat.shader=Shader.Find("CityForge/Trees/Close Leaf Clusters");mat.SetFloat("_LobeNormalBlend",.35f);mat.SetFloat("_DiffuseSkyFill",.50f);mat.SetFloat("_DistanceCoverage",0);mat.enableInstancing=true;mats[j]=Save(mat,folder+"Leaf"+j+".mat");}r.sharedMaterials=mats;}
     foreach(var f in tree.GetComponentsInChildren<MeshFilter>())f.sharedMesh=Save(UnityEngine.Object.Instantiate(f.sharedMesh),folder+f.name+".asset");
     var prefab=PrefabUtility.SaveAsPrefabAsset(tree,folder+"LOD0.prefab");var lib=UnityEngine.Object.Instantiate(original);lib.meshTiers=(GameObject[])original.meshTiers.Clone();lib.meshTiers[0]=prefab;family.Libraries((DenseForestSeason)season)[v]=Save(lib,folder+"GameplayLOD.asset");
    }finally{UnityEngine.Object.DestroyImmediate(stage);}
   }
   Save(family,"Assets/CityForge/Resources/DenseForestTreesClose.asset");AssetDatabase.SaveAssets();Debug.Log("CLOSE LOD0 BUILD COMPLETE: existing LOD1/2 and atlas references retained.");
  }
 }
}

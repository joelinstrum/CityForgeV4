using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using CityForge.World;
namespace CityForge.Editor
{
 public static class DenseForestFamilyBuilder
 {
  public const string Art="Assets/CityForge/Art/Trees/DenseForestTrees/";
  public static readonly string[] Names={"A_Rounded","B_Tall","C_Full","D_HighCrown"};
  static T Save<T>(T value,string path) where T:UnityEngine.Object {value.name=Path.GetFileNameWithoutExtension(path);var existing=AssetDatabase.LoadAssetAtPath<T>(path);if(existing!=null){EditorUtility.CopySerialized(value,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(value);return existing;}AssetDatabase.CreateAsset(value,path);return value;}
  public static DenseForestTreeFamily Build(Camera camera)
  {
   var family=ScriptableObject.CreateInstance<DenseForestTreeFamily>();family.variantNames=new[]{"Rounded baseline","Tall oval","Broad spreading","Uneven multi-leader"};family.springSummer=new BroadleafGameplayLODLibrary[4];family.fall=new BroadleafGameplayLODLibrary[4];family.winter=new BroadleafGameplayLODLibrary[4];family.summerPrefabs=new GameObject[4];family.fallPrefabs=new GameObject[4];family.winterPrefabs=new GameObject[4];
   ConfigureTexture(Art+"Fall_Leaf.png");
   var bark=new Material(AssetDatabase.LoadAssetAtPath<Material>(BroadleafGameplayLODBaker.Art+"FrozenBark.mat"));bark.enableInstancing=true;bark.EnableKeyword("_EMISSION");bark.SetColor("_EmissionColor",new Color(.10f,.092f,.082f,1));bark.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;bark=Save(bark,Art+"ForestBark.mat");
   var snow=new Material(Shader.Find("Universal Render Pipeline/Lit"));snow.SetColor("_BaseColor",new Color(.72f,.77f,.80f,1).gamma);snow.SetFloat("_Smoothness",.02f);snow.enableInstancing=true;snow=Save(snow,Art+"BranchSnow.mat");
   var summer=new Material[3];var autumn=new Material[3];var colors=new[]{new Color(.44f,.34f,.13f,1),new Color(.33f,.20f,.105f,1),new Color(.29f,.32f,.18f,1)};
   for(int j=0;j<3;j++){
    summer[j]=new Material(AssetDatabase.LoadAssetAtPath<Material>(BroadleafGameplayLODBaker.Folder+"GameplayLeaf"+j+".mat"));summer[j].enableInstancing=true;summer[j]=Save(summer[j],Art+"SummerLeaf"+j+".mat");
    autumn[j]=new Material(summer[j]);autumn[j].SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Fall_Leaf.png"));autumn[j].SetColor("_BaseColor",colors[j]);autumn[j]=Save(autumn[j],Art+"FallLeaf"+j+".mat");
   }
   for(int season=0;season<3;season++)for(int v=0;v<4;v++){
    string label=Names[v]+"_"+new[]{"Summer","Fall","Winter"}[season],folder=Art+label+"/";
    var lib=ScriptableObject.CreateInstance<BroadleafGameplayLODLibrary>();lib.meshTiers=new GameObject[3];
    for(int tier=0;tier<3;tier++){
     var path=folder+"LOD"+tier+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.SaveAndReimport();
     var stage=new GameObject("Family import guide stage");var tree=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),stage.transform);tree.name=label+"_LOD"+tier;
     try{
      foreach(var renderer in tree.GetComponentsInChildren<MeshRenderer>()){
       bool foliage=renderer.name.ToLowerInvariant().Contains("foliage"),isSnow=renderer.name.ToLowerInvariant().Contains("snow");var mats=renderer.sharedMaterials;
       for(int j=0;j<mats.Length;j++)mats[j]=foliage?(season==1?autumn:summer)[Mathf.Min(j,2)]:isSnow?snow:bark;renderer.sharedMaterials=mats;renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
      }
      if(season!=2){var guide=stage.AddComponent<FrozenCrownLighting>();guide.Initialize(AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"LOD"+tier+"_Guides.bytes"));if(guide.MissingVertices!=0)throw new Exception("Unmatched family lighting guides: "+label);}
      foreach(var filter in tree.GetComponentsInChildren<MeshFilter>()){var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh=Save(mesh,folder+"LOD"+tier+"_"+filter.name+".asset");filter.sharedMesh=mesh;}
      lib.meshTiers[tier]=PrefabUtility.SaveAsPrefabAsset(tree,folder+"LOD"+tier+".prefab");
     }finally{UnityEngine.Object.DestroyImmediate(stage);}
    }
    // Reuse accepted gameplay-atlas baker and runtime library, never overwrite the accepted baseline atlas.
    BroadleafGameplayLODBaker.BakeAtlas(camera,lib,lib.meshTiers[0],folder);lib=Save(lib,folder+"GameplayLOD.asset");
    var root=new GameObject(label);var levels=new LOD[4];
    for(int tier=0;tier<3;tier++){var child=UnityEngine.Object.Instantiate(lib.meshTiers[tier],root.transform);levels[tier]=new LOD(new[]{.12f,.06f,.015f}[tier],child.GetComponentsInChildren<Renderer>());}
    // Standard prefab stops at LOD2; zoom-aware forest use goes through BroadleafGameplayLOD and its baked atlas.
    var group=root.AddComponent<LODGroup>();group.SetLODs(levels.Take(3).ToArray());group.RecalculateBounds();Directory.CreateDirectory("Assets/CityForge/Resources/Trees/DenseForestTrees");var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/CityForge/Resources/Trees/DenseForestTrees/"+label+".prefab");UnityEngine.Object.DestroyImmediate(root);
    if(season==0){family.springSummer[v]=lib;family.summerPrefabs[v]=prefab;}else if(season==1){family.fall[v]=lib;family.fallPrefabs[v]=prefab;}else{family.winter[v]=lib;family.winterPrefabs[v]=prefab;}
    Debug.Log("DENSE FOREST BUILT "+label);
   }
   family=Save(family,"Assets/CityForge/Resources/DenseForestTrees.asset");AssetDatabase.SaveAssets();return family;
  }
  static void ConfigureTexture(string path){var t=(TextureImporter)AssetImporter.GetAtPath(path);t.alphaIsTransparency=true;t.mipmapEnabled=true;t.mipMapsPreserveCoverage=true;t.alphaTestReferenceValue=.3f;t.textureCompression=TextureImporterCompression.Uncompressed;t.SaveAndReimport();}
 }
}

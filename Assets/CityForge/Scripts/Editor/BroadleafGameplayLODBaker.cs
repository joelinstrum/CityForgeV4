using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CityForge.World;
namespace CityForge.Editor
{
 public static class BroadleafGameplayLODBaker
 {
  public const string Art="Assets/CityForge/Art/Trees/FrozenBroadleafEvaluation/";
  public const string Folder=Art+"GameplayLOD/";
  public static BroadleafGameplayLODLibrary Build(Camera gameplay)
  {
   var library=ScriptableObject.CreateInstance<BroadleafGameplayLODLibrary>();library.meshTiers=new GameObject[3];
   var leafMats=new Material[3];for(int j=0;j<3;j++){leafMats[j]=new Material(AssetDatabase.LoadAssetAtPath<Material>(Art+"FrozenLeaf"+j+".mat"));leafMats[j].enableInstancing=true;leafMats[j].SetFloat("_FoliageLighting",1);leafMats[j].SetFloat("_LobeNormalBlend",.70f);leafMats[j].SetFloat("_DiffuseSkyFill",.65f);AssetDatabase.CreateAsset(leafMats[j],Folder+"GameplayLeaf"+j+".mat");}
   var bark=new Material(AssetDatabase.LoadAssetAtPath<Material>(Art+"FrozenBark.mat"));bark.enableInstancing=true;bark.EnableKeyword("_EMISSION");bark.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;AssetDatabase.CreateAsset(bark,Folder+"GameplayBark.mat");
   for(int tier=0;tier<3;tier++)
   {
    var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"Broadleaf_LOD"+tier+".fbx");importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
    var parent=new GameObject("LOD material guide stage");var tree=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Broadleaf_LOD"+tier+".fbx"),parent.transform);tree.transform.localPosition=Vector3.zero;
    foreach(var r in tree.GetComponentsInChildren<MeshRenderer>()){var mats=r.sharedMaterials;bool wood=r.name.ToLowerInvariant().Contains("trunk");for(int j=0;j<mats.Length;j++)mats[j]=wood?bark:leafMats[Mathf.Min(j,2)];r.sharedMaterials=mats;r.shadowCastingMode=ShadowCastingMode.TwoSided;}
    var guides=parent.AddComponent<FrozenCrownLighting>();guides.Initialize(AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"LOD"+tier+"_Guides.bytes"));
    foreach(var filter in tree.GetComponentsInChildren<MeshFilter>()){var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);AssetDatabase.CreateAsset(mesh,Folder+"LOD"+tier+"_"+(filter.name.Contains("trunk")?"Wood":"Leaf")+".asset");filter.sharedMesh=mesh;}
    library.meshTiers[tier]=PrefabUtility.SaveAsPrefabAsset(tree,Folder+"Broadleaf_LOD"+tier+".prefab");UnityEngine.Object.DestroyImmediate(parent);
   }
   BakeAtlas(gameplay,library);
   AssetDatabase.CreateAsset(library,"Assets/CityForge/Resources/BroadleafGameplayLOD.asset");AssetDatabase.SaveAssets();return library;
  }
  public static BroadleafCanopyCluster BuildCluster(Camera gameplay,Renderer[] source)
  {
   var cluster=ScriptableObject.CreateInstance<BroadleafCanopyCluster>();var bounds=source[0].bounds;foreach(var r in source)bounds.Encapsulate(r.bounds);cluster.center=bounds.center;cluster.span=Mathf.Sqrt(bounds.size.x*bounds.size.x+bounds.size.z*bounds.size.z)+bounds.size.y;cluster.placementSha256=BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes("/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/UnityOrganicScatter/placements.json"))).Replace("-","").ToLowerInvariant();
   var layers=source.Select(r=>r.gameObject.layer).ToArray();var blocks=source.Select(r=>{var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);return b;}).ToArray();var shadows=source.Select(r=>r.shadowCastingMode).ToArray();
   var camera=new GameObject("Accepted whole stand zoom5 bake").AddComponent<Camera>();camera.CopyFrom(gameplay);camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=600;camera.orthographicSize=cluster.span/2;camera.aspect=1;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
   // Match the accepted zoom5 capture's world units per pixel, rather than adding bright fine detail.
   float referenceHalfHeight=gameplay.orthographicSize*Mathf.Pow(2,5-gameplay.GetComponent<CityForge.CameraSystem.CityForgeCameraController>().ZoomLevel);
   int size=Mathf.CeilToInt(cluster.span/(referenceHalfHeight*2/1080));size=Mathf.Clamp(size,128,512);
   var rt=new RenderTexture(size,size,24,RenderTextureFormat.ARGB32);rt.Create();var tile=new Texture2D(size,size,TextureFormat.RGBA32,false);var atlas=new Texture2D(size*4,size,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
   try{
    foreach(var r in source){r.enabled=true;r.gameObject.layer=31;r.shadowCastingMode=ShadowCastingMode.Off;foreach(var m in r.sharedMaterials)if(m.shader.name=="Universal Render Pipeline/Lit")m.EnableKeyword("_EMISSION");var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);b.SetFloat("_DistanceCoverage",1);b.SetFloat("_FarCanopyPresentation",1);b.SetFloat("_FoliageLighting",1);b.SetFloat("_LobeNormalBlend",.70f);b.SetFloat("_DiffuseSkyFill",.65f);if(r.sharedMaterials[0].shader.name=="Universal Render Pipeline/Lit"){b.SetColor("_BaseColor",new Color(.26f,.235f,.205f,1).gamma);b.SetColor("_EmissionColor",new Color(.10f,.092f,.082f,1));}r.SetPropertyBlock(b);}
    for(int view=0;view<4;view++){camera.transform.rotation=Quaternion.Euler(gameplay.transform.eulerAngles.x,45+view*90,0);camera.transform.position=cluster.center-camera.transform.forward*250;RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;tile.ReadPixels(new Rect(0,0,size,size),0,0);tile.Apply();atlas.SetPixels(view*size,0,size,size,tile.GetPixels());}
    atlas.Apply();var path=Folder+"Organic320Cluster.png";File.WriteAllBytes(path,atlas.EncodeToPNG());AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();cluster.material=new Material(Shader.Find("CityForge/Gameplay Broadleaf Impostor"));cluster.material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));AssetDatabase.CreateAsset(cluster.material,Folder+"Organic320Cluster.mat");AssetDatabase.CreateAsset(cluster,"Assets/CityForge/Resources/Organic320Canopy.asset");AssetDatabase.SaveAssets();return cluster;
   }finally{for(int i=0;i<source.Length;i++){source[i].gameObject.layer=layers[i];source[i].shadowCastingMode=shadows[i];source[i].SetPropertyBlock(blocks[i]);}RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tile);UnityEngine.Object.DestroyImmediate(atlas);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
  }
  public static void BakeAtlas(Camera gameplay,BroadleafGameplayLODLibrary library,GameObject sourcePrefab=null,string destination=Folder)
  {
   var stage=new GameObject("Accepted tree impostor bake");stage.transform.position=new Vector3(631,510,159);var tree=UnityEngine.Object.Instantiate(sourcePrefab!=null?sourcePrefab:AssetDatabase.LoadAssetAtPath<GameObject>(Art+"FrozenBroadleaf.prefab"),stage.transform);tree.transform.localPosition=Vector3.zero;tree.transform.localRotation=Quaternion.identity;tree.transform.localScale=Vector3.one;
   var renderers=tree.GetComponentsInChildren<Renderer>();foreach(var r in renderers){r.gameObject.layer=31;r.shadowCastingMode=ShadowCastingMode.Off;var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);block.SetFloat("_DistanceCoverage",1);block.SetFloat("_FarCanopyPresentation",1);block.SetFloat("_FoliageLighting",1);block.SetFloat("_LobeNormalBlend",.70f);block.SetFloat("_DiffuseSkyFill",.65f);r.SetPropertyBlock(block);}
   if(sourcePrefab==null){var guides=stage.AddComponent<FrozenCrownLighting>();guides.Initialize(AssetDatabase.LoadAssetAtPath<TextAsset>(Art+"Frozen_CrownLightingGuides.bytes"));}
   var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);library.center=tree.transform.InverseTransformPoint(bounds.center);library.span=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z)*1.55f;
   var camera=new GameObject("Gameplay atlas camera").AddComponent<Camera>();camera.CopyFrom(gameplay);camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.nearClipPlane=.1f;camera.farClipPlane=200;camera.orthographic=true;camera.orthographicSize=library.span*.5f;camera.aspect=1;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
   const int size=256;var rt=new RenderTexture(size,size,24,RenderTextureFormat.ARGB32);rt.Create();var atlas=new Texture2D(size*8,size*4,TextureFormat.RGBA32,false);var tile=new Texture2D(size,size,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
   try{for(int view=0;view<4;view++)for(int yaw=0;yaw<8;yaw++){
    tree.transform.localRotation=Quaternion.Euler(0,yaw*45,0);camera.transform.rotation=Quaternion.Euler(gameplay.transform.eulerAngles.x,45+view*90,0);camera.transform.position=tree.transform.TransformPoint(library.center)-camera.transform.forward*70;
    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;tile.ReadPixels(new Rect(0,0,size,size),0,0);tile.Apply();atlas.SetPixels(yaw*size,view*size,size,size,tile.GetPixels());
   }atlas.Apply();if(!atlas.GetPixels32().Any(pixel=>pixel.a>0))throw new Exception("Blank canopy atlas: offscreen render/readback failed. Existing texture is retained.");var path=destination+"AcceptedCanopyAtlas.png";File.WriteAllBytes(path,atlas.EncodeToPNG());AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=true;importer.alphaTestReferenceValue=.15f;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();library.impostorMaterial=AssetDatabase.LoadAssetAtPath<Material>(destination+"AcceptedCanopy.mat") ?? new Material(Shader.Find("CityForge/Gameplay Broadleaf Impostor"));library.impostorMaterial.SetFloat("_BakedColorGain",.70f);library.impostorMaterial.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(library.impostorMaterial)))AssetDatabase.CreateAsset(library.impostorMaterial,destination+"AcceptedCanopy.mat");else EditorUtility.SetDirty(library.impostorMaterial);}
   finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tile);UnityEngine.Object.DestroyImmediate(atlas);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(stage);}
  }
 }
}

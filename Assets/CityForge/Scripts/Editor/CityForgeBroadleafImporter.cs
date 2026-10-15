using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using CityForge.World;
namespace CityForge.Editor
{
 public static class CityForgeBroadleafImporter
 {
  const string Art="Assets/CityForge/Art/Trees/CityForgeBroadleaf/";
  const string Prefabs="Assets/CityForge/Resources/Trees/CityForgeBroadleaf/";
  [MenuItem("CityForge/Custom Broadleaf/Import Bake And Select")]
  public static void Build()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing trees.");
   Directory.CreateDirectory(Prefabs);AssetDatabase.Refresh();
   var ti=(TextureImporter)AssetImporter.GetAtPath(Art+"Forge_Leaf.png");ti.alphaIsTransparency=true;ti.mipmapEnabled=true;ti.mipMapsPreserveCoverage=true;ti.alphaTestReferenceValue=.3f;ti.anisoLevel=4;ti.SaveAndReimport();
   var bark=AssetDatabase.LoadAssetAtPath<Material>(Art+"Forge_Bark.mat");
   if(bark==null){bark=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(bark,Art+"Forge_Bark.mat");}
   bark.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Forge_Bark.png"));bark.SetColor("_BaseColor",new Color(.8f,.8f,.8f,1));bark.SetFloat("_Smoothness",.08f);bark.enableInstancing=true;EditorUtility.SetDirty(bark);
   foreach(var suffix in new[]{"A","B","C"})
   {
    string name="ForgeBroadleaf"+suffix;var root=new GameObject(name);
    try
    {
     var levels=new LOD[3];Material leaf=null;
     for(int level=0;level<3;level++)
     {
      var path=Art+name+"_LOD"+level+".fbx";var mi=(ModelImporter)AssetImporter.GetAtPath(path);mi.isReadable=true;mi.importCameras=false;mi.importLights=false;mi.importAnimation=false;mi.SaveAndReimport();
      var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.transform.SetParent(root.transform,false);
      var renderers=model.GetComponentsInChildren<MeshRenderer>();
      foreach(var renderer in renderers)
      {
       var slots=renderer.sharedMaterials;var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
       for(int slot=0;slot<slots.Length;slot++)
       {
        if(slots[slot].name.Contains("Leaf"))
        {
         if(leaf==null)
         {
          var vertices=mesh.vertices;var indices=mesh.GetIndices(slot);var bounds=new Bounds(vertices[indices[0]],Vector3.zero);foreach(var index in indices)bounds.Encapsulate(vertices[index]);
          leaf=ForestFoliageMaterials.Create(Art+name+"_Leaf.mat",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Forge_Leaf.png"),bounds);
          leaf.SetColor("_BaseColor",new Color(.55f,.68f,.46f,1));leaf.SetFloat("_CrownNormalBlend",.6f);leaf.SetFloat("_SkyFill",1.25f);leaf.SetFloat("_Transmission",.07f);leaf.SetFloat("_ShadowFill",.20f);EditorUtility.SetDirty(leaf);
         }
         slots[slot]=leaf;
        }
        else if(slots[slot].name.Contains("Bark"))slots[slot]=bark;
        else throw new InvalidOperationException("Unexpected broadleaf material "+slots[slot].name);
       }
       renderer.sharedMaterials=slots;renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
      }
      levels[level]=new LOD(level==0?.1f:level==1?.04f:.001f,renderers);
     }
     var lod=root.AddComponent<LODGroup>();lod.SetLODs(levels);lod.RecalculateBounds();PrefabUtility.SaveAsPrefabAsset(root,Prefabs+name+".prefab");
    }
    finally{UnityEngine.Object.DestroyImmediate(root);}
   }
   AssetDatabase.SaveAssets();TreeImpostorBaker.Bake("Trees/CityForgeBroadleaf");
   foreach(var suffix in new[]{"A","B","C"})
   {
    var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/CityForge/Art/Trees/ForgeBroadleaf"+suffix+"_Impostor.mat");material.SetColor("_BaseColor",new Color(.60f,.68f,.60f,1));EditorUtility.SetDirty(material);
   }
   AssetDatabase.SaveAssets();var settings=ForestPresentationSettings.Load();settings.resourceFolder="Trees/CityForgeBroadleaf";settings.fullCrownThroughZoom=1;File.WriteAllText("Assets/CityForge/Resources/Trees/ForestPresentation.json",JsonUtility.ToJson(settings,true)+"\n");AssetDatabase.Refresh();
   Debug.Log("CityForge: three authored Easy Tree broadleaf variants imported, baked and selected.");
  }
  [MenuItem("CityForge/Custom Broadleaf/Capture Broadleaf View")]
  public static void Capture()
  {
   var rig=UnityEngine.Object.FindFirstObjectByType<CityForge.CameraSystem.CityForgeCameraController>();if(!EditorApplication.isPlaying || rig==null)throw new InvalidOperationException("Preview first.");
   ScreenCapture.CaptureScreenshot("Documentation/03_Technical/Screenshots/Forge_Broadleaf_Zoom"+rig.ZoomLevel+".png");
  }
 }
}

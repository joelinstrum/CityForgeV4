#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.Lots;
using CityForge.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CityForge.EditorTools
{
 internal static class ArtDecoCharacterPreparation
 {
  internal const string Root="Assets/CityForge/Resources/Lots/ArtDecoCharacters/";
  [Serializable]internal sealed class Character{public string id,label,folder,albedo,normal;}
  [Serializable]sealed class Pack{public Character[] characters;}
  static Character[] Characters=>JsonUtility.FromJson<Pack>(Resources.Load<TextAsset>("Config/ArtDecoCharacters").text).characters;
  [MenuItem("CityForge/Import/Prepare Art Deco Characters")]
  public static void Prepare()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play Mode before preparing characters.");
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   foreach(var c in Characters)
   {
    var importer=AssetImporter.GetAtPath(Root+c.folder+"/Model.fbx") as ModelImporter;if(!importer)throw new InvalidOperationException("Missing character: "+c.id);
    importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
    importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
    var clips=importer.defaultClipAnimations;
    foreach(var clip in clips){clip.loopTime=true;clip.loopPose=true;clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;clip.keepOriginalPositionY=true;}
    if(clips.Length>0){importer.clipAnimations=clips;importer.SaveAndReimport();}
    var avatar=AssetDatabase.LoadAllAssetsAtPath(importer.assetPath).OfType<Avatar>().FirstOrDefault();if(!avatar||!avatar.isValid||!avatar.isHuman)throw new InvalidOperationException("Invalid humanoid avatar: "+c.id);
   }
   foreach(var c in Characters)
   {
    string folder=Root+c.folder+"/",path=folder+"Model.fbx";
    AnimationClip Clip(string state)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(a=>!a.name.StartsWith("__")&&a.name.Contains(state,StringComparison.OrdinalIgnoreCase));
    // Two supplied rigs lack takes. Humanoid retargeting uses the same pack's male walk/idle.
    AnimationClip Donor(string state)=>AssetDatabase.LoadAllAssetsAtPath(Root+"man-brown/Model.fbx").OfType<AnimationClip>().First(a=>!a.name.StartsWith("__")&&a.name.Contains(state,StringComparison.OrdinalIgnoreCase));
    var idle=Clip("idle")??Donor("idle");var walk=Clip("walk")??Donor("walk");
    var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"Character.controller");
    if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(folder+"Character.controller");
    var machine=controller.layers[0].stateMachine;
    foreach(var state in machine.states)machine.RemoveState(state.state);
    var idleState=machine.AddState("Idle");idleState.motion=idle;machine.defaultState=idleState;machine.AddState("Walk").motion=walk;EditorUtility.SetDirty(controller);
    var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"Character.mat");
    if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,folder+"Character.mat");}
    material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+c.albedo));material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",.18f);material.SetFloat("_Metallic",0);
    if(!string.IsNullOrEmpty(c.normal))
    {
     if(AssetImporter.GetAtPath(folder+c.normal) is TextureImporter normal&&normal.textureType!=TextureImporterType.NormalMap){normal.textureType=TextureImporterType.NormalMap;normal.SaveAndReimport();}
     material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+c.normal));material.SetFloat("_BumpScale",.65f);material.EnableKeyword("_NORMALMAP");
    }
    EditorUtility.SetDirty(material);
    var root=new GameObject(c.label);
    try
    {
     var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);model.name="Model";
     var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
     float scale=1.75f/Mathf.Max(.001f,bounds.size.y);model.transform.localScale*=scale;model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
     foreach(var renderer in renderers)renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
     var animator=model.GetComponent<Animator>()??model.AddComponent<Animator>();animator.avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().First();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
     PrefabUtility.SaveAsPrefabAsset(root,folder+"Character.prefab");
    }finally{Object.DestroyImmediate(root);}
   }
   AssetDatabase.SaveAssets();typeof(LotCatalogSetup).GetMethod("EnsureCatalog",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);Debug.Log("ART DECO CHARACTERS PREPARED");
  }
  internal static IEnumerable<LotAssetCatalogEntry> CatalogEntries()
  {
   foreach(var c in Characters)
   {
    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+c.folder+"/Character.prefab");if(!prefab)continue;
    yield return new LotAssetCatalogEntry{id=c.id,label=c.label,description="Art Deco pedestrian · walking and idle animations.",category="Characters",group=CityForgeTimeline.Find("modern").Name,eraId="modern",fromYear=1921,toYear=1945,prefab=prefab,footprint=Vector2.one*.7f,idleState="Idle",playerPlaceable=true};
   }
  }
 }
}
#endif

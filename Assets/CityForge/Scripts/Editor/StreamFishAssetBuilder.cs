#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.Animations;using UnityEngine;
namespace CityForge.Editor {
public static class StreamFishAssetBuilder {
 const string Root="Assets/CityForge/Resources/StreamFish/";
 public static void Build(){
  if(EditorApplication.isPlaying||File.Exists(Root+"BluegillFish.prefab"))throw new InvalidOperationException("Prepare once in Edit mode; preserve existing fish output.");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  string textures=Root+"BLUEGILL_TEXTURES/";
  foreach(string name in new[]{"BLUEGILL_COLOR.png","BLUEGILL_ALPHA.png","BLUEGILL_NORMAL.png"}){var importer=(TextureImporter)AssetImporter.GetAtPath(textures+name);importer.maxTextureSize=2048;importer.sRGBTexture=name.Contains("COLOR");importer.isReadable=!name.Contains("NORMAL");if(name.Contains("NORMAL"))importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();}
  var color=AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"BLUEGILL_COLOR.png");var alpha=AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"BLUEGILL_ALPHA.png");var pixels=color.GetPixels32();
  for(int y=0;y<color.height;y++)for(int x=0;x<color.width;x++){int i=y*color.width+x;pixels[i].a=(byte)Mathf.RoundToInt(alpha.GetPixelBilinear((x+.5f)/color.width,(y+.5f)/color.height).r*255);}
  var packed=new Texture2D(color.width,color.height,TextureFormat.RGBA32,false);packed.SetPixels32(pixels);packed.Apply();File.WriteAllBytes(Root+"ColorAlpha.png",packed.EncodeToPNG());UnityEngine.Object.DestroyImmediate(packed);AssetDatabase.ImportAsset(Root+"ColorAlpha.png",ImportAssetOptions.ForceSynchronousImport);
  var texImporter=(TextureImporter)AssetImporter.GetAtPath(Root+"ColorAlpha.png");texImporter.alphaSource=TextureImporterAlphaSource.FromInput;texImporter.alphaIsTransparency=true;texImporter.maxTextureSize=2048;texImporter.mipmapEnabled=true;texImporter.SaveAndReimport();
  var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Bluegill skin and cutout fins"};material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"ColorAlpha.png"));material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"BLUEGILL_NORMAL.png"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.45f);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.28f);material.SetFloat("_AlphaClip",1);material.EnableKeyword("_ALPHATEST_ON");material.SetFloat("_Cutoff",.25f);material.SetFloat("_Cull",0);material.SetOverrideTag("RenderType","TransparentCutout");material.renderQueue=2450;AssetDatabase.CreateAsset(material,Root+"Bluegill.mat");
  var model=(ModelImporter)AssetImporter.GetAtPath(Root+"Bluegill.fbx");model.materialImportMode=ModelImporterMaterialImportMode.None;model.importAnimation=true;model.animationType=ModelImporterAnimationType.Generic;model.importCameras=false;model.importLights=false;model.SaveAndReimport();
  var clips=model.defaultClipAnimations;foreach(var c in clips){c.loopTime=true;c.loopPose=true;}model.clipAnimations=clips;model.SaveAndReimport();
  var clip=AssetDatabase.LoadAllAssetsAtPath(Root+"Bluegill.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
  var controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"Bluegill.controller");var state=controller.layers[0].stateMachine.AddState("Swim");state.motion=clip;controller.layers[0].stateMachine.defaultState=state;
  var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Bluegill.fbx"));instance.name="Bluegill";
  foreach(var renderer in instance.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
  var animator=instance.GetComponent<Animator>();if(!animator)animator=instance.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
  var root=new GameObject("BluegillFish");instance.transform.SetParent(root.transform,false);instance.transform.localRotation=Quaternion.Euler(90,0,0);PrefabUtility.SaveAsPrefabAsset(root,Root+"BluegillFish.prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
 }
}
}
#endif

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.Editor
{
    public static class SiberianElmImporter
    {
        private const string Art = "Assets/CityForge/Art/Trees/SiberianElm/";
        private const string Prefabs = "Assets/CityForge/Resources/Trees/SiberianElm/";
        [MenuItem("CityForge/Import Siberian Elms")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before importing elms.");
            Directory.CreateDirectory(Prefabs); AssetDatabase.Refresh();
            var bark=Material("Bark",false); var leaves=Material("Leaf",true);
            foreach(var variant in new[]{"A","Large","Young"})
            {
                var name="SiberianElm"+variant; var root=new GameObject(name);
                Material crownLeaves=null;
                try
                {
                    var levels=new LOD[3];
                    for(var level=0;level<3;level++)
                    {
                        var path=Art+name+"_LOD"+level+".fbx";
                        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                        importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                        importer.importCameras=false; importer.importLights=false; importer.importAnimation=false;
                        importer.SaveAndReimport();
                        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                        model.transform.SetParent(root.transform,false);
                        var renderers=model.GetComponentsInChildren<MeshRenderer>();
                        foreach(var renderer in renderers)
                        {
                            var slots=renderer.sharedMaterials;
                            for(var i=0;i<slots.Length;i++)
                            {
                                var slotName=slots[i]?.name ?? "";
                                if(slotName.Contains("Leaf") && crownLeaves==null)
                                {
                                    var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                                    var vertices=mesh.vertices; var indices=mesh.GetIndices(i);
                                    var bounds=new Bounds(vertices[indices[0]],Vector3.zero);
                                    foreach(var index in indices) bounds.Encapsulate(vertices[index]);
                                    crownLeaves=ForestFoliageMaterials.Create(Art+"Leaf_"+variant+".mat",leaves.GetTexture("_BaseMap"),bounds);
                                }
                                slots[i]=slotName.Contains("Bark") ? bark : slotName.Contains("Leaf") ? crownLeaves :
                                    throw new InvalidOperationException("Unmapped elm material: "+slotName);
                            }
                            renderer.sharedMaterials=slots; renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
                        }
                        levels[level]=new LOD(level==0 ? .10f : level==1 ? .04f : .001f,renderers);
                    }
                    var group=root.AddComponent<LODGroup>(); group.SetLODs(levels); group.RecalculateBounds();
                    PrefabUtility.SaveAsPrefabAsset(root,Prefabs+name+".prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            AssetDatabase.SaveAssets();
            TreeImpostorBaker.Bake("Trees/SiberianElm");
            Debug.Log("CityForge: three summer Siberian elm variants imported with three mesh LODs and eight-view distant atlases.");
        }
        [MenuItem("CityForge/Preview Elm Woodland")]
        public static void PreviewWoodland()
        {
            if(!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
            var view=UnityEngine.Object.FindFirstObjectByType<CityForge.World.PrototypeRegionView>();
            if(view==null) { PrototypeRegionChecks.Preview(); view=UnityEngine.Object.FindFirstObjectByType<CityForge.World.PrototypeRegionView>(); }
            view.EnterDistrict(3);
            var camera=view.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>();
            var focus=new Vector3(view.MapSide*.245f,0,view.MapSide*.31f);
            focus.y=view.GetComponentInChildren<Terrain>().SampleHeight(focus)+8;
            var serialized=new SerializedObject(camera); serialized.FindProperty("focus").vector3Value=focus; serialized.ApplyModifiedPropertiesWithoutUndo();
            camera.SetZoomLevel(2); view.GetComponent<CityForge.World.DistantTreeRenderer>().Refresh();
        }

        [MenuItem("CityForge/Forest Canopy/Close")]
        public static void PreviewClose() { PreviewWoodland(); SetForestZoom(1); }
        [MenuItem("CityForge/Forest Canopy/Middle")]
        public static void PreviewMiddle() { PreviewWoodland(); SetForestZoom(3); }
        [MenuItem("CityForge/Forest Canopy/Distant")]
        public static void PreviewDistant() { PreviewWoodland(); SetForestZoom(4); }
        private static void SetForestZoom(int level)
        {
            var view=UnityEngine.Object.FindFirstObjectByType<CityForge.World.PrototypeRegionView>();
            view.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>().SetZoomLevel(level);
            view.GetComponent<CityForge.World.DistantTreeRenderer>().Refresh();
        }
        [MenuItem("CityForge/Forest Canopy/Capture")]
        public static void CaptureForest()
        {
            var camera=UnityEngine.Object.FindFirstObjectByType<CityForge.CameraSystem.CityForgeCameraController>();
            if(!EditorApplication.isPlaying || camera==null) throw new InvalidOperationException("Preview the forest first.");
            Directory.CreateDirectory("Documentation/03_Technical/Screenshots");
            ScreenCapture.CaptureScreenshot("Documentation/03_Technical/Screenshots/Forest_Crown_Pass_Zoom"+camera.ZoomLevel+".png");
        }

        private static Material Material(string kind,bool cutout)
        {
            var basePath=Art+kind+"_Base.png"; var normalPath=Art+kind+"_Normal.jpg"; var maskPath=Art+kind+"_Mask.png";
            var color=(TextureImporter)AssetImporter.GetAtPath(basePath);
            color.alphaIsTransparency=cutout; color.mipmapEnabled=true; color.mipMapsPreserveCoverage=cutout;
            color.alphaTestReferenceValue=.3f; color.anisoLevel=4; color.SaveAndReimport();
            var normal=(TextureImporter)AssetImporter.GetAtPath(normalPath);
            normal.textureType=TextureImporterType.NormalMap; normal.SaveAndReimport();
            var mask=(TextureImporter)AssetImporter.GetAtPath(maskPath); mask.sRGBTexture=false; mask.SaveAndReimport();
            var path=Art+kind+".mat"; var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path); }
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(basePath));
            material.SetColor("_BaseColor",Color.white);
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath)); material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath)); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath)); material.EnableKeyword("_OCCLUSIONMAP");
            material.SetFloat("_Smoothness",.35f); material.SetFloat("_OcclusionStrength",.35f); material.enableInstancing=true;
            if(cutout)
            {
                material.SetFloat("_AlphaClip",1); material.SetFloat("_Cutoff",.3f); material.SetFloat("_Cull",0);
                material.EnableKeyword("_ALPHATEST_ON"); material.SetOverrideTag("RenderType","TransparentCutout"); material.renderQueue=2450;
            }
            EditorUtility.SetDirty(material); return material;
        }
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using CityForge.World;
namespace CityForge.Editor
{
    public static class SummerForestImporter
    {
        const string Art="Assets/CityForge/Art/Trees/SummerForest/";
        const string Prefabs="Assets/CityForge/Resources/Trees/SummerForest/";
        [MenuItem("CityForge/Summer Forest/Import And Bake")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before importing.");
            Directory.CreateDirectory(Prefabs);AssetDatabase.Refresh();
            foreach(var species in new[]{"Maple","Linden","Elm"})
            {
                var texturePath=Art+species+"_Leaf.png";
                var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                ti.alphaIsTransparency=true;ti.mipmapEnabled=true;ti.mipMapsPreserveCoverage=true;
                ti.alphaTestReferenceValue=.3f;ti.anisoLevel=4;ti.SaveAndReimport();
                var barkName=species=="Maple" ? "Maple_Bark" : "Broadleaf_Bark";
                var normal=(TextureImporter)AssetImporter.GetAtPath(Art+barkName+"_Normal.jpg");
                normal.textureType=TextureImporterType.NormalMap;normal.SaveAndReimport();
                var barkPath=Art+barkName+".mat";
                var bark=AssetDatabase.LoadAssetAtPath<Material>(barkPath);
                if(bark==null){bark=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(bark,barkPath);}
                bark.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+barkName+".jpg"));
                bark.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+barkName+"_Normal.jpg"));
                bark.EnableKeyword("_NORMALMAP");bark.SetFloat("_Smoothness",.12f);bark.enableInstancing=true;EditorUtility.SetDirty(bark);
                var root=new GameObject("Summer"+species);
                try
                {
                    var levels=new LOD[3];Material leaf=null;
                    for(int level=0;level<3;level++)
                    {
                        var path=Art+root.name+"_LOD"+level+".fbx";
                        var mi=(ModelImporter)AssetImporter.GetAtPath(path);
                        mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                        mi.isReadable=true;mi.importCameras=false;mi.importLights=false;mi.importAnimation=false;mi.SaveAndReimport();
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
                                        var vertices=mesh.vertices;var indices=mesh.GetIndices(slot);var bounds=new Bounds(vertices[indices[0]],Vector3.zero);
                                        foreach(var index in indices)bounds.Encapsulate(vertices[index]);
                                        leaf=ForestFoliageMaterials.Create(Art+species+"_Leaf.mat",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath),bounds);
                                        ApplyLeafLighting(leaf);
                                    }
                                    slots[slot]=leaf;
                                }
                                else if(slots[slot].name.Contains("Bark"))slots[slot]=bark;
                                else throw new InvalidOperationException("Unknown summer material: "+slots[slot].name);
                            }
                            renderer.sharedMaterials=slots;renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
                        }
                        levels[level]=new LOD(level==0 ? .1f : level==1 ? .04f : .001f,renderers);
                    }
                    var lod=root.AddComponent<LODGroup>();lod.SetLODs(levels);lod.RecalculateBounds();
                    PrefabUtility.SaveAsPrefabAsset(root,Prefabs+root.name+".prefab");
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            AssetDatabase.SaveAssets();TreeImpostorBaker.Bake("Trees/SummerForest");
            ApplyDistantLighting();
            var mapleFolder="Assets/CityForge/Resources/Trees/SummerMaple/";
            Directory.CreateDirectory(mapleFolder);
            var contents=PrefabUtility.LoadPrefabContents(Prefabs+"SummerMaple.prefab");
            try{PrefabUtility.SaveAsPrefabAsset(contents,mapleFolder+"SummerMaple.prefab");}
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            AssetDatabase.SaveAssets();
            Debug.Log("CityForge: summer maple, linden and elm imported and baked; maple-only test available.");
        }
        static void ApplyLeafLighting(Material leaf)
        {
            leaf.SetColor("_BaseColor",new Color(.50f,.68f,.43f,1));
            leaf.SetFloat("_SkyFill",1f);
            leaf.SetFloat("_Transmission",.10f);
            leaf.SetFloat("_ShadowFill",.14f);
            EditorUtility.SetDirty(leaf);
        }
        [MenuItem("CityForge/Summer Forest/Apply Distant Lighting Tint")]
        static void ApplyDistantLighting()
        {
            foreach(var species in new[]{"Maple","Linden","Elm"})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/CityForge/Art/Trees/Summer"+species+"_Impostor.mat");
                if(material==null)continue;
                // Isolated baked trees lack the neighboring canopy shadows of a stand.
                material.SetColor("_BaseColor",new Color(.55f,.65f,.55f,1));
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("CityForge/Summer Forest/Tune Forest Lighting")]
        public static void TuneLighting()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before baking lighting.");
            foreach(var species in new[]{"Maple","Linden","Elm"})
            {
                var leaf=AssetDatabase.LoadAssetAtPath<Material>(Art+species+"_Leaf.mat");
                if(leaf!=null)ApplyLeafLighting(leaf);
            }
            AssetDatabase.SaveAssets();
            TreeImpostorBaker.Bake("Trees/SummerMaple");
            ApplyDistantLighting();
            Debug.Log("CityForge: forest lighting tuned; maple distant views rebaked.");
        }
        [MenuItem("CityForge/Summer Forest/Capture Lighting View")]
        public static void CaptureLighting()
        {
            var rig=UnityEngine.Object.FindFirstObjectByType<CityForge.CameraSystem.CityForgeCameraController>();
            if(!EditorApplication.isPlaying || rig==null)throw new InvalidOperationException("Preview first.");
            ScreenCapture.CaptureScreenshot("Documentation/03_Technical/Screenshots/Summer_Maple_Lighting_Zoom"+rig.ZoomLevel+".png");
        }
        [MenuItem("CityForge/Summer Forest/Capture Current View")]
        public static void Capture()
        {
            var rig=UnityEngine.Object.FindFirstObjectByType<CityForge.CameraSystem.CityForgeCameraController>();
            if(!EditorApplication.isPlaying || rig==null)throw new InvalidOperationException("Preview first.");
            ScreenCapture.CaptureScreenshot("Documentation/03_Technical/Screenshots/"+(ForestPresentationSettings.Load().resourceFolder=="Trees/SummerMaple" ? "Summer_Maple" : "Summer_Forest")+"_Zoom"+rig.ZoomLevel+".png");
        }
        [MenuItem("CityForge/Summer Forest/Select Maple Only")]
        public static void SelectMaple(){Select("Trees/SummerMaple",3);}
        [MenuItem("CityForge/Summer Forest/Select Summer Pack")]
        public static void SelectSummer(){Select("Trees/SummerForest",1);}
        [MenuItem("CityForge/Summer Forest/Select Siberian Elms")]
        public static void SelectElms(){Select("Trees/SiberianElm",3);}
        static void Select(string folder,int fullThrough)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before selecting the pack.");
            var settings=ForestPresentationSettings.Load();settings.resourceFolder=folder;settings.fullCrownThroughZoom=fullThrough;
            File.WriteAllText("Assets/CityForge/Resources/Trees/ForestPresentation.json",JsonUtility.ToJson(settings,true)+"\n");AssetDatabase.Refresh();
            Debug.Log("CityForge: active forest pack "+folder);
        }
    }
}

#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class MountainFirImporter
    {
        const string Art="Assets/CityForge/Art/Trees/MountainFirs/";
        const string Out="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/Fir-Import-Checks.txt";
        static MountainFirImporter(){EditorApplication.update+=Poll;}
        static void Poll(){const string request="Library/MountainFirImport.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}File.Delete(request);
            try{Build();}catch(Exception e){File.WriteAllText(Out,"FAIL: "+e);Debug.LogException(e);}}
        static Material Store(Material material,string path){var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old){EditorUtility.CopySerialized(material,old);UnityEngine.Object.DestroyImmediate(material);return old;}AssetDatabase.CreateAsset(material,path);return material;}
        static void Matte(Material material,Color tint){material.enableInstancing=true;material.SetColor("_BaseColor",tint);material.SetFloat("_Smoothness",.04f);material.SetFloat("_SpecularHighlights",0);material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");material.SetFloat("_EnvironmentReflections",0);material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");}
        [MenuItem("CityForge/Import/Mountain Fir Species")]
        public static void Build(){
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before importing firs.");
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles(Art,"*.png",SearchOption.AllDirectories)){
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);bool normal=path.EndsWith("_Normal.png");importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=!normal;importer.mipmapEnabled=true;importer.alphaIsTransparency=!normal;importer.mipMapsPreserveCoverage=!normal;importer.alphaTestReferenceValue=.15f;importer.SaveAndReimport();
            }
            File.WriteAllText(Out,"");
            foreach(var species in new[]{"BalsamFir","FraserFir","CilicianFir"}){
                var directory=Art+species+"/";var materials=new Dictionary<string,Material>();
                if(species=="BalsamFir"){
                    var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/CityForge/Art/Trees/BalsamFirStudy/Balsam.mat");var material=new Material(source){name="BalsamFir"};Matte(material,new Color(.65f,.78f,.66f));materials["bark11_Mat"]=Store(material,directory+"BalsamFir.mat");
                }else if(species=="FraserFir"){
                    foreach(var name in new[]{"fir_Cap_04","fir_FraserFirBark","fir_FraserFirNeedle"}){
                        var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/CityForge/Art/Trees/"+name+".mat");var material=new Material(source){name=name};
                        if(name.Contains("Needle"))Matte(material,new Color(.65f,.78f,.66f));materials[name]=Store(material,directory+name+".mat");
                    }
                }else{
                    foreach(var stem in new[]{"Bark1","Bark2","Needle1","Needle2","Needle3","Needle4","DryNeedle1"}){
                        string name=stem+"_Abiescilicica";var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
                        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(directory+name+"_Albedo.png"));material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(directory+name+"_Normal.png"));material.EnableKeyword("_NORMALMAP");
                        Matte(material,stem.StartsWith("Bark")?Color.white:new Color(.6f,.75f,.65f));material.SetFloat("_Cull",0);
                        if(!stem.StartsWith("Bark")){material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.15f);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=(int)RenderQueue.AlphaTest;material.SetOverrideTag("RenderType","TransparentCutout");}
                        materials[name+"_Mat"]=Store(material,directory+name+".mat");
                    }
                }
                var root=new GameObject(species);var levels=new LOD[3];var counts=new int[3];
                try{
                    for(int i=0;i<3;i++){
                        string path=directory+species+"_LOD"+i+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
                        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.transform.SetParent(root.transform,false);
                        var renderers=model.GetComponentsInChildren<MeshRenderer>();foreach(var renderer in renderers){var slots=renderer.sharedMaterials;for(int slot=0;slot<slots.Length;slot++){
                            string name=slots[slot]?slots[slot].name:"";if(!materials.TryGetValue(name,out var material))throw new Exception(species+" unmapped material: "+name);slots[slot]=material;
                        }renderer.sharedMaterials=slots;renderer.shadowCastingMode=ShadowCastingMode.TwoSided;counts[i]+=renderer.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;}
                        levels[i]=new LOD(i==0?.2f:i==1?.07f:.005f,renderers);
                    }
                    var bounds=levels[0].renderers[0].bounds;foreach(var renderer in levels[0].renderers)bounds.Encapsulate(renderer.bounds);float height=species=="BalsamFir"?18:species=="FraserFir"?20:21;float scale=height/bounds.size.y;
                    foreach(Transform model in root.transform){model.localScale*=scale;model.localPosition=new Vector3(model.localPosition.x*scale,(model.localPosition.y-bounds.min.y)*scale,model.localPosition.z*scale);}
                    var lod=root.AddComponent<LODGroup>();lod.SetLODs(levels);lod.RecalculateBounds();
                    bounds=levels[0].renderers[0].bounds;foreach(var renderer in levels[0].renderers)bounds.Encapsulate(renderer.bounds);
                    if(Mathf.Abs(bounds.size.y-height)>.1f||counts[0]<=counts[1]||counts[1]<=counts[2]||counts[0]>60000)throw new Exception("Invalid fir bounds or detail levels: "+species);
                    string destination="Assets/CityForge/Resources/Trees/MountainEvergreen/"+species+".prefab";PrefabUtility.SaveAsPrefabAsset(root,destination);
                    File.AppendAllText(Out,$"PASS {species}: {bounds.size.y:F1} m; triangles {string.Join(" / ",counts)}; mapped materials and three decreasing mesh LODs.\n");
                }finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            AssetDatabase.SaveAssets();
        }
    }
}
#endif

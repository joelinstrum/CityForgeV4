#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.EditorTools
{
    public sealed class BroadleafAssetProcessor:AssetPostprocessor
    {
        void OnPreprocessTexture(){if(!assetPath.StartsWith(BroadleafImporter.Art))return;var i=(TextureImporter)assetImporter;bool normal=assetPath.EndsWith("_Normal.png");i.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;i.sRGBTexture=!normal;i.alphaIsTransparency=!normal;i.mipmapEnabled=true;i.mipMapsPreserveCoverage=!normal;i.alphaTestReferenceValue=.2f;i.maxTextureSize=2048;}
        void OnPreprocessModel(){if(!assetPath.StartsWith(BroadleafImporter.Art))return;var i=(ModelImporter)assetImporter;i.importAnimation=false;i.importCameras=false;i.importLights=false;i.isReadable=true;i.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;}
    }
    [InitializeOnLoad] public static class BroadleafImporter
    {
        public const string Art="Assets/CityForge/Art/Trees/ForestBroadleaf/";
        const string Out="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/";
        [Serializable] public class Recipe {public Model[] models;public Mat[] materials;}
        [Serializable] public class Model {public string species,season;public float height;public string[] files;public Binding[] bindings;}
        [Serializable] public class Binding {public string source,material;}
        [Serializable] public class Mat {public string path,albedo,normal;public bool cutout;}
        static BroadleafImporter(){EditorApplication.update+=Poll;}
        static void Poll(){const string request="Library/BroadleafImport.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}File.Delete(request);try{Build();}catch(Exception e){File.WriteAllText(Out+"Broadleaf-Import-Checks.txt","FAIL: "+e);Debug.LogException(e);}}
        static T Store<T>(T asset,string path)where T:UnityEngine.Object {var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){EditorUtility.CopySerialized(asset,old);UnityEngine.Object.DestroyImmediate(asset);return old;}AssetDatabase.CreateAsset(asset,path);return asset;}
        static string Canonical(string name)=>Regex.Replace(name,@"\.\d+$","");
        static bool Keep(string species,string material,string season){if(!species.StartsWith("SilverMaple"))return true;if(season=="Winter")return !material.Contains("Leaf");if(season=="Summer")return !material.Contains("YellowLeaf");return !material.Contains("GreenLeaf");}
        static Mesh Filter(Mesh source,List<int> slots,string name)
        {
            var indices=slots.Select(i=>source.GetTriangles(i)).ToArray();var remap=new Dictionary<int,int>();var old=new List<int>();
            foreach(var faces in indices)for(int i=0;i<faces.Length;i++){int v=faces[i];if(!remap.TryGetValue(v,out int n)){n=old.Count;old.Add(v);remap[v]=n;}faces[i]=n;}
            var result=new Mesh{name=name,indexFormat=old.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};var vertices=source.vertices;result.vertices=old.Select(i=>vertices[i]).ToArray();
            var normals=source.normals;if(normals.Length==vertices.Length)result.normals=old.Select(i=>normals[i]).ToArray();
            var tangents=source.tangents;if(tangents.Length==vertices.Length)result.tangents=old.Select(i=>tangents[i]).ToArray();
            var uv=source.uv;if(uv.Length==vertices.Length)result.uv=old.Select(i=>uv[i]).ToArray();
            var uv2=source.uv2;if(uv2.Length==vertices.Length)result.uv2=old.Select(i=>uv2[i]).ToArray();
            var colors=source.colors;if(colors.Length==vertices.Length)result.colors=old.Select(i=>colors[i]).ToArray();
            result.subMeshCount=indices.Length;for(int i=0;i<indices.Length;i++)result.SetTriangles(indices[i],i);result.RecalculateBounds();return result;
        }
        [MenuItem("CityForge/Import/Optimized Forest Broadleaf Trees")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing trees.");AssetDatabase.Refresh();
            var recipe=JsonUtility.FromJson<Recipe>(File.ReadAllText(Art+"Import.json"));File.WriteAllText(Out+"Broadleaf-Import-Checks.txt","");
            foreach(var entry in recipe.materials){var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=Path.GetFileNameWithoutExtension(entry.path),enableInstancing=true};
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(entry.albedo);if(!texture)throw new Exception("Missing albedo: "+entry.albedo);material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",.04f);material.SetFloat("_SpecularHighlights",0);material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");material.SetFloat("_EnvironmentReflections",0);material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");material.SetFloat("_Cull",entry.cutout?0:2);
                if(!string.IsNullOrEmpty(entry.normal)){var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(entry.normal);if(!normal)throw new Exception("Missing normal: "+entry.normal);material.SetTexture("_BumpMap",normal);material.EnableKeyword("_NORMALMAP");}
                if(entry.cutout){material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.2f);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=(int)RenderQueue.AlphaTest;material.SetOverrideTag("RenderType","TransparentCutout");}ForestTreePalette.Apply(material,entry.path);Store(material,entry.path);
            }
            foreach(var model in recipe.models){
                var seasons=model.species.StartsWith("SilverMaple")?new[]{"Summer","Fall","Winter"}:new[]{model.season};
                var mapping=model.bindings.ToDictionary(b=>b.source,b=>AssetDatabase.LoadAssetAtPath<Material>(b.material));
                foreach(var season in seasons){var root=new GameObject(model.species+" "+season);var counts=new int[3];var levels=new LOD[3];Bounds reference=new Bounds();
                    try{for(int lod=0;lod<3;lod++){
                        var source=AssetDatabase.LoadAssetAtPath<GameObject>(model.files[lod]);if(!source)throw new Exception("Missing model "+model.files[lod]);var part=(GameObject)PrefabUtility.InstantiatePrefab(source);part.transform.SetParent(root.transform,false);var renderers=new List<Renderer>();
                        foreach(var renderer in part.GetComponentsInChildren<MeshRenderer>()){
                            var filter=renderer.GetComponent<MeshFilter>();var sourceMesh=filter.sharedMesh;var materials=renderer.sharedMaterials;
                            if(lod==0){if(reference.size==Vector3.zero)reference=renderer.bounds;else reference.Encapsulate(renderer.bounds);}
                            var slots=new List<int>();var selected=new List<Material>();
                            for(int i=0;i<materials.Length;i++){string key=Canonical(materials[i].name);if(!mapping.TryGetValue(key,out var material)||!material)throw new Exception(model.species+" unmapped material "+key);if(!Keep(model.species,key,season))continue;slots.Add(i);selected.Add(material);}
                            if(slots.Count==0){UnityEngine.Object.DestroyImmediate(renderer.gameObject);continue;}
                            var mesh=Filter(sourceMesh,slots,model.species+season+"LOD"+lod);var path=Art+model.species+"/"+season+"_LOD"+lod+"_"+renderers.Count+".asset";filter.sharedMesh=Store(mesh,path);renderer.sharedMaterials=selected.ToArray();renderer.shadowCastingMode=ShadowCastingMode.On;counts[lod]+=filter.sharedMesh.triangles.Length/3;renderers.Add(renderer);
                        }
                        levels[lod]=new LOD(lod==0?.16f:lod==1?.055f:.004f,renderers.ToArray());
                    }
                    float scale=model.height/reference.size.y;foreach(Transform part in root.transform){part.localScale*=scale;part.localPosition=new Vector3(part.localPosition.x*scale,(part.localPosition.y-reference.min.y)*scale,part.localPosition.z*scale);}
                    var group=root.AddComponent<LODGroup>();group.SetLODs(levels);group.RecalculateBounds();
                    if(counts.Any(c=>c<=0)||counts[0]<=counts[1]||counts[1]<=counts[2])throw new Exception(model.species+season+" invalid LOD counts: "+string.Join(" / ",counts));
                    var destination="Assets/CityForge/Resources/Trees/ForestBroadleaf/"+model.species;Directory.CreateDirectory(destination);AssetDatabase.Refresh();PrefabUtility.SaveAsPrefabAsset(root,destination+"/"+season+".prefab");
                    File.AppendAllText(Out+"Broadleaf-Import-Checks.txt",$"PASS {model.species} {season}: {string.Join(" / ",counts)} triangles; compact vertices, mapped cutout/normal textures, three mesh LODs; nominal summer/source height {model.height:F1}m.\n");
                    }finally{UnityEngine.Object.DestroyImmediate(root);}
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
#endif

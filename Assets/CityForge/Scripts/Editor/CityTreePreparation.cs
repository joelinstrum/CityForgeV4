#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.Lots;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace CityForge.EditorTools
{
    // Private imported geometry and textures are staged separately from this source-only preparer.
    internal static class CityTreePreparation
    {
        internal const string Root="Assets/CityForge/Resources/Lots/Trees/CityPack/";
        [MenuItem("CityForge/Import/Prepare City Tree Pack")]
        public static void Prepare()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play Mode before preparing trees.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var materials=new Dictionary<string,Material>{
                {"Concrete",MakeMaterial("Concrete","concrete-32_d100.jpg")},
                {"Earth",MakeMaterial("Mulch","Earth - Mulch 01 cbdp -opt.jpg")},
                {"Metal",MakeMaterial("Iron","Metal - Iron -opt.jpg")},
                {"Wood",MakeMaterial("Bark","bark_willow_02_diff_2k.jpg")},
                {"Leaf Oak Au2",MakeMaterial("Leaves-Fall-2","Leaf Texture OakAu2.png",true)},
                {"Leaf Oak Au",MakeMaterial("Leaves-Fall","Leaf Texture OakAu.png",true)},
                {"Leaf Oak2",MakeMaterial("Leaves-2","Leaf Texture Oak2.png",true)},
                {"Leaf Oak",MakeMaterial("Leaves","Leaf Texture Oak.png",true)}
            };
            for(int i=1;i<=4;i++)foreach(string season in new[]{"Summer","Fall","Winter"})foreach(bool planter in new[]{false,true})
            {
                string folder=Root+$"Tree{i:00}/{season}/";
                var root=new GameObject($"City Tree {i:00} · {season} · "+(planter?"Planter":"Bare"));
                try
                {
                    var levels=new List<LOD>();
                    foreach(string tier in new[]{"Near","Distant"})
                    {
                        string file=folder+tier+".fbx";
                        var importer=AssetImporter.GetAtPath(file) as ModelImporter;
                        if(!importer)throw new InvalidOperationException("Missing city tree mesh: "+file);
                        if(importer.materialImportMode!=ModelImporterMaterialImportMode.None||!importer.isReadable)
                        {importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.isReadable=true;importer.SaveAndReimport();}
                        var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(file),root.transform);
                        model.name=tier;
                        // Concrete skirt extends 20cm below the authored trunk origin.
                        model.transform.localPosition=Vector3.up*(planter?.2f:0);
                        var renderers=new List<Renderer>();
                        foreach(var renderer in model.GetComponentsInChildren<Renderer>())
                        {
                            string key=materials.Keys.FirstOrDefault(k=>renderer.name.StartsWith(k,StringComparison.Ordinal));
                            if(key==null)throw new InvalidOperationException("Unknown city tree material: "+renderer.name);
                            if(!planter&&(key=="Concrete"||key=="Earth"||key=="Metal")){Object.DestroyImmediate(renderer.gameObject);continue;}
                            renderer.sharedMaterials=Enumerable.Repeat(materials[key],renderer.sharedMaterials.Length).ToArray();
                            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;renderers.Add(renderer);
                        }
                        levels.Add(new LOD(levels.Count==0?.12f:.001f,renderers.ToArray()));
                    }
                    var lod=root.AddComponent<LODGroup>();lod.SetLODs(levels.ToArray());lod.RecalculateBounds();
                    PrefabUtility.SaveAsPrefabAsset(root,folder+(planter?"Planter":"Bare")+".prefab");
                }
                finally{Object.DestroyImmediate(root);}
            }
            AssetDatabase.SaveAssets();
            typeof(LotCatalogSetup).GetMethod("EnsureCatalog",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
            Debug.Log("CITY TREE PACK PREPARED: 8 seasonal catalog entries");
        }
        static Material MakeMaterial(string name,string texture,bool leaves=false)
        {
            string file=Root+"Textures/"+texture;
            if(AssetImporter.GetAtPath(file) is not TextureImporter importer)throw new InvalidOperationException("Missing city tree texture: "+file);
            importer.alphaIsTransparency=leaves;importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=leaves;importer.alphaTestReferenceValue=.4f;importer.maxTextureSize=2048;importer.SaveAndReimport();
            string path=Root+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(file));mat.SetColor("_BaseColor",Color.white);
            mat.SetFloat("_Smoothness",.12f);mat.SetFloat("_Metallic",0);mat.SetFloat("_Cull",leaves?0:2);mat.enableInstancing=true;
            if(leaves)
            {
                string normal=Root+"Textures/Leaf Texture Oak_N.png";
                if(AssetImporter.GetAtPath(normal) is TextureImporter ni&&ni.textureType!=TextureImporterType.NormalMap){ni.textureType=TextureImporterType.NormalMap;ni.SaveAndReimport();}
                mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));mat.SetFloat("_BumpScale",.5f);mat.EnableKeyword("_NORMALMAP");
                mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.4f);mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;mat.doubleSidedGI=true;
            }
            EditorUtility.SetDirty(mat);return mat;
        }
        internal static IEnumerable<LotAssetCatalogEntry> CatalogEntries()
        {
            for(int i=1;i<=4;i++)foreach(bool planter in new[]{false,true})
            {
                string variant=planter?"Planter":"Bare",folder=Root+$"Tree{i:00}/";
                var summer=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"Summer/"+variant+".prefab");if(!summer)continue;
                yield return new LotAssetCatalogEntry{
                    id=$"city-tree-{i:00}-"+(planter?"planter":"bare"),label=$"City Tree {i:00} · "+(planter?"Planter":"No planter"),
                    description="Seasonal city tree. Summer foliage, autumn color, and bare winter branches. "+(planter?"Includes the original concrete, mulch, and iron surround.":"Tree only, without the planter surround."),
                    category="Trees",group="Deciduous",prefab=summer,
                    autumnPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"Fall/"+variant+".prefab"),winterPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"Winter/"+variant+".prefab"),
                    playerPlaceable=true,preserveTreeMaterials=true,footprint=Vector2.one*(7-i)
                };
            }
        }
    }
}
#endif

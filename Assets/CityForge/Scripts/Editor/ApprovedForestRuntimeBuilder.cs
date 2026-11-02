#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using CityForge.World;

namespace CityForge.EditorTools
{
    public static class ApprovedForestRuntimeBuilder
    {
        const string Checkpoint="SourceAssets/ApprovedForest/RefinedC-2026-10-28/";
        const string Output="Assets/CityForge/Resources/ApprovedForest";
        [Serializable] sealed class MeshRecord { public bool leaf; public float[] vertices,normals,uv,colors; }
        [Serializable] sealed class MeshFile { public MeshRecord[] meshes; }
        [MenuItem("CityForge/Water/Prepare approved runtime river shader")]
        public static void PrepareRiverShader()
        {
            string source=RiverGlintShaderStudy.ShaderPath;
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(source);
            if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("First build the local glint shader using Library/RiverGlintShader.request.");
            const string destination="Assets/CityForge/Resources/ApprovedWater/RiverGlint.shader";
            Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(source,destination,true);AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceSynchronousImport);
            // A real material retains all runtime shader_feature variants (especially alpha clipping).
            var baseline=Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline");
            var material=new Material(baseline.waterTemplate.GetComponent<WaveHarmonic.Crest.WaterRenderer>().Surface.Material);
            if(material.isVariant)material.parent=null;
            material.shader=AssetDatabase.LoadAssetAtPath<Shader>(destination);ApprovedRiverWater.Configure(material);material.name="Approved Crest river";
            const string materialPath="Assets/CityForge/Resources/ApprovedWater/River.mat";
            var previous=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(previous){EditorUtility.CopySerialized(material,previous);UnityEngine.Object.DestroyImmediate(material);EditorUtility.SetDirty(previous);}
            else AssetDatabase.CreateAsset(material,materialPath);
            AssetDatabase.SaveAssets();
        }
        [MenuItem("CityForge/Forest/Prepare runtime terrain variants")]
        public static void PrepareTerrainVariants()
        {
            // District Terrain objects are created after loading. Build-time material discovery
            // cannot see their normal/mask keywords; retain the combinations without changing art.
            var baseline=Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline");
            if(!baseline||!baseline.terrainMaterial)throw new InvalidOperationException("Approved meadow terrain material missing.");
            Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            for(int mask=0;mask<4;mask++){
                var material=new Material(baseline.terrainMaterial);
                if(material.isVariant)material.parent=null;
                if((mask&1)!=0)material.EnableKeyword("_MASKMAP");else material.DisableKeyword("_MASKMAP");
                if((mask&2)!=0)material.EnableKeyword("_NORMALMAP");else material.DisableKeyword("_NORMALMAP");
                string path=Output+"/TerrainVariant"+mask+".mat";
                var previous=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(previous){EditorUtility.CopySerialized(material,previous);UnityEngine.Object.DestroyImmediate(material);EditorUtility.SetDirty(previous);}
                else AssetDatabase.CreateAsset(material,path);
            }
            AssetDatabase.SaveAssets();
        }
        public static void BuildNativeSummer()
        {
            var result=AssetDatabase.LoadAssetAtPath<ApprovedForestAssets>(Output+"/Forest.asset");
            var library=Resources.Load<SeasonalTreeLibrary>("Seasons/SeasonalTrees");
            string folder=Output+"/NativeSummer";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var originals=library.Prefabs(RegionSeason.Summer);result.nativeSummer=new GameObject[originals.Length];var materials=new Dictionary<Material,Material>();
            for(int i=0;i<originals.Length;i++){
                var clone=UnityEngine.Object.Instantiate(originals[i]);clone.name=originals[i].name;
                foreach(var renderer in clone.GetComponentsInChildren<Renderer>(true)){
                    var shared=renderer.sharedMaterials;
                    for(int j=0;j<shared.Length;j++){var source=shared[j];if(!source||!source.HasProperty("_HealthyColor")||!source.HasProperty("_DryColor"))continue;
                        if(!materials.TryGetValue(source,out var warm)){warm=new Material(source);if(warm.isVariant)warm.parent=null;
                            warm.SetColor("_HealthyColor",i<11?ApprovedForestColors.MeadowTreeGreen(source.GetColor("_HealthyColor")):ApprovedForestColors.WarmPale(source.GetColor("_HealthyColor")));warm.SetColor("_DryColor",i<11?ApprovedForestColors.MeadowTreeGreen(source.GetColor("_DryColor")):ApprovedForestColors.WarmPale(source.GetColor("_DryColor")));
                            AssetDatabase.CreateAsset(warm,folder+"/Material"+materials.Count+".mat");materials[source]=warm;}
                        shared[j]=warm;
                    }
                    renderer.sharedMaterials=shared;
                }
                result.nativeSummer[i]=PrefabUtility.SaveAsPrefabAsset(clone,folder+"/Tree"+i+".prefab");UnityEngine.Object.DestroyImmediate(clone);
            }
            EditorUtility.SetDirty(result);AssetDatabase.SaveAssets();
        }
        [MenuItem("CityForge/Forest/Refresh meadow tree greens")]
        public static void RefreshMeadowTreeGreens()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Refresh tree colors in Edit mode.");
            var assets=Resources.Load<ApprovedForestAssets>("ApprovedForest/Forest");
            var library=Resources.Load<SeasonalTreeLibrary>("Seasons/SeasonalTrees");
            if(!assets||assets.nativeSummer==null||assets.nativeSummer.Length<11||!library)
                throw new InvalidOperationException("Prepare the approved native summer tree assets first.");
            var sources=new Dictionary<Material,Material>();
            for(int i=0;i<11;i++){
                var source=library.summer[i].GetComponentsInChildren<Renderer>(true);
                var target=assets.nativeSummer[i].GetComponentsInChildren<Renderer>(true);
                if(source.Length!=target.Length)throw new InvalidOperationException("Native tree renderer layout differs.");
                for(int r=0;r<source.Length;r++){
                    var from=source[r].sharedMaterials;var to=target[r].sharedMaterials;
                    if(from.Length!=to.Length)throw new InvalidOperationException("Native tree material layout differs.");
                    for(int m=0;m<from.Length;m++){
                        if(!from[m]||!from[m].HasProperty("_HealthyColor")||!from[m].HasProperty("_DryColor"))continue;
                        if(!to[m]||to[m]==from[m]||!AssetDatabase.GetAssetPath(to[m]).StartsWith(Output+"/NativeSummer/",StringComparison.Ordinal))
                            throw new InvalidOperationException("Only prepared summer copies may be recolored.");
                        sources[to[m]]=from[m];
                    }
                }
            }
            foreach(var pair in sources){
                pair.Key.SetColor("_HealthyColor",ApprovedForestColors.MeadowTreeGreen(pair.Value.GetColor("_HealthyColor")));
                pair.Key.SetColor("_DryColor",ApprovedForestColors.MeadowTreeGreen(pair.Value.GetColor("_DryColor")));
                EditorUtility.SetDirty(pair.Key);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Refreshed "+sources.Count+" meadow tree leaf materials from original colors.");
        }

        [MenuItem("CityForge/Forest/Build approved runtime assets")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Build forest assets in Edit mode.");
            // Use the same verifier as the locked replay, including every mesh, texture and public recipe.
            var start=new System.Diagnostics.ProcessStartInfo("/usr/bin/python3","Tools/Trees/approved_forest_checkpoint.py verify"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
            using(var process=System.Diagnostics.Process.Start(start)){string output=process.StandardOutput.ReadToEnd(),error=process.StandardError.ReadToEnd();process.WaitForExit();if(process.ExitCode!=0)throw new InvalidOperationException(output+error);}
            if(Directory.Exists(Output))throw new InvalidOperationException("Runtime forest output already exists; preserve/review it before rebuilding.");
            Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            var result=ScriptableObject.CreateInstance<ApprovedForestAssets>();result.meshes=new Mesh[70];
            for(int variant=0;variant<7;variant++)for(int lod=0;lod<5;lod++){
                var file=JsonUtility.FromJson<MeshFile>(File.ReadAllText(Checkpoint+"Meshes/v"+variant+"-lod"+lod+".json"));
                foreach(var record in file.meshes){
                    var vs=new List<Vector3>();var ns=new List<Vector3>();var us=new List<Vector2>();var cs=new List<Color>();var indices=new List<int>();
                    var lookup=new Dictionary<(Vector3,Vector3,Vector2,Color),int>();
                    for(int i=0;i<record.vertices.Length/3;i++){
                        var v=new Vector3(record.vertices[i*3],record.vertices[i*3+1],record.vertices[i*3+2]);var n=new Vector3(record.normals[i*3],record.normals[i*3+1],record.normals[i*3+2]);var uv=new Vector2(record.uv[i*2],record.uv[i*2+1]);var c=new Color(record.colors[i*4],record.colors[i*4+1],record.colors[i*4+2],record.colors[i*4+3]);
                        var key=(v,n,uv,c);if(!lookup.TryGetValue(key,out int index)){index=vs.Count;lookup[key]=index;vs.Add(v);ns.Add(n);us.Add(uv);cs.Add(c);}indices.Add(index);
                    }
                    var mesh=new Mesh{name=$"Approved Tree06 v{variant} LOD{lod} {(record.leaf?"Leaf":"Wood")}",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vs);mesh.SetNormals(ns);mesh.SetUVs(0,us);mesh.SetColors(cs);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
                    int slot=variant*10+lod*2+(record.leaf?1:0);AssetDatabase.CreateAsset(mesh,Output+"/Mesh"+slot+".asset");result.meshes[slot]=mesh;
                }
            }
            // New GUIDs: do not duplicate or replace original study/checkpoint assets.
            foreach(string file in new[]{"LeafCoverage.png","EarthyGrass1.png","EarthyGrass2.png"})File.Copy(Checkpoint+"RuntimeAssets/Tree06Large/"+file,Output+"/"+file);
            File.Copy(Checkpoint+"RuntimeAssets/Bark.jpg",Output+"/Bark.jpg");
            File.Copy(Checkpoint+"Meshes/layout-600.json",Output+"/Layout.json");
            foreach(string name in new[]{"LeafCoverage.png","EarthyGrass1.png","EarthyGrass2.png","Bark.jpg"}){
                string source=Checkpoint+"RuntimeAssets/"+(name=="Bark.jpg"?"":"Tree06Large/")+name+".meta";
                File.WriteAllText(Output+"/"+name+".meta",Regex.Replace(File.ReadAllText(source),@"(?m)^guid: .*", "guid: "+Guid.NewGuid().ToString("N")));
            }
            AssetDatabase.Refresh();
            var shader=Shader.Find(ApprovedForestColors.ShaderName);if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Approved canopy shader missing or invalid.");
            result.leaf=new Material(shader){name="Approved refined C leaf",enableInstancing=true};result.leaf.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Output+"/LeafCoverage.png"));result.leaf.SetFloat("_Leaf",1);result.leaf.SetFloat("_Cutoff",.38f);result.leaf.SetFloat("_Smoothness",.12f);result.leaf.SetFloat("_OcclusionStrength",.65f);ApprovedForestColors.ConfigureLeafMaterial(result.leaf);AssetDatabase.CreateAsset(result.leaf,Output+"/Leaf.mat");
            result.wood=new Material(shader){name="Approved Tree06 bark",enableInstancing=true};result.wood.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Output+"/Bark.jpg"));result.wood.SetFloat("_Leaf",0);result.wood.SetFloat("_Cutoff",0);result.wood.SetFloat("_Smoothness",.08f);AssetDatabase.CreateAsset(result.wood,Output+"/Wood.mat");
            result.grass1=AssetDatabase.LoadAssetAtPath<Texture2D>(Output+"/EarthyGrass1.png");result.grass2=AssetDatabase.LoadAssetAtPath<Texture2D>(Output+"/EarthyGrass2.png");result.layout=AssetDatabase.LoadAssetAtPath<TextAsset>(Output+"/Layout.json");
            AssetDatabase.CreateAsset(result,Output+"/Forest.asset");AssetDatabase.SaveAssets();BuildNativeSummer();PrepareTerrainVariants();PrepareRiverShader();Debug.Log("Approved forest runtime assets built from verified checkpoint; private artwork remains Git-ignored.");
        }
    }
}
#endif

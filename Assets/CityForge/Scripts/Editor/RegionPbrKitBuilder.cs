using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CityForge.World;
using UnityEditor;
using UnityEngine;

namespace CityForge.Editor
{
    public static class RegionPbrKitBuilder
    {
        const string Art = "Assets/CityForge/Art/Environment/RegionPBR/";
        const string ResourcesPath = "Assets/CityForge/Resources/Region/";
        const string GraphPath = "Assets/CityForge/Rendering/Region/RegionLandCover.shadergraph";
        static readonly string[] Names = { "TemperateGrassland", "DryGrassland", "FertileSoil", "ForestCover", "EarthRock", "CoastalSoil" };
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        [MenuItem("CityForge/Build Region PBR Kit")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode to build the material kit.");
            AssetDatabase.Refresh();
            BuildGraph();
            var albedo = new Texture2DArray(512,512,6,TextureFormat.RGBA32,true,false) { name="Region Albedos", wrapMode=TextureWrapMode.Repeat, filterMode=FilterMode.Trilinear };
            var normals = new Texture2DArray(512,512,6,TextureFormat.RGBA32,true,true) { name="Region Normals", wrapMode=TextureWrapMode.Repeat, filterMode=FilterMode.Trilinear };
            var masks = new Texture2DArray(512,512,6,TextureFormat.RGBA32,true,true) { name="Region PBR Masks", wrapMode=TextureWrapMode.Repeat, filterMode=FilterMode.Trilinear };
            for(var layer=0;layer<6;layer++)
            {
                Import(Art+Names[layer]+"_Albedo.png",true);
                albedo.SetPixels(AssetDatabase.LoadAssetAtPath<Texture2D>(Art+Names[layer]+"_Albedo.png").GetPixels(),layer);
                var n=new Texture2D(512,512,TextureFormat.RGBA32,false,true);
                var m=new Texture2D(512,512,TextureFormat.RGBA32,false,true);
                var np=new Color[512*512];var mp=new Color[np.Length];
                for(var y=0;y<512;y++)for(var x=0;x<512;x++)
                {
                    // Seamless authored surface response: fine turf/soil relief, not inferred
                    // geometry from the albedo's lighting. The shader fades this at distance.
                    var u=x/512f;var v=y/512f;
                    var h=GroundNoise(u,v,layer);
                    var dx=(GroundNoise(u+1f/512,v,layer)-GroundNoise(u-1f/512,v,layer))*7f;
                    var dy=(GroundNoise(u,v+1f/512,layer)-GroundNoise(u,v-1f/512,layer))*7f;
                    var strength=layer==4 ? 1.3f : layer==5 ? .55f : .85f;
                    var normal=new Vector3(-dx*strength,-dy*strength,1).normalized;
                    np[y*512+x]=new Color(normal.x*.5f+.5f,normal.y*.5f+.5f,normal.z*.5f+.5f,1);
                    var smooth=layer==2?.13f:layer==5?.10f:layer==4?.10f:.07f;
                    mp[y*512+x]=new Color(0f,.94f+h*.05f,h,smooth);
                }
                n.SetPixels(np);n.Apply();m.SetPixels(mp);m.Apply();
                File.WriteAllBytes(Art+Names[layer]+"_Normal.png",n.EncodeToPNG());
                File.WriteAllBytes(Art+Names[layer]+"_Mask.png",m.EncodeToPNG());
                normals.SetPixels(np,layer);masks.SetPixels(mp,layer);
                UnityEngine.Object.DestroyImmediate(n);UnityEngine.Object.DestroyImmediate(m);
            }
            albedo.Apply();normals.Apply();masks.Apply();
            Save(albedo,ResourcesPath+"Albedos.asset");Save(normals,ResourcesPath+"Normals.asset");Save(masks,ResourcesPath+"Masks.asset");
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            if(shader==null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Region Shader Graph failed to import.");
            var material=new Material(shader){name="Region Land Cover PBR"};
            Save(material,ResourcesPath+"M_RegionLandCover.mat");
            var kit=ScriptableObject.CreateInstance<RegionTerrainKit>();
            kit.albedos=AssetDatabase.LoadAssetAtPath<Texture2DArray>(ResourcesPath+"Albedos.asset");
            kit.normals=AssetDatabase.LoadAssetAtPath<Texture2DArray>(ResourcesPath+"Normals.asset");
            kit.masks=AssetDatabase.LoadAssetAtPath<Texture2DArray>(ResourcesPath+"Masks.asset");
            kit.terrainMaterial=AssetDatabase.LoadAssetAtPath<Material>(ResourcesPath+"M_RegionLandCover.mat");
            BuildMeadowDetail(kit);
            Save(kit,ResourcesPath+"TerrainKit.asset");
            AssetDatabase.Refresh();
            foreach(var name in Names) { Import(Art+name+"_Normal.png",false);Import(Art+name+"_Mask.png",false); }
            AssetDatabase.SaveAssets();
            Debug.Log("CityForge: six-material Region PBR kit and editable Shader Graph built.");
        }

        static void BuildMeadowDetail(RegionTerrainKit kit)
        {
            const string path = Art + "MeadowTurf_Detail_v2.png";
            Import(path, true);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.maxTextureSize = 2048; importer.anisoLevel = 8; importer.SaveAndReimport();
            kit.meadowDetail = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            // Albedo-derived micro relief is an artistic approximation, not measured PBR data.
            // Small strength and mipmaps keep blade structure from becoming terrain ripples.
            const int n = 1024;
            var normal = new Texture2D(n, n, TextureFormat.RGBA32, true, true)
                { name = "Meadow blade micro normal", wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            float Luma(float u, float v) {
                var c = kit.meadowDetail.GetPixelBilinear(u, v);
                return c.r * .2126f + c.g * .7152f + c.b * .0722f;
            }
            var pixels = new Color[n*n];
            for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
            {
                var u = x / (float)n; var v = y / (float)n;
                var dx = (Luma(u+1f/n,v)-Luma(u-1f/n,v)) * .7f;
                var dy = (Luma(u,v+1f/n)-Luma(u,v-1f/n)) * .7f;
                var direction = new Vector3(-dx,-dy,1).normalized;
                pixels[y*n+x] = new Color(direction.x*.5f+.5f,direction.y*.5f+.5f,direction.z*.5f+.5f,1);
            }
            normal.SetPixels(pixels); normal.Apply();
            Save(normal, ResourcesPath + "MeadowDetailNormal.asset");
            kit.meadowDetailNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesPath + "MeadowDetailNormal.asset");
        }

        static float GroundNoise(float u,float v,int layer)
        {
            // Blend opposite edges of noise domains to make the texture periodic.
            u=Mathf.Repeat(u,1);v=Mathf.Repeat(v,1);
            float Periodic(float scale) {
                var x=u*scale+layer*17.1f;var y=v*scale+layer*9.3f;
                return Mathf.Lerp(Mathf.Lerp(Mathf.PerlinNoise(x,y),Mathf.PerlinNoise(x-scale,y),u),
                    Mathf.Lerp(Mathf.PerlinNoise(x,y-scale),Mathf.PerlinNoise(x-scale,y-scale),u),v);
            }
            return Periodic(23)*.65f+Periodic(61)*.35f;
        }

        static void Import(string path,bool srgb)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer==null) { AssetDatabase.ImportAsset(path);importer=(TextureImporter)AssetImporter.GetAtPath(path); }
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=srgb;
            importer.isReadable=true;importer.mipmapEnabled=true;importer.maxTextureSize=512;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;
            importer.SaveAndReimport();
        }
        static void Save(UnityEngine.Object value,string path)
        {
            var existing=AssetDatabase.LoadMainAssetAtPath(path);
            if(existing!=null) { EditorUtility.CopySerialized(value,existing);UnityEngine.Object.DestroyImmediate(value); }
            else AssetDatabase.CreateAsset(value,path);
        }
        static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
        static object New(string name) => Activator.CreateInstance(TypeOf(name),true);
        static object Get(object obj,string name) => obj.GetType().GetProperty(name,Flags).GetValue(obj);
        static void Set(object obj,string name,object value) => obj.GetType().GetProperty(name,Flags).SetValue(obj,value);
        static object Invoke(object obj,string name,params object[] args)
        {
            var method=obj.GetType().GetMethods(Flags).First(m=>m.Name==name && !m.IsGenericMethod && m.GetParameters().Length>=args.Length && m.GetParameters().Skip(args.Length).All(p=>p.IsOptional));
            var ps=method.GetParameters();var full=new object[ps.Length];
            for(int i=0;i<ps.Length;i++)full[i]=i<args.Length?args[i]:ps[i].DefaultValue;
            return method.Invoke(obj,full);
        }
        static void Slot(object node,int id,string name,bool output,bool vector)
        {
            var type=TypeOf("UnityEditor.ShaderGraph."+(vector?"Vector3MaterialSlot":"Vector1MaterialSlot"));
            var ctor=type.GetConstructors().First(c=>c.GetParameters().Length>4);
            var ps=ctor.GetParameters();var args=new object[ps.Length];
            args[0]=id;args[1]=name;args[2]=name;args[3]=Enum.Parse(ps[3].ParameterType,output?"Output":"Input");
            args[4]=vector?(object)Vector3.zero:0f;
            for(int i=5;i<ps.Length;i++)args[i]=ps[i].DefaultValue;
            Invoke(node,"AddSlot",ctor.Invoke(args));
        }
        static void Connect(object graph,object from,int fromId,object to,int toId) =>
            Invoke(graph,"Connect",Invoke(from,"GetSlotReference",fromId),Invoke(to,"GetSlotReference",toId));
        public static void BuildGraph()
        {
            const string ns="UnityEditor.ShaderGraph.";
            var graph=New(ns+"GraphData");Invoke(graph,"AddContexts");Set(graph,"path","CityForge/Region");
            var target=New("UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget");
            Invoke(target,"TrySetActiveSubTarget",TypeOf("UnityEditor.Rendering.Universal.ShaderGraph.UniversalLitSubTarget"));
            var targets=Array.CreateInstance(TypeOf(ns+"Target"),1);targets.SetValue(target,0);
            var fields=TypeOf(ns+"BlockFields").GetNestedType("SurfaceDescription",Flags);
            var names=new[]{"BaseColor","NormalTS","Smoothness","Occlusion"};
            // A default position output ensures URP generates vertex inputs for MotionVectors.
            var descriptors=Array.CreateInstance(TypeOf(ns+"BlockFieldDescriptor"),5);
            descriptors.SetValue(TypeOf(ns+"BlockFields").GetNestedType("VertexDescription",Flags).GetField("Position",Flags).GetValue(null),4);
            for(var i=0;i<4;i++)descriptors.SetValue(fields.GetField(names[i],Flags).GetValue(null),i);
            Invoke(graph,"InitializeOutputs",targets,descriptors);
            var function=New(ns+"CustomFunctionNode");
            Set(function,"functionName","RegionSurface");Set(function,"functionSource",AssetDatabase.AssetPathToGUID("Assets/CityForge/Rendering/Region/RegionSurface.hlsl"));
            Set(function,"precision",Enum.Parse(TypeOf(ns+"Internal.Precision"),"Single"));
            Slot(function,0,"WorldPosition",false,true);Slot(function,1,"WorldNormal",false,true);
            Slot(function,2,"Albedo",true,true);Slot(function,3,"NormalTS",true,true);
            Slot(function,4,"Smoothness",true,false);Slot(function,5,"AO",true,false);
            Invoke(graph,"AddNode",function);
            var position=New(ns+"PositionNode");Invoke(graph,"AddNode",position);
            var normal=New(ns+"NormalVectorNode");Invoke(graph,"AddNode",normal);
            Connect(graph,position,0,function,0);Connect(graph,normal,0,function,1);
            var context=Get(graph,"fragmentContext");var blocks=(IEnumerable)Get(context,"blocks");
            var iBlock=0;
            foreach(var entry in blocks)
            {
                var block=Get(entry,"value");var descriptor=Get(block,"descriptor");
                var index=Array.FindIndex(names,n=>(string)Get(descriptor,"name")==n);
                if(index>=0)Connect(graph,function,index+2,block,0);
                iBlock++;
            }
            Invoke(graph,"ValidateGraph");
            var serialize=TypeOf(ns+"Serialization.MultiJson").GetMethod("Serialize",Flags);
            File.WriteAllText(GraphPath,(string)serialize.Invoke(null,new[]{graph}));
            AssetDatabase.ImportAsset(GraphPath,ImportAssetOptions.ForceSynchronousImport);
        }
    }
}

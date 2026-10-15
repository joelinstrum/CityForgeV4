using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CityForge.Environment;

namespace CityForge.Editor
{
    [InitializeOnLoad]
    public static class NaturalTerrainLab
    {
        const string Root="Assets/CityForge/Art/Environment/NaturalTerrainLab/";
        const string Work="/Users/joelinstrum/Documents/Codex/2026-10-09/pl/work/terrain-natural/";
        const string Out="/Users/joelinstrum/Documents/Codex/2026-10-09/pl/outputs/";
        static Camera camera;
        static Renderer ground,grass;
        static Material baseline,natural,grassOnly;
        static int step=-1;
        static double due,lastFrame;
        static readonly List<double> frameIntervals=new List<double>();
        static readonly List<string> metrics=new List<string>();
        static readonly int[] Zooms={1,3,5};

        static NaturalTerrainLab(){EditorApplication.update+=Tick;}
        static bool Trigger(string name){
            string path=Work+name;if(!File.Exists(path))return false;
            File.Delete(path);return true;
        }
        static void Tick()
        {
            if(EditorApplication.isCompiling)return;
            try
            {
                if(Trigger("stop")){EditorApplication.isPlaying=false;step=-1;}
                if(!EditorApplication.isPlaying && Trigger("build"))Build();
                if(Trigger("play"))EditorApplication.isPlaying=true;
                if(Trigger("compare") && EditorApplication.isPlaying)BeginComparison();
                if(step>=0){
                    double now=EditorApplication.timeSinceStartup;
                    if(lastFrame>0)frameIntervals.Add((now-lastFrame)*1000);
                    lastFrame=now;
                    if(now>=due)NextCapture();
                }
            }
            catch(Exception e){step=-1;Debug.LogException(e);File.WriteAllText(Out+"NaturalTerrain_Error.txt",e.ToString());}
        }
        [MenuItem("CityForge/Ground Study/Natural Aerial Terrain Prototype")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before rebuilding.");
            Directory.CreateDirectory(Root);Directory.CreateDirectory(Out);
            var scene=EditorSceneManager.OpenScene("Assets/CityForge/Art/Environment/CrestVisualLab/LakeShore.unity");
            Bind();ValidateField();
            var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>(FindObjectsInactive.Include);
            var timer=System.Diagnostics.Stopwatch.StartNew();
            const int n=513;var heights=new float[n,n];
            var bounds=terrain.terrainData.size;var origin=terrain.transform.position;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                heights[z,x]=terrain.terrainData.GetInterpolatedHeight(x/(float)(n-1),z/(float)(n-1))+origin.y;
            var field=NaturalShoreDistanceField.Build(heights,6.8f,bounds.x/(n-1));
            field=Save(field,"ShoreDistance.asset");
            natural=new Material(baseline){name="Natural aerial grass and shoreline"};
            natural.shader=Shader.Find("CityForge/Natural Aerial Ground");
            if(!natural.shader)throw new Exception("Missing prototype shader");
            natural.SetTexture("_ShoreField",field);
            natural.SetVector("_FieldBounds",new Vector4(origin.x,origin.z,bounds.x,bounds.z));
            natural.SetFloat("_WaterLevel",6.8f);natural.SetFloat("_ShoreEnabled",1);
            natural=Save(natural,"NaturalGround.mat");
            grassOnly=new Material(natural){name="Grass only with baseline shoreline"};
            grassOnly.SetFloat("_ShoreEnabled",0);grassOnly=Save(grassOnly,"GrassOnly.mat");
            ground.sharedMaterial=natural;
            // Dense legacy grass is hidden only in this experiment. Ground texture and
            // subtle normals retain close detail; runtime tile grass is untouched.
            grass.enabled=false;
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene,Root+"NaturalTerrain.unity");
            File.WriteAllText(Out+"NaturalTerrain_Build.txt",$"513x513 terrain sample + signed chamfer field: {timer.ElapsedMilliseconds} ms (Editor, includes asset creation).\nField RHalf: {n*n*2} raw bytes; no mipmaps.\nBaseline grass triangles: {grass.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3}.\nWater renderer/materials are not modified.\n");
            Debug.Log("NATURAL TERRAIN READY");
        }
        static void ValidateField()
        {
            var plane=new float[9,9];
            for(int z=0;z<9;z++)for(int x=0;x<9;x++)plane[z,x]=x;
            var a=NaturalShoreDistanceField.Build(plane,3.5f,1);
            var b=NaturalShoreDistanceField.Build(plane,3.5f,1);
            for(int z=0;z<9;z++)for(int x=0;x<9;x++){
                float signed=a.GetPixel(x,z).r*64-32;
                if(Mathf.Abs(signed-(x-3.5f))>.03f)
                    throw new Exception("Planar shoreline distance/sign check failed");
                if(a.GetPixel(x,z)!=b.GetPixel(x,z))
                    throw new Exception("Field determinism check failed");
                if(plane[z,x]!=x)throw new Exception("Source heights mutated");
            }
            UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);
            var dry=NaturalShoreDistanceField.Build(plane,-1,1);
            var wet=NaturalShoreDistanceField.Build(plane,10,1);
            if(dry.GetPixel(4,4).r!=1||wet.GetPixel(4,4).r!=0)
                throw new Exception("Entirely wet/dry tile check failed");
            UnityEngine.Object.DestroyImmediate(dry);UnityEngine.Object.DestroyImmediate(wet);
            File.WriteAllText(Out+"NaturalTerrain_FieldChecks.txt",
                "PASS: known planar shoreline distances and signs; deterministic rebuild; unmodified source heights; entirely wet and dry tiles.\n");
        }
        static T Save<T>(T item,string name)where T:UnityEngine.Object
        {
            string path=Root+name;var old=AssetDatabase.LoadMainAssetAtPath(path);
            if(old){EditorUtility.CopySerialized(item,old);UnityEngine.Object.DestroyImmediate(item);return (T)old;}
            AssetDatabase.CreateAsset(item,path);return item;
        }
        static void Bind()
        {
            camera=GameObject.Find("Meadow presentation camera").GetComponent<Camera>();
            ground=GameObject.Find("Ground with meadow, canopy litter and dirt masks").GetComponent<Renderer>();
            grass=GameObject.Find("Continuous short meadow grass").GetComponent<Renderer>();
            baseline=AssetDatabase.LoadAssetAtPath<Material>("Assets/CityForge/Art/Environment/CrestVisualLab/ShoreGround.mat");
        }
        static void BeginComparison()
        {
            Bind();natural=AssetDatabase.LoadAssetAtPath<Material>(Root+"NaturalGround.mat");
            grassOnly=AssetDatabase.LoadAssetAtPath<Material>(Root+"GrassOnly.mat");
            step=0;metrics.Clear();SetStep();
        }
        static void SetStep()
        {
            int stage=step/3,zoom=Zooms[step%3];
            ground.sharedMaterial=stage==0?baseline:stage==1?grassOnly:natural;
            grass.enabled=stage==0;
            camera.orthographic=true;camera.orthographicSize=61.955f*Mathf.Pow(2,zoom-1);
            camera.transform.position=new Vector3(133,8,130)-camera.transform.forward*300;
            camera.nearClipPlane=.3f;camera.farClipPlane=2400;camera.ResetProjectionMatrix();
            due=EditorApplication.timeSinceStartup+4;lastFrame=0;frameIntervals.Clear();
        }
        static void NextCapture()
        {
            int stage=step/3,zoom=Zooms[step%3];
            string name=(stage==0?"Baseline":stage==1?"GrassOnly":"NaturalTerrain")+"_Zoom"+zoom;
            var times=frameIntervals.Skip(10).OrderBy(v=>v).ToArray();
            double median=times.Length>0?times[times.Length/2]:0;
            Capture(name);
            metrics.Add($"{name}: ortho half-height={camera.orthographicSize:F3}m, editor update median={median:F2}ms over {times.Length} updates (not GPU benchmark).");
            step++;
            if(step<9){SetStep();return;}
            step=-1;ground.sharedMaterial=natural;grass.enabled=false;
            camera.orthographicSize=40;camera.transform.position=new Vector3(133,8,130)-camera.transform.forward*110;
            camera.nearClipPlane=.3f;camera.farClipPlane=450;camera.ResetProjectionMatrix();
            Capture("NaturalTerrain_LakeDetail");
            File.WriteAllLines(Out+"NaturalTerrain_Metrics.txt",metrics);
            Debug.Log("NATURAL TERRAIN COMPARISONS COMPLETE");
        }
        static void Capture(string name)
        {
            var rt=new RenderTexture(1920,1080,24);rt.Create();var previous=RenderTexture.active;
            try{
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                RenderTexture.active=rt;
                var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();
                File.WriteAllBytes(Out+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
            }finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
            Debug.Log("NATURAL TERRAIN CAPTURE "+name);
        }
    }
}

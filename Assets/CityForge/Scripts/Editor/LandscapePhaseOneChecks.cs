using System;
using System.IO;
using CityForge.World;
using CityForge.CameraSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.Editor
{
    /// <summary>Explicit menu actions only; never watches historical authoring trigger files.</summary>
    [InitializeOnLoad]
    public static class LandscapePhaseOneChecks
    {
        const string Output = "/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/";
        static int stage;
        static double next;
        static PrototypeRegionView view;
        static LandscapePhaseOneChecks() { EditorApplication.update += Tick; }
        [MenuItem("CityForge/Archived Mesh Experiment/Prepare Gameplay Scene")]
        static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            EditorSceneManager.OpenScene("Assets/CityForge/Scenes/Gameplay/MainMenu.unity");
            var profile=Resources.Load<LandscapeProfile>("Region/ApprovedLandscapeProfile");
            if(profile==null || profile.grassAlbedo==null || profile.litterAlbedo==null) throw new Exception("Missing profile textures.");
            EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
        }
        [MenuItem("CityForge/Archived Mesh Experiment/Validate And Capture District")]
        static void Validate()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode in MainMenu first.");
            Directory.CreateDirectory(Output);
            view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(view==null) { PrototypeRegionChecks.Preview(); view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>(); }
            var terrain=view.GetComponentInChildren<Terrain>();
            var collider=terrain.GetComponent<TerrainCollider>();
            if(collider.terrainData!=terrain.terrainData) throw new Exception("Collider heightfield mismatch.");
            var heights=terrain.terrainData.GetHeights(0,0,513,513);
            var invalid=0;
            foreach(var h in heights) if(float.IsNaN(h)||float.IsInfinity(h)||h<0||h>1) invalid++;
            if(invalid!=0) throw new Exception("Invalid heights.");
            view.SetNaturalLandscape(false);
            if(Shader.GetGlobalFloat("_LandscapeEnabled")!=0) throw new Exception("Fallback failed.");
            view.SetNaturalLandscape(true);
            if(!view.NaturalLandscape||Shader.GetGlobalFloat("_LandscapeEnabled")!=1) throw new Exception("Profile failed.");
            for(var d=0;d<4;d++) {
                view.EnterDistrict(d);
                var mesh=view.transform.Find("RegionLandCover").GetComponent<MeshFilter>().sharedMesh;
                if(mesh.triangles.Length!=256*256*6) throw new Exception("District triangle count changed.");
                // The shader is anchored to retained world vertices, independent of district selection.
                var p=mesh.vertices[256*513+256];
                if(Mathf.Abs(p.x)>1e-4||Mathf.Abs(p.z)>1e-4) throw new Exception("Shared district corner changed.");
            }
            view.ShowOverview(); view.EnterDistrict(3);
            var rig=view.GetComponentInChildren<CityForgeCameraController>();rig.FrameArea(view.MapSide/2,terrain.terrainData.size.y,new Vector3(view.MapSide*.245f,0,view.MapSide*.28f));rig.SetZoomLevel(2);
            File.WriteAllText(Output+"Landscape-Pass1-Checks.txt", "PASS: profile textures loaded; fallback toggle; 263169 valid heights; terrain/collider share data; all four districts retain 393216 indices; shared central world vertex preserved. Seed="+view.Data.Seed+"; rendered side="+view.MapSide+"; geographic scale="+view.MapScale+". Ground-only integration; vegetation/road profile settings reserved for Pass 2.\n");
            stage=1; next=EditorApplication.timeSinceStartup+4;
        }
        [MenuItem("CityForge/Meadow Migration/Build Pilot Player")]
        static void BuildPlayer()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            // Runtime Shader.Find names are otherwise stripped from a standalone player.
            var names=new[]{"CityForge/Region Water","Universal Render Pipeline/Unlit","Universal Render Pipeline/Lit"};
            var assets=new[]{"RuntimeWaterShader","RuntimeGuideShader","RuntimeReliefShader"};
            for(var i=0;i<names.Length;i++) {
                var path="Assets/CityForge/Resources/Region/"+assets[i]+".mat";
                if(AssetDatabase.LoadAssetAtPath<Material>(path)!=null) continue;
                var shader=Shader.Find(names[i]);if(shader==null)throw new Exception("Missing runtime shader: "+names[i]);
                AssetDatabase.CreateAsset(new Material(shader){name=assets[i]},path);
            }
            AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/CityForge/Scenes/Gameplay/MainMenu.unity"},
                locationPathName="/Users/joelinstrum/Documents/Codex/2026-10-11/be/work/PhaseOnePlayer.app",
                target=BuildTarget.StandaloneOSX,options=BuildOptions.Development });
            File.WriteAllText(Output+"Meadow-Migration-Player-Build.txt",report.summary.result+"; errors="+report.summary.totalErrors+"; warnings="+report.summary.totalWarnings+"; duration="+report.summary.totalTime+"\n");
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Benchmark player build failed.");
        }
        static void Tick()
        {
            if(stage==0||!EditorApplication.isPlaying||view==null||EditorApplication.timeSinceStartup<next)return;
            try {
                if(stage==1) { Capture("Landscape-Natural-Close.png"); view.SetNaturalLandscape(false); }
                if(stage==2) { Capture("Landscape-Original-Close.png"); view.SetNaturalLandscape(true); view.GetComponentInChildren<CityForgeCameraController>().SetZoomLevel(4); }
                if(stage==3) { Capture("Landscape-Natural-Distant.png"); view.SetNaturalLandscape(false); }
                if(stage==4) { Capture("Landscape-Original-Distant.png");view.SetNaturalLandscape(true); view.GetComponentInChildren<CityForgeCameraController>().SetZoomLevel(1); Debug.Log("Landscape Pass 1 checks and captures completed.");stage=0;return; }
                stage++; next=EditorApplication.timeSinceStartup+3;
            } catch(Exception e) {stage=0;Debug.LogException(e);}
        }
        static void Capture(string name)
        {
            var camera=view.GetComponentInChildren<Camera>();
            var target=RenderTexture.GetTemporary(1600,1000,24,RenderTextureFormat.ARGB32);
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
            var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try {camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Output+name,image.EncodeToPNG());}
            finally {camera.targetTexture=oldTarget;RenderTexture.active=oldActive;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}

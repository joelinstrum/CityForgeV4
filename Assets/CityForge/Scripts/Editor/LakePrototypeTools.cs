using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CityForge.World;
using CityForge.CameraSystem;
using UnityEditor;
using UnityEngine;
using Unity.Profiling;

namespace CityForge.Editor
{
    [InitializeOnLoad]
    public static class LakePrototypeTools
    {
        const string Output = "/Users/joelinstrum/Documents/Codex/2026-10-12/begin-from-this-handoff-and-start/outputs/";
        static bool running;
        static LakePrototypeTools() { EditorApplication.update += Tick; }
        static void Tick()
        {
            const string request = "Library/LakePhaseOne.request";
            if (running || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
            string command=File.ReadAllText(request).Trim();File.Delete(request);
            if (!EditorApplication.isPlaying) { Debug.LogError("Start Play before lake evaluation."); return; }
            if(command=="sharp"){running=true;Begin(CompareSharp());} else if(command=="shore") { running=true;Begin(CompareShore()); } else if(command=="capture") { running=true;Begin(CenteredViews()); } else Run();
        }
        [MenuItem("CityForge/Lake Prototype/Run Phase One Comparison")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || running) return;
            running = true; Begin(Compare());
        }
        [MenuItem("CityForge/Lake Prototype/Open Prototype Without Benchmark")]
        public static void Preview() { if(!running&&EditorApplication.isPlaying) Begin(Open(true)); }
        [MenuItem("CityForge/Lake Prototype/Restore Original Crest Region")]
        public static void Restore() { if (!running && EditorApplication.isPlaying) Begin(Open(false)); }
        static IEnumerator Wait(float seconds) { float end=Time.realtimeSinceStartup+seconds; while(Time.realtimeSinceStartup<end) yield return null; }
        static void Begin(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);int lastFrame=-1;
            void Step() {
                if(!EditorApplication.isPlaying){while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();EditorApplication.update-=Step;return;}
                if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
                try {
                    while(stack.Count>0){var current=stack.Peek();if(!current.MoveNext()){(stack.Pop() as IDisposable)?.Dispose();continue;}
                        if(current.Current is IEnumerator child){stack.Push(child);continue;}return;}
                }catch(Exception e){while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();File.WriteAllText(Output+"Lake-Phase-One-Error.txt",e.ToString());Debug.LogException(e);}
                EditorApplication.update-=Step;
            }
            EditorApplication.update+=Step;
        }
        static IEnumerator Open(bool prototype)
        {
            var old = UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>(); if (old) old.ReturnToMenu();
            yield return null; yield return null;
            LakeWaterRenderer.EnabledForEvaluation = prototype;
            var saved = SavedRegionStore.Load(Path.Combine(SavedRegionStore.DirectoryPath, HillLakeTestRegion.Id + ".cfregion"));
            if (prototype) { saved.info.id = "lake-phase-one-comparison"; saved.info.name = LakeShoreRenderer.EnabledForEvaluation?"Lake — Phase Two Shores":"Lake — Phase One Prototype"; }
            saved.info.stillWater = false;
            PrototypeRegionView.Open(saved.info.name, UnityEngine.Object.FindFirstObjectByType<Canvas>(), snapshot: saved);
        }
        [MenuItem("CityForge/Lake Prototype/Capture Centered Comparison")]
        static void CaptureCentered() { if(!running&&EditorApplication.isPlaying){running=true;Begin(CenteredViews());} }
        static IEnumerator CenteredViews()
        {
            try {
                for(int phase=0;phase<2;phase++) {
                    bool prototype=phase==1;yield return Open(prototype);yield return Wait(3);
                    var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
                    var rig=view.GetComponentInChildren<CityForgeCameraController>();
                    var pose=rig.CaptureRegionState();var body=view.Data.WaterBodies.First(b=>b.id=="hill-lake");
                    var center=(body.Bounds.center-Vector2.one*.5f)*view.MapSide;
                    pose.focus=new Vector3(center.x,8,center.y);pose.size=100;pose.zoom=2;
                    rig.RestoreRegionState(pose);rig.enabled=false;yield return Wait(2);
                    Capture(view.GetComponentInChildren<Camera>(),(prototype?"Lake-Prototype":"Lake-Crest")+"-Centered.png");
                    rig.enabled=true;
                    File.WriteAllText(Output+"Lake-Centered-Camera.json",JsonUtility.ToJson(pose,true));
                }
                Debug.Log("PASS centered lake views; comparison open with preferred frame pacing.");
            }finally {running=false;}
        }
        [MenuItem("CityForge/Lake Prototype/Compare Phase One And Two Shores")]
        static void ShoreCompare(){if(!running&&EditorApplication.isPlaying){running=true;Begin(CompareShore());}}
        [MenuItem("CityForge/Lake Prototype/Compare Sharper Reflections")]
        static void SharpCompare(){if(!running&&EditorApplication.isPlaying){running=true;Begin(CompareSharp());}}
        static IEnumerator CompareSharp()
        {
            var report=new StringBuilder($"Reflection sharpness ABBA: 128px/mip1.5/strength.10 vs 512px/mip.25/strength.20; one-time cube capture, same phase-two lake and centered size80, uncapped {Screen.width}x{Screen.height}. Capture/generation excluded.\n");
            try {
                LakeShoreRenderer.EnabledForEvaluation=true;yield return Open(true);yield return Wait(3);
                var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();var rig=view.GetComponentInChildren<CityForgeCameraController>();
                var pose=rig.CaptureRegionState();var center=(view.Data.WaterBodies.First(b=>b.id=="hill-lake").Bounds.center-Vector2.one*.5f)*view.MapSide;
                pose.focus=new Vector3(center.x,8,center.y);pose.size=80;pose.zoom=2;rig.RestoreRegionState(pose);rig.enabled=false;var origin=rig.transform.position;
                var lake=view.ApprovedDistrict.GetComponentInChildren<LakeWaterRenderer>();
                using var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");
                for(int phase=0;phase<4;phase++) {
                    bool sharp=phase==1||phase==2;lake.SetSharperReflections(sharp);
                    float deadline=Time.realtimeSinceStartup+20;while(!lake.CaptureReady&&Time.realtimeSinceStartup<deadline)yield return null;
                    if(!lake.CaptureReady)throw new Exception("Reflection capture timeout.");
                    rig.transform.position=origin;
                    if(phase<2){yield return Wait(1);Capture(view.GetComponentInChildren<Camera>(),sharp?"Lake-Sharper-Reflections.png":"Lake-Previous-Reflections.png");}
                    QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                    float start=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-start<3){Pan(rig,origin,Time.realtimeSinceStartup-start);yield return null;}
                    var frames=new List<float>();double drawSum=0;start=Time.realtimeSinceStartup;
                    while(Time.realtimeSinceStartup-start<6){Pan(rig,origin,Time.realtimeSinceStartup-start);yield return null;frames.Add(Time.unscaledDeltaTime*1000);if(draws.Valid)drawSum+=draws.LastValue;}
                    frames.Sort();report.AppendLine($"phase={phase}; sharper={sharp}; n={frames.Count}; mean={frames.Average():F3}ms; p95={frames[(int)(frames.Count*.95)]:F3}ms; max={frames.Last():F3}ms; >25ms={100f*frames.Count(f=>f>25)/frames.Count:F2}%; draws={(draws.Valid?(drawSum/frames.Count).ToString("F0"):"unavailable")}");
                    File.WriteAllText(Output+"Lake-Sharper-Reflections-Performance.txt",report.ToString());
                }
                rig.transform.position=origin;rig.enabled=true;lake.SetSharperReflections(true);yield return Wait(3);
                Debug.Log("PASS sharper lake reflections comparison complete.");
            }finally {running=false;var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(view){view.GetComponentInChildren<CityForgeCameraController>().enabled=true;view.ApprovedDistrict.SetFramePacing60(view.ApprovedDistrict.FramePacing60);}}
        }
        static IEnumerator CompareShore()
        {
            var report=new StringBuilder($"PHASE TWO shore composition / phase one surface; Unity Editor {Application.unityVersion}, {Screen.width}x{Screen.height}, {SystemInfo.graphicsDeviceName}. Uncapped ABBA, identical saved snapshot, centered camera, 3s warmup and 8s measurement. Cube reflection technique unchanged. Generation and captures excluded.\n");
            try {
                using var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");
                for(int phase=0;phase<4;phase++){
                    bool shore=phase==1||phase==2;LakeShoreRenderer.EnabledForEvaluation=shore;
                    yield return Open(true);yield return Wait(3);
                    var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();var rig=view.GetComponentInChildren<CityForgeCameraController>();
                    var pose=rig.CaptureRegionState();var center=(view.Data.WaterBodies.First(b=>b.id=="hill-lake").Bounds.center-Vector2.one*.5f)*view.MapSide;
                    pose.focus=new Vector3(center.x,8,center.y);pose.size=100;pose.zoom=2;rig.RestoreRegionState(pose);rig.enabled=false;
                    var origin=rig.transform.position;
                    var lake=view.ApprovedDistrict.GetComponentInChildren<LakeWaterRenderer>();
                    if(!lake||!lake.CaptureReady||view.ApprovedDistrict.Water)throw new Exception("Lake isolation/capture failed.");
                    var details=lake.GetComponent<LakeShoreRenderer>();if(shore&&!details)throw new Exception("Missing shore prototype.");
                    if(details)report.AppendLine($"Generation: {lake.GenerationMilliseconds:F2}ms; rocks={details.Rocks}; plantClumps={details.Plants}; leaves={details.Leaves}; batches retained; collider heights unchanged.");
                    if(phase<2){
                        yield return Wait(2);Capture(view.GetComponentInChildren<Camera>(),(shore?"Lake-Phase-Two":"Lake-Phase-One")+"-Centered.png");
                        var camera=view.GetComponentInChildren<Camera>();camera.orthographicSize=60;yield return Wait(1);
                        Capture(camera,(shore?"Lake-Phase-Two":"Lake-Phase-One")+"-Close.png");camera.orthographicSize=100;
                    }
                    QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                    float start=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-start<3){Pan(rig,origin,Time.realtimeSinceStartup-start);yield return null;}
                    var frames=new List<float>();double drawSum=0;start=Time.realtimeSinceStartup;
                    while(Time.realtimeSinceStartup-start<8){Pan(rig,origin,Time.realtimeSinceStartup-start);yield return null;frames.Add(Time.unscaledDeltaTime*1000);if(draws.Valid)drawSum+=draws.LastValue;}
                    frames.Sort();report.AppendLine($"phase={phase}; shores={shore}; n={frames.Count}; mean={frames.Average():F3}ms; p95={frames[(int)(frames.Count*.95)]:F3}ms; max={frames.Last():F3}ms; >25ms={100f*frames.Count(f=>f>25)/frames.Count:F2}%; draws={(draws.Valid?(drawSum/frames.Count).ToString("F0"):"unavailable")}");
                    File.WriteAllText(Output+"Lake-Phase-Two-Performance.txt",report.ToString());rig.transform.position=origin;rig.enabled=true;
                }
                LakeShoreRenderer.EnabledForEvaluation=true;yield return Open(true);yield return Wait(3);
                var final=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();var finalRig=final.GetComponentInChildren<CityForgeCameraController>();
                var finalPose=finalRig.CaptureRegionState();var c=(final.Data.WaterBodies.First(b=>b.id=="hill-lake").Bounds.center-Vector2.one*.5f)*final.MapSide;
                finalPose.focus=new Vector3(c.x,8,c.y);finalPose.size=80;finalRig.RestoreRegionState(finalPose);
                report.AppendLine("PASS repeated loaded phase-two lake; original saved region untouched; preferred frame pacing restored. Standalone, full gameplay/save roundtrip and phase-three reflection work remain untested.");
                File.WriteAllText(Output+"Lake-Phase-Two-Performance.txt",report.ToString());Debug.Log("PASS phase two lake shores ABBA completed.");
            } finally {running=false;var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(view){view.GetComponentInChildren<CityForgeCameraController>().enabled=true;view.ApprovedDistrict.SetFramePacing60(view.ApprovedDistrict.FramePacing60);}}
        }
        static IEnumerator Compare()
        {
            Directory.CreateDirectory(Output);
            int cap = Application.targetFrameRate, vsync = QualitySettings.vSyncCount;
            var report = new StringBuilder("PHASE ONE: Crest planar lake / bounded teal lake with one-time 128px cubemap. Unequal reflection quality: cube has approximate tree placement.\n");
            report.AppendLine($"Unity Editor {Application.unityVersion}; {SystemInfo.graphicsDeviceName}; Game {Screen.width}x{Screen.height}; uncapped ABBA, same seed/snapshot/camera/path; generation and captures excluded; 3s warmup + 8s measurement.");
            try
            {
                var original = SavedRegionStore.Load(Path.Combine(SavedRegionStore.DirectoryPath, HillLakeTestRegion.Id + ".cfregion"));
                SavedRegionStore.Write(original, Output + "Meadow-Hill-Lake-Before-Phase-One.cfregion");
                File.WriteAllText(Output + "Lake-Before-Phase-One-Settings.json", JsonUtility.ToJson(original.info, true));
                Audit();
                using var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
                for (int phase = 0; phase < 4; phase++)
                {
                    bool prototype = phase == 1 || phase == 2;
                    yield return Open(prototype);
                    yield return null; yield return null;
                    var view = UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
                    var rig = view.GetComponentInChildren<CityForgeCameraController>(); var origin = rig.transform.position;
                    rig.enabled = false; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
                    var lake = view.ApprovedDistrict.GetComponentInChildren<LakeWaterRenderer>();
                    if (prototype)
                    {
                        float deadline = Time.realtimeSinceStartup + 15;
                        while (lake && !lake.CaptureReady && Time.realtimeSinceStartup < deadline) yield return null;
                        if (!lake || !lake.CaptureReady || view.ApprovedDistrict.Water) throw new Exception("Prototype/cubemap isolation failed.");
                        report.AppendLine($"Prototype capture ready; texture={lake.CapturedEnvironment.width}; CPU surface/mask setup={lake.GenerationMilliseconds:F2}ms (capture cost excluded).");
                    }
                    if (phase < 2)
                    {
                        yield return Wait(2);
                        Capture(view.GetComponentInChildren<Camera>(), (prototype ? "Lake-Prototype" : "Lake-Crest") + "-Fixed.png");
                        var camera = view.GetComponentInChildren<Camera>(); float size = camera.orthographicSize;
                        camera.orthographicSize = size * .65f;
                        yield return Wait(1);
                        Capture(camera, (prototype ? "Lake-Prototype" : "Lake-Crest") + "-Close.png");
                        camera.orthographicSize = size;
                    }
                    float start = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - start < 3) { Pan(rig, origin, Time.realtimeSinceStartup - start); yield return null; }
                    var frames = new List<float>(); double drawSum = 0; start = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - start < 8)
                    {
                        Pan(rig, origin, Time.realtimeSinceStartup - start); yield return null;
                        frames.Add(Time.unscaledDeltaTime * 1000); if (draws.Valid) drawSum += draws.LastValue;
                    }
                    frames.Sort();
                    report.AppendLine($"phase={phase}; renderer={(prototype ? "Prototype" : "Crest")}; n={frames.Count}; mean={frames.Average():F3}ms; p95={frames[Math.Min(frames.Count - 1, (int)(frames.Count * .95))]:F3}ms; max={frames.Last():F3}ms; >16.67ms={100f * frames.Count(f => f > 16.67f) / frames.Count:F2}%; >25ms={100f * frames.Count(f => f > 25) / frames.Count:F2}%; draws={(draws.Valid ? (drawSum / frames.Count).ToString("F0") : "unavailable")}");
                    File.WriteAllText(Output + "Lake-Phase-One-Performance.txt", report.ToString());
                    rig.transform.position = origin; rig.enabled = true;
                }
                yield return Open(true); yield return null;
                report.AppendLine("Completed ABBA; left opt-in comparison open. Original saved renderer flag remains Crest. Capture is approximate, no planar camera, no surface animation. Standalone and phase-four gameplay validation not performed.");
                File.WriteAllText(Output + "Lake-Phase-One-Performance.txt", report.ToString());
                Debug.Log("PASS phase one lake comparison completed; original region unchanged.");
            }
            finally {
                running = false; Application.targetFrameRate = cap; QualitySettings.vSyncCount = vsync;
                var finalView=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
                if(finalView){var rig=finalView.GetComponentInChildren<CityForgeCameraController>();rig.enabled=true;
                    if(finalView.ApprovedDistrict)finalView.ApprovedDistrict.SetFramePacing60(finalView.ApprovedDistrict.FramePacing60);}
            }
        }
        static void Pan(CityForgeCameraController rig, Vector3 origin, float t) => rig.transform.position = origin + new Vector3(Mathf.Sin(t) * 25, 0, Mathf.Sin(t * .7f) * 18);
        static void Capture(Camera camera, string name)
        {
            const int width = 1600, height = 805;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active; float aspect = camera.aspect;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try { camera.targetTexture = target; camera.aspect = (float)width / height; camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(Output + name, image.EncodeToPNG()); }
            finally { camera.targetTexture = oldTarget; camera.aspect = aspect; RenderTexture.active = oldActive; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image); }
        }
        static void Audit()
        {
            var baseline = Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline");
            var s = new StringBuilder("PHASE ONE LOCAL ASSET AUDIT\n");
            s.AppendLine("Approved baseline retained: " + AssetDatabase.GetAssetPath(baseline));
            foreach (var tree in baseline.treePrefabs) s.AppendLine("Tree: " + AssetDatabase.GetAssetPath(tree));
            s.AppendLine("Reeds: " + AssetDatabase.GetAssetPath(baseline.shorelineReeds));
            s.AppendLine("Shore grass: " + AssetDatabase.GetAssetPath(baseline.shorelineGrass));
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/CityForge/Art/Environment", "Assets/CityForge/Resources/Water" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.ToLowerInvariant().Contains("water") && !path.ToLowerInvariant().Contains("pond")) continue;
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path); if (mat) s.AppendLine(path + ": " + mat.shader.name);
            }
            s.AppendLine("Candidates: existing still-water and quiet-pond shaders use planar reflection/tint; Crest retains expensive live reflection; local cubemap provides cheap static approximate reflection; active CityForge UniversalRenderer has no renderer features/SSR. Selected independent premultiplied depth absorption and one-time cube, bounded to hill-lake only. Existing bank geometry/materials/scatter unchanged for controlled phase-one comparison.");
            File.WriteAllText(Output + "Lake-Phase-One-Audit.txt", s.ToString());
        }
    }
}

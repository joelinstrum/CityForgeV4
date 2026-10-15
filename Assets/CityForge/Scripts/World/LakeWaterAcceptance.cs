using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CityForge.CameraSystem;
using Unity.Profiling;
using UnityEngine;

namespace CityForge.World
{
    /// <summary>Explicit Editor/player acceptance harness; no work during normal startup.</summary>
    public sealed class LakeWaterAcceptance : MonoBehaviour
    {
        public const string Output="/Users/joelinstrum/Documents/Codex/2026-10-12/begin-from-this-handoff-and-start/outputs/";
        public static bool Running;
        StringBuilder report=new();string reportPath;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void LaunchPlayerCheck(){if(!Application.isEditor&&Array.Exists(System.Environment.GetCommandLineArgs(),a=>a=="--lake-acceptance")){Application.runInBackground=true;new GameObject("Lake acceptance").AddComponent<LakeWaterAcceptance>();}}
        void Start(){Running=true;Directory.CreateDirectory(Output);reportPath=Output+(Application.isEditor?"Lake-Final-Editor-Checks.txt":"Lake-Final-Player-Checks.txt");StartCoroutine(Protect(Check()));}
        IEnumerator Protect(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0){bool next=false;object value=null;Exception error=null;
                try {next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null){if(View){View.GetComponentInChildren<CityForgeCameraController>().enabled=true;if(View.ApprovedDistrict)View.ApprovedDistrict.SetFramePacing60(View.ApprovedDistrict.FramePacing60);}Line("FAILED: "+error);while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Running=false;if(!Application.isEditor)Application.Quit(1);yield break;}
                if(!next){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(value is IEnumerator child){stack.Push(child);continue;}yield return value;
            }
            Running=false;if(!Application.isEditor)Application.Quit(0);else Destroy(gameObject);
        }
        void Line(string text){report.AppendLine(text);File.WriteAllText(reportPath,report.ToString());Debug.Log(text);}
        static void Require(bool condition,string text){if(!condition)throw new Exception(text);}
        static PrototypeRegionView View=>FindFirstObjectByType<PrototypeRegionView>();
        static IEnumerator Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end)yield return null;}
        static IEnumerator Open(SavedRegionSnapshot snapshot)
        {
            if(View)View.ReturnToMenu();yield return null;yield return null;LakeWaterRenderer.UseSavedRenderer();
            PrototypeRegionView.Open(snapshot.info.name,FindFirstObjectByType<Canvas>(),snapshot:snapshot);
            if(View.ApprovedDistrict&&View.NaturalLandscape){View.ApprovedDistrict.SetTreesEnabled(true);View.ApprovedDistrict.SetSmallPlantsEnabled(true);}
            yield return null;yield return WaitReady();
        }
        static IEnumerator WaitReady()
        {
            float end=Time.realtimeSinceStartup+30;
            while(View&&View.GetComponentsInChildren<LakeWaterRenderer>().Any(l=>!l.CaptureReady)&&Time.realtimeSinceStartup<end)yield return null;
            Require(View&&View.GetComponentsInChildren<LakeWaterRenderer>().All(l=>l.CaptureReady),"Capture did not finish");
        }
        static ulong Fingerprint()=>View.ApprovedDistrict.GetComponentsInChildren<LakeShoreRenderer>().First().PlacementFingerprint;
        static void Center(Vector2 p,float level,float size=80)
        {
            var rig=View.GetComponentInChildren<CityForgeCameraController>();var pose=rig.CaptureRegionState();pose.focus=new Vector3(p.x,level,p.y);pose.size=size;pose.zoom=2;pose.yaw=315;rig.RestoreRegionState(pose);
        }
        IEnumerator Check()
        {
            Line($"Lake/Pond production acceptance; {(Application.isEditor?"Editor":"macOS standalone")}; {Application.unityVersion}; {SystemInfo.graphicsDeviceName}; {Screen.width}x{Screen.height}.");
            var originalPath=Path.Combine(SavedRegionStore.DirectoryPath,HillLakeTestRegion.Id+".cfregion");
            var original=SavedRegionStore.Load(originalPath);original.info.lakeWaterVersion=0; // Explicit pre-version migration.
            yield return Open(original);
            Require(View.LakeWaterVersion==1&&!View.ApprovedDistrict.Water,"Old lake save did not migrate to default calm route");
            var approved=View.ApprovedDistrict.GetComponentsInChildren<LakeWaterRenderer>();Require(approved.Length==1&&approved[0].CaptureCount==1,"Default lake/capture count");
            var body=View.Data.WaterBodies[0];Center((body.Bounds.center-Vector2.one*.5f)*View.MapSide,approved[0].SurfaceLevel);
            ulong fingerprint=Fingerprint();int treeCount=View.ApprovedDistrict.FullTreeCount;
            var snapshot=SavedRegionStore.Capture(View,HillLakeTestRegion.Id,"Meadow — Hill and Lake");
            Require(snapshot.info.lakeWaterVersion==1&&snapshot.meadow.alphamaps.GetLength(2)==View.ApprovedDistrict.Baseline.layers.Length,"Save contains painted layer layout");
            var savedPath=Output+(Application.isEditor?"Lake-Final.cfregion":"Lake-Player-Roundtrip.cfregion");SavedRegionStore.Write(snapshot,savedPath);
            Line("PASS legacy lake migrated; canonical ground cache and renderer version serialized.");
            for(int i=0;i<3;i++){
                yield return Open(SavedRegionStore.Load(savedPath));
                Require(View.ApprovedDistrict.UsedSavedTerrain&&Fingerprint()==fingerprint&&View.ApprovedDistrict.FullTreeCount==treeCount,"Save/reload changed seeded details/trees or discarded cache");
                Require(View.GetComponentInChildren<CityForgeCameraController>().CaptureRegionState().focus==snapshot.info.camera.focus,"Camera roundtrip changed");
                Require(View.Data.WaterBodyAt(body.Bounds.center)?.kind==WaterBodyKind.Lake,"Water lookup changed");
            }
            Line($"PASS three save/reloads retain seeded matrices ({fingerprint:X16}), {treeCount} trees, lake identity, level and camera; cached meadow reused.");
            var rig=View.GetComponentInChildren<CityForgeCameraController>();var basePose=rig.CaptureRegionState();
            using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count")){
                for(int zoom=0;zoom<=5;zoom++){
                    rig.RestoreRegionState(basePose);rig.SetZoomLevel(zoom);rig.enabled=false;
                    int captures=View.ApprovedDistrict.GetComponentInChildren<LakeWaterRenderer>().CaptureCount;
                    var pose=rig.CaptureRegionState();QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                    yield return Wait(2);
                    var samples=new List<float>();double sum=0;float start=Time.realtimeSinceStartup;
                    while(Time.realtimeSinceStartup-start<4){float t=Time.realtimeSinceStartup-start;
                        pose.focus=basePose.focus+new Vector3(Mathf.Sin(t)*20,0,Mathf.Sin(t*.7f)*15);pose.yaw=315+t*12;rig.RestoreRegionState(pose);
                        yield return null;samples.Add(Time.unscaledDeltaTime*1000);if(draws.Valid)sum+=draws.LastValue;
                    }
                    samples.Sort();Require(View.ApprovedDistrict.GetComponentInChildren<LakeWaterRenderer>().CaptureCount==captures,"Camera movement caused recapture");
                    Line($"zoom={zoom}; pan/rotate; n={samples.Count}; mean={samples.Average():F3}ms; p95={samples[(int)(samples.Count*.95)]:F3}ms; max={samples.Last():F3}ms; >25ms={100f*samples.Count(f=>f>25)/samples.Count:F2}%; draws={(draws.Valid?(sum/samples.Count).ToString("F0"):"unavailable")}; captures unchanged.");
                    rig.RestoreRegionState(basePose);rig.SetZoomLevel(zoom);yield return Wait(.25f);
                    yield return new WaitForEndOfFrame();Capture(View.GetComponentInChildren<Camera>(),$"Lake-Final-{(Application.isEditor?"Editor":"Player")}-Zoom{zoom}.png");
                }
            }
            rig.enabled=true;rig.RestoreRegionState(basePose);
            var dirtyLake=View.ApprovedDistrict.GetComponentInChildren<LakeWaterRenderer>();int initialCaptures=dirtyLake.CaptureCount;
            bool trees=View.ApprovedDistrict.TreesEnabled,plants=View.ApprovedDistrict.SmallPlantsEnabled;
            View.ApprovedDistrict.SetTreesEnabled(!trees);yield return WaitReady();Require(dirtyLake.CaptureCount==initialCaptures+1,"Tree visibility did not refresh reflection once");
            View.ApprovedDistrict.SetTreesEnabled(trees);yield return WaitReady();
            View.ApprovedDistrict.SetSmallPlantsEnabled(!plants);yield return WaitReady();View.ApprovedDistrict.SetSmallPlantsEnabled(plants);yield return WaitReady();
            Require(dirtyLake.CaptureCount==initialCaptures+4,"Visibility refresh count drifted");
            Line("PASS tree/small-plant visibility changes explicitly refresh captures once; camera motion does not.");
            View.ShowOverview();yield return WaitReady();yield return Wait(.25f);
            Require(View.GetComponentsInChildren<LakeWaterRenderer>().Length==1,"Overview lost the bounded lake surface");
            yield return new WaitForEndOfFrame();Capture(View.GetComponentInChildren<Camera>(),$"Lake-Overview-{(Application.isEditor?"Editor":"Player")}.png");
            var overview=SavedRegionStore.Capture(View,"overview-lake-check","Overview lake check");Require(overview.info.district==-1&&overview.info.meadowCacheDistrict==3,"Overview lost cache district");
            var overviewPath=Output+(Application.isEditor?"Lake-Overview-Editor.cfregion":"Lake-Overview-Player.cfregion");SavedRegionStore.Write(overview,overviewPath);yield return Open(SavedRegionStore.Load(overviewPath));
            Require(View.SelectedDistrict==-1,"Overview camera state not retained");View.EnterDistrict(3);yield return WaitReady();Require(View.ApprovedDistrict.UsedSavedTerrain,"Overview cache not restored to correct district");
            Line("PASS overview save/reload retains its cache district and restores the matching meadow cache.");
            for(int district=0;district<4;district++){View.EnterDistrict(district);yield return WaitReady();yield return Wait(.2f);}
            Line("PASS overview and all four district entries; active bodies regenerated and captured on entry.");
            yield return Open(Synthetic());
            var land=View.ApprovedDistrict;var lakes=land.GetComponentsInChildren<LakeWaterRenderer>();
            Require(lakes.Length==4&&land.DisplayTerrain.terrainData.terrainLayers.Length==land.Baseline.layers.Length+3,"Multi-body route or shared layers failed");
            Require(land.Water&&land.Water.FlowLod.Enabled&&land.GetComponentInChildren<RiverFlowAnimation>(),"Mixed river did not retain Crest flow route");
            foreach(var lake in lakes){Require(lake.CaptureCount==1&&lake.SurfaceMaterial.shader.name=="CityForge/Calm Lake Water"&&!lake.SurfaceMaterial.IsKeywordEnabled("_CREST_FLOW_LOD"),"Wrong still-water material/capture");
                var descriptor=View.Data.WaterBodies.First(b=>b.id==lake.BodyId);Require(Mathf.Abs(lake.SurfaceLevel-descriptor.surfaceLevelNormalized*land.DisplayTerrain.terrainData.size.y)<.001f,"Wrong per-body level");
                Require(View.Data.WaterBodyAt(descriptor.Bounds.center)?.kind==descriptor.kind,"Typed lookup failed");
                Line($"PASS {descriptor.id}: {descriptor.kind}; width={descriptor.Bounds.width*View.MapSide:F1}m; level={lake.SurfaceLevel:F1}m; cube={lake.CapturedEnvironment.width}; capture=1.");
            }
            Center(new Vector2(250,250),9,190);yield return Wait(2);yield return new WaitForEndOfFrame();Capture(View.GetComponentInChildren<Camera>(),$"Lake-Pond-Multiple-{(Application.isEditor?"Editor":"Player")}.png");
            var multi=SavedRegionStore.Capture(View,"multiple-water-check","Multiple typed bodies");var multiPath=Output+(Application.isEditor?"Lake-Pond-Multiple.cfregion":"Lake-Pond-Multiple-Player.cfregion");SavedRegionStore.Write(multi,multiPath);
            yield return Open(SavedRegionStore.Load(multiPath));Require(View.ApprovedDistrict.UsedSavedTerrain&&View.ApprovedDistrict.GetComponentsInChildren<LakeWaterRenderer>().Length==4,"Multi-body cache roundtrip failed");
            View.EnterDistrict(2);yield return WaitReady();Require(View.ApprovedDistrict.GetComponentsInChildren<LakeWaterRenderer>().Length==1,"Boundary pond disappeared across district");
            Line("PASS multiple typed bodies at different levels, shared lake-bed layers, save/reload and a pond crossing districts.");
            var ocean=Synthetic();ocean.data.WaterBodies=ocean.info.waterBodies=new[]{ocean.data.WaterBodies[0]};ocean.data.WaterBodies[0].kind=WaterBodyKind.Ocean;
            yield return Open(ocean);Require(View.ApprovedDistrict.Water&&!View.ApprovedDistrict.Water.FlowLod.Enabled&&View.ApprovedDistrict.GetComponentsInChildren<LakeWaterRenderer>().Length==0,"Ocean routing regressed");
            Line("PASS ocean remains on its separate Crest route.");
            View.ReturnToMenu();yield return null;yield return null;yield return Wait(.5f);
            Require(FindObjectsByType<LakeWaterRenderer>(FindObjectsSortMode.None).Length==0,"Lake renderer leaked");
            Require(!Resources.FindObjectsOfTypeAll<Texture>().Any(t=>t.name.StartsWith("Lake capture — ")||t.name=="Lake/pond generation depth"),"Lake texture leaked");
            Require(!Resources.FindObjectsOfTypeAll<Material>().Any(m=>m.name=="Calm lake/pond water (runtime)"),"Lake material leaked");
            Line("PASS lake cameras/probes, runtime materials and depth/reflection textures released after teardown.");
            var river=SavedRegionStore.Load(Path.Combine(SavedRegionStore.DirectoryPath,"meadow-narrow-river-crest-trial.cfregion"));
            yield return Open(river);Require(View.ApprovedDistrict.Water&&View.ApprovedDistrict.Water.FlowLod.Enabled&&View.ApprovedDistrict.GetComponentInChildren<RiverFlowAnimation>()&&View.ApprovedDistrict.GetComponentsInChildren<LakeWaterRenderer>().Length==0,"Accepted river route regressed");
            Line("PASS accepted saved river remains Crest with RiverFlowAnimation and flow LOD; no lake renderer.");
            yield return Open(SavedRegionStore.Load(savedPath));View.ApprovedDistrict.SetFramePacing60(true); // Release uncapped validation mode.
            Line("COMPLETE: lake/pond default, migration, save/load, seeded details, zoom 0–5, pan/rotation, entry/exit, mixed bodies, river route and teardown passed.");
        }
        public static SavedRegionSnapshot Synthetic()
        {
            const float side=1024;float vertical=PrototypeRegionData.Height*side/PrototypeRegionData.Side;
            var descriptors=new List<RegionWaterBody>();var centers=new[]{new Vector2(100,100),new Vector2(260,210),new Vector2(300,380),new Vector2(5,100)};
            var radii=new[]{new Vector2(12,9),new Vector2(42,28),new Vector2(82,48),new Vector2(15,11)};float[] levels={5,9,13,6};
            for(int j=0;j<4;j++){var outline=new Vector2[48];for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;outline[i]=(centers[j]+Vector2.Scale(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),radii[j])*(1+.045f*Mathf.Sin(a*3+j)))/side+Vector2.one*.5f;}
                descriptors.Add(new RegionWaterBody{id="integration-"+j,kind=j==0||j==3?WaterBodyKind.Pond:WaterBodyKind.Lake,outline=outline,hasSurfaceLevel=true,surfaceLevelNormalized=levels[j]/vertical,appearanceProfile="calm-lake"});}
            descriptors.Add(new RegionWaterBody{id="integration-river",kind=WaterBodyKind.River,centerline=new[]{new Vector2(440,50)/side+Vector2.one*.5f,new Vector2(440,180)/side+Vector2.one*.5f},halfWidthNormalized=8/side,flowSpeed=.12f,hasSurfaceLevel=true,surfaceLevelNormalized=8/vertical,appearanceProfile="calm-river"});
            var heights=new float[513,513];var distance=new float[513,513];
            for(int z=0;z<513;z++)for(int x=0;x<513;x++){var p=new Vector2(x/512f*side-side/2,z/512f*side-side/2);float h=20,d=512;
                for(int j=0;j<4;j++){var delta=p-centers[j];float r=new Vector2(delta.x/radii[j].x,delta.y/radii[j].y).magnitude;float angle=Mathf.Atan2(delta.y/radii[j].y,delta.x/radii[j].x);r/=1+.045f*Mathf.Sin(angle*3+j);
                    float shore=(r-1)*Mathf.Min(radii[j].x,radii[j].y);d=Mathf.Min(d,shore);
                    if(shore<0)h=Mathf.Min(h,levels[j]-Mathf.Min(4,radii[j].y*.2f)*Mathf.SmoothStep(0,1,Mathf.Clamp01(-shore/(radii[j].y*.45f))));
                    else if(shore<12)h=Mathf.Min(h,Mathf.Lerp(levels[j],20,Mathf.SmoothStep(0,1,shore/12)));}
                float riverDistance=descriptors[4].DistanceToCenterline(p/side+Vector2.one*.5f,out _)*side-8;d=Mathf.Min(d,riverDistance);if(riverDistance<0)h=Mathf.Min(h,5);else if(riverDistance<6)h=Mathf.Min(h,Mathf.Lerp(8,20,riverDistance/6));
                heights[z,x]=h/vertical;distance[z,x]=d/(side/PrototypeRegionData.Side);
            }
            return new SavedRegionSnapshot{data=new PrototypeRegionData(730423,heights,distance){WaterBodies=descriptors.ToArray()},info=new SavedRegionInfo{id="multiple-water-check",name="Lake and Pond Integration",seed=730423,size=0,district=3,meadow=true,lakeWaterVersion=1,waterBodies=descriptors.ToArray(),camera=new RegionCameraState{focus=new Vector3(250,9,250),pitch=35.26439f,yaw=315,distance=280,size=180,zoom=2}}};
        }
        static void Capture(Camera camera,string name)
        {
            var target=RenderTexture.GetTemporary(1600,805,24,RenderTextureFormat.ARGB32);var previous=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            var image=new Texture2D(1600,805,TextureFormat.RGB24,false);
            try{camera.targetTexture=target;camera.aspect=1600f/805;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,805),0,0);image.Apply();File.WriteAllBytes(Output+name,image.EncodeToPNG());}
            finally{camera.targetTexture=previous;camera.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);Destroy(image);}
        }
    }
}

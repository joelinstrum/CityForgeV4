#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using CityForge.World;
using CityForge.UI;
namespace CityForge.Editor
{
    // Explicit opt-in. Read-only probes can inspect an open region; scene trials require the menu.
    [InitializeOnLoad] public static class LoadingPerformanceChecks
    {
        const string Request="Library/LoadingPerformance.request";
        static LoadingPerformanceChecks(){EditorApplication.update+=Poll;}
        static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;if(!Application.isPlaying){EditorApplication.isPlaying=true;return;}string output=File.ReadAllText(Request).Trim();File.Delete(Request);EditorApplication.isPaused=false;EditorCoroutineRunner.Begin(Guard(Run(output),output));}
        static IEnumerator Guard(IEnumerator work,string output){bool background=Application.runInBackground;Application.runInBackground=true;try{while(true){EditorApplication.isPaused=false;bool more;try{more=work.MoveNext();}catch(Exception e){File.AppendAllText(Path.Combine(output,"Profile.txt"),"\nFAIL: "+e);Debug.LogException(e);yield break;}if(!more)yield break;yield return work.Current;}}finally{Application.runInBackground=background;}}
        static string CheckDistances(){
            var random=new System.Random(98123);int count=0;float flowError=0,error=0;double bruteMs=0,indexMs=0;
            foreach(int version in new[]{1,4,5})foreach(float side in new[]{4096f,8192f,16384f})foreach(var shape in new[]{RegionTerrainShape.Land,RegionTerrainShape.MountainBorder}){
                var recipe=new RegionCreationSettings{seed=8472,waterwayVersion=version,streams=2,rivers=1,terrainShape=shape};
                foreach(var body in RegionalRiverNetwork.CreateSeparated(recipe,side)){
                    var points=new Vector2[200];var expected=new float[points.Length];for(int i=0;i<points.Length;i++)points[i]=new Vector2((float)random.NextDouble()*1.2f-.1f,(float)random.NextDouble()*1.2f-.1f);
                    var timer=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<points.Length;i++)expected[i]=body.DistanceToCenterline(points[i],out _);bruteMs+=timer.Elapsed.TotalMilliseconds;
                    timer.Restart();for(int i=0;i<points.Length;i++){float actual=body.IndexedCenterlineDistance(points[i]);error=Mathf.Max(error,Mathf.Abs(actual-expected[i]));if(Mathf.Abs(actual-expected[i])>.0000002f)throw new Exception("Indexed route distance mismatch");count++;}indexMs+=timer.Elapsed.TotalMilliseconds;
                    var data=new PrototypeRegionData(1,new float[513,513],new float[513,513]){WaterBodies=new[]{body}};
                    for(int i=0;i<points.Length;i++){
                        var world=(points[i]-Vector2.one*.5f)*side;int channel=body.channelVersion;body.channelVersion=0;
                        var reference=LakeWaterRenderer.RiverCurrentCoordinates(data,side,world);body.channelVersion=channel;
                        var actual=LakeWaterRenderer.RiverCurrentCoordinates(data,side,world);
                        float difference=(actual-reference).magnitude;flowError=Mathf.Max(flowError,difference);if(difference>.002f)throw new Exception("Indexed flow mismatch: "+difference);
                    }
                }
            }
            return "Indexed flow regression: max vector error="+flowError+"\nExact distance regression: "+count+" points; maximum normalized error="+error+"; brute="+bruteMs.ToString("F1")+"ms; indexed="+indexMs.ToString("F1")+"ms\n";
        }
        static IEnumerator ProbeDistrict(MeadowDistrictLandscape land,string report){
            const int n=256;double sum=0;var view=land.View;
            var tests=new System.Collections.Generic.Dictionary<string,Func<Vector2,float>>{
                {"biome",p=>view.Data.Biomes.At(p/view.MapSide+Vector2.one*.5f).woodland},
                {"canopy",p=>land.Canopy(p)}, {"road",p=>land.RoadDistance(p)}, {"shore",p=>land.ShoreDistance(p)},
                {"stream nearest",p=>GeneratedStreamAppearance.Near(view,p,out _,out float d)?d:0},
                {"noise",p=>land.Noise(p,13,8)}
            };
            foreach(var test in tests){double ms=0;for(int z=0;z<n;z++){
                var timer=System.Diagnostics.Stopwatch.StartNew();for(int x=0;x<n;x++){var p=land.Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*land.Side;sum+=test.Value(p);}ms+=timer.Elapsed.TotalMilliseconds;if(z%16==0)yield return null;
            }File.AppendAllText(report,"District CPU sample "+test.Key+"="+ms.ToString("F1")+"ms/65536; projected full map="+(ms*land.DisplayTerrain.terrainData.alphamapResolution*land.DisplayTerrain.terrainData.alphamapResolution/(n*n)/1000).ToString("F3")+"s\n");}
            File.AppendAllText(report,"Sample checksum="+sum+"\n");
        }
        static string CheckPaint(PrototypeRegionData data){
            // Exercise the actual worker sampler against the pre-worker formula.
            const int n=256;const float mapSide=8192,side=1024,roadWidth=5,shoreWidth=8,radius=12,roadLimit=9;
            var origin=new Vector2(-1024,-2048);var offset=new Vector2(349,528);
            var canopy=new System.Collections.Generic.Dictionary<Vector2Int,System.Collections.Generic.List<Vector2>>();
            var random=new System.Random(721);for(int i=0;i<500;i++){var p=origin+new Vector2((float)random.NextDouble(),(float)random.NextDouble())*side;var key=Vector2Int.FloorToInt(p/16);if(!canopy.TryGetValue(key,out var list))canopy[key]=list=new System.Collections.Generic.List<Vector2>();list.Add(p);}
            var roads=new System.Collections.Generic.List<Vector2>{origin,origin+Vector2.one*side};
            var cells=new System.Collections.Generic.Dictionary<Vector2Int,System.Collections.Generic.List<int>>();for(int z=-130;z<=0;z++)for(int x=-66;x<=2;x++)cells[new Vector2Int(x,z)]=new System.Collections.Generic.List<int>{1};
            var type=typeof(MeadowDistrictLandscape).GetNestedType("ControlMapSampling",System.Reflection.BindingFlags.NonPublic);var sample=Activator.CreateInstance(type,true);var maps=new float[n,n,6];
            void Set(string name,object value)=>type.GetField(name).SetValue(sample,value);
            Set("n",n);Set("maps",maps);Set("origin",origin);Set("offset",offset);Set("side",side);Set("mapSide",mapSide);Set("roadWidth",roadWidth);Set("shoreWidth",shoreWidth);Set("canopyRadius",radius);Set("roadLimit",roadLimit);Set("data",data);Set("biomes",data.Biomes);Set("canopy",canopy);Set("roadCells",cells);Set("road",roads);
            var fill=(Action<int>)Delegate.CreateDelegate(typeof(Action<int>),sample,type.GetMethod("FillRow"));
            foreach(var body in data.WaterBodies){var bounds=body.Bounds;}
            var timer=System.Diagnostics.Stopwatch.StartNew();System.Threading.Tasks.Parallel.For(0,n,new System.Threading.Tasks.ParallelOptions{MaxDegreeOfParallelism=4},fill);double parallelMs=timer.Elapsed.TotalMilliseconds;timer.Restart();
            float maximum=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                var p=origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*side;var uv=p/mapSide+Vector2.one*.5f;
                float forest=data.Biomes.At(uv).woodland,can=0;var cell=Vector2Int.FloorToInt(p/16);
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)if(canopy.TryGetValue(cell+new Vector2Int(dx,dz),out var trees))foreach(var tree in trees){float d=Vector2.Distance(p,tree)/radius;if(d<1)can+=Mathf.SmoothStep(0,1,1-d)*.6f;}can=Mathf.Clamp01(can);
                var delta=roads[1]-roads[0];float t=Mathf.Clamp01(Vector2.Dot(p-roads[0],delta)/delta.sqrMagnitude);float roadDistance=Mathf.Min(roadLimit,Vector2.Distance(p,roads[0]+delta*t));
                float patch=Mathf.PerlinNoise(p.x/13+offset.x+8,p.y/13+offset.y-8),road=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(roadWidth*.35f,roadWidth,roadDistance));
                float bank=(1-Mathf.SmoothStep(0,1,Mathf.Abs(RegionalRiverNetwork.SignedDistance(data,uv)*mapSide)/shoreWidth))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.58f,.78f,patch));
                float dry=data.Biomes.At(uv).dryness,soil=road*.12f+bank*.23f+dry*.08f,grass=Mathf.Max(0,1-soil),litter=can*(.5f+patch*.25f);
                var expected=new[]{soil,grass*(1-forest*.4f)*(1-litter),grass*forest*.4f*(1-litter),grass*litter};for(int l=0;l<4;l++)maximum=Mathf.Max(maximum,Mathf.Abs(maps[z,x,l]-expected[l]));
            }
            if(maximum>.000001f)throw new Exception("Worker paint mismatch: "+maximum);
            return "Worker paint regression: 65536 texels; max weight error="+maximum+"; parallel="+parallelMs.ToString("F1")+"ms; original reference="+timer.Elapsed.TotalMilliseconds.ToString("F1")+"ms\n";
        }
        static IEnumerator CheckBanks(MeadowDistrictLandscape land,string report){
            var terrain=land.DisplayTerrain.terrainData;int n=terrain.alphamapResolution;var original=terrain.GetAlphamaps(0,0,n,n);
            var method=typeof(MeadowDistrictLandscape).GetMethod("PaintGeneratedRiverBanks",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            try{
                var full=(IEnumerator)method.Invoke(land,new object[]{false});while(full.MoveNext())yield return full.Current;var expected=terrain.GetAlphamaps(0,0,n,n);
                terrain.SetAlphamaps(0,0,original);var bounded=(IEnumerator)method.Invoke(land,new object[]{true});while(bounded.MoveNext())yield return bounded.Current;var actual=terrain.GetAlphamaps(0,0,n,n);
                float maximum=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++)for(int l=0;l<actual.GetLength(2);l++)maximum=Mathf.Max(maximum,Mathf.Abs(actual[z,x,l]-expected[z,x,l]));
                if(maximum!=0)throw new Exception("Bounded/full bank weights differ: "+maximum);File.AppendAllText(report,"Bank scan regression: "+n+"x"+n+" texels; all layers identical to full scan (max error0)\n");
            }finally{terrain.SetAlphamaps(0,0,original);}
        }
        static IEnumerator Run(string output){
            var previous=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(previous&&previous.RegionName=="Loading performance trial"){previous.ReturnToMenu();yield return null;yield return null;}
            Directory.CreateDirectory(output);string report=Path.Combine(output,"Profile.txt");File.WriteAllText(report,"Loading CPU probes — "+DateTime.UtcNow.ToString("O")+"\n");
            var queryCheck=Task.Run(CheckDistances);while(!queryCheck.IsCompleted)yield return null;if(queryCheck.IsFaulted)throw queryCheck.Exception.GetBaseException();File.AppendAllText(report,queryCheck.Result);
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            var recipe=view?.CreationSettings??new RegionCreationSettings{seed=8472,streams=2,waterwayVersion=5,terrainShape=RegionTerrainShape.MountainBorder};
            // Clone before probing: neither live metadata nor stored region arrays are changed.
            recipe=JsonUtility.FromJson<RegionCreationSettings>(JsonUtility.ToJson(recipe));
            var size=view?view.Size:RegionSize.Small;
            if(!view&&output.Contains("medium-few")){size=RegionSize.Medium;recipe=new RegionCreationSettings{seed=482402525,streams=1,waterwayVersion=5,terrainShape=RegionTerrainShape.MountainBorder};}
            float[,] field=null;var timer=System.Diagnostics.Stopwatch.StartNew();
            using(var source=new MapMagicRegionGeography(recipe,size)){
                var task=Task.Run(source.Generate);while(!task.IsCompleted)yield return null;if(task.IsFaulted)throw task.Exception.GetBaseException();field=task.Result;
            }
            File.AppendAllText(report,"MapMagic="+timer.Elapsed.TotalSeconds.ToString("F3")+"s\n");timer.Restart();
            var composition=Task.Run(()=>RegionMapGenerator.Generate("Read-only loading probe",size,recipe,field));while(!composition.IsCompleted)yield return null;if(composition.IsFaulted)throw composition.Exception.GetBaseException();
            var snapshot=composition.Result;double compositionSeconds=timer.Elapsed.TotalSeconds;
            var paintCheck=Task.Run(()=>CheckPaint(snapshot.data));while(!paintCheck.IsCompleted)yield return null;if(paintCheck.IsFaulted)throw paintCheck.Exception.GetBaseException();File.AppendAllText(report,paintCheck.Result);
            File.AppendAllText(report,"Composition="+compositionSeconds.ToString("F3")+"s; size="+size+"; seed="+recipe.seed+"; streams="+recipe.streams+"\n");
            if(view){
                if(view.Data!=null&&view.Data.Biomes!=null&&view.ApprovedDistrict){var probe=ProbeDistrict(view.ApprovedDistrict,report);while(probe.MoveNext())yield return probe.Current;}
                File.AppendAllText(report,"Open region preserved. No scene build or save.\n");yield break;
            }
            var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();if(!menu||menu.IsOpeningRegion){File.AppendAllText(report,"Scene trial skipped: menu unavailable.\n");yield break;}
            string profile="";void Log(string message,string trace,LogType type){if(message.StartsWith("Loading profile:")||message.StartsWith("Region generation profile:")||message.StartsWith("Meadow generation stages:")||message.StartsWith("Forest load profile:"))profile+=message+"\n";}
            Application.logMessageReceived+=Log;
            try{
                timer.Restart();menu.CreatePrototypeRegion("Loading performance trial",size,recipe);while(menu&&menu.IsOpeningRegion)yield return null;
                view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(!view)throw new Exception("Region creation failed");
                File.AppendAllText(report,"Region total="+timer.Elapsed.TotalSeconds.ToString("F3")+"s\n"+profile);profile="";
                var body=view.Data.WaterBodies[0];var p=body.centerline[body.centerline.Length/2];int district=view.DistrictAtWorld(new Vector3((p.x-.5f)*view.MapSide,0,(p.y-.5f)*view.MapSide));
                timer.Restart();view.RequestDistrict(district);while(view&&view.DistrictLoading)yield return null;
                File.AppendAllText(report,"District total="+timer.Elapsed.TotalSeconds.ToString("F3")+"s; district="+district+"\n"+profile);
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"District.png"));
                if(output.Contains("validation")){
                    var bankCheck=CheckBanks(view.ApprovedDistrict,report);while(bankCheck.MoveNext())yield return bankCheck.Current;
                    var dressing=view.GetComponentInChildren<GeneratedStreamAppearance>();int patches=dressing?dressing.PatchCount:0,stones=dressing?dressing.StoneCount:0,trees=view.ApprovedDistrict.DisplayTerrain.terrainData.treeInstanceCount;
                    var camera=view.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>();var state=camera.CaptureRegionState();
                    foreach(var candidate in body.centerline){var w=(candidate-Vector2.one*.5f)*view.MapSide;var local=(w-view.ApprovedDistrict.Origin)/view.ApprovedDistrict.Side;if(local.x>.25f&&local.x<.75f&&local.y>.25f&&local.y<.75f){p=candidate;break;}}
                    state.focus=new Vector3((p.x-.5f)*view.MapSide,RegionalRiverNetwork.GroundWaterLevel(view.Data,p.x,p.y),(p.y-.5f)*view.MapSide);state.zoom=1;state.size=26;state.distance=200;camera.RestoreRegionState(state);
                    int frame=Time.frameCount;while(Time.frameCount<frame+12)yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"Stream-Close.png"));frame=Time.frameCount;while(Time.frameCount<frame+2)yield return null;
                    view.ShowOverview();frame=Time.frameCount;while(Time.frameCount<frame+2)yield return null;profile="";timer.Restart();view.RequestDistrict(district);while(view.DistrictLoading)yield return null;
                    dressing=view.GetComponentInChildren<GeneratedStreamAppearance>();
                    if(!view.ApprovedDistrict.UsedSavedTerrain||!dressing||dressing.PatchCount!=patches||dressing.StoneCount!=stones||dressing.ShrubCount!=0||view.ApprovedDistrict.DisplayTerrain.terrainData.treeInstanceCount!=trees)throw new Exception("Cached stream/terrain regression");
                    File.AppendAllText(report,"Cache re-entry="+timer.Elapsed.TotalSeconds.ToString("F3")+"s; identical stones/patches/tree counts; no duplicate shrubs\n"+profile);profile="";
                    view.ShowOverview();frame=Time.frameCount;while(Time.frameCount<frame+2)yield return null;
                    var peaks=MountainSummitSnow.Select(view.Data,MountainBorderVariation.CapCount(view));var summit=peaks[0];int mountain=view.DistrictAtWorld(new Vector3((summit.uv.x-.5f)*view.MapSide,0,(summit.uv.y-.5f)*view.MapSide));
                    timer.Restart();view.RequestDistrict(mountain);while(view.DistrictLoading)yield return null;
                    if(!view.GetComponentInChildren<MountainSummitSnow>())throw new Exception("Summit snow missing on mountain district");
                    File.AppendAllText(report,"Mountain district="+mountain+"; entry="+timer.Elapsed.TotalSeconds.ToString("F3")+"s; summit snow retained\n"+profile);frame=Time.frameCount;while(Time.frameCount<frame+12)yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"Mountain-District.png"));
                }
            }finally{Application.logMessageReceived-=Log;}
            File.AppendAllText(report,"PASS: disposable unsaved trial.\n");
        }
    }
}
#endif

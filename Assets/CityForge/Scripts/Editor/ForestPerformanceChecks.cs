#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using CityForge.World;
using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class ForestPerformanceChecks
    {
        const string Out="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/";
        static ForestPerformanceChecks(){EditorApplication.update+=Poll;}
        static void Poll(){const string file="Library/ForestPerformance.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(file))return;if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}File.Delete(file);EditorApplication.isPaused=false;CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Run()));}
        static IEnumerator Guard(IEnumerator routine){while(true){object next;try{if(!routine.MoveNext())yield break;next=routine.Current;}catch(Exception e){File.AppendAllText(Out+"Forest-Performance.txt","FAIL: "+e);Debug.LogException(e);yield break;}yield return next;}}
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static IEnumerator Run()
        {
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(!view||view.SavedRegionId!=HillsMountainTestRegion.Id||!view.ApprovedDistrict){if(view)view.ReturnToMenu();yield return null;yield return null;var source=SavedRegionStore.Load(Path.Combine(SavedRegionStore.DirectoryPath,HillsMountainTestRegion.Id+".cfregion"));PrototypeRegionView.Open(source.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:source);yield return null;yield return null;view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();}
            var land=view.ApprovedDistrict;var rig=view.GetComponentInChildren<CityForgeCameraController>();var camera=rig.GetComponent<Camera>();var pose=rig.CaptureRegionState();int previousMask=ForestTreeMix.EnabledSpeciesMask;bool plants=land.SmallPlantsEnabled,treesOn=land.TreesEnabled,pacing=land.FramePacing60;var season=view.Season;int target=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;
            var canonical=land.CanonicalTreeInstances;var frozen=(TreeInstance[])canonical.Clone();int[] masks=new[]{0,ForestTreeMix.AllSpeciesMask,1,2,4,8,16,32,64,128,0};
            var record=new List<string>();var native=Resources.Load<SeasonalTreeLibrary>("Seasons/SeasonalTrees").summer;
            record.Add("Fixed-camera editor comparison; frame pacing temporarily uncapped; same positions/count, native poplar fallback for disabled new species. Frame time includes Editor overhead; not a standalone/GPU benchmark. No player save writes.");
            foreach(var tree in ForestTreeMix.LoadPrefabs(RegionSeason.Summer)){
                record.Add("ASSET "+Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(tree)))+": "+string.Join("; ",tree.GetComponent<LODGroup>().GetLODs().Select((lod,i)=>$"LOD{i}: {lod.renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3)} tris, {lod.renderers.Length} renderers, {lod.renderers.Sum(r=>r.sharedMaterials.Length)} material slots, {lod.renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count()} unique materials")));
            }
            record.Add("CSV view,mask,visible_new_trees,frames,mean_ms,median_ms,p95_ms,mean_draws,mean_triangles,mean_batches,resolution");File.WriteAllLines(Out+"Forest-Performance.txt",record);
            try{
                view.SetSeason(RegionSeason.Summer);land.SetSmallPlantsEnabled(false);land.SetTreesEnabled(true);land.SetFramePacing60(false);QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                Vector3 focus=Vector3.zero;float best=float.MaxValue;foreach(var tree in canonical)if(tree.prototypeIndex>=ForestTreeMix.BroadleafStart){var p=land.DisplayTerrain.transform.position+Vector3.Scale(tree.position,land.DisplayTerrain.terrainData.size);float distance=(new Vector2(p.x,p.z)-(land.Origin+Vector2.one*land.Side*.45f)).sqrMagnitude;if(distance<best){best=distance;focus=p;}}
                for(int shot=0;shot<2;shot++){
                    var state=rig.CaptureRegionState();state.focus=shot==0?focus+Vector3.up*8:new Vector3(land.Origin.x+land.Side/2,70,land.Origin.y+land.Side/2);state.size=shot==0?75:land.Side*.65f;state.distance=shot==0?350:1600;state.zoom=1;state.lowerView=false;rig.RestoreRegionState(state);
                    foreach(int mask in masks){ForestTreeMix.SetEnabledSpecies(mask);land.RefreshForestSpecies(false);
                        Require(land.CanonicalTreeInstances.Length==frozen.Length,"Canonical population retained");for(int i=0;i<frozen.Length;i++)Require(land.CanonicalTreeInstances[i].prototypeIndex==frozen[i].prototypeIndex&&land.CanonicalTreeInstances[i].position==frozen[i].position&&land.CanonicalTreeInstances[i].widthScale==frozen[i].widthScale,"Canonical data unchanged by switches");
                        var displayed=land.DisplayTerrain.terrainData.treeInstances;Require(displayed.All(t=>t.prototypeIndex<ForestTreeMix.BroadleafStart||(mask&(1<<(t.prototypeIndex-ForestTreeMix.BroadleafStart)))!=0),"Disabled species absent from presentation");
                        int visible=displayed.Count(t=>t.prototypeIndex>=ForestTreeMix.BroadleafStart);int frame=Time.frameCount,warm=0;double start=EditorApplication.timeSinceStartup;
                        while(warm<20&&EditorApplication.timeSinceStartup-start<4){yield return null;if(Time.frameCount!=frame){frame=Time.frameCount;warm++;}}
                        var times=new List<double>();double draws=0,tris=0,batches=0;start=EditorApplication.timeSinceStartup;
                        while(times.Count<75&&EditorApplication.timeSinceStartup-start<5){yield return null;if(Time.frameCount==frame)continue;frame=Time.frameCount;times.Add(Time.unscaledDeltaTime*1000);draws+=UnityStats.drawCalls;tris+=UnityStats.triangles;batches+=UnityStats.batches;}
                        Require(times.Count>=10,"Game view rendered sufficient frames");var ordered=times.OrderBy(t=>t).ToArray();string label=mask==0?"native":mask==ForestTreeMix.AllSpeciesMask?"all":ForestSpeciesControls.Labels[(int)Math.Log(mask,2)];
                        record.Add($"{(shot==0?"close":"wide")},{label},{visible},{times.Count},{times.Average():F2},{ordered[ordered.Length/2]:F2},{ordered[(int)((ordered.Length-1)*.95)]:F2},{draws/times.Count:F0},{tris/times.Count:F0},{batches/times.Count:F0},{camera.pixelWidth}x{camera.pixelHeight}");File.WriteAllLines(Out+"Forest-Performance.txt",record);
                    }
                }
                ForestTreeMix.SetEnabledSpecies(0);land.RefreshForestSpecies(false);var snapshot=SavedRegionStore.Capture(view);Require(snapshot.meadow.trees.Count(t=>t.prototypeIndex>=ForestTreeMix.BroadleafStart)==canonical.Count(t=>t.prototypeIndex>=ForestTreeMix.BroadleafStart),"Saving while disabled retains new species for future re-enable");
                record.Add("PASS: all per-species display masks; canonical placement/count/scale/index unchanged; hidden species absent from rendering; saving retains canonical species. No player saves written.");File.WriteAllLines(Out+"Forest-Performance.txt",record);
            }finally{ForestTreeMix.SetEnabledSpecies(previousMask);view.SetSeason(season);land.SetSmallPlantsEnabled(plants);land.SetTreesEnabled(treesOn);land.SetFramePacing60(pacing);Application.targetFrameRate=target;QualitySettings.vSyncCount=vsync;land.RefreshForestSpecies(false);rig.RestoreRegionState(pose);}
        }
    }
}
#endif

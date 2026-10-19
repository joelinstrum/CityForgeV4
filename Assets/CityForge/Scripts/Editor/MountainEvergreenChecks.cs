#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CityForge.World;
using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class MountainEvergreenChecks
    {
        const string Out="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/";
        static MountainEvergreenChecks(){EditorApplication.update+=Poll;}
        static void Poll(){const string request="Library/MountainEvergreen.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}File.Delete(request);EditorApplication.isPaused=false;CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Check()));}
        static IEnumerator Guard(IEnumerator routine){while(true){object current;try{if(!routine.MoveNext())yield break;current=routine.Current;}catch(Exception e){File.WriteAllText(Out+"Mountain-Evergreen-Checks.txt","FAIL: "+e);Debug.LogException(e);yield break;}yield return current;}}
        static void Require(bool ok,string name){if(!ok)throw new Exception(name);}
        static void Capture(Camera camera,string name){
            var target=new RenderTexture(1920,1080,24);target.Create();var previous=RenderTexture.active;
            try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;
                var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);try{pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Out+name,pixels.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(pixels);}
            }finally{RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
        static IEnumerator Check(){
            var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;yield return null;
            var source=SavedRegionStore.Load(Path.Combine(SavedRegionStore.DirectoryPath,HillsMountainTestRegion.Id+".cfregion"));
            var originalCache=source.meadow;
            PrototypeRegionView.Open(source.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:source);yield return null;yield return null;
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();var land=view.ApprovedDistrict;land.SetTreesEnabled(true);land.SetSmallPlantsEnabled(false);
            int index=land.Baseline.treePrefabs.Length;var trees=land.CanonicalTreeInstances;int evergreen=0;var speciesCounts=new int[MountainEvergreenMix.SpeciesCount];var speciesFocus=new Vector3[speciesCounts.Length];var speciesNearest=new float[speciesCounts.Length];for(int k=0;k<speciesNearest.Length;k++)speciesNearest[k]=float.MaxValue;Vector3 focus=Vector3.zero;float nearest=float.MaxValue;
            foreach(var tree in trees)if(tree.prototypeIndex>=index&&tree.prototypeIndex<index+MountainEvergreenMix.SpeciesCount){evergreen++;int species=tree.prototypeIndex-index;speciesCounts[species]++;var p=land.DisplayTerrain.transform.position+Vector3.Scale(tree.position,land.DisplayTerrain.terrainData.size);float distance=(new Vector2(p.x-500,p.z-510)).sqrMagnitude;if(distance<nearest){nearest=distance;focus=p;}if(distance<speciesNearest[species]){speciesNearest[species]=distance;speciesFocus[species]=p;}}
            Require(evergreen>0&&evergreen==land.EvergreenTreeCount,"Evergreens present in Hills and Mountain Test");
            if(land.UsedSavedTerrain){Require(trees.Length==originalCache.trees.Length,"Tree count preserved");for(int i=0;i<trees.Length;i++)Require(trees[i].position==originalCache.trees[i].position&&trees[i].rotation==originalCache.trees[i].rotation,"Saved tree placements preserved");}
            var data=land.DisplayTerrain.terrainData;var before=data.treeInstances;int repeat=MountainEvergreenMix.Apply(data,index,view.Data.Seed);var after=data.treeInstances;Require(repeat==evergreen&&before.Length==after.Length,"Mix idempotent");for(int i=0;i<before.Length;i++)Require(before[i].prototypeIndex==after[i].prototypeIndex&&before[i].widthScale==after[i].widthScale,"Stable species and scale");
            for(int k=0;k<speciesCounts.Length;k++)Require(speciesCounts[k]>0,"All fir species present");
            var prefabs=MountainEvergreenMix.LoadPrefabs();
            foreach(RegionSeason season in Enum.GetValues(typeof(RegionSeason))){view.SetSeason(season);for(int k=0;k<prefabs.Length;k++)Require(data.treePrototypes[index+k].prefab==prefabs[k],"Each evergreen stays green in every season");Require(data.treePrototypes[0].prefab==Resources.Load<SeasonalTreeLibrary>("Seasons/SeasonalTrees").Prefabs(season)[0],"Approved seasonal trees preserved");}
            view.SetSeason(RegionSeason.Summer);land.SetSmallPlantsEnabled(false);Require(Array.Exists(data.treeInstances,t=>t.prototypeIndex>=index&&t.prototypeIndex<index+MountainEvergreenMix.SpeciesCount),"Small plants toggle retains evergreens");
            var visibleSpecies=new System.Collections.Generic.HashSet<int>();foreach(var tree in data.treeInstances)visibleSpecies.Add(tree.prototypeIndex);for(int k=0;k<speciesCounts.Length;k++)Require(visibleSpecies.Contains(index+k),"Every evergreen type survives small plants toggle");
            var snapshot=SavedRegionStore.Capture(view);var temp=Path.Combine(Application.temporaryCachePath,"evergreen-check-"+Guid.NewGuid().ToString("N")+".cfregion");SavedRegionSnapshot loaded;
            try{SavedRegionStore.Write(snapshot,temp);loaded=SavedRegionStore.Load(temp);}finally{if(File.Exists(temp))File.Delete(temp);}
            Require(Array.Exists(loaded.meadow.trees,t=>t.prototypeIndex>=index&&t.prototypeIndex<index+MountainEvergreenMix.SpeciesCount),"Evergreen prototype index persists");
            view.ReturnToMenu();yield return null;yield return null;PrototypeRegionView.Open(loaded.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:loaded);yield return null;yield return null;
            view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();land=view.ApprovedDistrict;land.SetTreesEnabled(true);land.SetSmallPlantsEnabled(true);Require(land.UsedSavedTerrain&&land.EvergreenTreeCount==evergreen,"Saved terrain cache and evergreen count restore");
            var rig=view.GetComponentInChildren<CityForgeCameraController>();var pose=rig.CaptureRegionState();var close=rig.CaptureRegionState();close.focus=focus+Vector3.up*8;close.size=48;close.distance=350;close.zoom=1;rig.RestoreRegionState(close);
            for(int i=0;i<12;i++)yield return null;Capture(rig.GetComponent<Camera>(),"Mountain-Evergreens-Close.png");yield return null;
            for(int k=0;k<speciesFocus.Length;k++){
                close.focus=speciesFocus[k]+Vector3.up*9;rig.RestoreRegionState(close);for(int wait=0;wait<5;wait++)yield return null;
                Capture(rig.GetComponent<Camera>(),"Mountain-Fir-"+MountainEvergreenMix.SpeciesNames[k]+".png");
            }
            rig.RestoreRegionState(pose);for(int i=0;i<8;i++)yield return null;Capture(rig.GetComponent<Camera>(),"Mountain-Evergreens-Wide.png");
            File.WriteAllText(Out+"Mountain-Evergreen-Checks.txt",$"PASS: {evergreen} evergreens / {trees.Length} total placements; species counts [Pilot, Balsam, Fraser, Cilician] = {string.Join(" / ",speciesCounts)}; existing saved placements retained; idempotent mix; all three seasonal libraries retain evergreen slot; small-plants toggle retains mature evergreens; disk round trip and saved terrain cache restore. Existing player save untouched.\n");
        }
    }
}
#endif

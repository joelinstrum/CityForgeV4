#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CityForge.World;
using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class ForestTreeChecks
    {
        const string Out="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/";
        static ForestTreeChecks(){EditorApplication.update+=Poll;}
        static void Poll(){const string request="Library/ForestTrees.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}File.Delete(request);EditorApplication.isPaused=false;CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Check()));}
        static IEnumerator Guard(IEnumerator routine){int previous=ForestTreeMix.EnabledSpeciesMask;ForestTreeMix.SetEnabledSpecies(ForestTreeMix.AllSpeciesMask);try{while(true){object next;try{if(!routine.MoveNext())yield break;next=routine.Current;}catch(Exception e){File.WriteAllText(Out+"Forest-Tree-Checks.txt","FAIL: "+e);Debug.LogException(e);yield break;}yield return next;}}finally{ForestTreeMix.SetEnabledSpecies(previous);foreach(var land in UnityEngine.Object.FindObjectsByType<MeadowDistrictLandscape>(FindObjectsInactive.Include,FindObjectsSortMode.None))land.RefreshForestSpecies(false);}}
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Capture(Camera camera,string file){var rt=new RenderTexture(1920,1080,24);rt.Create();var previous=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);try{texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();File.WriteAllBytes(Out+file,texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}}finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
        static int Triangles(GameObject tree){return tree.GetComponent<LODGroup>().GetLODs()[0].renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3);}
        static IEnumerator Check()
        {
            File.WriteAllText(Out+"Forest-Tree-Checks.txt","");
            var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;yield return null;
            var path=Path.Combine(SavedRegionStore.DirectoryPath,HillsMountainTestRegion.Id+".cfregion");var stamp=File.GetLastWriteTimeUtc(path);var size=new FileInfo(path).Length;var source=SavedRegionStore.Load(path);var original=source.meadow;
            PrototypeRegionView.Open(source.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:source);yield return null;yield return null;
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();var land=view.ApprovedDistrict;land.SetTreesEnabled(true);land.SetSmallPlantsEnabled(false);
            var terrain=land.DisplayTerrain.terrainData;var trees=land.CanonicalTreeInstances;var counts=new int[ForestTreeMix.SpeciesCount];var focus=new Vector3[counts.Length];var nearest=Enumerable.Repeat(float.MaxValue,counts.Length).ToArray();
            foreach(var tree in trees)if(tree.prototypeIndex>=ForestTreeMix.BroadleafStart){int k=tree.prototypeIndex-ForestTreeMix.BroadleafStart;counts[k]++;var point=land.DisplayTerrain.transform.position+Vector3.Scale(tree.position,terrain.size);float distance=(new Vector2(point.x,point.z)-(land.Origin+Vector2.one*land.Side*.5f)).sqrMagnitude;if(distance<nearest[k]){nearest[k]=distance;focus[k]=point;}}
            Require(counts.All(c=>c>0),"All eight broadleaf variants present");Require(land.BroadleafTreeCount==counts.Sum(),"Broadleaf count matches");Require(land.EvergreenTreeCount>0,"Fir mix preserved");
            if(land.UsedSavedTerrain){Require(trees.Length==original.trees.Length,"Placement count preserved");for(int i=0;i<trees.Length;i++)Require(trees[i].position==original.trees[i].position&&trees[i].rotation==original.trees[i].rotation,"Saved positions/rotations preserved");}
            var temporary=new TerrainData();try{temporary.treePrototypes=terrain.treePrototypes;temporary.SetTreeInstances(trees,false);ForestTreeMix.Apply(temporary,view.Data.Seed);var repeat=temporary.treeInstances;for(int i=0;i<trees.Length;i++)Require(trees[i].prototypeIndex==repeat[i].prototypeIndex&&trees[i].widthScale==repeat[i].widthScale&&trees[i].heightScale==repeat[i].heightScale,"Stable repeat mix");}finally{UnityEngine.Object.DestroyImmediate(temporary);}
            var rig=view.GetComponentInChildren<CityForgeCameraController>();var pose=rig.CaptureRegionState();var close=rig.CaptureRegionState();close.focus=focus[0]+Vector3.up*7;close.size=48;close.distance=350;close.zoom=1;
            foreach(RegionSeason season in Enum.GetValues(typeof(RegionSeason))){
                view.SetSeason(season);var prefabs=ForestTreeMix.LoadPrefabs(season);Require(terrain.treePrototypes.Length==ForestTreeMix.PrototypeCount,"Append-only prototype layout");
                for(int k=0;k<prefabs.Length;k++){Require(terrain.treePrototypes[ForestTreeMix.BroadleafStart+k].prefab==prefabs[k],"Correct seasonal species slot");var lods=prefabs[k].GetComponent<LODGroup>().GetLODs();Require(lods.Length==3,"Three mesh LODs");foreach(var lod in lods)foreach(var r in lod.renderers){Require(r.GetComponent<MeshFilter>().sharedMesh.triangles.Length>0,"Non-empty mesh LOD");foreach(var m in r.sharedMaterials)Require(m&&m.shader.name=="Universal Render Pipeline/Lit"&&m.GetTexture("_BaseMap"),"Textured URP material");}}
                Require(terrain.treePrototypes[0].prefab==Resources.Load<SeasonalTreeLibrary>("Seasons/SeasonalTrees").Prefabs(season)[0],"Approved seasonal library retained");rig.RestoreRegionState(close);for(int i=0;i<10;i++)yield return null;Capture(rig.GetComponent<Camera>(),"Forest-"+season+".png");
                var visible=terrain.treeInstances;Require(visible.Any(t=>t.prototypeIndex>=ForestTreeMix.BroadleafStart),"Small-plants toggle retains broadleaf trees");
            }
            var summer=ForestTreeMix.LoadPrefabs(RegionSeason.Summer);var fall=ForestTreeMix.LoadPrefabs(RegionSeason.Fall);var winter=ForestTreeMix.LoadPrefabs(RegionSeason.Winter);
            for(int k=0;k<2;k++){Require(summer[k]!=fall[k]&&fall[k]!=winter[k],"Maple seasonal prefabs distinct");Require(Triangles(winter[k])<Triangles(summer[k]),"Winter maple loses foliage");foreach(var r in winter[k].GetComponentsInChildren<Renderer>())Require(r.sharedMaterials.All(m=>!m.name.Contains("Leaf")),"Winter maple has no leaf submesh");}
            for(int k=2;k<=5;k++)Require(summer[k]==fall[k]&&fall[k]==winter[k],"Live oaks retain evergreen foliage");for(int k=6;k<8;k++)Require(summer[k]!=fall[k]&&fall[k]!=winter[k],"OakPilot true seasonal exports");
            view.SetSeason(RegionSeason.Summer);var snapshot=SavedRegionStore.Capture(view);var temp=Path.Combine(Application.temporaryCachePath,"forest-check-"+Guid.NewGuid().ToString("N")+".cfregion");SavedRegionSnapshot loaded;try{SavedRegionStore.Write(snapshot,temp);loaded=SavedRegionStore.Load(temp);}finally{if(File.Exists(temp))File.Delete(temp);}
            Require(loaded.meadow.trees.Count(t=>t.prototypeIndex>=ForestTreeMix.BroadleafStart)==counts.Sum(),"Broadleaf save indices round-trip");view.ReturnToMenu();yield return null;yield return null;PrototypeRegionView.Open(loaded.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:loaded);yield return null;yield return null;view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();land=view.ApprovedDistrict;Require(land.UsedSavedTerrain&&land.BroadleafTreeCount==counts.Sum(),"Cache restores new species");land.SetTreesEnabled(true);land.SetSmallPlantsEnabled(false);
            rig=view.GetComponentInChildren<CityForgeCameraController>();land.SetTreesEnabled(false);
            for(int k=0;k<counts.Length;k++){
                var specimen=UnityEngine.Object.Instantiate(summer[k],land.transform);specimen.transform.position=focus[0];specimen.GetComponent<LODGroup>().ForceLOD(0);close.focus=focus[0]+Vector3.up*8;close.size=28;rig.RestoreRegionState(close);
                for(int i=0;i<6;i++)yield return null;Capture(rig.GetComponent<Camera>(),"Forest-Species-"+ForestTreeMix.SpeciesNames[k]+".png");UnityEngine.Object.DestroyImmediate(specimen);
            }
            land.SetTreesEnabled(true);
            rig.RestoreRegionState(pose);for(int i=0;i<6;i++)yield return null;Capture(rig.GetComponent<Camera>(),"Forest-Mixed-Wide.png");
            Require(File.GetLastWriteTimeUtc(path)==stamp&&new FileInfo(path).Length==size,"Player save unchanged");
            File.WriteAllText(Out+"Forest-Tree-Checks.txt",$"PASS: eight species [{string.Join(" / ",counts)}], {trees.Length} placements; positions/rotations unchanged; existing firs retained; stable repeated substitution; Summer/Fall/Winter prototypes; bare winter maple; evergreen live oaks; three textured 3D LODs; small-plants visibility; temporary save round trip and cache restore; original player save unchanged.\n");
        }
    }
}
#endif

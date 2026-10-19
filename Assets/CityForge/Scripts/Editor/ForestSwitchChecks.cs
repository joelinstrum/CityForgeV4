#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CityForge.World;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class ForestSwitchChecks
    {
        const string Out="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/";
        static ForestSwitchChecks(){EditorApplication.update+=Poll;}
        static void Poll(){const string file="Library/ForestSwitches.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(file))return;if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}var request=File.ReadAllText(file).Trim();File.Delete(file);EditorApplication.isPaused=false;if(int.TryParse(request,out int mask)){ForestTreeMix.SetEnabledSpecies(mask,true);foreach(var land in UnityEngine.Object.FindObjectsByType<MeadowDistrictLandscape>(FindObjectsInactive.Include,FindObjectsSortMode.None))land.RefreshForestSpecies();var lands=UnityEngine.Object.FindObjectsByType<MeadowDistrictLandscape>(FindObjectsSortMode.None);bool valid=lands.All(land=>land.DisplayTerrain.terrainData.treeInstances.All(t=>t.prototypeIndex<19||ForestTreeMix.SpeciesEnabled(t.prototypeIndex-19)));File.WriteAllText(Out+"Forest-Selection.txt",$"{(valid?"PASS":"FAIL")}: enabled mask {ForestTreeMix.EnabledSpeciesMask}; preference {PlayerPrefs.GetInt("CityForge.Forest.SpeciesMask")}; {lands.Length} active landscapes; disabled species absent from presentation. Player saves untouched.\n");return;}CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Check()));}
        static IEnumerator Guard(IEnumerator routine){while(true){object next;try{if(!routine.MoveNext())yield break;next=routine.Current;}catch(Exception e){File.WriteAllText(Out+"Forest-Switch-Checks.txt","FAIL: "+e);Debug.LogException(e);yield break;}yield return next;}}
        static void Require(bool c,string m){if(!c)throw new Exception(m);}
        static void Click(Transform button){Canvas.ForceUpdateCanvases();var rect=button.GetComponent<RectTransform>();var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));var pointer=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Require(hits.Count>0&&hits[0].gameObject==button.gameObject,"Button raycast: "+button.name+" first="+(hits.Count>0?hits[0].gameObject.name:"none"));ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);}
        static IEnumerator Check(){
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(!view||!view.ApprovedDistrict){var source=SavedRegionStore.Load(Path.Combine(SavedRegionStore.DirectoryPath,HillsMountainTestRegion.Id+".cfregion"));PrototypeRegionView.Open(source.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:source);yield return null;yield return null;view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();}
            Require(EventSystem.current,"Active UI event system");var land=view.ApprovedDistrict;var original=(TreeInstance[])land.CanonicalTreeInstances.Clone();int firs=original.Count(t=>t.prototypeIndex>=15&&t.prototypeIndex<19);var menu=view.transform.Find("RegionHud/ForestSpecies");Require(menu,"Species controls exist");
            ForestTreeMix.SetEnabledSpecies(0);land.RefreshForestSpecies(false);yield return null;Click(menu.Find("Open"));yield return null;Require(menu.Find("Panel").gameObject.activeSelf,"Open species panel");
            Click(menu.Find("Panel/Species0"));yield return null;Require(ForestTreeMix.EnabledSpeciesMask==1,"First species switch");Require(land.DisplayTerrain.terrainData.treeInstances.Any(t=>t.prototypeIndex==19),"Maple A displayed");
            Click(menu.Find("Panel/Species1"));yield return null;Require(ForestTreeMix.EnabledSpeciesMask==3,"Cumulative species switches");
            foreach(RegionSeason season in Enum.GetValues(typeof(RegionSeason))){view.SetSeason(season);yield return null;Require(land.DisplayTerrain.terrainData.treeInstances.All(t=>t.prototypeIndex<19||t.prototypeIndex<=20),"Disabled species absent across seasons");}
            view.SetSeason(RegionSeason.Summer);Click(menu.Find("Panel/None"));yield return null;Require(ForestTreeMix.EnabledSpeciesMask==0,"None switch");Require(PlayerPrefs.GetInt("CityForge.Forest.SpeciesMask",-1)==0,"Preference persists independently from region save");
            var shown=land.DisplayTerrain.terrainData.treeInstances;Require(shown.All(t=>t.prototypeIndex<19),"Only native trees and firs displayed");Require(shown.Count(t=>t.prototypeIndex>=15&&t.prototypeIndex<19)==firs,"Fir placements retained");
            var canonical=land.CanonicalTreeInstances;for(int i=0;i<original.Length;i++)Require(original[i].prototypeIndex==canonical[i].prototypeIndex&&original[i].position==canonical[i].position&&original[i].widthScale==canonical[i].widthScale&&original[i].rotation==canonical[i].rotation,"Canonical tree unchanged");
            var snapshot=SavedRegionStore.Capture(view);var temp=Path.Combine(Application.temporaryCachePath,"forest-switch-check-"+Guid.NewGuid().ToString("N")+".cfregion");try{SavedRegionStore.Write(snapshot,temp);var loaded=SavedRegionStore.Load(temp);Require(loaded.meadow.trees.Count(t=>t.prototypeIndex>=19)==canonical.Count(t=>t.prototypeIndex>=19),"Disk save while disabled retains canonical species");}finally{if(File.Exists(temp))File.Delete(temp);}
            for(int i=0;i<5;i++)yield return null;ScreenCapture.CaptureScreenshot(Out+"Forest-Species-Switches.png");int captureFrame=Time.frameCount;while(Time.frameCount<captureFrame+4)yield return null;Require(menu.Find("Panel").gameObject.activeInHierarchy,"Panel remains open after capture");Click(menu.Find("Panel/Close"));yield return null;Require(!menu.Find("Panel").gameObject.activeSelf,"Close species panel");
            File.WriteAllText(Out+"Forest-Switch-Checks.txt","PASS: UI raycast targets and pointer-click handlers; panel open/close; cumulative Maple A/B switches; Summer/Fall/Winter; None restores native poplar presentation; firs unchanged; global preference persists; canonical indices/positions/scales/rotations unchanged; temporary save round trip while disabled retains canonical species. Final state: new species OFF. Player saves untouched.\n");
        }
    }
}
#endif

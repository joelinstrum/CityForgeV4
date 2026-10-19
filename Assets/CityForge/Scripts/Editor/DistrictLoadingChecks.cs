#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CityForge.World;
using CityForge.UI;
using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
 [InitializeOnLoad] public static class DistrictLoadingChecks
 {
 const string Out="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/";
 static DistrictLoadingChecks(){EditorApplication.update+=Poll;}
 static void Poll(){const string f="Library/DistrictLoading.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(f))return;if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}File.Delete(f);CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Check()));}
 static IEnumerator Guard(IEnumerator routine){while(true){object next;try{if(!routine.MoveNext())yield break;next=routine.Current;}catch(Exception e){File.WriteAllText(Out+"District-Loading-Checks.txt","FAIL: "+e);Debug.LogException(e);yield break;}yield return next;}}
 static void Require(bool value,string name){if(!value)throw new Exception(name);}
 static IEnumerator Throwing(){yield return null;throw new InvalidOperationException("Disposable loading failure test");}
 static IEnumerator Check(){
 var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(!view){var saved=RegionMapGenerator.Generate("District Loading Trial",RegionSize.Medium,new RegionCreationSettings{topography=3,rivers=1});RegionBuildSteps.Complete(PrototypeRegionView.OpenStages(saved.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:saved));view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();}
 view.ShowOverview();yield return null;var rig=view.GetComponentInChildren<CityForgeCameraController>();
 var camera=rig.GetComponent<Camera>();var mesh=view.GetComponentInChildren<RegionReliefMap>().GetComponent<MeshFilter>().sharedMesh;foreach(var vertex in mesh.vertices){var screen=camera.WorldToViewportPoint(vertex);Require(screen.x>.01f&&screen.x<.99f&&screen.y>.01f&&screen.y<.99f&&screen.z>camera.nearClipPlane&&screen.z<camera.farClipPlane,"Entire regional relief fits camera including depth planes");}
 int firstFrame=Time.frameCount;view.RequestDistrict(9);var overlay=UnityEngine.Object.FindFirstObjectByType<RegionLoadingOverlay>();Require(overlay&&overlay.IsLoading&&view.DistrictLoading&&!view.enabled&&!rig.enabled,"Immediate overlay and input lock");view.RequestDistrict(10);
 float last=0;var stages=new HashSet<string>();bool captured=false;int frames=0,previous=Time.frameCount;double deadline=EditorApplication.timeSinceStartup+90;
 while(view.DistrictLoading&&EditorApplication.timeSinceStartup<deadline){yield return null;if(!view.DistrictLoading)break;Require(overlay,"Overlay retained while building");Require(overlay.Progress>=last,"Monotonic progress");last=overlay.Progress;stages.Add(overlay.Stage??"");if(Time.frameCount!=previous){frames++;previous=Time.frameCount;if(!captured&&Time.frameCount>=firstFrame+2){ScreenCapture.CaptureScreenshot(Out+"District-Loading-Overlay.png");captured=true;}}}
 Require(!view.DistrictLoading&&view.SelectedDistrict==9&&view.ApprovedDistrict.DistrictIndex==9,"Requested district completes; duplicate ignored");Require(view.enabled&&rig.enabled,"Input restored");Require(rig.ZoomLevel==5,"District opens at farthest zoom level5");Require(frames>=3&&stages.Count>=3&&captured,"Multiple rendered frames and named progress stages");yield return null;yield return null;Require(!UnityEngine.Object.FindFirstObjectByType<RegionLoadingOverlay>(),"Overlay removed");
 view.ShowOverview();yield return null;Require(view.SelectedDistrict==-1&&!view.ApprovedDistrict,"Return to relief overview");
 bool complete=false,failed=false;overlay=RegionLoadingOverlay.Show("Disposable failure",null,true);overlay.BeginDistrict(Throwing(),()=>complete=true,e=>failed=e is InvalidOperationException);deadline=EditorApplication.timeSinceStartup+5;while(!failed&&EditorApplication.timeSinceStartup<deadline)yield return null;Require(complete&&failed,"Shared runner catches failure and releases completion lock");
 File.WriteAllText(Out+"District-Loading-Checks.txt",$"PASS: entire regional relief fits viewport/depth planes; district opens at zoom5; overlay shown before work; district9 staged load; duplicate district10 ignored; camera/view input locked and restored; {frames} rendered frames and {stages.Count} progress stages; monotonic progress; overlay removed; return to relief overview; disposable failure callback/cleanup. Player saves untouched.\n");
 }
 }
}
#endif

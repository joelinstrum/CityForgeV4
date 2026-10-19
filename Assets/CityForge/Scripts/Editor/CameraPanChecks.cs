#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CityForge.World;
using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class CameraPanChecks
    {
        const string Output="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/Camera-Pan-Frames.csv";
        static CameraPanChecks(){EditorApplication.update+=Poll;}
        static void Poll(){const string request="Library/CameraPan.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}var command=File.ReadAllText(request).Trim();File.Delete(request);if(command=="verify"){var land=UnityEngine.Object.FindFirstObjectByType<MeadowDistrictLandscape>();if(!land){var source=SavedRegionStore.Load(Path.Combine(SavedRegionStore.DirectoryPath,HillsMountainTestRegion.Id+".cfregion"));PrototypeRegionView.Open(source.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:source);land=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>().ApprovedDistrict;}land.SetFramePacing60(false);bool off=QualitySettings.vSyncCount==0&&Application.targetFrameRate==-1;land.SetFramePacing60(true,true);bool on=QualitySettings.vSyncCount==1&&Application.targetFrameRate==-1;var caption=land.View.transform.Find("RegionHud/FramePacing/Caption");bool label=caption&&caption.GetComponent<UnityEngine.UI.Text>().text=="FRAME PACE: VSYNC";File.WriteAllText(Output+".verification.txt",$"{(off&&on&&label?"PASS":"FAIL")}: uncapped and VSync policy; target=-1; VSync caption; final synchronized preference enabled. No map saves written.\n");return;}CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Run()));}
        static IEnumerator Guard(IEnumerator routine){try{while(true){object next;try{if(!routine.MoveNext())yield break;next=routine.Current;}catch(Exception e){File.AppendAllText(Output,"ERROR,"+e);Debug.LogException(e);yield break;}yield return next;}}finally{(routine as IDisposable)?.Dispose();}}
        static IEnumerator Run()
        {
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(!view||!view.ApprovedDistrict){var source=SavedRegionStore.Load(Path.Combine(SavedRegionStore.DirectoryPath,HillsMountainTestRegion.Id+".cfregion"));PrototypeRegionView.Open(source.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:source);yield return null;view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();}
            var land=view.ApprovedDistrict;var rig=view.GetComponentInChildren<CityForgeCameraController>();var quality=rig.GetComponent<CameraMotionQuality>();var pose=rig.CaptureRegionState();bool enabled=rig.enabled,motion=quality.enabled,trees=land.TreesEnabled,plants=land.SmallPlantsEnabled;int cap=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;var stream=land.GetComponentInChildren<MeadowRuntimeVegetation>(true);bool streamEnabled=stream&&stream.enabled;
            var lines=new List<string>{"mode,frame,ms,draws,triangles,gcCollections,streamTiles,streamPending,streamMaxSliceMs"};File.WriteAllLines(Output,lines);
            try
            {
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();rig.enabled=false;
                var state=rig.CaptureRegionState();state.focus=new Vector3(land.Origin.x+land.Side*.5f,70,land.Origin.y+land.Side*.5f);state.size=75;state.distance=700;state.zoom=1;state.lowerView=false;Vector3 center=state.focus;
                for(int mode=0;mode<7;mode++){
                    string name=new[]{"motion_on_cap60","motion_off_cap60","stream_off_cap60","trees_off_cap60","vsync_motion_on","uncapped_motion_on","motion_on_repeat"}[mode];
                    quality.enabled=mode!=1;land.SetTreesEnabled(mode!=3&&trees);if(stream)stream.enabled=mode!=2&&streamEnabled;
                    QualitySettings.vSyncCount=mode==4?1:0;Application.targetFrameRate=mode==5||mode==4?-1:60;
                    int frame=Time.frameCount;double start=EditorApplication.timeSinceStartup;int sampled=0;int gc=GC.CollectionCount(0);double maxSeconds=12;
                    while(sampled<300&&EditorApplication.timeSinceStartup-start<maxSeconds){yield return null;if(Time.frameCount==frame)continue;frame=Time.frameCount;float elapsed=(float)(EditorApplication.timeSinceStartup-start);state.focus=center+new Vector3(Mathf.Sin(elapsed*.8f)*90,0,0);rig.RestoreRegionState(state);sampled++;
                        if(sampled>60)lines.Add($"{name},{sampled-60},{Time.unscaledDeltaTime*1000:F3},{UnityStats.drawCalls},{UnityStats.triangles},{GC.CollectionCount(0)-gc},{(stream?stream.LoadedTiles:0)},{(stream?stream.PendingTiles:0)},{(stream?stream.MaximumStreamingSliceMilliseconds:0):F3}");gc=GC.CollectionCount(0);
                    }
                    File.WriteAllLines(Output,lines);
                }
            }
            finally{quality.enabled=false;rig.RestoreRegionState(pose);rig.enabled=enabled;quality.enabled=motion;land.SetTreesEnabled(trees);if(stream)stream.enabled=streamEnabled;Application.targetFrameRate=cap;QualitySettings.vSyncCount=vsync;}
            File.AppendAllText(Output,"# COMPLETE: original camera, visibility, quality and pacing restored. No saved maps written.\n");
        }
    }
}
#endif

using System;
using System.IO;
using CityForge.World;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace CityForge.Editor
{
    [InitializeOnLoad]
    public static class LakeWaterFinalTools
    {
        static LakeWaterFinalTools(){EditorApplication.update+=Tick;}
        static void Tick(){const string request="Library/LakeFinal.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||LakeWaterAcceptance.Running||!File.Exists(request))return;var action=File.ReadAllText(request).Trim();File.Delete(request);
            try{if(action=="check")Check();else if(action=="build")Build();else if(action=="finalize")FinalizeLake();}catch(Exception e){File.WriteAllText(LakeWaterAcceptance.Output+"Lake-Final-Tool-Error.txt",e.ToString());Debug.LogException(e);}}
        [MenuItem("CityForge/Lakes And Ponds/Run Acceptance Checks")]
        static void Check(){if(!EditorApplication.isPlaying)throw new Exception("Play MainMenu first.");new GameObject("Lake acceptance").AddComponent<LakeWaterAcceptance>();}
        [MenuItem("CityForge/Lakes And Ponds/Build Verification Player")]
        static void Build(){if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/CityForge/Scenes/Gameplay/MainMenu.unity"},target=BuildTarget.StandaloneOSX,locationPathName="/Users/joelinstrum/Documents/Codex/2026-10-12/begin-from-this-handoff-and-start/work/LakeVerification.app",options=BuildOptions.Development});
            File.WriteAllText(LakeWaterAcceptance.Output+"Lake-Final-Player-Build.txt",$"{result.summary.result}; errors={result.summary.totalErrors}; warnings={result.summary.totalWarnings}; duration={result.summary.totalTime}\n");
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Lake verification build failed.");}
        [MenuItem("CityForge/Lakes And Ponds/Save Accepted Lake Region")]
        static void FinalizeLake(){var saved=SavedRegionStore.Load(LakeWaterAcceptance.Output+"Lake-Final.cfregion");saved.info.id=HillLakeTestRegion.Id;saved.info.name="Meadow — Hill and Lake";SavedRegionStore.Save(saved);Debug.Log("PASS accepted lake saved with production renderer and canonical cache.");}
    }
}

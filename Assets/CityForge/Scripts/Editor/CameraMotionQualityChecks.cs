#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class CameraMotionQualityChecks
    {
        const string Output="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/Camera-Motion-Quality.txt";
        static CameraMotionQualityChecks(){EditorApplication.update+=Poll;}
        static void Poll(){const string request="Library/CameraMotionQuality.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}File.Delete(request);CityForge.Editor.EditorCoroutineRunner.Begin(Check());}
        static IEnumerator Frame(){int frame=Time.frameCount+2;while(Time.frameCount<frame)yield return null;}
        static IEnumerator Check()
        {
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float scale=pipeline.renderScale,lod=QualitySettings.lodBias;
            var go=new GameObject("Temporary motion-quality check");var camera=go.AddComponent<Camera>();camera.cullingMask=0;camera.depth=-100;
            var quality=go.AddComponent<CameraMotionQuality>();string result="";
            try
            {
                var frame=Frame();while(frame.MoveNext())yield return frame.Current;
                go.transform.position+=Vector3.right;
                frame=Frame();while(frame.MoveNext())yield return frame.Current;
                if(!quality.ReducedQuality||Mathf.Abs(pipeline.renderScale-scale*.75f)>.001f||Mathf.Abs(QualitySettings.lodBias-lod*.5f)>.001f)result="FAIL: movement quality";
                float until=Time.unscaledTime+.3f;while(Time.unscaledTime<until)yield return null;
                if(quality.ReducedQuality||Mathf.Abs(pipeline.renderScale-scale)>.001f||Mathf.Abs(QualitySettings.lodBias-lod)>.001f)result+="FAIL: idle restoration";
                camera.orthographicSize+=1;
                frame=Frame();while(frame.MoveNext())yield return frame.Current;
                if(!quality.ReducedQuality)result+="FAIL: zoom detection";
                quality.enabled=false;
                if(Mathf.Abs(pipeline.renderScale-scale)>.001f||Mathf.Abs(QualitySettings.lodBias-lod)>.001f)result+="FAIL: disable restoration";
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
            File.WriteAllText(Output,string.IsNullOrEmpty(result)?"PASS: translation and zoom activate reduced quality; render scale 75%, LOD bias 50%; idle and disable restore exact previous settings. This is a lifecycle check, not a performance benchmark.\n":result);
        }
    }
}
#endif

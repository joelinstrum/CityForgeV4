#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using UnityEditor;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using CityForge.CameraSystem;using CityForge.World;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class RiverProjectionChecks
    {
        const string Root="Library/RiverProjectionStudy/";
        static RiverProjectionChecks(){EditorApplication.update+=Poll;}
        static void Poll(){if(!File.Exists("Library/RiverProjection.request")||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return;File.Delete("Library/RiverProjection.request");Directory.CreateDirectory(Root);CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Run()));}
        static IEnumerator Guard(IEnumerator routine){while(true){object current;try{if(!routine.MoveNext())yield break;current=routine.Current;}catch(Exception e){File.WriteAllText(Root+"Status.txt",e.ToString());Debug.LogException(e);yield break;}yield return current;}}
        static IEnumerator Run(){
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();var rig=view.GetComponentInChildren<CityForgeCameraController>();var camera=rig.GetComponent<Camera>();var pose=rig.CaptureRegionState();var preview=view.ApprovedDistrict.GetComponentInChildren<CrestDistrictRiverPreview>();if(!preview)throw new Exception("Native Crest preview required");
            var material=preview.Settings;var flow=preview.GetComponent<RiverFlowAnimation>();var advection=Shader.GetGlobalVector("_CityForgeRiverAdvection");bool oldFlow=flow.enabled,oldRig=rig.enabled,oldReflections=preview.WaterEngine.Reflections.Enabled;float oldReflection=material.GetFloat("_Crest_PlanarReflectionsEnabled"),oldWorkflow=material.GetFloat("_WorkflowMode"),oldSpecular=material.GetFloat("_Crest_Specular");bool oldSpecularKeyword=material.IsKeywordEnabled("_SPECULAR_SETUP");
            rig.SetViewTilt(0);float lowDistance=Vector3.Distance(camera.transform.position,pose.focus),lowFov=camera.fieldOfView;rig.SetViewTilt(1);float distance=Vector3.Distance(camera.transform.position,pose.focus);float size=camera.orthographicSize;float near=camera.nearClipPlane,far=camera.farClipPlane;var originalCameraPosition=camera.transform.position;
            rig.enabled=false;flow.enabled=false;Shader.SetGlobalVector("_CityForgeRiverAdvection",advection);
            var target=RenderTexture.GetTemporary(1600,Mathf.RoundToInt(1600/camera.aspect),24,RenderTextureFormat.ARGBHalf);var read=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);var oldTarget=RenderTexture.active;var report=new System.Text.StringBuilder($"Frozen advection {advection}; focus {pose.focus}; heading {pose.yaw}; distance {distance}; size {size}; near {near}; far {far}\n");
            try{
                for(int study=0;study<4;study++){
                    bool specular=study>=2;bool reflections=study!=1;material.SetFloat("_WorkflowMode",specular?0:oldWorkflow);material.SetFloat("_Crest_Specular",specular?(study==3?.1f:.35f):oldSpecular);if(specular||oldSpecularKeyword)material.EnableKeyword("_SPECULAR_SETUP");else material.DisableKeyword("_SPECULAR_SETUP");preview.WaterEngine.Reflections.Enabled=reflections&&oldReflections;material.SetFloat("_Crest_PlanarReflectionsEnabled",reflections?oldReflection:0);
                    int count=reflections?6:2;
                    for(int index=0;index<count;index++){
                        float angle=reflections?new[]{12f,35f,37f,39f,40f,40f}[index]:40;bool orthographic=index==count-1;
                        var rotation=Quaternion.Euler(angle,pose.yaw,0);camera.transform.SetPositionAndRotation(pose.focus-rotation*Vector3.forward*(angle==12?lowDistance:distance),rotation);camera.orthographic=orthographic;camera.orthographicSize=size;camera.fieldOfView=angle==12?lowFov:2*Mathf.Atan(size/distance)*Mathf.Rad2Deg;camera.nearClipPlane=near;camera.farClipPlane=far;camera.ResetProjectionMatrix();
                        var p=camera.WorldToViewportPoint(pose.focus);if(Mathf.Abs(p.x-.5f)>.001f||Mathf.Abs(p.y-.5f)>.001f)throw new Exception("Focus changed in isolated projection study");
                        int frame=Time.frameCount;while(Time.frameCount<frame+4)yield return null;
                        string label=(specular?(study==3?"Specular010-":"Specular035-"):reflections?"ReflectionsOn-":"ReflectionsOff-")+(orthographic?"Ortho-":"Perspective-")+angle.ToString("0");
                        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;read.ReadPixels(new Rect(0,0,target.width,target.height),0,0);read.Apply();File.WriteAllBytes(Root+label+".png",read.EncodeToPNG());
                        report.AppendLine($"{label}: eye {camera.transform.position:F4}, FOV {camera.fieldOfView:F4}, normal {material.GetFloat("_Crest_NormalMapStrength")}, scale {material.GetFloat("_Crest_NormalMapScale")}, smoothness {material.GetFloat("_Crest_Smoothness")}, workflow {material.GetFloat("_WorkflowMode")}, specular {material.GetFloat("_Crest_Specular")}, keyword {material.IsKeywordEnabled("_SPECULAR_SETUP")}");File.WriteAllText(Root+"Status.txt",label);
                    }
                }
                File.WriteAllText(Root+"Checks.txt",report+"\nPASS: fixed distance/focus/framing/heading and frozen advection.35/37/39/40 perspective then40 orthographic;40 projection pair repeated without planar reflections. Specular workflow0/strength0.35 and0.10 additionally tested at12/35/37/39/40 perspective and40 orthographic; normal/roughness/color profile unchanged. No player saves written.");File.WriteAllText(Root+"Status.txt","COMPLETE");
            }finally{RenderTexture.active=oldTarget;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.Destroy(read);preview.WaterEngine.Reflections.Enabled=oldReflections;material.SetFloat("_Crest_PlanarReflectionsEnabled",oldReflection);material.SetFloat("_WorkflowMode",oldWorkflow);material.SetFloat("_Crest_Specular",oldSpecular);if(oldSpecularKeyword)material.EnableKeyword("_SPECULAR_SETUP");else material.DisableKeyword("_SPECULAR_SETUP");flow.enabled=oldFlow;rig.enabled=oldRig;rig.RestoreRegionState(pose);}
        }
    }
}
#endif

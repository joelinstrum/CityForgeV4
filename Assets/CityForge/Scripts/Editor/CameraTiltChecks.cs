#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using UnityEditor;using UnityEngine;using UnityEngine.UI;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using CityForge.CameraSystem;using CityForge.World;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class CameraTiltChecks
    {
        const string Root="Library/CameraTiltStudy/";
        static CameraTiltChecks(){EditorApplication.update+=Poll;}
        static void Poll(){if(!File.Exists("Library/CameraTilt.request")||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return;File.Delete("Library/CameraTilt.request");Directory.CreateDirectory(Root);CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Run()));}
        static IEnumerator Guard(IEnumerator routine){while(true){object current;try{if(!routine.MoveNext())yield break;current=routine.Current;}catch(Exception e){File.WriteAllText(Root+"Status.txt",e.ToString());Debug.LogException(e);yield break;}yield return current;}}
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static IEnumerator Run(){
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();var rig=view.GetComponentInChildren<CityForgeCameraController>();var camera=rig.GetComponent<Camera>();var control=view.GetComponentInChildren<CameraTiltControls>(true);var pose=rig.CaptureRegionState();var slider=control.Slider;
            var steep=JsonUtility.FromJson<RegionCameraState>(JsonUtility.ToJson(pose));steep.pitch=85;steep.hasViewTilt=true;steep.viewTilt=1;rig.RestoreRegionState(steep);Require(Mathf.Abs(rig.ViewPitch-CityForgeCameraController.IsometricPitch)<.0001f,"Legacy steep camera clamps to approved endpoint");Require(Mathf.Abs(camera.transform.eulerAngles.x-CityForgeCameraController.IsometricPitch)<.001f,"Actual camera uses capped pitch");rig.RestoreRegionState(pose);
            var legacy=JsonUtility.FromJson<RegionCameraState>(JsonUtility.ToJson(pose));legacy.hasViewTilt=false;legacy.lowerView=true;rig.RestoreRegionState(legacy);Require(rig.ViewTilt==0,"Legacy low camera");legacy.lowerView=false;rig.RestoreRegionState(legacy);Require(rig.ViewTilt==1,"Legacy isometric camera");
            rig.SetViewTilt(.4f);var restored=JsonUtility.FromJson<RegionCameraState>(JsonUtility.ToJson(rig.CaptureRegionState()));rig.SetViewTilt(1);rig.RestoreRegionState(restored);Require(Mathf.Abs(rig.ViewTilt-.4f)<.0001f,"Intermediate camera JSON round trip");rig.RestoreRegionState(pose);
            var preview=view.ApprovedDistrict.GetComponentInChildren<CrestDistrictRiverPreview>();var material=preview?preview.Settings:null;string[] keys={"_Crest_NormalMapScale","_Crest_NormalMapStrength","_Crest_Smoothness","_Crest_PlanarReflectionsIntensity"};var values=new float[keys.Length];if(material)for(int i=0;i<keys.Length;i++)values[i]=material.GetFloat(keys[i]);
            var target=RenderTexture.GetTemporary(1600,Mathf.RoundToInt(1600/camera.aspect),24,RenderTextureFormat.ARGBHalf);var read=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);var previous=RenderTexture.active;var canvas=control.GetComponentInParent<Canvas>().rootCanvas;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;
            try{
                for(int frame=0;frame<=60;frame++){
                    slider.value=frame/60f;int waitFrame=Time.frameCount;while(Time.frameCount<=waitFrame)yield return null;
                    var now=rig.CaptureRegionState();Require((now.focus-pose.focus).sqrMagnitude<.0001f&&now.yaw==pose.yaw&&now.zoom==pose.zoom&&now.size==pose.size,"Focus/heading/zoom remain fixed while dragging");
                    Require(Mathf.Abs(rig.ViewTilt-frame/60f)<.0001f,"Slider reaches requested tilt");var center=camera.WorldToViewportPoint(pose.focus);Require(Mathf.Abs(center.x-.5f)<.002f&&Mathf.Abs(center.y-.5f)<.002f,"Focus stays centered through projection blend");
                    for(int i=0;i<16;i++)Require(float.IsFinite(camera.projectionMatrix[i]),"Finite projection matrix");
                    if(material)for(int i=0;i<keys.Length;i++)Require(material.GetFloat(keys[i])==values[i],"Tilt does not alter water material");
                    if(frame==0)Require(!camera.orthographic,"Low endpoint is perspective");if(frame==60){Require(camera.orthographic,"Top endpoint is orthographic");Require(Mathf.Abs(rig.ViewPitch-CityForgeCameraController.IsometricPitch)<.001f,"35.3 degree upper endpoint");}Require(rig.ViewPitch<=CityForgeCameraController.IsometricPitch+.001f,"Tilt never exceeds cap");
                    if(frame%10==0){
                        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+1;Canvas.ForceUpdateCanvases();
                        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;read.ReadPixels(new Rect(0,0,target.width,target.height),0,0);read.Apply();File.WriteAllBytes(Root+frame.ToString("D3")+".png",read.EncodeToPNG());
                        canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;
                    }
                    File.WriteAllText(Root+"Status.txt","Sweep frame "+frame);
                }
                Require(slider.handleRect.GetComponent<Image>().sprite!=null,"Visible round slider handle");
                rig.ToggleViewAngle();Require(rig.ViewTilt==0,"I reaches low endpoint");rig.ToggleViewAngle();Require(rig.ViewTilt==1,"I reaches isometric endpoint");
                File.WriteAllText(Root+"Checks.txt","PASS:35.26439 degree cap including legacy85 degree restore;61 slider positions; fixed focus/heading/zoom; focus centered through continuous tilt/FOV transition; finite matrices; original perspective/orthographic endpoints; I synchronization; legacy camera restore and intermediate JSON round trip; water material unchanged. Captures include tilt control. No player saves written.");File.WriteAllText(Root+"Status.txt","COMPLETE");
            }finally{canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.Destroy(read);rig.SetViewTilt(0);}
        }
    }
}
#endif

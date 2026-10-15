#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] static class CameraAngleChecks
    {
        static CameraAngleChecks(){EditorApplication.update+=Poll;}
        static void Require(bool condition,string message){if(!condition)throw new System.Exception(message);}
        static void Poll()
        {
            const string request="Library/CameraAngle.request";
            if(!File.Exists(request)||EditorApplication.isCompiling)return;
            var action=File.ReadAllText(request).Trim();File.Delete(request);
            try
            {
                EditorApplication.isPaused=false;
                var controller=Object.FindFirstObjectByType<CityForgeCameraController>();
                Require(controller!=null,"No active game camera");
                var camera=controller.GetComponent<Camera>();
                var initial=controller.CaptureRegionState();
                if(action=="check")
                {
                    foreach(int zoom in new[]{0,1,2,3,4,5})
                    {
                        controller.SetZoomLevel(zoom);var before=controller.CaptureRegionState();
                        controller.ToggleViewAngle();
                        Require(controller.LowerView&&!camera.orthographic,"Lower perspective failed");
                        Require(Mathf.Abs(camera.transform.eulerAngles.x-12f)<.01f,"Lower pitch failed");
                        var lower=controller.CaptureRegionState();
                        var restored=JsonUtility.FromJson<CityForge.World.RegionCameraState>(JsonUtility.ToJson(lower));
                        controller.RestoreRegionState(restored);
                        Require(controller.LowerView&&!camera.orthographic,"Save round trip failed");
                        Require(lower.focus==before.focus&&lower.yaw==before.yaw&&lower.zoom==before.zoom,"Toggle changed navigation");
                        controller.ToggleViewAngle();
                        Require(camera.orthographic&&JsonUtility.ToJson(controller.CaptureRegionState())==JsonUtility.ToJson(before),"Isometric pose did not restore");
                    }
                    controller.ResetView();Require(!controller.LowerView&&camera.orthographic,"Home reset failed");
                    controller.RestoreRegionState(initial);
                }
                var result=action=="check"?"PASS: six zoom levels, perspective pitch, focus/heading preservation, JSON save round trip, exact isometric return and Home reset.":"Current lowerView="+controller.LowerView+", orthographic="+camera.orthographic+", pitch="+camera.transform.eulerAngles.x;
                File.WriteAllText("/Users/joelinstrum/Documents/Codex/2026-10-12/begin-from-this-handoff-and-start/work/CameraAngle-"+action+".txt",result);
                Debug.Log(result);
            }
            catch(System.Exception e){Debug.LogException(e);}
        }
    }
}
#endif

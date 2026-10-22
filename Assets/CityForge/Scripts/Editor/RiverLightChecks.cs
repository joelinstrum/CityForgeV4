#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using CityForge.CameraSystem;
using CityForge.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityForge.EditorTools
{
    /// <summary>Temporary, reversible isolation of river surface shading at fixed camera poses.</summary>
    [InitializeOnLoad]
    public static class RiverLightChecks
    {
        const string Request = "Library/RiverLight.request";
        const string Output = "Library/RiverLightStudy/";
        static bool running;
        static RiverLightChecks() => EditorApplication.update += Poll;

        static void Poll()
        {
            if (running || !File.Exists(Request) || !EditorApplication.isPlaying ||
                EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(Request);
            Directory.CreateDirectory(Output);
            running = true;
            CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Run()));
        }

        static IEnumerator Guard(IEnumerator routine)
        {
            try
            {
                while (true)
                {
                    object next;
                    try { if (!routine.MoveNext()) yield break; next = routine.Current; }
                    catch (Exception e)
                    {
                        File.WriteAllText(Output + "Status.txt", "FAIL: " + e);
                        Debug.LogException(e);
                        yield break;
                    }
                    yield return next;
                }
            }
            finally { (routine as IDisposable)?.Dispose(); running = false; }
        }

        static IEnumerator Run()
        {
            var view = UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if (!view || !view.ApprovedDistrict) throw new InvalidOperationException("Open a native river district first.");
            var preview = view.ApprovedDistrict.GetComponentInChildren<CrestDistrictRiverPreview>();
            if (!preview) throw new InvalidOperationException("Native Crest preview required.");
            var rig = view.GetComponentInChildren<CityForgeCameraController>();
            var camera = rig.GetComponent<Camera>();
            var pose = rig.CaptureRegionState();
            var material = preview.Settings;
            var flow = preview.GetComponent<RiverFlowAnimation>();
            bool oldRig = rig.enabled, oldFlow = flow.enabled;
            var sun = RenderSettings.sun;
            if (!sun) foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (light.type == LightType.Directional && light.enabled) { sun = light; break; }
            if (!sun) throw new InvalidOperationException("Directional sunlight required.");
            float sunIntensity = sun.intensity;
            var sunRotation = sun.transform.rotation;
            var frozenAdvection = Shader.GetGlobalVector("_CityForgeRiverAdvection");
            var properties = new[] { "_WorkflowMode", "_Crest_Specular", "_Crest_NormalMapStrength", "_Crest_Smoothness", "_Crest_SmoothnessFar", "_Crest_NormalMapScale", "_Crest_Occlusion" };
            var values = Array.ConvertAll(properties, material.GetFloat);
            bool oldSpecular = material.IsKeywordEnabled("_SPECULAR_SETUP");
            var normalTexture = material.GetTexture("_Crest_NormalMapTexture");
            var read = new Texture2D(1600, Mathf.RoundToInt(1600 / camera.aspect), TextureFormat.RGB24, false);
            var target = RenderTexture.GetTemporary(read.width, read.height, 24, RenderTextureFormat.ARGBHalf);
            var previousTarget = RenderTexture.active;
            var report = new StringBuilder("River shading isolation. Advection frozen; camera pose fixed within each column.\n");
            string[] cases = { "Baseline", "IndirectOff", "SunOff", "SunAndIndirectOff", "SunTiltMinus10", "SunTiltPlus10" };
            try
            {
                rig.SetViewTilt(0);
                var lowPosition = camera.transform.position;
                float lowFov = camera.fieldOfView, lowNear = camera.nearClipPlane, lowFar = camera.farClipPlane;
                rig.SetViewTilt(1);
                float distance = Vector3.Distance(camera.transform.position, pose.focus);
                float size = camera.orthographicSize, near = camera.nearClipPlane, far = camera.farClipPlane;
                rig.enabled = false;
                flow.enabled = false;
                Shader.SetGlobalVector("_CityForgeRiverAdvection", frozenAdvection);
                for (int trial = 0; trial < cases.Length; trial++)
                {
                    for (int p = 0; p < properties.Length; p++) material.SetFloat(properties[p], values[p]);
                    if (oldSpecular) material.EnableKeyword("_SPECULAR_SETUP"); else material.DisableKeyword("_SPECULAR_SETUP");
                    material.SetTexture("_Crest_NormalMapTexture", normalTexture);
                    sun.intensity = trial == 2 || trial == 3 ? 0 : sunIntensity;
                    sun.transform.rotation = sunRotation;
                    if (trial == 1 || trial == 3) material.SetFloat("_Crest_Occlusion", 0);
                    if (trial == 4 || trial == 5) sun.transform.rotation = Quaternion.AngleAxis(trial == 4 ? -10 : 10, sun.transform.right) * sunRotation;
                    for (int vantage = 0; vantage < 3; vantage++)
                    {
                        float angle = vantage == 0 ? 12 : vantage == 1 ? 35 : 40;
                        var rotation = Quaternion.Euler(angle, pose.yaw, 0);
                        camera.transform.SetPositionAndRotation(vantage == 0 ? lowPosition : pose.focus - rotation * Vector3.forward * distance, rotation);
                        camera.orthographic = vantage == 2;
                        camera.orthographicSize = size;
                        camera.fieldOfView = vantage == 0 ? lowFov : 2 * Mathf.Atan(size / distance) * Mathf.Rad2Deg;
                        camera.nearClipPlane = vantage == 0 ? lowNear : near;
                        camera.farClipPlane = vantage == 0 ? lowFar : far;
                        camera.ResetProjectionMatrix();
                        int frame = Time.frameCount;
                        while (Time.frameCount < frame + 8) yield return null;
                        string label = cases[trial] + (vantage == 2 ? "-Ortho-40" : "-Perspective-" + angle);
                        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                        RenderTexture.active = target;
                        read.ReadPixels(new Rect(0, 0, read.width, read.height), 0, 0);
                        read.Apply();
                        File.WriteAllBytes(Output + label + ".png", read.EncodeToPNG());
                        report.AppendLine(label + ": sunIntensity=" + sun.intensity + ", sunRotation=" + sun.transform.eulerAngles + ", " + string.Join(", ", Array.ConvertAll(properties, p => p + "=" + material.GetFloat(p))) +
                            ", mipBias=" + material.GetTexture("_Crest_NormalMapTexture").mipMapBias + ", specularKeyword=" + material.IsKeywordEnabled("_SPECULAR_SETUP"));
                        File.WriteAllText(Output + "Status.txt", label);
                    }
                }
                File.WriteAllText(Output + "Checks.txt", report + "\nCompleted18 light-isolation captures. Baseline material/camera/flow restored in finally. No player saves written. Visual comparison required; completion is not aesthetic approval.");
                File.WriteAllText(Output + "Status.txt", "COMPLETE");
            }
            finally
            {
                for (int p = 0; p < properties.Length; p++) material.SetFloat(properties[p], values[p]);
                if (oldSpecular) material.EnableKeyword("_SPECULAR_SETUP"); else material.DisableKeyword("_SPECULAR_SETUP");
                material.SetTexture("_Crest_NormalMapTexture", normalTexture);
                RenderTexture.active = previousTarget;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.Destroy(read);
                sun.intensity = sunIntensity;
                sun.transform.rotation = sunRotation;
                flow.enabled = oldFlow;
                rig.enabled = oldRig;
                rig.RestoreRegionState(pose);
            }
        }
    }
}
#endif

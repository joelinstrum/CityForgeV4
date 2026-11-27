using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityForge.World
{
    // User-approved E is immutable. E2 changes only sky fill and soft filtering.
    internal static class AfternoonLightingBaseline
    {
        internal const float EAmbient = .60f;
        internal const float E2Ambient = .69f;
        internal static SphericalHarmonicsL2 SkyProbe(bool e2)
        {
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(new Color(.22f, .24f, .28f));
            probe.AddDirectionalLight(Vector3.up, new Color(.8f, .87f, 1f), .12f);
            var tint = new Color(.92f, .99f, 1.12f);
            for (int c = 0; c < 3; c++)
            for (int k = 0; k < 9; k++)
                probe[c, k] *= (e2 ? E2Ambient : EAmbient) * tint[c];
            return probe;
        }
        internal static void ApplySun(Light light, bool e2)
        {
            light.intensity = 2.6f;
            light.color = new Color(1f, .98f, .945f);
            light.transform.rotation = Quaternion.Euler(32f, 290f, 0f);
            light.shadowStrength = .97f;
            light.shadows = LightShadows.Soft;
            light.shadowBias = .015f;
            light.shadowNormalBias = .035f;
            var data = light.GetUniversalAdditionalLightData();
            data.usePipelineSettings = false;
            data.softShadowQuality = e2 ? SoftShadowQuality.High : SoftShadowQuality.Medium;
        }
    }

    // One small local probe, refreshed only after moving beyond its coverage or changing phase.
    // Capture cost is transient; never refresh every frame or change water reflections.
    internal sealed class DistrictLightingReflection : MonoBehaviour
    {
        internal PrototypeRegionView View;
        private ReflectionProbe probe;
        private Vector3 center;
        private RegionTimeOfDay phase;
        private float nextCheck;
        private int capture = -1;
        private void LateUpdate()
        {
            if (!View || !View.ApprovedDistrict || View.DistrictLoading || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 2f;
            var rig = View.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>();
            if (!rig) return;
            var camera = rig.GetComponent<Camera>();
            camera.allowHDR = true;
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f));
            var ground = new Plane(Vector3.up, View.ApprovedDistrict.transform.position);
            if (!ground.Raycast(ray, out float distance)) return;
            var point = ray.GetPoint(distance);
            for (int i = 0; i < 3; i++)
            {
                float height = View.ApprovedDistrict.Height(new Vector2(point.x, point.z));
                ground = new Plane(Vector3.up, new Vector3(0, height, 0));
                if (ground.Raycast(ray, out distance)) point = ray.GetPoint(distance);
            }
            point.y += 6f;
            if (probe && (capture >= 0 && !probe.IsFinishedRendering(capture) ||
                (Vector3.Distance(center, point) < 64f && phase == View.TimeOfDay))) return;
            if (!probe)
            {
                probe = new GameObject("District context reflection").AddComponent<ReflectionProbe>();
                probe.transform.SetParent(transform, false);
                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
                probe.resolution = 128;
                probe.size = Vector3.one * 160f;
                probe.clearFlags = ReflectionProbeClearFlags.Skybox;
                probe.nearClipPlane = .3f;
                probe.farClipPlane = 180f;
                probe.shadowDistance = 80f;
                probe.intensity = .3f;
                probe.importance = 100;
            }
            center = point;
            phase = View.TimeOfDay;
            probe.transform.position = point;
            capture = probe.RenderProbe();
        }
    }

    public sealed partial class MeadowDistrictLandscape
    {
        private Volume productionAfternoonVolume;
        private SphericalHarmonicsL2 productionAmbientFallback;

        private void ApplyProductionAfternoonLighting(RegionTimeOfDay time, float progress)
        {
            if (IsRegionOverview || !settingsApplied) return;
            bool afternoon = time == RegionTimeOfDay.Afternoon;
            if (!productionAfternoonVolume && afternoon)
            {
                var profile = Own(ScriptableObject.CreateInstance<VolumeProfile>());
                var grade = profile.Add<ColorAdjustments>();
                grade.postExposure.Override(0f);
                grade.contrast.Override(0f);
                grade.saturation.Override(0f);
                grade.colorFilter.Override(Color.white);
                profile.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);
                productionAfternoonVolume = new GameObject("Production afternoon E2").AddComponent<Volume>();
                productionAfternoonVolume.transform.SetParent(transform, false);
                productionAfternoonVolume.isGlobal = true;
                productionAfternoonVolume.priority = 100f;
                productionAfternoonVolume.sharedProfile = profile;
                var context = productionAfternoonVolume.gameObject.AddComponent<DistrictLightingReflection>();
                context.View = View;
            }
            // Keep the existing clock and its late-afternoon transition to evening.
            float weight = afternoon ? 1f - Mathf.SmoothStep(0f, 1f,
                PrototypeRegionView.LateAfternoonProgress(progress)) : 0f;
            if (productionAfternoonVolume)
            {
                productionAfternoonVolume.enabled = afternoon;
                productionAfternoonVolume.weight = weight;
            }
            if (!afternoon) return;
            foreach (var sun in districtSuns)
            {
                var rotation = sun.light.transform.rotation;
                float intensity = sun.light.intensity;
                var color = sun.light.color;
                float strength = sun.light.shadowStrength;
                AfternoonLightingBaseline.ApplySun(sun.light, true);
                sun.light.transform.rotation = Quaternion.Slerp(rotation, sun.light.transform.rotation, weight);
                sun.light.intensity = Mathf.Lerp(intensity, sun.light.intensity, weight);
                sun.light.color = Color.Lerp(color, sun.light.color, weight);
                sun.light.shadowStrength = Mathf.Lerp(strength, sun.light.shadowStrength, weight);
            }
            var target = AfternoonLightingBaseline.SkyProbe(true);
            var result = new SphericalHarmonicsL2();
            for (int c = 0; c < 3; c++)
            for (int k = 0; k < 9; k++)
                result[c, k] = Mathf.Lerp(productionAmbientFallback[c, k], target[c, k], weight);
            RenderSettings.ambientMode = AmbientMode.Custom;
            RenderSettings.ambientProbe = result;
        }
    }
}

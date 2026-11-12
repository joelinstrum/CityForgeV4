using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace CityForge.World
{
    public enum RegionTimeOfDay { Afternoon, Morning, Noon, Evening, Night }

    public enum AfternoonShadowMode { Baseline, Refined, DetailedCanopy }

    public sealed partial class PrototypeRegionView
    {
        internal static readonly RegionTimeOfDay[] DaylightChoices =
        {
            RegionTimeOfDay.Morning, RegionTimeOfDay.Noon, RegionTimeOfDay.Afternoon,
            RegionTimeOfDay.Evening, RegionTimeOfDay.Night
        };

        private Dropdown daylightPicker;
        private Dropdown afternoonShadowPicker;
        public AfternoonShadowMode AfternoonShadows { get; private set; } = AfternoonShadowMode.Refined;
        internal bool UseRefinedAfternoon => TimeOfDay == RegionTimeOfDay.Afternoon &&
            AfternoonShadows != AfternoonShadowMode.Baseline;
        private GameObject afternoonLightingButton;
        private GameObject afternoonLightingPanel;
        private readonly float[] afternoonLighting = (float[])AfternoonDefaults.Clone();
        private static bool LinearLighting => QualitySettings.activeColorSpace == ColorSpace.Linear;
        internal static readonly float[] LinearAfternoonDefaults = { 31f, 2.7379f, 1.096f, .62f, .68f };
        internal static readonly float[] AfternoonDefaults = { 36f, 1.23f, .98f, .62f, .05f };
        private bool afternoonCalibration = true;
        internal bool UseAfternoonCalibration => TimeOfDay == RegionTimeOfDay.Afternoon && afternoonCalibration;
        internal float AfternoonElevation => afternoonLighting[0];
        internal float AfternoonSunlight => afternoonLighting[1];
        internal float AfternoonAmbient => afternoonLighting[2];
        internal float AfternoonShadowStrength => afternoonLighting[3];
        internal float AfternoonExposure => afternoonLighting[4];

        public RegionTimeOfDay TimeOfDay { get; private set; }

        private void BuildDaylightPicker(Transform hud, Font font)
        {
            var picker = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            picker.name = "Time of Day";
            var rect = (RectTransform)picker.transform;
            rect.SetParent(hud, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-25, 72);
            rect.sizeDelta = new Vector2(205, 36);
            daylightPicker = picker.GetComponent<Dropdown>();
            daylightPicker.captionText.font = daylightPicker.itemText.font = font;
            var options = new List<string>(DaylightChoices.Length);
            foreach (var time in DaylightChoices) options.Add(time.ToString());
            daylightPicker.ClearOptions();
            daylightPicker.AddOptions(options);
            daylightPicker.onValueChanged.AddListener(index => SetTimeOfDay(DaylightChoices[index]));

            var label = HudText(rect, "Time of day label", font, 16);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, 1);
            label.rectTransform.anchoredPosition = new Vector2(0, 13);
            label.rectTransform.sizeDelta = new Vector2(205, 24);
            label.alignment = TextAnchor.MiddleCenter;
            label.text = "TIME OF DAY";
            var shadowPicker = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            shadowPicker.name = "Afternoon Shadows";
            var shadowRect = (RectTransform)shadowPicker.transform;
            shadowRect.SetParent(hud, false);
            shadowRect.anchorMin = shadowRect.anchorMax = shadowRect.pivot = new Vector2(1, 0);
            shadowRect.anchoredPosition = new Vector2(-25, 139);
            shadowRect.sizeDelta = rect.sizeDelta;
            afternoonShadowPicker = shadowPicker.GetComponent<Dropdown>();
            afternoonShadowPicker.captionText.font = afternoonShadowPicker.itemText.font = font;
            afternoonShadowPicker.ClearOptions();
            afternoonShadowPicker.AddOptions(new List<string> { "Shadows: Baseline", "Shadows: Refined", "Shadows: Detailed canopy" });
            afternoonShadowPicker.onValueChanged.AddListener(index => SetAfternoonShadows((AfternoonShadowMode)index));
            BuildAfternoonLightingPanel(hud, font);
            RefreshDaylightPicker();
        }

        private void BuildAfternoonLightingPanel(Transform hud, Font font)
        {
            Button AddButton(Transform parent, string caption, Vector2 position, Vector2 size)
            {
                var button = DefaultControls.CreateButton(new DefaultControls.Resources());
                button.name = caption;
                var rect = (RectTransform)button.transform;
                rect.SetParent(parent, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
                var label = button.GetComponentInChildren<Text>();
                label.font = font;
                label.text = caption;
                return button.GetComponent<Button>();
            }

            var defaults = LinearLighting ? LinearAfternoonDefaults : AfternoonDefaults;
            Array.Copy(defaults, afternoonLighting, defaults.Length);
            var open = AddButton(hud, "Afternoon lighting", new Vector2(-25, 181), new Vector2(205, 32));
            afternoonLightingButton = open.gameObject;
            afternoonLightingPanel = new GameObject("Afternoon lighting controls", typeof(RectTransform), typeof(Image));
            var panel = (RectTransform)afternoonLightingPanel.transform;
            panel.SetParent(hud, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1, 0);
            panel.anchoredPosition = new Vector2(-25, 219);
            panel.sizeDelta = new Vector2(300, 268);
            panel.GetComponent<Image>().color = new Color(.08f, .12f, .16f, .97f);
            panel.gameObject.SetActive(false);
            open.onClick.AddListener(() => panel.gameObject.SetActive(!panel.gameObject.activeSelf));

            var compare = AddButton(panel, "Lighting: Calibrated", new Vector2(-12, 10), new Vector2(174, 28));
            var reset = AddButton(panel, "Reset", new Vector2(-194, 10), new Vector2(94, 28));
            var names = new[] { "Sun elevation", "Sunlight", "Ambient fill", "Shadow strength", "Exposure" };
            var formats = new[] { "0°", "0.00×", "0.00×", "0%", "+0.00 EV;-0.00 EV;0.00 EV" };
            var minimum = new[] { 15f, .65f, .60f, .30f, -.75f };
            var maximum = LinearLighting
                ? new[] { 65f, 3.30f, 2.15f, .85f, 1.35f }
                : new[] { 65f, 1.65f, 1.30f, .85f, .50f };
            var sliders = new Slider[names.Length];

            void RefreshCalibration()
            {
                compare.GetComponentInChildren<Text>().text = afternoonCalibration ? "Lighting: Calibrated" : "Lighting: Previous";
                foreach (var slider in sliders) slider.interactable = afternoonCalibration;
                ApplyDistrictDaylight();
            }

            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                float bottom = 224 - i * 43;
                var label = HudText(panel, names[i], font, 15);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, 0);
                label.rectTransform.anchoredPosition = new Vector2(0, bottom + 25);
                label.rectTransform.sizeDelta = new Vector2(274, 20);
                label.alignment = TextAnchor.MiddleLeft;
                void UpdateLabel(float value) => label.text = names[index] + "   " + value.ToString(formats[index]);
                UpdateLabel(afternoonLighting[i]);

                var sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
                sliderObject.name = names[i] + " slider";
                var rect = (RectTransform)sliderObject.transform;
                rect.SetParent(panel, false);
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
                rect.anchoredPosition = new Vector2(0, bottom + 7);
                rect.sizeDelta = new Vector2(274, 18);
                var slider = sliders[i] = sliderObject.GetComponent<Slider>();
                slider.minValue = minimum[i];
                slider.maxValue = maximum[i];
                slider.wholeNumbers = i == 0;
                slider.SetValueWithoutNotify(afternoonLighting[i]);
                slider.onValueChanged.AddListener(value =>
                {
                    afternoonLighting[index] = value;
                    UpdateLabel(value);
                    ApplyDistrictDaylight();
                });
            }

            compare.onClick.AddListener(() =>
            {
                afternoonCalibration = !afternoonCalibration;
                RefreshCalibration();
            });
            reset.onClick.AddListener(() =>
            {
                for (int i = 0; i < sliders.Length; i++) sliders[i].value = defaults[i];
                afternoonCalibration = true;
                RefreshCalibration();
            });
        }

        public void SetAfternoonShadows(AfternoonShadowMode mode)
        {
            if (!Enum.IsDefined(typeof(AfternoonShadowMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));
            AfternoonShadows = mode;
            if (TimeOfDay == RegionTimeOfDay.Afternoon && ApprovedDistrict)
                ApprovedDistrict.ApplyDistrictSun();
            RefreshDaylightPicker();
        }

        public void SetTimeOfDay(RegionTimeOfDay time)
        {
            if (!Enum.IsDefined(typeof(RegionTimeOfDay), time))
                throw new ArgumentOutOfRangeException(nameof(time));
            bool changed = TimeOfDay != time;
            TimeOfDay = time;
            ApplyDistrictDaylight();
            // A daylight preset is a lighting cut. Drop history from the previous sun/exposure.
            if (changed && SelectedDistrict >= 0 && GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>() is { } rig &&
                rig.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData) &&
                cameraData.antialiasing == AntialiasingMode.TemporalAntiAliasing)
                cameraData.resetHistory = true;
            RefreshDaylightPicker();
        }

        private void RefreshDaylightPicker()
        {
            if (!daylightPicker) return;
            daylightPicker.gameObject.SetActive(SelectedDistrict >= 0);
            daylightPicker.SetValueWithoutNotify(Array.IndexOf(DaylightChoices, TimeOfDay));
            bool showAfternoon = SelectedDistrict >= 0 && ApprovedDistrict && TimeOfDay == RegionTimeOfDay.Afternoon;
            afternoonShadowPicker.gameObject.SetActive(showAfternoon);
            afternoonLightingButton.SetActive(showAfternoon);
            if (!showAfternoon) afternoonLightingPanel.SetActive(false);
            afternoonShadowPicker.SetValueWithoutNotify((int)AfternoonShadows);
        }

        private void ApplyDistrictDaylight()
        {
            if (SelectedDistrict < 0) return;
            if (ApprovedDistrict)
            {
                ApprovedDistrict.ApplyDistrictDaylight(TimeOfDay);
                return;
            }
            var sun = transform.Find("RegionSun");
            if (sun) ApplySun(sun.GetComponent<Light>(), TimeOfDay, Quaternion.Euler(32f, 205f, 0f),
                RegionSunIntensity * DistrictSunScale, new Color(1f, .94f, .82f));
            ApplyAmbient(LotDaylightProbe(), new Color(.32f, .38f, .46f), new Color(.22f, .245f, .25f),
                new Color(.105f, .095f, .08f), 1f, TimeOfDay);
        }

        private const float NoonElevation = 55f;
        private static readonly Color NoonSunColor = new Color(1f, .99f, .965f);

        internal static Color CalibratedAmbientTint(RegionTimeOfDay time) =>
            time == RegionTimeOfDay.Noon ? new Color(.98f, .995f, 1.015f) : new Color(.96f, .985f, 1.035f);

        internal static void ApplyCalibratedGrade(ColorAdjustments grade, RegionTimeOfDay time, float exposure, bool linear)
        {
            bool noon = time == RegionTimeOfDay.Noon;
            grade.postExposure.Override(noon ? exposure - .16f : exposure);
            grade.contrast.overrideState = linear;
            grade.contrast.value = noon ? 3f : 6f;
            grade.saturation.overrideState = linear || noon;
            grade.saturation.value = noon ? -7f : 0f;
        }

        internal static void ApplySun(Light light, RegionTimeOfDay time, Quaternion afternoonRotation,
            float afternoonIntensity, Color afternoonColor)
        {
            var afternoon = afternoonRotation.eulerAngles;
            // A directional light points toward the ground, away from the sun.
            // Map east is +X, west is -X, and south is -Z.
            var (rotation, intensity, color) = time switch
            {
                RegionTimeOfDay.Morning => (Quaternion.Euler(23f, 270f, 0), .85f, new Color(1f, .84f, .66f)),
                RegionTimeOfDay.Noon => (Quaternion.Euler(NoonElevation, 33f, 0f), .76f, NoonSunColor),
                RegionTimeOfDay.Evening => (Quaternion.Euler(12f, 90f, 0), .44f, new Color(1f, .68f, .43f)),
                RegionTimeOfDay.Night => (Quaternion.Euler(38f, 285f, 0), .17f, new Color(.66f, .78f, 1f)),
                _ => (Quaternion.Euler(afternoon.x, 65f, 0), 1f, afternoonColor)
            };
            light.transform.rotation = rotation;
            light.intensity = afternoonIntensity * intensity;
            light.color = color;
        }

        internal static void ApplyCalibratedSun(Light light, RegionTimeOfDay time, float baseIntensity,
            float elevation, float sunlight, bool linear)
        {
            bool noon = time == RegionTimeOfDay.Noon;
            light.transform.rotation = noon ? Quaternion.Euler(NoonElevation, 33f, 0f)
                : Quaternion.Euler(elevation, linear ? 25f : 65f, 0f);
            // The higher noon sun puts more light onto roofs and terrain.
            light.intensity = baseIntensity * sunlight * (noon ? (linear ? .70f : .76f) : 1f);
            light.color = noon ? NoonSunColor : linear ? new Color(1f, .927f, .8058f) : new Color(1f, .97f, .90f);
        }

        internal static void ApplyAmbient(SphericalHarmonicsL2 probe, Color sky, Color equator, Color ground,
            float intensity, RegionTimeOfDay time, float ambientScale = 1f, Color? ambientTint = null)
        {
            var (level, tint) = AmbientPreset(time);
            tint *= ambientTint ?? Color.white;
            RenderSettings.ambientSkyColor = sky * tint;
            RenderSettings.ambientEquatorColor = equator * tint;
            RenderSettings.ambientGroundColor = ground * tint;
            const float districtFillScale = .90f;
            RenderSettings.ambientIntensity = intensity * level * districtFillScale * ambientScale;
            RenderSettings.ambientProbe = TintDistrictProbe(probe, time, ambientScale, ambientTint) * districtFillScale;
        }

        internal static SphericalHarmonicsL2 TintDistrictProbe(SphericalHarmonicsL2 probe, RegionTimeOfDay time, float ambientScale = 1f, Color? ambientTint = null)
        {
            var (level, tint) = AmbientPreset(time);
            tint *= ambientTint ?? Color.white;
            for (int channel = 0; channel < 3; channel++)
            for (int coefficient = 0; coefficient < 9; coefficient++)
                probe[channel, coefficient] *= level * tint[channel] * ambientScale;
            return probe;
        }

        private static (float level, Color tint) AmbientPreset(RegionTimeOfDay time) => time switch
            {
                RegionTimeOfDay.Morning => (1.05f, new Color(1f, .97f, .94f)),
                RegionTimeOfDay.Evening => (.58f, new Color(1f, .90f, .84f)),
                RegionTimeOfDay.Night => (.19f, new Color(.56f, .72f, 1f)),
                _ => (1f, Color.white)
            };
    }

    public sealed partial class MeadowDistrictLandscape
    {
        private static readonly int FoliageShadowCutout = Shader.PropertyToID("_CityForgeFoliageShadowCutout");
        private float savedFoliageShadowCutout;
        private Volume afternoonCalibrationVolume;
        private ColorAdjustments afternoonColor;
        private float seasonalShadowStrength;
        internal bool CalibratedAfternoon => !IsRegionOverview && View.UseAfternoonCalibration;
        internal bool LinearAfternoon => (CalibratedAfternoon || UseBalancedNoon(View.TimeOfDay)) && QualitySettings.activeColorSpace == ColorSpace.Linear;
        internal static readonly Vector3 AfternoonSkyDirection = new Vector3(.70f, .12f, -.70f).normalized;
        internal SphericalHarmonicsL2 AddAfternoonSkyFill(SphericalHarmonicsL2 probe)
        {
            if (LinearAfternoon)
                probe.AddDirectionalLight(AfternoonSkyDirection, new Color(.80f, .89f, 1f), .49f * DistrictAmbientScale / 1.096f);
            return probe;
        }

        internal float DistrictAmbientScale => (CalibratedAfternoon || UseBalancedNoon(View.TimeOfDay)) ? View.AfternoonAmbient : 1f;
        internal Color DistrictAmbientTint => CalibratedAfternoon || UseBalancedNoon(View.TimeOfDay)
            ? PrototypeRegionView.CalibratedAmbientTint(View.TimeOfDay) : Color.white;

        private void BuildAfternoonCalibrationVolume()
        {
            if (IsRegionOverview) return;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(false);
            tone.mode.Override(TonemappingMode.ACES);
            afternoonColor = profile.Add<ColorAdjustments>(false);
            owned.Add(profile);
            owned.Add(tone);
            owned.Add(afternoonColor);
            afternoonCalibrationVolume = new GameObject("Afternoon calibration volume").AddComponent<Volume>();
            afternoonCalibrationVolume.transform.SetParent(transform, false);
            afternoonCalibrationVolume.isGlobal = true;
            afternoonCalibrationVolume.priority = 31;
            afternoonCalibrationVolume.sharedProfile = profile;
            ApplyAfternoonExposure();
        }

        private void ApplyAfternoonExposure()
        {
            if (!afternoonCalibrationVolume) return;
            afternoonCalibrationVolume.enabled = CalibratedAfternoon || UseBalancedNoon(View.TimeOfDay);
            PrototypeRegionView.ApplyCalibratedGrade(afternoonColor, View.TimeOfDay, View.AfternoonExposure, LinearAfternoon);
        }

        private readonly List<(Light light, Quaternion rotation, float intensity, Color color, bool pipelineBias, float depthBias, float normalBias)> districtSuns = new();

        private bool UseBalancedNoon(RegionTimeOfDay time) =>
            time == RegionTimeOfDay.Noon && !IsRegionOverview;

        internal void ApplyDistrictSun()
        {
            var time = View.TimeOfDay;
            bool refined = !IsRegionOverview && View.UseRefinedAfternoon;
            bool balanced = UseBalancedNoon(time);
            if (settingsApplied && !IsRegionOverview)
                Shader.SetGlobalFloat(FoliageShadowCutout,
                    refined && View.AfternoonShadows == AfternoonShadowMode.DetailedCanopy ? .16f : 0f);
            foreach (var sun in districtSuns)
            {
                PrototypeRegionView.ApplySun(sun.light, time, sun.rotation, sun.intensity, sun.color);
                var lightData = sun.light.GetUniversalAdditionalLightData();
                lightData.usePipelineSettings = !refined && sun.pipelineBias;
                lightData.softShadowQuality = SoftShadowQuality.Low;
                // URP scales caster bias by the soft filter radius. Keep thin rail shadows close to the ground.
                sun.light.shadowBias = refined ? .04f : sun.depthBias;
                sun.light.shadowNormalBias = refined ? .14f : sun.normalBias;
                sun.light.shadows = refined || balanced ? LightShadows.Soft : LightShadows.Hard;
                if (refined)
                    sun.light.color = new Color(sun.color.r, sun.color.g * .985f, sun.color.b * .955f);
                sun.light.shadowStrength = CalibratedAfternoon || balanced ? View.AfternoonShadowStrength : seasonalShadowStrength;
                if (CalibratedAfternoon || balanced)
                    PrototypeRegionView.ApplyCalibratedSun(sun.light, time, sun.intensity,
                        View.AfternoonElevation, View.AfternoonSunlight, LinearAfternoon);
            }
        }

        internal void ApplyDistrictDaylight(RegionTimeOfDay time)
        {
            ApplyDistrictSun();
            ApplyAfternoonExposure();
            if (!settingsApplied) return;
            var irradiance = new SphericalHarmonicsL2();
            for (int channel = 0; channel < 3; channel++)
            for (int coefficient = 0; coefficient < 9; coefficient++)
                irradiance[channel, coefficient] = Baseline.ambientSH[channel * 9 + coefficient] * (channel == 2 ? 1.28f : 1.20f);
            PrototypeRegionView.ApplyAmbient(irradiance, Baseline.ambientSky, Baseline.ambientEquator,
                Baseline.ambientGround, Baseline.ambientIntensity * 1.20f, time, DistrictAmbientScale, DistrictAmbientTint);
            if (LinearAfternoon)
                RenderSettings.ambientProbe = AddAfternoonSkyFill(RenderSettings.ambientProbe);
        }
    }
}

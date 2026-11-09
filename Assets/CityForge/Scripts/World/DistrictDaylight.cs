using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CityForge.World
{
    public enum RegionTimeOfDay { Afternoon, Morning, Noon, Evening, Night }

    public sealed partial class PrototypeRegionView
    {
        private static readonly RegionTimeOfDay[] DaylightChoices =
        {
            RegionTimeOfDay.Morning, RegionTimeOfDay.Noon, RegionTimeOfDay.Afternoon,
            RegionTimeOfDay.Evening, RegionTimeOfDay.Night
        };

        private Dropdown daylightPicker;
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
            RefreshDaylightPicker();
        }

        public void SetTimeOfDay(RegionTimeOfDay time)
        {
            if (!Enum.IsDefined(typeof(RegionTimeOfDay), time))
                throw new ArgumentOutOfRangeException(nameof(time));
            TimeOfDay = time;
            ApplyDistrictDaylight();
            RefreshDaylightPicker();
        }

        private void RefreshDaylightPicker()
        {
            if (!daylightPicker) return;
            daylightPicker.gameObject.SetActive(SelectedDistrict >= 0);
            daylightPicker.SetValueWithoutNotify(Array.IndexOf(DaylightChoices, TimeOfDay));
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

        internal static void ApplySun(Light light, RegionTimeOfDay time, Quaternion afternoonRotation,
            float afternoonIntensity, Color afternoonColor)
        {
            var afternoon = afternoonRotation.eulerAngles;
            // A directional light points toward the ground, away from the sun.
            // Map east is +X, west is -X, and south is -Z.
            var (rotation, intensity, color) = time switch
            {
                RegionTimeOfDay.Morning => (Quaternion.Euler(23f, 270f, 0), .68f, new Color(1f, .84f, .66f)),
                RegionTimeOfDay.Noon => (Quaternion.Euler(68f, 0f, 0), 1f, new Color(1f, .99f, .96f)),
                RegionTimeOfDay.Evening => (Quaternion.Euler(12f, 90f, 0), .44f, new Color(1f, .68f, .43f)),
                RegionTimeOfDay.Night => (Quaternion.Euler(38f, 285f, 0), .17f, new Color(.66f, .78f, 1f)),
                _ => (Quaternion.Euler(afternoon.x, 65f, 0), 1f, afternoonColor)
            };
            light.transform.rotation = rotation;
            light.intensity = afternoonIntensity * intensity;
            light.color = color;
        }

        internal static void ApplyAmbient(SphericalHarmonicsL2 probe, Color sky, Color equator, Color ground,
            float intensity, RegionTimeOfDay time)
        {
            var (level, tint) = AmbientPreset(time);
            RenderSettings.ambientSkyColor = sky * tint;
            RenderSettings.ambientEquatorColor = equator * tint;
            RenderSettings.ambientGroundColor = ground * tint;
            const float districtFillScale = .90f;
            RenderSettings.ambientIntensity = intensity * level * districtFillScale;
            RenderSettings.ambientProbe = TintDistrictProbe(probe, time) * districtFillScale;
        }

        internal static SphericalHarmonicsL2 TintDistrictProbe(SphericalHarmonicsL2 probe, RegionTimeOfDay time)
        {
            var (level, tint) = AmbientPreset(time);
            for (int channel = 0; channel < 3; channel++)
            for (int coefficient = 0; coefficient < 9; coefficient++)
                probe[channel, coefficient] *= level * tint[channel];
            return probe;
        }

        private static (float level, Color tint) AmbientPreset(RegionTimeOfDay time) => time switch
            {
                RegionTimeOfDay.Morning => (.84f, new Color(1f, .97f, .94f)),
                RegionTimeOfDay.Evening => (.58f, new Color(1f, .90f, .84f)),
                RegionTimeOfDay.Night => (.19f, new Color(.56f, .72f, 1f)),
                _ => (1f, Color.white)
            };
    }

    public sealed partial class MeadowDistrictLandscape
    {
        private readonly List<(Light light, Quaternion rotation, float intensity, Color color)> districtSuns = new();

        internal void ApplyDistrictDaylight(RegionTimeOfDay time)
        {
            foreach (var sun in districtSuns)
                PrototypeRegionView.ApplySun(sun.light, time, sun.rotation, sun.intensity, sun.color);

            if (!settingsApplied) return;
            var irradiance = new SphericalHarmonicsL2();
            for (int channel = 0; channel < 3; channel++)
            for (int coefficient = 0; coefficient < 9; coefficient++)
                irradiance[channel, coefficient] = Baseline.ambientSH[channel * 9 + coefficient] * (channel == 2 ? 1.28f : 1.20f);
            PrototypeRegionView.ApplyAmbient(irradiance, Baseline.ambientSky, Baseline.ambientEquator,
                Baseline.ambientGround, Baseline.ambientIntensity * 1.20f, time);
        }
    }
}

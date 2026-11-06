using System;
using System.Collections;
using CityForge.CameraSystem;
using CityForge.UI;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class MeadowDistrictLandscape
    {
        internal RegionWaterBody PickPlayerWater(Ray ray, float groundDistance)
        {
            if (ray.direction.y >= -.001f) return null;
            RegionWaterBody picked = null;
            float closest = groundDistance;
            foreach (var body in View.Data.WaterBodies)
            {
                if (!body.playerCreated && !body.playerChannel) continue;
                float level = body.surfaceLevelNormalized * terrainData.size.y;
                float distance = (level - ray.origin.y) / ray.direction.y;
                if (body.playerChannel)
                {
                    var grades = body.playerChannelLevels;
                    float near = Mathf.Max(0, (grades[0] - ray.origin.y) / ray.direction.y);
                    float far = Mathf.Min(closest, (grades[grades.Length - 1] - ray.origin.y) / ray.direction.y);
                    if (far < near) continue;
                    var bounds = body.Bounds;
                    var center = (bounds.center - Vector2.one * .5f) * View.MapSide;
                    var size = bounds.size * View.MapSide;
                    var box = new Bounds(new Vector3(center.x, (grades[0] + grades[grades.Length - 1]) * .5f, center.y),
                        new Vector3(size.x, Mathf.Max(.01f, grades[0] - grades[grades.Length - 1]), size.y));
                    if (!box.IntersectRay(ray, out float entry) || entry > far) continue;
                    float AboveWater(float at)
                    {
                        var sample = ray.GetPoint(at);
                        return sample.y - PlayerChannelGeometry.Level(body, new Vector2(sample.x, sample.z), View.MapSide);
                    }
                    if (AboveWater(near) < -.001f || AboveWater(far) > .001f) continue;
                    for (int i = 0; i < 14; i++)
                    {
                        float mid = (near + far) * .5f;
                        if (AboveWater(mid) > 0) near = mid;
                        else far = mid;
                    }
                    distance = (near + far) * .5f;
                }
                if (distance < 0 || distance >= closest) continue;
                var hit = ray.GetPoint(distance);
                var point = new Vector2(hit.x, hit.z);
                if (!Contains(point) || !body.Contains(point / View.MapSide + Vector2.one * .5f) || Height(point) >= hit.y) continue;
                picked = body;
                closest = distance;
            }
            return picked;
        }
    }

    public sealed partial class PrototypeRegionView
    {
        public bool RemovePlayerWater(RegionWaterBody body)
        {
            if (body == null || (!body.playerCreated && !body.playerChannel) || !approvedDistrict ||
                SelectedDistrict < 0 || districtLoading || IsSaving || returning ||
                Array.IndexOf(Data.WaterBodies, body) < 0) return false;

            var controls = GetComponentInChildren<CityForgeCameraController>();
            bool controlEnabled = controls.enabled, viewEnabled = enabled;
            var cameraState = controls.CaptureRegionState();
            districtLoading = true;
            enabled = controls.enabled = false;
            var overlay = RegionLoadingOverlay.ShowOperation("Removing water body…");
            overlay.BeginDistrict(RebuildAfterWaterRemoval(body, overlay.Report), () => {
                if (!this) return;
                controls.RestoreRegionState(cameraState);
                controls.enabled = controlEnabled;
                enabled = viewEnabled;
                districtLoading = false;
            }, error => {
                if (regionNote) regionNote.text = "Could not remove the water body.";
                Debug.LogException(error);
            }, "Restoring meadow");
            return true;
        }

        bool WaterClearedTree(RegionWaterBody body, Vector2 point)
        {
            return body.playerChannel ? InPlayerChannelClearing(body, point) : InPlayerLakeClearing(body, point);
        }

        IEnumerator RebuildAfterWaterRemoval(RegionWaterBody body, Action<string, float> progress)
        {
            var previousDistrict = approvedDistrict;
            var previousBodies = Data.WaterBodies;
            var previousMeadow = LoadedMeadow;
            var previousAdditions = ForestAdditions;
            int previousCacheDistrict = LoadedMeadowDistrict;
            bool previousWaterless = Waterless;
            var cached = previousDistrict.CaptureSnapshot();
            var weather = previousDistrict.GetComponent<DistrictWeather>();
            MeadowDistrictLandscape replacement = null;
            RegionReliefMap newRelief = null;
            bool committed = false;

            try
            {
                // The canonical cache carries the original ground paint and surviving trees.
                // BuildStages samples original region heights and applies only the remaining cuts.
                LoadedMeadow = cached;
                LoadedMeadowDistrict = SelectedDistrict;
                // Older saves can retain planted records hidden by a later water edit.
                // Leave the former bed as meadow when the water mask is removed.
                ForestAdditions = Array.FindAll(previousAdditions, tree =>
                    !WaterClearedTree(body, new Vector2(tree.x, tree.z)));
                additionIndex = null;
                Data.WaterBodies = Array.FindAll(previousBodies, water => water != body);
                Waterless = Data.WaterBodies.Length == 0;
                previousDistrict.gameObject.SetActive(false);

                var root = new GameObject("Approved meadow runtime district");
                root.transform.SetParent(transform, false);
                replacement = root.AddComponent<MeadowDistrictLandscape>();
                var baseline = Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline");
                yield return replacement.BuildStages(this, baseline, false,
                    (stage, amount) => progress(stage, .05f + amount * .72f));
                replacement.SetSeason(VisualSeason);
                replacement.SetTreesEnabled(previousDistrict.TreesEnabled);
                replacement.SetSmallPlantsEnabled(previousDistrict.SmallPlantsEnabled);
                if (weather && replacement.TryGetComponent<DistrictWeather>(out var newWeather))
                    newWeather.SetWeather(weather.Current);

                var map = new GameObject("Regional relief map");
                map.transform.SetParent(transform, false);
                map.SetActive(false);
                newRelief = map.AddComponent<RegionReliefMap>();
                yield return newRelief.BuildStages(this, (stage, amount) => progress(stage, .78f + amount * .21f));

                GetComponent<DistrictCombiner>()?.RefreshBorders(newRelief);
                var updatedCache = replacement.CaptureSnapshot();
                approvedDistrict = replacement;
                if (relief) Destroy(relief.gameObject);
                relief = newRelief;
                sessionMeadows[SelectedDistrict] = updatedCache;
                WoodlandRevision++;
                committed = true;
                Destroy(previousDistrict.gameObject);
            }
            finally
            {
                if (!committed && this)
                {
                    Data.WaterBodies = previousBodies;
                    ForestAdditions = previousAdditions;
                    additionIndex = null;
                    Waterless = previousWaterless;
                    LoadedMeadow = previousMeadow;
                    LoadedMeadowDistrict = previousCacheDistrict;
                    approvedDistrict = previousDistrict;
                    if (replacement)
                    {
                        replacement.gameObject.SetActive(false);
                        Destroy(replacement.gameObject);
                    }
                    if (newRelief) Destroy(newRelief.gameObject);
                    previousDistrict.gameObject.SetActive(true);
                }
            }
        }
    }
}

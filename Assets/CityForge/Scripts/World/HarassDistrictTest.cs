using CityForge.CameraSystem;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private DistrictHarass harassTest;
        internal DistrictHarass HarassTest => harassTest;
        internal Transform HarassCameraTarget => harassTest && harassTest.gameObject.activeInHierarchy ? harassTest.transform : null;

        internal bool EndHarassTest(bool showMessage = true)
        {
            if (!harassTest) return false;
            var actor = harassTest.gameObject;
            harassTest = null;
            actor.SetActive(false);
            foreach (var pedestrian in pedestrians)
            {
                if (pedestrian.HarassTestUntil <= 0) continue;
                pedestrian.HarassTestUntil = pedestrian.ScaredUntil = 0;
                if (pedestrian.Animator) pedestrian.Animator.speed = 1;
            }
            if (Application.isPlaying) Destroy(actor); else DestroyImmediate(actor);
            if (showMessage) ShowDistrictMessage("Hooligan harass test ended.");
            return true;
        }

        internal bool TryStartHarassTest(out string problem)
        {
            problem = "Needs a visible walking pedestrian near the camera. Try a closer view during daytime.";
            var rig = GetComponentInChildren<CityForgeCameraController>();
            if (rig && rig.ZoomLevel > 2) { problem = "Zoom closer to the town (levels 0–2) to test Harass."; return false; }
            var focus = rig ? rig.CaptureRegionState().focus : Vector3.zero;
            Pedestrian chosen = null;
            Vector2 start = default;
            float nearest = 80f * 80f;
            foreach (var candidate in pedestrians)
            {
                if (!HarassTargetAvailable(candidate)) continue;
                var point = new Vector2(candidate.Body.position.x, candidate.Body.position.z);
                float distance = (point - new Vector2(focus.x, focus.z)).sqrMagnitude;
                if (distance >= nearest) continue;
                int previous = candidate.Next - candidate.Direction;
                if (candidate.Closed) previous = (previous + candidate.Route.Length) % candidate.Route.Length;
                if (previous < 0 || previous >= candidate.Route.Length) continue;
                var site = Vector2.MoveTowards(point, candidate.Route[previous], 6f);
                if ((site - point).sqrMagnitude < 4f || !FreightClear(site, point, .2f)) continue;
                chosen = candidate; start = site; nearest = distance;
            }
            if (chosen == null) return false;
            var asset = LotAssetCatalog.Load()?.Find("v3-hooligan");
            if (asset?.prefab == null || !LotHarass.Supports(asset.prefab))
            { problem = "Street Hooligan is not available in the local catalog."; return false; }
            EndHarassTest(showMessage: false);
            var instance = LotRuntimeLoader.InstantiateObject(new LotObjectDefinition { assetId = asset.id }, asset, transform);
            instance.name = "Hooligan harass test (temporary)";
            instance.transform.position = new Vector3(start.x, HarassGroundHeight(start), start.y);
            harassTest = instance.AddComponent<DistrictHarass>();
            harassTest.view = this;
            harassTest.districtCamera = rig ? rig.GetComponent<Camera>() : null;
            harassTest.behavior = new LotHarass(instance.transform, startAnimation: false);
            harassTest.TestMode = true;
            harassTest.TestDistrict = SelectedDistrict;
            harassTest.TestEnds = Time.time + 60f;
            foreach (var animator in instance.GetComponentsInChildren<Animator>()) animator.cullingMode = AnimatorCullingMode.CullCompletely;
            // This object is deliberately absent from PlacedActors and PlacedLots: the command never persists it.
            problem = null;
            return true;
        }
    }
}

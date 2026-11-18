#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using CityForge.Lots;
using CityForge.CameraSystem;
using CityForge.World;
using UnityEngine;
namespace CityForge.Editor
{
    public static class HarassFollowChecks
    {
        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        public static void Run()
        {
            var root = new GameObject("Disposable evening Harass checks");
            var td = new TerrainData { heightmapResolution = 33, size = new Vector3(200, 40, 200) };
            var heights = new float[33, 33];
            for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = .5f;
            td.SetHeights(0, 0, heights);
            try
            {
                var view = root.AddComponent<PrototypeRegionView>();
                var land = root.AddComponent<MeadowDistrictLandscape>();
                var terrain = root.AddComponent<Terrain>(); terrain.terrainData = td;
                Set(view, "approvedDistrict", land);
                Set(view, "<Data>k__BackingField", new PrototypeRegionData(1, new float[513, 513], new float[513, 513]) { WaterBodies = Array.Empty<RegionWaterBody>() });
                Set(land, "<View>k__BackingField", view); Set(land, "<DisplayTerrain>k__BackingField", terrain);
                Set(land, "<Side>k__BackingField", 200f); Set(land, "terrainData", td);
                Set(land, "fullTreeInstances", Array.Empty<TreeInstance>());
                var catalog = LotAssetCatalog.Load(); var asset = catalog.Find("v3-hooligan");
                var actor = LotRuntimeLoader.InstantiateObject(new LotObjectDefinition { assetId = asset.id }, asset, root.transform);
                actor.transform.position = new Vector3(10, 20, 20);
                var runner = actor.AddComponent<DistrictHarass>(); runner.view = view; runner.behavior = new LotHarass(actor.transform, startAnimation: false);
                var person = new GameObject("Pedestrian on an existing circuit"); person.transform.SetParent(root.transform); person.transform.position = new Vector3(16, 20, 20);
                var target = new PrototypeRegionView.Pedestrian { Body = person.transform, Route = new[] { new Vector2(10, 20), new Vector2(26, 20) }, Next = 0, Direction = -1, Speed = 1.15f };
                var actors = (List<PrototypeRegionView.Pedestrian>)typeof(PrototypeRegionView).GetField("pedestrians", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
                actors.Add(target);
                foreach (var phase in new[] { RegionTimeOfDay.Morning, RegionTimeOfDay.Noon, RegionTimeOfDay.Afternoon, RegionTimeOfDay.Night })
                {
                    Set(view, "<TimeOfDay>k__BackingField", phase);
                    var p = actor.transform.position; int probes = runner.MovementProbes, candidates = view.HarassCandidateChecks;
                    for (int i = 0; i < 100; i++) runner.Step(.1f, i * .1f, true);
                    Require(p == actor.transform.position && runner.MovementProbes == probes && view.HarassCandidateChecks == candidates, "Non-evening Harass performed movement or searches");
                }
                Set(view, "<TimeOfDay>k__BackingField", RegionTimeOfDay.Evening);
                for (int i = 0; i < 40; i++) runner.Step(.1f, 20 + i * .1f, true);
                Require(actor.transform.position.x > 11 && actor.transform.position.x < 14.1f, "Harasser did not follow or violated personal space");
                Require(target.ScaredUntil > 0 && target.Next == 1 && target.Direction == 1, "Pedestrian did not turn away on its existing circuit");
                Require(runner.MovementProbes < 20 && view.HarassCandidateChecks <= 8, "Harass scanned or probed per frame");
                for (int i = 0; i < 10; i++) runner.Step(.001f, 25, true);
                long activeBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 10000; i++) runner.Step(.001f, 25, true);
                Require(GC.GetAllocatedBytesForCurrentThread() - activeBefore < 128, "Steady active Harass allocates per tick");
                var stopped = actor.transform.position; int oldProbes = runner.MovementProbes, oldCandidates = view.HarassCandidateChecks;
                for (int i = 0; i < 1000; i++) runner.Step(.1f, 30 + i * .1f, false);
                Require(stopped == actor.transform.position && oldProbes == runner.MovementProbes && oldCandidates == view.HarassCandidateChecks, "Offscreen Harass continued expensive work");
                target.Vehicle = true; uint random = 127;
                int count = view.HarassCandidateChecks;
                Require(view.ChooseHarassTarget(Vector2.zero, ref random) == null && view.HarassCandidateChecks - count == 8, "Vehicle targeting or search cap failed");
                target.Vehicle = false;
                // A new blocker invalidates the short movement certificate before another step is taken.
                person.transform.position = new Vector3(26, 20, 20);
                view.PlacedLots.Add(new DistrictLotRecord { center = new Vector2(18, 20), definition = new LotDefinition { width = 5, length = 5,
                    objects = new List<LotObjectDefinition> { new LotObjectDefinition { assetId = "v3-building-r2-four-story-low-wealth-stone-base" } } } });
                stopped = actor.transform.position;
                runner.Step(.2f, 150, true);
                Require(stopped == actor.transform.position && runner.target == null, "Harasser crossed a newly blocked segment");
                Set(view, "<TimeOfDay>k__BackingField", RegionTimeOfDay.Night);
                for (int i = 0; i < 10; i++) runner.Step(.1f, 160, true);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 10000; i++) runner.Step(.1f, 160, true);
                Require(GC.GetAllocatedBytesForCurrentThread() - before < 128, "Inactive Harass allocates per tick");
                view.PlacedLots.Clear();
                person.transform.position = new Vector3(16, 20, 20);
                target.Next = 0; target.Direction = -1; target.ScaredUntil = target.HarassTestUntil = 0;
                Set(view, "<TimeOfDay>k__BackingField", RegionTimeOfDay.Noon);
                Set(view, "<SelectedDistrict>k__BackingField", 3);
                int savedActors = view.PlacedActors.Count;
                Require(view.TryStartHarassTest(out var problem), "District test command failed: " + problem);
                var test = view.HarassTest;
                var testStart = test.transform.position;
                float now = Time.time;
                for (int i = 0; i < 40; i++) test.Step(.1f, now + i * .1f, true);
                Require(view.TimeOfDay == RegionTimeOfDay.Noon && test.transform.position != testStart && target.HarassTestUntil > 0,
                    "Test command did not bypass evening for pursuit and fright while preserving the clock");
                Require(view.PlacedActors.Count == savedActors && view.PlacedLots.Count == 0 && test.TestEnds >= now + 59,
                    "Test command persisted a character or lost its cleanup deadline");
                var cameraObject = new GameObject("Disposable follow camera"); cameraObject.transform.SetParent(root.transform);
                var camera = cameraObject.AddComponent<CityForgeCameraController>(); camera.SetZoomLevel(2);
                Require(camera.FollowDistrictActor(view) && camera.ZoomLevel == 0 && camera.CaptureRegionState().focus == test.transform.position,
                    "Camera did not follow the Hooligan at close zoom");
                test.transform.position += Vector3.right;
                camera.FollowDistrictActor(view);
                Require(camera.CaptureRegionState().focus == test.transform.position, "Follow camera lost moving target");
                var cameraAtEnd = camera.CaptureRegionState().focus;
                Require(view.EndHarassTest(showMessage: false) && !view.HarassCameraTarget && view.SelectedDistrict == 3 && target.HarassTestUntil == 0,
                    "Cancel failed to clear the test or changed districts");
                Require(!camera.FollowDistrictActor(view) && camera.CaptureRegionState().focus == cameraAtEnd && !view.EndHarassTest(false),
                    "Camera follow continued or cancellation was not idempotent");
                Require(view.TryStartHarassTest(out problem), "Could not restart test for expiry check: " + problem);
                view.HarassTest.Step(.1f, view.HarassTest.TestEnds, true);
                Require(!view.HarassCameraTarget && view.SelectedDistrict == 3, "Expiry did not release camera or changed districts");
                Debug.Log("HARASS FOLLOW CHECKS PASS: evening-only following, personal space, pedestrian turns away on retained route, bounded searches/probes, offscreen pause, vehicle exclusion, new blockers, 10000 active and inactive updates without allocations; daytime command, moving camera follow, cancellation and expiry without district navigation.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(td); }
        }
    }
}
#endif

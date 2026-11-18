#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using CityForge.World;
using UnityEngine;

namespace CityForge.Editor
{
    public static class FreightChecks
    {
        private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
        public static void Run()
        {
            LumberjackChecks.Run();
            var roads = new[] {
                (new[] { new Vector2(0,0), new Vector2(100,0) }, false),
                (new[] { new Vector2(50,-20), new Vector2(50,80) }, false),
                (new[] { new Vector2(50,80), new Vector2(100,80) }, false)
            };
            Require(FreightRoadGraph.TryRoute(roads, new Vector2(5,3), new Vector2(95,82), 24, 16, out var route), "Connected road trip failed");
            Require(route.Any(p => p == new Vector2(50,0)) && route[^1] == new Vector2(95,80), "Crossing split or receiver projection failed");
            Require(!FreightRoadGraph.TryRoute(roads, new Vector2(5,30), new Vector2(95,82), 24, 16, out _), "Disconnected camp access accepted");
            var disconnected = new[] { (new[] { Vector2.zero, new Vector2(20,0) }, false), (new[] { new Vector2(100,0), new Vector2(120,0) }, false) };
            Require(!FreightRoadGraph.TryRoute(disconnected, Vector2.zero, new Vector2(120,0), 1, 1, out _), "Disconnected road networks linked");
            var overlap = new[] { (new[] { Vector2.zero, new Vector2(60,0) }, false), (new[] { new Vector2(40,0), new Vector2(100,0) }, false) };
            Require(FreightRoadGraph.TryRoute(overlap, Vector2.zero, new Vector2(100,0), 1, 1, out _), "Overlapping collinear roads failed");
            var camp = new World.DistrictLotRecord { id = Guid.NewGuid().ToString("N"), definition = new LotDefinition(), storedTimber = 8 };
            var receiver = new World.DistrictLotRecord { id = Guid.NewGuid().ToString("N"), definition = new LotDefinition { deliveryRole = LotDeliveryRole.TownCenter } };
            var wagon = new LotObjectDefinition { assetId = "v3-vehicle-lumber-wagon", vehicleScript = new LotVehicleScript { behavior = LotVehicleBehavior.LumberWagon, capacity = 4, handlingSeconds = 4 } };
            camp.definition.objects.Add(wagon);
            var job = new LotVehicleJobState { objectId = wagon.id, position = new Vector2(5,3), hasPosition = true };
            camp.vehicleJobs.Add(job);
            Require(World.LotFreightLedger.Load(camp, job, 4, receiver.id) && camp.storedTimber == 4 && job.cargo == 4, "Load did not transfer stock");
            Require(!World.LotFreightLedger.Load(camp, job, 4, receiver.id) && camp.storedTimber == 4, "Double loading consumed stock");
            var restored = JsonUtility.FromJson<World.DistrictLotRecord>(JsonUtility.ToJson(camp));
            LotFreightValidation.Validate(restored);
            Require(restored.vehicleJobs[0].cargo == 4 && restored.vehicleJobs[0].phase == LotFreightPhase.Outbound && restored.vehicleJobs[0].position == job.position, "In-flight cargo save/reload failed");
            Require(World.LotFreightLedger.Deliver(receiver, restored.vehicleJobs[0]) && receiver.receivedTimber == 4, "Receiver did not receive cargo");
            Require(!World.LotFreightLedger.Deliver(receiver, restored.vehicleJobs[0]) && receiver.receivedTimber == 4, "Delivery credited twice");
            Require(LotDefinition.Parse(JsonUtility.ToJson(camp.definition)).objects[0].vehicleScript.behavior == LotVehicleBehavior.LumberWagon, "Vehicle script lot roundtrip failed");
            Require(LotDefinition.Parse(JsonUtility.ToJson(receiver.definition)).deliveryRole == LotDeliveryRole.TownCenter, "Explicit receiver role roundtrip failed");
            var legacy = JsonUtility.FromJson<World.DistrictLotRecord>("{\"storedTimber\":0,\"definition\":{}}");
            LotFreightValidation.Validate(legacy);
            var bad = new LotVehicleScript { capacity = -1 };
            Require(!bad.IsValid, "Invalid capacity accepted");
            restored.storedTimber = -1;
            bool rejected = false; try { LotFreightValidation.Validate(restored); } catch (InvalidDataException) { rejected = true; }
            Require(rejected, "Invalid saved stock accepted");
            var profile = VehicleProfile.Find(wagon.assetId);
            Require(profile != null && profile.supportsLumberFreight && profile.spawnEvery == 0, "Lumber wagon still auto-spawns or lacks freight capability");
            CheckRunner();
            Debug.Log("FREIGHT CHECKS PASS: crossing/collinear/connected/disconnected routing, access limits, atomic loading/delivery, no duplicate credit, in-flight save/reload, old-save defaults, roles/scripts roundtrip, stock validation, opt-in wagon profile.");
        }
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static void CheckRunner()
        {
            var root = new GameObject("Disposable freight loop");
            var td = new TerrainData { heightmapResolution = 33, size = new Vector3(200, 40, 200) };
            var heights = new float[33,33];
            for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z,x] = .25f;
            td.SetHeights(0,0,heights);
            try
            {
                var view = root.AddComponent<PrototypeRegionView>();
                var land = root.AddComponent<MeadowDistrictLandscape>();
                var terrain = root.AddComponent<Terrain>(); terrain.terrainData = td;
                Set(view, "approvedDistrict", land);
                Set(view, "<SelectedDistrict>k__BackingField", 3);
                var data = new PrototypeRegionData(1, new float[513,513], new float[513,513]) { WaterBodies = Array.Empty<RegionWaterBody>() };
                Set(view, "<Data>k__BackingField", data);
                Set(land, "<View>k__BackingField", view);
                Set(land, "<DisplayTerrain>k__BackingField", terrain);
                Set(land, "<Side>k__BackingField", 200f);
                Set(land, "terrainData", td);
                Set(land, "fullTreeInstances", Array.Empty<TreeInstance>());
                var roadViews = (Dictionary<DistrictRoadRecord, MeshFilter>)view.GetType().GetField("districtRoadMeshes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view);
                roadViews.Add(new DistrictRoadRecord { district = 3, width = 4, points = new[] { new Vector2(10,20), new Vector2(140,20) } }, root.AddComponent<MeshFilter>());
                var camp = new DistrictLotRecord { id = Guid.NewGuid().ToString("N"), district = 3, center = new Vector2(15,30), storedTimber = 8, definition = new LotDefinition() };
                var mill = new DistrictLotRecord { id = Guid.NewGuid().ToString("N"), district = 3, center = new Vector2(95,35), definition = new LotDefinition { deliveryRole = LotDeliveryRole.LumberMill } };
                var town = new DistrictLotRecord { id = Guid.NewGuid().ToString("N"), district = 3, center = new Vector2(45,40), definition = new LotDefinition() };
                town.definition.objects.Add(new LotObjectDefinition{assetId="harbor-council-house"});
                view.PlacedLots.AddRange(new[] { camp, mill, town });
                Require(view.TryFreightDestination(camp, new Vector2(15,23), null, out var chosen, out _) && chosen == mill, "Mill did not take priority over closer town center");
                mill.center = new Vector2(190,190);
                Require(view.TryFreightDestination(camp, new Vector2(15,23), null, out chosen, out _) && chosen == town, "Unreachable mill prevented town fallback");
                mill.center = new Vector2(95,35);
                var item = new LotObjectDefinition { assetId = "v3-vehicle-lumber-wagon", vehicleScript = new LotVehicleScript { behavior = LotVehicleBehavior.LumberWagon, capacity = 4, handlingSeconds = 1 } };
                camp.definition.objects.Add(item);
                var asset = LotAssetCatalog.Load().Find(item.assetId);
                Require(asset?.prefab, "Local wagon asset missing");
                var wagon = LotRuntimeLoader.InstantiateObject(item, asset, root.transform, RegionSeason.Summer);
                wagon.transform.position = new Vector3(15,10,23);
                var runner = wagon.AddComponent<LotResourceVehicle>(); runner.Initialize(view, camp, item, null);
                for (int i = 0; i < 40; i++) runner.Tick(.1f);
                Require(camp.vehicleJobs[0].cargo == 4 && camp.vehicleJobs[0].phase == LotFreightPhase.Outbound, "Runner did not start loaded trip");
                int plansBeforeEdit = view.FreightRoutePlans;
                Set(view, "<LotPresentationBuilds>k__BackingField", 1);
                var routeField = runner.GetType().GetField("route", BindingFlags.NonPublic | BindingFlags.Instance);
                var established = routeField.GetValue(runner);
                runner.Tick(.1f);
                Require(ReferenceEquals(established, routeField.GetValue(runner)) && view.FreightRoutePlans == plansBeforeEdit,
                    "Unrelated lot edit discarded an established wagon route");
                var blocker = new Vector2(45,20);
                Set(land,"fullTreeInstances",new[]{new TreeInstance{prototypeIndex=3,position=new Vector3(blocker.x/200f,0,blocker.y/200f)}});
                for(int i=0;i<400&&routeField.GetValue(runner)!=null;i++)runner.Tick(.1f);
                Require(routeField.GetValue(runner)==null && Vector2.Distance(camp.vehicleJobs[0].position,blocker)>.65f &&
                    camp.vehicleJobs[0].cargo==4 && view.FreightRoutePlans==plansBeforeEdit,"Wagon crossed a new blocker, lost cargo, or replanned before reaching it");
                Set(land,"fullTreeInstances",Array.Empty<TreeInstance>());
                for(int i=0;i<30;i++)runner.Tick(.1f);
                Require(routeField.GetValue(runner)!=null,"Wagon did not retry after the blocker was removed");
                var savedCamp = JsonUtility.FromJson<DistrictLotRecord>(JsonUtility.ToJson(camp));
                LotFreightValidation.Validate(savedCamp);
                UnityEngine.Object.DestroyImmediate(wagon);
                view.PlacedLots.Remove(camp); camp = savedCamp; view.PlacedLots.Add(camp);
                item = camp.definition.objects[0];
                wagon = LotRuntimeLoader.InstantiateObject(item, asset, root.transform, RegionSeason.Summer);
                wagon.transform.position = new Vector3(15,10,23);
                runner = wagon.AddComponent<LotResourceVehicle>(); runner.Initialize(view, camp, item, null);
                Require(Vector2.Distance(new Vector2(wagon.transform.position.x, wagon.transform.position.z), camp.vehicleJobs[0].position) < .001f, "Loaded wagon position did not restore");
                for (int i = 0; i < 2000 && mill.receivedTimber < 4; i++) runner.Tick(.1f);
                Require(mill.receivedTimber == 4 && camp.storedTimber == 4, "Actual runner failed to load/drive/unload to mill");
                Require(town.receivedTimber == 0, "Preferred mill trip credited the town");
                mill.definition.deliveryRole = LotDeliveryRole.None;
                for (int i = 0; i < 2000 && town.receivedTimber < 4; i++) runner.Tick(.1f);
                Require(town.receivedTimber == 4 && camp.storedTimber == 0, "Actual return/reload/fallback trip failed");
                for (int i = 0; i < 2000 && camp.vehicleJobs[0].phase != LotFreightPhase.Waiting; i++) runner.Tick(.1f);
                Require(camp.vehicleJobs[0].phase == LotFreightPhase.Waiting && Vector2.Distance(camp.vehicleJobs[0].position, new Vector2(15,23)) < .1f, "Wagon failed to return home");
                camp.storedTimber = 4; town.definition.deliveryRole = LotDeliveryRole.None; town.definition.deliveryRoleConfigured = true;
                for (int i = 0; i < 100; i++) runner.Tick(.1f);
                Require(camp.storedTimber + camp.vehicleJobs[0].cargo == 4 && camp.vehicleJobs[0].phase != LotFreightPhase.Outbound, "Missing receiver lost stock or dispatched cargo");
                town.definition.deliveryRole=LotDeliveryRole.TownCenter;
                item.vehicleScript=new LotVehicleScript{behavior=LotVehicleBehavior.ResourceDelivery,configured=true,enabled=true,
                    category=VehicleTaskCategory.Resource,resource=VehicleResource.Fish,when=RegionTimeOfDay.Noon,capacity=2,handlingSeconds=1};
                VehicleResources.Store(camp,VehicleResource.Fish,6);
                Set(view,"<TimeOfDay>k__BackingField",RegionTimeOfDay.Morning);
                for(int i=0;i<100;i++)runner.Tick(.1f);
                Require(VehicleResources.Stored(camp,VehicleResource.Fish)+camp.vehicleJobs[0].cargo==6&&VehicleResources.Received(town,VehicleResource.Fish)==0,"Scheduled loading lost cargo or departed at wrong time");
                Set(view,"<TimeOfDay>k__BackingField",RegionTimeOfDay.Noon);
                for(int i=0;i<2000&&VehicleResources.Received(town,VehicleResource.Fish)<2;i++)runner.Tick(.1f);
                Require(VehicleResources.Received(town,VehicleResource.Fish)==2&&camp.storedTimber==4,"Generic scheduled delivery used wrong cargo");
                for(int i=0;i<2000;i++)runner.Tick(.1f);
                Require(VehicleResources.Received(town,VehicleResource.Fish)==2,"Scheduled runner repeated within the same window");
                int plansBeforeRepeat = view.FreightRoutePlans;
                Set(view,"<TimeOfDay>k__BackingField",RegionTimeOfDay.Afternoon);runner.Tick(.1f);
                Set(view,"<TimeOfDay>k__BackingField",RegionTimeOfDay.Noon);
                for(int i=0;i<2000&&VehicleResources.Received(town,VehicleResource.Fish)<4;i++)runner.Tick(.1f);
                Require(VehicleResources.Received(town,VehicleResource.Fish)==4,"Schedule did not reset for next selected period");
                Require(view.FreightRoutePlans == plansBeforeRepeat, "Repeated delivery planned a route instead of reusing its round trip");
                Debug.Log("FREIGHT RUNNER PASS: actual local horse wagon loaded, resumed an in-flight saved trip, drove to preferred mill, unloaded, returned, reloaded, used town fallback, returned home, waited without stock loss when no receiver exists. Synthetic flat QA district, no player save.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(td); }
        }
    }
}
#endif

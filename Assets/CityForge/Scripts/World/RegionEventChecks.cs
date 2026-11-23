#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.Editor
{
    public static class RegionEventChecks
    {
        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        static void Set(object o, string key, object value) => o.GetType().GetField(key, Flags).SetValue(o, value);
        static object Get(object o, string key) => o.GetType().GetField(key, Flags).GetValue(o);
        static object Call(object o, string key, params object[] values) => o.GetType().GetMethod(key, Flags).Invoke(o, values);
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        public static void Run()
        {
            bool Clear(Vector2 a, Vector2 b)
            {
                for (int i=0;i<=20;i++) { var p=Vector2.Lerp(a,b,i/20f); if (p.x>4 && p.x<12 && Mathf.Abs(p.y)<6) return false; }
                return true;
            }
            Require(World.BoundedGroundRoute.TryFind(Vector2.zero,Vector2.right*20,Clear,out var detour) &&
                detour.All(p=>p.x<=4 || p.x>=12 || Mathf.Abs(p.y)>=6) &&
                Enumerable.Range(1,detour.Length-1).All(i=>Clear(detour[i-1],detour[i])), "Bounded walk did not safely detour around council-sized obstacle");
            Require(!World.BoundedGroundRoute.TryFind(Vector2.zero,Vector2.right*20,(a,b)=>a.x<8&&b.x<8,out _), "Bounded walk crossed impassable water barrier");
            var root = new GameObject("Disposable immigration event fixture");
            var td = new TerrainData { heightmapResolution = 33, size = new Vector3(200, 40, 200) };
            var heights = new float[33,33];
            for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z,x] = .5f;
            td.SetHeights(0,0,heights);
            string path = Path.Combine(Path.GetTempPath(), "CityForge-immigration-" + Guid.NewGuid().ToString("N") + ".cfregion");
            try
            {
                var view = root.AddComponent<World.PrototypeRegionView>();
                var land = root.AddComponent<World.MeadowDistrictLandscape>();
                var terrain = root.AddComponent<Terrain>(); terrain.terrainData = td;
                Set(view, "approvedDistrict", land); Set(view, "terrain", terrain);
                Set(view, "<SelectedDistrict>k__BackingField", 3);
                var data = new World.PrototypeRegionData(1, new float[513,513], new float[513,513]) { WaterBodies = Array.Empty<World.RegionWaterBody>() };
                Set(view, "<Data>k__BackingField", data);
                Set(land, "<View>k__BackingField", view); Set(land, "<DisplayTerrain>k__BackingField", terrain);
                Set(land, "<Side>k__BackingField", 200f); Set(land, "terrainData", td);
                Set(view, "lotFont", Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
                var hud = new GameObject("RegionHud", typeof(RectTransform), typeof(Canvas)); hud.transform.SetParent(root.transform, false);
                var town = new World.RegionTown { id = "immigration-test", name = "Test Harbor", capital = true,
                    position = Vector2.one * .51f, establishedYear = 1700 };
                Set(view, "<Towns>k__BackingField", new[] { town });
                var source = new World.DistrictLotRecord { id = Guid.NewGuid().ToString("N"), district = 3,
                    center = new Vector2(60,60), definition = new LotDefinition { name = "Harbor Council House", category = "civics",
                        subcategory = "founders-buildings", width = 6, length = 6, waterfrontRows = 1,
                        objects = new List<LotObjectDefinition> { new() { assetId = "harbor-council-house" } } } };
                view.PlacedLots.Add(source);
                Call(view, "SyncRegionEvents");
                var state = view.RegionEvents.Single();
                Require(state.occurrences == 0, "Placement consumed an arrival");
                Set(view,"nextEventTick",0f);Call(view,"TickRegionEvents");
                Require(view.ImmigrantAssignmentScans==0,"Idle harbor scanned vacant zoning without passengers");
                view.HarborVesselArrived(source, World.HarborVesselKind.CargoBarge);
                view.HarborVesselArrived(source, World.HarborVesselKind.PirateShip);
                Require(state.occurrences == 0, "Other vessel consumed a galleon wave");
                var rule = World.RegionEventCatalog.Definitions.Single(d => d.id == "immigrant-first-wave");
                int firstYear = rule.firstYear;
                rule.firstYear = view.TownYear(source.district) + 1;
                view.HarborVesselArrived(source, World.HarborVesselKind.Galleon);
                Require(state.occurrences == 0, "Arrival outside configured date window fired");
                rule.firstYear = firstYear;
                view.HarborVesselArrived(source, World.HarborVesselKind.Galleon);
                Require(state.occurrences == 1 && state.immigrants.Count == 12, "First galleon did not deliver twelve immigrants");
                Require(town.development.population == 120, "Arrival did not credit town population exactly once");
                Require(state.immigrants.All(p=>p.hasArrivalPoint && view.FreightClear(p.position,p.arrivalPoint,.15f)),
                    "Passenger spawn must stay connected to the landing instead of appearing in an isolated pocket");
                Require(state.immigrants.Sum(p=>p.Population)==120&&state.populationArrived==120,"Visible representatives must account for120 actual people");
                Call(view,"BuildGameplayTools",hud.transform,Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
                Call(view,"OpenGameplayTools");
                var toggle=((UnityEngine.UI.Text)Get(view,"immigrationToggleLabel")).GetComponentInParent<UnityEngine.UI.Button>();
                toggle.onClick.Invoke();
                Require(view.ImmigrationPaused,"Gameplay button did not pause immigration");
                view.HarborVesselArrived(source,World.HarborVesselKind.Galleon);
                Require(state.occurrences==1&&town.development.population==120,"Paused ship arrival still added people or consumed arrival count");
                var legacyFlag=JsonUtility.FromJson<World.SavedRegionInfo>("{\"schema\":1}");
                Require(!legacyFlag.immigrationPaused,"Old saves must default to immigration enabled");
                toggle.onClick.Invoke();Require(!view.ImmigrationPaused,"Gameplay button did not resume immigration");Call(view,"CloseGameplayTools");
                var message = (UnityEngine.UI.Text)Get(view, "eventNoticeText");
                Require(message.text.Contains("Immigrants arrive at Test Harbor"), "Town arrival notice missing");
                Set(view, "nextEventTick", 0f); Call(view, "TickRegionEvents");
                var actors = (IList)Get(view, "pedestrians");
                Require(actors.Count == 12 && state.immigrants.All(p => !p.hasDestination), "No-zone arrivals must wait instead of disappearing");
                source.center += Vector2.right * 4;
                Call(view, "SyncRegionEvents");
                Require(!state.cancelled && view.RegionEvents.Count == 1, "Moving the lot reset/cancelled the event");
                // Actual binary save/reopen, including progress, population and passenger positions.
                var info = new World.SavedRegionInfo { id = Guid.NewGuid().ToString("N"), name = "Immigration QA", district = 3,
                    immigrationPaused = true, towns = new[] { town }, lots = new[] { source }, events = view.CaptureRegionEvents(), waterBodies = data.WaterBodies };
                World.SavedRegionStore.Write(new World.SavedRegionSnapshot { info = info, data = data }, path);
                var reopened = World.SavedRegionStore.Load(path);
                Require(reopened.info.events.Single().occurrences == 1 && reopened.info.events.Single().immigrants.Count == 12 &&
                    reopened.info.towns.Single().development.population == 120 && reopened.info.immigrationPaused, "Binary reopen lost wave/passengers/population");
                foreach (var actor in actors) UnityEngine.Object.DestroyImmediate(((World.PrototypeRegionView.Pedestrian)actor).Body.gameObject);
                actors.Clear(); view.RegionEvents.Clear(); view.RegionEvents.AddRange(reopened.info.events); state = view.RegionEvents.Single();
                Set(view, "nextEventTick", 0f); Call(view, "TickRegionEvents");
                Require(actors.Count == 12, "Restoring passengers duplicated/lost actors");
                var definition = World.RegionEventCatalog.Definitions.Single(d => d.id == "immigrant-first-wave");
                int DestinationCount() => ((IEnumerable)Call(view,"ImmigrantDestinations",definition)).Cast<object>().Count();
                var zones = (Dictionary<Vector2Int,World.TownZone>)Get(view,"townZones");
                var wealth = (Dictionary<Vector2Int,LotWealth>)Get(view,"zoneWealth");
                var cell = (Vector2Int)Call(view,"DistrictGridCell",new Vector2(85,35));
                zones[cell] = World.TownZone.Residential1; wealth[cell] = LotWealth.Low;
                Require(DestinationCount()==0, "Vacant zoning must not make immigrants disappear into empty grass");
                var building = LotAssetCatalog.Load().entries.First(a=>a.category=="Buildings"&&a.prefab);
                var receivers = new List<World.DistrictLotRecord>();
                foreach (var at in new[]{new Vector2(110,40),new Vector2(130,100),new Vector2(50,130)})
                {
                    var lot = new World.DistrictLotRecord {id=Guid.NewGuid().ToString("N"),district=3,center=at,
                        definition=new LotDefinition {name="Receiving lot",category="residential",wealth=LotWealth.Low,width=6,length=6,
                            objects=new List<LotObjectDefinition>{new(){assetId=building.id}}}};
                    receivers.Add(lot);view.PlacedLots.Add(lot);
                }
                Require(DestinationCount()==3,"Low-wealth developed residential lots not found");
                receivers[0].definition.wealth=LotWealth.High;
                Require(DestinationCount()==2,"High-wealth receiving lot accepted");
                receivers[0].definition.wealth=LotWealth.Low;receivers[0].definition.category="industrial";
                Require(DestinationCount()==2,"Industrial receiving lot accepted");
                receivers[0].definition.category="commercial";
                Require(DestinationCount()==3,"Low-wealth commercial receiving lot rejected");
                for(int retry=0;retry<24;retry++){Set(view,"nextEventTick",0f);Call(view,"TickRegionEvents");}
                Call(view,"MarkImmigrationSitesChanged");Call(view,"RefreshImmigrantAssignments");
                for(int retry=0;retry<24;retry++){Set(view,"nextEventTick",0f);Call(view,"TickRegionEvents");}
                Require(state.immigrants.All(p=>p.hasDestination&&!string.IsNullOrEmpty(p.destinationLotId)),"Developed lots not assigned");
                var allocations=state.immigrants.GroupBy(p=>p.destinationLotId).Select(g=>g.Count()).ToArray();
                Require(allocations.Length==3&&allocations.OrderBy(n=>n).SequenceEqual(new[]{1,1,10}),"Residential reservations must be unique; overflow must use commercial refuge");
                Require(view.ReservedResidentialParcels==2 && view.AvailableResidentialParcels==0 && view.UnhousedPopulation==120,
                    "Housing ledger did not separate reservations from actual housed population");
                int assignmentScans=view.ImmigrantAssignmentScans;
                for(int idle=0;idle<100;idle++){Set(view,"nextEventTick",0f);Call(view,"TickRegionEvents");}
                Require(view.ImmigrantAssignmentScans==assignmentScans,"Routing polled destinations without a world change");
                var temporary=state.immigrants.First(p=>p.destinationLotId==receivers[0].id);
                Require(temporary.awaitingHousing && JsonUtility.FromJson<World.EventImmigrant>(JsonUtility.ToJson(temporary)).awaitingHousing,
                    "Commercial refuge was incorrectly counted as housing or lost its pending housing state");
                temporary.settled=true;
                var temporaryActor=actors.Cast<World.PrototypeRegionView.Pedestrian>().First(a=>a.Immigrant==temporary);
                actors.Remove(temporaryActor);long housedBefore=state.settledPopulation;
                Call(view,"SyncRegionEvents");Call(view,"UpdateImmigrantHousingCounts");
                Require(state.immigrants.Contains(temporary)&&state.settledPopulation==housedBefore&&view.UnhousedPopulation==120,
                    "Commercial arrival was archived as housed instead of remaining in the pending ledger");
                actors.Add(temporaryActor);temporary.settled=false;
                info.lots=view.PlacedLots.ToArray();info.events=view.CaptureRegionEvents();
                state.immigrants[0].settled=true;
                World.SavedRegionStore.Write(new World.SavedRegionSnapshot{info=info,data=data},path);
                var movedIn=World.SavedRegionStore.Load(path).info.events.Single().immigrants[0];
                Require(movedIn.settled&&movedIn.destinationLotId==state.immigrants[0].destinationLotId,"Save/reopen lost moved-in state or assigned lot");
                var gone=(World.PrototypeRegionView.Pedestrian)actors[0];UnityEngine.Object.DestroyImmediate(gone.Body.gameObject);actors.RemoveAt(0);
                Set(view,"nextEventTick",0f);Call(view,"TickRegionEvents");
                Require(actors.Count==11&&town.development.population==120,"Moved-in passenger respawned or population changed");
                var pending=(World.PrototypeRegionView.Pedestrian)actors[0];
                pending.Immigrant.destinationLotId="deleted-receiver";
                Call(view,"BeginImmigrantMoveIn",pending);
                Require(!pending.Immigrant.settled&&!pending.Immigrant.hasDestination,"Passenger moved into a deleted lot");
                // Two connected street alternatives: the shorter U is120m, the long U180m.
                var roads=(Dictionary<World.DistrictRoadRecord,MeshFilter>)Get(view,"districtRoadMeshes");
                foreach(float z in new[]{60f,30f})
                {
                    var road=new World.DistrictRoadRecord{district=3,width=4,points=new[]{new Vector2(10,10),new Vector2(10,z),new Vector2(90,z),new Vector2(90,10)}};
                    var mesh=new GameObject("Disposable event street").AddComponent<MeshFilter>();mesh.transform.SetParent(root.transform);roads.Add(road,mesh);
                }
                var args=new object[]{new Vector2(10,10),new Vector2(90,10),state.immigrants[1],null};
                Require((bool)view.GetType().GetMethod("TryImmigrantRoute",Flags).Invoke(view,args),"Street route unavailable");
                var street=(Vector2[])args[3];float length=0;for(int i=1;i<street.Length;i++)length+=Vector2.Distance(street[i-1],street[i]);
                Require(Mathf.Abs(length-120)<.05f,"Did not choose the shortest connected street route / cut across grass instead");
                view.HarborVesselArrived(source, World.HarborVesselKind.Galleon);
                view.HarborVesselArrived(source, World.HarborVesselKind.Galleon);
                view.HarborVesselArrived(source, World.HarborVesselKind.Galleon);
                Require(state.occurrences == 4 && town.development.population == 480 && state.populationArrived == 480,
                    "Immigration did not continue beyond the former three-trip limit");
                for(int trip=0;trip<100;trip++)view.HarborVesselArrived(source,World.HarborVesselKind.Galleon);
                Require(state.occurrences==104&&town.development.population==12480&&state.populationArrived==12480,
                    "Ongoing population accounting stopped or multiplied incorrectly");
                Require(state.immigrants.Count<=96&&state.immigrants.Sum(p=>p.Population)+state.settledPopulation==12480,
                    "Ongoing crowd ledger grew unbounded or lost population when grouping representatives");
                view.PlacedLots.Remove(source); Call(view,"SyncRegionEvents");
                Require(state.cancelled, "Demolition did not cancel the source event");
                view.HarborVesselArrived(source, World.HarborVesselKind.Galleon);
                Require(state.occurrences==104&&state.populationArrived==12480, "Deleted source delivered passengers");
                source.id=Guid.NewGuid().ToString("N"); view.PlacedLots.Add(source); Call(view,"SyncRegionEvents");
                view.HarborVesselArrived(source, World.HarborVesselKind.Galleon);
                Require(view.RegionEvents.Count==2 && view.RegionEvents.Last().occurrences==1, "Replacement did not receive independent event progress");
                var replacement = view.RegionEvents.Last(); view.PlacedLots.Remove(source); Call(view,"SyncRegionEvents");
                Require(replacement.cancelled && replacement.occurrences==1 && replacement.immigrants.Count==12, "Deleting after first trip must cancel remaining waves and keep arrived people");
                Debug.Log("REGION EVENT CHECKS PASS: ongoing104 arrivals,120 people/12 representatives, gameplay pause/resume, persisted pause flag, bounded visual ledger and legacy defaults; JSON configuration; galleon-only trigger; 12 passengers; named town notice; wait without zones; move keeps identity; binary save/reopen without duplicate population; low-wealth residential/commercial routing; reject industrial/high wealth; continued arrivals; demolition/replacement lifecycle.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(td); if (File.Exists(path)) File.Delete(path); }
        }
    }
}
#endif

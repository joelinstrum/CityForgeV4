using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        internal readonly List<RegionEventState> RegionEvents = new();
        private float nextEventTick, eventNoticeUntil, nextArrivedGrowthRetry;
        private readonly Dictionary<string, float> immigrantPlanRetry = new();
        private readonly Dictionary<string,float> immigrantZoneWaitStarted = new();
        private readonly Dictionary<string,Dictionary<(Vector2,Vector2),bool>> immigrantRouteChecks=new();
        private (int Roads,int Lots,int Builds) immigrantCheckVersion;
        private readonly Dictionary<string,(Vector2 From,Vector2 To,FreightRoadGraph.Search Search)> immigrantRoadSearches=new();
        private RectTransform eventNotice;
        private Text eventNoticeText;

        private void SyncRegionEvents()
        {
            foreach (var state in RegionEvents)
            {
                if (!PlacedLots.Any(lot => lot.id == state.sourceLotId)) state.cancelled = true;
                state.moveIns ??= new List<ImmigrantSettlement>();
                // Older saves counted one person per visible actor. Never retroactively re-credit them.
                if (state.populationArrived == 0 && state.immigrants.Count > 0)
                    state.populationArrived = state.immigrants.Sum(p => p.Population);
                foreach (var person in state.immigrants.Where(p => p.settled && !p.awaitingHousing && !pedestrians.Any(a => a.Immigrant == p)).ToArray())
                {
                    state.settledPopulation += person.Population;
                    if (!string.IsNullOrEmpty(person.destinationLotId))
                    {
                        var moveIn = state.moveIns.FirstOrDefault(m => m.lotId == person.destinationLotId);
                        if (moveIn == null) state.moveIns.Add(moveIn = new ImmigrantSettlement { lotId = person.destinationLotId });
                        moveIn.population += person.Population;
                    }
                    immigrantRouteChecks.Remove(person.id);immigrantRoadSearches.Remove(person.id);
                    immigrantZoneWaitStarted.Remove(person.id);
                    state.immigrants.Remove(person);
                }
                state.moveIns.RemoveAll(m => !PlacedLots.Any(lot => lot.id == m.lotId));
            }
            foreach (var definition in RegionEventCatalog.Definitions)
                foreach (var lot in PlacedLots)
                    if (!string.IsNullOrEmpty(lot.id) && lot.definition.objects.Any(o => o.assetId == definition.sourceAssetId) &&
                        !RegionEvents.Any(s => s.eventId == definition.id && s.sourceLotId == lot.id))
                        RegionEvents.Add(new RegionEventState { eventId = definition.id, sourceLotId = lot.id });
        }

        internal RegionEventState[] CaptureRegionEvents()
        {
            SyncRegionEvents();
            CaptureImmigrantPositions();
            return RegionEvents.ToArray();
        }

        private void CaptureImmigrantPositions()
        {
            foreach (var actor in pedestrians)
                if (actor.Immigrant != null && actor.Body && !actor.EagleCarried)
                {
                    actor.Immigrant.position = new Vector2(actor.Body.position.x, actor.Body.position.z);
                    actor.Immigrant.district = DistrictAtWorld(actor.Body.position);
                }
        }

        internal void HarborVesselArrived(DistrictLotRecord port, HarborVesselKind kind)
        {
            if (ImmigrationPaused || kind != HarborVesselKind.Galleon || !PlacedLots.Contains(port)) return;
            SyncRegionEvents();
            foreach (var definition in RegionEventCatalog.Definitions.Where(e => e.trigger == "galleon-docked"))
            {
                var state = RegionEvents.FirstOrDefault(s => s.eventId == definition.id && s.sourceLotId == port.id);
                int year = TownYear(port.district);
                if (state == null || state.cancelled || (definition.maxOccurrences > 0 && state.occurrences >= definition.maxOccurrences) ||
                    year < definition.firstYear || year > definition.lastYear) continue;
                var catalog = LotAssetCatalog.Load();
                var assets = definition.actorAssetIds.Select(id => catalog ? catalog.Find(id) : null)
                    .Where(a => a != null && a.IsCharacter && a.prefab).ToArray();
                // Do not consume a wave when private character assets or a safe shore are unavailable.
                if (assets.Length == 0 || !TryImmigrantShore(port, out var shore)) continue;
                long population = (long)definition.immigrantsPerArrival * definition.populationPerRepresentative;
                var town = Towns.Where(t => DistrictAnchor(t.DistrictIndex(GridSide)) == DistrictAnchor(port.district))
                    .OrderBy(t => ((t.position - Vector2.one * .5f) * MapSide - port.center).sqrMagnitude).FirstOrDefault();
                if (town?.development != null && town.development.population > long.MaxValue - population) continue;
                if (state.occurrences == long.MaxValue || state.populationArrived > long.MaxValue - population) continue;
                state.occurrences++;
                state.populationArrived += population;
                for (int i = 0; i < definition.immigrantsPerArrival; i++)
                {
                    var position = shore;
                    for (int attempt = 0; attempt < 24; attempt++)
                    {
                        float angle = (i * 2.399963f + attempt * .61f);
                        var candidate = shore + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (1 + i * .3f);
                        if (InActiveDistrict(candidate) && FreightClear(shore, candidate, .2f)) { position = candidate; break; }
                    }
                    // One avatar may represent a larger waiting group when the visible crowd is full.
                    if (state.immigrants.Count >= 96)
                    {
                        var waiting = state.immigrants.Where(p => !p.settled || p.awaitingHousing).OrderBy(p => p.Population).FirstOrDefault();
                        if (waiting != null) { waiting.representedPopulation = waiting.Population + definition.populationPerRepresentative; continue; }
                    }
                    state.immigrants.Add(new EventImmigrant { id = Guid.NewGuid().ToString("N"),
                        assetId = assets[i % assets.Length].id, representedPopulation = definition.populationPerRepresentative, position = position, district = port.district,
                        arrivalPoint = shore, hasArrivalPoint = true });
                }

                if (town != null)
                {
                    town.development ??= new TownDevelopmentState();
                    town.development.population = checked(town.development.population + population);
                }
                ShowEventNotice(definition.arrivalMessage.Replace("{town}", town?.name ?? RegionName) +
                    $"\n{definition.name} · {population:N0} newcomers · Arrival {state.occurrences:N0}");
            }
            MarkImmigrationSitesChanged();
            RefreshImmigrantAssignments();
        }

        private bool TryImmigrantShore(DistrictLotRecord port, out Vector2 shore)
        {
            float depth = port.definition.WaterfrontSize.y * .5f;
            for (int ring = 0; ring < 16; ring++)
                for (int side = -1; side <= 1; side++)
                {
                    shore = WaterfrontWorldPoint(port, new Vector2(side * (port.definition.WaterfrontSize.x * .5f + 2), -depth - 2 - ring * 1.5f));
                    if (InActiveDistrict(shore) && FreightClear(shore, shore, .2f)) return true;
                }
            shore = default;
            return false;
        }

        private void TickRegionEvents()
        {
            if(ZoningSimulationPaused)return;
            if (eventNotice && Time.time >= eventNoticeUntil) eventNotice.gameObject.SetActive(false);
            if (DistrictLoading || IsSaving || Time.time < nextEventTick) return;
            nextEventTick = float.PositiveInfinity;
            if(immigrantAssignmentsDirty)RefreshImmigrantAssignments();
            if (SelectedDistrict < 0 || !ApprovedDistrict) return;
            var catalog = LotAssetCatalog.Load();
            if (!catalog) return;
            long planningMilliseconds = 0;
            foreach (var state in RegionEvents)
            {
                var definition = RegionEventCatalog.Definitions.FirstOrDefault(d => d.id == state.eventId);
                if (definition == null || !state.immigrants.Any(p=>!p.settled && ActiveDistrictMembers.Contains(p.district))) continue;

                foreach (var person in state.immigrants.Where(p => !p.settled && ActiveDistrictMembers.Contains(p.district)))
                {
                    var actor = pedestrians.FirstOrDefault(a => a.Immigrant == person);
                    if (actor == null)
                    {
                        if (pedestrians.Count(a => a.Immigrant != null) >= 96) continue;
                        var asset = catalog.Find(person.assetId);
                        if (asset == null || !asset.prefab) continue;
                        actor = CreatePedestrian(new DistrictPlacedActor { position = person.position }, asset);
                        actor.Immigrant = person;
                        actor.AssetId = asset.id;
                        actor.Route = Array.Empty<Vector2>();
                        actor.SectionAvailable = (a, b) => FreightClear(a, b, .15f);
                        SetPedestrianAnimation(actor, false);
                    }
                    if(person.arrivedAtZone || string.IsNullOrEmpty(person.destinationLotId))continue;
                    if(!immigrantAssignedSites.TryGetValue(person.destinationLotId,out var target))continue;
                    if(actor.Route.Length<2 && planningMilliseconds>=4){nextEventTick=Mathf.Min(nextEventTick,Time.time+.1f);continue;}
                    if(actor.Route.Length<2 && immigrantPlanRetry.TryGetValue(person.id,out var due) && Time.time<due)
                    {nextEventTick=Mathf.Min(nextEventTick,due);continue;}
                    var assigned=PlacedLots.FirstOrDefault(l=>l.id==person.destinationLotId);
                    if (!person.targetIsZone && assigned!=null && !LotPopulationReady(assigned)) continue;
                    if (!person.settled && actor.Route.Length < 2 && planningMilliseconds < 4 &&
                        (!immigrantPlanRetry.TryGetValue(person.id, out var retry) || Time.time >= retry))
                    {
                        var planning = System.Diagnostics.Stopwatch.StartNew();
                        PlanImmigrantRoute(actor, new[]{target});
                        planningMilliseconds += planning.ElapsedMilliseconds;
                        if (person.hasDestination) immigrantPlanRetry.Remove(person.id);
                        else immigrantPlanRetry[person.id] = Time.time + (planning.Elapsed.TotalMilliseconds>=8 ? .1f : 5f);
                        if(!person.hasDestination)nextEventTick=Mathf.Min(nextEventTick,immigrantPlanRetry[person.id]);
                    }
                }
            }
        }

        private readonly struct ImmigrantDestination
        {
            internal readonly DistrictLotRecord Lot;
            internal readonly Vector2 Point;
            internal readonly bool Zone;
            internal readonly bool Residential;
            internal readonly Vector2Int Cell;
            internal string Key => Lot?.id ?? $"zone:{Cell.x}:{Cell.y}";
            internal ImmigrantDestination(DistrictLotRecord lot, Vector2 point, bool zone=false, Vector2Int cell=default,bool residential=true)
            { Lot = lot; Point = point; Zone=zone; Cell=cell; Residential=residential; }
        }

        private IEnumerable<ImmigrantDestination> ImmigrantDestinations(RegionEventDefinition definition)
        {
            var catalog = LotAssetCatalog.Load();
            if (!catalog) yield break;
            foreach (var lot in PlacedLots.Where(l => InActiveDistrict(l.center)))
            {
                var d = lot.definition;
                var category = IsResidentialZone(d.zoningLayout.zone)?"residential":IsCommercialZone(d.zoningLayout.zone)?"commercial":
                    LotClassification.FindCategory(d.category)?.id;
                var wealth = d.zoningLayout.zone!=TownZone.None ? d.zoningLayout.wealth : d.wealth;
                if (wealth != LotWealth.Low || !definition.destinationCategories.Contains(category) ||
                    !d.objects.Any(o => catalog.Find(o.assetId) is {category:"Buildings",prefab:not null})) continue;
                // Stop just outside a front facade, rather than disappearing in the street
                // or at the edge of an oversized parcel. Footprint includes authored transforms.
                var front = d.objects.Select(o => (Item: o, Asset: catalog.Find(o.assetId)))
                    .Where(b => b.Asset is {category:"Buildings",prefab:not null})
                    .Select(b => LotRuntimeLoader.Footprint(b.Item, b.Asset)).OrderBy(bounds => bounds.yMin).First();
                var entrance = new DistrictLotFootprint(lot).WorldPoint(new Vector2(front.center.x, front.yMin - .8f));
                var building=d.objects.FirstOrDefault(o=>o.id==lot.growthBuildingId) ??
                    d.objects.First(o=>catalog.Find(o.assetId) is {category:"Buildings",prefab:not null});
                if(TryResidentialEntry(d,building,out var entry,out var forward))
                {
                    var footprint=new DistrictLotFootprint(lot);
                    for(float setback=.8f;setback<=5;setback+=.4f)
                    {
                        var candidate=footprint.WorldPoint(entry+forward*setback);
                        entrance=candidate;
                        if(FreightClear(candidate,candidate,.15f))break;
                    }
                }
                yield return new ImmigrantDestination(lot, entrance,residential:category=="residential");
            }
            foreach(var site in VacantImmigrantHomesites()) yield return site;
        }

        private IEnumerable<ImmigrantDestination> VacantImmigrantHomesites()
        {
            foreach(var lot in PendingPlayerLots.Where(l=>InActiveDistrict(l.center) &&
                IsResidentialZone(l.definition.zoningLayout.zone) && l.definition.zoningLayout.wealth==LotWealth.Low))
                yield return new ImmigrantDestination(lot,WaterfrontWorldPoint(lot,new Vector2(0,-lot.definition.Size.y*.5f-1)),true);
            if (!districtRoadMeshes.Any(r=>r.Value && r.Value.sharedMesh)) yield break;
            var occupied=OccupiedGrowthCells();
            var considered=new HashSet<Vector2Int>();
            (LotDefinition Layout,LotObjectDefinition[] Buildings)[] options=null;
            foreach(var cell in townZones.Where(c=>IsResidentialZone(c.Value)||IsCommercialZone(c.Value)))
            {
                var key=zoneLotAnchors.GetValueOrDefault(cell.Key,cell.Key);
                if(!considered.Add(key)||WealthAt(key)!=LotWealth.Low||occupied.Contains(key))continue;
                var facing=residentialFacing.GetValueOrDefault(key);
                if(facing==TownZoneFacing.None)continue;
                var requestedSize=ZonedLotSize(key);var size=requestedSize;
                bool residential=IsResidentialZone(cell.Value);
                if(size==Vector2Int.zero)
                {
                    // Auto paint has no parcel anchors. Partition it once into actual
                    // fitting layout footprints; never reserve individual paint cells.
                    options??=FoundersGrowthLayouts(out _);
                    size=options.Where(o=>o.Layout.zoningLayout.zone==cell.Value && o.Layout.zoningLayout.wealth==LotWealth.Low && o.Buildings.Length>0)
                        .Select(o=>GrowthLotCells(o.Layout,(8-(int)facing-(int)o.Layout.zoningLayout.frontage)%4))
                        .OrderByDescending(v=>v.x*v.y).FirstOrDefault(v=>HasGrowthZoning(new RectInt(key,v),cell.Value,facing,LotWealth.Low,Vector2Int.zero) &&
                            !ImmigrationCells(new RectInt(key,v)).Any(occupied.Contains));
                    if(size==Vector2Int.zero)continue;
                }
                var bounds=new RectInt(key,size);
                if(ImmigrationCells(bounds).Any(occupied.Contains))continue;
                var min=ZoneCorner(bounds.min);var max=ZoneCorner(bounds.max);var center=(min+max)*.5f;
                if(!InActiveDistrict(center)||PendingPlayerLots.Any(l=>new DistrictLotFootprint(l).Contains(center)))continue;
                var direction=facing switch {TownZoneFacing.East=>Vector2.right,TownZoneFacing.North=>Vector2.up,
                    TownZoneFacing.West=>Vector2.left,_=>Vector2.down};
                float reach=(facing is TownZoneFacing.East or TownZoneFacing.West?max.x-min.x:max.y-min.y)*.5f;
                var arrival=center+direction*(reach+1);
                if(!OnImmigrationStreet(arrival))continue;
                foreach(var at in bounds.allPositionsWithin)occupied.Add(at);
                yield return new ImmigrantDestination(null,arrival,true,key,residential);
            }
        }

        private void PlanImmigrantRoute(Pedestrian actor, IReadOnlyList<ImmigrantDestination> targets)
        {
            var person = actor.Immigrant;
            var from = new Vector2(actor.Body.position.x, actor.Body.position.z);
            person.hasDestination = false;
            var preferred=person.destinationLotId;
            person.destinationLotId = null;
            // Balance reservations and completed move-ins, then shuffle nearby alternatives.
            // Randomness chooses a home/workplace; it never randomizes the walking route.
            var loads = RegionEvents.SelectMany(s => s.immigrants).Where(p => p != person &&
                !string.IsNullOrEmpty(p.destinationLotId) && (p.hasDestination || p.settled))
                .GroupBy(p => p.destinationLotId).ToDictionary(g => g.Key, g => g.Sum(p => p.Population));
            foreach (var settled in RegionEvents.SelectMany(s => s.moveIns))
                loads[settled.lotId] = loads.GetValueOrDefault(settled.lotId) + settled.population;
            bool reservedHome=targets.Any(t=>!t.Zone&&t.Key==preferred);
            var available=reservedHome?targets.Where(t=>t.Key==preferred):targets;
            var nearby = available.OrderBy(t=>t.Key==preferred?0:1).ThenBy(t => loads.GetValueOrDefault(t.Key))
                .ThenBy(t => (t.Point - from).sqrMagnitude).Take(16).ToArray();
            var routeBudget=System.Diagnostics.Stopwatch.StartNew();
            foreach (var target in nearby.OrderBy(t=>t.Key==preferred?0:1).ThenBy(t => loads.GetValueOrDefault(t.Key))
                .ThenBy(t => ImmigrantChoiceHash(person.id + t.Key)))
            {
                if(Application.isPlaying && routeBudget.Elapsed.TotalMilliseconds>=8)break;
                if (!TryImmigrantRouteWithinBudget(from, target.Point, person, out var route,routeBudget))
                {
                    if(Application.isPlaying && routeBudget.Elapsed.TotalMilliseconds>=8)
                    {person.destinationLotId=target.Key;person.targetIsZone=target.Zone;break;}
                    continue;
                }
                immigrantRouteChecks.Remove(person.id);immigrantRoadSearches.Remove(person.id);
                person.destination = target.Point;
                person.destinationLotId = target.Key;
                person.targetIsZone=target.Zone; person.destinationZoneCell=target.Cell; person.arrivedAtZone=false;
                person.hasDestination = true;
                actor.Route = route;
                actor.Next = 1; actor.Direction = 1; actor.Closed = false;
                RememberPedestrianRoute(actor);
                SetPedestrianAnimation(actor, true);
                return;
            }
            if(!string.IsNullOrEmpty(preferred))person.destinationLotId=preferred;
            actor.Route = Array.Empty<Vector2>();
            SetPedestrianAnimation(actor, false);
        }

        private static uint ImmigrantChoiceHash(string text)
        {
            uint hash = 2166136261;
            foreach (char c in text) hash = unchecked((hash ^ c) * 16777619);
            return hash;
        }

        private bool TryImmigrantRoute(Vector2 from, Vector2 target, EventImmigrant person, out Vector2[] route) =>
            TryImmigrantRouteWithinBudget(from,target,person,out route,null);

        private bool TryImmigrantRouteWithinBudget(Vector2 from, Vector2 target, EventImmigrant person, out Vector2[] route, System.Diagnostics.Stopwatch budget)
        {
            bool Expired()=>Application.isPlaying && budget!=null && budget.Elapsed.TotalMilliseconds>=8;
            var checkVersion=(FreightRoadRevision,PlacedLots.Count,LotPresentationBuilds);
            if(checkVersion!=immigrantCheckVersion){immigrantRouteChecks.Clear();immigrantRoadSearches.Clear();immigrantCheckVersion=checkVersion;}
            if(!immigrantRouteChecks.TryGetValue(person.id,out var checks))
                immigrantRouteChecks[person.id]=checks=new();
            bool Clear(Vector2 a,Vector2 b)
            {
                // Retain completed short sections across budget-limited retries. A long
                // approach or ground search must not restart from zero every time.
                int count=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/2));
                for(int i=0;i<count;i++)
                {
                    if(Expired())return false;
                    var start=Vector2.Lerp(a,b,(float)i/count);var end=Vector2.Lerp(a,b,(float)(i+1)/count);
                    var key=(start,end);
                    if(!checks.TryGetValue(key,out var clear))
                    {
                        clear=FreightClear(start,end,.15f);
                        if(checks.Count<8192)checks[key]=clear;
                    }
                    if(!clear)return false;
                }
                return true;
            }
            var roads = pedestrianRoutes.Select(r => (r.Points, r.Closed))
                .Concat(districtRoadMeshes.Keys.Select(r => (RoadCenterline(r).ToArray(), r.closed)));
            // Dijkstra chooses the shortest connected street/sidewalk path. Use safe
            // ground access only when there is no usable connected network to this lot.
            if(!immigrantRoadSearches.TryGetValue(person.id,out var roadSearch) || roadSearch.From!=from || roadSearch.To!=target)
            {
                roadSearch=(from,target,new FreightRoadGraph.Search(roads.ToArray(),from,target,24,16));
                immigrantRoadSearches[person.id]=roadSearch;
            }
            while(!roadSearch.Search.Complete && !Expired())roadSearch.Search.Step();
            route=roadSearch.Search.Route;
            if(!roadSearch.Search.Complete)return false;
            if (route.Length>1)
            {
                route = route.Concat(new[] { target }).ToArray();
                var roadPath = route;
                if (Enumerable.Range(1, roadPath.Length - 1).All(i => Clear(roadPath[i - 1], roadPath[i]))) return true;
            }
            if (Clear(from, target)) { route = new[] { from, target }; return true; }
            if (person.hasArrivalPoint && Clear(from, person.arrivalPoint) &&
                Clear(person.arrivalPoint, target))
            {
                route = new[] { from, person.arrivalPoint, target };
                return true;
            }
            return BoundedGroundRoute.TryFind(from, target, Clear, out route);
        }

        private void BeginImmigrantMoveIn(Pedestrian actor)
        {
            var person = actor.Immigrant;
            var state = RegionEvents.FirstOrDefault(s => s.immigrants.Contains(person));
            var rule = state == null ? null : RegionEventCatalog.Definitions.FirstOrDefault(d => d.id == state.eventId);
            if (rule == null || !immigrantAssignedSites.TryGetValue(person.destinationLotId??"",out var receiving) ||
                (receiving.Point-person.destination).sqrMagnitude>=.01f)
            {
                // A lot can disappear or move between the last planning tick and arrival.
                actor.Route = Array.Empty<Vector2>();
                person.hasDestination = false; person.destinationLotId = null;
                SetPedestrianAnimation(actor, false);
                nextEventTick = 0;
                return;
            }
            if(person.targetIsZone)
            {
                person.arrivedAtZone=true;
                immigrantZoneWaitStarted[person.id]=Time.time;
                actor.Route=Array.Empty<Vector2>();SetPedestrianAnimation(actor,false);
                if(receiving.Residential)QueueLotGrowth();
                UpdateImmigrantHousingCounts();
                return;
            }
            person.awaitingHousing=!receiving.Residential;
            actor.Immigrant.settled = true;
            UpdateImmigrantHousingCounts();
            if(!DeveloperMode)QueueLotGrowth();
            actor.Route = Array.Empty<Vector2>();
            SetPedestrianAnimation(actor, false);
            StartCoroutine(FadeImmigrantMoveIn(actor));
        }

        private IEnumerator FadeImmigrantMoveIn(Pedestrian actor)
        {
            // Clones belong only to this departing avatar; catalog/shared materials stay intact.
            var materials = new List<(Material Material, string Color, Color Tint)>();
            if (actor.Body)
                foreach (var renderer in actor.Body.GetComponentsInChildren<Renderer>(true))
                {
                    var copies = renderer.sharedMaterials.Select(m => m ? Own(new Material(m)) : null).ToArray();
                    foreach (var material in copies)
                    {
                        if (!material) continue;
                        string color = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                        var tint = material.HasProperty(color) ? material.GetColor(color) : Color.white;
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0);
                        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                        material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.SetShaderPassEnabled("ShadowCaster", false);
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                        materials.Add((material, color, tint));
                    }
                    renderer.sharedMaterials = copies;
                }
            try
            {
                const float duration = 1.2f;
                for (float elapsed = 0; elapsed < duration && actor.Body; elapsed += Time.deltaTime)
                {
                    float alpha = 1 - Mathf.SmoothStep(0, 1, elapsed / duration);
                    foreach (var entry in materials)
                    {
                        var tint = entry.Tint; tint.a *= alpha;
                        if (entry.Material.HasProperty(entry.Color)) entry.Material.SetColor(entry.Color, tint);
                    }
                    yield return null;
                }
            }
            finally
            {
                if (actor.Body) { actor.Body.gameObject.SetActive(false); Destroy(actor.Body.gameObject); }
                pedestrians.Remove(actor);
                SyncRegionEvents();
                UpdateImmigrantHousingCounts();
                if(actor.Immigrant.awaitingHousing && AvailableResidentialParcels>0)MarkImmigrationSitesChanged();
                foreach (var entry in materials) if (entry.Material) Destroy(entry.Material);
            }
        }

        private void ShowEventNotice(string message)
        {
            var hud = transform.Find("RegionHud");
            if (!hud) return;
            if (!eventNotice)
            {
                eventNotice = FounderRect(hud, "Regional event arrival", Vector2.zero, new Vector2(700, 84));
                eventNotice.anchorMin = eventNotice.anchorMax = eventNotice.pivot = new Vector2(.5f, 1);
                eventNotice.anchoredPosition = new Vector2(0, -32);
                StyleTownPanel(eventNotice, TownPanelColor);
                eventNotice.GetComponent<Image>().raycastTarget = false;
                eventNoticeText = FounderText(eventNotice, "", Vector2.zero, new Vector2(670, 76), 21);
                eventNoticeText.color = TownGold;
                eventNoticeText.resizeTextForBestFit = true;
                eventNoticeText.resizeTextMinSize = 14;
                eventNoticeText.resizeTextMaxSize = 21;
            }
            eventNoticeText.text = message;
            eventNotice.gameObject.SetActive(true);
            eventNotice.SetAsLastSibling();
            eventNoticeUntil = Time.time + 14;
        }
    }
}

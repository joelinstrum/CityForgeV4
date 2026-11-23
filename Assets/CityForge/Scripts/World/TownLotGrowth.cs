using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private const float LotGrowthDelay = 3f;
        private float nextLotGrowth = float.PositiveInfinity;
        private int growthDistrict = -1;
        private bool fullGrowthCheckQueued;
        private readonly HashSet<Vector2Int> localGrowthCells = new();
        private (LotDefinition Layout, LotObjectDefinition[] Buildings)[] FoundersGrowthLayouts(out string problem)
        {
            var options = new System.Collections.Generic.List<(LotDefinition Layout, LotObjectDefinition[] Buildings)>();
            problem = "Save a Founders-era residential lot in the Zoning Lots library.";
            try
            {
                var catalog = LotAssetCatalog.Load();
                var layouts = LotLibrary.Local.List(LotType.Zoning).Select(entry => entry.Lot)
                    .Where(lot => (string.IsNullOrEmpty(lot.firstEraId) || lot.firstEraId == "founders") && SupportsLotGrowth(lot.zoningLayout.zone) &&
                        !lot.IsWaterfront && ResidentialWealthPolicy.Allows(lot.zoningLayout.zone, lot.zoningLayout.wealth, ResidentialGround.LotEra(lot)))
                    .OrderByDescending(lot => lot.width * lot.length)
                    .ThenBy(lot => lot.name, StringComparer.Ordinal)
                    .ThenBy(lot => lot.id, StringComparer.Ordinal);
                foreach (var layout in layouts)
                {
                    var invalid = !layout.zoningLayout.Fits(layout) ? "Check its growable layout."
                        : layout.objects.Count >= 512 ? "Leave room for a house in its object list."
                        : LotAssetProblem(layout, catalog);
                    if (invalid != null)
                    {
                        problem = $"{layout.name}: {invalid}";
                        continue;
                    }

                    var buildings = LotGrowableBuildings.Candidates(layout, catalog)
                        .Where(building => catalog.Find(building.assetId).eraId == "founders").ToArray();
                    bool propFiller = layout.width == 1 && layout.length == 1 && layout.objects.Count > 0;
                    if (buildings.Length == 0 && !propFiller)
                        problem = $"{layout.name}: No matching building fits the building slot.";
                    else
                        options.Add((layout, buildings));
                }
                if (!options.Any(option => option.Layout.zoningLayout.zone == TownZone.Commercial1 &&
                    option.Layout.zoningLayout.wealth == LotWealth.Low) &&
                    catalog && catalog.Find("v3-building-plymouth-store") is { } shop && shop.prefab)
                {
                    int tiles = Mathf.Max(1, Mathf.CeilToInt((Mathf.Max(shop.footprint.x, shop.footprint.y) + 1f) /
                        DistrictGridCellMeters));
                    var layout = new LotDefinition
                    {
                        id = "7f1780084b9548d7a34037c6094f20bc",
                        name = "Founders Shops", type = LotType.Zoning, firstEraId = "founders",
                        category = "commercial", wealth = LotWealth.Low, width = tiles, length = tiles,
                        zoningLayout = new LotZoningLayout
                        {
                            zone = TownZone.Commercial1, wealth = LotWealth.Low, buildingSize = Vector2.one *
                                Mathf.Max(shop.footprint.x, shop.footprint.y)
                        }
                    };
                    var shops = LotGrowableBuildings.Candidates(layout, catalog);
                    if (shops.Length > 0) options.Add((layout, shops));
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                problem = "Could not read the growable lot library.";
                Debug.LogWarning($"{problem} {error.Message}");
            }
            return options.ToArray();
        }

        private static Vector2Int GrowthFrontageSize(LotDefinition layout) =>
            GrowthLotCells(layout, layout.zoningLayout.frontage is LotFrontage.West or LotFrontage.East ? 1 : 0);

        private static string GrowthSpaceHint(LotDefinition layout)
        {
            var size = GrowthFrontageSize(layout);
            return $"{layout.name} needs {size.x} cells along the road and {size.y} cells deep.";
        }

        private static Vector2Int GrowthLotCells(LotDefinition layout, int rotation) =>
            Vector2Int.RoundToInt(layout.SizeForRotation(rotation) / DistrictGridCellMeters);

        private void QueueLotGrowth()
        {
            immigrantRouteChecks.Clear();immigrantRoadSearches.Clear();
            fullGrowthCheckQueued = true;
            localGrowthCells.Clear();
            nextLotGrowth = Time.time + LotGrowthDelay;
        }

        private void QueueZoningGrowth(RectInt edited)
        {
            MarkImmigrationSitesChanged();
            // A lot grows from its minimum corner. Include corners outside the edit
            // whose footprints could extend into it, without scanning other zones.
            int reach = LotDefinition.MaxTiles - 1;
            int side = Mathf.RoundToInt(MapSide / DistrictGridCellMeters);
            for (int z = Mathf.Max(0, edited.yMin - reach); z < Mathf.Min(side, edited.yMax); z++)
            for (int x = Mathf.Max(0, edited.xMin - reach); x < Mathf.Min(side, edited.xMax); x++)
            {
                var cell = new Vector2Int(x, z);
                if (townZones.TryGetValue(cell, out var zone) && SupportsLotGrowth(zone))
                    localGrowthCells.Add(cell);
            }
            nextLotGrowth = Time.time + LotGrowthDelay;
        }

        private void QueueDemolitionGrowth(IEnumerable<DistrictLotRecord> removed)
        {
            removed=removed.ToArray();
            ReleaseImmigrantHomes(removed);
            MarkImmigrationSitesChanged();
            foreach (var lot in removed)
            {
                var footprint = new DistrictLotFootprint(lot);
                var half = lot.definition.Size * .5f;
                var a = footprint.WorldPoint(half) - lot.center;
                var b = footprint.WorldPoint(new Vector2(half.x, -half.y)) - lot.center;
                var extent = Vector2.Max(new Vector2(Mathf.Abs(a.x), Mathf.Abs(a.y)),
                    new Vector2(Mathf.Abs(b.x), Mathf.Abs(b.y)));
                var origin = ZoneCorner(Vector2Int.zero);
                var min = Vector2Int.FloorToInt((lot.center - extent - origin) / DistrictGridCellMeters);
                var max = Vector2Int.CeilToInt((lot.center + extent - origin) / DistrictGridCellMeters);
                for (int z = min.y; z < max.y; z++)
                for (int x = min.x; x < max.x; x++)
                {
                    var cell = new Vector2Int(x, z);
                    if (townZones.TryGetValue(cell, out var zone) && SupportsLotGrowth(zone) &&
                        footprint.Contains(ZoneCorner(cell) + Vector2.one * (DistrictGridCellMeters * .5f)))
                        localGrowthCells.Add(cell);
                }
            }
            if (localGrowthCells.Count > 0)
                nextLotGrowth = Time.time + LotGrowthDelay;
        }

        private void UpdateLotGrowth()
        {
            if(ZoningSimulationPaused)return;
            if (growthDistrict != SelectedDistrict)
            {
                growthDistrict = SelectedDistrict;
                QueueLotGrowth();
            }
            if (!CanBuildTown || IsSaving || movingDistrictLot != null || detonateLastPoint.HasValue ||
                Time.time < nextLotGrowth) return;
            nextLotGrowth = float.PositiveInfinity;
            GrowPendingPlayerLots();
            // No arrived residents means no possible Player home construction.
            // Avoid loading layouts and walking all parcel combinations at population zero.
            if(!DeveloperMode && !PlayerPopulationPresent)
            {fullGrowthCheckQueued=false;localGrowthCells.Clear();return;}
            IEnumerable<Vector2Int> pending = fullGrowthCheckQueued ? townZones.Where(pair => SupportsLotGrowth(pair.Value)).Select(pair => pair.Key) : localGrowthCells;
            if(!DeveloperMode)
            {
                var arrivals=WaitingResidents.Where(p=>p.destinationLotId!=null && p.destinationLotId.StartsWith("zone:"))
                    .Select(p=>p.destinationZoneCell).ToArray();
                pending=pending.Where(cell=>townZones.TryGetValue(cell,out var zone) &&
                    (IsResidentialZone(zone) ? arrivals.Any(at=>at.x>=cell.x && at.y>=cell.y &&
                        at.x-cell.x<LotDefinition.MaxTiles && at.y-cell.y<LotDefinition.MaxTiles) :
                        PlayerGrowthAllowed(default,zone,WealthAt(cell))));
            }
            var cellsToCheck = pending.OrderBy(cell => cell.y).ThenBy(cell => cell.x).ToArray();
            fullGrowthCheckQueued = false;
            localGrowthCells.Clear();
            if (cellsToCheck.Length == 0) return;
            var options = FoundersGrowthLayouts(out var problem);
            if (options.Length == 0)
            {
                ShowGrowthStatus(problem);
                return;
            }

            // Try deeper homes before fillers so a one-cell lot cannot take a house site.
            var layoutsByWealth = options.Where(option => option.Layout.width * option.Layout.length > 1)
                .GroupBy(option => (option.Layout.zoningLayout.zone, option.Layout.zoningLayout.wealth))
                .ToDictionary(group => group.Key, group => group.GroupBy(option => GrowthFrontageSize(option.Layout))
                    .OrderByDescending(size => size.Key.y).ThenByDescending(size => size.Key.x)
                    .Select(size => size.ToArray()).ToArray());
            var fillersByWealth = options.Where(option => option.Layout.width == 1 && option.Layout.length == 1)
                .GroupBy(option => (option.Layout.zoningLayout.zone, option.Layout.zoningLayout.wealth))
                .ToDictionary(group => group.Key, group => new[] { group.ToArray() });
            var passes = fillersByWealth.Count == 0 ? new[] { layoutsByWealth } :
                new[] { layoutsByWealth, fillersByWealth };
            var frontageChecks = new Dictionary<(RectInt cells, TownZoneFacing facing, TownZone zone, int maxDepth), bool>();
            var water = DistrictLotWater();
            var grownLots = new List<DistrictLotRecord>();
            var grownNames = new System.Collections.Generic.HashSet<string>();
            // Developed zoning remains in the persistent grid. Do not retry growth from
            // cells already hidden by a placed lot; their offset candidates create false overlap errors.
            var occupiedGrowthCells = OccupiedGrowthCells();
            bool hasGrowableZoning = false;
            string blocked = null;
            foreach (var pass in passes)
            foreach (var cell in cellsToCheck)
            {
                if (!townZones.TryGetValue(cell, out var zone) || !SupportsLotGrowth(zone) ||
                    !InActiveDistrict(ZoneCorner(cell) +
                    Vector2.one * (DistrictGridCellMeters * .5f))) continue;
                if(occupiedGrowthCells.Contains(cell))continue;
                hasGrowableZoning = true;
                var facing = residentialFacing.GetValueOrDefault(cell);
                if (facing == TownZoneFacing.None && IsResidentialZone(zone)) continue;
                var wealth = WealthAt(cell);
                var key = (zone, wealth);
                if (!pass.TryGetValue(key, out var sizeGroups))
                {
                    if (!layoutsByWealth.ContainsKey(key) && !fillersByWealth.ContainsKey(key))
                        blocked ??= MissingGrowthContent(zone,wealth);
                    continue;
                }
                var requiredSize = ZonedLotSize(cell);
                if (requiredSize != Vector2Int.zero && !options.Any(option =>
                    option.Layout.zoningLayout.zone == zone && option.Layout.zoningLayout.wealth == wealth && GrowthFrontageSize(option.Layout) == requiredSize))
                {
                    blocked ??= $"This parcel requires a {requiredSize.x} × {requiredSize.y} zoning lot. Save a matching layout for this zone and wealth; smaller lots will not fill it.";
                    continue;
                }
                bool placed = false;
                var optionsForCell = GrowthOptions(sizeGroups).ToArray();
                for (int cornersFirst = 1; cornersFirst >= 0 && !placed; cornersFirst--)
                for (int facingOffset = 0; facingOffset < 4 && !placed; facingOffset++)
                foreach (var (layout, buildings) in optionsForCell)
                {
                    if ((layout.SideStreetTurn != 0) != (cornersFirst == 1)) continue;
                    var roadFacing = (TownZoneFacing)(1 + ((int)(facing == TownZoneFacing.None ? TownZoneFacing.South : facing) - 1 + facingOffset) % 4);
                    var requestedSize = ZonedLotSize(cell);
                    if (requestedSize != Vector2Int.zero && requestedSize != GrowthFrontageSize(layout)) continue;
                    int rotation = (8 - (int)roadFacing - (int)layout.zoningLayout.frontage) % 4;
                    var size = GrowthLotCells(layout, rotation);
                    var cells = new RectInt(cell, size);
                    if (!HasGrowthZoning(cells, zone, facing, wealth, requestedSize)) continue;
                    if ((!DeveloperMode && buildings.Length==0) || !PlayerGrowthAllowed(cells,zone,wealth))
                    { blocked ??= "Waiting for residents to reach a low-wealth homesite beside a road."; continue; }
                    int maxDepth = GrowthFrontageSize(layout).y;

                    var center = (ZoneCorner(cells.min) + ZoneCorner(cells.max)) * .5f;
                    var record = new DistrictLotRecord
                    {
                        center = center, rotation = rotation, definition = layout,
                        district = DistrictAtWorld(new Vector3(center.x, 0f, center.y))
                    };
                    if (PlacedLots.Any(lot => lot.center == center)) continue;
                    var siteProblem = InCampClearing(center, layout.Size.magnitude * .5f)
                        ? "Keep the lot clear of camps." : LotSiteProblem(record, water, growing: true);
                    if (siteProblem != null)
                    {
                        blocked ??= siteProblem;
                        continue;
                    }
                    var frontageKey = (cells, roadFacing, zone, maxDepth);
                    if (!frontageChecks.TryGetValue(frontageKey, out bool hasFrontage))
                    {
                        hasFrontage = ResidentialFrontageProblem(cells, maxDepth, out _,
                            requiredFacing: roadFacing, commercial: IsCommercialZone(zone), allowRoundedLotCorners: true) == null;
                        frontageChecks.Add(frontageKey, hasFrontage);
                    }
                    if (!hasFrontage)
                    {
                        blocked ??= IsCommercialZone(zone) ? "Keep the full shop frontage beside a street." :
                            "Keep the full lot footprint beside a road.";
                        continue;
                    }

                    if (layout.SideStreetTurn != 0)
                    {
                        var sideFacing = (TownZoneFacing)(1 + ((int)roadFacing - 1 - layout.SideStreetTurn + 4) % 4);
                        var sideKey = (cells, sideFacing, zone, LotDefinition.MaxTiles);
                        if (!frontageChecks.TryGetValue(sideKey, out bool hasSideStreet))
                        {
                            hasSideStreet = ResidentialFrontageProblem(cells, LotDefinition.MaxTiles, out _,
                                requiredFacing: sideFacing, commercial: IsCommercialZone(zone), allowRoundedLotCorners: true) == null;
                            frontageChecks.Add(sideKey, hasSideStreet);
                        }
                        if (!hasSideStreet)
                        {
                            blocked ??= "This corner lot needs a street along its side frontage.";
                            continue;
                        }
                    }

                    if (buildings.Length > 0)
                    {
                        var preferredBuilding = ChooseGrowthBuilding(record, roadFacing, buildings);
                        LotObjectDefinition building=null;
                        foreach(var candidate in new[]{preferredBuilding}.Concat(buildings.Where(b=>b.assetId!=preferredBuilding.assetId)))
                        {
                            record.definition=LotGrowableBuildings.Materialize(layout,candidate,layout.id+":"+record.district+":"+record.center);
                            if(GrowableBuildingsOverlapRoad(record))continue;
                            building=candidate;break;
                        }
                        if(building==null){blocked??="Keep buildings clear of the road surface.";continue;}
                        record.growthBuildingId = record.definition.objects.Last().id;
                        if (IsResidentialZone(zone)) AddResidentialEntryPath(record, building);
                    }
                    else
                        record.definition = LotDefinition.Parse(JsonUtility.ToJson(layout));
                    if(GrowableBuildingsOverlapRoad(record))
                    {
                        blocked ??= "Keep buildings clear of the road surface.";
                        continue;
                    }
                    PrepareLotGrading(record);
                    HoldLotPopulation(record);
                    record.id = Guid.NewGuid().ToString("N");
                    LotPropStyles.Apply(record.definition, record.id, LotAssetCatalog.Load());
                    PlacedLots.Add(record);
                    BindResidentsToBuilding(record,cells);
                    grownLots.Add(record);
                    grownNames.Add(layout.name);
                    placed = true;
                    break;
                }
            }
            if (grownNames.Count > 0)
            {
                ClearDevelopedSites(grownLots);
                PresentDistrictLots(grownLots);
                foreach (var grown in grownLots) StartCoroutine(ConstructZonedLot(grown));
                RefreshZoneOverlay();
                RefreshDirtRoadEndFades();
                if (ApprovedDistrict)
                {
                    ApprovedDistrict.RefreshHarborStones();
                    ApprovedDistrict.Geography?.RefreshSettlements();
                }
                ShowGrowthStatus($"{string.Join(", ", grownNames.OrderBy(name => name, StringComparer.Ordinal))} grew. Save the region to keep it.");
            }
            else if (hasGrowableZoning)
                ShowGrowthStatus(blocked ?? GrowthSpaceHint(options[^1].Layout));
        }

        private System.Collections.IEnumerator ConstructZonedLot(DistrictLotRecord record)
        {
            if (!districtLotRoots.TryGetValue(record, out var finished) || !finished) {ReleaseLotPopulation(record);yield break;}
            var catalog = LotAssetCatalog.Load();
            var building = record.definition.objects.FirstOrDefault(item => item.id == record.growthBuildingId)
                ?? record.definition.objects.FirstOrDefault(item => catalog?.Find(item.assetId)?.category == "Buildings");
            if (building == null) {ReleaseLotPopulation(record);yield break;}
            var asset = catalog.Find(building.assetId);
            if (asset == null || !asset.prefab) {ReleaseLotPopulation(record);yield break;}

            var site = new GameObject("Lot under construction").transform;
            site.SetParent(lotInstances, false);
            site.SetPositionAndRotation(finished.position, finished.rotation);
            var wood = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            wood.SetColor("_BaseColor", new Color(.47f, .29f, .13f));
            var dirt = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            dirt.SetColor("_BaseColor", new Color(.66f, .50f, .33f));
            dirt.SetTexture("_BaseMap", Resources.Load<Texture2D>("Lots/PathMaterials/DirtRoadEarth"));
            var frame = new GameObject("Rising timber frame").transform;
            frame.SetParent(site, false);

            // Read the already-instantiated building, including its final fit, scale and LOD.
            lotBuildingViews.TryGetValue((record,building.id),out var model);
            var renderers = record.definition.zoningLayout.buildingRow ? record.definition.objects
                .Where(item=>item.generatedRowBuilding).Select(item=>lotBuildingViews.GetValueOrDefault((record,item.id)))
                .Where(child=>child).SelectMany(child=>child.GetComponentsInChildren<Renderer>(true)).ToArray() :
                model ? model.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            var visible = renderers.Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy).ToArray();
            if (visible.Length == 0) visible = renderers;
            if (visible.Length == 0)
            {
                Destroy(site.gameObject);
                Destroy(wood);
                Destroy(dirt);
                ReleaseLotPopulation(record);
                yield break;
            }
            float baseY = float.PositiveInfinity;
            float roofY = float.NegativeInfinity;
            foreach (var renderer in visible)
            {
                baseY = Mathf.Min(baseY, renderer.bounds.min.y);
                roofY = Mathf.Max(roofY, renderer.bounds.max.y);
            }
            float measuredHeight = roofY - baseY;
            var min = Vector3.zero;
            var max = Vector3.zero;
            var footprint = LotRuntimeLoader.Footprint(building, asset);
            if(record.definition.zoningLayout.buildingRow)foreach(var rowItem in record.definition.objects.Where(item=>item.generatedRowBuilding)){
                var rowBounds=LotRuntimeLoader.Footprint(rowItem,catalog.Find(rowItem.assetId));
                footprint=Rect.MinMaxRect(Mathf.Min(footprint.xMin,rowBounds.xMin),Mathf.Min(footprint.yMin,rowBounds.yMin),Mathf.Max(footprint.xMax,rowBounds.xMax),Mathf.Max(footprint.yMax,rowBounds.yMax));
            }
            min.x = footprint.xMin;
            min.z = footprint.yMin;
            max.x = footprint.xMax;
            max.z = footprint.yMax;
            float height = measuredHeight;
            frame.localPosition = new Vector3(0f, baseY-site.position.y, 0f);
            var pieces = new List<Transform>();
            var groundMeshes = new List<Mesh>();
            var veil = ConstructionDustVeil.Create(transform, record, baseY, measuredHeight);
            try
            {
                revealingLots.Add(record);
                finished.gameObject.SetActive(false);
                var size = record.definition.Size;
                for (int z = 0; z < record.definition.length; z++)
                for (int x = 0; x < record.definition.width; x++)
                {
                    var vertices = new List<Vector3>();
                    var uv = new List<Vector2>();
                    var triangles = new List<int>();
                    const int divisions = 4;
                    for (int tz = 0; tz <= divisions; tz++)
                    for (int tx = 0; tx <= divisions; tx++)
                    {
                        var local = new Vector3((x + (float)tx / divisions) * 4f - size.x * .5f,
                            0f, (z + (float)tz / divisions) * 4f - size.y * .5f);
                        var world = site.TransformPoint(local);
                        local.y = LotHeight(new Vector2(world.x, world.z)) + .12f;
                        vertices.Add(local);
                        uv.Add(new Vector2((float)tx / divisions, (float)tz / divisions));
                        if (tx == divisions || tz == divisions) continue;
                        int v = tz * (divisions + 1) + tx;
                        triangles.AddRange(new[] { v, v + divisions + 1, v + 1,
                            v + 1, v + divisions + 1, v + divisions + 2 });
                    }
                    var tile = new GameObject("Construction dirt tile", typeof(MeshFilter), typeof(MeshRenderer));
                    tile.transform.SetParent(site, false);
                    var mesh = new Mesh();
                    mesh.SetVertices(vertices);
                    mesh.SetUVs(0, uv);
                    mesh.SetTriangles(triangles, 0);
                    mesh.RecalculateNormals();
                    tile.GetComponent<MeshFilter>().sharedMesh = mesh;
                    tile.GetComponent<Renderer>().sharedMaterial = dirt;
                    groundMeshes.Add(mesh);
                }

                int levels = Mathf.Max(1, Mathf.CeilToInt(height / 2.7f));
                int across = Mathf.Max(1, Mathf.CeilToInt(footprint.width / 2f));
                int deep = Mathf.Max(1, Mathf.CeilToInt(footprint.height / 2f));
                for (int level = 0; level < levels; level++)
                {
                    float bottom = height * level / levels;
                    float top = height * (level + 1) / levels;
                    for (int x = 0; x <= across; x++)
                    {
                        float at = Mathf.Lerp(min.x, max.x, (float)x / across);
                        Post(at, min.z, bottom, top);
                        Post(at, max.z, bottom, top);
                    }
                    for (int z = 1; z < deep; z++)
                    {
                        float at = Mathf.Lerp(min.z, max.z, (float)z / deep);
                        Post(min.x, at, bottom, top);
                        Post(max.x, at, bottom, top);
                    }
                    AddFrame(new Vector3((min.x + max.x) * .5f, top, min.z), new Vector3(footprint.width, .18f, .18f));
                    AddFrame(new Vector3((min.x + max.x) * .5f, top, max.z), new Vector3(footprint.width, .18f, .18f));
                    AddFrame(new Vector3(min.x, top, (min.z + max.z) * .5f), new Vector3(.18f, .18f, footprint.height));
                    AddFrame(new Vector3(max.x, top, (min.z + max.z) * .5f), new Vector3(.18f, .18f, footprint.height));
                }

                // Let timber construction remain visible through the smaller translucent dust.
                // The finished lot is revealed after the construction schedule completes.
                site.gameObject.SetActive(true);
                yield return new WaitForSeconds(.6f);
                float delay = 7f / pieces.Count;
                foreach (var piece in pieces)
                {
                    if (!site || !PlacedLots.Contains(record)) yield break;
                    piece.gameObject.SetActive(true);
                    yield return new WaitForSeconds(delay);
                }
                yield return new WaitForSeconds(.5f);
            }
            finally
            {
                if (veil) veil.Reveal();
                revealingLots.Remove(record);
                ReleaseLotPopulation(record);
                if (districtLotRoots.TryGetValue(record, out var reveal) && reveal) reveal.gameObject.SetActive(true);
                if (site) Destroy(site.gameObject);
                Destroy(wood);
                Destroy(dirt);
                foreach (var mesh in groundMeshes) Destroy(mesh);
            }

            void Post(float x, float z, float bottom, float top) =>
                AddFrame(new Vector3(x, (bottom + top) * .5f, z), new Vector3(.16f, top - bottom, .16f));

            void AddFrame(Vector3 position, Vector3 size)
            {
                var beam = Beam(frame, "Timber", position, size, wood);
                beam.gameObject.SetActive(false);
                pieces.Add(beam);
            }
        }

        private Transform Beam(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = name;
            beam.transform.SetParent(parent, false);
            beam.transform.localPosition = position;
            beam.transform.localScale = size;
            var collider = beam.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            beam.GetComponent<Renderer>().sharedMaterial = material;
            return beam.transform;
        }

        private LotObjectDefinition ChooseGrowthBuilding(DistrictLotRecord site, TownZoneFacing facing,
            LotObjectDefinition[] buildings)
        {
            if (buildings.Length == 1) return buildings[0];
            bool sideStreet = facing is TownZoneFacing.East or TownZoneFacing.West;
            bool positive = facing is TownZoneFacing.East or TownZoneFacing.North;
            var half = site.definition.SizeForRotation(site.rotation) * .5f;
            var neighborModels = new HashSet<string>();
            foreach (var neighbor in PlacedLots)
            {
                var layout = neighbor.definition.zoningLayout;
                if (layout.zone != site.definition.zoningLayout.zone || neighbor.diagonalRotation ||
                    ((int)layout.frontage + neighbor.rotation + (int)facing) % 4 != 0) continue;
                var otherHalf = neighbor.definition.SizeForRotation(neighbor.rotation) * .5f;
                var delta = neighbor.center - site.center;
                float along = sideStreet ? delta.y : delta.x;
                float across = sideStreet ? delta.x : delta.y;
                float expectedAlong = sideStreet ? half.y + otherHalf.y : half.x + otherHalf.x;
                float edgeDifference = across + (positive ? 1f : -1f) *
                    (sideStreet ? otherHalf.x - half.x : otherHalf.y - half.y);
                if (Mathf.Abs(Mathf.Abs(along) - expectedAlong) > .05f || Mathf.Abs(edgeDifference) > .05f) continue;
                var placedBuilding = neighbor.definition.objects.LastOrDefault();
                if (placedBuilding != null) neighborModels.Add(placedBuilding.assetId);
            }

            var different = buildings.Where(building => !neighborModels.Contains(building.assetId)).ToArray();
            var choices = different.Length > 0 ? different : buildings;
            return choices[UnityEngine.Random.Range(0, choices.Length)];
        }

        private static IEnumerable<(LotDefinition Layout, LotObjectDefinition[] Buildings)> GrowthOptions(
            (LotDefinition Layout, LotObjectDefinition[] Buildings)[][] sizeGroups)
        {
            foreach (var group in sizeGroups)
            {
                int start = group.Length > 1 ? UnityEngine.Random.Range(0, group.Length) : 0;
                for (int decorated = 1; decorated >= 0; decorated--)
                for (int i = 0; i < group.Length; i++)
                {
                    var option = group[(start + i) % group.Length];
                    if ((option.Layout.objects.Count > 0 || option.Layout.paths.Count > 0) == (decorated == 1))
                        yield return option;
                }
            }
        }

        private void ShowGrowthStatus(string message)
        {
            if (!string.IsNullOrEmpty(message)) ShowDistrictMessage(message);
            if (zoneStatus && zoneToolArmed && SupportsLotGrowth(activeZone) && !zoneDragStart.HasValue)
                zoneStatus.text = message;
        }

        private HashSet<Vector2Int> OccupiedGrowthCells()
        {
            var occupied=new HashSet<Vector2Int>();var zero=ZoneCorner(Vector2Int.zero);var members=ActiveDistrictMembers;
            foreach(var record in PlacedLots)
            {
                if(!members.Contains(record.district))continue;
                var footprint=new DistrictLotFootprint(record);var bounds=footprint.Bounds;
                var min=Vector2Int.FloorToInt((bounds.min-zero)/DistrictGridCellMeters);
                var max=Vector2Int.CeilToInt((bounds.max-zero)/DistrictGridCellMeters);
                for(int z=min.y;z<max.y;z++)for(int x=min.x;x<max.x;x++)
                {var cell=new Vector2Int(x,z);if(footprint.Contains(ZoneCorner(cell)+Vector2.one*(DistrictGridCellMeters*.5f),.001f))occupied.Add(cell);}
            }
            return occupied;
        }
        private string MissingGrowthContent(TownZone zone,LotWealth wealth)
        {
            string label=new LotZoningLayout{zone=zone,wealth=wealth}.Label.Replace("-","");
            var catalog=LotAssetCatalog.Load();bool models=catalog&&catalog.entries.Any(asset=>asset!=null&&asset.CanPreviewGrowable(zone,wealth,"founders"));
            return models?$"{label}: layout missing.":
                $"{label}: buildings missing.";
        }
        private bool HasGrowthZoning(RectInt cells, TownZone targetZone, TownZoneFacing facing, LotWealth wealth, Vector2Int lotSize)
        {
            if (lotSize != Vector2Int.zero && cells.size != ParcelWorldSize(lotSize, facing)) return false;
            bool anchored = zoneLotAnchors.ContainsKey(cells.min);
            foreach (var cell in cells.allPositionsWithin)
            {
                if (zoneLotAnchors.TryGetValue(cell, out var anchor) != anchored ||
                    (anchored && anchor != cells.min)) return false;
                if (!townZones.TryGetValue(cell, out var zone) || zone != targetZone ||
                    (WealthAt(cell) != wealth) || ZonedLotSize(cell) != lotSize ||
                    residentialFacing.GetValueOrDefault(cell) != facing) return false;
            }
            return true;
        }
    }
}

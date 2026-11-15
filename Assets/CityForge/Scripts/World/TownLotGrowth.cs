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
            problem = "Save a Founders-era R1 lot in the Zoning Lots library.";
            try
            {
                var catalog = LotAssetCatalog.Load();
                var layouts = LotLibrary.Local.List(LotType.Zoning).Select(entry => entry.Lot)
                    .Where(lot => (string.IsNullOrEmpty(lot.firstEraId) || lot.firstEraId == "founders") && lot.zoningLayout.zone == TownZone.Residential1 &&
                        !lot.IsWaterfront)
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
                        problem = $"{layout.name}: No matching house fits the building slot.";
                    else
                        options.Add((layout, buildings));
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
            fullGrowthCheckQueued = true;
            localGrowthCells.Clear();
            nextLotGrowth = Time.unscaledTime + LotGrowthDelay;
        }

        private void QueueZoningGrowth(RectInt edited)
        {
            // A lot grows from its minimum corner. Include corners outside the edit
            // whose footprints could extend into it, without scanning other zones.
            int reach = LotDefinition.MaxTiles - 1;
            int side = Mathf.RoundToInt(MapSide / DistrictGridCellMeters);
            for (int z = Mathf.Max(0, edited.yMin - reach); z < Mathf.Min(side, edited.yMax); z++)
            for (int x = Mathf.Max(0, edited.xMin - reach); x < Mathf.Min(side, edited.xMax); x++)
            {
                var cell = new Vector2Int(x, z);
                if (townZones.TryGetValue(cell, out var zone) && zone == TownZone.Residential1)
                    localGrowthCells.Add(cell);
            }
            nextLotGrowth = Time.unscaledTime + LotGrowthDelay;
        }

        private void QueueDemolitionGrowth(IEnumerable<DistrictLotRecord> removed)
        {
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
                    if (townZones.TryGetValue(cell, out var zone) && zone == TownZone.Residential1 &&
                        footprint.Contains(ZoneCorner(cell) + Vector2.one * (DistrictGridCellMeters * .5f)))
                        localGrowthCells.Add(cell);
                }
            }
            if (localGrowthCells.Count > 0)
                nextLotGrowth = Time.unscaledTime + LotGrowthDelay;
        }

        private void UpdateLotGrowth()
        {
            if (growthDistrict != SelectedDistrict)
            {
                growthDistrict = SelectedDistrict;
                QueueLotGrowth();
            }
            if (!CanBuildTown || IsSaving || movingDistrictLot != null || detonateLastPoint.HasValue ||
                Time.unscaledTime < nextLotGrowth) return;
            nextLotGrowth = float.PositiveInfinity;
            IEnumerable<Vector2Int> pending = fullGrowthCheckQueued ? residentialFacing.Keys : localGrowthCells;
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
                .GroupBy(option => option.Layout.zoningLayout.wealth)
                .ToDictionary(group => group.Key, group => group.GroupBy(option => GrowthFrontageSize(option.Layout))
                    .OrderByDescending(size => size.Key.y).ThenByDescending(size => size.Key.x)
                    .Select(size => size.ToArray()).ToArray());
            var fillersByWealth = options.Where(option => option.Layout.width == 1 && option.Layout.length == 1)
                .GroupBy(option => option.Layout.zoningLayout.wealth)
                .ToDictionary(group => group.Key, group => new[] { group.ToArray() });
            var passes = fillersByWealth.Count == 0 ? new[] { layoutsByWealth } :
                new[] { layoutsByWealth, fillersByWealth };
            var frontageChecks = new Dictionary<(RectInt cells, TownZoneFacing facing), bool>();
            var water = DistrictLotWater();
            var grownLots = new List<DistrictLotRecord>();
            var grownNames = new System.Collections.Generic.HashSet<string>();
            bool hasResidential = false;
            string blocked = null;
            foreach (var pass in passes)
            foreach (var cell in cellsToCheck)
            {
                if (!residentialFacing.TryGetValue(cell, out var facing) ||
                    !townZones.TryGetValue(cell, out var zone) || zone != TownZone.Residential1 ||
                    !InActiveDistrict(ZoneCorner(cell) +
                    Vector2.one * (DistrictGridCellMeters * .5f))) continue;
                hasResidential = true;
                var wealth = WealthAt(cell);
                if (!pass.TryGetValue(wealth, out var sizeGroups))
                {
                    if (!layoutsByWealth.ContainsKey(wealth) && !fillersByWealth.ContainsKey(wealth))
                        blocked ??= $"Save a Founders-era R1 · {new string('$', (int)wealth)} lot in the Zoning Lots library.";
                    continue;
                }
                bool placed = false;
                for (int facingOffset = 0; facingOffset < 4 && !placed; facingOffset++)
                foreach (var (layout, buildings) in GrowthOptions(sizeGroups))
                {
                    var roadFacing = (TownZoneFacing)(1 + ((int)facing - 1 + facingOffset) % 4);
                    var requestedSize = ZonedLotSize(cell);
                    if (requestedSize != Vector2Int.zero && requestedSize != GrowthFrontageSize(layout)) continue;
                    int rotation = (8 - (int)roadFacing - (int)layout.zoningLayout.frontage) % 4;
                    var size = GrowthLotCells(layout, rotation);
                    var cells = new RectInt(cell, size);
                    if (!HasGrowthZoning(cells, facing, wealth, requestedSize)) continue;

                    var center = (ZoneCorner(cells.min) + ZoneCorner(cells.max)) * .5f;
                    var record = new DistrictLotRecord
                    {
                        center = center, rotation = rotation, definition = layout,
                        district = DistrictAtWorld(new Vector3(center.x, 0f, center.y))
                    };
                    if (PlacedLots.Any(lot => lot.definition.id == layout.id && lot.center == center)) continue;
                    var siteProblem = InCampClearing(center, layout.Size.magnitude * .5f)
                        ? "Keep the lot clear of camps." : LotSiteProblem(record, water);
                    if (siteProblem != null)
                    {
                        blocked ??= siteProblem;
                        continue;
                    }
                    var frontageKey = (cells, roadFacing);
                    if (!frontageChecks.TryGetValue(frontageKey, out bool hasFrontage))
                    {
                        hasFrontage = ResidentialFrontageProblem(cells, ResidentialDepth(zone), out _,
                            requiredFacing: roadFacing) == null;
                        frontageChecks.Add(frontageKey, hasFrontage);
                    }
                    if (!hasFrontage)
                    {
                        blocked ??= "Keep the full lot footprint beside a road.";
                        continue;
                    }

                    if (buildings.Length > 0)
                    {
                        var building = ChooseGrowthBuilding(record, roadFacing, buildings);
                        record.definition = LotGrowableBuildings.Materialize(layout, building);
                        AddResidentialEntryPath(record, building);
                    }
                    else
                        record.definition = LotDefinition.Parse(JsonUtility.ToJson(layout));
                    record.id = Guid.NewGuid().ToString("N");
                    PlacedLots.Add(record);
                    grownLots.Add(record);
                    grownNames.Add(layout.name);
                    placed = true;
                    break;
                }
            }
            if (grownNames.Count > 0)
            {
                PresentDistrictLots(grownLots);
                RefreshZoneOverlay();
                RefreshDirtRoadEndFades();
                if (ApprovedDistrict)
                {
                    ApprovedDistrict.RefreshHarborStones();
                    ApprovedDistrict.Geography?.RefreshSettlements();
                }
                ShowGrowthStatus($"{string.Join(", ", grownNames.OrderBy(name => name, StringComparer.Ordinal))} grew. Save the region to keep it.");
            }
            else if (hasResidential)
                ShowGrowthStatus(blocked ?? GrowthSpaceHint(options[^1].Layout));
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
                if (layout.zone != TownZone.Residential1 || neighbor.diagonalRotation ||
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
            if (zoneStatus && activeZone == TownZone.Residential1 && !zoneDragStart.HasValue)
                zoneStatus.text = message;
        }

        private bool HasGrowthZoning(RectInt cells, TownZoneFacing facing, LotWealth wealth, Vector2Int lotSize)
        {
            foreach (var cell in cells.allPositionsWithin)
                if (!townZones.TryGetValue(cell, out var zone) || zone != TownZone.Residential1 ||
                    WealthAt(cell) != wealth || ZonedLotSize(cell) != lotSize || !residentialFacing.TryGetValue(cell, out var cellFacing) || cellFacing != facing) return false;
            return true;
        }
    }
}

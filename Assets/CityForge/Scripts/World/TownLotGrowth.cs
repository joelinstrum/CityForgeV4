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
            problem = "Save a Founders-era R1 · $ lot in the Zoning Lots library.";
            try
            {
                var catalog = LotAssetCatalog.Load();
                var layouts = LotLibrary.Local.List(LotType.Zoning).Select(entry => entry.Lot)
                    .Where(lot => (string.IsNullOrEmpty(lot.firstEraId) || lot.firstEraId == "founders") && lot.zoningLayout.zone == TownZone.Residential1 &&
                        lot.zoningLayout.wealth == LotWealth.Low && !lot.IsWaterfront)
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
                    if (buildings.Length == 0)
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

        private string ResidentialPaintStatus(RectInt bounds, TownZoneFacing facing)
        {
            var options = FoundersGrowthLayouts(out var problem);
            if (options.Length == 0) return problem;
            bool alongX = facing is TownZoneFacing.East or TownZoneFacing.West;
            var painted = alongX ? new Vector2Int(bounds.height, bounds.width) : bounds.size;
            foreach (var (layout, _) in options)
            {
                var required = GrowthFrontageSize(layout);
                if (painted.x >= required.x && painted.y >= required.y)
                    return $"Checking this site for {layout.name}…";
            }
            return GrowthSpaceHint(options[^1].Layout);
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
            if (!CanBuildTown || IsSaving || lotPlacementDraft != null || detonateLastPoint.HasValue ||
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

            var water = DistrictLotWater();
            var grownNames = new System.Collections.Generic.HashSet<string>();
            bool hasResidential = false;
            string blocked = null;
            foreach (var cell in cellsToCheck)
            {
                if (!residentialFacing.TryGetValue(cell, out var facing) ||
                    !townZones.TryGetValue(cell, out var zone) || zone != TownZone.Residential1 ||
                    !InActiveDistrict(ZoneCorner(cell) +
                    Vector2.one * (DistrictGridCellMeters * .5f))) continue;
                hasResidential = true;
                foreach (var (layout, buildings) in options)
                {
                    int rotation = (8 - (int)facing - (int)layout.zoningLayout.frontage) % 4;
                    var size = GrowthLotCells(layout, rotation);
                    var cells = new RectInt(cell, size);
                    if (!HasGrowthZoning(cells, facing)) continue;

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
                    // Road access is independent of the direction the player wants the house to face.
                    if (ResidentialFrontageProblem(cells, ResidentialDepth(zone), out _) != null)
                    {
                        blocked ??= "Keep the full lot footprint beside a road.";
                        continue;
                    }

                    record.definition = LotGrowableBuildings.Materialize(layout,
                        buildings[UnityEngine.Random.Range(0, buildings.Length)]);
                    record.id = Guid.NewGuid().ToString("N");
                    PlacedLots.Add(record);
                    grownNames.Add(layout.name);
                    break;
                }
            }
            if (grownNames.Count > 0)
            {
                RebuildDistrictLots();
                ShowGrowthStatus($"{string.Join(", ", grownNames.OrderBy(name => name, StringComparer.Ordinal))} grew. Save the region to keep it.");
            }
            else if (hasResidential)
                ShowGrowthStatus(blocked ?? GrowthSpaceHint(options[^1].Layout));
        }

        private void ShowGrowthStatus(string message)
        {
            if (zoneStatus && activeZone == TownZone.Residential1 && !zoneDragStart.HasValue)
                zoneStatus.text = message;
        }

        private bool HasGrowthZoning(RectInt cells, TownZoneFacing facing)
        {
            foreach (var cell in cells.allPositionsWithin)
                if (!townZones.TryGetValue(cell, out var zone) || zone != TownZone.Residential1 ||
                    !residentialFacing.TryGetValue(cell, out var cellFacing) || cellFacing != facing) return false;
            return true;
        }
    }
}

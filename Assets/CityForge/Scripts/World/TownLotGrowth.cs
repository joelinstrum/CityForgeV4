using System;
using System.IO;
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
        private bool loadedGrowthLayout;
        private LotDefinition growthLayout;
        private string growthLayoutProblem = "Save Old Farm 1 as a Founders-era R1 · $ growable lot.";

        private LotDefinition FoundersGrowthLayout()
        {
            if (loadedGrowthLayout) return growthLayout;
            loadedGrowthLayout = true;
            try
            {
                // Keep the authored surroundings separate from each household's selected building.
                var layout = LotLibrary.Local.List(LotType.Zoning).Select(entry => entry.Lot)
                    .FirstOrDefault(lot => lot.name == "Old Farm 1" && lot.firstEraId == "founders" &&
                        lot.zoningLayout.zone == TownZone.Residential1 && lot.zoningLayout.wealth == LotWealth.Low &&
                        !lot.IsWaterfront && lot.zoningLayout.Fits(lot));
                if (layout == null) return null;

                growthLayoutProblem = "Old Farm 1 needs room in its object list.";
                if (layout.objects.Count >= 512) return null;
                var catalog = LotAssetCatalog.Load();
                growthLayoutProblem = LotAssetProblem(layout, catalog);
                if (growthLayoutProblem == null) growthLayout = layout;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                growthLayoutProblem = "Could not read the growable lot library.";
                Debug.LogWarning($"{growthLayoutProblem} {error.Message}");
            }
            return growthLayout;
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
            var layout = FoundersGrowthLayout();
            if (layout == null) return growthLayoutProblem;
            bool alongX = facing is TownZoneFacing.East or TownZoneFacing.West;
            var painted = alongX ? new Vector2Int(bounds.height, bounds.width) : bounds.size;
            var required = GrowthFrontageSize(layout);
            return painted.x >= required.x && painted.y >= required.y
                ? $"Checking this site for {layout.name}…"
                : GrowthSpaceHint(layout);
        }

        private static Vector2Int GrowthLotCells(LotDefinition layout, int rotation) =>
            Vector2Int.RoundToInt(layout.SizeForRotation(rotation) / DistrictGridCellMeters);

        private void QueueLotGrowth() => nextLotGrowth = Time.unscaledTime + LotGrowthDelay;

        private void UpdateLotGrowth()
        {
            if (growthDistrict != SelectedDistrict)
            {
                growthDistrict = SelectedDistrict;
                QueueLotGrowth();
            }
            if (!CanBuildTown || IsSaving || lotPlacementDraft != null || Time.unscaledTime < nextLotGrowth) return;
            nextLotGrowth = float.PositiveInfinity;
            if (FoundersGrowthLayout() is not { } layout)
            {
                ShowGrowthStatus(growthLayoutProblem);
                return;
            }

            var catalog = LotAssetCatalog.Load();
            var buildings = LotGrowableBuildings.Candidates(layout, catalog);
            if (buildings.Length == 0)
            {
                ShowGrowthStatus("No matching house fits this lot within the 10% footprint allowance.");
                return;
            }
            var water = DistrictLotWater();
            bool grew = false;
            bool hasResidential = false;
            string blocked = null;
            foreach (var pair in residentialFacing.OrderBy(pair => pair.Key.y).ThenBy(pair => pair.Key.x))
            {
                if (townZones[pair.Key] != TownZone.Residential1 || !InActiveDistrict(ZoneCorner(pair.Key) +
                    Vector2.one * (DistrictGridCellMeters * .5f))) continue;
                hasResidential = true;
                int rotation = (8 - (int)pair.Value - (int)layout.zoningLayout.frontage) % 4;
                var size = GrowthLotCells(layout, rotation);
                var cells = new RectInt(pair.Key, size);
                if (!HasGrowthZoning(cells, pair.Value)) continue;

                var center = (ZoneCorner(cells.min) + ZoneCorner(cells.max)) * .5f;
                var record = new DistrictLotRecord
                {
                    center = center, rotation = rotation, definition = layout,
                    district = DistrictAtWorld(new Vector3(center.x, 0f, center.y))
                };
                if (PlacedLots.Any(lot => lot.definition.id == layout.id && lot.center == center)) continue;
                var siteProblem = InCampClearing(center, layout.Size.magnitude * .5f)
                    ? "Keep the farm clear of camps." : LotSiteProblem(record, water);
                if (siteProblem != null)
                {
                    blocked ??= siteProblem;
                    continue;
                }
                bool facesAlongX = pair.Value is TownZoneFacing.East or TownZoneFacing.West;
                int depth = facesAlongX ? size.x : size.y;
                // The zoning arrow already chose the frontage axis. A side street at a corner
                // must not disqualify a farm that still has a road along that axis.
                if (ResidentialFrontageProblem(cells, depth, out _, frontageAlongX: facesAlongX) != null)
                {
                    blocked ??= "Keep the full farm footprint beside a road.";
                    continue;
                }

                record.definition = LotGrowableBuildings.Materialize(layout,
                    buildings[UnityEngine.Random.Range(0, buildings.Length)]);
                record.id = Guid.NewGuid().ToString("N");
                PlacedLots.Add(record);
                grew = true;
            }
            if (grew)
            {
                RebuildDistrictLots();
                ShowGrowthStatus($"{layout.name} grew. Save the region to keep it.");
            }
            else if (hasResidential)
                ShowGrowthStatus(blocked ?? GrowthSpaceHint(layout));
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

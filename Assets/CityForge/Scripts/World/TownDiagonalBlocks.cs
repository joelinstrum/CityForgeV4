using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private (LotDefinition Layout, LotObjectDefinition[] Buildings)[] diagonalGrowthOptions;

        // Diagonal stamps use native rotated lot records, never rasterized square-grid cells.
        private Vector2 DiagonalBlockPoint(ZonedBlockPlan plan, Vector2 point)
        {
            var center = (ZoneCorner(plan.Bounds.min) + ZoneCorner(plan.Bounds.max)) * .5f;
            var rotated = Quaternion.Euler(0, 45, 0) * new Vector3(point.x - center.x, 0, point.y - center.y);
            return center + new Vector2(rotated.x, rotated.z);
        }

        private string PrepareDiagonalBlock(ZonedBlockPlan plan, out List<DistrictLotRecord> lots,
            out List<DistrictRoadRecord> streets)
        {
            lots = new(); streets = new();
            var zone = plan.Cells[0].zone;
            var wealth = plan.Cells[0].wealth;
            diagonalGrowthOptions ??= FoundersGrowthLayouts(out _);
            var options = diagonalGrowthOptions.Where(option => option.Buildings.Length > 0 && option.Layout.zoningLayout.zone == zone &&
                option.Layout.zoningLayout.wealth == wealth && option.Layout.SideStreetTurn == 0 &&
                GrowthFrontageSize(option.Layout) == new Vector2Int(plan.LotSize,plan.LotDepth)).ToArray();
            if (options.Length == 0) return $"Save a matching {plan.LotSize} × {plan.LotDepth} growable layout for this zone to try a diagonal block.";
            var water = DistrictLotWater();
            foreach (var parcel in plan.Cells.GroupBy(cell => new Vector2Int(cell.lotAnchorX, cell.lotAnchorZ)))
            {
                var template = options[0].Layout;
                var facing = parcel.First().facing;
                int rotation = (8 - (int)facing - (int)template.zoningLayout.frontage) % 4;
                var center = DiagonalBlockPoint(plan, ZoneCorner(parcel.Key) + new Vector2(parcel.First().lotWidth,parcel.First().lotDepth) * (DistrictGridCellMeters*.5f));
                var record = new DistrictLotRecord { center = center, rotation = rotation, diagonalRotation = true,
                    definition = template, district = DistrictAtWorld(new Vector3(center.x, 0, center.y)) };
                var footprint = new DistrictLotFootprint(record);
                if (PlacedLots.Any(lot => footprint.Overlaps(new DistrictLotFootprint(lot))) ||
                    FootprintHasGridZoning(footprint) || LotOverlapsDistrictRoad(record, ResidentialRoadCornerInset)) continue;
                if (InCampClearing(center, template.Size.magnitude * .5f)) return "Keep the lot clear of camps.";
                var problem = LotSiteProblem(record, water, growing: true);
                if (problem != null) return problem;
                lots.Add(record);
            }
            if (lots.Count == 0) return "No room for another complete lot here. Move onto open ground.";
            var ring = plan.Street(ZoneCorner(plan.Bounds.min), DistrictGridCellMeters, SelectedDistrict);
            ring.points = ring.points.Select(point => DiagonalBlockPoint(plan, point)).ToArray();
            ring.gridAligned = false;
            var runs = new List<DistrictRoadRecord>();
            bool clipped = false;
            for (int side = 0; side < 4; side++)
            {
                var a = ring.points[side]; var b = ring.points[(side + 1) % 4];
                int steps = Mathf.RoundToInt(Vector2.Distance(a, b) / DistrictGridCellMeters);
                var run = new List<Vector2>();
                for (int step = 0; step < steps; step++)
                {
                    var start = Vector2.Lerp(a, b, (float)step / steps);
                    var end = Vector2.Lerp(a, b, (float)(step + 1) / steps);
                    var center = (start + end) * .5f;
                    var span = new DistrictLotRecord { center = center, rotation = side % 2 == 0 ? plan.Turns : plan.Turns + 1,
                        diagonalRotation = true, definition = new LotDefinition { width = 1, length = 1 } };
                    var footprint = new DistrictLotFootprint(span);
                    bool occupied = PlacedLots.Any(lot => footprint.Overlaps(new DistrictLotFootprint(lot))) || FootprintHasGridZoning(footprint);
                    if (!occupied)
                    {
                        var mesh = BuildDistrictRoadMesh(Street(new[] { start, end }));
                        occupied = RoadHitsBuilding(mesh, mesh.vertices);
                        var problem = occupied ? null : RoadSiteProblem(mesh);
                        Destroy(mesh);
                        if (problem != null) return problem;
                    }
                    if (occupied) { clipped = true; Finish(); continue; }
                    if (run.Count == 0) run.Add(start);
                    run.Add(end);
                }
                Finish();
                void Finish()
                {
                    if (run.Count >= 2) runs.AddRange(UnpavedRoadPieces(Street(new[] { run[0], run[^1] })));
                    run.Clear();
                }
            }
            streets.AddRange(clipped ? runs : UnpavedRoadPieces(ring));
            JoinZonedStreetRuns(streets); CompleteZonedStreetJunctions(streets);
            foreach (var road in streets)
            {
                road.gridAligned = false;
                var mesh = BuildDistrictRoadMesh(road); var problem = RoadSiteProblem(mesh); Destroy(mesh);
                if (problem != null) return problem;
            }
            return districtRoads.Count + streets.Count > MaxDistrictRoads ? "This region has reached its road limit." : null;
            DistrictRoadRecord Street(Vector2[] points) => new() { points = points, width = ring.width,
                surfaceId = ring.surfaceId, district = ring.district, curved = ring.curved };
        }

        private bool FootprintHasGridZoning(DistrictLotFootprint footprint)
        {
            var bounds = footprint.Bounds;
            var low = Vector2Int.FloorToInt((bounds.min + Vector2.one * (MapSide * .5f)) / DistrictGridCellMeters);
            var high = Vector2Int.CeilToInt((bounds.max + Vector2.one * (MapSide * .5f)) / DistrictGridCellMeters);
            foreach (var cell in new RectInt(low, high - low).allPositionsWithin)
                if (townZones.ContainsKey(cell) && footprint.Overlaps(new DistrictLotFootprint(
                    ZoneCorner(cell) + Vector2.one * (DistrictGridCellMeters * .5f), Vector2.one * (DistrictGridCellMeters * .5f)))) return true;
            return false;
        }

        private string CommitDiagonalBlock(ZonedBlockPlan plan)
        {
            if (!CanBuildTown || IsSaving) return "Establish the town center before zoning.";
            var problem = PrepareDiagonalBlock(plan, out var lots, out var streets);
            if (problem != null) return problem;
            var options = diagonalGrowthOptions;
            var catalog = LotAssetCatalog.Load();
            foreach (var record in lots)
            {
                var option = options.First(item => item.Layout.id == record.definition.id);
                if (option.Buildings.Length == 0) return "This layout needs a matching growable building.";
                var building = option.Buildings[UnityEngine.Random.Range(0, option.Buildings.Length)];
                record.definition = LotGrowableBuildings.Materialize(option.Layout, building, option.Layout.id+":"+record.district+":"+record.center);
                record.growthBuildingId = record.definition.objects.Last().id;
                record.id = Guid.NewGuid().ToString("N");
                if (IsResidentialZone(record.definition.zoningLayout.zone)) AddResidentialEntryPath(record, building);
                LotPropStyles.Apply(record.definition, record.id, catalog);
            }
            problem = CommitNewRoadPieces(streets);
            if (problem != null) return problem;
            CommitBlockGroundPlane(plan,streets,true);
            foreach(var lot in lots){PrepareLotGrading(lot);HoldLotPopulation(lot);}
            PlacedLots.AddRange(lots); ClearDevelopedSites(lots); PresentDistrictLots(lots);
            foreach (var record in lots) AddLotStreetlamps(record);
            foreach (var record in lots) StartCoroutine(ConstructZonedLot(record));
            RefreshZoneOverlay(); RefreshDirtRoadEndFades();
            if (ApprovedDistrict)
            {
                ApprovedDistrict.RefreshHarborStones();
                ApprovedDistrict.Geography?.RefreshSettlements();
            }
            return null;
        }

        private List<DistrictLotRecord> DiagonalPreviewFootprints(ZonedBlockPlan plan)
        {
            var result=new List<DistrictLotRecord>();
            foreach(var parcel in plan.Cells.GroupBy(cell=>new Vector2Int(cell.lotAnchorX,cell.lotAnchorZ)))
            {
                var cell=parcel.First();
                var center=DiagonalBlockPoint(plan,ZoneCorner(parcel.Key)+new Vector2(cell.lotWidth,cell.lotDepth)*(DistrictGridCellMeters*.5f));
                result.Add(new DistrictLotRecord{center=center,rotation=(8-(int)cell.facing)%4,diagonalRotation=true,
                    definition=new LotDefinition{width=plan.LotSize,length=plan.LotDepth}});
            }
            return result;
        }

        private void FillDiagonalBlockPreview(ZonedBlockPlan plan, List<DistrictLotRecord> lots, bool valid)
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>[ZoneColors.Length + 1];
            for (int i = 0; i < indices.Length; i++) indices[i] = new();
            foreach (var record in lots)
            {
                var footprint = new DistrictLotFootprint(record);
                var size = record.definition.Size;
                for (int z = 0; z < record.definition.length; z++)
                for (int x = 0; x < record.definition.width; x++)
                {
                    var local = new Vector2(x * DistrictGridCellMeters, z * DistrictGridCellMeters) - size * .5f;
                    int start = vertices.Count;
                    float left = x == 0 ? .09f : 0, bottom = z == 0 ? .09f : 0;
                    float right = x == record.definition.width - 1 ? 3.91f : 4f;
                    float top = z == record.definition.length - 1 ? 3.91f : 4f;
                    Add(local + new Vector2(left, bottom)); Add(local + new Vector2(left, top));
                    Add(local + new Vector2(right, top)); Add(local + new Vector2(right, bottom));
                    indices[valid ? (int)plan.Cells[0].zone : 0].AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                }
                void Add(Vector2 point)
                {
                    var world = footprint.WorldPoint(point); vertices.Add(new Vector3(world.x, LotHeight(world) + .27f, world.y));
                }
            }
            zonePreviewMesh.Clear(); zonePreviewMesh.SetVertices(vertices); zonePreviewMesh.subMeshCount = indices.Length;
            for (int i = 0; i < indices.Length; i++) zonePreviewMesh.SetTriangles(indices[i], i, false);
            zonePreviewMesh.RecalculateBounds();
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private bool zonedBlockArmed;
        private int zonedBlockSize, zonedBlockTurns;
        private Vector2Int? zonedBlockHover;
        private ZonedBlockPlan zonedBlockPreview;
        private string zonedBlockProblem;
        private NavigationState zonedBlockNavigation;
        private Transform zonedBlockRoadPreview;
        private Mesh zonedBlockRoadMesh;
        private Material zonedBlockRoadMaterial;

        private void ArmZonedBlock(TownZone zone, int size)
        {
            CancelRoadPlacement(); CancelLotPlacement(); CancelZoneDrag(); CancelZonedBlock(false);
            zoneToolArmed = false; activeZone = zone; zonedBlockSize = size; zonedBlockTurns = 0; zonedBlockArmed = true;
            SelectDistrictLot(null); EnsureZoneSurfaces();
            if (townPanel) townPanel.gameObject.SetActive(false);
            ShowDistrictMessage("Move the block into place · Tab to rotate · Click to build streets and zone · Esc to cancel");
        }

        private void CancelZonedBlock(bool showPanel)
        {
            zonedBlockArmed = false; zonedBlockHover = null; zonedBlockPreview = null;
            if (zonePreviewRenderer) zonePreviewRenderer.enabled = false;
            if (zonedBlockRoadPreview) { zonedBlockRoadPreview.gameObject.SetActive(false); Destroy(zonedBlockRoadPreview.gameObject); }
            if (zonedBlockRoadMesh) Destroy(zonedBlockRoadMesh);
            if (zonedBlockRoadMaterial) Destroy(zonedBlockRoadMaterial);
            zonedBlockRoadPreview = null; zonedBlockRoadMesh = null; zonedBlockRoadMaterial = null;
            if (showPanel && townPanel) townPanel.gameObject.SetActive(true);
        }

        private bool UpdateZonedBlock()
        {
            if (!Application.isFocused || !TownConstructionVisible || !CanBuildTown || IsSaving)
            {
                if (zonePreviewRenderer) zonePreviewRenderer.enabled = false;
                if (zonedBlockRoadPreview) zonedBlockRoadPreview.gameObject.SetActive(false);
                return false;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            { CancelZonedBlock(true); ShowDistrictMessage(""); return true; }
            bool rotated = Input.GetKeyDown(KeyCode.Tab);
            if (rotated) zonedBlockTurns = (zonedBlockTurns + 1) % 4;
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            {
                zonePreviewRenderer.enabled = false;
                if (zonedBlockRoadPreview) zonedBlockRoadPreview.gameObject.SetActive(false);
                return false;
            }
            if (!TryZoneCell(out var cell))
            {
                zonePreviewRenderer.enabled = false;
                if (zonedBlockRoadPreview) zonedBlockRoadPreview.gameObject.SetActive(false);
                return false;
            }
            var navigation = new NavigationState(this);
            if (zonedBlockHover != cell || rotated || !navigation.Equals(zonedBlockNavigation))
            {
                zonedBlockHover = cell; zonedBlockNavigation = navigation;
                var dimensions = zonedBlockTurns % 2 == 0 ? new Vector2Int(ZonedBlockPlan.RowTiles, zonedBlockSize * 2)
                    : new Vector2Int(zonedBlockSize * 2, ZonedBlockPlan.RowTiles);
                zonedBlockPreview = new ZonedBlockPlan(cell - dimensions / 2, zonedBlockSize, zonedBlockTurns, activeZone, selectedZoneWealth);
                zonedBlockProblem = PrepareZonedBlock(zonedBlockPreview, out var cells, out var streets);
                if (zonedBlockRoadMesh) Destroy(zonedBlockRoadMesh);
                zonedBlockRoadMesh = new Mesh { name = "Available automatic streets" };
                var meshes = streets.Select(BuildDistrictRoadMesh).ToArray();
                if (meshes.Length > 0) zonedBlockRoadMesh.CombineMeshes(meshes.Select(mesh => new CombineInstance { mesh = mesh, transform = Matrix4x4.identity }).ToArray());
                foreach (var mesh in meshes) Destroy(mesh);
                FillZoneMesh(zonePreviewMesh, (zonedBlockProblem == null ? cells : zonedBlockPreview.Cells).Select(item => new KeyValuePair<Vector2Int, TownZone>(new Vector2Int(item.x, item.z),
                    zonedBlockProblem == null ? item.zone : TownZone.None)), .27f, overrideFacing: zonedBlockPreview.Facings);
                if (!zonedBlockRoadPreview)
                {
                    zonedBlockRoadPreview = new GameObject("Automatic street preview", typeof(MeshFilter), typeof(MeshRenderer)).transform;
                    zonedBlockRoadPreview.SetParent(transform, false);
                    zonedBlockRoadMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    LotGroundTexture.ConfigureTransparentLayer(zonedBlockRoadMaterial, 5);
                    var renderer = zonedBlockRoadPreview.GetComponent<MeshRenderer>(); renderer.sharedMaterial = zonedBlockRoadMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                zonedBlockRoadPreview.GetComponent<MeshFilter>().sharedMesh = zonedBlockRoadMesh;
                zonedBlockRoadMaterial.SetColor("_BaseColor", zonedBlockProblem == null ? new Color(.88f, .72f, .38f, .5f) : new Color(.92f, .15f, .12f, .6f));
                ShowDistrictMessage(zonedBlockProblem ?? $"{cells.Length / (zonedBlockSize * zonedBlockSize)} new lots · Existing neighborhood preserved\nClick to place · Tab to rotate · Esc to cancel");
            }
            zonePreviewRenderer.enabled = true;
            if (zonedBlockRoadPreview) zonedBlockRoadPreview.gameObject.SetActive(true);
            if (!Input.GetMouseButtonDown(0)) return false;
            if (zonedBlockProblem == null) zonedBlockProblem = CommitZonedBlock(zonedBlockPreview);
            ShowDistrictMessage(zonedBlockProblem ?? "Block zoned with streets · Move to place another · Esc to finish");
            zonedBlockHover = null;
            return true;
        }

        // Overlapping stamps add complete free parcels and leave all existing parcels intact.
        private string PrepareZonedBlock(ZonedBlockPlan plan, out TownZoneCell[] cells, out List<DistrictRoadRecord> streets)
        {
            var available = new List<TownZoneCell>();
            streets = new List<DistrictRoadRecord>();
            cells = System.Array.Empty<TownZoneCell>();
            var water = DistrictLotWater();
            foreach (var parcel in plan.Cells.GroupBy(cell => new Vector2Int(cell.lotAnchorX, cell.lotAnchorZ)))
            {
                var bounds = new RectInt(parcel.Key, Vector2Int.one * plan.LotSize);
                if (parcel.Any(cell => townZones.ContainsKey(new Vector2Int(cell.x, cell.z)))) continue;
                var center = (ZoneCorner(bounds.min) + ZoneCorner(bounds.max)) * .5f;
                var footprint = new DistrictLotFootprint(center, (Vector2)bounds.size * (DistrictGridCellMeters * .5f) - Vector2.one * .025f);
                if (PlacedLots.Any(lot => footprint.Overlaps(new DistrictLotFootprint(lot)))) continue;
                var record = new DistrictLotRecord { center = center, definition = new LotDefinition { width = bounds.width, length = bounds.height } };
                if (LotOverlapsDistrictRoad(record, .05f)) continue;
                var problem = ZoneSiteProblem(bounds, parcel.First().zone, water, out _, requireFrontage: false);
                if (problem != null) return problem;
                available.AddRange(parcel);
            }
            cells = available.ToArray();
            if (cells.Length == 0) return "No room for another complete lot here. Move the block farther onto open ground.";
            if (townZones.Count + cells.Length > MaxZonedCells) return "This region has reached its zoning limit.";

            var streetsLocal = new List<DistrictRoadRecord>();
            bool clipped = false;
            var ring = plan.Street(ZoneCorner(plan.Bounds.min), DistrictGridCellMeters, SelectedDistrict);
            // Test one-tile street spans, then merge consecutive clear spans before normal road reuse.
            for (int side = 0; side < ring.points.Length; side++)
            {
                var start = ring.points[side];
                var end = ring.points[(side + 1) % ring.points.Length];
                int steps = Mathf.RoundToInt(Vector2.Distance(start, end) / DistrictGridCellMeters);
                var run = new List<Vector2>();
                for (int step = 0; step < steps; step++)
                {
                    var a = Vector2.Lerp(start, end, (float)step / steps);
                    var b = Vector2.Lerp(start, end, (float)(step + 1) / steps);
                    var half = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y)) * .5f + Vector2.one * (ring.width * .5f - .025f);
                    // The ribbon has no end caps: its longitudinal footprint stops at a/b.
                    if (Mathf.Abs(b.x - a.x) > .1f) half.x = Mathf.Abs(b.x - a.x) * .5f - .025f;
                    else half.y = Mathf.Abs(b.y - a.y) * .5f - .025f;
                    var footprint = new DistrictLotFootprint((a + b) * .5f, half);
                    bool occupied = PlacedLots.Any(lot => footprint.Overlaps(new DistrictLotFootprint(lot)));
                    var low = Vector2Int.FloorToInt(((a + b) * .5f - half + Vector2.one * (MapSide * .5f)) / DistrictGridCellMeters);
                    var high = Vector2Int.CeilToInt(((a + b) * .5f + half + Vector2.one * (MapSide * .5f)) / DistrictGridCellMeters);
                    foreach (var key in new RectInt(low, high - low).allPositionsWithin)
                        occupied |= townZones.ContainsKey(key);
                    if (!occupied)
                    {
                        var piece = StreetSpan(new[] { a, b });
                        var mesh = BuildDistrictRoadMesh(piece);
                        occupied = RoadHitsBuilding(mesh, mesh.vertices);
                        var problem = occupied ? null : RoadSiteProblem(mesh);
                        Destroy(mesh);
                        if (problem != null) return problem;
                    }
                    if (occupied) { clipped = true; FinishRun(); continue; }
                    if (run.Count == 0) run.Add(a);
                    run.Add(b);
                }
                FinishRun();
                void FinishRun()
                {
                    if (run.Count >= 2) streetsLocal.AddRange(UnpavedRoadPieces(StreetSpan(new[] { run[0], run[^1] })));
                    run.Clear();
                }
                DistrictRoadRecord StreetSpan(Vector2[] points) => new() { district = ring.district, surfaceId = ring.surfaceId,
                    width = ring.width, gridAligned = true, points = points };
            }
            streets.AddRange(clipped ? streetsLocal : UnpavedRoadPieces(ring));
            foreach (var street in streets)
            {
                street.gridAligned = true;
                var mesh = BuildDistrictRoadMesh(street);
                var problem = RoadSiteProblem(mesh);
                Destroy(mesh);
                if (problem != null) return problem;
            }
            return districtRoads.Count + streets.Count > MaxDistrictRoads ? "This region has reached its road limit." : null;
        }

        internal string CommitZonedBlock(ZonedBlockPlan plan)
        {
            if (!CanBuildTown || IsSaving) return "Establish the town center before zoning.";
            var problem = PrepareZonedBlock(plan, out var cells, out var pieces);
            if (problem != null) return problem;
            problem = CommitNewRoadPieces(pieces);
            if (problem != null) return problem;
            foreach (var cell in cells)
            {
                var key = new Vector2Int(cell.x, cell.z);
                townZones[key] = cell.zone; zoneWealth[key] = cell.wealth; residentialFacing[key] = cell.facing;
                zoneLotSizes[key] = new Vector2Int(cell.lotWidth, cell.lotDepth);
                zoneLotAnchors[key] = new Vector2Int(cell.lotAnchorX, cell.lotAnchorZ);
            }
            QueueZoningGrowth(plan.Bounds); RefreshZoneOverlay();
            return null;
        }
    }
}

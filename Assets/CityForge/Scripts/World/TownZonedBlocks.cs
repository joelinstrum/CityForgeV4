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
                var street = zonedBlockPreview.Street(ZoneCorner(zonedBlockPreview.Bounds.min), DistrictGridCellMeters, SelectedDistrict);
                if (zonedBlockRoadMesh) Destroy(zonedBlockRoadMesh);
                zonedBlockRoadMesh = BuildDistrictRoadMesh(street);
                zonedBlockProblem = ZonedBlockSiteProblem(zonedBlockPreview, zonedBlockRoadMesh);
                FillZoneMesh(zonePreviewMesh, zonedBlockPreview.Cells.Select(item => new KeyValuePair<Vector2Int, TownZone>(new Vector2Int(item.x, item.z),
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
                ShowDistrictMessage(zonedBlockProblem ?? $"{zonedBlockPreview.LotCount} lots · Back-to-back · Dirt streets included\nClick to place · Tab to rotate · Esc to cancel");
            }
            zonePreviewRenderer.enabled = true;
            if (zonedBlockRoadPreview) zonedBlockRoadPreview.gameObject.SetActive(true);
            if (!Input.GetMouseButtonDown(0)) return false;
            if (zonedBlockProblem == null) zonedBlockProblem = CommitZonedBlock(zonedBlockPreview);
            ShowDistrictMessage(zonedBlockProblem ?? "Block zoned with streets · Move to place another · Esc to finish");
            zonedBlockHover = null;
            return true;
        }

        private string ZonedBlockSiteProblem(ZonedBlockPlan plan, Mesh road)
        {
            if (townZones.Count + plan.Cells.Length > MaxZonedCells) return "This region has reached its zoning limit.";
            foreach (var cell in plan.OuterBounds.allPositionsWithin)
                if (townZones.ContainsKey(cell)) return "Keep the new block and its streets clear of existing zoning.";
            var problem = ZoneSiteProblem(plan.Bounds, plan.Cells[0].zone, DistrictLotWater(), out _, requireFrontage: false);
            if (problem != null) return problem;
            var center = (ZoneCorner(plan.Bounds.min) + ZoneCorner(plan.Bounds.max)) * .5f;
            var footprint = new DistrictLotRecord { center = center, definition = new LotDefinition { width = plan.Bounds.width, length = plan.Bounds.height } };
            if (LotOverlapsDistrictRoad(footprint, .05f)) return "Keep existing streets outside the zoned rows.";
            return RoadSiteProblem(road);
        }

        internal string CommitZonedBlock(ZonedBlockPlan plan)
        {
            if (!CanBuildTown || IsSaving) return "Establish the town center before zoning.";
            var street = plan.Street(ZoneCorner(plan.Bounds.min), DistrictGridCellMeters, SelectedDistrict);
            var mesh = BuildDistrictRoadMesh(street);
            var problem = ZonedBlockSiteProblem(plan, mesh);
            Destroy(mesh);
            if (problem != null) return problem;
            var pieces = UnpavedRoadPieces(street);
            foreach (var piece in pieces) piece.gridAligned = true;
            problem = CommitNewRoadPieces(pieces);
            if (problem != null) return problem;
            foreach (var cell in plan.Cells)
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

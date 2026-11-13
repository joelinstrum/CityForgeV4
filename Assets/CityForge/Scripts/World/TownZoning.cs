using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CityForge.World
{
    public enum TownZone
    {
        None, Residential1, Residential2, Residential3, Commercial1, Commercial2, Commercial3,
        Industrial1, Industrial2, Industrial3
    }

    [Serializable]
    public struct TownZoneCell
    {
        public int x, z;
        public TownZone zone;
        public TownZoneFacing facing;
        public LotWealth wealth;
    }

    public enum TownZoneFacing { None, East, North, West, South }

    public sealed partial class PrototypeRegionView
    {
        internal const int TownZoneGridVersion = 1;
        private const float LegacyTownZoneCellMeters = 8f;
        private const int LegacyMaxZonedCells = 32768;
        private const int MaxZonedCells = LegacyMaxZonedCells * 4;
        private const int MaxZoneDragSide = 32;
        private static readonly Color[] ZoneColors =
        {
            new(.82f, .28f, .26f), new(.60f, .93f, .46f), new(.32f, .79f, .28f), new(.18f, .58f, .23f),
            new(.56f, .82f, 1f), new(.27f, .62f, .96f), new(.23f, .38f, .84f),
            new(1f, .91f, .53f), new(.96f, .70f, .25f), new(.86f, .49f, .16f)
        };

        private readonly Dictionary<Vector2Int, LotWealth> zoneWealth = new();
        private LotWealth selectedZoneWealth = LotWealth.Low;
        private Dropdown zoneWealthPicker;

        private LotWealth WealthAt(Vector2Int cell) => zoneWealth.GetValueOrDefault(cell, LotWealth.Low);

        private void BuildZoneWealthPicker(Transform hud, Font font)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var picker = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            picker.name = "Zoning wealth";
            var rect = (RectTransform)picker.transform;
            rect.SetParent(hud, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-25, 20);
            rect.sizeDelta = new Vector2(205, 36);
            zoneWealthPicker = picker.GetComponent<Dropdown>();
            zoneWealthPicker.captionText.font = zoneWealthPicker.itemText.font = font;
            zoneWealthPicker.ClearOptions();
            zoneWealthPicker.AddOptions(new List<string> { "Low Wealth", "Medium Wealth", "High Wealth" });
            zoneWealthPicker.onValueChanged.AddListener(index => selectedZoneWealth = (LotWealth)(index + 1));
            picker.SetActive(false);
#endif
        }

        private readonly Dictionary<Vector2Int, TownZone> townZones = new();
        private readonly Dictionary<Vector2Int, TownZoneFacing> residentialFacing = new();
        private readonly Button[] zoneButtons = new Button[10];
        private readonly List<RaycastResult> zoneUiHits = new();
        private Mesh zoneMesh, zonePreviewMesh;
        private MeshRenderer zoneRenderer, zonePreviewRenderer;
        private Material[] zoneMaterials;
        private Text zoneStatus;
        private TownZone activeZone;
        private bool zoneToolArmed, restoreZonePanelAfterDrag;
        private Vector2Int? zoneDragStart, zoneHoverCell;
        private RectInt? zoneDragBounds;
        private string zoneProblem;
        private TownZoneFacing zoneDragFrontage;
        private int residentialFacingTurns;
        private WildlifeWaterAccess zoneWater;
        private readonly Dictionary<Vector2Int, bool> zoneDragDryCells = new();

        internal TownZoneCell[] CaptureTownZones() => townZones.OrderBy(pair => pair.Key.y).ThenBy(pair => pair.Key.x)
            .Select(pair => new TownZoneCell
            {
                x = pair.Key.x, z = pair.Key.y, zone = pair.Value, wealth = zoneWealth.GetValueOrDefault(pair.Key), facing = residentialFacing.GetValueOrDefault(pair.Key)
            }).ToArray();

        private void RestoreTownZones(TownZoneCell[] cells, int gridVersion)
        {
            if (cells == null) return;
            int scale = gridVersion == 0 ? 2 : 1;
            foreach (var cell in cells)
                for (int z = 0; z < scale; z++)
                    for (int x = 0; x < scale; x++)
                    {
                        var key = new Vector2Int(cell.x * scale + x, cell.z * scale + z);
                        townZones.Add(key, cell.zone);
                        if (cell.wealth != LotWealth.None) zoneWealth.Add(key, cell.wealth);
                        if (cell.facing != TownZoneFacing.None) residentialFacing.Add(key, cell.facing);
                    }
        }

        internal static void ValidateTownZones(TownZoneCell[] cells, float mapSide, int gridVersion)
        {
            if (gridVersion < 0 || gridVersion > TownZoneGridVersion)
                throw new InvalidDataException("Unsupported town zoning grid.");
            if (cells == null) return;
            bool legacy = gridVersion == 0;
            int mapCells = Mathf.RoundToInt(mapSide / (legacy ? LegacyTownZoneCellMeters : DistrictGridCellMeters));
            if (cells.Length > (legacy ? LegacyMaxZonedCells : MaxZonedCells))
                throw new InvalidDataException("Too many zoned cells.");
            var seen = new HashSet<Vector2Int>();
            foreach (var cell in cells)
                if (cell.x < 0 || cell.x >= mapCells || cell.z < 0 || cell.z >= mapCells ||
                    cell.zone < TownZone.Residential1 || cell.zone > TownZone.Industrial3 ||
                    cell.wealth < LotWealth.None || cell.wealth > LotWealth.High ||
                    cell.facing < TownZoneFacing.None || cell.facing > TownZoneFacing.South ||
                    (cell.facing != TownZoneFacing.None && ResidentialDepth(cell.zone) == 0) ||
                    !seen.Add(new Vector2Int(cell.x, cell.z)))
                    throw new InvalidDataException("Invalid town zone cell.");
        }

        private void ShowZoning()
        {
            string[] names = { "Residential", "Commercial", "Industrial" };
            var concept = Resources.Load<Texture2D>("UI/TownConstruction/ZoneConcept");
            var artwork = new[]
            {
                new Rect(550, 560, 186, 105), new Rect(791, 560, 186, 105), new Rect(1031, 560, 186, 105)
            };
            for (int use = 0; use < names.Length; use++)
            {
                var card = FounderRect(townPanel, names[use] + " zoning", new Vector2(-178 + use * 231, -5),
                    new Vector2(219, 174));
                StyleTownPanel(card, new Color(.14f, .15f, .15f));
                if (concept)
                    TownArtwork(card, names[use] + " illustration", concept, artwork[use],
                        new Vector2(0, 28), new Vector2(186, 105));
                FounderText(card, names[use], new Vector2(0, -28), new Vector2(203, 26), 20).color = TownGold;
                for (int density = 1; density <= 3; density++)
                {
                    var zone = (TownZone)(use * 3 + density);
                    zoneButtons[(int)zone] = TownButton(card, lotFont, density.ToString(),
                        17 + (density - 1) * 63, -131, 58, () => ArmZone(zone));
                }
            }

            zoneButtons[0] = TownButton(townPanel, lotFont, "Erase zoning", 18, -113, 108, () => ArmZone(TownZone.None));
            zoneStatus = FounderText(townPanel, "Choose a zone and density, then drag on the map.",
                new Vector2(0, -112), new Vector2(800, 28), 16);
            zoneStatus.color = TownGold;
            EnsureZoneSurfaces();
            RefreshZoneOverlay();
        }

        private void ArmZone(TownZone zone)
        {
            CancelZoneDrag();
            SelectDistrictLot(null);
            activeZone = zone;
            residentialFacingTurns = 0;
            zoneToolArmed = true;
            for (int i = 0; i < zoneButtons.Length; i++) MarkTownButton(zoneButtons[i], i == (int)zone);
            zoneStatus.text = zone == TownZone.None ? "Drag over cells to erase their zoning." :
                ResidentialDepth(zone) > 0 ? ResidentialDragHint(zone) :
                "Drag to zone land. Drag over an existing zone to replace it.";
            townPanel.gameObject.SetActive(false);
        }

        private void CloseZoning()
        {
            CancelZoneDrag();
            zoneToolArmed = false;
            zoneStatus = null;
        }

        private void CancelZoneDrag()
        {
            if (restoreZonePanelAfterDrag && townPanel) townPanel.gameObject.SetActive(true);
            restoreZonePanelAfterDrag = false;
            zoneDragStart = null;
            zoneHoverCell = null;
            zoneDragBounds = null;
            zoneWater = null;
            zoneDragDryCells.Clear();
            if (zonePreviewRenderer) zonePreviewRenderer.enabled = false;
        }

        private bool UpdateZoning()
        {
            if (!Application.isFocused || !zoneToolArmed || !TownConstructionVisible || !CanBuildTown || IsSaving)
            {
                if (zoneDragStart.HasValue || zoneHoverCell.HasValue) CancelZoneDrag();
                return false;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!zoneDragStart.HasValue) return false;
                CancelZoneDrag();
                return true;
            }

            bool rotated = ResidentialDepth(activeZone) > 0 && Input.GetKeyDown(KeyCode.Tab);
            if (rotated)
            {
                residentialFacingTurns = (residentialFacingTurns + 1) % 4;
                if (!zoneDragStart.HasValue)
                    zoneStatus.text = ResidentialDragHint(activeZone);
            }

            bool down = Input.GetMouseButtonDown(0), up = Input.GetMouseButtonUp(0);
            var events = EventSystem.current;
            bool overUi = events && events.IsPointerOverGameObject();
            if (events && (down || up))
            {
                zoneUiHits.Clear();
                events.RaycastAll(new PointerEventData(events) { position = Input.mousePosition }, zoneUiHits);
                overUi = zoneUiHits.Count > 0;
            }
            if (!overUi && TryZoneCell(out var cell))
            {
                if (down || zoneDragStart.HasValue) ProcessZoneDrag(cell, down, up, rotated);
                else if (zoneHoverCell != cell)
                {
                    zoneHoverCell = cell;
                    FillZoneMesh(zonePreviewMesh, PreviewZoneCells(new RectInt(cell, Vector2Int.one), activeZone), .27f);
                    zonePreviewRenderer.enabled = true;
                }
            }
            else if (zoneHoverCell.HasValue && !zoneDragStart.HasValue) CancelZoneDrag();
            else if (rotated && zoneDragBounds is RectInt preview) RefreshZonePreview(preview);
            if (zoneDragStart.HasValue && (up || (!down && !Input.GetMouseButton(0)))) CancelZoneDrag();
            return down || up || zoneDragStart.HasValue;
        }

        private void ProcessZoneDrag(Vector2Int cell, bool down, bool up, bool rotated)
        {
            if (down)
            {
                ShowDistrictMessage("");
                zoneHoverCell = null;
                zoneDragStart = cell;
                restoreZonePanelAfterDrag = townPanel && townPanel.gameObject.activeSelf;
                if (restoreZonePanelAfterDrag) townPanel.gameObject.SetActive(false);
                zoneWater = DistrictLotWater();
            }
            if (zoneDragStart is not Vector2Int start) return;
            var min = Vector2Int.Min(start, cell);
            var max = Vector2Int.Max(start, cell) + Vector2Int.one;
            var bounds = new RectInt(min, max - min);
            if (zoneDragBounds != bounds)
            {
                zoneDragBounds = bounds;
                zoneProblem = ZoneSiteProblem(bounds, activeZone, zoneWater, out zoneDragFrontage, zoneDragDryCells);
                RefreshZonePreview(bounds);
            }
            else if (rotated) RefreshZonePreview(bounds);
            if (!up) return;
            var problem = zoneProblem ?? ApplyTownZone(bounds, activeZone);
            if (problem == null)
            {
                restoreZonePanelAfterDrag = false;
                zoneStatus.text = "Drag to continue zoning · Esc to exit";
            }
            else
            {
                zoneStatus.text = problem;
                ShowDistrictMessage(problem);
            }
        }

        private void RefreshZonePreview(RectInt bounds)
        {
            bool residential = ResidentialDepth(activeZone) > 0;
            bool enclosed = residential && zoneProblem == null && zoneDragFrontage == TownZoneFacing.None;
            string action = enclosed ? "Faces surrounding streets · Release to apply" : activeZone == TownZone.None ? "Release to erase" :
                residential ? "Tab to rotate · Release to apply" : "Release to apply";
            zoneStatus.text = zoneProblem ?? $"{bounds.width} × {bounds.height} cells · {action}";
            zonePreviewRenderer.enabled = residential
                ? zoneProblem == null || (long)bounds.width * bounds.height <= MaxZoneDragSide * MaxZoneDragSide
                : bounds.width <= MaxZoneDragSide && bounds.height <= MaxZoneDragSide;
            if (zonePreviewRenderer.enabled)
                FillZoneMesh(zonePreviewMesh, PreviewZoneCells(bounds, zoneProblem == null ? activeZone : TownZone.None),
                    .27f, zoneProblem == null ? ResidentialFacing(zoneDragFrontage) : TownZoneFacing.None,
                    enclosed ? bounds : (RectInt?)null);
        }

        private bool TryZoneCell(out Vector2Int cell)
        {
            cell = default;
            var ground = ApprovedDistrict && ApprovedDistrict.DisplayTerrain ? ApprovedDistrict.DisplayTerrain : terrain;
            var collider = ground ? ground.GetComponent<TerrainCollider>() : null;
            var camera = GetComponentInChildren<Camera>();
            if (!collider || !camera || !collider.Raycast(camera.ScreenPointToRay(Input.mousePosition), out var hit, camera.farClipPlane))
                return false;
            cell = DistrictGridCell(new Vector2(hit.point.x, hit.point.z));
            return InActiveDistrict(ZoneCorner(cell) + Vector2.one * (DistrictGridCellMeters * .5f));
        }

        private string ZoneSiteProblem(RectInt bounds, TownZone zone, WildlifeWaterAccess water, out TownZoneFacing frontage,
            Dictionary<Vector2Int, bool> dryCells = null)
        {
            frontage = TownZoneFacing.None;
            if (bounds.width < 1 || bounds.height < 1) return "Select at least one cell.";
            int maxDepth = ResidentialDepth(zone);
            if (maxDepth > 0)
            {
                if ((long)bounds.width * bounds.height > MaxZonedCells) return "This region has reached its zoning limit.";
                var problem = ResidentialFrontageProblem(bounds, maxDepth, out frontage, allowEnclosedBlock: true);
                if (problem != null) return problem;
            }
            else if (bounds.width > MaxZoneDragSide || bounds.height > MaxZoneDragSide)
                return $"Zone up to {MaxZoneDragSide} × {MaxZoneDragSide} cells at once.";
            var selection = new DistrictLotFootprint(
                (ZoneCorner(bounds.min) + ZoneCorner(bounds.max)) * .5f,
                (Vector2)bounds.size * (DistrictGridCellMeters * .5f));
            var nearbyLots = zone == TownZone.None ? Array.Empty<DistrictLotFootprint>() : PlacedLots
                .Select(lot => new DistrictLotFootprint(lot)).Where(lot => lot.Overlaps(selection)).ToArray();
            int count = townZones.Count;
            foreach (var cell in bounds.allPositionsWithin)
            {
                var center = ZoneCorner(cell) + Vector2.one * (DistrictGridCellMeters * .5f);
                if (!InActiveDistrict(center)) return "Keep zoning inside the active district.";
                if (zone == TownZone.None) continue;
                if (!townZones.ContainsKey(cell) && ++count > MaxZonedCells) return "This region has reached its zoning limit.";
                var footprint = new DistrictLotFootprint(center, Vector2.one * (DistrictGridCellMeters * .5f));
                foreach (var lot in nearbyLots)
                    if (footprint.Overlaps(lot)) return "Keep zoning clear of placed lots.";
                if (dryCells != null && dryCells.TryGetValue(cell, out bool dry))
                {
                    if (!dry) return "Zone dry land.";
                    continue;
                }
                var lower = ZoneCorner(cell);
                for (int z = 0; z < 3; z++)
                for (int x = 0; x < 3; x++)
                {
                    const float inset = .2f;
                    const float step = (DistrictGridCellMeters - 2 * inset) * .5f;
                    var point = lower + new Vector2(inset + step * x, inset + step * z);
                    if (water != null ? water.Wet(point, out _, out _) : !IsTownLand(point / MapSide + Vector2.one * .5f))
                    {
                        if (dryCells != null) dryCells[cell] = false;
                        return "Zone dry land.";
                    }
                }
                if (dryCells != null) dryCells[cell] = true;
            }
            return null;
        }

        private string ApplyTownZone(RectInt bounds, TownZone zone)
        {
            if (!CanBuildTown || IsSaving) return "Establish the town center before zoning.";
            var problem = ZoneSiteProblem(bounds, zone, DistrictLotWater(), out var frontage);
            if (problem != null) return problem;
            var facing = ResidentialFacing(frontage);
            bool enclosed = ResidentialDepth(zone) > 0 && frontage == TownZoneFacing.None;
            bool affectsGrowth = zone == TownZone.Residential1;
            foreach (var cell in bounds.allPositionsWithin)
            {
                affectsGrowth |= townZones.TryGetValue(cell, out var previous) && previous == TownZone.Residential1;
                if (zone == TownZone.None)
                {
                    townZones.Remove(cell);
                    zoneWealth.Remove(cell);
                }
                else
                {
                    townZones[cell] = zone;
                    zoneWealth[cell] = selectedZoneWealth;
                }
                if (enclosed) residentialFacing[cell] = EnclosedBlockFacing(bounds, cell);
                else if (facing != TownZoneFacing.None) residentialFacing[cell] = facing;
                else residentialFacing.Remove(cell);
            }
            if (affectsGrowth) QueueZoningGrowth(bounds);
            RefreshZoneOverlay();
            return null;
        }

        private Vector2 ZoneCorner(Vector2Int cell) =>
            (Vector2)cell * DistrictGridCellMeters - Vector2.one * (MapSide * .5f);

        private void EnsureZoneSurfaces()
        {
            if (zoneRenderer) return;
            zoneMaterials = ZoneColors.Select(color =>
            {
                var material = Own(new Material(Shader.Find("Universal Render Pipeline/Unlit")));
                color.a = .48f;
                material.SetColor("_BaseColor", color);
                LotGroundTexture.ConfigureTransparentLayer(material, 1);
                return material;
            }).Append(CreateArrowMaterial()).ToArray();
            zoneRenderer = CreateSurface("District zoning", out zoneMesh);
            zonePreviewRenderer = CreateSurface("Zoning drag preview", out zonePreviewMesh);
            zonePreviewRenderer.enabled = false;
            var previewMaterials = (Material[])zoneMaterials.Clone();
            // Draw the drag over saved arrows, including when the selected cells will be erased.
            StylePreview(TownZone.None, new Color(.92f, .18f, .16f, .92f));
            for (int index = 1; index < ZoneColors.Length; index++)
                StylePreview((TownZone)index, ZoneColors[index]);
            previewMaterials[^1] = Own(new Material(zoneMaterials[^1]));
            LotGroundTexture.ConfigureTransparentLayer(previewMaterials[^1], 4);
            zonePreviewRenderer.sharedMaterials = previewMaterials;

            void StylePreview(TownZone zone, Color color)
            {
                int index = (int)zone;
                var material = Own(new Material(zoneMaterials[index]));
                material.SetColor("_BaseColor", color);
                LotGroundTexture.ConfigureTransparentLayer(material, 3);
                previewMaterials[index] = material;
            }

            MeshRenderer CreateSurface(string title, out Mesh mesh)
            {
                var go = new GameObject(title, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                mesh = Own(new Mesh { name = title, indexFormat = IndexFormat.UInt32 });
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = zoneMaterials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                return renderer;
            }
        }

        private void RefreshZoneOverlay()
        {
            if (!zoneRenderer && (SelectedDistrict < 0 || townZones.Count == 0)) return;
            EnsureZoneSurfaces();
            zoneRenderer.enabled = SelectedDistrict >= 0;
            if (!zoneRenderer.enabled) return;

            var districtMembers = ActiveDistrictMembers;
            var developed = PlacedLots.Where(lot => districtMembers.Contains(lot.district))
                .Select(lot => new DistrictLotFootprint(lot)).ToArray();
            FillZoneMesh(zoneMesh, townZones.Where(pair =>
            {
                var center = ZoneCorner(pair.Key) + Vector2.one * (DistrictGridCellMeters * .5f);
                return InActiveDistrict(center) && !developed.Any(lot => lot.Contains(center, .001f));
            }), .15f);
        }

        private void FillZoneMesh(Mesh mesh, IEnumerable<KeyValuePair<Vector2Int, TownZone>> cells, float lift,
            TownZoneFacing previewFacing = TownZoneFacing.None, RectInt? enclosedBlock = null)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>[ZoneColors.Length + 1];
            for (int i = 0; i < triangles.Length; i++) triangles[i] = new List<int>();
            const float inset = .09f;
            float edge = DistrictGridCellMeters - inset;
            foreach (var cell in cells)
            {
                var corner = ZoneCorner(cell.Key);
                int first = vertices.Count;
                Add(inset, inset);
                Add(inset, edge);
                Add(edge, edge);
                Add(edge, inset);
                Add(DistrictGridCellMeters * .5f, DistrictGridCellMeters * .5f);
                var indices = triangles[(int)cell.Value];
                for (int side = 0; side < 4; side++)
                {
                    indices.Add(first + side);
                    indices.Add(first + (side + 1) % 4);
                    indices.Add(first + 4);
                }

                if (ResidentialDepth(cell.Value) > 0)
                {
                    var facing = enclosedBlock is RectInt block ? EnclosedBlockFacing(block, cell.Key) :
                        mesh == zonePreviewMesh ? previewFacing : residentialFacing.GetValueOrDefault(cell.Key);
                    if (facing != TownZoneFacing.None)
                        AddResidentialArrow(vertices, triangles[^1], corner + Vector2.one * (DistrictGridCellMeters * .5f),
                            facing, lift + .045f);
                }

                void Add(float x, float z)
                {
                    var point = corner + new Vector2(x, z);
                    vertices.Add(new Vector3(point.x, LotHeight(point) + lift, point.y));
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.subMeshCount = triangles.Length;
            for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i, false);
            mesh.RecalculateBounds();
        }
        private static IEnumerable<KeyValuePair<Vector2Int, TownZone>> PreviewZoneCells(RectInt bounds, TownZone zone)
        {
            foreach (var cell in bounds.allPositionsWithin) yield return new KeyValuePair<Vector2Int, TownZone>(cell, zone);
        }
    }
}

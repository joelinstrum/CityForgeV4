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
    }

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

        private readonly Dictionary<Vector2Int, TownZone> townZones = new();
        private readonly Button[] zoneButtons = new Button[10];
        private readonly List<RaycastResult> zoneUiHits = new();
        private Mesh zoneMesh, zonePreviewMesh;
        private MeshRenderer zoneRenderer, zonePreviewRenderer;
        private Material[] zoneMaterials;
        private Text zoneStatus;
        private TownZone activeZone;
        private bool zoneToolArmed;
        private Vector2Int? zoneDragStart;
        private RectInt? zoneDragBounds;
        private string zoneProblem;
        private WildlifeWaterAccess zoneWater;

        internal TownZoneCell[] CaptureTownZones() => townZones.OrderBy(pair => pair.Key.y).ThenBy(pair => pair.Key.x)
            .Select(pair => new TownZoneCell { x = pair.Key.x, z = pair.Key.y, zone = pair.Value }).ToArray();

        private void RestoreTownZones(TownZoneCell[] cells, int gridVersion)
        {
            if (cells == null) return;
            int scale = gridVersion == 0 ? 2 : 1;
            foreach (var cell in cells)
                for (int z = 0; z < scale; z++)
                    for (int x = 0; x < scale; x++)
                        townZones.Add(new Vector2Int(cell.x * scale + x, cell.z * scale + z), cell.zone);
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
            zoneRenderer.enabled = true;
        }

        private void ArmZone(TownZone zone)
        {
            CancelZoneDrag();
            SelectDistrictLot(null);
            activeZone = zone;
            zoneToolArmed = true;
            for (int i = 0; i < zoneButtons.Length; i++) MarkTownButton(zoneButtons[i], i == (int)zone);
            zoneStatus.text = zone == TownZone.None ? "Drag over cells to erase their zoning." :
                "Drag to zone land. Drag over an existing zone to replace it.";
        }

        private void CloseZoning()
        {
            CancelZoneDrag();
            zoneToolArmed = false;
            zoneStatus = null;
            if (zoneRenderer) zoneRenderer.enabled = false;
        }

        private void CancelZoneDrag()
        {
            zoneDragStart = null;
            zoneDragBounds = null;
            zoneWater = null;
            if (zonePreviewRenderer) zonePreviewRenderer.enabled = false;
        }

        private bool UpdateZoning()
        {
            if (!zoneToolArmed || !TownConstructionVisible || !CanBuildTown || IsSaving)
            {
                if (zoneDragStart.HasValue) CancelZoneDrag();
                return false;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!zoneDragStart.HasValue) return false;
                CancelZoneDrag();
                return true;
            }

            bool down = Input.GetMouseButtonDown(0), up = Input.GetMouseButtonUp(0);
            if (!down && !zoneDragStart.HasValue) return false;
            var events = EventSystem.current;
            bool overUi = events && events.IsPointerOverGameObject();
            if (events && (down || up))
            {
                zoneUiHits.Clear();
                events.RaycastAll(new PointerEventData(events) { position = Input.mousePosition }, zoneUiHits);
                overUi = zoneUiHits.Count > 0;
            }
            if (!overUi && TryZoneCell(out var cell)) ProcessZoneDrag(cell, down, up);
            if (up || (!down && !Input.GetMouseButton(0))) CancelZoneDrag();
            return down || up || zoneDragStart.HasValue;
        }

        private void ProcessZoneDrag(Vector2Int cell, bool down, bool up)
        {
            if (down)
            {
                zoneDragStart = cell;
                zoneWater = DistrictLotWater();
            }
            if (zoneDragStart is not Vector2Int start) return;
            var min = Vector2Int.Min(start, cell);
            var max = Vector2Int.Max(start, cell) + Vector2Int.one;
            var bounds = new RectInt(min, max - min);
            if (!zoneDragBounds.HasValue || zoneDragBounds.Value != bounds)
            {
                zoneDragBounds = bounds;
                zoneProblem = ZoneSiteProblem(bounds, activeZone, zoneWater);
                zoneStatus.text = zoneProblem ?? $"{bounds.width} × {bounds.height} cells · Release to apply";
                zonePreviewRenderer.enabled = bounds.width <= MaxZoneDragSide && bounds.height <= MaxZoneDragSide;
                if (zonePreviewRenderer.enabled)
                    FillZoneMesh(zonePreviewMesh, PreviewZoneCells(bounds, zoneProblem == null ? activeZone : TownZone.None), .27f);
            }
            if (up && zoneProblem == null)
                zoneStatus.text = ApplyTownZone(bounds, activeZone) ?? "Zoning updated. Save the region to keep it.";
        }

        private bool TryZoneCell(out Vector2Int cell)
        {
            cell = default;
            var ground = ApprovedDistrict && ApprovedDistrict.DisplayTerrain ? ApprovedDistrict.DisplayTerrain : terrain;
            var collider = ground ? ground.GetComponent<TerrainCollider>() : null;
            var camera = GetComponentInChildren<Camera>();
            if (!collider || !camera || !collider.Raycast(camera.ScreenPointToRay(Input.mousePosition), out var hit, camera.farClipPlane))
                return false;
            float half = MapSide * .5f, tile = DistrictGridCellMeters;
            cell = new Vector2Int(Mathf.FloorToInt((hit.point.x + half) / tile), Mathf.FloorToInt((hit.point.z + half) / tile));
            return true;
        }

        private string ZoneSiteProblem(RectInt bounds, TownZone zone, WildlifeWaterAccess water)
        {
            if (bounds.width < 1 || bounds.height < 1 || bounds.width > MaxZoneDragSide || bounds.height > MaxZoneDragSide)
                return $"Zone up to {MaxZoneDragSide} × {MaxZoneDragSide} cells at once.";
            int count = townZones.Count;
            foreach (var cell in bounds.allPositionsWithin)
            {
                var center = ZoneCorner(cell) + Vector2.one * (DistrictGridCellMeters * .5f);
                if (!InActiveDistrict(center)) return "Keep zoning inside the active district.";
                if (zone == TownZone.None) continue;
                if (!townZones.ContainsKey(cell) && ++count > MaxZonedCells) return "This region has reached its zoning limit.";
                var footprint = new DistrictLotFootprint(center, Vector2.one * (DistrictGridCellMeters * .5f));
                foreach (var lot in PlacedLots)
                    if (footprint.Overlaps(new DistrictLotFootprint(lot))) return "Keep zoning clear of placed lots.";
                var lower = ZoneCorner(cell);
                for (int z = 0; z < 3; z++)
                for (int x = 0; x < 3; x++)
                {
                    const float inset = .2f;
                    const float step = (DistrictGridCellMeters - 2 * inset) * .5f;
                    var point = lower + new Vector2(inset + step * x, inset + step * z);
                    if (water != null ? water.Wet(point, out _, out _) : !IsTownLand(point / MapSide + Vector2.one * .5f))
                        return "Zone dry land.";
                }
            }
            return null;
        }

        private string ApplyTownZone(RectInt bounds, TownZone zone)
        {
            if (!CanBuildTown || IsSaving) return "Establish the town center before zoning.";
            var problem = ZoneSiteProblem(bounds, zone, DistrictLotWater());
            if (problem != null) return problem;
            foreach (var cell in bounds.allPositionsWithin)
            {
                if (zone == TownZone.None) townZones.Remove(cell);
                else townZones[cell] = zone;
            }
            if (zoneRenderer) RefreshZoneOverlay();
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
            }).ToArray();
            zoneRenderer = CreateSurface("District zoning", out zoneMesh);
            zonePreviewRenderer = CreateSurface("Zoning drag preview", out zonePreviewMesh);
            zonePreviewRenderer.enabled = false;

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

        private void RefreshZoneOverlay() => FillZoneMesh(zoneMesh, townZones.Where(pair =>
            InActiveDistrict(ZoneCorner(pair.Key) + Vector2.one * (DistrictGridCellMeters * .5f))), .15f);

        private void FillZoneMesh(Mesh mesh, IEnumerable<KeyValuePair<Vector2Int, TownZone>> cells, float lift)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>[ZoneColors.Length];
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

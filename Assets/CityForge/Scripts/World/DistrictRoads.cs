using System;
using System.Collections.Generic;
using System.IO;
using CityForge.CameraSystem;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.World
{
    [Serializable]
    public sealed class DistrictRoadRecord
    {
        public int district;
        public string surfaceId;
        public Vector2[] points;
        public bool closed;
        public bool curved;
        public bool gridAligned;
        public int width;
    }

    public sealed partial class PrototypeRegionView
    {
        // Stable IDs are persisted. The popup and placement tool read the same surface catalog.
        private static readonly (string Id, string Label, string Material, string Description)[] RoadSurfaces =
        {
            ("dirt", "Dirt Road", "packed-earth", "An unpaved earth road for simple overland connections."),
            ("wagon", "Wagon Road", "packed-earth", "A rural road with wheel ruts from wagon traffic."),
            ("gravel", "Gravel Road", "gravel", "A compacted gravel surface for rural connections."),
            ("macadam", "Macadam Road", "gravel", "A road built up from layers of crushed stone."),
            ("cobblestone", "Cobblestone Road", "cobblestone", "A road paved with rounded stone setts."),
            ("brick", "Brick Road", "brick", "A road paved with laid brick."),
            ("blacktop", "Blacktop Road", "concrete", "An asphalt surface for motor traffic."),
            ("paved-street", "Paved Street", "concrete", "A paved street for built-up districts.")
        };

        private const int HistoricalRoadSurfaceCount = 6;
        private const int PavedStreetIndex = HistoricalRoadSurfaceCount + 1;
        private const int RoadWidth = 3;
        private static readonly int DirtEndFadeProperty = Shader.PropertyToID("_DirtEndFade");
        private MaterialPropertyBlock dirtEndProperties;
        private const float RoadSurfaceLift = .13f;
        private const int MaxDistrictRoads = 256;
        private const int MaxRoadPoints = 64;
        private const float MaxRoadLength = 1024f;
        private readonly List<DistrictRoadRecord> districtRoads = new();
        private readonly List<UnityEngine.Object> roadPresentationAssets = new();
        private readonly Dictionary<string, Material> districtRoadMaterials = new();
        private readonly Dictionary<DistrictRoadRecord, MeshFilter> districtRoadMeshes = new();
        private Transform districtRoadRoot;
        private PointWireTool roadWire;
        private RectTransform roadControls;
        private Button finishRoadButton;
        private int roadSurfaceIndex;
        private int? lastRepaveSurface;
        private DistrictRoadRecord repaveTarget;
        private DistrictRoadRecord roadExtensionHint;

        private bool RoadPlacementArmed => roadWire && roadWire.Armed;
        private Terrain RoadTerrain => ApprovedDistrict && ApprovedDistrict.DisplayTerrain
            ? ApprovedDistrict.DisplayTerrain : terrain;
        private bool CanPlaceRoad => CanEditRoad && TownConstructionVisible;
        private bool CanEditRoad => CanBuildTown && !IsSaving && RoadTerrain &&
            !playerLotWorkspace && !founderPicker && !returning && !RemakeBlocksInput;

        internal DistrictRoadRecord[] CaptureDistrictRoads() => districtRoads.ToArray();

        private void RestoreDistrictRoads(DistrictRoadRecord[] records)
        {
            if (records != null) districtRoads.AddRange(records);
        }

        internal static void ValidateDistrictRoads(DistrictRoadRecord[] records, int districtCount, float mapSide)
        {
            if (records == null) return;
            if (records.Length > MaxDistrictRoads) throw new InvalidDataException("Too many district roads.");
            foreach (var record in records)
            {
                if (record == null || record.district < 0 || record.district >= districtCount ||
                    FindRoadSurface(record.surfaceId) < 0 || Array.IndexOf(LotPathSurface.Widths, record.width) < 0 ||
                    RoadGuideProblem(record.points, record.closed, mapSide * .5f) != null)
                    throw new InvalidDataException("Invalid district road.");
            }
        }

        private static int FindRoadSurface(string id) => Array.FindIndex(RoadSurfaces, surface => surface.Id == id);

        private static string RoadGuideProblem(IReadOnlyList<Vector2> points, bool closed, float halfMap)
        {
            if (points == null || points.Count < (closed ? 3 : 2) || points.Count > MaxRoadPoints)
                return $"Use {(closed ? 3 : 2)}–{MaxRoadPoints} markers per road.";
            float length = 0;
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (!float.IsFinite(point.x) || !float.IsFinite(point.y) ||
                    Mathf.Abs(point.x) > halfMap || Mathf.Abs(point.y) > halfMap)
                    return "Keep the road inside the map.";
                if (i == 0 && !closed) continue;
                float segment = Vector2.Distance(point, points[(i + points.Count - 1) % points.Count]);
                if (segment < 1f) return "Space road markers at least one meter apart.";
                length += segment;
            }
            return length > MaxRoadLength ? $"Place up to {MaxRoadLength:N0} meters per road." : null;
        }

        private bool snapNewRoadsToTileCenters = true;

        private void StartRoadPlacement(int surfaceIndex)
        {
            if (!CanPlaceRoad) return;
            CloseTownSection();
            SetRoadPaths(false);
            CancelLotPlacement();
            SelectDistrictLot(null);
            if (!roadWire)
            {
                var hud = townToolbar.parent;
                roadWire = gameObject.AddComponent<PointWireTool>();
                var districtCamera = GetComponentInChildren<CityForgeCameraController>().GetComponent<Camera>();
                roadWire.Initialize(districtCamera, hud, new RoadWireSurface(this),
                    new Color(1f, .72f, .30f));
                roadWire.OpenPath = true;
                roadWire.RibbonFillColor = new Color(.50f, .52f, .55f);
                roadWire.HandleEscape = false;
                roadWire.RestrictUiToHud = true;
                roadWire.FinishOverUi = true;
                roadWire.AllowClosedUndo = false;
                roadWire.SnapPoint = SnapRoadPlacementPoint;
                roadWire.StartPointHovered += HighlightRoadStartTile;
                roadWire.CompletionRequested = points => PlaceDistrictRoad(points, false);
                roadWire.LoopCompletionRequested = points => PlaceDistrictRoad(points, true);
                roadWire.RepeatOnCompletion = true;

                roadControls = FounderRect(hud, "Road placement controls", new Vector2(0, 161), new Vector2(242, 40));
                roadControls.anchorMin = roadControls.anchorMax = roadControls.pivot = new Vector2(.5f, 0);
                finishRoadButton = TownButton(roadControls, lotFont, "Place Road", 0, 0, 130, FinishRoad);
                TownButton(roadControls, lotFont, "Cancel", 138, 0, 104, CancelRoadPlacement);
            }

            roadSurfaceIndex = surfaceIndex;
            lastRepaveSurface = surfaceIndex;
            string label = RoadSurfaces[surfaceIndex].Label;
            roadWire.Label = label;
            roadWire.ProjectGuide = ProjectRoadPlacementGuide;
            RefreshRoadPlacementHint(force: true);
            roadWire.Arm();
            roadControls.gameObject.SetActive(roadWire.Armed);
            finishRoadButton.interactable = false;
        }

        private bool UpdateRoadPlacement()
        {
            if (!RoadPlacementArmed)
            {
                if (roadControls && roadControls.gameObject.activeSelf) roadControls.gameObject.SetActive(false);
                return false;
            }
            if (!CanPlaceRoad)
            {
                CancelRoadPlacement();
                return true;
            }

            RefreshRoadPlacementHint();
            finishRoadButton.interactable = roadWire.Points.Count >= 2;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (roadWire.Points.Count >= 2) FinishRoad();
                else CancelRoadPlacement();
            }
            return true;
        }

        private void FinishRoad()
        {
            if (RoadPlacementArmed) roadWire.CompletePath();
        }

        private void CancelRoadPlacement()
        {
            if (roadWire) roadWire.Cancel();
            if (roadControls) roadControls.gameObject.SetActive(false);
        }

        private string PlaceDistrictRoad(IReadOnlyList<Vector2> points, bool closed)
        {
            if (!CanPlaceRoad) return "Road placement is unavailable.";
            if (points.Count > 0 && FindRoadExtensionEnd(points[0], .001f, out bool prepend) is { } road)
                return ExtendDistrictRoad(road, prepend, points, closed);
            if (districtRoads.Count >= MaxDistrictRoads) return "This region has reached its road limit.";
            var problem = RoadGuideProblem(points, closed, MapSide * .5f);
            if (problem != null) return problem;

            var record = new DistrictRoadRecord
            {
                district = SelectedDistrict,
                surfaceId = RoadSurfaces[roadSurfaceIndex].Id,
                points = new List<Vector2>(points).ToArray(),
                closed = closed,
                width = RoadWidth
            };
            var mesh = BuildDistrictRoadMesh(record);
            bool committed = false;
            try
            {
                problem = RoadSiteProblem(mesh);
                if (problem != null) return problem;
                PresentDistrictRoad(record, mesh, districtRoads.Count);
                districtRoads.Add(record);
                committed = true;
                RefreshRoadShoulderDetails();
                RefreshDirtRoadEndFades();
                RefreshDirtJunctionWear();
                return null;
            }
            finally
            {
                if (!committed) Destroy(mesh);
            }
        }

        private void RefreshRoadPlacementHint(bool force = false)
        {
            var road = roadWire.Points.Count > 0 ? FindRoadExtensionEnd(roadWire.Points[0], .001f, out _) : null;
            if (!force && roadExtensionHint == road) return;
            roadExtensionHint = road;
            roadWire.RibbonWidth = road?.width ?? RoadWidth;
            roadWire.DrawingHint = road == null
                ? roadWire.Label + " · Click an existing endpoint to continue that road, or click open ground to start a road\n" +
                    "Click the first marker to close a loop · Enter or Esc to place · Backspace to undo"
                : "Continue " + RoadSurfaces[FindRoadSurface(road.surfaceId)].Label +
                    " · Click to add markers · Enter or Esc to finish · Backspace to undo";
            roadWire.SetMessage(roadWire.DrawingHint);
            finishRoadButton.GetComponentInChildren<Text>().text = road == null ? "Place Road" : "Extend Road";
        }

        private Vector2 SnapRoadPlacementPoint(Vector2 point)
        {
            if (roadWire.Points.Count == 0)
            {
                var road = FindRoadExtensionEnd(point, 1.25f, out bool prepend);
                if (road != null) return prepend ? road.points[0] : road.points[^1];
            }
            else if (FindRoadExtensionEnd(roadWire.Points[0], .001f, out _) != null)
            {
                return point;
            }

            if (!snapNewRoadsToTileCenters) return point;
            float origin = -MapSide * .5f;
            return new Vector2(TileCenter(point.x), TileCenter(point.y));

            float TileCenter(float coordinate) => origin +
                (Mathf.Floor((coordinate - origin) / DistrictGridCellMeters) + .5f) * DistrictGridCellMeters;
        }

        private DistrictRoadRecord FindRoadExtensionEnd(Vector2 point, float radius, out bool prepend)
        {
            DistrictRoadRecord nearest = null;
            prepend = false;
            float best = radius * radius;
            foreach (var road in districtRoadMeshes.Keys)
            {
                if (road.closed) continue;
                float start = (road.points[0] - point).sqrMagnitude;
                float end = (road.points[^1] - point).sqrMagnitude;
                float distance = Mathf.Min(start, end);
                if (distance >= best) continue;
                best = distance;
                nearest = road;
                prepend = start <= end;
            }
            return nearest;
        }

        private IReadOnlyList<Vector2> ProjectRoadPlacementGuide(IReadOnlyList<Vector2> points, bool closed)
        {
            if (points.Count < 2) return points;
            var road = FindRoadExtensionEnd(points[0], .001f, out bool prepend);
            if (road == null || !road.gridAligned) return points;
            var combined = BuildRoadExtensionGuide(road, prepend, points);
            var preview = new Vector2[points.Count];
            for (int i = 0; i < preview.Length; i++)
                preview[i] = combined[prepend ? preview.Length - 1 - i : road.points.Length - 1 + i];
            return preview;
        }

        private string ExtendDistrictRoad(DistrictRoadRecord road, bool prepend,
            IReadOnlyList<Vector2> points, bool closed)
        {
            if (closed) return "Finish the road extension with Enter.";
            if (points.Count < 2) return "Add at least one waypoint beyond the endpoint.";
            var end = prepend ? road.points[0] : road.points[^1];
            var interior = prepend ? road.points[1] : road.points[^2];
            var outgoing = points[1] - end;
            if (Vector2.Dot((interior - end).normalized, outgoing.normalized) > .995f)
                return "Extend away from the endpoint to avoid doubling back over the road.";
            return CommitRoadEdit(road, BuildRoadExtensionGuide(road, prepend, points), road.curved, road.gridAligned);
        }

        private Vector2[] BuildRoadExtensionGuide(DistrictRoadRecord road, bool prepend, IReadOnlyList<Vector2> points)
        {
            var combined = new Vector2[road.points.Length + points.Count - 1];
            int added = points.Count - 1;
            Array.Copy(road.points, 0, combined, prepend ? added : 0, road.points.Length);
            for (int i = 1; i < points.Count; i++)
                combined[prepend ? added - i : road.points.Length + i - 1] = points[i];
            if (!road.gridAligned) return combined;
            // Establish existing runs first, even when adding to the beginning of the road.
            if (prepend) Array.Reverse(combined);
            var aligned = AlignRoadGuide(combined, false, road.width);
            if (prepend) Array.Reverse(aligned);
            return aligned;
        }

        private string RoadSiteProblem(Mesh mesh)
        {
            var water = DistrictLotWater();
            var vertices = mesh.vertices;
            foreach (var vertex in vertices)
            {
                var point = new Vector2(vertex.x, vertex.z);
                if (!InActiveDistrict(point)) return "Keep the full road width inside the district.";
                // The mesh samples the full ribbon already. Check actual water levels at those
                // samples so near-sea-level harbor ground can be used up to the lot edge.
                float height = LotHeight(point);
                if ((!Waterless && height <= SeaLevel) ||
                    (water != null
                        ? water.Wet(point, height, out _, out _, surfaceTolerance: 0f)
                        : TryLotWater(point, height, null, out _)))
                    return "Keep the full road width on dry ground.";
                if (InCampClearing(point)) return "Keep the road clear of camps.";

            }
            return RoadHitsBuilding(mesh, vertices) ? "Keep the road clear of buildings." : null;
        }

        private Mesh BuildDistrictRoadMesh(DistrictRoadRecord record)
        {
            var style = LotPathMaterial.Find(RoadSurfaces[FindRoadSurface(record.surfaceId)].Material);
            var path = new LotPathDefinition
            {
                points = record.points,
                closed = record.closed,
                curved = record.curved,
                width = record.width
            };
            return LotPathSurface.BuildMesh(path, style.TileSize, RoadSurfaceLift, LotHeight);
        }

        private void PresentDistrictRoad(DistrictRoadRecord record, Mesh mesh, int order)
        {
            if (!districtRoadMaterials.TryGetValue(record.surfaceId, out var material))
            {
                material = LotPathMaterial.Find(RoadSurfaces[FindRoadSurface(record.surfaceId)].Material).CreateMaterial();
                if (record.surfaceId == "blacktop")
                    material.SetColor("_BaseColor", new Color(.27f, .28f, .30f));
                LotPathMaterial.ApplyEdgeTreatment(material, new LotPathDefinition { width = RoadWidth }, 8, 0);
                if (record.surfaceId == "dirt" && material.HasProperty("_RoadWearMap"))
                {
                    const string folder = "Lots/PathMaterials/";
                    var earth = Resources.Load<Texture2D>(folder + "DirtRoadEarth");
                    var wear = Resources.Load<Texture2D>(folder + "DirtRoadWear");
                    var shoulder = Resources.Load<Texture2D>(folder + "DirtRoadShoulder");
                    if (earth && wear && shoulder)
                    {
                        material.SetTexture("_BaseMap", earth);
                        material.SetTexture("_RoadWearMap", wear);
                        material.SetTexture("_RoadShoulderMask", shoulder);
                        material.EnableKeyword("_ROAD_WEAR");
                    }
                }
                if (record.surfaceId == "wagon" && material.HasProperty("_WagonMask"))
                {
                    var albedo = Resources.Load<Texture2D>("Lots/PathMaterials/WagonRoadAlbedo");
                    var mask = Resources.Load<Texture2D>("Lots/PathMaterials/WagonRoadMask");
                    if (albedo && mask)
                    {
                        material.SetTexture("_BaseMap", albedo);
                        material.SetTexture("_WagonMask", mask);
                        material.SetColor("_BaseColor", new Color(.83f, .78f, .72f, 1f));
                        material.SetFloat("_Smoothness", .03f);
                        material.EnableKeyword("_WAGON_SURFACE");
                    }
                }
                districtRoadMaterials.Add(record.surfaceId, material);
                roadPresentationAssets.Add(material);
            }
            if (!districtRoadRoot)
            {
                districtRoadRoot = new GameObject("Placed district roads").transform;
                districtRoadRoot.SetParent(transform, false);
            }
            var road = new GameObject(record.surfaceId + " road", typeof(MeshFilter), typeof(MeshRenderer));
            road.transform.SetParent(districtRoadRoot, false);
            var filter = road.GetComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            districtRoadMeshes.Add(record, filter);
            var renderer = road.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            renderer.enabled = !showRoadPaths;
            if (showRoadPaths) RefreshRoadPathGuide(record);
            roadPresentationAssets.Add(mesh);
        }

        private void RebuildDistrictRoads()
        {
            ClearDistrictRoadPresentation();
            if (SelectedDistrict < 0) return;
            var active = new HashSet<int>(ActiveDistrictMembers);
            for (int i = 0; i < districtRoads.Count; i++)
            {
                var road = districtRoads[i];
                if (active.Contains(road.district)) PresentDistrictRoad(road, BuildDistrictRoadMesh(road), i);
            }
            RefreshRoadShoulderDetails();
            RefreshDirtJunctionWear();
        }

        private void RefreshDirtRoadEndFades()
        {
            foreach (var pair in districtRoadMeshes)
            {
                var road = pair.Key;
                if (road.surfaceId != "dirt" || road.closed) continue;
                var renderer = pair.Value.GetComponent<MeshRenderer>();
                dirtEndProperties ??= new MaterialPropertyBlock();
                renderer.GetPropertyBlock(dirtEndProperties);
                dirtEndProperties.SetVector(DirtEndFadeProperty, new Vector4(
                    DirtEndConnected(road, road.points[0]) ? 0f : 1f,
                    DirtEndConnected(road, road.points[^1]) ? 0f : 1f, 0f, 0f));
                renderer.SetPropertyBlock(dirtEndProperties);
            }
        }

        private bool DirtEndConnected(DistrictRoadRecord road, Vector2 endpoint)
        {
            foreach (var lot in PlacedLots)
                if (new DistrictLotFootprint(lot).OverlapsCircle(endpoint, .20f)) return true;

            foreach (var other in districtRoadMeshes.Keys)
            {
                if (other == road) continue;
                float reach = other.width * .5f + .20f;
                if ((ClosestRoadPoint(other, endpoint) - endpoint).sqrMagnitude <= reach * reach) return true;
            }
            return false;
        }

        private void RefreshRoadShoulderDetails()
        {
            if (ApprovedDistrict) ApprovedDistrict.RefreshRoadShoulders(districtRoads);
        }

        private void ClearDistrictRoadPresentation()
        {
            ClearRoadPathGuides();
            if (districtRoadRoot)
            {
                districtRoadRoot.gameObject.SetActive(false);
                Destroy(districtRoadRoot.gameObject);
                districtRoadRoot = null;
            }
            foreach (var asset in roadPresentationAssets) Destroy(asset);
            roadPresentationAssets.Clear();
            districtRoadMaterials.Clear();
            districtRoadMeshes.Clear();
        }

        private sealed class RoadWireSurface : PointWireSurface
        {
            private readonly PrototypeRegionView view;
            public RoadWireSurface(PrototypeRegionView view) => this.view = view;
            public override bool Available => view && view.CanPlaceRoad;
            public override bool CanEdit => Available;
            public override Collider Collider => view.RoadTerrain.GetComponent<TerrainCollider>();
            public override bool Contains(Vector2 point) => view.InActiveDistrict(point);
            public override float Height(Vector2 point) => view.LotHeight(point);
        }
    }
}

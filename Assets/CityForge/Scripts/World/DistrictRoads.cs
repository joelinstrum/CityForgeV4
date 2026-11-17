using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            ("cobblestone", "Cobblestone Road", "cobblestone-road-9", "A one-tile road paved with rounded stone setts and soft shoulders."),
            ("brick", "Brick Road", "damaged-brick", "A one-tile road paved with weathered brick and soft shoulders."),
            ("blacktop", "Blacktop Road", "concrete", "An asphalt surface for motor traffic."),
            ("paved-street", "Paved Street", "concrete", "A paved street for built-up districts."),
            (FoundersStreetId, "Dirt Street", "packed-earth", "A one-tile dirt street with plank sidewalks. Supports Commercial I zoning.")
        };

        private const int HistoricalRoadSurfaceCount = 6;
        private const int PavedStreetIndex = HistoricalRoadSurfaceCount + 1;
        private const int FoundersStreetIndex = PavedStreetIndex + 1;
        private const string FoundersStreetId = "founders-dirt-street";
        private const int RoadWidth = 3;
        private const int OneTileRoadWidth = (int)DistrictGridCellMeters;
        private static readonly int DirtEndFadeProperty = Shader.PropertyToID("_DirtEndFade");
        private MaterialPropertyBlock dirtEndProperties;
        private const float RoadSurfaceLift = .13f;
        private const float MasonryPavingLift = .115f;
        private const float SidewalkLift = .088f;
        private const int MaxDistrictRoads = 256;
        private const int MaxRoadPoints = 64;
        private const float MaxRoadLength = 1024f;
        private readonly List<DistrictRoadRecord> districtRoads = new();
        private readonly List<UnityEngine.Object> roadPresentationAssets = new();
        private readonly Dictionary<string, Material> districtRoadMaterials = new();
        private readonly Dictionary<DistrictRoadRecord, MeshFilter> districtRoadMeshes = new();
        private Transform districtRoadRoot, districtSidewalkRoot;
        private readonly List<UnityEngine.Object> sidewalkAssets = new();
        private PointWireTool roadWire;
        private RectTransform roadControls;
        private Button finishRoadButton;
        private int roadSurfaceIndex;
        private int? lastRepaveSurface;
        private DistrictRoadRecord repaveTarget;
        private DistrictRoadRecord roadExtensionHint, preferredRoadExtension;
        private bool preferredRoadPrepend;
        private Vector2? roadGestureStart;

        private bool RoadPlacementArmed => roadWire && roadWire.Armed;
        private Terrain RoadTerrain => ApprovedDistrict && ApprovedDistrict.DisplayTerrain
            ? ApprovedDistrict.DisplayTerrain : terrain;
        private bool CanPlaceRoad => CanEditRoad;
        private bool CanEditRoad => CanBuildTown && !IsSaving && RoadTerrain &&
            !playerLotWorkspace && !founderPicker && !returning && !RemakeBlocksInput;

        internal DistrictRoadRecord[] CaptureDistrictRoads() => districtRoads.ToArray();

        private void RestoreDistrictRoads(DistrictRoadRecord[] records)
        {
            if (records == null) return;
            foreach (var road in records)
            {
                if (HasSidewalks(road.surfaceId) && road.width > OneTileRoadWidth)
                    road.width = OneTileRoadWidth;
                districtRoads.Add(road);
            }
        }

        internal static void ValidateDistrictRoads(DistrictRoadRecord[] records, int districtCount, float mapSide)
        {
            if (records == null) return;
            if (records.Length > MaxDistrictRoads) throw new InvalidDataException("Too many district roads.");
            foreach (var record in records)
            {
                if (record == null || record.district < 0 || record.district >= districtCount ||
                    FindRoadSurface(record.surfaceId) < 0 || (record.width != OneTileRoadWidth && Array.IndexOf(LotPathSurface.Widths, record.width) < 0) ||
                    RoadGuideProblem(record.points, record.closed, mapSide * .5f) != null)
                    throw new InvalidDataException("Invalid district road.");
            }
        }

        private static int FindRoadSurface(string id) => Array.FindIndex(RoadSurfaces, surface => surface.Id == id);

        private static bool IsMasonryRoad(string id) => id is "brick" or "cobblestone";

        private static bool HasSidewalks(string id) => IsMasonryRoad(id) || id == FoundersStreetId;

        private static bool SupportsCommercialFrontage(string id) => id is FoundersStreetId or "paved-street";

        private static int DefaultRoadWidth(string id) => HasSidewalks(id) ? OneTileRoadWidth : RoadWidth;

        private static LotPathMaterial DistrictRoadStyle(string id)
        {
            var style = LotPathMaterial.Find(RoadSurfaces[FindRoadSurface(id)].Material);
            // Source-only checkouts can still render these roads with the simpler surface.
            return IsMasonryRoad(id) && !style.IsAvailable ? LotPathMaterial.Find(id) : style;
        }

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
                roadWire.FinishOpenAtMarker = point =>
                {
                    var source = FindRoadExtensionEnd(roadWire.Points[0], .001f, out _);
                    return TrySnapRoadJunction(point, out var target, source) && (point - target).sqrMagnitude < .0025f;
                };
                roadWire.StartPointHovered += HighlightRoadStartTile;
                roadWire.CompletionRequested = points => PlaceDistrictRoad(points, false);
                roadWire.LoopCompletionRequested = points => PlaceDistrictRoad(points, true);
                roadWire.RepeatOnCompletion = true;

                roadControls = FounderRect(hud, "Road placement controls", new Vector2(0, 215), new Vector2(242, 40));
                roadControls.anchorMin = roadControls.anchorMax = roadControls.pivot = new Vector2(.5f, 0);
                finishRoadButton = TownButton(roadControls, lotFont, "Place Road", 0, 0, 130, FinishRoad);
                TownButton(roadControls, lotFont, "Cancel", 138, 0, 104, CancelRoadPlacement);
            }

            preferredRoadExtension = null;
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
                CancelRoadPlacement();
                ShowDistrictMessage("");
            }
            return true;
        }

        private void FinishRoad()
        {
            if (RoadPlacementArmed) roadWire.CompletePath();
        }

        private void CancelRoadPlacement()
        {
            preferredRoadExtension = null;
            roadGestureStart = null;
            if (roadWire) roadWire.Cancel();
            if (roadControls) roadControls.gameObject.SetActive(false);
        }

        private string PlaceDistrictRoad(IReadOnlyList<Vector2> points, bool closed)
        {
            if (!CanPlaceRoad) return "Road placement is unavailable.";
            if (points.Count > 0 && FindRoadExtensionEnd(points[0], .001f, out bool prepend) is { } road)
                return ExtendDistrictRoad(road, prepend, points, closed);
            if (!closed && points.Count >= 2 && TryContinueFromRoadRun(points, out var existing, out bool atStart, out var tail))
                return tail.Length < 2 ? null : ExtendDistrictRoad(existing, atStart, tail, false);
            var problem = RoadGuideProblem(points, closed, MapSide * .5f);
            if (problem != null) return problem;

            int width = DefaultRoadWidth(RoadSurfaces[roadSurfaceIndex].Id);
            var guide = NewRoadGuide(points, closed, width, out bool aligned);
            var record = new DistrictRoadRecord
            {
                district = SelectedDistrict,
                surfaceId = RoadSurfaces[roadSurfaceIndex].Id,
                points = guide,
                closed = closed,
                gridAligned = aligned,
                width = width
            };
            return CommitNewRoadPieces(UnpavedRoadPieces(record));
        }

        private string CommitNewRoadPieces(List<DistrictRoadRecord> pieces)
        {
            if (districtRoads.Count + pieces.Count > MaxDistrictRoads) return "This region has reached its road limit.";
            var meshes = new List<Mesh>(pieces.Count);
            foreach (var piece in pieces)
            {
                var mesh = BuildDistrictRoadMesh(piece);
                meshes.Add(mesh);
                var problem = RoadSiteProblem(mesh);
                if (problem == null) continue;
                foreach (var rejected in meshes) Destroy(rejected);
                return problem;
            }
            for (int i = 0; i < pieces.Count; i++)
            {
                PresentDistrictRoad(pieces[i], meshes[i], districtRoads.Count);
                districtRoads.Add(pieces[i]);
            }
            if (pieces.Count == 0) return null;
            RefreshRoadShoulderDetails();
            RefreshDirtRoadEndFades();
            RefreshDirtJunctionWear();
            QueueLotGrowth();
            return null;
        }

        private void RefreshRoadPlacementHint(bool force = false)
        {
            var road = roadWire.Points.Count > 0 ? FindRoadExtensionEnd(roadWire.Points[0], .001f, out _) : null;
            if (!force && roadExtensionHint == road) return;
            roadExtensionHint = road;
            roadWire.RibbonWidth = road?.width ?? DefaultRoadWidth(RoadSurfaces[roadSurfaceIndex].Id);
            roadWire.DrawingHint = road == null
                ? roadWire.Label + " · Click an existing endpoint to continue that road, or click open ground to start a road\n" +
                    "Click the first marker to close a loop · Enter to place · Esc to cancel · Backspace to undo"
                : "Continue " + RoadSurfaces[FindRoadSurface(road.surfaceId)].Label +
                    " · Click another road to connect, or press Enter to finish · Esc to cancel";
            roadWire.SetMessage(roadWire.DrawingHint);
            finishRoadButton.GetComponentInChildren<Text>().text = road == null ? "Place Road" : "Extend Road";
        }

        private bool TryStartRoadFromClick(Vector2 screen)
        {
            if (!CanPlaceRoad || roadContext || !TryRoadGround(screen, out var point)) return false;
            var road = PickDistrictRoad(point);
            if (road == null) return false;
            StartRoadPlacement(FindRoadSurface(road.surfaceId));
            if (!RoadPlacementArmed) return false;
            if (!road.closed && FindRoadExtensionEnd(point, road.width * .5f + .6f, out bool prepend) == road)
            {
                preferredRoadExtension = road;
                preferredRoadPrepend = prepend;
                point = prepend ? road.points[0] : road.points[^1];
            }
            if (!roadWire.PlaceMarker(ClosestRoadPoint(road, point))) return false;
            roadGestureStart = screen;
            RefreshRoadPlacementHint(force: true);
            return true;
        }

        private void CompleteRoadGesture(Event pointer)
        {
            if (pointer.type != EventType.MouseUp || pointer.button != 0 || !roadGestureStart.HasValue) return;
            var start = roadGestureStart.Value;
            roadGestureStart = null;
            var end = new Vector2(pointer.mousePosition.x, Screen.height - pointer.mousePosition.y);
            if ((end - start).sqrMagnitude < 64f || RoadContextOverUi(end) || !TryRoadGround(end, out var point)) return;
            if (!roadWire.PlaceMarker(point)) return;
            roadWire.CompletePath();
            pointer.Use();
        }

        private Vector2 SnapRoadPlacementPoint(Vector2 point)
        {
            if (roadWire.Points.Count == 0)
            {
                var road = FindRoadExtensionEnd(point, 1.25f, out bool prepend);
                if (road != null) return prepend ? road.points[0] : road.points[^1];
            }
            var source = roadWire.Points.Count == 0 ? null : FindRoadExtensionEnd(roadWire.Points[0], .001f, out _);
            if (TrySnapRoadJunction(point, out var junction, source)) return junction;
            if (!snapNewRoadsToTileCenters) return point;
            float origin = -MapSide * .5f;
            return new Vector2(TileCenter(point.x), TileCenter(point.y));

            float TileCenter(float coordinate) => origin +
                (Mathf.Floor((coordinate - origin) / DistrictGridCellMeters) + .5f) * DistrictGridCellMeters;
        }

        private bool TryContinueFromRoadRun(IReadOnlyList<Vector2> points, out DistrictRoadRecord road,
            out bool prepend, out Vector2[] tail)
        {
            road = PickDistrictRoad(points[0]);
            prepend = false;
            tail = null;
            if (road == null || road.closed) return false;
            var original = road.points;
            var direction = (original[^1] - original[0]).normalized;
            if (original.Any(point => Mathf.Abs(Vector2.Dot(point - original[0],
                Vector2.Perpendicular(direction))) > .1f)) return false;
            var outward = (points[1] - points[0]).normalized;
            if (Mathf.Abs(Vector2.Dot(outward, direction)) < .98f) return false;
            prepend = Vector2.Dot(outward, direction) < 0;
            var endpoint = prepend ? road.points[0] : road.points[^1];
            tail = new[] { endpoint }.Concat(points.Skip(1).SkipWhile(point =>
                Vector2.Dot(point - endpoint, outward) < 1f)).ToArray();
            return true;
        }

        private List<DistrictRoadRecord> UnpavedRoadPieces(DistrictRoadRecord proposal, DistrictRoadRecord exclude = null)
        {
            var existing = districtRoadMeshes.Keys.Where(road => road != exclude)
                .Select(road => (Road: road, Line: RoadCenterline(road))).ToArray();
            var pieces = new List<DistrictRoadRecord>();
            var run = new List<Vector2>();
            bool clipped = false;
            int count = proposal.closed ? proposal.points.Length : proposal.points.Length - 1;
            for (int i = 0; i < count; i++)
            {
                var start = proposal.points[i];
                var end = proposal.points[(i + 1) % proposal.points.Length];
                float length = Vector2.Distance(start, end);
                if (length < .001f) continue;
                var direction = (end - start) / length;
                var normal = Vector2.Perpendicular(direction);
                var covered = new List<(float Start, float End)>();
                foreach (var (road, line) in existing)
                {
                    int segments = road.closed ? line.Count : line.Count - 1;
                    for (int j = 0; j < segments; j++)
                    {
                        var a = line[j] - start;
                        var b = line[(j + 1) % line.Count] - start;
                        if (Mathf.Abs(Vector2.Dot((b - a).normalized, direction)) < .995f ||
                            Mathf.Abs(Vector2.Dot(a, normal)) > road.width * .5f ||
                            Mathf.Abs(Vector2.Dot(b, normal)) > road.width * .5f) continue;
                        float x = Vector2.Dot(a, direction), y = Vector2.Dot(b, direction);
                        float low = Mathf.Max(0f, Mathf.Min(x, y)), high = Mathf.Min(length, Mathf.Max(x, y));
                        if (high - low > .025f) covered.Add((low, high));
                    }
                }
                float cursor = 0f;
                foreach (var (low, high) in covered.OrderBy(pair => pair.Start))
                {
                    clipped = true;
                    if (low > cursor + .025f) AddSpan(cursor, low);
                    FinishRun();
                    cursor = Mathf.Max(cursor, high);
                }
                if (length > cursor + .025f) AddSpan(cursor, length);

                void AddSpan(float from, float to)
                {
                    var a = start + direction * from;
                    var b = start + direction * to;
                    if (from > .025f && TrySnapRoadJunction(a, out var joinA)) a = joinA;
                    if (to < length - .025f && TrySnapRoadJunction(b, out var joinB)) b = joinB;
                    if (run.Count == 0) run.Add(a);
                    run.Add(b);
                }
            }
            if (!clipped) return new List<DistrictRoadRecord> { proposal };
            FinishRun();
            return pieces;

            void FinishRun()
            {
                if (run.Count >= 2 && RoadGuideProblem(run, false, MapSide * .5f) == null)
                    pieces.Add(new DistrictRoadRecord
                    {
                        district = proposal.district, surfaceId = proposal.surfaceId, width = proposal.width,
                        points = run.ToArray(), curved = proposal.curved
                    });
                run.Clear();
            }
        }

        private bool TrySnapRoadJunction(Vector2 point, out Vector2 junction, DistrictRoadRecord exclude = null)
        {
            junction = point;
            bool found = false;
            float best = float.PositiveInfinity;
            foreach (var road in districtRoadMeshes.Keys)
            {
                if (road == exclude) continue;
                var candidate = ClosestRoadPoint(road, point);
                float distance = (point - candidate).sqrMagnitude;
                float reach = road.width * .5f + .6f;
                if (distance > reach * reach || distance >= best) continue;
                junction = candidate;
                best = distance;
                found = true;
            }
            return found;
        }

        private Vector2[] NewRoadGuide(IReadOnlyList<Vector2> points, bool closed, int width, out bool aligned)
        {
            var result = points.ToArray();
            bool joined = false;
            if (!closed)
            {
                if (TrySnapRoadJunction(result[0], out var start)) { result[0] = start; joined = true; }
                if (TrySnapRoadJunction(result[^1], out var end)) { result[^1] = end; joined = true; }
                if (RoadGuideProblem(result, false, MapSide * .5f) != null)
                {
                    aligned = false;
                    return points.ToArray();
                }
            }
            if (snapNewRoadsToTileCenters && !joined)
            {
                var snapped = AlignRoadGuide(result, closed, width);
                if (RoadGuideProblem(snapped, closed, MapSide * .5f) == null)
                {
                    aligned = true;
                    return snapped;
                }
            }
            aligned = false;
            return result;
        }

        private DistrictRoadRecord FindRoadExtensionEnd(Vector2 point, float radius, out bool prepend)
        {
            if (preferredRoadExtension is { } preferred && districtRoadMeshes.ContainsKey(preferred))
            {
                var endpoint = preferredRoadPrepend ? preferred.points[0] : preferred.points[^1];
                if ((endpoint - point).sqrMagnitude <= radius * radius)
                {
                    prepend = preferredRoadPrepend;
                    return preferred;
                }
            }
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
            if (road == null) return NewRoadGuide(points, closed, DefaultRoadWidth(RoadSurfaces[roadSurfaceIndex].Id), out _);
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
            {
                if (TryContinueFromRoadRun(points, out var opposite, out bool atStart, out var tail) && opposite == road)
                    return tail.Length < 2 ? null : ExtendDistrictRoad(road, atStart, tail, false);
                return "Extend away from the endpoint to avoid doubling back over the road.";
            }
            var guide = BuildRoadExtensionGuide(road, prepend, points);
            int added = guide.Length - road.points.Length;
            var extension = new DistrictRoadRecord
            {
                district = road.district, surfaceId = road.surfaceId, width = road.width, curved = road.curved,
                points = prepend ? guide.Take(added + 1).ToArray() : guide.Skip(road.points.Length - 1).ToArray()
            };
            var pieces = UnpavedRoadPieces(extension, road);
            if (pieces.Count != 1 || pieces[0] != extension) return CommitNewRoadPieces(pieces);
            bool aligned = road.gridAligned && guide.Zip(guide.Skip(1), (a, b) =>
                Mathf.Abs(a.x - b.x) < .001f || Mathf.Abs(a.y - b.y) < .001f).All(value => value);
            return CommitRoadEdit(road, guide, road.curved, aligned, preserveGuide: true);
        }

        private Vector2[] BuildRoadExtensionGuide(DistrictRoadRecord road, bool prepend, IReadOnlyList<Vector2> points)
        {
            var extension = points.ToArray();
            if (road.gridAligned)
            {
                var aligned = AlignRoadGuide(extension, false, road.width);
                if (RoadGuideProblem(aligned, false, MapSide * .5f) == null) extension = aligned;
            }
            // The saved endpoint and the destination junction take priority over grid alignment.
            extension[0] = prepend ? road.points[0] : road.points[^1];
            if (TrySnapRoadJunction(points[^1], out var junction, road))
                extension[^1] = junction;

            return prepend
                ? extension.Skip(1).Reverse().Concat(road.points).ToArray()
                : road.points.Concat(extension.Skip(1)).ToArray();
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
            var style = DistrictRoadStyle(record.surfaceId);
            var path = new LotPathDefinition
            {
                points = record.points,
                closed = record.closed,
                curved = record.curved,
                width = record.width
            };
            float lift = HasSidewalks(record.surfaceId) ? MasonryPavingLift : RoadSurfaceLift;
            var mesh = LotPathSurface.BuildMesh(path, style.TileSize, lift, LotHeight);
            if (record.surfaceId == FoundersStreetId)
            {
                var ribbon = new List<Vector4>();
                mesh.GetUVs(1, ribbon);
                const float nominalRepeat = 4.5f;
                float loopLength = record.closed && ribbon.Count > 0 ? -ribbon[0].w : 0f;
                float repeat = loopLength > 0f ? loopLength / Mathf.Max(1, Mathf.RoundToInt(loopLength / nominalRepeat)) : nominalRepeat;
                mesh.SetUVs(0, ribbon.ConvertAll(uv => new Vector2(uv.z / repeat, uv.x / uv.y)));
                mesh.RecalculateTangents();
            }
            return mesh;
        }

        private void PresentDistrictRoad(DistrictRoadRecord record, Mesh mesh, int order)
        {
            if (!districtRoadMaterials.TryGetValue(record.surfaceId, out var material))
            {
                material = DistrictRoadStyle(record.surfaceId).CreateMaterial();
                if (record.surfaceId == "blacktop")
                    material.SetColor("_BaseColor", new Color(.27f, .28f, .30f));
                if (!HasSidewalks(record.surfaceId))
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
                if (record.surfaceId is "wagon" or FoundersStreetId)
                {
                    var albedo = Resources.Load<Texture2D>("Lots/PathMaterials/WagonRoadAlbedo");
                    var mask = Resources.Load<Texture2D>("Lots/PathMaterials/WagonRoadMask");
                    if (albedo && (record.surfaceId == FoundersStreetId || mask && material.HasProperty("_WagonMask")))
                    {
                        material.SetTexture("_BaseMap", albedo);
                        material.SetColor("_BaseColor", new Color(.83f, .78f, .72f, 1f));
                        material.SetFloat("_Smoothness", .03f);
                        if (record.surfaceId == "wagon")
                        {
                            material.SetTexture("_WagonMask", mask);
                            material.EnableKeyword("_WAGON_SURFACE");
                        }
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
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
            RefreshDirtRoadEndFades();
            RefreshDirtJunctionWear();
        }

        private void RefreshDirtRoadEndFades()
        {
            foreach (var pair in districtRoadMeshes)
            {
                var road = pair.Key;
                if (road.closed) continue;
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
            pedestrianRoutesDirty = true;
            if (ApprovedDistrict) ApprovedDistrict.RefreshRoadShoulders(districtRoads);
            RefreshDistrictSidewalks();
        }

        private void RefreshDistrictSidewalks()
        {
            ClearDistrictSidewalks();
            if (!districtRoadRoot) return;
            var roads = districtRoadMeshes.Keys.Where(road => HasSidewalks(road.surfaceId)).ToArray();
            if (roads.Length == 0) return;
            districtSidewalkRoot = new GameObject("District sidewalks").transform;
            districtSidewalkRoot.SetParent(districtRoadRoot, false);
            var materials = new Dictionary<string, Material>();
            foreach (var road in roads)
            {
                bool plank = road.surfaceId == FoundersStreetId;
                string styleId = plank ? LotPathMaterial.Plankboard : LotPathMaterial.Bluestone;
                var style = LotPathMaterial.Find(styleId);
                if (!materials.TryGetValue(styleId, out var material))
                {
                    material = style.CreateMaterial();
                    materials.Add(styleId, material);
                    sidewalkAssets.Add(material);
                }
                const int width = LotPathSurface.ResidentialSidewalkWidth;
                for (int side = -1; side <= 1; side += 2)
                {
                    var path = new LotPathDefinition { points = road.points, width = road.width, closed = road.closed, curved = road.curved };
                    // Use the same cross sections as the pavement so the inner sidewalk edge cannot curl over it.
                    var mesh = LotPathSurface.BuildMesh(path, style.TileSize, SidewalkLift, LotHeight,
                        shoulderSide: side, shoulderWidth: width);
                    if (plank)
                    {
                        var ribbon = new List<Vector4>();
                        mesh.GetUVs(1, ribbon);
                        mesh.SetUVs(0, ribbon.ConvertAll(uv => new Vector2(uv.x / uv.y, uv.z / style.TileSize.y)));
                        mesh.RecalculateTangents();
                    }
                    var walk = new GameObject(plank ? "Plank sidewalk" : "Bluestone sidewalk", typeof(MeshFilter), typeof(MeshRenderer));
                    walk.transform.SetParent(districtSidewalkRoot, false);
                    walk.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = walk.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    sidewalkAssets.Add(mesh);
                }
            }

        }

        private void ClearDistrictSidewalks()
        {
            if (districtSidewalkRoot)
            {
                districtSidewalkRoot.gameObject.SetActive(false);
                Destroy(districtSidewalkRoot.gameObject);
                districtSidewalkRoot = null;
            }
            foreach (var asset in sidewalkAssets) Destroy(asset);
            sidewalkAssets.Clear();
        }

        private void ClearDistrictRoadPresentation()
        {
            ClearRoadPathGuides();
            ClearDistrictSidewalks();
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

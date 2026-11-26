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
        public string streetTreeStyle; // Missing/empty in older saves means no automatic planting.
        public Vector2[] points;
        public bool closed;
        public bool curved;
        [NonSerialized] internal HashSet<int> junctionCorners;
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
            ("cobblestone", "Cobblestone Road", "cobblestone-road-9", "A one-tile road paved with rounded stone setts and soft shoulders. Supports commercial frontage."),
            ("brick", "Brick Road", "damaged-brick", "A one-tile road paved with weathered brick and soft shoulders. Supports commercial frontage."),
            ("blacktop", "Blacktop Road", "concrete", "An asphalt surface for motor traffic."),
            ("paved-street", "Paved Street", "concrete", "A paved street for built-up districts."),
            (FoundersStreetId, "Dirt Street", "packed-earth", "A one-tile (4 m) street using worn wagon-trail dirt. Supports commercial frontage."),
            (FoundersCobblestoneStreetId, "Cobblestone Street", "cobblestone-road-9", "A one-tile (4 m) street paved with rounded cobblestone. Supports commercial frontage."),
            (FoundersBrickStreetId, "Weathered Brick Street", "damaged-brick", "A one-tile (4 m) street paved with weathered brick. Supports commercial frontage."),
            ("founders-early-macadam-gravel-street", "Macadam Gravel Street", "early-macadam-gravel", "A one-tile (4 m) street with macadam gravel paving. Supports commercial frontage at every wealth level."),
            ("founders-mossy-fieldstone-street", "Mossy Fieldstone Street", "mossy-fieldstone", "A one-tile (4 m) street with mossy fieldstone paving. Supports commercial frontage at every wealth level."),
            ("founders-weathered-herringbone-pavement-street", "Herringbone Brick Street", "weathered-herringbone-pavement", "A one-tile (4 m) street with herringbone brick paving. Supports commercial frontage at every wealth level."),
            ("founders-irregular-flagstone-pavement-street", "Irregular Flagstone Street", "irregular-flagstone-pavement", "A one-tile (4 m) street with irregular flagstone paving. Supports commercial frontage at every wealth level."),
            ("founders-weathered-red-brick-pavement-street", "Weathered Red Brick Street", "weathered-red-brick-pavement", "A one-tile (4 m) street with weathered red brick paving. Supports commercial frontage at every wealth level."),
            ("founders-rustic-wood-pavement-street", "Rustic Wood Planks Street", "rustic-wood-pavement", "A one-tile (4 m) street with rustic wood planks paving. Supports commercial frontage at every wealth level."),
            ("weathered-concrete-road", "Concrete Road", "weathered-concrete-pavement", "Weathered gray concrete paving. Available from the Art Deco era."),
            ("weathered-asphalt-road", "Asphalt Road", "weathered-charcoal-asphalt", "Weathered charcoal asphalt paving. Available from the Art Deco era."),
            ("weathered-concrete-street", "Concrete Street", "weathered-concrete-pavement", "A one-tile (4 m) concrete street with sidewalks. Optional automatic street trees."),
            ("art-deco-concrete-avenue", "Concrete Avenue", "weathered-concrete-pavement", "A 12 m avenue with sidewalks and optional curbside trees. Available from Art Deco onward."),
            ("art-deco-divided-boulevard", "Divided Boulevard", "weathered-concrete-pavement", "A 20 m boulevard: two 8 m carriageways around a 4 m planted median, with sidewalks. Available from Art Deco onward."),
            ("weathered-asphalt-street", "Asphalt Street", "weathered-charcoal-asphalt", "A one-tile (4 m) asphalt street with sidewalks. Optional automatic street trees.")
        };

        private const int HistoricalRoadSurfaceCount = 6;
        private const int PavedStreetIndex = HistoricalRoadSurfaceCount + 1;
        private const int FoundersStreetIndex = PavedStreetIndex + 1;
        private const string FoundersStreetId = "founders-dirt-street";
        private const string FoundersCobblestoneStreetId = "founders-cobblestone-street";
        private const string FoundersBrickStreetId = "founders-brick-street";

        internal static bool IsAvenue(string id) => id is "art-deco-concrete-avenue" or "art-deco-divided-boulevard";
        internal static bool IsDividedBoulevard(string id) => id == "art-deco-divided-boulevard";
        internal static bool IsModernStreet(string id) => IsAvenue(id) || id is "weathered-concrete-street" or "weathered-asphalt-street";
        internal static bool IsModernRoadSurface(string id) => IsModernStreet(id) || id is "weathered-concrete-road" or "weathered-asphalt-road";
        internal static bool IsFoundersStreet(string id) => FoundersStreetSlot(id) >= 0;
        private static int FoundersStreetSlot(string id) => id switch
        {
            FoundersStreetId => 0, FoundersCobblestoneStreetId => 1, FoundersBrickStreetId => 2,
            "founders-early-macadam-gravel-street" => 3,
            "founders-mossy-fieldstone-street" => 4,
            "founders-weathered-herringbone-pavement-street" => 5,
            "founders-irregular-flagstone-pavement-street" => 6,
            "founders-weathered-red-brick-pavement-street" => 7,
            "founders-rustic-wood-pavement-street" => 8,
            _ => -1
        };
        private const int RoadWidth = 3;
        private const int OneTileRoadWidth = (int)DistrictGridCellMeters;
        private static readonly int DirtEndFadeProperty = Shader.PropertyToID("_DirtEndFade");
        private MaterialPropertyBlock dirtEndProperties;
        private const float RoadSurfaceLift = .13f;
        private const float MasonryPavingLift = .115f;
        private const float SidewalkLift = .12f;
        private const int MaxDistrictRoads = 256;
        private const int MaxRoadPoints = 64;
        private const float MaxRoadLength = 1024f;
        private readonly List<DistrictRoadRecord> districtRoads = new();
        private readonly List<UnityEngine.Object> roadPresentationAssets = new();
        private readonly Dictionary<string, Material> districtRoadMaterials = new();
        internal int FreightRoadRevision { get; private set; }
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
                if (HasSidewalks(road.surfaceId) && !IsAvenue(road.surfaceId) && road.width > OneTileRoadWidth)
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
                    FindRoadSurface(record.surfaceId) < 0 || (IsAvenue(record.surfaceId) ? record.width != DefaultRoadWidth(record.surfaceId) : record.width != OneTileRoadWidth && Array.IndexOf(LotPathSurface.Widths, record.width) < 0) ||
                    RoadGuideProblem(record.points, record.closed, mapSide * .5f) != null || !StreetTreePalette.Valid(record.streetTreeStyle))
                    throw new InvalidDataException("Invalid district road.");
            }
        }

        private static int FindRoadSurface(string id) => Array.FindIndex(RoadSurfaces, surface => surface.Id == id);

        private static bool IsMasonryRoad(string id) => id is "brick" or "cobblestone" or FoundersCobblestoneStreetId or FoundersBrickStreetId or
            "founders-mossy-fieldstone-street" or "founders-weathered-herringbone-pavement-street" or
            "founders-irregular-flagstone-pavement-street" or "founders-weathered-red-brick-pavement-street";

        private static bool HasSidewalks(string id) => IsMasonryRoad(id) || IsFoundersStreet(id) || IsModernStreet(id);

        // Masonry roads already have the same pavement and sidewalks as streets.
        private static bool SupportsCommercialFrontage(string id) => HasSidewalks(id) || id == "paved-street";

        private static int DefaultRoadWidth(string id) => IsDividedBoulevard(id) ? 20 : IsAvenue(id) ? 12 : HasSidewalks(id) ? OneTileRoadWidth : RoadWidth;

        private static LotPathMaterial DistrictRoadStyle(string id)
        {
            var style = LotPathMaterial.Find(RoadSurfaces[FindRoadSurface(id)].Material);
            // Source-only checkouts can still render these roads with the simpler surface.
            return IsMasonryRoad(id) && !style.IsAvailable ? LotPathMaterial.Find(id is "brick" or FoundersBrickStreetId ? "brick" : "cobblestone") : style;
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

            roadStampShape = RoadStampShape.None;
            roadStampQuarterTurns = 0;
            roadWire.StampGuide = null;
            roadWire.StampCompletionRequested = null;
            roadWire.StampPaths = null; roadWire.StampPathsCompletionRequested = null;
            preferredRoadExtension = null;
            roadSurfaceIndex = surfaceIndex;
            lastRepaveSurface = surfaceIndex;
            string label = RoadSurfaces[surfaceIndex].Label;
            roadWire.Label = label;
            roadWire.PlaceOnRelease = StreetJunctions.IsStreet(RoadSurfaces[surfaceIndex].Id);
            roadWire.ConnectionPreview = roadWire.PlaceOnRelease ? PreviewStreetConnection : null;
            streetSnapTarget = null;
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

            UpdateRoadStampShortcut();
            if (roadStampShape != RoadStampShape.None)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) { SetRoadStamp(RoadStampShape.None); return true; }
                if (stampWasLarge != LargeRoundaboutHeld) RefreshRoadStampHint();
                finishRoadButton.interactable = false;
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
            CancelBridgeTool();
            roadStampShape = RoadStampShape.None;
            roadStampQuarterTurns = 0;
            if (roadWire) { roadWire.StampGuide = null; roadWire.StampCompletionRequested = null; roadWire.StampPaths = null; roadWire.StampPathsCompletionRequested = null; }
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
                streetTreeStyle = HasSidewalks(RoadSurfaces[roadSurfaceIndex].Id) ? newStreetTreeStyle : null,
                points = guide,
                closed = closed,
                curved = true,
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
            RefreshRoadShoulderDetails(true);
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
            roadWire.DrawingHint = roadWire.PlaceOnRelease
                ? roadWire.Label + " · r/d/t/x/s/i: shapes · Drag to draw · Release on a green square to join · Drag through to cross\n" +
                    "Click to add waypoints · Enter to place · Esc to cancel · Backspace to undo"
                : road == null
                ? roadWire.Label + " · r/d/t/x/s/i: shapes · Click an existing endpoint to continue that road, or click open ground to start a road\n" +
                    "Click the first marker to close a loop · Enter to place · Esc to cancel · Backspace to undo"
                : "Continue " + RoadSurfaces[FindRoadSurface(road.surfaceId)].Label +
                    " · Click another road to connect\n" +
                    "r/d/t/x/s/i: shapes · Enter: finish · Esc: cancel · Backspace: undo";
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

        private DistrictRoadRecord streetSnapTarget;
        private Vector2 streetSnapPoint;

        private Vector2 SnapRoadPlacementPoint(Vector2 point)
        {
            if (StreetJunctions.IsStreet(RoadSurfaces[roadSurfaceIndex].Id))
                return SnapStreetPlacementPoint(point);
            if (roadWire.Points.Count == 0)
            {
                var road = FindRoadExtensionEnd(point, 1.25f, out bool prepend);
                if (road != null) return prepend ? road.points[0] : road.points[^1];
            }
            var source = roadWire.Points.Count == 0 ? null : FindRoadExtensionEnd(roadWire.Points[0], .001f, out _);
            if (TrySnapRoadJunction(point, out var junction, source)) return junction;
            if (!snapNewRoadsToTileCenters) return point;
            return SnapRoadGridPoint(point);
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
                .Select(road => (Road: road, Line: road.curved && proposal.curved
                    ? (IReadOnlyList<Vector2>)road.points : RoadCenterline(road))).ToArray();
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
                        district = proposal.district, surfaceId = proposal.surfaceId, width = proposal.width,streetTreeStyle=proposal.streetTreeStyle,
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
                district = road.district, surfaceId = road.surfaceId, width = road.width, curved = road.curved,streetTreeStyle=road.streetTreeStyle,
                points = prepend ? guide.Take(added + 1).ToArray() : guide.Skip(road.points.Length - 1).ToArray()
            };
            var pieces = UnpavedRoadPieces(extension, road);
            if (pieces.Count != 1 || pieces[0] != extension) return CommitNewRoadPieces(pieces);
            bool aligned = road.gridAligned && guide.Zip(guide.Skip(1), (a, b) =>
                Mathf.Abs(a.x - b.x) < .001f || Mathf.Abs(a.y - b.y) < .001f ||
                Mathf.Abs(Mathf.Abs(a.x - b.x) - Mathf.Abs(a.y - b.y)) < .001f).All(value => value);
            return CommitRoadEdit(road, guide, road.curved, aligned, preserveGuide: true);
        }

        private Vector2[] BuildRoadExtensionGuide(DistrictRoadRecord road, bool prepend, IReadOnlyList<Vector2> points)
        {
            var extension = points.ToArray();
            if (road.gridAligned)
            {
                var aligned = AlignRoadGuide(extension, false, road.width);
                if (RoadGuideProblem(aligned, false, MapSide * .5f) == null)
                {
                    // Keep the existing endpoint fixed without tilting a snapped segment.
                    var offset = points[0] - aligned[0];
                    extension = aligned.Select(p => p + offset).ToArray();
                }
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
                if (BridgeRoadConflict(point, 0) is { } bridgeProblem) return bridgeProblem;
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
            var mesh = LotPathSurface.BuildMesh(path, style.TileSize, lift, LotHeight, sharpCorners: record.junctionCorners);
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
                LotPathMaterial.ApplyEdgeTreatment(material, new LotPathDefinition
                {
                    width = record.width, depth = LotPathDepth.HalfFoot, recessed = true,
                    border = IsModernRoadSurface(record.surfaceId) || IsMasonryRoad(record.surfaceId) || record.surfaceId is "blacktop" or "paved-street" ? LotPathBorder.Curb : LotPathBorder.None
                }, HasSidewalks(record.surfaceId) ? 0 : 8, 0);
                material.renderQueue = DistrictLotSurfaces.PathRenderQueue + 1;
                BindRoadRecess(material);
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
            districtRoadMeshes.Add(record, filter); FreightRoadRevision++; MarkImmigrationSitesChanged();
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
            CancelBridgeTool();
            RestoreBridgeEarthwork();
            ClearBridgePresentation();
            ClearDistrictRoadPresentation();
            if (SelectedDistrict < 0) return;
            ApplyZoneGroundPlanes();
            var active = new HashSet<int>(ActiveDistrictMembers);
            for (int i = 0; i < districtRoads.Count; i++)
            {
                var road = districtRoads[i];
                if (active.Contains(road.district)) PresentDistrictRoad(road, BuildDistrictRoadMesh(road), i);
            }
            RebuildBridgePresentation();
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

        private void RefreshRoadShoulderDetails(bool deferSidewalks=false)
        {
            RefreshConnectedRoadCorners();
            pedestrianRoutesDirty = true;
            if (ApprovedDistrict) ApprovedDistrict.RefreshRoadShoulders(districtRoads);
            if(deferSidewalks)QueueDistrictSidewalkRefresh();else RefreshDistrictSidewalks();
        }

        private readonly HashSet<DistrictLotSurfaces> deferredLotStreetMasks=new();
        private Coroutine queuedSidewalkJob;
        private bool queuedSidewalkDirty;
        private Transform oldSidewalkRoot;
        private UnityEngine.Object[] oldSidewalkAssets;
        private StreetSurfaceMask cachedPavementMask,cachedLotPathMask;
        private int sidewalkMaskRoadRevision=-1;
        private void DisposeOldSidewalks()
        {
            if(oldSidewalkRoot){oldSidewalkRoot.gameObject.SetActive(false);Destroy(oldSidewalkRoot.gameObject);}oldSidewalkRoot=null;
            if(oldSidewalkAssets!=null)foreach(var asset in oldSidewalkAssets)if(asset)Destroy(asset);oldSidewalkAssets=null;
        }
        private void CancelQueuedSidewalks()
        {
            if(queuedSidewalkJob!=null)StopCoroutine(queuedSidewalkJob);
            queuedSidewalkJob=null;queuedSidewalkDirty=false;DisposeOldSidewalks();
        }
        private void QueueDistrictSidewalkRefresh()
        {
            queuedSidewalkDirty=true;
            if(queuedSidewalkJob==null)queuedSidewalkJob=StartCoroutine(RebuildQueuedSidewalks());
        }
        private System.Collections.IEnumerator RebuildQueuedSidewalks()
        {
            yield return null;
            while(queuedSidewalkDirty)
            {
                queuedSidewalkDirty=false;
                oldSidewalkRoot=districtSidewalkRoot;oldSidewalkAssets=sidewalkAssets.ToArray();
                districtSidewalkRoot=null;sidewalkAssets.Clear();
                var work=BuildDistrictSidewalks(true);while(work.MoveNext())yield return work.Current;
                if(districtSidewalkRoot)districtSidewalkRoot.gameObject.SetActive(true);
                DisposeOldSidewalks();pedestrianRoutesDirty=true;
            }
            queuedSidewalkJob=null;
        }
        private void RefreshDistrictSidewalks()
        {
            CancelQueuedSidewalks();
            var work=BuildDistrictSidewalks(false);while(work.MoveNext()){}
        }
        private System.Collections.IEnumerator BuildDistrictSidewalks(bool paced)
        {
            PruneAutomaticStreetTrees();
            ClearDistrictSidewalks();
            var visibleRoads = districtRoadMeshes.Keys.ToArray();
            var pavementMask = paced&&sidewalkMaskRoadRevision==FreightRoadRevision?cachedPavementMask:null;
            var lotPathMask = paced&&sidewalkMaskRoadRevision==FreightRoadRevision?cachedLotPathMask:null;
            bool roadMasksChanged=pavementMask==null||lotPathMask==null;
            if(roadMasksChanged)
            {
            pavementMask=new StreetSurfaceMask();lotPathMask=new StreetSurfaceMask();
            foreach (var road in visibleRoads)
            {
                var centerline = RoadCenterline(road);
                pavementMask.AddRibbon(road, centerline, road.width);
                lotPathMask.AddRibbon(road, centerline, road.width + (HasSidewalks(road.surfaceId) ? 2 * LotPathSurface.ResidentialSidewalkWidth : 0));
            }
            foreach (var node in StreetJunctions.Build(visibleRoads, RoadCenterline))
            {
                var outline = StreetJunctions.Outline(node);
                if (!CityForge.Geometry.PointWireGeometry.IsSimple(outline)) continue;
                for (int i = 0; i < outline.Count; i++)
                {
                    pavementMask.AddTriangle(node.Point, outline[i], outline[(i + 1) % outline.Count]);
                    lotPathMask.AddTriangle(node.Point, outline[i], outline[(i + 1) % outline.Count]);
                }
            }
            cachedPavementMask=pavementMask;cachedLotPathMask=lotPathMask;sidewalkMaskRoadRevision=FreightRoadRevision;
            }
            if(paced){
                // A previous paced job may have been cancelled after caching its masks.
                // Finish the matching recess field even when those masks are reused.
                ulong previousRecess=roadRecessSignature;
                var recess=BuildRoadRecess(pavementMask,visibleRoads,true);while(recess.MoveNext())yield return recess.Current;
                if(roadMasksChanged||previousRecess!=roadRecessSignature)RebuildParkPaths(lotPathMask);
            }
            if(paced){foreach(var surface in deferredLotStreetMasks)if(surface)surface.ApplyStreetMask(lotPathMask);deferredLotStreetMasks.Clear();}
            if(!paced)
            {
            deferredLotStreetMasks.Clear();
            RefreshRoadRecess(pavementMask, visibleRoads);
            RebuildParkPaths(lotPathMask);
            if (lotInstances)
                foreach (var surface in lotInstances.GetComponentsInChildren<DistrictLotSurfaces>(true)) surface.ApplyStreetMask(lotPathMask);
            }
            if (!districtRoadRoot) yield break;
            var frontages = NeighborhoodFootpaths.Collect(districtLotRoots.Keys);
            var shopFrontages = NeighborhoodFootpaths.Collect(districtLotRoots.Keys, commercial: true);
            var roads = districtRoadMeshes.Keys.Where(road => HasSidewalks(road.surfaceId) || frontages.Count > 0).ToArray();
            if (roads.Length == 0) yield break;
            districtSidewalkRoot = new GameObject("District sidewalks").transform;
            districtSidewalkRoot.SetParent(districtRoadRoot, false);
            if(paced)districtSidewalkRoot.gameObject.SetActive(false);
            var materials = new Dictionary<string, Material>();
            const int width = LotPathSurface.ResidentialSidewalkWidth;
            foreach (var road in roads)
            {
                if(paced)yield return null;
                bool paved = HasSidewalks(road.surfaceId);
                string eraPavingId = EraAppearance.Profile(DistrictAppearanceEra(SelectedDistrict))?.generalMaterial;
                bool modernPaving = !string.IsNullOrEmpty(eraPavingId) && LotPathMaterial.Find(eraPavingId)?.IsAvailable == true;
                bool plank = road.surfaceId == FoundersStreetId && !modernPaving;
                string styleId = modernPaving ? eraPavingId : plank ? LotPathMaterial.Plankboard : LotPathMaterial.Bluestone;
                var style = LotPathMaterial.Find(styleId);
                var path = new LotPathDefinition { points = road.points, width = road.width, closed = road.closed, curved = road.curved };
                var line = RoadCenterline(road).ToList();
                if (line.Count < 2) continue;
                if (road.closed && line[0] != line[^1]) line.Add(line[0]);
                var minimum = line[0]; var maximum = line[0];
                foreach (var point in line) { minimum = Vector2.Min(minimum, point); maximum = Vector2.Max(maximum, point); }
                var reach = Vector2.one * (road.width * .5f + 6f);
                var search = Rect.MinMaxRect(minimum.x - reach.x, minimum.y - reach.y, maximum.x + reach.x, maximum.y + reach.y);
                var nearby = frontages.FindAll(edge => search.Overlaps(Rect.MinMaxRect(Mathf.Min(edge.A.x, edge.B.x),
                    Mathf.Min(edge.A.y, edge.B.y), Mathf.Max(edge.A.x, edge.B.x), Mathf.Max(edge.A.y, edge.B.y))));
                if (modernPaving) nearby.Clear();
                if (!paved && nearby.Count == 0) continue;
                for (int side = -1; side <= 1; side += 2)
                {
                    if (plank)
                    {
                        var shops = shopFrontages.FindAll(edge => search.Overlaps(Rect.MinMaxRect(Mathf.Min(edge.A.x, edge.B.x),
                            Mathf.Min(edge.A.y, edge.B.y), Mathf.Max(edge.A.x, edge.B.x), Mathf.Max(edge.A.y, edge.B.y))));
                        var earth = LotPathMaterial.Find(LotPathMaterial.ResidentialFootpath);
                        var dirtWalk = LotPathSurface.BuildMesh(path, earth.TileSize, DistrictLotSurfaces.PathLift, LotHeight,
                            shoulderSide: side, shoulderWidth: width, sharpCorners: road.junctionCorners);
                        if (shops.Count > 0) NeighborhoodFootpaths.ApplyCoverage(dirtWalk, line, shops, inverse: true);
                        var clippedDirt = pavementMask.Clip(dirtWalk, transform.localToWorldMatrix, .3f, road);
                        if (clippedDirt != dirtWalk) { Destroy(dirtWalk); dirtWalk = clippedDirt; }
                        Add("Soft dirt sidewalk", dirtWalk, Material(earth.Id + "-street", earth, soft: true, masked: shops.Count > 0));
                        if (shops.Count > 0)
                        {
                            var woodWalk = LotPathSurface.BuildMesh(path, style.TileSize, SidewalkLift, LotHeight,
                                shoulderSide: side, shoulderWidth: width, sharpCorners: road.junctionCorners);
                            NeighborhoodFootpaths.ApplyCoverage(woodWalk, line, shops, inverse: false);
                            var uv = new List<Vector4>(); woodWalk.GetUVs(1, uv);
                            woodWalk.SetUVs(0, uv.ConvertAll(v => new Vector2(v.x / v.y, v.z / style.TileSize.y)));
                            var clippedWood = pavementMask.Clip(woodWalk, transform.localToWorldMatrix, exclude: road);
                            if (clippedWood != woodWalk) { Destroy(woodWalk); woodWalk = clippedWood; }
                            woodWalk.RecalculateTangents();
                            Add("Commercial plank sidewalk", woodWalk, Material(styleId + "-commercial", style, soft: false, masked: true));
                        }
                        continue;
                    }
                    bool dirt = false;
                    if (nearby.Count > 0)
                    {
                        var earth = LotPathMaterial.Find(LotPathMaterial.ResidentialFootpath);
                        var mesh = LotPathSurface.BuildMesh(path, earth.TileSize, DistrictLotSurfaces.PathLift, LotHeight,
                            shoulderSide: side, shoulderWidth: NeighborhoodFootpaths.Width, sharpCorners: road.junctionCorners);
                        NeighborhoodFootpaths.ApplyCoverage(mesh, line, nearby, inverse: false);
                        var trimmed = pavementMask.Clip(mesh, transform.localToWorldMatrix, .3f, road);
                        if (trimmed != mesh) { Destroy(mesh); mesh = trimmed; }
                        dirt = mesh.colors.Any(color => color.a > .001f);
                        if (dirt)
                        {
                            var ribbon = new List<Vector4>(); mesh.GetUVs(1, ribbon);
                            mesh.SetUVs(1, ribbon.ConvertAll(uv => new Vector4(uv.x / uv.y * NeighborhoodFootpaths.Width, NeighborhoodFootpaths.Width, uv.z, uv.w)));
                            var material = Material(earth.Id + "-neighborhood", earth, soft: true);
                            Add("Soft dirt walking path", mesh, material);
                        }
                        else Destroy(mesh);
                    }
                    if (!paved) continue;
                    // Preserve the street's authored cross sections and alignment.
                    var paving = LotPathSurface.BuildMesh(path, style.TileSize, SidewalkLift, LotHeight,
                        shoulderSide: side, shoulderWidth: width, sharpCorners: road.junctionCorners);
                    if (plank)
                    {
                        var ribbon = new List<Vector4>(); paving.GetUVs(1, ribbon);
                        paving.SetUVs(0, ribbon.ConvertAll(uv => new Vector2(uv.x / uv.y, uv.z / style.TileSize.y)));
                        paving.RecalculateTangents();
                    }
                    if (dirt) NeighborhoodFootpaths.ApplyCoverage(paving, line, nearby, inverse: true);
                    var trimmedPaving = pavementMask.Clip(paving, transform.localToWorldMatrix, exclude: road);
                    if (trimmedPaving != paving) { Destroy(paving); paving = trimmedPaving; }
                    Add(plank ? "Plank sidewalk" : "Bluestone sidewalk", paving,
                        Material(styleId + (dirt ? "-transition" : ""), style, soft: false, masked: dirt));
                }
            }

            PresentBoulevardMedians(visibleRoads,Add);
            var planting=RefreshAutomaticStreetTreesStages();while(planting.MoveNext()){if(paced)yield return planting.Current;}

            Material Material(string key, LotPathMaterial style, bool soft, bool masked = false)
            {
                if (materials.TryGetValue(key, out var existing)) return existing;
                var material = style.CreateMaterial();
                if (soft || masked)
                {
                    var treatment = new LotPathDefinition { width = soft ? NeighborhoodFootpaths.Width : width, materialId = style.Id };
                    LotPathMaterial.ApplyEdgeTreatment(material, treatment, soft ? 40 : 0, 0);
                    material.SetFloat("_VertexOpacity", 1);
                    material.SetFloat("_NaturalFootpath", soft ? 1 : 0);
                }
                material.renderQueue = soft ? DistrictLotSurfaces.PathRenderQueue : DistrictLotSurfaces.SidewalkRenderQueue;
                materials.Add(key, material); sidewalkAssets.Add(material);
                return material;
            }
            void Add(string label, Mesh mesh, Material material)
            {
                var walk = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
                walk.transform.SetParent(districtSidewalkRoot, false);
                walk.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = walk.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.sortingOrder = MaxDistrictRoads + 1;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sidewalkAssets.Add(mesh);
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
            CancelQueuedSidewalks();
            ClearAutomaticStreetTrees();
            ClearRoadRecess();
            ClearStreetJunctionPresentation();
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
            districtRoadMeshes.Clear(); FreightRoadRevision++; MarkImmigrationSitesChanged();
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

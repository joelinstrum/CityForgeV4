using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.CameraSystem;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace CityForge.World
{
    [Serializable]
    public sealed class DistrictLotRecord
    {
        public string id;
        public int district;
        public Vector2 center;
        // Keep the saved quarter turns and store the extra half-step separately.
        public int rotation;
        public bool diagonalRotation;
        internal int RotationSteps
        {
            get => rotation * 2 + (diagonalRotation ? 1 : 0);
            set { rotation = value / 2; diagonalRotation = value % 2 != 0; }
        }
        internal int FacingDegrees => RotationSteps * 45;
        public float waterfrontDeckHeight;
        public LotDefinition definition;
        public LotCustomization customization;
    }

    public sealed partial class PrototypeRegionView
    {
        internal List<DistrictLotRecord> PlacedLots { get; } = new List<DistrictLotRecord>();
        private Transform lotInstances;
        private GameObject lotActions;
        private Text lotFeedback;
        private Text placeLotLabel, lotFacingLabel, selectedLotTitle, selectedLotDetails, selectedLotFeedback;
        private Button lotFacingButton;
        private GameObject selectedLotPanel;
        private DistrictLotRecord selectedDistrictLot;
        private LineRenderer selectedLotOutline;
        private readonly Dictionary<DistrictLotRecord, Transform> districtLotRoots = new();
        private readonly Dictionary<(DistrictLotRecord lot, string worker), GameObject> dockWorkerHomes = new();
        private Font lotFont;
        private int lotRotation;
        private GameObject playerLotWorkspace;
        private CityForgeCameraController lotCamera;

        private void BuildLotActions(Transform hud, Font font)
        {
            lotActions = new GameObject("District lot actions", typeof(RectTransform));
            lotActions.transform.SetParent(hud, false);
            var rect = (RectTransform)lotActions.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -23);
            rect.sizeDelta = new Vector2(556, 72);
            lotFont = font;
            placeLotLabel = AddLotButton(lotActions.transform, font, "Place saved lot", 0, 220, () =>
            {
                CancelRoadPlacement();
                if (movingDistrictLot != null) CancelLotPlacement();
                else if (PendingFounder != null) OpenFounderPicker();
                else PlaceDraftLot();
            });
            lotFacingLabel = AddLotButton(lotActions.transform, font, "Facing: 0°", 228, 128, () =>
            {
                if (movingDistrictLot != null) lotMoveRotation = (lotMoveRotation + 1) % 8;
                else lotRotation = (lotRotation + 1) % 8;
                RefreshLotFacing();
                lotPlacementEvaluated = false;
            });
            lotFacingButton = lotFacingLabel.GetComponentInParent<Button>(includeInactive: true);
            AddLotButton(lotActions.transform, font, "Customize centered lot", 364, 190, () => CustomizeLot(font));
            lotFeedback = HudText(lotActions.transform, "Lot feedback", font, 16);
            Position(lotFeedback.rectTransform, new Vector2(0, -39), new Vector2(520, 29));
            lotActions.SetActive(false);
            BuildSelectedLotPanel(hud, font);
        }

        private void RefreshLotFacing(DistrictLotRecord record = null)
        {
            if (!lotFacingLabel || !lotFacingButton) return;
            bool auto = movingDistrictLot == null && lotPlacementDraft?.IsWaterfront == true;
            lotFacingButton.interactable = !auto;
            lotFacingLabel.text = auto
                ? record == null ? "Water: Auto" : $"Water: {WorldWaterfrontSide(record)}"
                : $"Facing: {(movingDistrictLot == null ? lotRotation : lotMoveRotation) * 45}°";
        }

        private static readonly string[] WaterfrontDirections =
            { "North", "Northeast", "East", "Southeast", "South", "Southwest", "West", "Northwest" };

        private static string WorldWaterfrontSide(DistrictLotRecord record) =>
            WaterfrontDirections[(record.RotationSteps + 2 * (int)record.definition.waterfrontSide) % 8];

        private void BuildSelectedLotPanel(Transform hud, Font font)
        {
            selectedLotPanel = new GameObject("Selected lot", typeof(RectTransform), typeof(Image));
            selectedLotPanel.transform.SetParent(hud, false);
            selectedLotPanel.GetComponent<Image>().color = new Color(.07f, .10f, .14f, .97f);
            var rect = (RectTransform)selectedLotPanel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-18, -108);
            rect.sizeDelta = new Vector2(300, 234);

            selectedLotTitle = HudText(rect, "Lot name", font, 20);
            selectedLotTitle.supportRichText = false;
            Position(selectedLotTitle.rectTransform, new Vector2(14, -12), new Vector2(224, 48));
            selectedLotDetails = HudText(rect, "Lot details", font, 15);
            Position(selectedLotDetails.rectTransform, new Vector2(14, -64), new Vector2(270, 28));
            AddPanelButton("×", 250, -12, 36, () => SelectDistrictLot(null));
            AddPanelButton("Rotate left 45°", 14, -96, 130, () => RotateSelectedLot(-1));
            AddPanelButton("Rotate right 45°", 156, -96, 130, () => RotateSelectedLot(1));
            AddPanelButton("Move", 14, -140, 272, () =>
            {
                if (selectedDistrictLot != null && !DistrictLoading && !IsSaving)
                    BeginLotPlacement(selectedDistrictLot.definition, selectedDistrictLot);
            });
            selectedLotFeedback = HudText(rect, "Lot rotation feedback", font, 14);
            Position(selectedLotFeedback.rectTransform, new Vector2(14, -182), new Vector2(270, 46));
            selectedLotPanel.SetActive(false);

            void AddPanelButton(string label, int x, int y, int width, UnityEngine.Events.UnityAction action)
            {
                var text = AddLotButton(rect, font, label, x, width, action);
                ((RectTransform)text.transform.parent).anchoredPosition = new Vector2(x, y);
            }
        }

        private void SelectDistrictLot(DistrictLotRecord record)
        {
            selectedDistrictLot = record;
            if (!selectedLotPanel) return;
            selectedLotPanel.SetActive(record != null);
            if (selectedLotOutline) selectedLotOutline.enabled = record != null;
            if (record == null) return;
            if (!selectedLotOutline)
            {
                selectedLotOutline = new GameObject("Selected lot gold outline").AddComponent<LineRenderer>();
                selectedLotOutline.transform.SetParent(transform, false);
                selectedLotOutline.loop = true;
                selectedLotOutline.widthMultiplier = .48f;
                selectedLotOutline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                selectedLotOutline.receiveShadows = false;
                selectedLotOutline.sharedMaterial = Own(new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                    { color = new Color(1f, .72f, .22f) });
            }
            var water = record.definition.IsWaterfront ? DistrictLotWater() : null;
            DrawLotFootprint(record, selectedLotOutline, water);
            selectedLotTitle.text = record.definition.name;
            selectedLotDetails.text = record.definition.IsWaterfront
                ? $"Facing {record.FacingDegrees}°  ·  Water to the {WorldWaterfrontSide(record).ToLowerInvariant()}"
                : $"Facing {record.FacingDegrees}°";
            selectedLotFeedback.text = "";
        }

        private bool TrySelectDistrictLot(Ray ray)
        {
            var nature = GetComponent<NatureToolbar>();
            if (nature && nature.Selected != NatureTool.None) return false;
            var ground = ApprovedDistrict && ApprovedDistrict.DisplayTerrain ? ApprovedDistrict.DisplayTerrain : terrain;
            var collider = ground ? ground.GetComponent<TerrainCollider>() : null;
            float nearest = collider && collider.Raycast(ray, out var hit, float.PositiveInfinity)
                ? hit.distance + .01f : float.PositiveInfinity;
            DistrictLotRecord picked = null;
            foreach (var (record, root) in districtLotRoots)
            {
                if (record.definition.IsWaterfront)
                {
                    var deck = new Plane(Vector3.up, Vector3.up * record.waterfrontDeckHeight);
                    if (deck.Raycast(ray, out float distance) && distance < nearest &&
                        LotContains(record, LotPoint(ray.GetPoint(distance))))
                    {
                        nearest = distance;
                        picked = record;
                    }
                }
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                    if (renderer.enabled && renderer.bounds.IntersectRay(ray, out float distance) && distance < nearest)
                    {
                        nearest = distance;
                        picked = record;
                    }
            }
            if (picked == null && float.IsFinite(nearest))
            {
                var point = LotPoint(ray.GetPoint(nearest - .01f));
                picked = PlacedLots.Find(record => districtLotRoots.ContainsKey(record) && LotContains(record, point));
            }
            bool handled = picked != null || selectedDistrictLot != null;
            SelectDistrictLot(picked);
            return handled;

            static Vector2 LotPoint(Vector3 point) => new Vector2(point.x, point.z);
        }

        private void RotateSelectedLot(int turns)
        {
            var record = selectedDistrictLot;
            if (record == null || DistrictLoading || IsSaving || !districtLotRoots.ContainsKey(record)) return;
            var candidate = new DistrictLotRecord
            {
                id = record.id, district = record.district, center = record.center,
                definition = record.definition, RotationSteps = (record.RotationSteps + turns + 8) % 8
            };
            var problem = LotSiteProblem(candidate, DistrictLotWater(), editing: true);
            if (problem != null)
            {
                selectedLotFeedback.text = problem;
                return;
            }

            record.RotationSteps = candidate.RotationSteps;
            record.waterfrontDeckHeight = candidate.waterfrontDeckHeight;
            RebuildDistrictLots();
            selectedLotFeedback.text = "Lot rotated. Save the region to keep it.";
        }

        private static Text AddLotButton(Transform parent, Font font, string label, int x, int width,
            UnityEngine.Events.UnityAction onClick)
        {
            var root = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            Position((RectTransform)root.transform, new Vector2(x, 0), new Vector2(width, 36));
            var image = root.GetComponent<Image>();
            image.color = new Color(.12f, .17f, .21f, .97f);
            var button = root.GetComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null) button.onClick.AddListener(onClick);
            var text = HudText(root.transform, "Caption", font, 16);
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            return text;
        }

        private void CustomizeLot(Font font)
        {
            if (SelectedDistrict < 0 || DistrictLoading || IsSaving || playerLotWorkspace || lotPlacementDraft != null) return;
            var rig = GetComponentInChildren<CityForgeCameraController>();
            var focus = rig.CaptureRegionState().focus;
            var center = new Vector2(focus.x, focus.z);
            var record = PlacedLots.Find(item => InActiveDistrict(item.center) && LotContains(item, center));
            if (record == null)
            {
                lotFeedback.text = "Center the view over a placed lot to customize it.";
                return;
            }
            try
            {
                LotCustomizationService.Apply(record.definition, record.customization, LotAssetCatalog.Load());
                GetComponent<NatureToolbar>()?.Dismiss();
                var back = LotEditorWorkspace.Create(font, ClosePlayerLot, record.definition, record.customization,
                    customization =>
                    {
                        record.customization = customization;
                        RebuildDistrictLots();
                        lotFeedback.text = "Landscaping applied. Save the region to keep it.";
                    });
                playerLotWorkspace = back.GetComponentInParent<Canvas>().gameObject;
                lotCamera = rig;
                lotCamera.enabled = false;
                var events = UnityEngine.EventSystems.EventSystem.current;
                if (events) events.SetSelectedGameObject(back.gameObject);
            }
            catch (InvalidDataException error)
            {
                lotFeedback.text = error.Message;
            }
        }

        private void ClosePlayerLot()
        {
            if (!playerLotWorkspace) return;
            playerLotWorkspace.SetActive(false);
            Destroy(playerLotWorkspace);
            playerLotWorkspace = null;
            if (lotCamera) lotCamera.enabled = true;
            lotCamera = null;
        }

        private void PlaceDraftLot()
        {
            if (SelectedDistrict < 0 || DistrictLoading || IsSaving || PendingFounder != null) return;
            if (lotPlacementDraft != null)
            {
                CancelLotPlacement();
                return;
            }
            if (Capital == null)
            {
                lotFeedback.text = "Establish the capital on the region map first.";
                return;
            }
            try
            {
                var lot = LotLibrary.Local.List().Select(entry => entry.Lot).FirstOrDefault(entry => !IsFoundersLot(entry));
                if (lot == null) throw new FileNotFoundException();
                if (lot.IsWaterfront)
                {
                    var assetProblem = LotAssetProblem(lot, LotAssetCatalog.Load());
                    if (assetProblem != null) throw new InvalidDataException(assetProblem);
                    BeginLotPlacement(lot);
                    return;
                }
                var focus = GetComponentInChildren<CityForgeCameraController>().CaptureRegionState().focus;
                var record = new DistrictLotRecord
                {
                    id = Guid.NewGuid().ToString("N"), center = new Vector2(focus.x, focus.z),
                    district = DistrictAtWorld(focus), definition = lot, RotationSteps = lotRotation
                };
                var problem = LotAssetProblem(lot, LotAssetCatalog.Load()) ?? LotSiteProblem(record, DistrictLotWater());
                if (problem != null) throw new InvalidDataException(problem);
                PlacedLots.Add(record);
                RebuildDistrictLots();
                lotFeedback.text = "Lot placed. Save the region to keep it.";
            }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is ArgumentException || error is UnauthorizedAccessException)
            {
                lotFeedback.text = error is FileNotFoundException || error is DirectoryNotFoundException
                    ? "Save a lot in the Lot Editor first." : error.Message;
            }
        }

        private static string LotAssetProblem(LotDefinition lot, LotAssetCatalog catalog)
        {
            foreach (var item in lot.objects)
            {
                var asset = catalog ? catalog.Find(item.assetId) : null;
                if (asset == null || !asset.prefab) return "Missing asset: " + item.assetId;
                if (!LotRuntimeLoader.Fits(lot, item, asset)) return "An object is outside the saved lot boundaries.";
            }
            return null;
        }

        private WildlifeWaterAccess DistrictLotWater() => ApprovedDistrict && ApprovedDistrict.DisplayTerrain
            ? new WildlifeWaterAccess(ApprovedDistrict) : null;

        private string LotSiteProblem(DistrictLotRecord record, WildlifeWaterAccess water, bool editing = false)
        {
            var footprint = new DistrictLotFootprint(record);
            foreach (var other in PlacedLots)
                if ((record.id == null || record.id != other.id) && footprint.Overlaps(new DistrictLotFootprint(other)))
                    return "That lot overlaps another lot.";

            if (record.definition.IsWaterfront)
                return WaterfrontSiteProblem(record, InActiveDistrict, LotHeight, (point, height) =>
                    TryLotWater(point, height, water, out float level) ? level : (float?)null, editing);

            float low = float.PositiveInfinity, high = float.NegativeInfinity;
            var size = record.definition.Size;
            int stepsX = Mathf.CeilToInt(size.x / 2);
            int stepsY = Mathf.CeilToInt(size.y / 2);
            for (int z = 0; z <= stepsY; z++)
            for (int x = 0; x <= stepsX; x++)
            {
                var point = footprint.WorldPoint(new Vector2(size.x * ((float)x / stepsX - .5f),
                    size.y * ((float)z / stepsY - .5f)));
                if (!InActiveDistrict(point) || (water != null ? !water.Dry(point) :
                    !IsTownLand(point / MapSide + Vector2.one * .5f)))
                    return "Choose dry ground inside the district.";
                float height = LotHeight(point);
                low = Mathf.Min(low, height);
                high = Mathf.Max(high, height);
                if (!editing && high - low > 2f) return "Choose flatter ground.";
            }
            return null;
        }

        internal static string WaterfrontSiteProblem(DistrictLotRecord record, Func<Vector2, bool> inDistrict,
            Func<Vector2, float> ground, Func<Vector2, float, float?> waterLevel, bool editing = false)
        {
            const float spacing = 2f;
            var lot = record.definition;
            var half = lot.WaterfrontSize * .5f;
            float low = float.PositiveInfinity, high = float.NegativeInfinity;
            float shoreHeight = float.NegativeInfinity, highestWater = float.NegativeInfinity;
            bool outerEdgeWet = false;
            var footprint = new DistrictLotFootprint(record);
            for (float z = -half.y; z <= half.y; z += spacing)
            for (float x = -half.x; x <= half.x; x += spacing)
            {
                var point = WaterfrontPoint(new Vector2(x, z));
                if (!inDistrict(point)) return "Keep the whole waterfront lot inside the district.";
                float height = ground(point);
                var level = waterLevel(point, height);
                if (z >= lot.WaterfrontStart)
                {
                    if (level.HasValue)
                    {
                        highestWater = Mathf.Max(highestWater, level.Value);
                        if (z == half.y) outerEdgeWet = true;
                    }
                    if (z > lot.WaterfrontStart) continue;

                    // The split belongs to both bands. Sample just inland for the bank.
                    point = WaterfrontPoint(new Vector2(x, z - .10f));
                    height = ground(point);
                    level = waterLevel(point, height);
                    shoreHeight = Mathf.Max(shoreHeight, height);
                }
                if (!editing && level.HasValue) return "Keep the land rows on dry ground.";
                low = Mathf.Min(low, height);
                high = Mathf.Max(high, height);
                if (!editing && high - low > 2f) return "Choose flatter ground for the land rows.";
            }
            if (!editing && !outerEdgeWet) return "Extend the blue end out over water.";
            record.waterfrontDeckHeight = Mathf.Max(shoreHeight, highestWater + .4f);
            return null;

            Vector2 WaterfrontPoint(Vector2 local) =>
                footprint.WorldPoint(LotGroundOverlay.QuarterTurn(local, (int)lot.waterfrontSide));
        }

        private bool TryLotWater(Vector2 point, float height, WildlifeWaterAccess water, out float level)
        {
            if (water != null) return water.Wet(point, height, out _, out level);
            level = SeaLevel;
            if (Waterless) return false;
            var uv = point / MapSide + Vector2.one * .5f;
            foreach (var body in Data.WaterBodies)
            {
                if (!body.Bounds.Contains(uv) || !body.Contains(uv)) continue;
                level = body.playerChannel ? PlayerChannelGeometry.Level(body, point, MapSide) :
                    body.kind == WaterBodyKind.River && RegionalRiverNetwork.HasGroundFollowingWater(Data)
                        ? RegionalRiverNetwork.GroundWaterLevel(Data, uv.x, uv.y)
                        : body.hasSurfaceLevel ? body.surfaceLevelNormalized * VerticalHeight : SeaLevel;
                if (height <= level + .025f) return true;
            }
            level = SeaLevel;
            return height <= level + .025f && !IsTownLand(uv);
        }

        internal static Vector2 WaterfrontWorldPoint(DistrictLotRecord record, Vector2 local) =>
            new DistrictLotFootprint(record).WorldPoint(LotGroundOverlay.QuarterTurn(local, (int)record.definition.waterfrontSide));

        private static bool LotContains(DistrictLotRecord record, Vector2 point) =>
            new DistrictLotFootprint(record).Contains(point);

        private readonly struct DistrictLotFootprint
        {
            private readonly Vector2 center, half, right, forward;

            public DistrictLotFootprint(DistrictLotRecord record)
            {
                center = record.center;
                half = record.definition.Size * .5f;
                var axis = Quaternion.Euler(0, record.FacingDegrees, 0) * Vector3.right;
                right = new Vector2(axis.x, axis.z);
                forward = new Vector2(-right.y, right.x);
            }

            public DistrictLotFootprint(Vector2 center, Vector2 half)
            {
                this.center = center;
                this.half = half;
                right = Vector2.right;
                forward = Vector2.up;
            }

            public Vector2 WorldPoint(Vector2 local) => center + right * local.x + forward * local.y;

            public bool Contains(Vector2 point, float inset = 0f)
            {
                var offset = point - center;
                return Mathf.Abs(Vector2.Dot(offset, right)) <= half.x - inset &&
                    Mathf.Abs(Vector2.Dot(offset, forward)) <= half.y - inset;
            }

            public bool OverlapsCircle(Vector2 point, float radius)
            {
                var offset = point - center;
                var outside = new Vector2(Mathf.Max(0, Mathf.Abs(Vector2.Dot(offset, right)) - half.x),
                    Mathf.Max(0, Mathf.Abs(Vector2.Dot(offset, forward)) - half.y));
                return outside.sqrMagnitude < radius * radius;
            }

            public Rect Bounds
            {
                get
                {
                    var extent = new Vector2(Radius(Vector2.right), Radius(Vector2.up));
                    return new Rect(center - extent, extent * 2f);
                }
            }

            public bool Overlaps(DistrictLotFootprint other)
            {
                var delta = other.center - center;
                return Intersects(right, delta, other) && Intersects(forward, delta, other) &&
                    Intersects(other.right, delta, other) && Intersects(other.forward, delta, other);
            }

            private bool Intersects(Vector2 axis, Vector2 delta, DistrictLotFootprint other) =>
                Mathf.Abs(Vector2.Dot(delta, axis)) < Radius(axis) + other.Radius(axis) - .001f;

            private float Radius(Vector2 axis) =>
                half.x * Mathf.Abs(Vector2.Dot(axis, right)) + half.y * Mathf.Abs(Vector2.Dot(axis, forward));
        }

        private float LotHeight(Vector2 point)
        {
            if (ApprovedDistrict && ApprovedDistrict.DisplayTerrain) return ApprovedDistrict.Height(point);
            return terrain.SampleHeight(new Vector3(point.x, 0, point.y)) + terrain.transform.position.y;
        }

        internal bool HarborStoneOverlapsLot(Vector2 position, float radius, DistrictLotRecord except = null)
        {
            foreach (var lot in PlacedLots)
                if (lot != except && new DistrictLotFootprint(lot).OverlapsCircle(position, radius)) return true;
            return false;
        }

        private void RebuildDistrictLots()
        {
            if (zoneRenderer && zoneRenderer.enabled) RefreshZoneOverlay();
            RefreshDirtRoadEndFades();
            districtLotRoots.Clear();
            roadBuildings.Clear();
            dockWorkerHomes.Clear();
            if (selectedDistrictLot != null && (SelectedDistrict < 0 || !ActiveDistrictMembers.Contains(selectedDistrictLot.district)))
                SelectDistrictLot(null);
            if (ApprovedDistrict)
            {
                ApprovedDistrict.RefreshHarborStones();
                if (SelectedDistrict >= 0 && PlacedLots.Any(lot => InActiveDistrict(lot.center) && lot.definition.AttractsVessels) &&
                    !ApprovedDistrict.GetComponent<HarborVesselVisits>())
                    ApprovedDistrict.gameObject.AddComponent<HarborVesselVisits>().Initialize(ApprovedDistrict);
            }
            if (lotInstances)
            {
                lotInstances.gameObject.SetActive(false);
                Destroy(lotInstances.gameObject);
                lotInstances = null;
            }
            if (lotActions)
            {
                lotActions.SetActive(SelectedDistrict >= 0);
                placeLotLabel.text = PendingFounder != null ? "Founder’s Building…" : "Place saved lot";
            }
            if (SelectedDistrict < 0 || PlacedLots.Count == 0) return;
            var catalog = LotAssetCatalog.Load();
            lotInstances = new GameObject("Placed lots").transform;
            lotInstances.SetParent(transform, false);
            var active = new HashSet<int>(ActiveDistrictMembers);
            var camera = GetComponentInChildren<Camera>();
            bool meadowGrade = ApprovedDistrict && camera &&
                camera.GetUniversalAdditionalCameraData().renderPostProcessing;
            foreach (var record in PlacedLots)
            {
                if (!active.Contains(record.district)) continue;
                var root = new GameObject(record.definition.name).transform;
                root.SetParent(lotInstances, false);
                districtLotRoots.Add(record, root);
                var definition = record.definition;
                if (catalog && record.customization != null)
                {
                    try { definition = LotCustomizationService.Apply(definition, record.customization, catalog); }
                    catch (InvalidDataException error) { Debug.LogWarning(error.Message); }
                }
                root.SetPositionAndRotation(new Vector3(record.center.x, 0, record.center.y),
                    Quaternion.Euler(0, record.FacingDegrees, 0));
                Transform harborPorch = null;
                var porches = new List<(LotObjectDefinition item, Transform view)>();
                var characters = new List<(LotObjectDefinition item, Transform view)>();
                foreach (var item in definition.objects)
                {
                    var asset = catalog ? catalog.Find(item.assetId) : null;
                    if (asset == null || !asset.prefab) continue;
                    bool dockWorker = asset.IsCharacter && definition.AttractsVessels &&
                        item.characterScript.scriptedBehavior == LotScriptedBehavior.DockWorker;
                    if (dockWorker && !item.characterScript.dockStayVisible) continue;
                    var instance = LotRuntimeLoader.InstantiateObject(item, asset, root).transform;
                    var point = instance.position;
                    point.y = LotHeight(new Vector2(point.x, point.z));
                    if (definition.IsWaterfront && LotRuntimeLoader.Footprint(item, asset).Overlaps(definition.WaterfrontBounds))
                        point.y = Mathf.Max(point.y, record.waterfrontDeckHeight);
                    instance.position = point;
                    if (asset.category == "Buildings")
                    {
                        if (meadowGrade) DistrictLotSurfaces.MatchGradedBuilding(instance);
                        roadBuildings.Add(new RoadBuildingFootprint(instance, asset.footprint * item.scale));
                    }
                    if (item.assetId == LotCharacterDeck.HarborHouse)
                    {
                        harborPorch = instance;
                        porches.Add((item, instance));
                    }
                    if (asset.IsCharacter) characters.Add((item, instance));
                    if (dockWorker) dockWorkerHomes.Add((record, item.id), instance.gameObject);
                }
                foreach (var (item, character) in characters)
                {
                    var point = character.position;
                    foreach (var (building, porch) in porches)
                        if (LotCharacterDeck.TryHeight(building, item.position, out float deck))
                            point.y = Mathf.Max(point.y, porch.position.y + deck);
                    character.position = point;
                }
                if (definition.AttractsVessels && ApprovedDistrict && ApprovedDistrict.DisplayTerrain)
                    HarborLanding.Build(root, ApprovedDistrict, record, harborPorch);
                root.gameObject.AddComponent<DistrictLotSurfaces>().Build(definition, LotHeight, record.waterfrontDeckHeight, meadowGrade);
            }
            SelectDistrictLot(selectedDistrictLot);
        }

        internal GameObject DockWorkerHome(DistrictLotRecord lot, string worker) =>
            dockWorkerHomes.TryGetValue((lot, worker), out var home) ? home : null;

        internal HarborLanding HarborLandingFor(DistrictLotRecord lot) =>
            districtLotRoots.TryGetValue(lot, out var root) && root ? root.GetComponentInChildren<HarborLanding>() : null;

        internal static void ValidateLotRecords(DistrictLotRecord[] records, int districtCount)
        {
            if (records == null) return;
            var ids = new HashSet<string>();
            foreach (var record in records)
            {
                if (record == null || !Guid.TryParseExact(record.id, "N", out _) || !ids.Add(record.id) ||
                    record.district < 0 || record.district >= districtCount ||
                    !float.IsFinite(record.center.x) || !float.IsFinite(record.center.y) ||
                    record.rotation < 0 || record.rotation > 3 || record.definition == null || !float.IsFinite(record.waterfrontDeckHeight))
                    throw new InvalidDataException("Invalid placed lot.");
                var validated = LotDefinition.Parse(JsonUtility.ToJson(record.definition));
                if (record.definition.schema != LotDefinition.CurrentSchema)
                    record.definition = validated;
            }
        }
    }
}

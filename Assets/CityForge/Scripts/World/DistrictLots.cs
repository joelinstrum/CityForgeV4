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
        public string growthBuildingId;
        public int storedTimber, receivedTimber;
        public List<LotResourceAmount> storedResources = new(), receivedResources = new();
        public List<LotVehicleJobState> vehicleJobs = new();
    }

    public sealed partial class PrototypeRegionView
    {
        internal List<DistrictLotRecord> PlacedLots { get; } = new List<DistrictLotRecord>();
        private Transform lotInstances;
        internal int LotPresentationBuilds { get; private set; }
        private GameObject lotActions;
        private Text lotFeedback;
        private Text placeLotLabel, lotFacingLabel, selectedLotTitle, selectedLotDetails, selectedLotFeedback;
        private Button lotFacingButton, changeBuildingButton, deliveryRoleButton, selectedLotDetonateButton;
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
            rect.sizeDelta = new Vector2(556, 92);
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
            Position(lotFeedback.rectTransform, new Vector2(0, -39), new Vector2(556, 46));
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
            rect.sizeDelta = new Vector2(300, 278);

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
            changeBuildingButton = AddPanelButton("Change building", 14, -184, 272, ChangeSelectedLotBuilding);
            deliveryRoleButton = AddPanelButton("Receives: None · change", 14, -228, 272, () =>
            {
                var lot = selectedDistrictLot;
                if (lot == null || DistrictLoading || IsSaving) return;
                // Clone before editing: a receiver role belongs to this placed lot instance.
                lot.definition = LotDefinition.Parse(JsonUtility.ToJson(lot.definition));
                lot.definition.deliveryRole = (LotDeliveryRole)(((int)lot.definition.DeliveryRole + 1) % 3);
                lot.definition.deliveryRoleConfigured = true;
                SelectDistrictLot(lot);
                selectedLotFeedback.text = "Delivery role changed. Save the region to keep it.";
            });
            selectedLotDetonateButton = AddPanelButton("Detonate", 14, -272, 272, DetonateSelectedLot);
            selectedLotDetonateButton.GetComponent<Image>().color = new Color(.40f, .14f, .10f, 1f);
            selectedLotFeedback = HudText(rect, "Lot action feedback", font, 14);
            Position(selectedLotFeedback.rectTransform, new Vector2(14, -226), new Vector2(270, 46));
            selectedLotPanel.SetActive(false);

            Button AddPanelButton(string label, int x, int y, int width, UnityEngine.Events.UnityAction action)
            {
                var text = AddLotButton(rect, font, label, x, width, action);
                ((RectTransform)text.transform.parent).anchoredPosition = new Vector2(x, y);
                return text.GetComponentInParent<Button>(includeInactive: true);
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
            bool growable = GrowthBuildingIndex(record, LotAssetCatalog.Load()) >= 0;
            changeBuildingButton.gameObject.SetActive(growable);
            var panel = (RectTransform)selectedLotPanel.transform;
            panel.sizeDelta = new Vector2(300, growable ? 366 : 322);
            ((RectTransform)deliveryRoleButton.transform).anchoredPosition = new Vector2(14, growable ? -228 : -184);
            deliveryRoleButton.GetComponentInChildren<Text>().text = "Receives: " + (record.definition.DeliveryRole switch
            { LotDeliveryRole.LumberMill => "Lumber Mill", LotDeliveryRole.TownCenter => "Town Center", _ => "None" }) + " · change";
            ((RectTransform)selectedLotDetonateButton.transform).anchoredPosition = new Vector2(14, growable ? -272 : -228);
            Position(selectedLotFeedback.rectTransform, new Vector2(14, growable ? -314 : -270), new Vector2(270, 46));
            RefreshSelectedFreight(record);
        }

        internal void RefreshSelectedFreight(DistrictLotRecord record)
        {
            if (record != selectedDistrictLot || !selectedLotFeedback) return;
            var job = record.vehicleJobs?.FirstOrDefault();
            var item = job == null ? null : record.definition.objects.Find(candidate => candidate.id == job.objectId);
            if (job != null && item != null)
            {
                var resource = job.cargo > 0 ? job.cargoResource : item.vehicleScript.resource;
                string state = job.phase switch
                {
                    LotFreightPhase.Loading when job.cargo >= item.vehicleScript.Capacity => "Full · waiting",
                    LotFreightPhase.Loading => "Loading", LotFreightPhase.Unloading => "Unloading",
                    LotFreightPhase.Outbound => "Delivering", LotFreightPhase.Returning => "Returning empty", _ => "Empty"
                };
                selectedLotFeedback.text = $"{VehicleResources.Label(resource)}: {job.cargo}/{item.vehicleScript.Capacity} · {state}\nAt lot: {VehicleResources.Stored(record, resource)} · Delivered: {VehicleResources.Received(record, resource)}";
            }
            else selectedLotFeedback.text = $"Timber at lot: {record.storedTimber} · Delivered: {record.receivedTimber}";
        }

        private bool TrySelectDistrictLot(Ray ray)
        {
            var nature = GetComponent<NatureToolbar>();
            if (nature && nature.Selected != NatureTool.None) return false;
            var picked = PickDistrictLot(ray);
            bool handled = picked != null || selectedDistrictLot != null;
            SelectDistrictLot(picked);
            return handled;
        }

        private DistrictLotRecord PickDistrictLot(Ray ray)
        {
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
            return picked;

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

        private static int GrowthBuildingIndex(DistrictLotRecord record, LotAssetCatalog catalog)
        {
            var lot = record.definition;
            if (lot.type != LotType.Zoning || !lot.zoningLayout.Configured || !catalog) return -1;
            if (!string.IsNullOrEmpty(record.growthBuildingId))
                return lot.objects.FindIndex(item => item.id == record.growthBuildingId);

            // Older grown lots append the building after the authored objects.
            int last = lot.objects.Count - 1;
            if (last < 0) return -1;
            var layout = lot.zoningLayout;
            var asset = catalog.Find(lot.objects[last].assetId);
            return asset?.CanPreviewGrowable(layout.zone, layout.wealth, lot.firstEraId) == true ? last : -1;
        }

        private void ChangeSelectedLotBuilding()
        {
            var record = selectedDistrictLot;
            if (record == null || DistrictLoading || IsSaving || !districtLotRoots.ContainsKey(record)) return;
            var catalog = LotAssetCatalog.Load();
            int index = GrowthBuildingIndex(record, catalog);
            if (index < 0) return;

            var draft = LotDefinition.Parse(JsonUtility.ToJson(record.definition));
            var current = draft.objects[index];
            draft.objects.RemoveAt(index);
            string era = ResidentialGround.LotEra(draft);
            var choices = LotGrowableBuildings.Candidates(draft, catalog)
                .Where(item => catalog.Find(item.assetId).eraId == era).ToArray();
            int currentIndex = Array.FindIndex(choices, item => item.assetId == current.assetId);
            if (choices.Length == 0 || (choices.Length == 1 && currentIndex == 0))
            {
                selectedLotFeedback.text = "No other eligible building fits this lot.";
                return;
            }

            var building = choices[(currentIndex + 1) % choices.Length];
            building.id = current.id;
            draft.objects.Insert(index, building);
            draft.paths.RemoveAll(path => path.generatedResidentialEntry ||
                IsLegacyResidentialEntryPath(draft, current, path));
            AddResidentialEntryPath(record, building, draft);

            record.definition = draft;
            record.growthBuildingId = building.id;
            RebuildDistrictLots();
            selectedLotFeedback.text = $"Building changed to {catalog.Find(building.assetId).label}. Save the region to keep it.";
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
                    }, placedLotId: record.id);
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
            if (savedLotPicker)
            {
                CloseSavedLotPicker();
                return;
            }
            OpenSavedLotPicker();
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

        private string LotSiteProblem(DistrictLotRecord record, WildlifeWaterAccess water, bool editing = false, bool growing = false, bool replacing = false)
        {
            var footprint = new DistrictLotFootprint(record);
            foreach (var other in PlacedLots)
                if (!replacing && (record.id == null || record.id != other.id) && footprint.Overlaps(new DistrictLotFootprint(other)))
                    return "That lot overlaps another lot.";

            if (LotOverlapsDistrictRoad(record, growing ? ResidentialRoadCornerInset : 0f)) return "Keep lots clear of roads.";

            if (!growing && ExtractionSiteProblem(record) is { } captureProblem) return captureProblem;
            if (record.definition.IsWaterfront)
                return WaterfrontSiteProblem(record, InActiveDistrict, LotHeight, (point, height) =>
                    TryLotWater(point, height, water, out float level) ? level : (float?)null, editing);

            float low = float.PositiveInfinity, high = float.NegativeInfinity;
            var size = record.definition.Size;
            // Keep the grade limit comparable across different growable lot widths.
            float maxRise = growing ? Mathf.Clamp(Mathf.Min(size.x, size.y) * .16f, 2f, 5.2f) : 2f;
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
                if (!editing && high - low > maxRise) return "Choose flatter ground.";
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
            int rearDry = 0, rearSamples = 0;
            var footprint = new DistrictLotFootprint(record);
            for (float x = -half.x; x <= half.x; x += spacing)
            {
                float bankHeight = float.NegativeInfinity;
                for (float z = -half.y; z <= half.y; z += spacing)
                {
                    var point = WaterfrontPoint(new Vector2(x, z));
                    if (!inDistrict(point)) return "Keep the whole waterfront lot inside the district.";
                    float height = ground(point);
                    var level = waterLevel(point, height);
                    if (z == -half.y)
                    {
                        rearSamples++;
                        if (!level.HasValue) rearDry++;
                    }
                    if (level.HasValue)
                    {
                        highestWater = Mathf.Max(highestWater, level.Value);
                        if (z == half.y) outerEdgeWet = true;
                        continue;
                    }

                    if (z > lot.WaterfrontStart) continue;
                    // Use the last dry sample in each column for deck height.
                    bankHeight = height;
                    low = Mathf.Min(low, height);
                    high = Mathf.Max(high, height);
                }
                shoreHeight = Mathf.Max(shoreHeight, bankHeight);
            }

            if (!editing)
            {
                if (!outerEdgeWet) return "Extend the blue end out over water.";
                if (rearDry * 2 < rearSamples) return "Keep the landward edge connected to dry ground.";

                // Cell centers estimate area without counting the shoreline itself as an extra row.
                int wetCells = 0, cells = 0;
                for (float z = -half.y + spacing * .5f; z < half.y; z += spacing)
                for (float x = -half.x + spacing * .5f; x < half.x; x += spacing)
                {
                    var point = WaterfrontPoint(new Vector2(x, z));
                    if (!inDistrict(point)) return "Keep the whole waterfront lot inside the district.";
                    var level = waterLevel(point, ground(point));
                    cells++;
                    if (!level.HasValue) continue;
                    wetCells++;
                    highestWater = Mathf.Max(highestWater, level.Value);
                }

                float allowedWetFraction = Mathf.Max(.5f, lot.waterfrontRows / (float)lot.WaterfrontDepth);
                if (wetCells > cells * allowedWetFraction)
                    return "Move the waterfront lot farther onto land.";
                if (high - low > 2f) return "Choose flatter ground for the land rows.";
            }

            record.waterfrontDeckHeight = Mathf.Max(shoreHeight, highestWater + .4f);
            if (!float.IsFinite(record.waterfrontDeckHeight))
                record.waterfrontDeckHeight = ground(record.center);
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

        private bool hasSeasonalLotTrees;

        private void RebuildDistrictLots()
        {
            pedestrianRoutesDirty = true;
            hasSeasonalLotTrees = false;
            RefreshZoneOverlay();
            RefreshDirtRoadEndFades();
            districtLotRoots.Clear();
            roadBuildings.Clear();
            eagleBuildingBounds.Clear();
            lotTreePositions.Clear();
            eagleLotTrees.Clear();
            dockWorkerHomes.Clear();
            if (selectedDistrictLot != null && (SelectedDistrict < 0 || !ActiveDistrictMembers.Contains(selectedDistrictLot.district)))
                SelectDistrictLot(null);
            if (ApprovedDistrict)
            {
                ApprovedDistrict.RefreshHarborStones();
                ApprovedDistrict.Geography?.RefreshSettlements();
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
            PresentDistrictLots(PlacedLots);
            SelectDistrictLot(selectedDistrictLot);
        }

        private void PresentDistrictLots(IEnumerable<DistrictLotRecord> records)
        {
            LotPresentationBuilds++;
            pedestrianRoutesDirty = true;
            var catalog = LotAssetCatalog.Load();
            if (!lotInstances)
            {
                lotInstances = new GameObject("Placed lots").transform;
                lotInstances.SetParent(transform, false);
                lotInstances.gameObject.AddComponent<ResidentialGroundBatches>();
            }
            var residentialGround = lotInstances.GetComponent<ResidentialGroundBatches>();
            var active = new HashSet<int>(ActiveDistrictMembers);
            var camera = GetComponentInChildren<Camera>();
            var cameraRig = camera ? camera.GetComponent<CityForgeCameraController>() : null;
            bool meadowGrade = ApprovedDistrict && camera &&
                camera.GetUniversalAdditionalCameraData().renderPostProcessing;
            foreach (var record in records)
            {
                if (!active.Contains(record.district)) continue;
                var root = new GameObject(record.definition.name).transform;
                root.SetParent(lotInstances, false);
                districtLotRoots.Add(record, root);
                if (revealingLots.Contains(record)) root.gameObject.SetActive(false);
                var definition = record.definition;
                LotGrowableBuildings.CorrectLegacyCabinFacing(definition);
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
                foreach (var authored in definition.objects)
                {
                    var item = authored;
                    var asset = catalog ? catalog.Find(item.assetId) : null;
                    if (asset == null || !asset.prefab) continue;
                    if (item.varyTreeOnPlacement && asset.category == "Trees")
                        (item, asset) = VaryPlacedTree(record.id, definition, item, asset, catalog);
                    bool dockWorker = asset.IsCharacter && definition.AttractsVessels &&
                        item.characterScript.scriptedBehavior == LotScriptedBehavior.DockWorker;
                    if (dockWorker && !item.characterScript.dockStayVisible) continue;
                    hasSeasonalLotTrees |= asset.IsSeasonalTree;
                    var instance = LotRuntimeLoader.InstantiateObject(item, asset, root, VisualSeason, cameraRig).transform;
                    var point = instance.position;
                    point.y = LotHeight(new Vector2(point.x, point.z));
                    if (definition.IsWaterfront && LotRuntimeLoader.Footprint(item, asset).Overlaps(definition.WaterfrontBounds))
                        point.y = Mathf.Max(point.y, record.waterfrontDeckHeight);
                    instance.position = point;
                    if (asset.category == "Trees")
                    {
                        lotTreePositions.Add(new Vector2(point.x, point.z));
                        eagleLotTrees.Add(instance);
                    }
                    if (asset.category == "Buildings")
                    {
                        if (meadowGrade) DistrictLotSurfaces.MatchGradedBuilding(instance);
                        roadBuildings.Add(new RoadBuildingFootprint(instance, asset.footprint * item.scale));
                        RegisterEagleObstacle(instance);
                    }
                    ResidentialWindowLights.Attach(instance.gameObject, asset, TimeOfDay);
                    SetVehicleMotion(instance, asset.id, false);
                    if (item.vehicleScript.behavior != LotVehicleBehavior.None)
                        AttachVehicleScript(instance, item, asset, record, camera);
                    LamppostLight.Attach(instance.gameObject, asset, TimeOfDay);
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
                    if (item.characterScript.configured && item.characterScript.scriptedBehavior == LotScriptedBehavior.Lumberjack && ApprovedDistrict)
                        character.gameObject.AddComponent<DistrictLumberjack>().Initialize(ApprovedDistrict, camera, record);
                    if (item.characterScript.configured && item.characterScript.scriptedBehavior == LotScriptedBehavior.Shopkeeper &&
                        LotShopkeeper.TryEntrance(definition, catalog, item.position, null, out var door, out var forward))
                    {
                        var runner = character.gameObject.AddComponent<DistrictShopkeeper>();
                        runner.districtCamera = camera;
                        foreach (var animator in character.GetComponentsInChildren<Animator>())
                            animator.cullingMode = AnimatorCullingMode.CullCompletely;
                        runner.behavior = new LotShopkeeper(character, catalog.Find(item.assetId), door, forward, local =>
                        {
                            var world = root.TransformPoint(new Vector3(local.x, 0f, local.y));
                            world.y = LotHeight(new Vector2(world.x, world.z));
                            return root.InverseTransformPoint(world).y;
                        });
                    }
                }
                if (definition.AttractsVessels && ApprovedDistrict && ApprovedDistrict.DisplayTerrain)
                    HarborLanding.Build(root, ApprovedDistrict, record, harborPorch);
                residentialGround.Add(definition, record.id, ResidentialGround.LotEra(definition), root, LotHeight);
                root.gameObject.AddComponent<DistrictLotSurfaces>().Build(definition, LotHeight, record.waterfrontDeckHeight, meadowGrade);
            }
            residentialGround.Build();
            RefreshDistrictSidewalks();
        }

        private static (LotObjectDefinition item, LotAssetCatalogEntry asset) VaryPlacedTree(
            string lotId, LotDefinition lot, LotObjectDefinition authored, LotAssetCatalogEntry original, LotAssetCatalog catalog)
        {
            // Derive the choice from saved IDs so reloading, moving or changing seasons never rerolls it.
            uint seed = 2166136261;
            foreach (char character in lotId + authored.id)
                seed = unchecked((seed ^ character) * 16777619);
            var item = new LotObjectDefinition
            {
                id = authored.id, position = authored.position, assetId = authored.assetId,
                scale = Mathf.Clamp(authored.scale * Mathf.Lerp(.9f, 1.1f, (seed & 255) / 255f), .25f, 3f),
                yaw = Mathf.Repeat(authored.yaw + ((seed >> 8) & 255) * (360f / 256f), 360f)
            };
            var choices = catalog.entries.Where(asset => asset != null && asset.prefab &&
                asset.category == "Trees" && asset.group == original.group &&
                (!original.playerPlaceable || asset.playerPlaceable) &&
                LotRuntimeLoader.Fits(lot, item, asset))
                .OrderBy(asset => asset.id, StringComparer.Ordinal).ToArray();
            if (choices.Length == 0) return (authored, original);
            var chosen = choices[(int)((seed >> 16) % (uint)choices.Length)];
            item.assetId = chosen.id;
            return (item, chosen);
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
                LotFreightValidation.Validate(record);
                var validated = LotDefinition.Parse(JsonUtility.ToJson(record.definition));
                if (record.definition.schema != LotDefinition.CurrentSchema)
                    record.definition = validated;
            }
        }
    }
}

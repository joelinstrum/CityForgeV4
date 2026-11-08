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
    public sealed class DistrictLotRecord
    {
        public string id;
        public int district;
        public Vector2 center;
        public int rotation;
        public LotDefinition definition;
        public LotCustomization customization;
    }

    public sealed partial class PrototypeRegionView
    {
        internal List<DistrictLotRecord> PlacedLots { get; } = new List<DistrictLotRecord>();
        private Transform lotInstances;
        private GameObject lotActions;
        private Text lotFeedback;
        private Text placeLotLabel;
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
            placeLotLabel = AddLotButton(lotActions.transform, font, "Place saved lot at center", 0, 220, () =>
            {
                if (PendingFounder != null) OpenFounderPicker();
                else PlaceDraftLot();
            });
            var rotate = AddLotButton(lotActions.transform, font, "Facing: 0°", 228, 128, null);
            rotate.GetComponentInParent<Button>(true).onClick.AddListener(() =>
            {
                lotRotation = (lotRotation + 1) % 4;
                rotate.text = $"Facing: {lotRotation * 90}°";
                founderPreviewEvaluated = false;
            });
            AddLotButton(lotActions.transform, font, "Customize centered lot", 364, 190, () => CustomizeLot(font));
            lotFeedback = HudText(lotActions.transform, "Lot feedback", font, 16);
            Position(lotFeedback.rectTransform, new Vector2(0, -39), new Vector2(520, 29));
            lotActions.SetActive(false);
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
            if (SelectedDistrict < 0 || DistrictLoading || IsSaving || playerLotWorkspace || founderDraft != null) return;
            var rig = GetComponentInChildren<CityForgeCameraController>();
            var focus = rig.CaptureRegionState().focus;
            var center = new Vector2(focus.x, focus.z);
            var record = PlacedLots.Find(item => InActiveDistrict(item.center) && LotBounds(item).Contains(center));
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
            if (Capital == null)
            {
                lotFeedback.text = "Establish the capital on the region map first.";
                return;
            }
            try
            {
                var lot = LotLibrary.Local.List().Select(entry => entry.Lot).FirstOrDefault(entry => !IsFoundersLot(entry));
                if (lot == null) throw new FileNotFoundException();
                var focus = GetComponentInChildren<CityForgeCameraController>().CaptureRegionState().focus;
                var record = new DistrictLotRecord
                {
                    id = Guid.NewGuid().ToString("N"), center = new Vector2(focus.x, focus.z),
                    district = DistrictAtWorld(focus), definition = lot, rotation = lotRotation
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

        private string LotSiteProblem(DistrictLotRecord record, WildlifeWaterAccess water)
        {
            var bounds = LotBounds(record);
            foreach (var other in PlacedLots)
                if (bounds.Overlaps(LotBounds(other))) return "That lot overlaps another lot.";

            float low = float.PositiveInfinity, high = float.NegativeInfinity;
            int stepsX = Mathf.CeilToInt(bounds.width / 2);
            int stepsY = Mathf.CeilToInt(bounds.height / 2);
            for (int z = 0; z <= stepsY; z++)
            for (int x = 0; x <= stepsX; x++)
            {
                var point = new Vector2(Mathf.Lerp(bounds.xMin, bounds.xMax, (float)x / stepsX),
                    Mathf.Lerp(bounds.yMin, bounds.yMax, (float)z / stepsY));
                if (!InActiveDistrict(point) || (water != null ? !water.Dry(point) :
                    !IsTownLand(point / MapSide + Vector2.one * .5f)))
                    return "Choose dry ground inside the district.";
                float height = LotHeight(point);
                low = Mathf.Min(low, height);
                high = Mathf.Max(high, height);
                if (high - low > 2f) return "Choose flatter ground.";
            }
            return null;
        }

        private static Rect LotBounds(DistrictLotRecord record)
        {
            var size = record.definition.Size;
            if (record.rotation % 2 != 0) size = new Vector2(size.y, size.x);
            return new Rect(record.center - size * .5f, size);
        }

        private float LotHeight(Vector2 point)
        {
            if (ApprovedDistrict && ApprovedDistrict.DisplayTerrain) return ApprovedDistrict.Height(point);
            return terrain.SampleHeight(new Vector3(point.x, 0, point.y)) + terrain.transform.position.y;
        }

        private void RebuildDistrictLots()
        {
            if (lotInstances)
            {
                lotInstances.gameObject.SetActive(false);
                Destroy(lotInstances.gameObject);
                lotInstances = null;
            }
            if (lotActions)
            {
                lotActions.SetActive(SelectedDistrict >= 0);
                placeLotLabel.text = PendingFounder != null ? "Founder’s Building…" : "Place saved lot at center";
            }
            if (SelectedDistrict < 0 || PlacedLots.Count == 0) return;
            var catalog = LotAssetCatalog.Load();
            lotInstances = new GameObject("Placed lots").transform;
            lotInstances.SetParent(transform, false);
            var active = new HashSet<int>(ActiveDistrictMembers);
            foreach (var record in PlacedLots)
            {
                if (!active.Contains(record.district)) continue;
                var root = new GameObject(record.definition.name).transform;
                root.SetParent(lotInstances, false);
                var definition = record.definition;
                if (catalog && record.customization != null)
                {
                    try { definition = LotCustomizationService.Apply(definition, record.customization, catalog); }
                    catch (InvalidDataException error) { Debug.LogWarning(error.Message); }
                }
                foreach (var item in definition.objects)
                {
                    var asset = catalog ? catalog.Find(item.assetId) : null;
                    if (asset == null || !asset.prefab) continue;
                    LotRuntimeLoader.InstantiateObject(item, asset, root);
                }
                root.SetPositionAndRotation(new Vector3(record.center.x, 0, record.center.y),
                    Quaternion.Euler(0, record.rotation * 90, 0));
                foreach (Transform item in root)
                {
                    var point = item.position;
                    point.y = LotHeight(new Vector2(point.x, point.z));
                    item.position = point;
                }
                root.gameObject.AddComponent<DistrictLotSurfaces>().Build(definition, LotHeight);
            }
        }

        internal static void ValidateLotRecords(DistrictLotRecord[] records, int districtCount)
        {
            if (records == null) return;
            var ids = new HashSet<string>();
            foreach (var record in records)
            {
                if (record == null || !Guid.TryParseExact(record.id, "N", out _) || !ids.Add(record.id) ||
                    record.district < 0 || record.district >= districtCount ||
                    !float.IsFinite(record.center.x) || !float.IsFinite(record.center.y) ||
                    record.rotation < 0 || record.rotation > 3 || record.definition == null)
                    throw new InvalidDataException("Invalid placed lot.");
                LotDefinition.Parse(JsonUtility.ToJson(record.definition));
            }
        }
    }
}

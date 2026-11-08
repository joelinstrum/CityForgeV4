using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.CameraSystem;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private static readonly string[] FounderResources =
        {
            "timber", "stone", "iron", "coal", "fish", "fertile-soil", "pasture",
            "fresh-water", "natural-harbor", "scenic-beauty", "clay"
        };

        private RectTransform founderPicker;
        private LotAssetThumbnails founderThumbnails;
        private CityForgeCameraController founderCameraControl;
        private bool founderCameraWasEnabled, offeredFounderPicker;
        private LotDefinition founderDraft;
        private WildlifeWaterAccess founderWater;
        private Camera founderMapCamera;
        private TerrainCollider founderGround;
        private LineRenderer founderOutline;
        private Material founderOutlineMaterial;
        private Vector2 founderCenter;
        private string founderPreviewProblem;
        private bool founderPreviewEvaluated;
        private readonly List<RaycastResult> founderUiHits = new();

        private RegionTown PendingFounder => Capital is { awaitingFoundersBuilding: true } capital ? capital : null;
        private bool InCapitalDistrict(RegionTown capital) => SelectedDistrict >= 0 &&
            DistrictAnchor(capital.DistrictIndex(GridSide)) == SelectedDistrict;

        private static bool IsFoundersLot(LotDefinition lot) =>
            LotClassification.FindCategory(lot.category)?.id == "civics" &&
            LotClassification.ForCategory(lot.category).Any(child => child.id == "founders-buildings" &&
                (lot.subcategory == child.id || lot.subcategory == child.name || lot.subcategory == child.legacyName));

        private void OpenFounderPicker()
        {
            var capital = PendingFounder;
            if (capital == null || DistrictLoading || IsSaving || founderPicker || playerLotWorkspace) return;
            if (!InCapitalDistrict(capital))
            {
                lotFeedback.text = "Open the capital’s district to place its Founder’s Building.";
                return;
            }
            offeredFounderPicker = true;
            CancelFounderPlacement();
            GetComponent<NatureToolbar>()?.Dismiss();
            founderCameraControl = GetComponentInChildren<CityForgeCameraController>();
            founderCameraWasEnabled = founderCameraControl.enabled;
            founderCameraControl.enabled = false;

            founderPicker = FounderRect(lotActions.transform.parent, "Founder’s Building Picker", Vector2.zero, Vector2.zero);
            founderPicker.anchorMin = Vector2.zero;
            founderPicker.anchorMax = Vector2.one;
            founderPicker.offsetMin = founderPicker.offsetMax = Vector2.zero;
            founderPicker.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .65f);

            var panel = FounderRect(founderPicker, "Founder’s Building", Vector2.zero, new Vector2(720, 492));
            panel.gameObject.AddComponent<Image>().color = new Color(.10f, .12f, .09f);
            FounderText(panel, $"Choose a Founder’s Building for {capital.name}", new Vector2(0, 211), new Vector2(668, 48), 25);
            var close = AddLotButton(panel, lotFont, "Choose later", 0, 160, CloseFounderPicker);
            var closeRect = (RectTransform)close.transform.parent;
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = Vector2.one * .5f;
            closeRect.anchoredPosition = new Vector2(0, -215);

            var scrollRect = FounderRect(panel, "Saved Founder’s lots", new Vector2(0, 4), new Vector2(666, 356));
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.gameObject.AddComponent<Image>().color = new Color(.075f, .09f, .07f);
            scrollRect.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = scrollRect;
            var content = FounderRect(scrollRect, "Lot cards", Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            scroll.content = content;
            var catalog = LotAssetCatalog.Load();
            var resources = RegionalResources.InDistrict(capital.DistrictIndex(GridSide), GridSide);
            LotDefinition[] lots;
            try
            {
                lots = LotLibrary.Local.List().Select(entry => entry.Lot).Where(IsFoundersLot).ToArray();
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                FounderText(panel, "Could not read the saved lot library.", Vector2.zero, new Vector2(620, 80), 18);
                return;
            }
            if (lots.Length == 0)
                FounderText(panel, "Save a Civics → Founder’s Buildings lot in the Lot Editor to make it available here.",
                    Vector2.zero, new Vector2(590, 85), 18);

            founderThumbnails = new LotAssetThumbnails();
            content.sizeDelta = new Vector2(0, lots.Length * 146);
            for (int i = 0; i < lots.Length; i++)
            {
                var lot = lots[i];
                var missing = lot.requiredResources.Where(resource => !resources.TryGetValue(FounderResources[(int)resource], out var value) || value <= 0)
                    .Select(FounderResourceName).ToList();
                if (lot.requiresKiln) missing.Add("kiln access");
                int firstYear = LotClassification.Era(lot)?.FirstYear ?? lot.firstEraYear;
                string problem = lot.objects.Count == 0 ? "This lot has no placed assets." : LotAssetProblem(lot, catalog);
                if (problem == null && missing.Count > 0) problem = "Unavailable here • " + string.Join(", ", missing);
                if (problem == null && firstYear > capital.establishedYear) problem = $"Available from {firstYear}.";

                var card = FounderRect(content, lot.name, new Vector2(0, -73 - i * 146), new Vector2(652, 138));
                card.anchorMin = card.anchorMax = new Vector2(.5f, 1);
                var background = card.gameObject.AddComponent<Image>();
                background.color = new Color(.20f, .25f, .18f);
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                button.interactable = problem == null;
                button.onClick.AddListener(() => BeginFounderPlacement(lot));
                var thumbnail = FounderRect(card, "Lot thumbnail", new Vector2(-264, 0), new Vector2(100, 100))
                    .gameObject.AddComponent<RawImage>();
                thumbnail.texture = founderThumbnails.GetLot(lot, catalog);
                thumbnail.enabled = thumbnail.texture;
                thumbnail.raycastTarget = false;
                var name = FounderText(card, $"{lot.name}  ·  {lot.width} × {lot.length} tiles", new Vector2(57, 47), new Vector2(512, 30), 19);
                var requirements = lot.requiredResources.Select(FounderResourceName).ToList();
                if (lot.requiresKiln) requirements.Add("kiln access");
                FounderText(card, "Requires  ·  " + (requirements.Count == 0 ? "None" : string.Join(", ", requirements)),
                    new Vector2(57, 17), new Vector2(512, 33), 15).alignment = TextAnchor.MiddleLeft;
                FounderText(card, "Bonus  ·  " + FounderBonus(lot.capitalBonus), new Vector2(57, -17), new Vector2(512, 33), 15)
                    .alignment = TextAnchor.MiddleLeft;
                name.alignment = TextAnchor.MiddleLeft;
                var status = FounderText(card, problem ?? "Available · Click to place", new Vector2(57, -48), new Vector2(512, 32), 14);
                status.alignment = TextAnchor.MiddleLeft;
                status.color = problem == null ? new Color(.77f, .91f, .67f) : new Color(1f, .72f, .58f);
            }
        }

        private static string FounderResourceName(LotRequiredResource resource)
        {
            string name = FounderResources[(int)resource].Replace('-', ' ');
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        private static string FounderBonus(LotCapitalBonus bonus)
        {
            string effect = bonus.effect switch
            {
                LotCapitalEffect.PublicBuildingCostReduction => "lower public building costs",
                LotCapitalEffect.CrimeReduction => "lower crime",
                LotCapitalEffect.CivicPrestigeIncrease => "higher civic prestige",
                LotCapitalEffect.PublicBuildingRepairCostReduction => "lower public building repair costs",
                LotCapitalEffect.PortFeeRevenueIncrease => "higher port fee revenue",
                LotCapitalEffect.PopulationGrowthIncrease => "faster population growth",
                _ => null
            };
            if (effect == null) return "None";
            string uses = bonus.eligibleBuildings > 0 ? $" for the next {bonus.eligibleBuildings} buildings" : "";
            return $"{bonus.percent}% {effect}{uses}" + (bonus.requiresOperatingPort ? " · Requires an operating port" : "");
        }

        private static RectTransform FounderRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private Text FounderText(Transform parent, string caption, Vector2 position, Vector2 size, int fontSize)
        {
            var label = HudText(parent, "Founder label", lotFont, fontSize);
            label.rectTransform.anchoredPosition = position;
            label.rectTransform.sizeDelta = size;
            label.text = caption;
            label.alignment = TextAnchor.MiddleCenter;
            label.supportRichText = false;
            label.raycastTarget = false;
            return label;
        }

        private void CloseFounderPicker()
        {
            if (!founderPicker) return;
            founderPicker.gameObject.SetActive(false);
            Destroy(founderPicker.gameObject);
            founderPicker = null;
            founderThumbnails?.Dispose();
            founderThumbnails = null;
            if (founderCameraControl) founderCameraControl.enabled = founderCameraWasEnabled;
            founderCameraControl = null;
        }

        private void BeginFounderPlacement(LotDefinition lot)
        {
            if (!founderPicker || PendingFounder == null) return;
            CloseFounderPicker();
            founderDraft = lot;
            founderWater = DistrictLotWater();
            founderMapCamera = GetComponentInChildren<Camera>();
            var ground = ApprovedDistrict && ApprovedDistrict.DisplayTerrain ? ApprovedDistrict.DisplayTerrain : terrain;
            founderGround = ground ? ground.GetComponent<TerrainCollider>() : null;
            founderPreviewEvaluated = false;
            founderOutline = new GameObject("Founder lot footprint").AddComponent<LineRenderer>();
            founderOutline.transform.SetParent(transform, false);
            founderOutlineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            founderOutline.sharedMaterial = founderOutlineMaterial;
            founderOutline.widthMultiplier = .65f;
            founderOutline.loop = true;
            founderOutline.shadowCastingMode = ShadowCastingMode.Off;
            founderOutline.receiveShadows = false;
            founderOutline.enabled = false;
        }

        private void CancelFounderPlacement()
        {
            if (founderDraft == null) return;
            founderDraft = null;
            founderWater = null;
            founderMapCamera = null;
            founderGround = null;
            founderOutline.enabled = false;
            Destroy(founderOutline.gameObject);
            Destroy(founderOutlineMaterial);
            founderOutline = null;
            founderOutlineMaterial = null;
            if (lotFeedback) lotFeedback.text = "";
        }

        private bool UpdateFounderPlacement()
        {
            var capital = PendingFounder;
            if (capital == null || !InCapitalDistrict(capital))
            {
                offeredFounderPicker = false;
                CloseFounderPicker();
                CancelFounderPlacement();
                return false;
            }
            if (!offeredFounderPicker && !DistrictLoading && !IsSaving)
            {
                var rig = GetComponentInChildren<CityForgeCameraController>();
                var state = rig.CaptureRegionState();
                var point = (capital.position - Vector2.one * .5f) * MapSide;
                state.focus = new Vector3(point.x, LotHeight(point), point.y);
                rig.RestoreRegionState(state);
                OpenFounderPicker();
            }
            if (founderPicker)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) CloseFounderPicker();
                return true;
            }
            if (founderDraft == null) return false;
            if (DistrictLoading || IsSaving || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelFounderPlacement();
                return true;
            }
            UpdateFounderCursor(capital);
            return true;
        }

        private void UpdateFounderCursor(RegionTown capital)
        {
            founderOutline.enabled = false;
            var events = EventSystem.current;
            bool clicked = Input.GetMouseButtonDown(0);
            if (events && clicked)
            {
                founderUiHits.Clear();
                events.RaycastAll(new PointerEventData(events) { position = Input.mousePosition }, founderUiHits);
                if (founderUiHits.Count > 0) return;
            }
            else if (events && events.IsPointerOverGameObject()) return;

            if (!founderGround || !founderGround.Raycast(founderMapCamera.ScreenPointToRay(Input.mousePosition),
                out var hit, founderMapCamera.farClipPlane)) return;
            int district = capital.DistrictIndex(GridSide);
            var record = new DistrictLotRecord { district = district, definition = founderDraft, rotation = lotRotation };
            var half = LotBounds(record).size * .5f;
            var origin = DistrictOrigin(district);
            var local = new Vector2(hit.point.x, hit.point.z) - origin - half;
            float tile = LotDefinition.TileMeters;
            record.center = origin + half + new Vector2(Mathf.Round(local.x / tile), Mathf.Round(local.y / tile)) * tile;
            var bounds = LotBounds(record);
            var districtBounds = new Rect(origin, Vector2.one * DistrictSide);
            if (!founderPreviewEvaluated || record.center != founderCenter)
            {
                founderCenter = record.center;
                founderPreviewEvaluated = true;
                founderPreviewProblem = bounds.xMin < districtBounds.xMin || bounds.xMax > districtBounds.xMax ||
                    bounds.yMin < districtBounds.yMin || bounds.yMax > districtBounds.yMax
                    ? "Stay inside the capital’s district." : LotSiteProblem(record, founderWater);
                DrawFounderOutline(bounds);
            }

            founderOutline.enabled = true;
            lotFeedback.text = (founderPreviewProblem ?? "Click to place " + founderDraft.name) + " · Esc to cancel";
            if (clicked && founderPreviewProblem == null) PlaceFounderLot(capital, record);
        }

        private void PlaceFounderLot(RegionTown capital, DistrictLotRecord record)
        {
            var problem = LotSiteProblem(record, DistrictLotWater());
            if (problem != null)
            {
                founderWater = DistrictLotWater();
                founderPreviewEvaluated = false;
                return;
            }
            record.id = Guid.NewGuid().ToString("N");
            PlacedLots.Add(record);
            capital.awaitingFoundersBuilding = false;
            CancelFounderPlacement();
            RebuildDistrictLots();
            lotFeedback.text = "Founder’s Building placed. Save the region to keep it.";
        }

        private void DrawFounderOutline(Rect bounds)
        {
            founderOutlineMaterial.color = founderPreviewProblem == null ? new Color(.89f, .84f, .47f) : new Color(.93f, .34f, .23f);
            Vector2[] corners = { bounds.min, new Vector2(bounds.xMax, bounds.yMin), bounds.max, new Vector2(bounds.xMin, bounds.yMax) };
            const int samplesPerEdge = 16;
            founderOutline.positionCount = corners.Length * samplesPerEdge;
            for (int edge = 0; edge < corners.Length; edge++)
            for (int sample = 0; sample < samplesPerEdge; sample++)
            {
                var point = Vector2.Lerp(corners[edge], corners[(edge + 1) % corners.Length], (float)sample / samplesPerEdge);
                founderOutline.SetPosition(edge * samplesPerEdge + sample,
                    new Vector3(point.x, LotHeight(point) + .35f, point.y));
            }
        }
    }
}

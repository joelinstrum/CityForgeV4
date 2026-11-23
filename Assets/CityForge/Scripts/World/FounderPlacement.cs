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
        private LotDefinition lotPlacementDraft;
        private GameObject placementTerrainChoice;
        private DistrictLotRecord pendingTerrainPlacement;
        private void ShowPlacementTerrainChoice(RegionTown capital,DistrictLotRecord record)
        {
            pendingTerrainPlacement=JsonUtility.FromJson<DistrictLotRecord>(JsonUtility.ToJson(record));
            var overlay=FounderRect(townToolbar.parent,"Choose lot terrain",Vector2.zero,Vector2.zero);
            overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;
            overlay.gameObject.AddComponent<Image>().color=new Color(0,0,0,.5f);placementTerrainChoice=overlay.gameObject;
            var panel=FounderRect(overlay,"Lot terrain",Vector2.zero,new Vector2(650,260));
            var image=panel.gameObject.AddComponent<Image>();image.sprite=ZoneModalArtwork.Panel;image.type=Image.Type.Sliced;
            FounderText(panel,"How should this lot meet the ground?",new Vector2(0,88),new Vector2(610,42),25).color=TownGold;
            FounderText(panel,"Your placement is fixed. Choose the terrain before construction.",new Vector2(0,42),new Vector2(610,34),17).color=TownGold;
            void Choose(bool level){var placed=pendingTerrainPlacement;if(placed==null)return;placed.definition.levelLot=level;pendingTerrainPlacement=null;placementTerrainChoice.SetActive(false);Destroy(placementTerrainChoice);placementTerrainChoice=null;CommitLotPlacement(capital,placed,true);}
            var slope=TownButton(panel,lotFont,"Follow ground slope",24,-133,292,()=>Choose(false));
            var flat=TownButton(panel,lotFont,"Level lot",334,-133,292,()=>Choose(true));
            foreach(var button in new[]{slope,flat}){button.image.sprite=ZoneModalArtwork.Dark;button.image.type=Image.Type.Sliced;button.image.color=Color.white;}
            var cancel=TownButton(panel,lotFont,"Cancel placement",175,-199,300,()=>CancelLotPlacement());cancel.image.sprite=ZoneModalArtwork.Dark;cancel.image.type=Image.Type.Sliced;
        }
        private DistrictLotRecord movingDistrictLot;
        private int lotMoveRotation;
        private bool lotMoveDragging, lotMoveCursorActive;
        private Vector2 lotMoveGrabOffset, lotMovePress;
        private Texture2D lotMoveCursor;
        private WildlifeWaterAccess lotPlacementWater;
        private Camera lotPlacementCamera;
        private TerrainCollider lotPlacementGround;
        private LineRenderer lotOutline, waterfrontGuide;
        private Material lotOutlineMaterial, waterfrontGuideMaterial;
        private DistrictLotRecord evaluatedLotPlacement;
        private string lotPlacementProblem;
        private bool lotPlacementEvaluated;
        private readonly List<RaycastResult> lotPlacementUiHits = new();

        // The saved founding flag alone cannot detect a demolished Founder’s Building.
        private RegionTown PendingFounder => Capital is { } capital &&
            (capital.awaitingFoundersBuilding || !PlacedLots.Exists(lot => IsFoundersLot(lot.definition)))
                ? capital : null;
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
                SetLotFeedback("Open the capital’s district to place its Founder’s Building.");
                return;
            }
            offeredFounderPicker = true;
            CancelLotPlacement();
            lastRepaveSurface = null;
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
                LotCapitalEffect.ResourceCostReduction => $"lower {bonus.resource.ToString().ToLowerInvariant()} costs",
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
            BeginLotPlacement(lot);
        }

        private void BeginLotPlacement(LotDefinition lot, DistrictLotRecord moving = null)
        {
            CancelLotPlacement();
            lastRepaveSurface = null;
            GetComponent<NatureToolbar>()?.Dismiss();
            SelectDistrictLot(null);
            lotPlacementDraft = LotDefinition.Parse(JsonUtility.ToJson(lot));
            movingDistrictLot = moving;
            if (moving != null)
            {
                lotMoveRotation = moving.RotationSteps;
                placeLotLabel.text = "Cancel move";
                SetLotFeedback("Drag the lot to move it · Esc to cancel");
                SetLotMoveCursor(true);
            }
            RefreshLotFacing();
            lotPlacementWater = DistrictLotWater();
            lotPlacementCamera = GetComponentInChildren<Camera>();
            var ground = ApprovedDistrict && ApprovedDistrict.DisplayTerrain ? ApprovedDistrict.DisplayTerrain : terrain;
            lotPlacementGround = ground ? ground.GetComponent<TerrainCollider>() : null;
            lotPlacementEvaluated = false;
            lotOutline = new GameObject("Lot placement footprint").AddComponent<LineRenderer>();
            lotOutline.transform.SetParent(transform, false);
            lotOutlineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lotOutline.sharedMaterial = lotOutlineMaterial;
            lotOutline.widthMultiplier = .65f;
            lotOutline.loop = true;
            lotOutline.shadowCastingMode = ShadowCastingMode.Off;
            lotOutline.receiveShadows = false;
            lotOutline.enabled = false;
            if (lot.IsWaterfront)
            {
                waterfrontGuide = new GameObject("Waterfront rows guide").AddComponent<LineRenderer>();
                waterfrontGuide.transform.SetParent(lotOutline.transform, false);
                waterfrontGuideMaterial = new Material(lotOutlineMaterial) { color = new Color(.25f, .78f, 1f) };
                waterfrontGuide.sharedMaterial = waterfrontGuideMaterial;
                waterfrontGuide.widthMultiplier = .45f;
                waterfrontGuide.shadowCastingMode = ShadowCastingMode.Off;
                waterfrontGuide.receiveShadows = false;
                waterfrontGuide.enabled = false;
            }
        }

        private void CancelLotPlacement(bool restoreUi = true)
        {
            pendingTerrainPlacement=null;
            if(placementTerrainChoice){placementTerrainChoice.SetActive(false);Destroy(placementTerrainChoice);placementTerrainChoice=null;}
            CloseSavedLotPicker();
            DestroySavedLotGhost();
            if(extractionSphere)extractionSphere.SetActive(false);
            captureCached=false;
            if (lotPlacementDraft == null) return;
            var moving = movingDistrictLot;
            if (moving != null)
            {
                ResetLotMoveDrag();
                SetLotMoveCursor(false);
            }
            movingDistrictLot = null;
            lotPlacementDraft = null;
            evaluatedLotPlacement = null;
            if (restoreUi)
            {
                if (moving != null && placeLotLabel) placeLotLabel.text = "Place saved lot";
                if (moving != null && SelectedDistrict >= 0 && ActiveDistrictMembers.Contains(moving.district))
                    SelectDistrictLot(moving);
                RefreshLotFacing();
                if (lotFeedback) SetLotFeedback("");
            }
            lotPlacementWater = null;
            lotPlacementCamera = null;
            lotPlacementGround = null;
            if (lotOutline)
            {
                lotOutline.enabled = false;
                Destroy(lotOutline.gameObject);
            }
            Destroy(lotOutlineMaterial);
            if (waterfrontGuideMaterial) Destroy(waterfrontGuideMaterial);
            waterfrontGuideMaterial = null;
            lotOutline = null;
            waterfrontGuide = null;
            lotOutlineMaterial = null;
        }

        private bool UpdateLotPlacement()
        {
            var capital = PendingFounder;
            if (movingDistrictLot != null || capital == null || !InCapitalDistrict(capital))
            {
                if (movingDistrictLot == null) offeredFounderPicker = false;
                CloseFounderPicker();
                if (lotPlacementDraft == null) return false;
                if ((capital != null && movingDistrictLot == null) || SelectedDistrict < 0 ||
                    DistrictLoading || IsSaving || Input.GetKeyDown(KeyCode.Escape))
                    CancelLotPlacement();
                else UpdateLotCursor(null);
                return true;
            }
            if (capital.awaitingFoundersBuilding && !offeredFounderPicker && !DistrictLoading && !IsSaving)
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
            if (lotPlacementDraft == null) return false;
            if (DistrictLoading || IsSaving || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelLotPlacement();
                return true;
            }
            UpdateLotCursor(capital);
            return true;
        }

        private void UpdateLotCursor(RegionTown capital)
        {
            if(pendingTerrainPlacement!=null)return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (movingDistrictLot != null) lotMoveRotation = (lotMoveRotation + 1) % 8;
                else lotRotation = (lotRotation + 1) % 8;
                RefreshLotFacing();
                lotPlacementEvaluated = false;
            }
            var events = EventSystem.current;
            bool pressed = Input.GetMouseButtonDown(0);
            bool released = Input.GetMouseButtonUp(0);
            bool overUi = events && events.IsPointerOverGameObject();
            if (events && (pressed || released))
            {
                lotPlacementUiHits.Clear();
                events.RaycastAll(new PointerEventData(events) { position = Input.mousePosition }, lotPlacementUiHits);
                overUi = lotPlacementUiHits.Count > 0;
            }

            var mouse = (Vector2)Input.mousePosition;
            var ray = lotPlacementCamera.ScreenPointToRay(mouse);
            UpdateLotPointer(capital, ray, mouse, pressed, Input.GetMouseButton(0), released, overUi);
        }

        private float nextLotPreviewCheck;
        private void UpdateLotPointer(RegionTown capital, Ray ray, Vector2 mouse,
            bool pressed, bool held, bool released, bool overUi)
        {
            if(pendingTerrainPlacement!=null)return;
            if(!pressed&&!released&&!overUi&&lotPlacementEvaluated&&Time.unscaledTime<nextLotPreviewCheck)return;
            nextLotPreviewCheck=Time.unscaledTime+.0833f;
            if (lotGhost) lotGhost.SetActive(false);
            if(extractionSphere)extractionSphere.SetActive(false);
            lotOutline.enabled = false;
            if (waterfrontGuide) waterfrontGuide.enabled = false;
            Vector2 cursor = default;
            bool hasGround = !overUi && TryLotCursorPoint(ray, out cursor);
            if (movingDistrictLot != null)
            {
                SetLotMoveCursor(!overUi);
                if (pressed && hasGround) BeginLotMoveDrag(ray, cursor, mouse);
                if (!lotMoveDragging || (mouse - lotMovePress).sqrMagnitude < 16f)
                {
                    if (!held) ResetLotMoveDrag();
                    else if (lotMoveDragging) PreviewLotMove(movingDistrictLot);
                    lotPlacementEvaluated = false;
                    lotPlacementProblem = null;
                    DrawLotOutline(movingDistrictLot);
                    UpdateExtractionSphere(movingDistrictLot);
                    lotOutline.enabled = true;
                    return;
                }
                if (!hasGround || (!released && !held))
                {
                    if (!held) ResetLotMoveDrag();
                    return;
                }
                cursor += lotMoveGrabOffset;
            }
            else if (!hasGround) return;

            bool fixedDistrict = capital != null || (movingDistrictLot != null && IsFoundersLot(movingDistrictLot.definition));
            int district = capital != null ? capital.DistrictIndex(GridSide) :
                fixedDistrict ? movingDistrictLot.district : DistrictAtWorld(new Vector3(cursor.x, 0, cursor.y));
            if (district < 0) return;
            var record = new DistrictLotRecord
            {
                id = movingDistrictLot?.id, district = district, definition = lotPlacementDraft,
                RotationSteps = movingDistrictLot == null ? lotRotation : lotMoveRotation
            };
            var origin = DistrictOrigin(district);
            record.center = SnapLotCenter(record, origin, cursor);
            if(!record.definition.IsWaterfront&&!Input.GetKey(KeyCode.LeftShift)&&!Input.GetKey(KeyCode.RightShift))record.center=SnapLotToRoadEdge(record);
            // Keep the original cell containing the lot, including inside combined districts.
            {
                record.district = DistrictAtWorld(new Vector3(record.center.x, 0, record.center.y));
                if (record.district < 0) return;
            }
            if (!lotPlacementEvaluated || record.center != evaluatedLotPlacement.center ||
                record.district != evaluatedLotPlacement.district ||
                (LotExtraction.Applies(record.definition) && (captureRevision != WoodlandRevision || captureDiscoveries != RegionalResources.DiscoveryCount || captureSeason != VisualSeason)))
            {
                var districtBounds = ActiveDistrictBounds; // Founding may use any member of the capital’s combined district.
                if (record.definition.IsWaterfront && movingDistrictLot == null)
                    lotPlacementProblem = OrientWaterfrontPlacement(record, fixedDistrict, districtBounds);
                else
                    lotPlacementProblem = PlacementSiteProblem(record, fixedDistrict, districtBounds);
                evaluatedLotPlacement = record;
                lotPlacementEvaluated = true;
                RefreshLotFacing(record);
                DrawLotOutline(record);
            }

            UpdateSavedLotGhost(evaluatedLotPlacement);
            UpdateExtractionSphere(evaluatedLotPlacement);
            lotOutline.enabled = true;
            if (waterfrontGuide) waterfrontGuide.enabled = true;
            if (movingDistrictLot == null)
            {
                SetLotFeedback((lotPlacementProblem ?? "Click to place " + lotPlacementDraft.name) + " · Tab to rotate · Shift bypasses road snap · Esc to cancel");
                if (pressed && lotPlacementProblem == null) CommitLotPlacement(capital, evaluatedLotPlacement);
                return;
            }

            SetLotFeedback((lotPlacementProblem ?? "Release to place the lot") + " · Tab to rotate · Shift bypasses road snap · Esc to cancel");
            PreviewLotMove(lotPlacementProblem == null ? evaluatedLotPlacement : movingDistrictLot);
            if (!released) return;
            if (lotPlacementProblem == null) CommitLotPlacement(capital, evaluatedLotPlacement);
            if (movingDistrictLot != null) ResetLotMoveDrag();
        }

        private bool TryLotCursorPoint(Ray ray, out Vector2 cursor)
        {
            cursor = default;
            if (!lotPlacementGround || !lotPlacementGround.Raycast(ray, out var hit, lotPlacementCamera.farClipPlane))
                return false;
            cursor = new Vector2(hit.point.x, hit.point.z);
            if (!lotPlacementDraft.IsWaterfront || !TryLotWater(cursor, hit.point.y, lotPlacementWater, out float waterHeight))
                return true;
            var surface = new Plane(Vector3.up, Vector3.up * waterHeight);
            if (surface.Raycast(ray, out float distance) && distance < hit.distance)
            {
                var surfacePoint = ray.GetPoint(distance);
                var point = new Vector2(surfacePoint.x, surfacePoint.z);
                if (InActiveDistrict(point) && TryLotWater(point, LotHeight(point), lotPlacementWater, out _))
                    cursor = point;
            }
            return true;
        }

        private void BeginLotMoveDrag(Ray ray, Vector2 cursor, Vector2 mouse)
        {
            if (!districtLotRoots.TryGetValue(movingDistrictLot, out var root) || !root) return;
            if (!LotContains(movingDistrictLot, cursor) && !root.GetComponentsInChildren<MeshRenderer>().Any(
                renderer => renderer.enabled && renderer.bounds.IntersectRay(ray))) return;

            lotMoveDragging = true;
            lotMovePress = mouse;
            lotMoveGrabOffset = movingDistrictLot.center - cursor;
            lotPlacementEvaluated = false;
        }

        private void PreviewLotMove(DistrictLotRecord candidate)
        {
            if (!districtLotRoots.TryGetValue(movingDistrictLot, out var root) || !root) return;
            float lift = candidate.definition.IsWaterfront
                ? candidate.waterfrontDeckHeight - movingDistrictLot.waterfrontDeckHeight
                : LotHeight(candidate.center) - LotHeight(movingDistrictLot.center);
            root.SetPositionAndRotation(new Vector3(candidate.center.x, lift, candidate.center.y),
                Quaternion.Euler(0, candidate.FacingDegrees, 0));
        }

        private void ResetLotMoveDrag()
        {
            lotMoveDragging = false;
            lotPlacementEvaluated = false;
            if (movingDistrictLot != null && districtLotRoots.TryGetValue(movingDistrictLot, out var root) && root)
                root.SetPositionAndRotation(new Vector3(movingDistrictLot.center.x, 0, movingDistrictLot.center.y),
                    Quaternion.Euler(0, movingDistrictLot.FacingDegrees, 0));
        }

        private void SetLotMoveCursor(bool visible)
        {
            if (lotMoveCursorActive == visible) return;
            if (visible && !lotMoveCursor)
            {
                const int size = 32;
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    if (Arrow(x, y)) pixels[y * size + x] = new Color32(255, 238, 188, 255);
                    else if (Arrow(x - 1, y) || Arrow(x + 1, y) || Arrow(x, y - 1) || Arrow(x, y + 1))
                        pixels[y * size + x] = new Color32(24, 25, 28, 255);
                }
                lotMoveCursor = Own(new Texture2D(size, size, TextureFormat.RGBA32, false)
                    { name = "District lot move cursor", wrapMode = TextureWrapMode.Clamp });
                lotMoveCursor.SetPixels32(pixels);
                lotMoveCursor.Apply();
            }
            Cursor.SetCursor(visible ? lotMoveCursor : null, visible ? new Vector2(15, 16) : Vector2.zero, CursorMode.Auto);
            lotMoveCursorActive = visible;

            static bool Arrow(int x, int y)
            {
                int along = Mathf.Max(Mathf.Abs(x - 15), Mathf.Abs(y - 15));
                int across = Mathf.Min(Mathf.Abs(x - 15), Mathf.Abs(y - 15));
                return along <= 12 && (across <= 1 || (along >= 8 && across <= 12 - along));
            }
        }

        private static Vector2 SnapLotCenter(DistrictLotRecord record, Vector2 origin, Vector2 cursor)
        {
            // Keep the existing footprint-aligned grid at right angles. Diagonal lots
            // use grid intersections for their centers because their corners cannot align.
            var offset = record.diagonalRotation ? Vector2.zero :
                record.definition.SizeForRotation(record.rotation) * .5f;
            float tile = record.definition.IsWaterfront ? 2f : 1f;
            var local = (cursor - origin - offset) / tile;
            return origin + offset + new Vector2(Mathf.Round(local.x), Mathf.Round(local.y)) * tile;
        }

        private string PlacementSiteProblem(DistrictLotRecord record, bool founder, Rect districtBounds)
        {
            var bounds = new DistrictLotFootprint(record).Bounds;
            if (founder && (bounds.xMin < districtBounds.xMin || bounds.xMax > districtBounds.xMax ||
                bounds.yMin < districtBounds.yMin || bounds.yMax > districtBounds.yMax))
                return "Stay inside the capital’s district.";
            return LotSiteProblem(record, lotPlacementWater, editing: movingDistrictLot != null, replacing: true);
        }

        private string OrientWaterfrontPlacement(DistrictLotRecord record, bool founder, Rect districtBounds)
        {
            int preferred = evaluatedLotPlacement?.RotationSteps ?? lotRotation;
            var candidates = Enumerable.Range(0, 8).Select(offset =>
            {
                int rotation = (preferred + offset) % 8;
                record.RotationSteps = rotation;
                return (rotation, score: WaterfrontFacingScore(record));
            }).OrderByDescending(candidate => candidate.score).ToArray();

            string bestProblem = null;
            foreach (var candidate in candidates)
            {
                record.RotationSteps = candidate.rotation;
                string problem = PlacementSiteProblem(record, founder, districtBounds);
                if (problem == null) return null;
                bestProblem ??= problem;
            }
            record.RotationSteps = candidates[0].rotation;
            return bestProblem;
        }

        private int WaterfrontFacingScore(DistrictLotRecord record)
        {
            var half = record.definition.WaterfrontSize * .5f;
            int score = 0;
            for (int column = -1; column <= 1; column++)
            {
                float x = column * half.x * .8f;
                if (Wet(x, half.y)) score += 2;
                if (Wet(x, half.y + 4f)) score++;
                if (!Wet(x, -half.y)) score++;
            }
            return score;

            bool Wet(float x, float z)
            {
                var point = WaterfrontWorldPoint(record, new Vector2(x, z));
                return InActiveDistrict(point) && TryLotWater(point, LotHeight(point), lotPlacementWater, out _);
            }
        }

        private void CommitLotPlacement(RegionTown capital, DistrictLotRecord record, bool terrainChosen=false)
        {
            var problem = ExtractionSiteProblem(record, force:true) ?? LotSiteProblem(record, DistrictLotWater(), editing: movingDistrictLot != null, replacing: true);
            if (problem != null)
            {
                lotPlacementWater = DistrictLotWater();
                lotPlacementEvaluated = false;
                SetLotFeedback(problem);
                return;
            }
            if(!terrainChosen&&movingDistrictLot==null&&record.definition.type!=LotType.Zoning&&!record.definition.IsWaterfront){ShowPlacementTerrainChoice(capital,record);return;}
            ClearLotStructures(record);
            if (movingDistrictLot != null)
            {
                RemoveDistrictLotViews(new[]{movingDistrictLot});
                movingDistrictLot.definition=LotDefinition.Parse(JsonUtility.ToJson(record.definition));
                movingDistrictLot.center = record.center;
                movingDistrictLot.district = record.district;
                movingDistrictLot.RotationSteps = record.RotationSteps;
                movingDistrictLot.waterfrontDeckHeight = record.waterfrontDeckHeight;
                if (movingDistrictLot.cutToLowest)
                {
                    PrepareLotGrading(record);
                    movingDistrictLot.cutGroundHeight = record.cutGroundHeight;
                }
                ClearLotVegetation(movingDistrictLot);
                if (movingDistrictLot.definition.type == LotType.Zoning) QueueLotGrowth();
                var movedLot = movingDistrictLot;
                CancelLotPlacement();
                PresentDistrictLotBatch(new[]{movedLot},true);
                RefreshPlacedLotEnvironment(movedLot);
                SelectDistrictLot(movedLot);
                selectedLotFeedback.text = "Lot moved. Save the region to keep it.";
                return;
            }
            record.id = Guid.NewGuid().ToString("N");
            record.definition = LotDefinition.Parse(JsonUtility.ToJson(record.definition));
            record.authoredElevationVersion = 1;
            if (record.definition.type == LotType.Zoning) PrepareLotGrading(record);
            PlacedLots.Add(record);
            HoldLotPopulation(record);ReleaseLotPopulation(record);
            RegisterDistrictTownCenter(record);
            ClearLotVegetation(record);
            if (capital != null)
            {
                capital.awaitingFoundersBuilding = false;
                CancelLotPlacement();
            }
            else
                CancelLotPlacement();
            PresentDistrictLotBatch(new[]{record},true);
            RefreshPlacedLotEnvironment(record);
            StartCoroutine(LotPlacementDust(record));
            SetLotFeedback(capital != null ? "Founder’s Building placed. Save the region to keep it." :
                "Lot placed. Save the region to keep it.");
        }

        private void RefreshPlacedLotEnvironment(DistrictLotRecord record)
        {
            EnsureDistrictRats();
            if(!ApprovedDistrict)return;
            ApprovedDistrict.Geography?.RefreshSettlements();
            if(record.definition.IsWaterfront||record.definition.AttractsVessels)ApprovedDistrict.RefreshHarborStones();
            if(record.definition.AttractsVessels&&!ApprovedDistrict.GetComponent<HarborVesselVisits>())
                ApprovedDistrict.gameObject.AddComponent<HarborVesselVisits>().Initialize(ApprovedDistrict);
        }

        private void DrawLotOutline(DistrictLotRecord record)
        {
            lotOutlineMaterial.color = lotPlacementProblem == null ? new Color(.89f, .84f, .47f) : new Color(.93f, .34f, .23f);
            DrawLotFootprint(record, lotOutline, lotPlacementWater);
            const int samplesPerEdge = 16;
            if (waterfrontGuide)
            {
                // The U marks the end that may extend across the shoreline.
                float halfWidth = record.definition.WaterfrontSize.x * .5f;
                float outer = record.definition.WaterfrontSize.y * .5f;
                float split = record.definition.WaterfrontStart;
                Vector2[] shore = { new(-halfWidth, split), new(-halfWidth, outer), new(halfWidth, outer), new(halfWidth, split) };
                waterfrontGuide.positionCount = (shore.Length - 1) * samplesPerEdge + 1;
                for (int edge = 0; edge < shore.Length - 1; edge++)
                for (int sample = 0; sample < samplesPerEdge; sample++)
                    waterfrontGuide.SetPosition(edge * samplesPerEdge + sample, LotOutlinePoint(record, WaterfrontWorldPoint(record,
                        Vector2.Lerp(shore[edge], shore[edge + 1], (float)sample / samplesPerEdge)), lotPlacementWater));
                waterfrontGuide.SetPosition(waterfrontGuide.positionCount - 1,
                    LotOutlinePoint(record, WaterfrontWorldPoint(record, shore[^1]), lotPlacementWater));
            }
        }

        private void DrawLotFootprint(DistrictLotRecord record, LineRenderer outline, WildlifeWaterAccess water)
        {
            var footprint = new DistrictLotFootprint(record);
            var half = record.definition.Size * .5f;
            Vector2[] corners = { -half, new Vector2(half.x, -half.y), half, new Vector2(-half.x, half.y) };
            float? platform = record.definition.IsWaterfront || !record.definition.levelLot ? null :
                LotHeight(record.center)+record.definition.elevation+record.elevation; // Full grade sampling happens on commit only.
            const int samplesPerEdge = 16;
            outline.positionCount = corners.Length * samplesPerEdge;
            for (int edge = 0; edge < corners.Length; edge++)
            for (int sample = 0; sample < samplesPerEdge; sample++)
            {
                var local = Vector2.Lerp(corners[edge], corners[(edge + 1) % corners.Length], (float)sample / samplesPerEdge);
                var world=footprint.WorldPoint(local);
                outline.SetPosition(edge * samplesPerEdge + sample, platform.HasValue ? new Vector3(world.x,platform.Value+.35f,world.y) : LotOutlinePoint(record,world,water));
            }
        }

        private Vector3 LotOutlinePoint(DistrictLotRecord record, Vector2 point, WildlifeWaterAccess water)
        {
            float height = LotHeight(point);
            if (record.definition.IsWaterfront && TryLotWater(point, height, water, out float level))
                height = Mathf.Max(height, level);
            return new Vector3(point.x, height + .35f, point.y);
        }
    }
}

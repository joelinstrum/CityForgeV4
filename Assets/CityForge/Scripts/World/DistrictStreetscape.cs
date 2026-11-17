using CityForge.CameraSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private const string StreetscapeHistoryKey = "CityForge.Streetscape.Recent";
        private LotAssetCatalogEntry streetscapeAsset;
        private PointWireTool streetscapeWire;
        private Transform streetscapeRoot;
        private readonly Dictionary<DistrictPlacedActor, GameObject> streetscapeViews = new();
        private readonly Dictionary<Material, Material> streetscapeGhostMaterials = new();
        private DistrictPlacedActor movingStreetscape;
        private GameObject streetscapeGhost;
        private LotObjectDefinition streetscapeMoveDraft;
        private LineRenderer streetscapeOutline;
        private RectTransform streetscapePanel;
        private Vector2? streetscapeDragStart;
        private bool streetscapeDragging;

        private static string[] StreetscapeHistory() =>
            PlayerPrefs.GetString(StreetscapeHistoryKey).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

        private void ArmStreetscape(LotAssetCatalogEntry asset)
        {
            CloseTransportModal();
            CancelStreetscape();
            streetscapeAsset = asset;
            if (!DrawsFenceRun(asset))
            {
                ShowDistrictMessage($"Click to place {asset.label} · Esc to stop");
                return;
            }

            if (!streetscapeWire)
            {
                streetscapeWire = gameObject.AddComponent<PointWireTool>();
                streetscapeWire.Initialize(GetComponentInChildren<Camera>(), townToolbar.parent,
                    new StreetscapeSurface(this), new Color(.78f, .53f, 1f));
                streetscapeWire.OpenPath = true;
                streetscapeWire.HandleEscape = false;
                streetscapeWire.RepeatOnCompletion = true;
                streetscapeWire.RestrictUiToHud = true;
                streetscapeWire.CompletionRequested = points => PlaceFenceRun(points, false);
                streetscapeWire.LoopCompletionRequested = points => PlaceFenceRun(points, true);
            }
            streetscapeWire.Label = asset.label;
            streetscapeWire.DrawingHint = "Click to draw fence spans · Enter to finish · Click first marker to close · Esc to stop";
            streetscapeWire.Arm();
        }

        private static bool DrawsFenceRun(LotAssetCatalogEntry asset) =>
            asset.category == "Fences" && asset.fenceSpan >= .1f;

        private void CancelStreetscape()
        {
            if (streetscapeAsset == null) return;
            if (movingStreetscape != null && streetscapeViews.TryGetValue(movingStreetscape, out var original) && original)
                original.SetActive(true);
            movingStreetscape = null;
            streetscapeMoveDraft = null;
            streetscapeDragStart = null;
            streetscapeDragging = false;
            CloseStreetscapePanel();
            if (streetscapeOutline) streetscapeOutline.enabled = false;
            if (streetscapeGhost) { streetscapeGhost.SetActive(false); Destroy(streetscapeGhost); }
            streetscapeGhost = null;
            foreach (var material in streetscapeGhostMaterials.Values) Destroy(material);
            streetscapeGhostMaterials.Clear();
            streetscapeAsset = null;
            if (streetscapeWire) streetscapeWire.Cancel();
            if (districtMessage) ShowDistrictMessage("");
        }

        private bool UpdateStreetscape()
        {
            var screen = (Vector2)Input.mousePosition;
            bool click = Application.isFocused && Input.GetMouseButtonDown(0);
            if (streetscapeAsset == null)
            {
                if (!click || !CanBuildTown || !TownConstructionVisible || IsSaving || RoadContextOverUi(screen) ||
                    lotPlacementDraft != null || armedDistrictActor != null || zoneToolArmed ||
                    RoadPlacementArmed) return false;
                var nature = GetComponent<NatureToolbar>();
                if (nature && nature.Selected != NatureTool.None) return false;
                var camera = GetComponentInChildren<Camera>();
                if (!camera) return false;
                var ray = camera.ScreenPointToRay(screen);
                float nearest = float.PositiveInfinity;
                foreach (var (record, instance) in streetscapeViews)
                {
                    if (!instance || !instance.activeInHierarchy) continue;
                    foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                    {
                        if (!renderer.enabled || !renderer.bounds.IntersectRay(ray, out float distance) || distance >= nearest) continue;
                        nearest = distance;
                        movingStreetscape = record;
                    }
                }
                if (movingStreetscape == null) return false;
                streetscapeAsset = LotAssetCatalog.Load()?.Find(movingStreetscape.assetId);
                if (streetscapeAsset == null) { movingStreetscape = null; return false; }
                SelectDistrictLot(null);
                DrawStreetscapeOutline();
                OpenStreetscapePanel();
                streetscapeDragStart = screen;
                ShowDistrictMessage($"{streetscapeAsset.label} selected · Drag to move");
                click = false;
            }
            if (!CanBuildTown || !TownConstructionVisible || IsSaving || Input.GetKeyDown(KeyCode.Escape))
                CancelStreetscape();
            else if (movingStreetscape != null && streetscapeMoveDraft == null)
            {
                if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
                    DeleteSelectedStreetscape();
                else if (Input.GetKeyDown(KeyCode.M)) BeginStreetscapeMove(dragging: false);
                else if (Input.GetKeyDown(KeyCode.R)) RotateSelectedStreetscape();
                else if (streetscapeDragStart is Vector2 start && Input.GetMouseButton(0) &&
                    (screen - start).sqrMagnitude >= 36f)
                    BeginStreetscapeMove(dragging: true);
                else if (click && !RoadContextOverUi(screen))
                {
                    CancelStreetscape();
                    return UpdateStreetscape();
                }
                if (Input.GetMouseButtonUp(0)) streetscapeDragStart = null;
            }
            else if (movingStreetscape != null || !DrawsFenceRun(streetscapeAsset))
            {
                if (RoadContextOverUi(screen) || !TryRoadGround(screen, out var point))
                {
                    if (streetscapeGhost) streetscapeGhost.SetActive(false);
                    if (streetscapeDragging && Input.GetMouseButtonUp(0)) streetscapeDragging = false;
                    return true;
                }
                var item = streetscapeMoveDraft ?? new LotObjectDefinition { assetId = streetscapeAsset.id };
                if (streetscapeMoveDraft != null && Input.GetKeyDown(KeyCode.R))
                    item.yaw = Mathf.Repeat(item.yaw + 45f, 360f);
                PreviewStreetscape(item, point);
                if (streetscapeDragging ? !Input.GetMouseButtonUp(0) : !click) return true;
                streetscapeDragging = false;
                if (movingStreetscape == null)
                {
                    item.position = point;
                    ShowDistrictMessage(PlaceStreetscape(new[] { item }) ?? $"{streetscapeAsset.label} placed · Esc to stop");
                }
                else
                {
                    var problem = StreetscapePlacementProblem(item, point, DistrictLotWater());
                    if (problem != null) { ShowDistrictMessage(problem); return true; }
                    var record = movingStreetscape;
                    record.position = record.streetscape.position = point;
                    record.streetscape.yaw = item.yaw;
                    record.district = DistrictAtWorld(new Vector3(point.x, 0, point.y));
                    streetscapeViews[record].transform.SetPositionAndRotation(
                        new Vector3(point.x, LotHeight(point), point.y), Quaternion.Euler(0, item.yaw, 0));
                    streetscapeMoveDraft = null;
                    streetscapeViews[record].SetActive(true);
                    if (streetscapeGhost) streetscapeGhost.SetActive(false);
                    DrawStreetscapeOutline();
                    OpenStreetscapePanel();
                    ShowDistrictMessage("Prop moved.");
                }
            }
            return true;
        }

        private void OpenStreetscapePanel()
        {
            if (streetscapePanel || !townToolbar || movingStreetscape == null) return;
            streetscapePanel = FounderRect(townToolbar.parent, "Selected prop actions",
                Vector2.zero, new Vector2(276, 174));
            streetscapePanel.anchorMin = streetscapePanel.anchorMax = new Vector2(1f, .5f);
            streetscapePanel.anchoredPosition = new Vector2(-154, 0);
            StyleTownPanel(streetscapePanel, TownPanelColor);
            FounderText(streetscapePanel, streetscapeAsset.label, new Vector2(0, 66), new Vector2(256, 30), 20).color = TownGold;
            TownButton(streetscapePanel, lotFont, "Move", 12, -41, 252, () => BeginStreetscapeMove(dragging: false));
            TownButton(streetscapePanel, lotFont, "Rotate 45°", 12, -80, 252, RotateSelectedStreetscape);
            TownButton(streetscapePanel, lotFont, "Delete", 12, -119, 252, DeleteSelectedStreetscape);
        }

        private void CloseStreetscapePanel()
        {
            if (!streetscapePanel) return;
            streetscapePanel.gameObject.SetActive(false);
            Destroy(streetscapePanel.gameObject);
            streetscapePanel = null;
        }

        private void BeginStreetscapeMove(bool dragging)
        {
            if (movingStreetscape == null || streetscapeMoveDraft != null) return;
            streetscapeMoveDraft = JsonUtility.FromJson<LotObjectDefinition>(JsonUtility.ToJson(movingStreetscape.streetscape));
            streetscapeDragging = dragging;
            streetscapeViews[movingStreetscape].SetActive(false);
            if (streetscapeOutline) streetscapeOutline.enabled = false;
            CloseStreetscapePanel();
            ShowDistrictMessage(dragging ? "Release to move · Esc to cancel" : "Click to place · Esc to cancel");
        }

        private void RotateSelectedStreetscape()
        {
            if (movingStreetscape == null || streetscapeMoveDraft != null || IsSaving) return;
            var item = movingStreetscape.streetscape;
            float originalYaw = item.yaw;
            item.yaw = Mathf.Repeat(originalYaw + 45f, 360f);
            var problem = StreetscapePlacementProblem(item, item.position, DistrictLotWater());
            if (problem != null)
            {
                item.yaw = originalYaw;
                ShowDistrictMessage(problem);
                return;
            }
            streetscapeViews[movingStreetscape].transform.localRotation = Quaternion.Euler(0, item.yaw, 0);
            DrawStreetscapeOutline();
        }

        private void DeleteSelectedStreetscape()
        {
            if (movingStreetscape == null || IsSaving) return;
            var record = movingStreetscape;
            CancelStreetscape();
            PlacedActors.Remove(record);
            if (streetscapeViews.Remove(record, out var view) && view)
            {
                view.SetActive(false);
                Destroy(view);
            }
            ShowDistrictMessage("Prop deleted.");
        }

        private void DrawStreetscapeOutline()
        {
            if (!streetscapeOutline)
            {
                streetscapeOutline = new GameObject("Selected prop gold outline").AddComponent<LineRenderer>();
                streetscapeOutline.transform.SetParent(transform, false);
                streetscapeOutline.loop = true;
                streetscapeOutline.positionCount = 4;
                streetscapeOutline.widthMultiplier = .075f;
                streetscapeOutline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                streetscapeOutline.receiveShadows = false;
                streetscapeOutline.sharedMaterial = Own(new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                    { color = new Color(1f, .72f, .22f) });
            }
            var item = movingStreetscape.streetscape;
            var bounds = LotRuntimeLoader.Footprint(item, streetscapeAsset);
            var half = streetscapeAsset.footprint * (item.scale * .5f) + Vector2.one * .15f;
            if (item.fenceLength > 0f) half.x = item.fenceLength * .5f + .15f;
            var turn = Quaternion.Euler(0, item.yaw, 0);
            for (int i = 0; i < 4; i++)
            {
                var local = turn * new Vector3(i is 0 or 3 ? -half.x : half.x, 0, i < 2 ? -half.y : half.y);
                var point = bounds.center + new Vector2(local.x, local.z);
                streetscapeOutline.SetPosition(i, new Vector3(point.x, LotHeight(point) + .14f, point.y));
            }
            streetscapeOutline.enabled = true;
        }

        private void PreviewStreetscape(LotObjectDefinition item, Vector2 point)
        {
            if (!streetscapeGhost)
            {
                streetscapeGhost = LotRuntimeLoader.InstantiateObject(item, streetscapeAsset, transform);
                foreach (var collider in streetscapeGhost.GetComponentsInChildren<Collider>()) collider.enabled = false;
                foreach (var renderer in streetscapeGhost.GetComponentsInChildren<Renderer>())
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var slots = renderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        var original = slots[i];
                        if (!original || !original.HasProperty("_Surface") || !original.HasColor("_BaseColor")) continue;
                        if (!streetscapeGhostMaterials.TryGetValue(original, out var material))
                        {
                            material = new Material(original);
                            var color = material.GetColor("_BaseColor");
                            color.a *= .48f;
                            material.SetColor("_BaseColor", color);
                            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            LotGroundTexture.ConfigureTransparentLayer(material, LotGroundOverlay.MaxCount + 3);
                            streetscapeGhostMaterials.Add(original, material);
                        }
                        slots[i] = material;
                    }
                    renderer.sharedMaterials = slots;
                }
            }
            streetscapeGhost.transform.localRotation = Quaternion.Euler(0, item.yaw, 0);
            streetscapeGhost.transform.position = new Vector3(point.x, LotHeight(point), point.y);
            streetscapeGhost.SetActive(true);
        }

        private string PlaceFenceRun(IReadOnlyList<Vector2> points, bool closed)
        {
            if (streetscapeAsset == null || !CanPlaceRoad) return "Fence placement is unavailable.";
            if (!LotFenceLayout.TryBuild(points, closed, streetscapeAsset, MaxPlacedActors - PlacedActors.Count,
                    null, out var pieces, out var problem)) return problem ?? "Draw at least one fence span.";
            return PlaceStreetscape(pieces);
        }

        private string PlaceStreetscape(IReadOnlyList<LotObjectDefinition> pieces)
        {
            if (pieces.Count > MaxPlacedActors - PlacedActors.Count) return "The region has reached its placement limit.";
            var water = DistrictLotWater();
            foreach (var piece in pieces)
            {
                var problem = StreetscapePlacementProblem(piece, piece.position, water);
                if (problem != null) return problem;
            }
            foreach (var piece in pieces)
            {
                var record = new DistrictPlacedActor
                {
                    assetId = piece.assetId, position = piece.position, streetscape = piece,
                    district = DistrictAtWorld(new Vector3(piece.position.x, 0, piece.position.y))
                };
                PresentStreetscape(record, streetscapeAsset);
                PlacedActors.Add(record);
            }
            PlayerPrefs.SetString(StreetscapeHistoryKey, string.Join("|", StreetscapeHistory()
                .Prepend(streetscapeAsset.id).Distinct().Take(32)));
            return null;
        }

        private string StreetscapePlacementProblem(LotObjectDefinition piece, Vector2 center, WildlifeWaterAccess water)
        {
            var direction = new Vector2(Mathf.Cos(piece.yaw * Mathf.Deg2Rad), -Mathf.Sin(piece.yaw * Mathf.Deg2Rad));
            int steps = Mathf.Max(1, Mathf.CeilToInt(piece.fenceLength));
            for (int i = 0; i <= steps; i++)
            {
                var point = center + direction * (piece.fenceLength * ((float)i / steps - .5f));
                if (!InActiveDistrict(point) || TryLotWater(point, LotHeight(point), water, out _) ||
                    PickDistrictRoad(point) != null || roadBuildings.Any(building => building.Contains(point, .1f)))
                    return "Keep the placement on dry ground, clear of roads and buildings.";
            }
            return null;
        }

        private void PresentStreetscape(DistrictPlacedActor record, LotAssetCatalogEntry asset)
        {
            if (!streetscapeRoot)
            {
                streetscapeRoot = new GameObject("District Streetscape").transform;
                streetscapeRoot.SetParent(transform, false);
            }
            var cameraRig = GetComponentInChildren<CityForgeCameraController>();
            var instance = LotRuntimeLoader.InstantiateObject(record.streetscape, asset, streetscapeRoot,
                districtCamera: cameraRig);
            instance.transform.position = new Vector3(record.position.x, LotHeight(record.position), record.position.y);
            LamppostLight.Attach(instance, asset, TimeOfDay);
            SetVehicleMotion(instance.transform, asset.id, false);
            streetscapeViews[record] = instance;
        }

        private void RebuildStreetscape()
        {
            CancelStreetscape();
            streetscapeViews.Clear();
            if (streetscapeRoot)
            {
                streetscapeRoot.gameObject.SetActive(false);
                Destroy(streetscapeRoot.gameObject);
                streetscapeRoot = null;
            }
            if (SelectedDistrict < 0) return;
            var catalog = LotAssetCatalog.Load();
            if (!catalog) return;
            foreach (var record in PlacedActors)
                if (record.streetscape != null && ActiveDistrictMembers.Contains(record.district) &&
                    catalog.Find(record.assetId) is { } asset && asset.prefab)
                    PresentStreetscape(record, asset);
        }

        private sealed class StreetscapeSurface : PointWireSurface
        {
            private readonly PrototypeRegionView view;
            public StreetscapeSurface(PrototypeRegionView view) => this.view = view;
            public override bool Available => view && view.CanEditRoad && view.streetscapeAsset != null;
            public override bool CanEdit => Available;
            public override Collider Collider => view.RoadTerrain.GetComponent<TerrainCollider>();
            public override bool Contains(Vector2 point) => view.InActiveDistrict(point);
            public override float Height(Vector2 point) => view.LotHeight(point);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private const float ParkTreeRadius = 12f;
        private const float ParkTreeSpacing = 4f;
        private const float ParkTreeInterval = .18f;
        private readonly List<Vector2> lotTreePositions = new();
        private readonly System.Random parkTreeRandom = new();
        private LotAssetCatalogEntry[] parkTreeAssets;
        private bool parkTreeStroke, removingParkTrees, savedParkCursor;
        private LotAssetCatalogEntry selectedParkTree;
        private Vector2 lastParkTreePoint;
        private RectTransform parkRemoveCursor;
        private LineRenderer parkRemoveRing;
        private static readonly Color ParkRemoveRed = new(1f, .23f, .17f, .92f);
        private bool ParkTreeBrushArmed => parkTreeAssets != null || removingParkTrees;
        private float nextParkTreeTime;

        private void ParkTreeCard(string title, string description, float x,
            UnityEngine.Events.UnityAction action, bool single = false, bool remove = false)
        {
            var card = FounderRect(townPanel, title, new Vector2(x, 95), new Vector2(258, 76));
            var image = card.gameObject.AddComponent<Image>();
            image.color = new Color(.13f, .17f, .19f);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var tint = remove ? ParkRemoveRed : TownGold;
            if (!single)
            {
                ParkTreeIcon(card, new Vector2(-97, -6), 27, tint);
                ParkTreeIcon(card, new Vector2(-72, -6), 27, tint);
            }
            ParkTreeIcon(card, new Vector2(-85, 5), 37, tint);
            FounderText(card, title, new Vector2(36, 12), new Vector2(172, 28), 20).color = TownGold;
            FounderText(card, description, new Vector2(36, -15), new Vector2(172, 28), 14);
        }

        private static RawImage ParkTreeIcon(Transform parent, Vector2 offset, float size, Color tint)
        {
            var icon = FounderRect(parent, "Fir tree", offset, Vector2.one * size).gameObject.AddComponent<RawImage>();
            icon.texture = Resources.Load<Texture2D>("UI/NatureToolbarIcons");
            icon.uvRect = new Rect(.5f, 2f / 3f, .25f, 1f / 3f);
            icon.color = tint;
            icon.raycastTarget = false;
            return icon;
        }

        private LotAssetCatalogEntry[] AvailableParkTrees()
        {
            var catalog = LotAssetCatalog.Load();
            return catalog ? catalog.entries.Where(asset =>
                asset != null && asset.prefab && asset.playerPlaceable && asset.category == "Trees" &&
                (asset.group != "Tropical" || Climate == RegionClimate.Tropical)).ToArray()
                : System.Array.Empty<LotAssetCatalogEntry>();
        }

        private void OpenParkTreePicker()
        {
            if (!townPanel || !CanBuildTown || IsSaving) return;
            var available = AvailableParkTrees();
            if (available.Length == 0)
            {
                ShowDistrictMessage("No placeable trees are available in the Lot Editor catalog.");
                return;
            }

            var byId = available.ToDictionary(asset => asset.id);
            var recent = Enumerable.Reverse(PlacedActors).Where(actor => actor.tree && byId.ContainsKey(actor.assetId))
                .Select(actor => actor.assetId).Distinct().Take(4).Select(id => byId[id]).ToArray();
            var recentIds = new HashSet<string>(recent.Select(asset => asset.id));
            var other = available.Where(asset => !recentIds.Contains(asset.id))
                .OrderBy(asset => asset.label, System.StringComparer.CurrentCultureIgnoreCase).ToArray();

            var picker = FounderRect(townPanel, "Place a Tree picker", Vector2.zero, townPanel.sizeDelta);
            StyleTownPanel(picker, TownPanelColor);
            TownButton(picker, lotFont, "Back", 16, -9, 86, () =>
            {
                picker.gameObject.SetActive(false);
                Destroy(picker.gameObject);
            });
            FounderText(picker, "Place a Tree", new Vector2(0, 163), new Vector2(400, 34), 25).color = TownGold;
            TownButton(picker, lotFont, "×", (int)picker.sizeDelta.x - 47, -9, 36, CloseTownSection);

            const int width = 750;
            var window = FounderRect(picker, "Tree list", new Vector2(0, -23), new Vector2(width, 292));
            window.gameObject.AddComponent<Image>().color = new Color(.05f, .07f, .09f);
            window.gameObject.AddComponent<RectMask2D>();
            var scroll = window.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = window;
            var content = FounderRect(window, "Available trees", Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            scroll.content = content;

            int top = 6;
            AddGroup("Recently planted", recent);
            AddGroup(recent.Length > 0 ? "All other trees" : "Available trees", other);
            content.sizeDelta = new Vector2(0, top + 6);
            content.anchoredPosition = Vector2.zero;

            void AddGroup(string title, LotAssetCatalogEntry[] trees)
            {
                if (trees.Length == 0) return;
                var heading = FounderText(content, title, Vector2.zero, new Vector2(width - 20, 24), 16);
                heading.alignment = TextAnchor.MiddleLeft;
                heading.color = TownGold;
                Position(heading.rectTransform, new Vector2(10, -top), new Vector2(width - 20, 24));
                top += 28;
                for (int index = 0; index < trees.Length; index++)
                {
                    var asset = trees[index];
                    TownButton(content, lotFont, asset.label, 10 + index % 2 * 370, -top - index / 2 * 42,
                        360, () => ArmParkTreeBrush(false, asset));
                }
                top += (trees.Length + 1) / 2 * 42 + 8;
            }
        }

        private void ArmParkTreeBrush(bool remove, LotAssetCatalogEntry selected = null)
        {
            if (!CanBuildTown || !TownConstructionVisible || IsSaving) return;
            var assets = remove ? null : selected == null ? AvailableParkTrees() : new[] { selected };
            if (!remove && (assets == null || assets.Length == 0))
            {
                ShowDistrictMessage("No placeable trees are available in the Lot Editor catalog.");
                return;
            }

            CloseTownSection();
            CancelDistrictActor();
            SelectDistrictLot(null);
            parkTreeAssets = assets;
            selectedParkTree = selected;
            removingParkTrees = remove;
            if (remove)
            {
                savedParkCursor = Cursor.visible;
                if (!parkRemoveCursor)
                {
                    parkRemoveCursor = ParkTreeIcon(townToolbar.parent, Vector2.zero, 26, ParkRemoveRed).rectTransform;
                    parkRemoveCursor.gameObject.SetActive(false);
                    var shader = Resources.Load<Shader>("UI/ForestRemovalCircle");
                    if (shader)
                    {
                        parkRemoveRing = new GameObject("Park tree removal radius").AddComponent<LineRenderer>();
                        parkRemoveRing.transform.SetParent(transform, false);
                        parkRemoveRing.loop = true;
                        parkRemoveRing.positionCount = 48;
                        parkRemoveRing.widthMultiplier = .13f;
                        parkRemoveRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        parkRemoveRing.receiveShadows = false;
                        var material = Own(new Material(shader));
                        material.SetColor("_Color", ParkRemoveRed);
                        parkRemoveRing.sharedMaterial = material;
                        parkRemoveRing.enabled = false;
                    }
                }
            }
            ShowDistrictMessage(remove ? "Click or drag to remove individual trees · Esc to stop"
                : selected != null ? $"Click to plant {selected.label} · Esc to stop"
                : "Hold and drag to plant trees · Esc to stop");
        }

        private void CancelParkTreeBrush()
        {
            if (!ParkTreeBrushArmed) return;
            if (removingParkTrees)
            {
                Cursor.visible = savedParkCursor;
                if (parkRemoveCursor) parkRemoveCursor.gameObject.SetActive(false);
                if (parkRemoveRing) parkRemoveRing.enabled = false;
            }
            removingParkTrees = false;
            parkTreeAssets = null;
            selectedParkTree = null;
            parkTreeStroke = false;
            if (districtMessage) ShowDistrictMessage("");
        }

        private bool UpdateParkTreeBrush()
        {
            if (!ParkTreeBrushArmed) return false;
            if (!CanBuildTown || !TownConstructionVisible || IsSaving || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelParkTreeBrush();
                return true;
            }
            if (removingParkTrees)
            {
                UpdateParkTreeRemoval();
                return true;
            }
            if (!Application.isFocused || !Input.GetMouseButton(0))
            {
                parkTreeStroke = false;
                return true;
            }

            bool first = Input.GetMouseButtonDown(0);
            if (!first && (selectedParkTree != null || !parkTreeStroke)) return true;
            var screen = (Vector2)Input.mousePosition;
            if (RoadContextOverUi(screen) || !TryRoadGround(screen, out var center)) return true;
            if (first)
            {
                parkTreeStroke = true;
                nextParkTreeTime = Time.unscaledTime;
            }
            if (Time.unscaledTime < nextParkTreeTime) return true;
            nextParkTreeTime = Time.unscaledTime + ParkTreeInterval;
            if (PlacedActors.Count >= MaxPlacedActors)
            {
                CancelParkTreeBrush();
                ShowDistrictMessage("The region has reached its placed population limit.");
                return true;
            }

            var water = DistrictLotWater();
            if (selectedParkTree != null)
            {
                ShowDistrictMessage(TryPlantParkTree(center, water)
                    ? $"Planted {selectedParkTree.label} · Click to plant again · Esc to stop"
                    : $"Choose open, dry ground for {selectedParkTree.label} · Esc to stop");
                return true;
            }
            if (first && TryPlantParkTree(center, water)) return true;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float angle = (float)parkTreeRandom.NextDouble() * 2f * Mathf.PI;
                float radius = ParkTreeRadius * Mathf.Sqrt((float)parkTreeRandom.NextDouble());
                var point = center + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (TryPlantParkTree(point, water)) break;
            }
            return true;
        }

        private void UpdateParkTreeRemoval()
        {
            var screen = (Vector2)Input.mousePosition;
            Vector2 center = default;
            bool valid = Application.isFocused && !RoadContextOverUi(screen) &&
                TryRoadGround(screen, out center) && InActiveDistrict(center);
            parkRemoveCursor.gameObject.SetActive(valid);
            Cursor.visible = !valid && savedParkCursor;
            if (parkRemoveRing) parkRemoveRing.enabled = valid;
            if (!valid)
            {
                parkTreeStroke = false;
                return;
            }

            var hud = (RectTransform)townToolbar.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hud, screen, null, out var local);
            parkRemoveCursor.anchoredPosition = local;
            if (parkRemoveRing)
                for (int i = 0; i < parkRemoveRing.positionCount; i++)
                {
                    float angle = i * 2f * Mathf.PI / parkRemoveRing.positionCount;
                    var edge = center + ParkTreeRadius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    parkRemoveRing.SetPosition(i, new Vector3(edge.x, LotHeight(edge) + .12f, edge.y));
                }

            if (!Input.GetMouseButton(0))
            {
                parkTreeStroke = false;
                return;
            }
            bool first = Input.GetMouseButtonDown(0);
            if (!first && (!parkTreeStroke || (center - lastParkTreePoint).sqrMagnitude < .25f)) return;
            RemoveParkTrees(first ? center : lastParkTreePoint, center);
            parkTreeStroke = true;
            lastParkTreePoint = center;
        }

        private void RemoveParkTrees(Vector2 start, Vector2 end)
        {
            var segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            PlacedActors.RemoveAll(record =>
            {
                if (!record.tree || !InActiveDistrict(record.position)) return false;
                float along = lengthSquared > 0
                    ? Mathf.Clamp01(Vector2.Dot(record.position - start, segment) / lengthSquared) : 0f;
                if ((record.position - (start + segment * along)).sqrMagnitude > ParkTreeRadius * ParkTreeRadius)
                    return false;
                if (placedTreeViews.Remove(record, out var tree) && tree)
                {
                    tree.SetActive(false);
                    Destroy(tree);
                }
                return true;
            });
        }

        private bool TryPlantParkTree(Vector2 point, WildlifeWaterAccess water)
        {
            if (!InActiveDistrict(point) || PickDistrictRoad(point) != null ||
                (ApprovedDistrict && ApprovedDistrict.Baseline &&
                    ApprovedDistrict.RoadDistance(point) <= ApprovedDistrict.Baseline.roadWidth) ||
                roadBuildings.Any(building => building.Contains(point, .4f)))
                return false;

            float height = LotHeight(point);
            if (TryLotWater(point, height, water, out _) ||
                (!Waterless && water == null && !IsTownLand(point / MapSide + Vector2.one * .5f)))
                return false;

            const float spacingSquared = ParkTreeSpacing * ParkTreeSpacing;
            foreach (var tree in lotTreePositions)
                if ((tree - point).sqrMagnitude < spacingSquared) return false;
            foreach (var actor in PlacedActors)
                if (actor.tree && (actor.position - point).sqrMagnitude < spacingSquared) return false;

            var asset = parkTreeAssets[parkTreeRandom.Next(parkTreeAssets.Length)];
            var record = new DistrictPlacedActor
            {
                assetId = asset.id,
                tree = true,
                position = point,
                district = DistrictAtWorld(new Vector3(point.x, 0f, point.y))
            };
            PresentPlacedTree(record, asset);
            PlacedActors.Add(record);
            return true;
        }
    }
}

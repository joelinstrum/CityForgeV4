using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.World;
using CityForge.CameraSystem;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.UI
{
    internal sealed class RegionResourcesOverlay : MonoBehaviour
    {
        const int Stone = 1, Iron = 2, Coal = 3;
        static readonly string[] Resources =
        {
            "timber", "stone", "iron", "coal", "fish", "fertile-soil", "pasture",
            "fresh-water", "natural-harbor", "scenic-beauty"
        };
        static readonly string[] ResourceNames =
        {
            "Timber", "Stone", "Iron", "Coal", "Fish", "Fertile Soil", "Pasture",
            "Fresh Water", "Natural Harbor", "Scenic Beauty"
        };
        static readonly string[] Categories = { "Agriculture", "Industry", "Commerce", "Population Growth", "Culture" };
        static readonly Color Ink = new Color(.94f, .90f, .76f);
        static readonly Color ButtonColor = new Color(.18f, .23f, .18f);
        static readonly Color ActiveColor = new Color(.29f, .40f, .28f);
        enum Mode { Overview, Resource, Suitability }

        PrototypeRegionView view;
        DistrictCombiner combiner;
        Camera mapCamera;
        RectTransform hud, panel, markerRoot;
        Button toggle, enter;
        Text guidance, summary;
        Dropdown modeSelect, choice;
        Font font;
        Mode mode;
        int resource, category, selected = -1, discoveryCount = -1;
        bool active, visible, markerLayoutDirty;
        Matrix4x4 markerProjection;
        Vector2Int markerScreen;
        Dictionary<string, float>[] districts;
        SettlementSuitability[] suitability;
        CityForgeCameraController cameraControls;
        RectTransform tooltip;
        Text tooltipText;
        int hovered = -1, markerZoom = -1;
        readonly List<Site> sites = new();
        readonly List<Image> icons = new();
        readonly List<(Site site, Vector2 position)> displayed = new();
        readonly Sprite[] sprites = new Sprite[Resources.Length];
        readonly Texture2D[] ownedIcons = new Texture2D[Resources.Length];
        readonly int[] perType = new int[Resources.Length];
        int[] perDistrict;
        readonly struct Site
        {
            public readonly Vector3 position;
            public readonly int type, district;
            public readonly float strength;
            public Site(Vector3 position, int type, int district, float strength)
            {
                this.position = position;
                this.type = type;
                this.district = district;
                this.strength = strength;
            }
        }
        static readonly float[] OverviewPriority = { .22f, .18f, .20f, .20f, .18f, .24f, .12f, .16f, .14f, -.18f };
        static readonly string[] Advantages =
        {
            "Supplies wood for early construction and a local lumber industry.",
            "Useful for construction, roads, bridges, and civic buildings.",
            "Supports metalworking, tools, and industrial development.",
            "Provides fuel for industry as coal becomes useful.",
            "Supports a fishing economy and supplies food for settlement growth.",
            "Supports productive farming and population growth.",
            "Provides grazing land for livestock and supports agriculture.",
            "Supports settlement growth with access to fresh water.",
            "Sheltered water can support a port and regional trade.",
            "An attractive landscape can support cultural development."
        };

        internal void Initialize(PrototypeRegionView source, Transform parent, Font uiFont)
        {
            view = source;
            hud = (RectTransform)parent;
            font = uiFont;
            combiner = view.GetComponent<DistrictCombiner>();
            mapCamera = view.GetComponentInChildren<Camera>();
            cameraControls = mapCamera.GetComponent<CityForgeCameraController>();
            markerRoot = Rect(hud, "Resource markers", 0, 0, 0, 0);
            markerRoot.anchorMin = markerRoot.anchorMax = markerRoot.pivot = new Vector2(.5f, .5f);
            markerRoot.anchoredPosition = Vector2.zero;

            toggle = Action(hud, "Resources", 0, 0, 205, 42, Toggle);
            Right((RectTransform)toggle.transform, -25, -304);
            panel = Rect(hud, "Resources panel", 0, 0, 360, 304);
            Right(panel, -25, -354);
            panel.gameObject.AddComponent<Image>().color = new Color(.08f, .12f, .10f, .97f);
            Label(panel, "REGIONAL RESOURCES", 16, -10, 328, 26, 19);
            modeSelect = Select(panel, "Resources mode", -42, new[] { "Overview", "Individual Resource", "Settlement Suitability" });
            modeSelect.onValueChanged.AddListener(value => { mode = (Mode)value; RefreshMode(); });
            choice = Select(panel, "Resources choice", -81, ResourceNames);
            choice.onValueChanged.AddListener(value =>
            {
                if (mode == Mode.Resource) resource = value;
                if (mode == Mode.Suitability) category = value;
                Refresh();
            });
            guidance = Label(panel, "", 16, -120, 328, 48);
            summary = Label(panel, "", 16, -166, 328, 99);
            enter = Action(panel, "Open District", 16, -260, 328, 32, () => view.RequestDistrict(selected));

            tooltip = Rect(hud, "Resource tooltip", 0, 0, 268, 112);
            tooltip.gameObject.AddComponent<Image>().color = new Color(.07f, .095f, .08f, .98f);
            tooltip.GetComponent<Image>().raycastTarget = false;
            tooltipText = Label(tooltip, "", 12, -8, 244, 96);
            tooltip.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);
        }

        internal bool TrySelect(Ray ray)
        {
            if (!visible || combiner.Active || !view.RegionalResources.HasSurvey ||
                !view.Relief.TryPick(ray, out int district)) return false;
            selected = district;
            RefreshSummary();
            return true;
        }

        void Toggle()
        {
            active = !active;
            SyncVisibility();
        }

        void LateUpdate()
        {
            SyncVisibility();
            if (!visible || !view.RegionalResources.HasSurvey) return;
            if (discoveryCount != view.RegionalResources.DiscoveryCount) Refresh();
            var projection = mapCamera.projectionMatrix * mapCamera.worldToCameraMatrix;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            int zoom = cameraControls.ZoomLevel;
            if (markerLayoutDirty || markerProjection != projection || markerScreen != screenSize || markerZoom != zoom)
            {
                markerLayoutDirty = false;
                markerProjection = projection;
                markerScreen = screenSize;
                markerZoom = zoom;
                LayoutIcons(zoom);
            }
            if (Application.isFocused) UpdateTooltip((Vector2)Input.mousePosition);
            else HideTooltip();
        }

        void LayoutIcons(int zoom)
        {
            displayed.Clear();
            Array.Clear(perDistrict, 0, perDistrict.Length);
            Array.Clear(perType, 0, perType.Length);
            int budget = zoom >= 5 ? 30 : zoom == 4 ? 44 : 64;
            int districtBudget = mode == Mode.Resource ? 1 : zoom >= 5 ? 1 : zoom == 4 ? 2 : 3;
            float spacing = zoom >= 5 ? 36 : zoom == 4 ? 31 : 27;

            if (mode == Mode.Overview)
            {
                // Give scarce materials room before widespread resources fill the map.
                Reserve(Iron, 1);
                Reserve(Coal, 1);
                Reserve(Stone, zoom >= 5 ? 3 : 5);
            }
            if (mode != Mode.Suitability)
                foreach (var site in sites)
                {
                    if (displayed.Count >= budget) break;
                    if (mode == Mode.Resource && site.type != resource) continue;
                    if (mode == Mode.Overview && (site.strength < .26f || perType[site.type] >= (site.type == 9 ? 2 : budget / 4))) continue;
                    PlaceSite(site);
                }
            for (int i = displayed.Count; i < icons.Count; i++) icons[i].gameObject.SetActive(false);
            HideTooltip();

            void Reserve(int type, int slots)
            {
                foreach (var site in sites)
                {
                    if (perType[type] >= slots) break;
                    if (site.type == type) PlaceSite(site);
                }
            }

            void PlaceSite(Site site)
            {
                if (perDistrict[site.district] >= districtBudget) return;
                var screen = mapCamera.WorldToScreenPoint(site.position);
                if (screen.z <= 0 || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) return;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(hud, screen, null, out var point)) return;
                if (displayed.Exists(item => (item.position - point).sqrMagnitude < spacing * spacing)) return;
                perDistrict[site.district]++;
                perType[site.type]++;
                var icon = IconAt(displayed.Count);
                icon.sprite = SpriteFor(site.type);
                icon.rectTransform.anchoredPosition = point;
                icon.gameObject.SetActive(true);
                displayed.Add((site, point));
            }
        }

        void UpdateTooltip(Vector2 cursor)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(panel, cursor) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(hud, cursor, null, out var point))
            {
                HideTooltip();
                return;
            }
            int index = displayed.FindIndex(item => Mathf.Abs(item.position.x - point.x) <= 12 &&
                Mathf.Abs(item.position.y - point.y) <= 12);
            if (index < 0)
            {
                HideTooltip();
                return;
            }
            if (hovered != index)
            {
                hovered = index;
                var site = displayed[index].site;
                bool quality = site.type is 5 or 8 or 9;
                tooltipText.text = $"<b>{ResourceNames[site.type]}</b>\n{(quality ? "Quality" : "Abundance")}: {Grade(site.strength)}\n{Advantages[site.type]}";
                tooltip.SetAsLastSibling();
                tooltip.gameObject.SetActive(true);
            }
            var bounds = hud.rect;
            float left = point.x + 27;
            if (left + tooltip.rect.width > bounds.xMax - 8) left = point.x - tooltip.rect.width - 27;
            float top = point.y - 18;
            if (top - tooltip.rect.height < bounds.yMin + 8) top = point.y + tooltip.rect.height + 18;
            tooltip.anchoredPosition = new Vector2(
                Mathf.Clamp(left, bounds.xMin + 8, bounds.xMax - tooltip.rect.width - 8) - bounds.xMin,
                Mathf.Clamp(top, bounds.yMin + tooltip.rect.height + 8, bounds.yMax - 8) - bounds.yMax);
        }

        static string Grade(float strength) => strength >= .80f ? "Exceptional" :
            strength >= .55f ? "High" : strength >= .30f ? "Moderate" : "Low";

        void HideTooltip()
        {
            hovered = -1;
            if (tooltip) tooltip.gameObject.SetActive(false);
        }

        void SyncVisibility()
        {
            bool overview = view.SelectedDistrict < 0 && !view.DistrictLoading;
            toggle.gameObject.SetActive(overview);
            bool next = active && overview && view.Relief;
            if (next == visible) return;
            visible = next;
            toggle.image.color = active ? ActiveColor : ButtonColor;
            panel.gameObject.SetActive(visible);
            markerRoot.gameObject.SetActive(visible);
            if (visible) Refresh();
            else HideTooltip();
        }

        void RefreshMode()
        {
            choice.ClearOptions();
            choice.AddOptions(new List<string>(mode == Mode.Suitability ? Categories : ResourceNames));
            choice.SetValueWithoutNotify(mode == Mode.Suitability ? category : resource);
            Refresh();
        }

        void Refresh()
        {
            if (!visible) return;
            bool available = view.RegionalResources.HasSurvey;
            modeSelect.gameObject.SetActive(available);
            choice.gameObject.SetActive(available && mode != Mode.Overview);
            if (!available)
            {
                guidance.text = "This region has no saved resource survey.";
                summary.text = "";
                enter.gameObject.SetActive(false);
                return;
            }

            EnsureCache();
            markerLayoutDirty = true;
            guidance.text = mode == Mode.Suitability
                ? "Select a district to compare scores at a dry site in its current year."
                : "Hover over a symbol to inspect the resource.";
            if (mode == Mode.Resource && (resource is Iron or Coal) && !sites.Exists(site => site.type == resource))
                guidance.text = $"No known {ResourceNames[resource].ToLowerInvariant()} deposits in this region.";
            RefreshSummary();
        }

        void EnsureCache()
        {
            var map = view.RegionalResources;
            if (discoveryCount == map.DiscoveryCount) return;
            discoveryCount = map.DiscoveryCount;
            int grid = view.GridSide, count = grid * grid;
            districts = new Dictionary<string, float>[count];
            suitability = new SettlementSuitability[count];
            for (int i = 0; i < count; i++) districts[i] = map.InDistrict(i, grid);
            perDistrict = new int[count];
            BuildSites(map, grid);
        }

        void BuildSites(ResourceMap map, int grid)
        {
            sites.Clear();
            for (int type = 0; type < Resources.Length; type++)
            {
                var best = new (Vector2 position, float strength)[grid * grid];
                foreach (var sample in map.SurveySites(Resources[type]))
                {
                    int district = DistrictAt(sample.position, grid);
                    if (sample.strength > best[district].strength) best[district] = sample;
                }
                for (int district = 0; district < best.Length; district++)
                {
                    var sample = best[district];
                    if (sample.strength >= .12f) AddSite(sample.position, type, district, sample.strength);
                }
            }
            foreach (var deposit in map.KnownDeposits())
            {
                int type = Array.IndexOf(Resources, deposit.type);
                if (type >= 0) AddSite(deposit.position, type, DistrictAt(deposit.position, grid), deposit.abundance);
            }
            sites.Sort((a, b) => (b.strength + OverviewPriority[b.type]).CompareTo(a.strength + OverviewPriority[a.type]));
        }

        static int DistrictAt(Vector2 position, int grid) =>
            Mathf.Min(grid - 1, (int)(position.y * grid)) * grid + Mathf.Min(grid - 1, (int)(position.x * grid));

        void AddSite(Vector2 position, int type, int district, float strength)
        {
            var world = (position - Vector2.one * .5f) * view.MapSide;
            var ray = new Ray(new Vector3(world.x, 20000, world.y), Vector3.down);
            if (view.Relief.TryPick(ray, out _, out var point))
                sites.Add(new Site(point, type, district, strength));
        }

        SettlementSuitability Suitability(int index)
        {
            if (suitability[index] != null) return suitability[index];
            var bounds = RegionBiomeMap.DistrictBounds(index, view.GridSide);
            Vector2 best = default;
            float distance = float.PositiveInfinity;
            for (int z = 0; z < 5; z++)
            for (int x = 0; x < 5; x++)
            {
                var sample = bounds.min + new Vector2(x + .5f, z + .5f) * (bounds.width / 5);
                float offset = (sample - bounds.center).sqrMagnitude;
                if (offset >= distance || !view.IsTownLand(sample)) continue;
                best = sample;
                distance = offset;
            }
            if (!float.IsPositiveInfinity(distance))
                return suitability[index] = view.EvaluateSettlement((best - Vector2.one * .5f) * view.MapSide, view.TownYear(index));

            var unavailable = new SettlementSuitability();
            unavailable.limitations.Add("No dry site in the district samples");
            return suitability[index] = unavailable;
        }

        void RefreshSummary()
        {
            enter.gameObject.SetActive(selected >= 0);
            if (selected < 0)
            {
                summary.text = "Select a district to inspect its resources.";
                return;
            }
            var members = view.DistrictMembers(selected);
            string name = members.Length > 1 ? $"Combined district ({members.Length})" :
                $"District {selected % view.GridSide + 1}–{selected / view.GridSide + 1}";
            if (mode == Mode.Resource)
            {
                float peak = members.Max(i => districts[i].TryGetValue(Resources[resource], out float strength) ? strength : 0);
                string value = peak > 0 ? $"{peak:P0} survey strength" : "None recorded";
                summary.text = $"{name}\n{ResourceNames[resource]} · {value}";
            }
            else if (mode == Mode.Suitability)
            {
                float score = members.Max(i => Suitability(i).scores[category]);
                string geography = string.Join(" · ", members.SelectMany(i =>
                    Suitability(i).geography.Concat(Suitability(i).limitations)).Distinct());
                summary.text = $"{name}\n{Categories[category]} · {score:F0}/100\n{geography}";
            }
            else
            {
                var best = members.SelectMany(i => districts[i])
                    .GroupBy(pair => pair.Key).OrderByDescending(group => group.Max(pair => pair.Value))
                    .Take(4).Select(group => $"{NameOf(group.Key)} · {group.Max(pair => pair.Value):P0}");
                summary.text = name + "\n" + string.Join("\n", best.DefaultIfEmpty("No resources recorded"));
            }
        }

        static string NameOf(string id)
        {
            int index = Array.IndexOf(Resources, id);
            return index < 0 ? id : ResourceNames[index];
        }

        Image IconAt(int index)
        {
            if (index < icons.Count) return icons[index];
            var icon = Rect(markerRoot, "Resource icon", 0, 0, 24, 24);
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(.5f, .5f);
            var image = icon.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            icons.Add(image);
            return image;
        }

        Sprite SpriteFor(int type)
        {
            if (sprites[type]) return sprites[type];
            sprites[type] = UnityEngine.Resources.Load<Sprite>("Region/ResourceIcons/" + Resources[type]);
            if (sprites[type]) return sprites[type];
            var texture = ownedIcons[type] = RegionalResourceIcons.Draw(type);
            return sprites[type] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
        }

        Dropdown Select(Transform parent, string name, int y, string[] options)
        {
            var go = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, 16, y, 328, 32);
            foreach (var text in go.GetComponentsInChildren<Text>(true)) text.font = font;
            var dropdown = go.GetComponent<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            return dropdown;
        }

        Button Action(Transform parent, string name, int x, int y, int width, int height, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(parent, name, x, y, width, height);
            var background = rect.gameObject.AddComponent<Image>();
            background.color = ButtonColor;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(action);
            Label(rect, name, 0, 0, width, height, 17).alignment = TextAnchor.MiddleCenter;
            return button;
        }

        Text Label(Transform parent, string value, int x, int y, int width, int height, int size = 16)
        {
            var text = Rect(parent, "Label", x, y, width, height).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Ink;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        static RectTransform Rect(Transform parent, string name, int x, int y, int width, int height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            Place(rect, x, y, width, height);
            return rect;
        }

        static void Place(RectTransform rect, int x, int y, int width, int height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static void Right(RectTransform rect, int x, int y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(x, y);
        }

        void OnDisable()
        {
            HideTooltip();
            visible = false;
            if (panel) panel.gameObject.SetActive(false);
            if (markerRoot) markerRoot.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            for (int i = 0; i < ownedIcons.Length; i++)
            {
                if (!ownedIcons[i]) continue;
                Destroy(sprites[i]);
                Destroy(ownedIcons[i]);
            }
        }
    }
}

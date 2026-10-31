using System.IO;
using CityForge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CityForge.Editor
{
    public static class ModalPrefabBuilder
    {
        private const string Root = "Assets/CityForge";
        private const string Art = Root + "/Art/UI/Modal";
        private const string BackgroundPath = Art + "/T_Modal_Background.png";
        private const string RoundedPath = Art + "/T_Rounded_10px.png";
        private const string NavigationButtonsPath = Art + "/T_Region_Nav_Buttons.png";
        private const string NavigationIconsPath = Art + "/T_Region_Nav_Icons.png";
        private const string ArrowsPath = Art + "/T_Region_Arrows.png";
        private const string PrefabPath = Root + "/Prefabs/PF_Modal_75.prefab";
        private const string ScenePath = Root + "/Scenes/Gameplay/MainMenu.unity";
        private const float HeaderHeight = 96f;
        private const float FooterHeight = 54f;
        private static readonly Color Gold = new Color(0.76f, 0.60f, 0.36f);
        private static readonly Color Ivory = new Color(0.98f, 0.91f, 0.75f);

        [MenuItem("CityForge/Install 75% Modal in Main Menu")]
        public static void InstallInMainMenu()
        {
            if (EditorApplication.isPlaying)
                throw new System.InvalidOperationException("Stop Play mode before installing the modal.");

            AssetDatabase.Refresh();
            BuildPrefab();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            InstallIntoScene(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("CityForge: reusable 75% modal installed in MainMenu.");
        }

        [MenuItem("CityForge/Preview Create Region Modal")]
        public static void PreviewCreateRegion()
        {
            var controller = Object.FindFirstObjectByType<MainMenuController>();
            if (controller == null)
                throw new System.InvalidOperationException("Open the MainMenu scene to preview its modal.");
            if (EditorApplication.isPlaying)
            {
                var button = GameObject.Find("CreateRegionButton").GetComponent<Button>();
                button.onClick.Invoke();
            }
            else
            {
                controller.Handle(MenuAction.CreateRegion);
            }
        }

        [MenuItem("CityForge/Preview Terrain Settings")]
        public static void PreviewTerrain()
        {
            PreviewCreateRegion();
            var modal = Object.FindFirstObjectByType<CityForgeModal>(FindObjectsInactive.Include);
            if (modal == null)
                throw new System.InvalidOperationException("Create Region modal is missing.");
            modal.SetRegionName("Great River Valley");
            modal.SelectCreateRegionSection(1);
        }

        public static void BuildPrefab()
        {
            Directory.CreateDirectory(Art);
            if (!File.Exists(BackgroundPath))
                throw new System.IO.FileNotFoundException("Modal background is missing", BackgroundPath);

            EnsureRoundedSprite();
            ImportBackground();
            ImportNavigationTexture(NavigationButtonsPath);
            ImportNavigationTexture(NavigationIconsPath);
            ImportNavigationTexture(ArrowsPath);
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            var rounded = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPath);
            var navigationButtonsTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(NavigationButtonsPath);
            var navigationIconsTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(NavigationIconsPath);
            var arrowsTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ArrowsPath);
            var icons = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/UI/Menu/Menu_IconSheet.png");
            var font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/Cinzel-Regular.ttf");
            if (background == null || rounded == null || icons == null || font == null ||
                navigationButtonsTexture == null || navigationIconsTexture == null || arrowsTexture == null)
                throw new System.InvalidOperationException("Modal art, menu icons, or font failed to import.");

            var root = NewUI("Modal75Overlay", null);
            Stretch(root.GetComponent<RectTransform>());
            var modal = root.AddComponent<CityForgeModal>();

            var backdrop = NewUI("ClickAwayBackdrop", root.transform);
            Stretch(backdrop.GetComponent<RectTransform>());
            var backdropImage = backdrop.AddComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.72f);
            var backdropButton = backdrop.AddComponent<Button>();
            backdropButton.targetGraphic = backdropImage;
            backdropButton.transition = Selectable.Transition.None;

            var panel = NewUI("Panel75", root.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.125f, 0.125f);
            panelRect.anchorMax = new Vector2(0.875f, 0.875f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var border = panel.AddComponent<Image>();
            border.sprite = rounded;
            border.type = Image.Type.Sliced;
            border.color = Gold;

            var clipped = NewUI("RoundedContentClip", panel.transform);
            var clippedRect = clipped.GetComponent<RectTransform>();
            Stretch(clippedRect);
            clippedRect.offsetMin = new Vector2(2f, 2f);
            clippedRect.offsetMax = new Vector2(-2f, -2f);
            var clipImage = clipped.AddComponent<Image>();
            clipImage.sprite = rounded;
            clipImage.type = Image.Type.Sliced;
            clipped.AddComponent<Mask>().showMaskGraphic = false;

            var paper = NewUI("ModalBackground", clipped.transform);
            Stretch(paper.GetComponent<RectTransform>());
            var paperImage = paper.AddComponent<RawImage>();
            paperImage.texture = background;
            paperImage.raycastTarget = false;

            var header = NewUI("Header", clipped.transform);
            TopStretch(header.GetComponent<RectTransform>(), HeaderHeight);
            var hero = NewUI("HeroIcon", header.transform);
            TopLeft(hero.GetComponent<RectTransform>(), 35f, 16f, 64f, 64f);
            var heroImage = hero.AddComponent<RawImage>();
            heroImage.texture = icons;
            heroImage.raycastTarget = false;
            var headerLabel = Label(header.transform, "HeaderLabel", font, 36, Ivory,
                TextAnchor.MiddleLeft);
            var titleRect = headerLabel.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(116f, 0f);
            titleRect.offsetMax = new Vector2(-88f, 0f);

            var close = NewUI("CloseX", header.transform);
            var closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-20f, -18f);
            closeRect.sizeDelta = new Vector2(54f, 54f);
            var closeImage = close.AddComponent<Image>();
            closeImage.color = new Color(0f, 0f, 0f, 0.24f);
            var closeButton = close.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            var xLabel = Label(close.transform, "XLabel", font, 33, Ivory,
                TextAnchor.MiddleCenter);
            xLabel.text = "X";
            Stretch(xLabel.GetComponent<RectTransform>());

            var headerRule = NewUI("HeaderRule", clipped.transform);
            TopStretch(headerRule.GetComponent<RectTransform>(), 2f);
            headerRule.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -HeaderHeight);
            headerRule.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, 0.5f);

            var bodyArea = NewUI("ScrollViewport", clipped.transform);
            var viewport = bodyArea.GetComponent<RectTransform>();
            Stretch(viewport);
            viewport.offsetMin = new Vector2(0f, FooterHeight);
            viewport.offsetMax = new Vector2(0f, -(HeaderHeight + 2f));
            var hitArea = bodyArea.AddComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0f);
            bodyArea.AddComponent<RectMask2D>();
            var scroll = bodyArea.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var content = NewUI("ScrollContent", bodyArea.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(38, 38, 28, 28);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var bodyLabel = Label(content.transform, "BodyLabel", font, 24, Ivory,
                TextAnchor.UpperLeft);
            bodyLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyLabel.verticalOverflow = VerticalWrapMode.Overflow;
            scroll.content = contentRect;

            var basicSettings = NewUI("CreateRegionBasicSettings", content.transform);
            basicSettings.AddComponent<LayoutElement>().preferredHeight = 456f;
            var sectionTitle = Label(basicSettings.transform, "SectionTitle", font, 25, Ivory,
                TextAnchor.MiddleLeft);
            sectionTitle.text = "BASIC SETTINGS";
            TopLeft(sectionTitle.GetComponent<RectTransform>(), 0f, 0f, 510f, 38f);
            var sectionDescription = Label(basicSettings.transform, "SectionDescription", font, 14, Ivory,
                TextAnchor.MiddleLeft);
            sectionDescription.text = "Define your region and its founding period.";
            TopLeft(sectionDescription.GetComponent<RectTransform>(), 0f, 38f, 530f, 28f);

            var regionSize = CreateTerrainDropdown(basicSettings.transform, "Region Size", font, rounded, 550f, 90f,
                new[] { "Small — 1,024 m", "Medium — 2,048 m", "Large — 4,096 m" });
            regionSize.value = 1;
            var nameInput = CreateNameInput(basicSettings.transform, font, rounded);
            CreateSettingLabel(basicSettings.transform, "RegionNameLabel", "REGION NAME", font, 90f);
            CreateSettingLabel(basicSettings.transform, "EraLabel", "ERA", font, 158f);
            CreateSettingLabel(basicSettings.transform, "PeriodLabel", "PERIOD", font, 238f);
            var eraValue = CreateOptionSelector(basicSettings.transform, "EraSelector", font, rounded,
                arrowsTexture,
                158f, out var eraPrevious, out var eraNext);
            var periodValue = CreateOptionSelector(basicSettings.transform, "PeriodSelector", font, rounded,
                arrowsTexture,
                238f, out var periodPrevious, out var periodNext);
            periodValue.resizeTextForBestFit = true;
            periodValue.resizeTextMinSize = 13;
            periodValue.resizeTextMaxSize = 17;
            var periodYears = Label(basicSettings.transform, "PeriodYears", font, 14, Gold,
                TextAnchor.MiddleLeft);
            TopLeft(periodYears.GetComponent<RectTransform>(), 170f, 290f, 340f, 28f);
            var nextButton = CreateNextButton(basicSettings.transform, font, rounded, arrowsTexture);
            basicSettings.SetActive(false);

            var terrainIntro = NewUI("CreateRegionTerrainIntro", content.transform);
            terrainIntro.AddComponent<LayoutElement>().preferredHeight = 456f;
            var terrainTitle = Label(terrainIntro.transform, "SectionTitle", font, 25, Ivory,
                TextAnchor.MiddleLeft);
            terrainTitle.text = "TERRAIN";
            TopLeft(terrainTitle.GetComponent<RectTransform>(), 0f, 0f, 140f, 38f);
            var terrainDescription = Label(terrainIntro.transform, "SectionDescription", font, 14,
                Ivory, TextAnchor.MiddleLeft);
            terrainDescription.text = "Shape the land, water, and climate of your region.";
            TopLeft(terrainDescription.GetComponent<RectTransform>(), 150f, 0f, 620f, 38f);
            CreateTerrainDropdown(terrainIntro.transform, "Climate", font, rounded, 0f, 46f,
                new[] { "Temperate — balanced", "Temperate — dry", "Temperate — wet", "Tropical" });
            var climateNote = Label(terrainIntro.transform, "ClimateNote", font, 12, Gold,
                TextAnchor.UpperLeft);
            climateNote.text = "This is the default climate, but you can change this for each region.";
            TopLeft(climateNote.GetComponent<RectTransform>(), 0f, 131f, 350f, 42f);
            CreateTerrainDropdown(terrainIntro.transform, "Topography", font, rounded, 0f, 195f,
                new[] { "Flat", "Hills and Valleys", "Scattered Hills",
                    "A Few Hills and Mountains", "Mountain Range" });
            var mountainNote = Label(terrainIntro.transform, "MountainNote", font, 12, Gold,
                TextAnchor.UpperLeft);
            mountainNote.text = "Mountain Range is limited to one side of the map.";
            TopLeft(mountainNote.GetComponent<RectTransform>(), 0f, 280f, 350f, 35f);
            CreateTerrainDropdown(terrainIntro.transform, "Rivers", font, rounded, 425f, 46f,
                new[] { "None", "One Major River", "Two Major Rivers", "Three Major Rivers" });
            CreateTerrainDropdown(terrainIntro.transform, "Streams", font, rounded, 425f, 195f,
                new[] { "None", "A Few Streams", "Many Streams" });
            CreateTerrainDropdown(terrainIntro.transform, "Land Type", font, rounded, 0f, 315f,
                new[] { "Pangea", "Islands", "Continent", "Coastal", "Archipelago", "Peninsula" });
            var coastalNote = Label(terrainIntro.transform, "CoastalNote", font, 12, Gold,
                TextAnchor.UpperLeft);
            coastalNote.text = "Coastal reserves about one quarter of the map for coastal water.";
            TopLeft(coastalNote.GetComponent<RectTransform>(), 0f, 400f, 350f, 40f);
            var terrainNext = CreateNextButton(terrainIntro.transform, font, rounded, arrowsTexture);
            terrainIntro.SetActive(false);

            var navigation = NewUI("CreateRegionNavigation", clipped.transform);
            var navigationRect = navigation.GetComponent<RectTransform>();
            navigationRect.anchorMin = new Vector2(0f, 0f);
            navigationRect.anchorMax = new Vector2(0f, 1f);
            navigationRect.pivot = new Vector2(0f, 0.5f);
            navigationRect.anchoredPosition = new Vector2(0f, (FooterHeight - HeaderHeight - 2f) * 0.5f);
            navigationRect.sizeDelta = new Vector2(270f, -(HeaderHeight + 2f + FooterHeight));
            navigation.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.24f);
            var navigationLayout = navigation.AddComponent<VerticalLayoutGroup>();
            navigationLayout.padding = new RectOffset(10, 10, 9, 9);
            navigationLayout.spacing = 3f;
            navigationLayout.childAlignment = TextAnchor.UpperCenter;
            navigationLayout.childControlWidth = true;
            navigationLayout.childControlHeight = true;
            navigationLayout.childForceExpandWidth = true;
            navigationLayout.childForceExpandHeight = true;
            var divider = NewUI("NavigationDivider", navigation.transform);
            // The divider is positioned after the layout has placed the buttons below.
            divider.transform.SetAsFirstSibling();
            var dividerLayout = divider.AddComponent<LayoutElement>();
            dividerLayout.ignoreLayout = true;
            var dividerRect = divider.GetComponent<RectTransform>();
            dividerRect.anchorMin = new Vector2(1f, 0f);
            dividerRect.anchorMax = new Vector2(1f, 1f);
            dividerRect.pivot = new Vector2(1f, 0.5f);
            dividerRect.anchoredPosition = Vector2.zero;
            dividerRect.sizeDelta = new Vector2(1f, 0f);
            divider.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, 0.5f);

            var navNames = new[] { "BASIC SETTINGS", "ENVIRONMENT", "TERRAIN",
                "STARTING TOWN", "DIFFICULTY", "ADVANCED" };
            var navDescriptions = new[] { "Name and period", "Climate and ecology",
                "Region shape", "Initial settlement", "Economic conditions",
                "Custom options" };
            var navIconCrops = new[]
            {
                new Rect(85f, 45f, 345f, 310f),
                new Rect(450f, 395f, 430f, 275f),
                new Rect(85f, 385f, 300f, 305f),
                new Rect(930f, 45f, 340f, 310f),
                new Rect(1755f, 45f, 355f, 310f),
                new Rect(1765f, 380f, 355f, 310f)
            };
            var navBackgrounds = new RawImage[navNames.Length];
            var navButtons = new Button[navNames.Length];
            for (var index = 0; index < navNames.Length; index++)
            {
                var item = NewUI("Nav" + index + navNames[index].Replace(" ", ""), navigation.transform);
                var itemLayout = item.AddComponent<LayoutElement>();
                itemLayout.minHeight = 62f;
                itemLayout.flexibleHeight = 1f;
                var itemBackground = item.AddComponent<RawImage>();
                itemBackground.texture = navigationButtonsTexture;
                itemBackground.uvRect = PixelUv(0f, 0f, 1070f, 192f, 1070f, 635f);
                var itemButton = item.AddComponent<Button>();
                itemButton.targetGraphic = itemBackground;
                itemButton.transition = Selectable.Transition.ColorTint;
                item.AddComponent<CanvasGroup>();
                navBackgrounds[index] = itemBackground;
                navButtons[index] = itemButton;

                var icon = NewUI("Icon", item.transform);
                TopLeft(icon.GetComponent<RectTransform>(), 14f, 12f, 48f, 48f);
                var iconImage = icon.AddComponent<RawImage>();
                iconImage.texture = navigationIconsTexture;
                var crop = navIconCrops[index];
                iconImage.uvRect = PixelUv(crop.x, crop.y, crop.width, crop.height, 2172f, 724f);
                iconImage.raycastTarget = false;

                var title = Label(item.transform, "Title", font, 17, Ivory, TextAnchor.LowerLeft);
                var navTitleRect = title.GetComponent<RectTransform>();
                navTitleRect.anchorMin = new Vector2(0f, 0.5f);
                navTitleRect.anchorMax = new Vector2(1f, 1f);
                navTitleRect.offsetMin = new Vector2(72f, -2f);
                navTitleRect.offsetMax = new Vector2(-8f, -7f);
                title.text = navNames[index];
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = 13;
                title.resizeTextMaxSize = 17;
                var description = Label(item.transform, "Description", font, 11, Ivory,
                    TextAnchor.UpperLeft);
                var descriptionRect = description.GetComponent<RectTransform>();
                descriptionRect.anchorMin = new Vector2(0f, 0f);
                descriptionRect.anchorMax = new Vector2(1f, 0.5f);
                descriptionRect.offsetMin = new Vector2(72f, 3f);
                descriptionRect.offsetMax = new Vector2(-8f, 0f);
                description.text = navDescriptions[index];
            }
            navigation.SetActive(false);

            var footer = NewUI("Footer", clipped.transform);
            BottomStretch(footer.GetComponent<RectTransform>(), FooterHeight);
            var footerRule = NewUI("FooterRule", footer.transform);
            TopStretch(footerRule.GetComponent<RectTransform>(), 2f);
            footerRule.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, 0.5f);
            var footerLabel = Label(footer.transform, "FooterLabel", font, 17, Gold,
                TextAnchor.MiddleLeft);
            var footerRect = footerLabel.GetComponent<RectTransform>();
            Stretch(footerRect);
            footerRect.offsetMin = new Vector2(38f, 0f);
            footerRect.offsetMax = new Vector2(-38f, 0f);

            modal.Configure(backdropButton, closeButton, heroImage, headerLabel, bodyLabel,
                footerLabel, scroll);
            modal.ConfigureCreateRegionNavigation(navigation, navBackgrounds, navButtons,
                navigationButtonsTexture);
            modal.ConfigureCreateRegionSettings(basicSettings, nameInput, eraValue,
                eraPrevious, eraNext, periodValue, periodPrevious, periodNext, periodYears,
                nextButton, terrainIntro, terrainNext, regionSize);
            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        public static void InstallIntoScene(Scene scene)
        {
            var canvas = GameObject.Find("MainMenuCanvas");
            var controller = Object.FindFirstObjectByType<MainMenuController>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var icons = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/UI/Menu/Menu_IconSheet.png");
            if (canvas == null || controller == null || prefab == null || icons == null)
                throw new System.InvalidOperationException("MainMenu or modal prefab is incomplete.");

            for (var i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                var child = canvas.transform.GetChild(i);
                if (child.GetComponent<CityForgeModal>() != null)
                    Object.DestroyImmediate(child.gameObject);
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "Modal75Overlay";
            instance.transform.SetParent(canvas.transform, false);
            instance.transform.SetAsLastSibling();
            var modal = instance.GetComponent<CityForgeModal>();
            modal.Close();
            controller.ConfigureLargeModal(modal, icons);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void EnsureRoundedSprite()
        {
            if (!File.Exists(RoundedPath))
            {
                const int size = 32;
                const float radius = 10f;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var px = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    var py = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                        new Vector2(px, py));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f,
                        Mathf.Clamp01(radius + 0.5f - distance)));
                }
                texture.Apply();
                File.WriteAllBytes(RoundedPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(RoundedPath);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(RoundedPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(10f, 10f, 10f, 10f);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ImportBackground()
        {
            AssetDatabase.ImportAsset(BackgroundPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(BackgroundPath);
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static void ImportNavigationTexture(string path)
        {
            if (!File.Exists(path))
                throw new System.IO.FileNotFoundException("Region navigation art is missing", path);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        private static Rect PixelUv(float x, float y, float width, float height,
            float textureWidth, float textureHeight)
        {
            return new Rect(x / textureWidth, 1f - (y + height) / textureHeight,
                width / textureWidth, height / textureHeight);
        }

        private static void CreateSettingLabel(Transform parent, string name, string caption,
            Font font, float y)
        {
            var label = Label(parent, name, font, 16, Ivory, TextAnchor.MiddleLeft);
            label.text = caption;
            TopLeft(label.GetComponent<RectTransform>(), 0f, y, 155f, 48f);
        }

        private static Dropdown CreateTerrainDropdown(Transform parent, string name, Font font,
            Sprite rounded, float x, float y, string[] choices)
        {
            var label = Label(parent, name + "Label", font, 16, Ivory, TextAnchor.MiddleLeft);
            label.text = name.ToUpperInvariant();
            TopLeft(label.GetComponent<RectTransform>(), x, y, 350f, 30f);

            var control = NewUI(name + "Dropdown", parent);
            TopLeft(control.GetComponent<RectTransform>(), x, y + 34f, 350f, 48f);
            var frame = control.AddComponent<Image>();
            frame.sprite = rounded;
            frame.type = Image.Type.Sliced;
            frame.color = new Color(0.055f, 0.045f, 0.03f, 1f);
            var outline = control.AddComponent<Outline>();
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.75f);
            outline.effectDistance = new Vector2(1f, -1f);
            var selected = Label(control.transform, "Selected", font, 16, Ivory,
                TextAnchor.MiddleLeft);
            selected.raycastTarget = false;
            Stretch(selected.GetComponent<RectTransform>());
            selected.GetComponent<RectTransform>().offsetMin = new Vector2(13f, 0f);
            selected.GetComponent<RectTransform>().offsetMax = new Vector2(-43f, 0f);
            var arrow = Label(control.transform, "Arrow", font, 20, Gold,
                TextAnchor.MiddleCenter);
            arrow.text = "V";
            arrow.raycastTarget = false;
            var arrowRect = arrow.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = new Vector2(1f, 1f);
            arrowRect.offsetMin = new Vector2(-39f, 0f);
            arrowRect.offsetMax = new Vector2(-7f, 0f);

            var template = NewUI("Template", control.transform);
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -4f);
            templateRect.sizeDelta = new Vector2(0f, 230f);
            var templateImage = template.AddComponent<Image>();
            templateImage.sprite = rounded;
            templateImage.type = Image.Type.Sliced;
            templateImage.color = new Color(0.09f, 0.075f, 0.055f, 1f);
            var templateScroll = template.AddComponent<ScrollRect>();
            templateScroll.horizontal = false;
            templateScroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = NewUI("Viewport", template.transform);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            templateScroll.viewport = viewport.GetComponent<RectTransform>();

            var content = NewUI("Content", viewport.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 38f);
            templateScroll.content = contentRect;

            var item = NewUI("Item", content.transform);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 38f);
            var itemImage = item.AddComponent<Image>();
            itemImage.color = new Color(0.11f, 0.09f, 0.055f, 1f);
            var toggle = item.AddComponent<Toggle>();
            toggle.targetGraphic = itemImage;
            var itemText = Label(item.transform, "Item Label", font, 15, Ivory,
                TextAnchor.MiddleLeft);
            Stretch(itemText.GetComponent<RectTransform>());
            itemText.GetComponent<RectTransform>().offsetMin = new Vector2(13f, 0f);
            itemText.GetComponent<RectTransform>().offsetMax = new Vector2(-12f, 0f);
            itemText.raycastTarget = false;

            var dropdown = control.AddComponent<Dropdown>();
            dropdown.targetGraphic = frame;
            dropdown.template = templateRect;
            dropdown.captionText = selected;
            dropdown.itemText = itemText;
            dropdown.options.Clear();
            foreach (var choice in choices) dropdown.options.Add(new Dropdown.OptionData(choice));
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            template.SetActive(false);
            return dropdown;
        }

        private static InputField CreateNameInput(Transform parent, Font font, Sprite rounded)
        {
            var frame = NewUI("RegionNameInput", parent);
            TopLeft(frame.GetComponent<RectTransform>(), 170f, 90f, 340f, 48f);
            var border = frame.AddComponent<Image>();
            border.sprite = rounded;
            border.type = Image.Type.Sliced;
            border.color = new Color(Gold.r, Gold.g, Gold.b, 0.8f);
            var inside = NewUI("Inside", frame.transform);
            Stretch(inside.GetComponent<RectTransform>());
            inside.GetComponent<RectTransform>().offsetMin = new Vector2(1f, 1f);
            inside.GetComponent<RectTransform>().offsetMax = new Vector2(-1f, -1f);
            var insideImage = inside.AddComponent<Image>();
            insideImage.sprite = rounded;
            insideImage.type = Image.Type.Sliced;
            insideImage.color = new Color(0.035f, 0.03f, 0.025f, 0.94f);
            insideImage.raycastTarget = false;
            var value = Label(frame.transform, "Value", font, 17, Ivory, TextAnchor.MiddleLeft);
            Stretch(value.GetComponent<RectTransform>());
            value.GetComponent<RectTransform>().offsetMin = new Vector2(13f, 2f);
            value.GetComponent<RectTransform>().offsetMax = new Vector2(-13f, -2f);
            value.supportRichText = false;
            value.raycastTarget = false;
            var hint = Label(frame.transform, "Placeholder", font, 16,
                new Color(Ivory.r, Ivory.g, Ivory.b, 0.5f), TextAnchor.MiddleLeft);
            hint.text = "Enter region name";
            hint.raycastTarget = false;
            Stretch(hint.GetComponent<RectTransform>());
            hint.GetComponent<RectTransform>().offsetMin = new Vector2(13f, 2f);
            hint.GetComponent<RectTransform>().offsetMax = new Vector2(-13f, -2f);
            var input = frame.AddComponent<InputField>();
            input.targetGraphic = border;
            input.textComponent = value;
            input.placeholder = hint;
            input.characterLimit = 64;
            input.lineType = InputField.LineType.SingleLine;
            input.customCaretColor = true;
            input.caretColor = Ivory;
            input.caretWidth = 2;
            input.caretBlinkRate = 0.65f;
            return input;
        }

        private static Text CreateOptionSelector(Transform parent, string name, Font font,
            Sprite rounded, Texture2D arrows, float y, out Button previous, out Button next)
        {
            var frame = NewUI(name, parent);
            TopLeft(frame.GetComponent<RectTransform>(), 170f, y, 340f, 48f);
            var border = frame.AddComponent<Image>();
            border.sprite = rounded;
            border.type = Image.Type.Sliced;
            border.color = new Color(Gold.r, Gold.g, Gold.b, 0.8f);
            var inside = NewUI("Inside", frame.transform);
            Stretch(inside.GetComponent<RectTransform>());
            inside.GetComponent<RectTransform>().offsetMin = new Vector2(1f, 1f);
            inside.GetComponent<RectTransform>().offsetMax = new Vector2(-1f, -1f);
            var fill = inside.AddComponent<Image>();
            fill.sprite = rounded;
            fill.type = Image.Type.Sliced;
            fill.color = new Color(0.035f, 0.03f, 0.025f, 0.94f);
            var value = Label(frame.transform, "SelectedValue", font, 17, Ivory,
                TextAnchor.MiddleCenter);
            Stretch(value.GetComponent<RectTransform>());
            value.GetComponent<RectTransform>().offsetMin = new Vector2(46f, 0f);
            value.GetComponent<RectTransform>().offsetMax = new Vector2(-46f, 0f);
            previous = CreateSelectorArrow(frame.transform, "Previous", arrows, true);
            next = CreateSelectorArrow(frame.transform, "Next", arrows, false);
            return value;
        }

        private static Button CreateSelectorArrow(Transform parent, string name,
            Texture2D arrows, bool left)
        {
            var buttonObject = NewUI(name, parent);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(left ? 0f : 1f, 0.5f);
            rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(45f, 48f);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.01f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            var arrow = NewUI("Arrow", buttonObject.transform);
            var arrowRect = arrow.GetComponent<RectTransform>();
            arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(25f, 32f);
            var arrowImage = arrow.AddComponent<RawImage>();
            arrowImage.texture = arrows;
            arrowImage.uvRect = PixelUv(left ? 370f : 1335f, 50f, 470f, 605f, 2172f, 724f);
            arrowImage.raycastTarget = false;
            return button;
        }

        private static Button CreateNextButton(Transform parent, Font font, Sprite rounded,
            Texture2D arrows)
        {
            var frame = NewUI("NextButton", parent);
            // All setup panes share the same button position and dimensions.
            TopLeft(frame.GetComponent<RectTransform>(), 615f, 392f, 160f, 52f);
            var border = frame.AddComponent<Image>();
            border.sprite = rounded;
            border.type = Image.Type.Sliced;
            border.color = Gold;
            var button = frame.AddComponent<Button>();
            button.targetGraphic = border;
            frame.AddComponent<CanvasGroup>();
            var fill = NewUI("Fill", frame.transform);
            Stretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<RectTransform>().offsetMin = new Vector2(1f, 1f);
            fill.GetComponent<RectTransform>().offsetMax = new Vector2(-1f, -1f);
            var fillImage = fill.AddComponent<Image>();
            fillImage.sprite = rounded;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = new Color(0.16f, 0.11f, 0.045f, 0.94f);
            fillImage.raycastTarget = false;
            var caption = Label(frame.transform, "Caption", font, 19, Ivory,
                TextAnchor.MiddleCenter);
            caption.text = "NEXT";
            Stretch(caption.GetComponent<RectTransform>());
            caption.GetComponent<RectTransform>().offsetMax = new Vector2(-20f, 0f);
            var arrow = NewUI("Arrow", frame.transform);
            var arrowRect = arrow.GetComponent<RectTransform>();
            arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-13f, 0f);
            arrowRect.sizeDelta = new Vector2(20f, 26f);
            var arrowImage = arrow.AddComponent<RawImage>();
            arrowImage.texture = arrows;
            arrowImage.uvRect = PixelUv(1335f, 50f, 470f, 605f, 2172f, 724f);
            arrowImage.raycastTarget = false;
            return button;
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void TopStretch(RectTransform rect, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }

        private static void BottomStretch(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }

        private static void TopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static Text Label(Transform parent, string name, Font font, int size, Color color,
            TextAnchor alignment)
        {
            var go = NewUI(name, parent);
            var label = go.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }
    }
}

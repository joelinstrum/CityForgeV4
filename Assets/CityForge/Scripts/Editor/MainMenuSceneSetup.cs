using System.IO;
using CityForge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityForge.Editor
{
    public static class MainMenuSceneSetup
    {
        private const string Root = "Assets/CityForge";
        private const string Art = Root + "/Art/UI/Menu";
        private const string ScenePath = Root + "/Scenes/Gameplay/MainMenu.unity";
        private static readonly Color PanelColor = new Color(0.055f, 0.047f, 0.034f, 0.72f);
        private static readonly Color Gold = new Color(0.75f, 0.59f, 0.34f, 0.9f);
        private static readonly Color Ivory = new Color(0.98f, 0.91f, 0.75f, 1f);

        [MenuItem("CityForge/Create Opening Menu")]
        public static void Create()
        {
            AssetDatabase.Refresh();
            ImportTexture(Art + "/Menu_Background.png", false);
            ImportTexture(Art + "/Menu_Logo.png", true);
            ImportTexture(Art + "/Menu_IconSheet.png", true);
            ImportTexture(Art + "/Menu_UtilityIcons.png", true);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.Log("CityForge: MainMenu already exists; existing scene was preserved.");
                return;
            }

            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Menu_Background.png");
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Menu_Logo.png");
            var icons = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Menu_IconSheet.png");
            var utilityIcons = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Menu_UtilityIcons.png");
            var font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/Cinzel-Regular.ttf");
            if (background == null || logo == null || icons == null || utilityIcons == null || font == null)
                throw new System.InvalidOperationException("Menu images or font are missing.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";
            var cameraObject = new GameObject("MenuCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.tag = "MainCamera";

            var canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1672f, 941f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var bg = NewUI("Background", canvasObject.transform);
            Stretch(bg.GetComponent<RectTransform>());
            bg.AddComponent<RawImage>().texture = background;
            var fitter = bg.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)background.width / background.height;

            var wash = NewUI("LeftContrastWash", canvasObject.transform);
            PlaceTopLeft(wash.GetComponent<RectTransform>(), 0f, 0f, 660f, 941f);
            wash.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.17f);

            var logoObject = NewUI("CityForgeLogo", canvasObject.transform);
            PlaceTopLeft(logoObject.GetComponent<RectTransform>(), 78f, 75f, 640f, 242f);
            logoObject.AddComponent<RawImage>().texture = logo;

            var controllerObject = new GameObject("MainMenuController");
            var controller = controllerObject.AddComponent<MainMenuController>();
            var names = new[] { "Open Region", "Create Region", "Lot Editor", "Water Systems", "Mods" };
            var actions = new[] { MenuAction.OpenRegion, MenuAction.CreateRegion, MenuAction.LotEditor,
                MenuAction.WaterSystems, MenuAction.Mods };
            var crops = new[]
            {
                new Rect(10f, 105f, 405f, 395f),
                new Rect(390f, 130f, 465f, 365f),
                new Rect(860f, 120f, 365f, 380f),
                new Rect(1230f, 100f, 395f, 390f),
                new Rect(1650f, 125f, 395f, 370f)
            };
            for (var i = 0; i < names.Length; i++)
                CreateMainButton(canvasObject.transform, controller, names[i], actions[i], icons,
                    crops[i], 359f + i * 92f, font);

            CreateUtilityButton(canvasObject.transform, controller, "SETTINGS", MenuAction.Settings,
                utilityIcons, new Rect(245f, 20f, 680f, 670f), -218f, font);
            CreateUtilityButton(canvasObject.transform, controller, "EXIT", MenuAction.Exit,
                utilityIcons, new Rect(1150f, 0f, 770f, 700f), -36f, font);

            var settings = CreateModal(canvasObject.transform, "SettingsPanel", font, out var settingsCard);
            CreateText(settingsCard.transform, "SettingsTitle", "SETTINGS", font, 36,
                TextAnchor.MiddleCenter, Ivory, 50f, 27f, 550f, 62f);
            var displayButton = CreatePanelButton(settingsCard.transform, controller, MenuAction.ToggleFullscreen,
                "DisplayButton", "DISPLAY  WINDOWED", font, 74f, 110f, 500f, 63f);
            var volumeButton = CreatePanelButton(settingsCard.transform, controller, MenuAction.CycleVolume,
                "VolumeButton", "AUDIO  100%", font, 74f, 186f, 500f, 63f);
            CreatePanelButton(settingsCard.transform, controller, MenuAction.CloseDialog,
                "SettingsBackButton", "BACK", font, 224f, 283f, 200f, 56f);

            var info = CreateModal(canvasObject.transform, "InformationPanel", font, out var infoCard);
            var infoTitle = CreateText(infoCard.transform, "InformationTitle", "OPEN REGION", font, 34,
                TextAnchor.MiddleCenter, Ivory, 50f, 26f, 550f, 62f);
            var infoBody = CreateText(infoCard.transform, "InformationBody", "", font, 23,
                TextAnchor.MiddleCenter, Ivory, 67f, 112f, 516f, 150f);
            CreatePanelButton(infoCard.transform, controller, MenuAction.CloseDialog,
                "InformationBackButton", "BACK", font, 224f, 283f, 200f, 56f);
            controller.Configure(settings, info, infoTitle, infoBody,
                displayButton.GetComponentInChildren<Text>(), volumeButton.GetComponentInChildren<Text>());
            settings.SetActive(false);
            info.SetActive(false);

            ModalPrefabBuilder.BuildPrefab();
            ModalPrefabBuilder.InstallIntoScene(scene);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            var visualTest = Root + "/Scenes/Development/VisualTest.unity";
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(visualTest, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("CityForge: opening menu created at " + ScenePath);
        }

        [MenuItem("CityForge/Validate Opening Menu")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var camera = Object.FindFirstObjectByType<Camera>();
            var buttons = Object.FindObjectsByType<MenuActionButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log("CityForge menu check: controller=" + (menu != null) + ", canvas=" +
                (canvas != null) + ", camera=" + (camera != null) + ", action buttons=" + buttons.Length);
            if (menu == null || canvas == null || camera == null || buttons.Length != 11)
                throw new System.InvalidOperationException("Opening menu scene is incomplete.");
            foreach (var actionButton in buttons)
            {
                var reference = new SerializedObject(actionButton).FindProperty("controller").objectReferenceValue;
                if (reference != menu || actionButton.GetComponent<Button>() == null)
                    throw new System.InvalidOperationException("A menu action is not connected to its button or controller.");
            }
            if (EditorBuildSettings.scenes.Length == 0 || EditorBuildSettings.scenes[0].path != ScenePath)
                throw new System.InvalidOperationException("MainMenu is not the first build scene.");
            var capturePath = System.Environment.GetEnvironmentVariable("CITYFORGE_MENU_CAPTURE_PATH");
            if (!string.IsNullOrEmpty(capturePath)) Capture(camera, capturePath);
            menu.Handle(MenuAction.Settings);
            if (!canvas.transform.Find("SettingsPanel").gameObject.activeSelf)
                throw new System.InvalidOperationException("Settings did not open.");
            var settingsCapture = System.Environment.GetEnvironmentVariable("CITYFORGE_SETTINGS_CAPTURE_PATH");
            if (!string.IsNullOrEmpty(settingsCapture)) Capture(camera, settingsCapture);
            menu.Handle(MenuAction.CloseDialog);
            menu.Handle(MenuAction.CreateRegion);
            var largeModal = canvas.transform.Find("Modal75Overlay");
            var navigation = largeModal != null
                ? largeModal.Find("Panel75/RoundedContentClip/CreateRegionNavigation") : null;
            if (largeModal == null || !largeModal.gameObject.activeSelf ||
                navigation == null || !navigation.gameObject.activeSelf || navigation.childCount != 7)
                throw new System.InvalidOperationException("Create Region navigation is incomplete.");
            var regionModal = largeModal.GetComponent<CityForgeModal>();
            var settings = largeModal.Find(
                "Panel75/RoundedContentClip/ScrollViewport/ScrollContent/CreateRegionBasicSettings");
            if (settings == null || !settings.gameObject.activeSelf ||
                regionModal.SelectedEra.Name != "Founder's Era" ||
                regionModal.SelectedPeriod.Name != "Frontier Settlement")
                throw new System.InvalidOperationException("Create Region basic settings are incomplete.");
            var next = settings.Find("NextButton").GetComponent<Button>();
            var terrain = largeModal.Find(
                "Panel75/RoundedContentClip/ScrollViewport/ScrollContent/CreateRegionTerrainIntro");
            var terrainButton = navigation.Find("Nav1TERRAIN").GetComponent<Button>();
            regionModal.SetRegionName("A");
            if (next.interactable || terrainButton.interactable)
                throw new System.InvalidOperationException("Terrain unlocked before the region name was valid.");
            regionModal.SetRegionName("Ab");
            if (!next.interactable || !terrainButton.interactable)
                throw new System.InvalidOperationException("A valid region name did not unlock Terrain.");
            regionModal.SelectCreateRegionSection(1);
            if (terrain == null || !terrain.gameObject.activeSelf)
                throw new System.InvalidOperationException("Terrain did not open from Next.");
            var basicButton = navigation.Find("Nav0BASICSETTINGS").GetComponent<Button>();
            if (!basicButton.interactable)
                throw new System.InvalidOperationException("The previous Basic Settings step is disabled.");
            regionModal.SelectCreateRegionSection(0);
            if (!settings.gameObject.activeSelf || terrain.gameObject.activeSelf || regionModal.RegionName != "Ab")
                throw new System.InvalidOperationException("Returning to Basic Settings lost the region setup.");
            regionModal.SelectCreateRegionSection(1);
            var terrainNextRect = terrain.Find("NextButton").GetComponent<RectTransform>();
            var basicNextRect = next.GetComponent<RectTransform>();
            if (terrainNextRect.anchoredPosition != basicNextRect.anchoredPosition ||
                terrainNextRect.sizeDelta != basicNextRect.sizeDelta)
                throw new System.InvalidOperationException("Next buttons do not have matching layouts.");
            regionModal.SetRegionName(string.Empty);
            if (!settings.gameObject.activeSelf || terrainButton.interactable)
                throw new System.InvalidOperationException("Terrain stayed available after clearing the name.");
            for (var eraIndex = 0; eraIndex < CityForgeTimeline.Eras.Length; eraIndex++)
            {
                regionModal.SelectEra(eraIndex);
                if (regionModal.SelectedPeriod != CityForgeTimeline.Eras[eraIndex].Periods[0])
                    throw new System.InvalidOperationException("Period did not follow its era.");
                var lastPeriod = CityForgeTimeline.Eras[eraIndex].Periods.Length - 1;
                regionModal.SelectPeriod(lastPeriod);
                if (regionModal.SelectedPeriod != CityForgeTimeline.Eras[eraIndex].Periods[lastPeriod])
                    throw new System.InvalidOperationException("Period selection is incorrect.");
            }
            regionModal.SelectEra(0);
            menu.Handle(MenuAction.CloseDialog);
            menu.Handle(MenuAction.OpenRegion);
            if (!largeModal.gameObject.activeSelf || navigation.gameObject.activeSelf)
                throw new System.InvalidOperationException("The reusable region modal did not open.");
            menu.Handle(MenuAction.CloseDialog);
            Debug.Log("CityForge: opening menu validation passed.");
        }

        private static void ImportTexture(string path, bool alpha)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new System.InvalidOperationException("Missing texture: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = alpha;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void PlaceBottomRight(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void PlaceCenter(RectTransform rect, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void MakeFrame(GameObject go, Color fill)
        {
            go.AddComponent<Image>().color = fill;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(1.5f, 1.5f);
        }

        private static Text CreateText(Transform parent, string name, string content, Font font, int size,
            TextAnchor alignment, Color color, float x, float y, float width, float height)
        {
            var go = NewUI(name, parent);
            PlaceTopLeft(go.GetComponent<RectTransform>(), x, y, width, height);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateIcon(Transform parent, string name, Texture2D sheet, Rect crop,
            Vector2 position, Vector2 size)
        {
            var go = NewUI(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var raw = go.AddComponent<RawImage>();
            raw.texture = sheet;
            raw.uvRect = new Rect(crop.x / sheet.width,
                1f - (crop.y + crop.height) / sheet.height,
                crop.width / sheet.width, crop.height / sheet.height);
            raw.raycastTarget = false;
        }

        private static GameObject AddButton(GameObject go, MainMenuController controller, MenuAction action)
        {
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.22f, 1.15f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.67f, 0.55f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            go.AddComponent<MenuActionButton>().Configure(controller, action);
            return go;
        }

        private static void CreateMainButton(Transform parent, MainMenuController controller,
            string label, MenuAction action, Texture2D iconSheet, Rect crop, float top, Font font)
        {
            var go = NewUI(label.Replace(" ", "") + "Button", parent);
            PlaceTopLeft(go.GetComponent<RectTransform>(), 84f, top, 446f, 82f);
            MakeFrame(go, PanelColor);
            AddButton(go, controller, action);
            CreateIcon(go.transform, "Icon", iconSheet, crop, new Vector2(24f, 0f), new Vector2(70f, 67f));
            CreateText(go.transform, "Label", label, font, 31, TextAnchor.MiddleLeft,
                Ivory, 128f, 7f, 305f, 68f);
        }

        private static void CreateUtilityButton(Transform parent, MainMenuController controller,
            string label, MenuAction action, Texture2D iconSheet, Rect crop, float rightOffset, Font font)
        {
            var go = NewUI(label + "Button", parent);
            PlaceBottomRight(go.GetComponent<RectTransform>(), rightOffset, 34f, 170f, 56f);
            MakeFrame(go, PanelColor);
            AddButton(go, controller, action);
            CreateIcon(go.transform, "Icon", iconSheet, crop, new Vector2(14f, 0f), new Vector2(42f, 42f));
            CreateText(go.transform, "Label", label, font, 22, TextAnchor.MiddleLeft,
                Ivory, 62f, 4f, 102f, 48f);
        }

        private static GameObject CreateModal(Transform canvas, string name, Font font, out GameObject card)
        {
            var overlay = NewUI(name, canvas);
            Stretch(overlay.GetComponent<RectTransform>());
            overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.64f);
            card = NewUI("Card", overlay.transform);
            PlaceCenter(card.GetComponent<RectTransform>(), 650f, 380f);
            MakeFrame(card, new Color(0.065f, 0.056f, 0.041f, 0.97f));
            return overlay;
        }

        private static GameObject CreatePanelButton(Transform parent, MainMenuController controller,
            MenuAction action, string name, string label, Font font,
            float x, float y, float width, float height)
        {
            var go = NewUI(name, parent);
            PlaceTopLeft(go.GetComponent<RectTransform>(), x, y, width, height);
            MakeFrame(go, PanelColor);
            AddButton(go, controller, action);
            CreateText(go.transform, "Label", label, font, 25, TextAnchor.MiddleCenter,
                Ivory, 8f, 3f, width - 16f, height - 6f);
            return go;
        }

        private static void Capture(Camera camera, string path)
        {
            var texture = new RenderTexture(1672, 941, 24);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var image = new Texture2D(1672, 941, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = texture;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0f, 0f, 1672f, 941f), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log("CityForge: captured opening menu at " + path);
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(texture);
            }
        }
    }
}

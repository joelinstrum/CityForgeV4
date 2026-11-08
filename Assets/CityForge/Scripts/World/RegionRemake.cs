using System.Collections;
using CityForge.CameraSystem;
using CityForge.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        const string RemakeWarningKey = "CityForge.RegionRemakeAcknowledged";
        Button remakeButton;
        GameObject remakeWarning;
        bool remaking, skipPresentationCleanup;
        bool RemakeBlocksInput => remaking || remakeWarning;
        bool CanRemake => SelectedDistrict < 0 && CreationSettings != null && Towns.Length == 0
            && !IsSaving && !DistrictLoading && !returning && !RemakeBlocksInput;

        void BuildRemakeButton(Transform hud, Font font)
        {
            remakeButton = CreateRemakeAction(hud, font, "Remake Map", RequestRemake);
            var rect = (RectTransform)remakeButton.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-465, -25);
            rect.sizeDelta = new Vector2(205, 48);
        }

        void RefreshRemakeButton()
        {
            if (!remakeButton) return;
            remakeButton.gameObject.SetActive(SelectedDistrict < 0 && CreationSettings != null && Towns.Length == 0);
            remakeButton.interactable = CanRemake;
        }

        void RequestRemake()
        {
            if (!CanRemake) return;
            townTools.Cancel();
            var combiner = GetComponent<DistrictCombiner>();
            if (combiner.Active) combiner.SetActive(false);
            if (PlayerPrefs.GetInt(RemakeWarningKey) != 0)
            {
                BeginRemake();
                return;
            }

            var hud = remakeButton.transform.parent;
            var font = remakeButton.GetComponentInChildren<Text>().font;
            remakeWarning = new GameObject("Remake Map Warning", typeof(RectTransform), typeof(Image));
            remakeWarning.transform.SetParent(hud, false);
            var shade = remakeWarning.GetComponent<Image>();
            shade.color = new Color(.035f, .05f, .04f, .85f);
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = Vector2.one;
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(remakeWarning.transform, false);
            panel.GetComponent<Image>().color = new Color(.10f, .12f, .09f, 1);
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(640, 240);
            var title = HudText(panel.transform, "Title", font, 27);
            title.text = "Remake this map?";
            Position(title.rectTransform, new Vector2(28, -20), new Vector2(584, 42));
            var message = HudText(panel.transform, "Message", font, 21);
            message.text = "Remaking permanently replaces this map and erases everything on it, including district edits and placed lots.";
            Position(message.rectTransform, new Vector2(28, -72), new Vector2(584, 75));
            var cancel = CreateRemakeAction(panel.transform, font, "Cancel", CloseRemakeWarning);
            Position((RectTransform)cancel.transform, new Vector2(262, -168), new Vector2(155, 46));
            var confirm = CreateRemakeAction(panel.transform, font, "Remake Map", () =>
            {
                CloseRemakeWarning();
                BeginRemake();
            });
            Position((RectTransform)confirm.transform, new Vector2(427, -168), new Vector2(185, 46));
            cancel.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = confirm };
            confirm.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = cancel };
            cancel.Select();
            RefreshRemakeButton();
        }

        static Button CreateRemakeAction(Transform parent, Font font, string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(.18f, .20f, .15f, .98f);
            var caption = HudText(go.transform, "Caption", font, 19);
            caption.text = label;
            caption.alignment = TextAnchor.MiddleCenter;
            caption.rectTransform.anchorMin = Vector2.zero;
            caption.rectTransform.anchorMax = Vector2.one;
            caption.rectTransform.offsetMin = caption.rectTransform.offsetMax = Vector2.zero;
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(action);
            return button;
        }

        void CloseRemakeWarning()
        {
            remakeWarning.SetActive(false);
            Destroy(remakeWarning);
            remakeWarning = null;
            RefreshRemakeButton();
        }

        void BeginRemake()
        {
            if (!CanRemake) return;
            remaking = true;
            RefreshRemakeButton();
            var camera = GetComponentInChildren<CityForgeCameraController>();
            camera.enabled = false;
            var overlay = RegionLoadingOverlay.ShowOperation("Remaking " + RegionName);
            overlay.BeginDistrict(RemakeStages(overlay), () =>
            {
                if (!this || skipPresentationCleanup) return;
                remaking = false;
                camera.enabled = true;
                RefreshRemakeButton();
            }, error =>
            {
                if (this) regionNote.text = "Could not remake the map. Your current map was kept.";
                Debug.LogException(error);
            }, "Preparing a new map");
        }

        IEnumerator RemakeStages(RegionLoadingOverlay overlay)
        {
            var settings = JsonUtility.FromJson<RegionCreationSettings>(JsonUtility.ToJson(CreationSettings));
            do settings.seed = System.Guid.NewGuid().GetHashCode() & int.MaxValue; while (settings.seed == Data.Seed);
            if (settings.terrainShape == RegionTerrainShape.RiverAndBay)
            {
                settings.riverBayVariant = RiverBayLayout.ChooseRemakeVariant(settings.riverBayVariant, settings.seed);
                settings.waterwayVersion = Mathf.Max(settings.waterwayVersion, 6);
            }
            bool saved = SavedRegionStore.Exists(SavedRegionId);
            SavedRegionSnapshot snapshot = null;
            yield return RegionMapGenerator.GenerateStages(RegionName, Size, settings, value => snapshot = value, overlay.Report);
            if (saved) snapshot.info.id = SavedRegionId;

            var regionField = Shader.GetGlobalTexture("_RegionField");
            var meadowCover = Shader.GetGlobalTexture("_MeadowCoverField");
            var woodlandCover = Shader.GetGlobalTexture("_WoodlandCoverField");
            RestorePresentationSettings();
            gameObject.SetActive(false);
            PrototypeRegionView replacement = null;
            try
            {
                yield return OpenStages(RegionName, menuCanvas, snapshot: snapshot, progress: overlay.Report, remakingMap: true);
                replacement = FindFirstObjectByType<PrototypeRegionView>();
                replacement.menuCamera = menuCamera;
                if (saved)
                {
                    replacement.remaking = true;
                    replacement.RefreshRemakeButton();
                    overlay.Report("Saving remade map", .99f);
                    var write = SavedRegionStore.SaveAsync(snapshot);
                    while (!write.IsCompleted) yield return null;
                    write.GetAwaiter().GetResult();
                    replacement.remaking = false;
                    replacement.RefreshRemakeButton();
                }
                skipPresentationCleanup = true;
                Destroy(gameObject);
                if (PlayerPrefs.GetInt(RemakeWarningKey) == 0)
                {
                    PlayerPrefs.SetInt(RemakeWarningKey, 1);
                    PlayerPrefs.Save();
                }
            }
            finally
            {
                if (!skipPresentationCleanup)
                {
                    if (replacement)
                    {
                        replacement.remaking = false;
                        replacement.skipPresentationCleanup = true;
                        replacement.ReturnToMenu();
                    }
                    if (menuCanvas) menuCanvas.gameObject.SetActive(false);
                    if (menuCamera) menuCamera.enabled = false;
                    ApplyOverviewLighting();
                    Shader.SetGlobalTexture("_RegionField", regionField);
                    Shader.SetGlobalTexture("_MeadowCoverField", meadowCover);
                    Shader.SetGlobalTexture("_WoodlandCoverField", woodlandCover);
                    gameObject.SetActive(true);
                }
            }
        }
    }
}

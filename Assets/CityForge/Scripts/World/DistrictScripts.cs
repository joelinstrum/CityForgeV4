using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.UI;
using CityForge.CameraSystem;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private RectTransform districtScriptDialog;
        private InputField districtScriptInput;
        private Text districtScriptFeedback;
        private RectTransform regionMessage;
        private Font regionMessageFont;
        private readonly HashSet<string> readRegionMessages = new(StringComparer.Ordinal);
        internal bool DistrictScriptDialogOpen => districtScriptDialog || regionMessage;

        internal void RestoreReadRegionMessages(string[] ids)
        {
            if (ids != null) readRegionMessages.UnionWith(ids.Where(id => !string.IsNullOrEmpty(id)));
        }

        internal string[] CaptureReadRegionMessages() =>
            readRegionMessages.OrderBy(id => id, StringComparer.Ordinal).ToArray();

        internal void ShowNewRegionWelcome()
        {
            var founders = CityForgeTimeline.Find("founders");
            if (founders != null && !readRegionMessages.Contains("era:" + founders.id))
                ShowRegionEraMessage(founders, preview: false);
        }

        private bool UpdateRegionMessage()
        {
            if (!regionMessage) return false;
            if (Input.GetKeyDown(KeyCode.Escape)) CloseRegionMessage();
            return true;
        }

        private void ShowRegionEraMessage(CityForgeTimeline.Era era, bool preview)
        {
            var hud = transform.Find("RegionHud");
            if (!hud || regionMessage || string.IsNullOrWhiteSpace(era.summary)) return;
            string id = "era:" + era.id;
            regionMessage = FounderRect(hud, "Region era message", Vector2.zero, Vector2.zero);
            regionMessage.anchorMin = Vector2.zero;
            regionMessage.anchorMax = Vector2.one;
            regionMessage.offsetMin = regionMessage.offsetMax = Vector2.zero;
            regionMessage.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .72f);

            var panel = FounderRect(regionMessage, "Welcome", Vector2.zero, new Vector2(1370, 754));
            AddArtwork("RegionWelcomeFrame", Vector2.zero, new Vector2(1470, 820));
            AddArtwork("RegionWelcomeParchment", new Vector2(0, -3), new Vector2(1270, 584));
            bool founders = era.id == "founders";
            var illustration = string.IsNullOrWhiteSpace(era.messageIllustration)
                ? null : Resources.Load<Texture2D>(era.messageIllustration);
            if (illustration)
            {
                var size = new Vector2(illustration.width, illustration.height);
                size *= Mathf.Min(587f / size.x, 402f / size.y);
                TownArtwork(panel, "Era illustration", illustration,
                    new Rect(0, 0, illustration.width, illustration.height), new Vector2(-303, -17), size);
            }
            AddArtwork("RegionWelcomePlaque", new Vector2(0, 334), new Vector2(870, 282));

            if (!regionMessageFont)
            {
                regionMessageFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman" }, 24);
                if (regionMessageFont) Own(regionMessageFont);
            }
            var title = MessageText(founders ? "The " + era.Name : era.Name, new Vector2(0, 336),
                new Vector2(660, 68), 45, TownGold);
            title.fontStyle = FontStyle.Bold;
            var period = era.Periods[0];
            string date = founders
                ? $"{period.Name.ToUpperInvariant()}  ·  {period.FirstYear}–{period.LastYear}"
                : $"{era.FirstYear}–{era.LastYear}";
            MessageText(date, new Vector2(0, 298), new Vector2(660, 36), 21, TownGold);

            var body = MessageText(era.summary, new Vector2(illustration ? 300 : 0, 38),
                new Vector2(illustration ? 520 : 986, 340), 33, new Color(.20f, .11f, .055f));
            body.alignment = illustration ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            body.lineSpacing = 1.12f;

            var close = TownButton(panel, lotFont, "×", 1290, -47, 39, CloseRegionMessage);
            close.image.color = new Color(.30f, .19f, .08f);
            bool alreadyRead = readRegionMessages.Contains(id);
            var acknowledge = TownButton(panel, lotFont,
                alreadyRead ? "Close" : preview ? "Mark as Read" : "Begin Settlement",
                993, -678, 296, () =>
                {
                    readRegionMessages.Add(id);
                    CloseRegionMessage();
                });
            ((RectTransform)acknowledge.transform).sizeDelta = new Vector2(296, 48);
            acknowledge.image.color = new Color(.45f, .28f, .10f);
            var caption = acknowledge.GetComponentInChildren<Text>();
            if (regionMessageFont) caption.font = regionMessageFont;
            caption.fontSize = 24;
            caption.color = TownGold;
            acknowledge.Select();

            void AddArtwork(string name, Vector2 position, Vector2 size)
            {
                var texture = Resources.Load<Texture2D>("UI/TownConstruction/" + name);
                if (texture)
                    TownArtwork(panel, name, texture, new Rect(0, 0, texture.width, texture.height), position, size);
            }

            Text MessageText(string text, Vector2 position, Vector2 size, int fontSize, Color color)
            {
                var label = FounderText(panel, text, position, size, fontSize);
                if (regionMessageFont) label.font = regionMessageFont;
                label.color = color;
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;
                return label;
            }
        }

        private void CloseRegionMessage()
        {
            if (!regionMessage) return;
            regionMessage.gameObject.SetActive(false);
            Destroy(regionMessage.gameObject);
            regionMessage = null;
        }

        private bool UpdateDistrictScripts()
        {
            if (districtScriptDialog)
            {
                if (DistrictLoading || IsSaving || Input.GetKeyDown(KeyCode.Escape))
                    CloseDistrictScripts();
                else if (districtScriptInput && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                    RunDistrictScript();
                return true;
            }

            if (DistrictLoading || IsSaving || RemakeBlocksInput ||
                playerLotWorkspace || founderPicker || savedLotPicker || transportModal || !townToolbar) return false;
            if (!Input.GetKeyDown(KeyCode.S) || !(Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)))
                return false;

            districtScriptDialog = FounderRect(townToolbar.parent, "District script dialog", Vector2.zero, Vector2.zero);
            districtScriptDialog.anchorMin = Vector2.zero;
            districtScriptDialog.anchorMax = Vector2.one;
            districtScriptDialog.offsetMin = districtScriptDialog.offsetMax = Vector2.zero;
            districtScriptDialog.gameObject.AddComponent<Image>().color = Color.clear;
            var panel = FounderRect(districtScriptDialog, "Run script", Vector2.zero, new Vector2(520, 50));
            StyleTownPanel(panel, TownPanelColor);
            districtScriptInput = DefaultControls.CreateInputField(new DefaultControls.Resources()).GetComponent<InputField>();
            districtScriptInput.transform.SetParent(panel, false);
            Position((RectTransform)districtScriptInput.transform, Vector2.zero, new Vector2(500, 34));
            districtScriptInput.textComponent.font = lotFont;
            if (districtScriptInput.placeholder is Text placeholder)
            {
                placeholder.font = lotFont;
                placeholder.text = "Help";
            }
            districtScriptInput.characterLimit = 96;
            districtScriptFeedback = FounderText(panel, "", new Vector2(0, -43), new Vector2(520, 32), 16);
            districtScriptFeedback.color = TownGold;
            districtScriptInput?.Select();
            districtScriptInput?.ActivateInputField();
            return true;
        }

        private static bool IsEagleCommand(string command) =>
            string.Equals(command?.Trim(), "The eagle has landed", StringComparison.OrdinalIgnoreCase);

        private void RunDistrictScript()
        {
            if (!districtScriptDialog || !districtScriptInput) return;
            RunNamedScript(districtScriptInput.text);
        }

        private void RunNamedScript(string command)
        {
            command = command?.Trim() ?? "";
            if (string.Equals(command, "Help", StringComparison.OrdinalIgnoreCase))
            { OpenScriptHelp(); return; }
            if (command.StartsWith("Season:", StringComparison.OrdinalIgnoreCase) &&
                Enum.TryParse<RegionSeason>(command.Substring(7).Trim(), true, out var season) &&
                Enum.IsDefined(typeof(RegionSeason), season))
            { SetSeason(season); CloseDistrictScripts(); return; }
            if (SelectedDistrict >= 0 && string.Equals(command, "Place lot", StringComparison.OrdinalIgnoreCase))
            {
                CloseDistrictScripts(); CloseTownSection(); CancelLotPlacement();
                CancelRoadPlacement(); GetComponent<NatureToolbar>()?.Dismiss();
                OpenSavedLotPicker();
                return;
            }
            bool rain = string.Equals(command, "Let it rain", StringComparison.OrdinalIgnoreCase);
            bool snow = string.Equals(command, "Let it snow", StringComparison.OrdinalIgnoreCase);
            if (SelectedDistrict >= 0 && (rain || snow))
            {
                var weather = ApprovedDistrict ? ApprovedDistrict.GetComponent<DistrictWeather>() : null;
                if (!weather) { districtScriptFeedback.text = "Weather is not ready yet."; return; }
                SetSeason(snow ? RegionSeason.Winter : Season == RegionSeason.Winter ? RegionSeason.Summer : Season);
                if (snow && VisualSeason != RegionSeason.Winter)
                { districtScriptFeedback.text = "Snow requires a temperate district."; return; }
                weather.SetWeather(snow ? DistrictWeather.WeatherKind.Snow : DistrictWeather.WeatherKind.Rain);
                CloseDistrictScripts(); return;
            }
            const string displayMessage = "Display message:";
            if (SelectedDistrict < 0 && command.TrimStart().StartsWith(displayMessage, StringComparison.OrdinalIgnoreCase))
            {
                var name = command.TrimStart().Substring(displayMessage.Length).Trim(' ', '\t', '"', '“', '”');
                var era = Array.Find(CityForgeTimeline.Eras, item =>
                    string.Equals(item.id, name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(EraCommandName(item.Name), EraCommandName(name), StringComparison.OrdinalIgnoreCase));
                if (era != null && !string.IsNullOrWhiteSpace(era.summary))
                {
                    CloseDistrictScripts();
                    ShowRegionEraMessage(era, preview: true);
                    return;
                }
                districtScriptFeedback.text = "Try Display message: Founder’s Era.";
                districtScriptInput?.ActivateInputField();
                return;
            }
            if (SelectedDistrict < 0)
            {
                districtScriptFeedback.text = "Try Display message: Founder’s Era. District scripts require an open district.";
                districtScriptInput?.ActivateInputField();
                return;
            }

            if (string.Equals(command?.Trim(), "Hooligan harass", StringComparison.OrdinalIgnoreCase))
            {
                if (TryStartHarassTest(out var problem))
                {
                    CloseDistrictScripts();
                    ShowDistrictMessage("Following Hooligan · Patrol constables respond · Esc to end.");
                    return;
                }
                districtScriptFeedback.text = problem;
                districtScriptInput?.ActivateInputField();
                return;
            }
            bool perch = string.Equals(command?.Trim(), "Eagle perch", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(command?.Trim(), "Fell fir", StringComparison.OrdinalIgnoreCase))
            {
                var camera = GetComponentInChildren<CityForgeCameraController>();
                var focus = camera ? camera.CaptureRegionState().focus : Vector3.zero;
                if (camera && ApprovedDistrict && ApprovedDistrict.TryFellCilicianFir(new Vector2(focus.x, focus.z), 40f))
                {
                    CloseDistrictScripts();
                    return;
                }
                districtScriptFeedback.text = "No harvestable Cilician fir within 40 m of the camera focus.";
            }
            else if (!perch && !IsEagleCommand(command))
                districtScriptFeedback.text = "Type Help to see available scripts.";
            else if (!eagle)
                districtScriptFeedback.text = "Requires daylight, Animals enabled, and a built-up district.";
            else if (perch)
            {
                if (TryPerchEagle())
                {
                    CloseDistrictScripts();
                    return;
                }
                districtScriptFeedback.text = "The eagle needs to be circling and a suitable tree must be available.";
            }
            else if (eaglePhase is EaglePhase.SearchClimb or EaglePhase.Search or EaglePhase.StreetApproach or EaglePhase.Reach or EaglePhase.Carry or EaglePhase.DropRun or EaglePhase.Release or EaglePhase.Splash)
                districtScriptFeedback.text = "The eagle encounter is already running.";
            else if (!StartEagleSearch(command: true))
                districtScriptFeedback.text = "Requires a walking pedestrian and water or trees for a landing.";
            else
            {
                eagleCameraFollow = true;
                CloseDistrictScripts();
                ShowDistrictMessage("Eagle is searching.");
                return;
            }
            districtScriptInput?.Select();
            districtScriptInput?.ActivateInputField();
        }

        private void OpenScriptHelp()
        {
            CloseDistrictScripts();
            districtScriptDialog = FounderRect(townToolbar.parent, "Script help", Vector2.zero, Vector2.zero);
            districtScriptDialog.anchorMin = Vector2.zero; districtScriptDialog.anchorMax = Vector2.one;
            districtScriptDialog.offsetMin = districtScriptDialog.offsetMax = Vector2.zero;
            districtScriptDialog.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .65f);
            var panel = FounderRect(districtScriptDialog, "Available scripts", Vector2.zero, new Vector2(700, 560));
            StyleTownPanel(panel, TownPanelColor);
            FounderText(panel, SelectedDistrict < 0 ? "Region scripts" : "District scripts",
                new Vector2(0, 242), new Vector2(610, 40), 28);
            var close = TownButton(panel, lotFont, "×", 650, -15, 36, CloseDistrictScripts);
            var viewport = FounderRect(panel, "Script viewport", new Vector2(0, 3), new Vector2(650, 425));
            viewport.gameObject.AddComponent<Image>().color = new Color(.08f, .09f, .10f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            var content = FounderRect(viewport, "Scripts", Vector2.zero, new Vector2(650, 0));
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            var commands = new List<string> { "Help", "Season: Summer", "Season: Fall", "Season: Winter" };
            if (SelectedDistrict >= 0)
                commands.AddRange(new[] { "Let it rain", "Let it snow", "Place lot", "Hooligan harass", "Fell fir", "Eagle perch", "The eagle has landed" });
            else
                commands.AddRange(CityForgeTimeline.Eras.Where(era => !string.IsNullOrWhiteSpace(era.summary))
                    .Select(era => "Display message: " + era.Name));
            content.sizeDelta = new Vector2(650, commands.Count * 50 + 10);
            for (int i = 0; i < commands.Count; i++)
            {
                string command = commands[i];
                var button = TownButton(content, lotFont, command, 10, -5 - i * 50, 630, () => RunNamedScript(command));
                ((RectTransform)button.transform).sizeDelta = new Vector2(630, 42);
            }
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            districtScriptFeedback = FounderText(panel, "Click a script to run it · Scroll for more · Esc to close",
                new Vector2(0, -242), new Vector2(650, 38), 16);
            districtScriptFeedback.color = TownGold;
        }

        private static string EraCommandName(string name) =>
            name.Replace("'", "").Replace("’", "");

        private void CloseDistrictScripts()
        {
            if (!districtScriptDialog) return;
            districtScriptDialog.gameObject.SetActive(false);
            Destroy(districtScriptDialog.gameObject);
            districtScriptDialog = null;
            districtScriptInput = null;
            districtScriptFeedback = null;
        }
    }
}

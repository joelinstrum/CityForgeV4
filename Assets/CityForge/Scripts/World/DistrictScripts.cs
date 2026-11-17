using System;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private RectTransform districtScriptDialog;
        private InputField districtScriptInput;
        private Text districtScriptFeedback;
        internal bool DistrictScriptDialogOpen => districtScriptDialog;

        private bool UpdateDistrictScripts()
        {
            if (districtScriptDialog)
            {
                if (SelectedDistrict < 0 || DistrictLoading || IsSaving || Input.GetKeyDown(KeyCode.Escape))
                    CloseDistrictScripts();
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    RunDistrictScript();
                return true;
            }

            if (SelectedDistrict < 0 || DistrictLoading || IsSaving || RemakeBlocksInput ||
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
                placeholder.text = "The eagle has landed";
            }
            districtScriptInput.characterLimit = 96;
            districtScriptFeedback = FounderText(panel, "", new Vector2(0, -43), new Vector2(520, 32), 16);
            districtScriptFeedback.color = TownGold;
            districtScriptInput.Select();
            districtScriptInput.ActivateInputField();
            return true;
        }

        private static bool IsEagleCommand(string command) =>
            string.Equals(command?.Trim(), "The eagle has landed", StringComparison.OrdinalIgnoreCase);

        private void RunDistrictScript()
        {
            if (!districtScriptDialog) return;
            string command = districtScriptInput.text;
            bool perch = string.Equals(command?.Trim(), "Eagle perch", StringComparison.OrdinalIgnoreCase);
            if (!perch && !IsEagleCommand(command))
                districtScriptFeedback.text = "Unknown script. Try Eagle perch or The eagle has landed.";
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
                districtScriptFeedback.text = "Requires a walking pedestrian and a district with water.";
            else
            {
                eagleCameraFollow = true;
                CloseDistrictScripts();
                ShowDistrictMessage("Eagle is searching.");
                return;
            }
            districtScriptInput.Select();
            districtScriptInput.ActivateInputField();
        }

        private void CloseDistrictScripts()
        {
            if (!districtScriptDialog) return;
            districtScriptDialog.gameObject.SetActive(false);
            Destroy(districtScriptDialog.gameObject);
            districtScriptDialog = null;
        }
    }
}

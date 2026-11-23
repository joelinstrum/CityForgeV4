using System.Linq;
using CityForge.CameraSystem;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        public bool ImmigrationPaused { get; private set; }
        private RectTransform gameplayToolsButton, gameplayToolsPanel;
        private Text immigrationSummary, immigrationStatus, immigrationToggleLabel, modeStatus, modeToggleLabel;
        private CityForgeCameraController gameplayCamera;
        private bool gameplayCameraWasEnabled;

        private void BuildGameplayTools(Transform hud, Font font)
        {
            LoadGovernmentArtwork();
            gameplayToolsButton = FounderRect(hud, "Game", Vector2.zero, new Vector2(70, 70));
            gameplayToolsButton.anchorMin = gameplayToolsButton.anchorMax = gameplayToolsButton.pivot = new Vector2(0, 1);
            var button = TownButton(gameplayToolsButton, font, "Game", 0, 0, 70, OpenGameplayTools);
            ((RectTransform)button.transform).sizeDelta = new Vector2(70,70);
            button.image.color = Color.clear;
            button.GetComponentInChildren<Text>().enabled=false;
            var artwork=Resources.Load<Texture2D>("UI/Gameplay/GameIcon");
            if(artwork)
            {
            var icon=TownArtwork(button.transform,"Game icon",artwork,
                new Rect(0,0,1,1),Vector2.zero,new Vector2(66,66));
            icon.uvRect=new Rect(.24f,.16f,.52f,.71f);
            var shader=Resources.Load<Shader>("UI/TownButtonArtwork");
            if(shader)icon.material=Own(new Material(shader));
            button.targetGraphic=icon;
            }
            else {button.image.color=TownPanelColor;button.GetComponentInChildren<Text>().enabled=true;}
            var tip=FounderRect(gameplayToolsButton,"Game tooltip",new Vector2(0,-82),new Vector2(80,28));
            StyleTownPanel(tip,TownPanelColor);tip.GetComponent<Image>().raycastTarget=false;
            FounderText(tip,"Game",Vector2.zero,new Vector2(76,24),16).color=TownGold;
            button.gameObject.AddComponent<CityForge.UI.GameplayIconHint>().Initialize(tip.gameObject);
        }

        private bool UpdateGameplayTools()
        {
            if (gameplayToolsButton)
            {
                gameplayToolsButton.anchoredPosition = new Vector2(25, SelectedDistrict < 0 ? -252 : -18);
                gameplayToolsButton.GetComponentInChildren<Button>().interactable = !IsSaving && !DistrictLoading &&
                    !founderPicker && !transportModal && !playerLotWorkspace && !regionMessage;
            }
            if (!gameplayToolsPanel) return false;
            if (Input.GetKeyDown(KeyCode.Escape)) CloseGameplayTools();
            return true;
        }

        private void OpenGameplayTools()
        {
            if (gameplayToolsPanel || DistrictLoading || IsSaving) return;
            var hud = transform.Find("RegionHud");
            if (!hud) return;
            gameplayCamera = GetComponentInChildren<CityForgeCameraController>();
            if (gameplayCamera) { gameplayCameraWasEnabled = gameplayCamera.enabled; gameplayCamera.enabled = false; }
            gameplayToolsPanel = FounderRect(hud, "Gameplay settings", Vector2.zero, Vector2.zero);
            gameplayToolsPanel.anchorMin = Vector2.zero; gameplayToolsPanel.anchorMax = Vector2.one;
            gameplayToolsPanel.offsetMin = gameplayToolsPanel.offsetMax = Vector2.zero;
            gameplayToolsPanel.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .6f);
            BuildGovernmentDashboard();
            RefreshImmigrationControl();
            gameplayToolsPanel.SetAsLastSibling();
        }

        internal void SetImmigrationPaused(bool paused)
        {
            ImmigrationPaused = paused;
            RefreshImmigrationControl();
        }

        private void RefreshImmigrationControl()
        {
            RefreshGovernmentDashboard();
        }

        private void CloseGameplayTools()
        {
            if (!gameplayToolsPanel) return;
            gameplayToolsPanel.gameObject.SetActive(false); Destroy(gameplayToolsPanel.gameObject);
            governmentPage=null;governmentContent=null;governmentEventsText=null;governmentNav.Clear();
            governmentModeBadge=null;governmentGlance=null;immigrationSummary=null;
            gameplayToolsPanel = null; immigrationStatus = null; immigrationToggleLabel = null;modeStatus=modeToggleLabel=null;
            if (gameplayCamera) gameplayCamera.enabled = gameplayCameraWasEnabled;
            gameplayCamera = null;
        }
    }
}

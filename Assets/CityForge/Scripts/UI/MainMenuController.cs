using UnityEngine;
using UnityEngine.UI;

namespace CityForge.UI
{
    public enum MenuAction
    {
        OpenRegion,
        CreateRegion,
        LotEditor,
        WaterSystems,
        Mods,
        Settings,
        Exit,
        CloseDialog,
        ToggleFullscreen,
        CycleVolume
    }

    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject informationPanel;
        [SerializeField] private Text informationTitle;
        [SerializeField] private Text informationBody;
        [SerializeField] private Text fullscreenLabel;
        [SerializeField] private Text volumeLabel;
        [SerializeField] private CityForgeModal largeModal;
        [SerializeField] private Texture2D menuIconSheet;

        private const string VolumeKey = "CityForge.MasterVolume";
        private const string FullscreenKey = "CityForge.Fullscreen";

        public void Configure(GameObject settings, GameObject information, Text title, Text body,
            Text fullscreen, Text volume)
        {
            settingsPanel = settings;
            informationPanel = information;
            informationTitle = title;
            informationBody = body;
            fullscreenLabel = fullscreen;
            volumeLabel = volume;
        }

        public void ConfigureLargeModal(CityForgeModal modal, Texture2D icons)
        {
            largeModal = modal;
            menuIconSheet = icons;
        }

        private void Awake()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (informationPanel != null) informationPanel.SetActive(false);
            if (largeModal != null) largeModal.Close();
            AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            if (PlayerPrefs.HasKey(FullscreenKey))
                Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey) == 1;
            RefreshSettingsLabels();
        }

        public bool IsOpeningRegion {get;private set;}
        private void Update()
        {
            if (!IsOpeningRegion&&Input.GetKeyDown(KeyCode.Escape)) CloseDialog();
        }

        public void Handle(MenuAction action)
        {
            if(IsOpeningRegion)return;
            switch (action)
            {
                case MenuAction.OpenRegion:
                    ShowRegionModal("OPEN REGION",string.Empty,new Rect(10f,105f,405f,395f));
                    if(largeModal)largeModal.ShowSavedRegions(World.SavedRegionStore.ListRegions(),OpenSavedRegion);
                    break;
                case MenuAction.CreateRegion:
                    ShowRegionModal("CREATE REGION", string.Empty,
                        new Rect(390f, 130f, 465f, 365f), true);
                    break;
                case MenuAction.LotEditor:
                    ShowInformation("LOT EDITOR", "The lot editor has not been built for V4 yet.");
                    break;
                case MenuAction.WaterSystems:
                    ShowInformation("WATER SYSTEMS", "Water is a separate V4 rendering and design milestone.");
                    break;
                case MenuAction.Mods:
                    ShowInformation("MODS", "Mod support will be added as V4 content systems take shape.");
                    break;
                case MenuAction.Settings:
                    if (informationPanel != null) informationPanel.SetActive(false);
                    if (largeModal != null) largeModal.Close();
                    if (settingsPanel != null) settingsPanel.SetActive(true);
                    RefreshSettingsLabels();
                    break;
                case MenuAction.Exit:
                    Exit();
                    break;
                case MenuAction.CloseDialog:
                    CloseDialog();
                    break;
                case MenuAction.ToggleFullscreen:
                    Screen.fullScreen = !Screen.fullScreen;
                    PlayerPrefs.SetInt(FullscreenKey, Screen.fullScreen ? 1 : 0);
                    PlayerPrefs.Save();
                    RefreshSettingsLabels();
                    break;
                case MenuAction.CycleVolume:
                    var next = AudioListener.volume >= 0.99f ? 0f : Mathf.Min(1f, AudioListener.volume + 0.25f);
                    AudioListener.volume = next;
                    PlayerPrefs.SetFloat(VolumeKey, next);
                    PlayerPrefs.Save();
                    RefreshSettingsLabels();
                    break;
            }
        }

        private void ShowInformation(string title, string body)
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (largeModal != null) largeModal.Close();
            if (informationTitle != null) informationTitle.text = title;
            if (informationBody != null) informationBody.text = body;
            if (informationPanel != null) informationPanel.SetActive(true);
        }

        private void ShowRegionModal(string title, string body, Rect iconCrop,
            bool showCreateNavigation = false)
        {
            if (largeModal == null || menuIconSheet == null)
            {
                ShowInformation(title, body);
                return;
            }

            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (informationPanel != null) informationPanel.SetActive(false);
            var uv = new Rect(iconCrop.x / menuIconSheet.width,
                1f - (iconCrop.y + iconCrop.height) / menuIconSheet.height,
                iconCrop.width / menuIconSheet.width, iconCrop.height / menuIconSheet.height);
            if (showCreateNavigation)
                largeModal.ShowCreateRegion(title, menuIconSheet, uv, "CITYFORGE  /  REGIONS");
            else
                largeModal.Show(title, body, menuIconSheet, uv, "CITYFORGE  /  REGIONS");
        }

        private void CloseDialog()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (informationPanel != null) informationPanel.SetActive(false);
            if (largeModal != null) largeModal.Close();
        }

        private void RefreshSettingsLabels()
        {
            if (fullscreenLabel != null)
                fullscreenLabel.text = Screen.fullScreen ? "DISPLAY  FULL SCREEN" : "DISPLAY  WINDOWED";
            if (volumeLabel != null)
                volumeLabel.text = "AUDIO  " + Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
        }

        public void OpenSavedRegion(World.SavedRegionInfo info)
        {
            if(!Application.isPlaying||IsOpeningRegion)return;
            var canvas=largeModal.GetComponentInParent<Canvas>();
            var label=largeModal.GetComponentInChildren<Text>(true);
            CloseDialog();IsOpeningRegion=true;
            var loading=RegionLoadingOverlay.Show(info.name,label?label.font:null);
            loading.Begin(info,canvas,()=>{IsOpeningRegion=false;if(World.PanningTestDistricts.TryGetPercent(info.id,out _)){var view=FindFirstObjectByType<World.PrototypeRegionView>();if(view)view.RequestDistrict(3);}},error=>{
                Debug.LogException(error);
                ShowRegionModal("OPEN REGION","This saved region could not be opened. Your save file has been kept.",new Rect(10f,105f,405f,395f));
            });
        }

#if UNITY_EDITOR
        // Historical asset-study tools deliberately inspect the result in the same call.
        public void OpenSavedRegionImmediatelyForTools(World.SavedRegionInfo info)
        {
            if(IsOpeningRegion)throw new System.InvalidOperationException("A region is loading.");
            var saved=World.SavedRegionStore.Load(info.path);
            World.PrototypeRegionView.Open(saved.info.name,largeModal.GetComponentInParent<Canvas>(),snapshot:saved);
            CloseDialog();
        }
#endif

        public void CreatePrototypeRegion(string regionName, CityForge.World.RegionSize size = CityForge.World.RegionSize.Medium, World.RegionCreationSettings settings = null)
        {
            if (!Application.isPlaying) return;
            if(IsOpeningRegion)return;
            var menu=largeModal.GetComponentInParent<Canvas>();
            var overlay=RegionLoadingOverlay.Show(regionName,null);IsOpeningRegion=true;
            overlay.BeginDistrict(CreateRegionStages(regionName,size,settings??new World.RegionCreationSettings(),menu,overlay.Report),()=>{if(this)IsOpeningRegion=false;},error=>{Debug.LogException(error);ShowRegionModal("CREATE REGION","The region could not be generated. Please try again.",new Rect(10f,105f,405f,395f));},"Preparing new region");
            CloseDialog();
        }
        private System.Collections.IEnumerator CreateRegionStages(string name,World.RegionSize size,World.RegionCreationSettings settings,Canvas menu,System.Action<string,float> progress)
        {
            World.SavedRegionSnapshot snapshot=null;
            yield return World.RegionMapGenerator.GenerateStages(name,size,settings,value=>snapshot=value,progress);
            yield return World.PrototypeRegionView.OpenStages(name,menu,snapshot:snapshot,progress:progress);
        }

        private static void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

}

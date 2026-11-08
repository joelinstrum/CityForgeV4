using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityForge.UI
{
    /// <summary>A reusable, screen-relative modal shell with a clipped scrolling body.</summary>
    public sealed class CityForgeModal : MonoBehaviour
    {
        [SerializeField] private Button backdropButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RawImage heroIcon;
        [SerializeField] private Text headerLabel;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Text footerLabel;
        [SerializeField] private ScrollRect bodyScroll;
        [SerializeField] private RectTransform bodyViewport;
        [SerializeField] private GameObject createRegionNavigation;
        [SerializeField] private RawImage[] navigationBackgrounds;
        [SerializeField] private Button[] navigationButtons;
        [SerializeField] private Texture2D navigationButtonTexture;
        [SerializeField] private GameObject createRegionBasicSettings;
        [SerializeField] private InputField regionNameInput;
        [SerializeField] private Dropdown regionSizeInput;
        [SerializeField] private Text eraValue;
        [SerializeField] private Button eraPrevious;
        [SerializeField] private Button eraNext;
        [SerializeField] private Text periodValue;
        [SerializeField] private Button periodPrevious;
        [SerializeField] private Button periodNext;
        [SerializeField] private Text periodYears;
        [SerializeField] private Button nextButton;
        [SerializeField] private GameObject createRegionTerrainIntro;
        [SerializeField] private Button terrainNextButton;

        private InputField generationSeed;
        private World.PlaceNameGenerator regionNames;
        private string selectedRegionSuffix = string.Empty;
        private int regionNameCharacterLimit;
        private GameObject regionNameHint;
        GameObject terrainShapePanel;Dropdown terrainShapeChoice;Button createMapButton;Text terrainShapeDescription;RawImage terrainShapePreview;Texture2D terrainShapeTexture;
        GameObject resourcesPanel;
        Dropdown resourceAbundance, resourceDistribution, resourceDiscovery;
        Button terrainContinueButton;
        const int ResourcesSection = 3;

        static readonly World.RegionTerrainShape[] TerrainShapes={World.RegionTerrainShape.Peninsula,World.RegionTerrainShape.Land,World.RegionTerrainShape.Coast,World.RegionTerrainShape.MountainBorder,World.RegionTerrainShape.OneIsland,World.RegionTerrainShape.SmallIslands,World.RegionTerrainShape.RiverAndBay};
        private GameObject savedRegionList;
        private const float NavigationWidth = 270f;
        private int selectedNavigationIndex;
        private int selectedEraIndex;
        private int selectedPeriodIndex;

        public CityForge.World.RegionSize SelectedSize => regionSizeInput != null ? (CityForge.World.RegionSize)regionSizeInput.value : CityForge.World.RegionSize.Medium;
        public void SetRegionSize(CityForge.World.RegionSize size) { if (regionSizeInput != null) regionSizeInput.value = (int)size; }

        private string BaseRegionName
        {
            get
            {
                string name = regionNameInput ? regionNameInput.text.Trim() : string.Empty;
                string suffix = " " + selectedRegionSuffix;
                return selectedRegionSuffix.Length > 0 && name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                    ? name.Substring(0, name.Length - suffix.Length).TrimEnd()
                    : name;
            }
        }
        public string RegionName => AppendRegionSuffix(BaseRegionName);

        private string AppendRegionSuffix(string name)
        {
            if (name.Length == 0 || selectedRegionSuffix.Length == 0) return name;
            string suffix = " " + selectedRegionSuffix;
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - suffix.Length).TrimEnd();
            return name + suffix;
        }

        public CityForgeTimeline.Era SelectedEra => CityForgeTimeline.Eras[selectedEraIndex];
        public CityForgeTimeline.Period SelectedPeriod => SelectedEra.Periods[selectedPeriodIndex];
        public bool BasicSettingsComplete => CountLetters(BaseRegionName) >= 2;

        public void SetRegionName(string name)
        {
            if (regionNameInput != null)
            {
                string displayName = AppendRegionSuffix(name?.Trim() ?? string.Empty);
                if (regionNameCharacterLimit > 0)
                    regionNameInput.characterLimit = Mathf.Max(regionNameCharacterLimit, displayName.Length);
                regionNameInput.text = displayName;
            }
            RefreshProgression();
        }

        public void Configure(Button backdrop, Button close, RawImage icon, Text header,
            Text body, Text footer, ScrollRect scroll)
        {
            backdropButton = backdrop;
            closeButton = close;
            heroIcon = icon;
            headerLabel = header;
            bodyLabel = body;
            footerLabel = footer;
            bodyScroll = scroll;
            bodyViewport = scroll != null ? scroll.GetComponent<RectTransform>() : null;
        }

        public void ConfigureCreateRegionNavigation(GameObject navigation, RawImage[] backgrounds,
            Button[] buttons, Texture2D buttonTexture)
        {
            createRegionNavigation = navigation;
            navigationBackgrounds = backgrounds;
            navigationButtons = buttons;
            navigationButtonTexture = buttonTexture;
        }

        public void ConfigureCreateRegionSettings(GameObject settings, InputField nameInput,
            Text era, Button previousEra, Button nextEra, Text period,
            Button previousPeriod, Button nextPeriod, Text years, Button next,
            GameObject terrainIntro, Button terrainNext, Dropdown sizeInput)
        {
            createRegionBasicSettings = settings;
            regionNameInput = nameInput;
            regionSizeInput = sizeInput;
            eraValue = era;
            eraPrevious = previousEra;
            eraNext = nextEra;
            periodValue = period;
            periodPrevious = previousPeriod;
            periodNext = nextPeriod;
            periodYears = years;
            nextButton = next;
            createRegionTerrainIntro = terrainIntro;
            terrainNextButton = terrainNext;
        }

        private void Awake()
        {
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (eraPrevious != null) eraPrevious.onClick.AddListener(() => ChangeEra(-1));
            if (eraNext != null) eraNext.onClick.AddListener(() => ChangeEra(1));
            if (periodPrevious != null) periodPrevious.onClick.AddListener(() => ChangePeriod(-1));
            if (periodNext != null) periodNext.onClick.AddListener(() => ChangePeriod(1));
            if (regionNameInput != null) regionNameInput.onValueChanged.AddListener(_ => RefreshProgression());
            if (nextButton != null) nextButton.onClick.AddListener(() => SelectCreateRegionSection(1));
            if (terrainNextButton != null) terrainNextButton.onClick.AddListener(()=>SelectCreateRegionSection(2));
            RefreshTimeSelection();
            if (navigationButtons != null)
            {
                for (var index = 0; index < navigationButtons.Length; index++)
                {
                    var button = navigationButtons[index];
                    if (button != null)
                        button.onClick.AddListener(() => SelectCreateRegionSection(Array.IndexOf(navigationButtons, button)));
                }
            }
        }

        public void Show(string header, string body, Texture iconTexture, Rect iconUv, string footer)
        {
            if(savedRegionList)savedRegionList.SetActive(false);
            SetCreateRegionNavigationVisible(false);
            if (createRegionBasicSettings != null) createRegionBasicSettings.SetActive(false);
            if (createRegionTerrainIntro != null) createRegionTerrainIntro.SetActive(false);
            if(terrainShapePanel)terrainShapePanel.SetActive(false);
            if(resourcesPanel)resourcesPanel.SetActive(false);
            if (bodyLabel != null) bodyLabel.gameObject.SetActive(true);
            if (headerLabel != null) headerLabel.text = header;
            if (bodyLabel != null) bodyLabel.text = body;
            if (footerLabel != null) footerLabel.text = footer;
            if (heroIcon != null)
            {
                heroIcon.gameObject.SetActive(iconTexture != null);
                heroIcon.texture = iconTexture;
                heroIcon.uvRect = iconUv;
            }

            gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            if (bodyScroll != null) bodyScroll.verticalNormalizedPosition = 1f;
        }

        public void ShowSavedRegions(System.Collections.Generic.List<World.SavedRegionInfo> regions,System.Action<World.SavedRegionInfo> open)
        {
            if(savedRegionList){savedRegionList.SetActive(false);Destroy(savedRegionList);}
            savedRegionList=new GameObject("SavedRegionList",typeof(RectTransform),typeof(VerticalLayoutGroup));savedRegionList.transform.SetParent(bodyScroll.content,false);
            var layout=savedRegionList.GetComponent<VerticalLayoutGroup>();layout.spacing=12;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
            bodyLabel.gameObject.SetActive(false);
            foreach(var entry in regions){
                var region=entry;
                var row=new GameObject("OpenRegion_"+region.id,typeof(RectTransform),typeof(Image),typeof(Button),typeof(LayoutElement));row.transform.SetParent(savedRegionList.transform,false);row.GetComponent<LayoutElement>().preferredHeight=104;
                row.GetComponent<Image>().color=new Color(.12f,.15f,.14f,.98f);var colors=row.GetComponent<Button>().colors;colors.highlightedColor=new Color(.78f,.87f,.78f);colors.pressedColor=new Color(.65f,.76f,.65f);row.GetComponent<Button>().colors=colors;
                var name=SavedText(row.transform,"Name",region.name,27);name.rectTransform.anchorMin=new Vector2(0,1);name.rectTransform.anchorMax=new Vector2(1,1);name.rectTransform.pivot=new Vector2(0,1);name.rectTransform.offsetMin=new Vector2(24,-52);name.rectTransform.offsetMax=new Vector2(-290,-14);
                string district=region.district==3?"Northeast district":region.district<0?"Region overview":"District "+(region.district+1);
                var detail=SavedText(row.transform,"Details",((World.RegionSize)region.size)+" region  ·  "+district,18);detail.color=new Color(.72f,.76f,.70f);detail.rectTransform.anchorMin=new Vector2(0,0);detail.rectTransform.anchorMax=new Vector2(1,0);detail.rectTransform.pivot=new Vector2(0,0);detail.rectTransform.offsetMin=new Vector2(24,18);detail.rectTransform.offsetMax=new Vector2(-290,45);
                var action=SavedText(row.transform,"OpenLabel","OPEN",21);action.alignment=TextAnchor.MiddleRight;action.rectTransform.anchorMin=new Vector2(1,0);action.rectTransform.anchorMax=Vector2.one;action.rectTransform.offsetMin=new Vector2(-270,0);action.rectTransform.offsetMax=new Vector2(-155,0);
                var openButton=row.GetComponent<Button>();
                openButton.onClick.AddListener(()=>open(region));
                var deleteObject=new GameObject("DeleteRegion",typeof(RectTransform),typeof(Image),typeof(Button));deleteObject.transform.SetParent(row.transform,false);
                var deleteRect=deleteObject.GetComponent<RectTransform>();deleteRect.anchorMin=new Vector2(1,.5f);deleteRect.anchorMax=new Vector2(1,.5f);deleteRect.pivot=new Vector2(1,.5f);deleteRect.anchoredPosition=new Vector2(-16,0);deleteRect.sizeDelta=new Vector2(125,64);
                deleteObject.GetComponent<Image>().color=new Color(.35f,.16f,.14f,1);
                var deleteLabel=SavedText(deleteObject.transform,"Label","DELETE",19);deleteLabel.alignment=TextAnchor.MiddleCenter;deleteLabel.rectTransform.anchorMin=Vector2.zero;deleteLabel.rectTransform.anchorMax=Vector2.one;deleteLabel.rectTransform.offsetMin=Vector2.zero;deleteLabel.rectTransform.offsetMax=Vector2.zero;
                bool confirming=false;
                deleteObject.GetComponent<Button>().onClick.AddListener(()=>{
                    if(!confirming){
                        confirming=true;name.text="Delete “"+region.name+"”?";detail.text="Remove this region from your saved regions?";
                        action.text="CANCEL";deleteLabel.text="CONFIRM";
                        openButton.onClick.RemoveAllListeners();openButton.onClick.AddListener(()=>{
                            confirming=false;name.text=region.name;detail.text=((World.RegionSize)region.size)+" region  ·  "+district;action.text="OPEN";deleteLabel.text="DELETE";
                            openButton.onClick.RemoveAllListeners();openButton.onClick.AddListener(()=>open(region));
                        });
                        return;
                    }
                    try{World.SavedRegionStore.Delete(region);ShowSavedRegions(World.SavedRegionStore.ListRegions(),open);}
                    catch(System.Exception error){Debug.LogException(error);detail.text="Could not delete this region. Please try again.";}
                });
            }
            if(regions.Count==0){bodyLabel.gameObject.SetActive(true);bodyLabel.text="No saved regions yet. Use Save Region while viewing a region.";}
            Canvas.ForceUpdateCanvases();bodyScroll.verticalNormalizedPosition=1;
            var first=savedRegionList.GetComponentInChildren<Button>();if(first&&EventSystem.current)EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        Text SavedText(Transform parent,string name,string value,int size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var text=go.GetComponent<Text>();text.text=value;text.font=bodyLabel.font;text.fontSize=size;text.color=new Color(.92f,.91f,.82f);text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;return text;
        }

        public void ShowCreateRegion(string header, Texture iconTexture, Rect iconUv, string footer)
        {
            Show(header, string.Empty, iconTexture, iconUv, footer);
            SetCreateRegionNavigationVisible(true);
            if (bodyLabel != null) bodyLabel.gameObject.SetActive(false);
            ConfigureGenerationControls();
            ConfigureRegionNameAutogenerate();
            generationSeed.text=(System.Guid.NewGuid().GetHashCode()&int.MaxValue).ToString();
            RefreshTimeSelection();
            SelectCreateRegionSection(0);
            RefreshProgression();
            if (Application.isPlaying && regionNameInput != null)
            {
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(regionNameInput.gameObject);
                regionNameInput.ActivateInputField();
            }
        }

        private void ConfigureRegionNameAutogenerate()
        {
            if (!regionNameInput || regionNameHint) return;

            var field = (RectTransform)regionNameInput.transform;
            var buttonObject = new GameObject("Autogenerate Region Name", typeof(RectTransform), typeof(Image), typeof(Button));
            var buttonRect = (RectTransform)buttonObject.transform;
            buttonRect.SetParent(field.parent, false);
            buttonRect.anchorMin = field.anchorMin;
            buttonRect.anchorMax = field.anchorMax;
            buttonRect.pivot = field.pivot;
            buttonRect.sizeDelta = Vector2.one * field.sizeDelta.y;
            const float gap = 8f;
            field.sizeDelta -= new Vector2(buttonRect.sizeDelta.x + gap, 0);
            buttonRect.anchoredPosition = field.anchoredPosition + new Vector2(field.sizeDelta.x + gap, 0);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(.24f, .20f, .13f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(AutogenerateRegionName);
            regionNameCharacterLimit = regionNameInput.characterLimit;
            ConfigureRegionNameSuffix();
            regionNameInput.onEndEdit.AddListener(_ =>
            {
                if (selectedRegionSuffix.Length > 0) SetRegionName(BaseRegionName);
            });

            var glyph = SavedText(buttonRect, "Refresh", "↻", 28);
            glyph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            glyph.alignment = TextAnchor.MiddleCenter;
            glyph.rectTransform.anchorMin = Vector2.zero;
            glyph.rectTransform.anchorMax = Vector2.one;
            glyph.rectTransform.offsetMin = glyph.rectTransform.offsetMax = Vector2.zero;

            var hint = SavedText(field.parent, "Autogenerate hint", "Autogenerate", 15);
            hint.alignment = TextAnchor.MiddleRight;
            hint.rectTransform.anchorMin = buttonRect.anchorMin;
            hint.rectTransform.anchorMax = buttonRect.anchorMax;
            hint.rectTransform.pivot = buttonRect.pivot;
            hint.rectTransform.sizeDelta = new Vector2(125, 24);
            hint.rectTransform.anchoredPosition = buttonRect.anchoredPosition +
                new Vector2(buttonRect.sizeDelta.x - hint.rectTransform.sizeDelta.x, 24);
            regionNameHint = hint.gameObject;
            regionNameHint.SetActive(false);

            var hover = buttonObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => regionNameHint.SetActive(true));
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => regionNameHint.SetActive(false));
            hover.triggers.Add(enter);
            hover.triggers.Add(exit);
        }

        private void ConfigureRegionNameSuffix()
        {
            if (!regionSizeInput) return;

            var sizeRect = (RectTransform)regionSizeInput.transform;
            var sizeLabel = (RectTransform)sizeRect.parent.Find("Region SizeLabel");
            var suffix = Instantiate(regionSizeInput, sizeRect.parent);
            suffix.name = "Region name suffix";
            suffix.onValueChanged = new Dropdown.DropdownEvent();
            var choices = new System.Collections.Generic.List<string> { "None" };
            foreach (var option in World.PlaceNameGenerator.Suffixes)
                choices.Add(char.ToUpperInvariant(option[0]) + option.Substring(1));
            suffix.ClearOptions();
            suffix.AddOptions(choices);
            suffix.SetValueWithoutNotify(0);
            suffix.onValueChanged.AddListener(index =>
            {
                string name = BaseRegionName;
                selectedRegionSuffix = index == 0 ? string.Empty : choices[index];
                SetRegionName(name);
            });

            var label = SavedText(sizeRect.parent, "Region name suffix label", "NAME SUFFIX", 16);
            var labelRect = label.rectTransform;
            label.alignment = TextAnchor.MiddleLeft;
            labelRect.anchorMin = sizeRect.anchorMin;
            labelRect.anchorMax = sizeRect.anchorMax;
            labelRect.pivot = sizeRect.pivot;
            labelRect.sizeDelta = new Vector2(sizeRect.sizeDelta.x, 30);
            labelRect.anchoredPosition = sizeRect.anchoredPosition + new Vector2(0, 34);

            var rowOffset = new Vector2(0, -(sizeRect.sizeDelta.y + 44));
            sizeRect.anchoredPosition += rowOffset;
            if (sizeLabel) sizeLabel.anchoredPosition += rowOffset;
        }

        private void AutogenerateRegionName()
        {
            regionNames ??= new World.PlaceNameGenerator("RegionNames", regionNameCharacterLimit, randomSuffix: false);
            var name = regionNames.Next(BaseRegionName);
            if (name == null) return;

            SetRegionName(name);
            regionNameInput.Select();
            regionNameInput.ActivateInputField();
            StartCoroutine(PlaceRegionNameCaret());
        }

        private System.Collections.IEnumerator PlaceRegionNameCaret()
        {
            // InputField selects all text when it activates in LateUpdate.
            yield return null;
            if (regionNameInput.isFocused)
                regionNameInput.caretPosition = BaseRegionName.Length;
        }

        private void ChangeEra(int direction)
        {
            SelectEra((selectedEraIndex + direction + CityForgeTimeline.Eras.Length)
                % CityForgeTimeline.Eras.Length);
        }

        public void SelectEra(int index)
        {
            if (index < 0 || index >= CityForgeTimeline.Eras.Length) return;
            selectedEraIndex = index;
            selectedPeriodIndex = 0;
            RefreshTimeSelection();
        }

        private void ChangePeriod(int direction)
        {
            var periods = CityForgeTimeline.Eras[selectedEraIndex].Periods;
            SelectPeriod((selectedPeriodIndex + direction + periods.Length) % periods.Length);
        }

        public void SelectPeriod(int index)
        {
            if (index < 0 || index >= SelectedEra.Periods.Length) return;
            selectedPeriodIndex = index;
            RefreshTimeSelection();
        }

        private void RefreshTimeSelection()
        {
            var era = CityForgeTimeline.Eras[selectedEraIndex];
            var period = era.Periods[selectedPeriodIndex];
            if (eraValue != null) eraValue.text = era.Name;
            if (periodValue != null) periodValue.text = period.Name;
            if (periodYears != null)
                periodYears.text = period.FirstYear + "–" + period.LastYear;
        }

        public void SelectCreateRegionSection(int index)
        {
            if (navigationBackgrounds == null || index < 0 || index >= navigationBackgrounds.Length)
                return;
            if(index>0&&!BasicSettingsComplete)return;
            if(index>ResourcesSection || (index==ResourcesSection && !resourcesPanel))return;
            if(index>=2&&generationSeed&&!int.TryParse(generationSeed.text,out _))return;

            selectedNavigationIndex = index;
            if (regionNameHint) regionNameHint.SetActive(false);
            if (createRegionBasicSettings != null)
                createRegionBasicSettings.SetActive(index == 0);
            if (createRegionTerrainIntro != null)
                createRegionTerrainIntro.SetActive(index == 1);
            if(terrainShapePanel)terrainShapePanel.SetActive(index==2);
            if(resourcesPanel)resourcesPanel.SetActive(index==ResourcesSection);
            if(index==2)RefreshTerrainPreview();
            for (var i = 0; i < navigationBackgrounds.Length; i++)
            {
                var image = navigationBackgrounds[i];
                if (image == null) continue;
                image.texture = navigationButtonTexture;
                // Top strip: unselected. Middle strip: illuminated selection.
                image.uvRect = i == selectedNavigationIndex
                    ? PixelUv(0f, 196f, 1070f, 242f, 1070f, 635f)
                    : PixelUv(0f, 0f, 1070f, 192f, 1070f, 635f);
            }
            if (bodyScroll != null) bodyScroll.verticalNormalizedPosition = 1f;
        }

        private void RefreshProgression()
        {
            var nameIsValid = BasicSettingsComplete;
            var canCreate = nameIsValid && (!generationSeed || int.TryParse(generationSeed.text,out _));
            if (terrainNextButton != null)
            {
                terrainNextButton.interactable = canCreate;
                terrainNextButton.GetComponent<CanvasGroup>().alpha = canCreate ? 1f : 0.4f;
            }
            if (!nameIsValid && selectedNavigationIndex > 0)
                SelectCreateRegionSection(0);
            if (nextButton != null)
            {
                nextButton.interactable = nameIsValid;
                var group = nextButton.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = nameIsValid ? 1f : 0.4f;
            }
            if(createMapButton)createMapButton.interactable=canCreate;
            if(terrainContinueButton)terrainContinueButton.interactable=canCreate;
            if (navigationButtons == null) return;
            for (var i = 0; i < navigationButtons.Length; i++)
            {
                var unlocked=i==0||(i==1&&nameIsValid)||((i==2||(i==ResourcesSection&&resourcesPanel))&&canCreate);
                if (navigationButtons[i] == null) continue;
                navigationButtons[i].interactable = unlocked;
                var group = navigationButtons[i].GetComponent<CanvasGroup>();
                if (group != null) group.alpha = unlocked ? 1f : 0.4f;
            }
        }

        private static int CountLetters(string value)
        {
            var count = 0;
            foreach (var character in value)
                if (char.IsLetter(character)) count++;
            return count;
        }

        public void CreatePrototypeRegion()
        {
            if (!Application.isPlaying || !BasicSettingsComplete || (generationSeed && !int.TryParse(generationSeed.text,out _))) return;
            var controller = FindFirstObjectByType<MainMenuController>();
            if (controller != null) controller.CreatePrototypeRegion(RegionName, SelectedSize, ReadCreationSettings());
        }

        Dropdown generationAnimals, generationCamps, generationTreeCoverage;
        int[] districtTreeCoverage;
        Text[] districtCoverageLabels;
        Dropdown TerrainChoice(string name) => createRegionTerrainIntro.transform.Find(name+"Dropdown").GetComponent<Dropdown>();
        public World.RegionCreationSettings ReadCreationSettings()
        {
            return new World.RegionCreationSettings { forestAppearanceVersion=1,treeCoverageVersion=1,treeCoveragePercent=generationTreeCoverage?(generationTreeCoverage.value+1)*25:50,districtTreeCoverage=districtTreeCoverage==null?null:(int[])districtTreeCoverage.Clone(),waterwayVersion=6,nativeCampVersion=generationCamps&&generationCamps.value>0?1:0,nativeCampCount=generationCamps&&generationCamps.value>0?generationCamps.value+1:0,seed=generationSeed&&int.TryParse(generationSeed.text,out var seed)?seed:World.PrototypeRegionData.DefaultSeed,
                resourceAbundance=resourceAbundance?(World.ResourceAbundance)resourceAbundance.value:World.ResourceAbundance.Normal,
                resourceDistribution=resourceDistribution?(World.ResourceDistribution)resourceDistribution.value:World.ResourceDistribution.DistinctDistricts,
                resourceDiscovery=resourceDiscovery?(World.ResourceDiscovery)resourceDiscovery.value:World.ResourceDiscovery.HistoricalProgressive,
                terrainShape=terrainShapeChoice?TerrainShapes[terrainShapeChoice.value]:World.RegionTerrainShape.Land,peninsulaNorthToSouth=true,
                climate=TerrainChoice("Climate").value==3?World.RegionClimate.Tropical:World.RegionClimate.Temperate,
                moisture=TerrainChoice("Climate").value==3?World.RegionMoisture.Balanced:(World.RegionMoisture)TerrainChoice("Climate").value,vegetation=(World.RegionVegetation)TerrainChoice("Land Type").value,
                topography=TerrainChoice("Topography").value,rivers=TerrainChoice("Rivers").value,streams=TerrainChoice("Streams").value,
                animals=generationAnimals&&generationAnimals.value==1,era=SelectedEra.Name,period=SelectedPeriod.Name };
        }
        void ConfigureGenerationControls()
        {
            if(!createRegionTerrainIntro||generationSeed)return;
            var parent=createRegionTerrainIntro.transform;
            TerrainChoice("Climate").ClearOptions();TerrainChoice("Climate").AddOptions(new System.Collections.Generic.List<string>{"Temperate — balanced", "Temperate — dry", "Temperate — wet", "Tropical"});TerrainChoice("Climate").interactable=true;
            TerrainChoice("Land Type").ClearOptions();TerrainChoice("Land Type").AddOptions(new System.Collections.Generic.List<string>{"Meadow and woodland", "Open meadow", "Woodland"});TerrainChoice("Land Type").interactable=true;
            parent.Find("ClimateNote").GetComponent<Text>().text="Moisture influences woodland and riverside vegetation.";
            parent.Find("Land TypeLabel").GetComponent<Text>().text="TREE COVERAGE";TerrainChoice("Land Type").value=0;TerrainChoice("Land Type").gameObject.SetActive(false);
            parent.Find("CoastalNote").GetComponent<Text>().text="One inland region, 64 connected districts.";
            parent.Find("SectionTitle").GetComponent<Text>().text="ENVIRONMENT";
            parent.Find("SectionTitle").GetComponent<RectTransform>().sizeDelta=new Vector2(240,38);
            parent.Find("SectionDescription").GetComponent<Text>().text="Climate, waterways and relief.";
            parent.Find("SectionDescription").GetComponent<RectTransform>().anchoredPosition=new Vector2(250,0);
            parent.Find("SectionDescription").GetComponent<RectTransform>().sizeDelta=new Vector2(525,38);
            var label=Instantiate(parent.Find("StreamsLabel").gameObject,parent);label.name="SeedLabel";label.GetComponent<Text>().text="MAP SEED";
            label.GetComponent<RectTransform>().anchoredPosition=new Vector2(425,-262);
            generationSeed=Instantiate(regionNameInput,parent);generationSeed.name="MapSeedInput";
            var rect=generationSeed.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(425,-296);rect.sizeDelta=new Vector2(350,48);
            generationSeed.onValueChanged=new InputField.OnChangeEvent();generationSeed.onEndEdit=new InputField.EndEditEvent();
            generationSeed.characterValidation=InputField.CharacterValidation.Integer;generationSeed.characterLimit=11;generationSeed.text=World.PrototypeRegionData.DefaultSeed.ToString();
            generationSeed.onValueChanged.AddListener(_=>{RefreshProgression();RefreshTerrainPreview();});
            var animalLabel=Instantiate(parent.Find("StreamsLabel").gameObject,parent);animalLabel.name="AnimalsLabel";animalLabel.GetComponent<Text>().text="ANIMALS";
            animalLabel.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-372);
            generationAnimals=Instantiate(TerrainChoice("Streams"),parent);generationAnimals.name="AnimalsDropdown";
            generationAnimals.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-406);
            generationAnimals.onValueChanged=new Dropdown.DropdownEvent();generationAnimals.ClearOptions();
            generationAnimals.AddOptions(new System.Collections.Generic.List<string>{"None","Temperate wildlife"});generationAnimals.value=1;generationAnimals.interactable=true;
            var campLabel=Instantiate(parent.Find("StreamsLabel").gameObject,parent);campLabel.name="IndigenousCampsLabel";campLabel.GetComponent<Text>().text="INDIGENOUS CAMPS";
            campLabel.GetComponent<RectTransform>().anchoredPosition=new Vector2(425,-372);
            generationCamps=Instantiate(TerrainChoice("Streams"),parent);generationCamps.name="IndigenousCampsDropdown";
            generationCamps.GetComponent<RectTransform>().anchoredPosition=new Vector2(425,-406);
            generationCamps.onValueChanged=new Dropdown.DropdownEvent();generationCamps.ClearOptions();
            generationCamps.AddOptions(new System.Collections.Generic.List<string>{"Off","2 Camps","3 Camps"});generationCamps.value=1;generationCamps.interactable=true;
            TerrainChoice("Topography").value=2;
            TerrainChoice("Rivers").value=1;
            var streams=TerrainChoice("Streams");
            streams.options[1].text="A Few Small Streams";
            streams.value=1;
            foreach(var choice in new[]{"Topography","Streams"}){
                parent.Find(choice+"Label").GetComponent<RectTransform>().anchoredPosition=new Vector2(choice=="Streams"?425:0,-154);
                TerrainChoice(choice).GetComponent<RectTransform>().anchoredPosition=new Vector2(choice=="Streams"?425:0,-188);
            }
            parent.Find("Land TypeLabel").GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-262);
            TerrainChoice("Land Type").GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-296);
            var climateNote=parent.Find("ClimateNote").GetComponent<Text>();climateNote.text="Tropical adds light aqua coastal water.";climateNote.rectTransform.sizeDelta=new Vector2(350,18);
            parent.Find("MountainNote").GetComponent<Text>().text="Overall relief; Terrain can add a mountain border.";
            var mountainNote=parent.Find("MountainNote").GetComponent<RectTransform>();mountainNote.anchoredPosition=new Vector2(0,-242);mountainNote.sizeDelta=new Vector2(350,18);
            var coverageNote=parent.Find("CoastalNote").GetComponent<Text>();
            coverageNote.rectTransform.anchoredPosition=new Vector2(0,-346);
            coverageNote.rectTransform.sizeDelta=new Vector2(350,24);
            coverageNote.text="Sets all districts. Adjust individual districts in Terrain.";
            parent.GetComponent<LayoutElement>().preferredHeight=520;
            terrainNextButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(615,-472);
            terrainNextButton.GetComponentInChildren<Text>().text="NEXT";
            ConfigureTerrainStep();
        }

        void ConfigureTerrainStep()
        {
            if(terrainShapePanel)return;
            for(int i=0;i<navigationButtons.Length;i++)if(navigationButtons[i])foreach(var text in navigationButtons[i].GetComponentsInChildren<Text>(true)){
                if(i==1&&(text.text=="TERRAIN"||text.text=="ENVIRONMENT"))text.text="ENVIRONMENT";else if(i==2&&(text.text=="NATURAL RESOURCES"||text.text=="TERRAIN"))text.text="TERRAIN";
                else if(text.text=="Land, water, climate")text.text="Climate and ecology";else if(text.text=="What the land provides")text.text="Region shape";
            }
            terrainShapePanel=new GameObject("CreateRegionTerrainShape",typeof(RectTransform),typeof(LayoutElement));terrainShapePanel.transform.SetParent(createRegionTerrainIntro.transform.parent,false);terrainShapePanel.GetComponent<LayoutElement>().preferredHeight=900;
            Text CopyText(string template,string name,string value,Vector2 position,Vector2 size){var t=Instantiate(createRegionTerrainIntro.transform.Find(template).GetComponent<Text>(),terrainShapePanel.transform);t.name=name;t.text=value;t.rectTransform.anchoredPosition=position;t.rectTransform.sizeDelta=size;return t;}
            CopyText("SectionTitle","SectionTitle","TERRAIN",Vector2.zero,new Vector2(300,38));
            CopyText("SectionDescription","SectionDescription","Choose the outline of your region. Each map contains 64 connected districts.",new Vector2(0,-42),new Vector2(775,42));
            CopyText("StreamsLabel","ShapeLabel","REGION SHAPE",new Vector2(0,-100),new Vector2(350,30));
            terrainShapeChoice=Instantiate(TerrainChoice("Land Type"),terrainShapePanel.transform);terrainShapeChoice.name="RegionShapeDropdown";terrainShapeChoice.gameObject.SetActive(true);terrainShapeChoice.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-134);terrainShapeChoice.onValueChanged=new Dropdown.DropdownEvent();terrainShapeChoice.ClearOptions();terrainShapeChoice.AddOptions(new System.Collections.Generic.List<string>{"Peninsula","Land","Coast","Mountain Border","Pangea","Archipelago","River & Bay"});terrainShapeChoice.value=1;terrainShapeChoice.onValueChanged.AddListener(_=>RefreshTerrainPreview());
            terrainShapeDescription=CopyText("SectionDescription","ShapeDescription","",new Vector2(0,-202),new Vector2(350,140));terrainShapeDescription.alignment=TextAnchor.UpperLeft;terrainShapeDescription.fontSize=19;
            var preview=new GameObject("ShapePreview",typeof(RectTransform),typeof(RawImage));preview.transform.SetParent(terrainShapePanel.transform,false);terrainShapePreview=preview.GetComponent<RawImage>();var rect=preview.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(425,-100);rect.sizeDelta=new Vector2(280,280);terrainShapePreview.raycastTarget=false;
            terrainContinueButton=Instantiate(terrainNextButton,terrainShapePanel.transform);
            terrainContinueButton.name="NextButton";
            terrainContinueButton.onClick=new Button.ButtonClickedEvent();
            terrainContinueButton.onClick.AddListener(() => SelectCreateRegionSection(ResourcesSection));
            ConfigureTreeCoverage();
            terrainContinueButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-820);
            terrainShapePanel.SetActive(false);
            ConfigureResourcesStep();
            RefreshTerrainPreview();
        }

        void ConfigureResourcesStep()
        {
            var terrainNav = navigationButtons[2];
            var resourceNav = Instantiate(terrainNav, terrainNav.transform.parent);
            resourceNav.name = "NavResources";
            resourceNav.transform.SetSiblingIndex(terrainNav.transform.GetSiblingIndex() + 1);
            resourceNav.transform.Find("Title").GetComponent<Text>().text = "RESOURCES";
            resourceNav.transform.Find("Description").GetComponent<Text>().text = "Natural wealth and deposits";
            var icon = resourceNav.transform.Find("Icon").GetComponent<RawImage>();
            icon.enabled = false;
            var diamond = SavedText(icon.transform, "Resource icon", "◇", 38);
            diamond.alignment = TextAnchor.MiddleCenter;
            diamond.rectTransform.anchorMin = Vector2.zero;
            diamond.rectTransform.anchorMax = Vector2.one;
            diamond.rectTransform.offsetMin = diamond.rectTransform.offsetMax = Vector2.zero;
            resourceNav.onClick = new Button.ButtonClickedEvent();
            resourceNav.onClick.AddListener(() => SelectCreateRegionSection(ResourcesSection));

            var buttons = new System.Collections.Generic.List<Button>(navigationButtons);
            var backgrounds = new System.Collections.Generic.List<RawImage>(navigationBackgrounds);
            buttons.Insert(ResourcesSection, resourceNav);
            backgrounds.Insert(ResourcesSection, resourceNav.GetComponent<RawImage>());
            navigationButtons = buttons.ToArray();
            navigationBackgrounds = backgrounds.ToArray();

            resourcesPanel = new GameObject("CreateRegionResources", typeof(RectTransform), typeof(LayoutElement));
            var parent = resourcesPanel.transform;
            parent.SetParent(terrainShapePanel.transform.parent, false);
            resourcesPanel.GetComponent<LayoutElement>().preferredHeight = 570;

            void Label(string name, string value, float y, int fontSize, float height)
            {
                var label = SavedText(parent, name, value, fontSize);
                var rect = label.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(0, y);
                rect.sizeDelta = new Vector2(680, height);
            }

            Dropdown Choice(string name, string title, float y, string help, params string[] options)
            {
                Label(name + "Label", title, y, 20, 28);
                var choice = Instantiate(terrainShapeChoice, parent);
                choice.name = name + "Dropdown";
                choice.onValueChanged = new Dropdown.DropdownEvent();
                choice.ClearOptions();
                choice.AddOptions(new System.Collections.Generic.List<string>(options));
                choice.SetValueWithoutNotify(0);
                var rect = (RectTransform)choice.transform;
                rect.anchoredPosition = new Vector2(0, y - 30);
                rect.sizeDelta = new Vector2(350, 48);
                Label(name + "Help", help, y - 83, 16, 38);
                return choice;
            }

            Label("SectionTitle", "RESOURCES", 0, 28, 38);
            Label("SectionDescription", "Natural wealth, resource distribution and discovery.", -42, 20, 34);
            resourceAbundance = Choice("ResourceAbundance", "RESOURCE ABUNDANCE", -94,
                "Changes how rich resources are in suitable terrain.", "Normal", "Sparse", "Abundant", "Very Abundant");
            resourceDistribution = Choice("ResourceDistribution", "RESOURCE DISTRIBUTION", -218,
                "Controls how strongly districts differ in their natural advantages.", "Distinct Districts", "Balanced", "Highly Specialized");
            resourceDiscovery = Choice("ResourceDiscovery", "RESOURCE DISCOVERY", -342,
                "Start with hidden deposits or reveal them from the beginning.", "Historical — Progressive", "Revealed at Start");
            createMapButton = Instantiate(terrainNextButton, parent);
            createMapButton.name = "CreateMapButton";
            createMapButton.GetComponentInChildren<Text>().text = "CREATE MAP";
            createMapButton.onClick = new Button.ButtonClickedEvent();
            createMapButton.onClick.AddListener(CreatePrototypeRegion);
            createMapButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -494);
            resourcesPanel.SetActive(false);
        }
        void ConfigureTreeCoverage()
        {
            var parent=terrainShapePanel.transform;
            Text Label(string name,string value,Vector2 position,Vector2 size,int fontSize){
                var t=SavedText(parent,name,value,fontSize);var r=t.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=size;t.alignment=TextAnchor.UpperLeft;return t;
            }
            Label("TreeCoverageTitle","TREE COVERAGE",new Vector2(0,-430),new Vector2(350,32),24);
            Label("TreeCoverageHelp","Click a district to cycle 25%, 50%, or 75%. Use Tree Coverage in Environment to set all districts.\n\nWater, steep slopes and camp clearings stay open.",new Vector2(0,-480),new Vector2(350,180),19);
            var environment = createRegionTerrainIntro.transform;
            generationTreeCoverage = Instantiate(TerrainChoice("Land Type"), environment);
            generationTreeCoverage.name = "TreeCoverageDropdown";
            generationTreeCoverage.gameObject.SetActive(true);
            var coverageRect = (RectTransform)generationTreeCoverage.transform;
            coverageRect.anchoredPosition = new Vector2(0, -296);
            coverageRect.sizeDelta = new Vector2(210, 48);
            generationTreeCoverage.onValueChanged=new Dropdown.DropdownEvent();generationTreeCoverage.ClearOptions();generationTreeCoverage.AddOptions(new System.Collections.Generic.List<string>{"25%","50%","75%"});generationTreeCoverage.value=1;
            districtTreeCoverage=new int[64];districtCoverageLabels=new Text[64];
            Label("TreeCoverageNorth","NORTH",new Vector2(425,-434),new Vector2(350,24),16);
            for(int z=7;z>=0;z--)for(int x=0;x<8;x++){
                int index=z*8+x;districtTreeCoverage[index]=50;
                var go=new GameObject("DistrictCoverage"+index,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
                var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(425+x*44,-464-(7-z)*44);r.sizeDelta=new Vector2(42,42);
                var button=go.GetComponent<Button>();button.targetGraphic=go.GetComponent<Image>();
                var text=SavedText(go.transform,"Coverage","",16);text.alignment=TextAnchor.MiddleCenter;text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;districtCoverageLabels[index]=text;
                button.onClick.AddListener(()=>{districtTreeCoverage[index]=districtTreeCoverage[index]==75?25:districtTreeCoverage[index]+25;RefreshCoverageCell(index);});RefreshCoverageCell(index);
            }
            Label("TreeCoverageSouth","SOUTH · 8 × 8 DISTRICTS",new Vector2(425,-820),new Vector2(350,24),16);
            void ApplyAll(int value){for(int i=0;i<districtTreeCoverage.Length;i++){districtTreeCoverage[i]=(value+1)*25;RefreshCoverageCell(i);}}
            generationTreeCoverage.onValueChanged.AddListener(ApplyAll);
            var apply=Instantiate(terrainNextButton,environment);apply.name="ApplyTreeCoverage";apply.onClick=new Button.ButtonClickedEvent();apply.onClick.AddListener(()=>ApplyAll(generationTreeCoverage.value));
            var applyRect=apply.GetComponent<RectTransform>();applyRect.anchoredPosition=new Vector2(225,-296);applyRect.sizeDelta=new Vector2(125,48);apply.GetComponentInChildren<Text>().text="APPLY";
        }
        void RefreshCoverageCell(int index)
        {
            int coverage=districtTreeCoverage[index];var t=districtCoverageLabels[index];t.text=coverage+"%";
            t.transform.parent.GetComponent<Image>().color=Color.Lerp(new Color(.34f,.39f,.25f),new Color(.12f,.22f,.13f),(coverage-25)/50f);
        }
        void RefreshTerrainPreview()
        {
            if(!terrainShapeChoice||!terrainShapePreview)return;
            var recipe=ReadCreationSettings();var layout=new World.RegionTerrainLayout(recipe);
            terrainShapeDescription.text=recipe.terrainShape switch{
                World.RegionTerrainShape.Peninsula=>"Land extending south from a northern mainland, with water on the other three sides.",
                World.RegionTerrainShape.Land=>"Inland terrain fills the region. Your rivers and relief shape the landscape.",
                World.RegionTerrainShape.Coast=>"Open ocean along one side, with a winding coast and a broad mainland.",
                World.RegionTerrainShape.MountainBorder=>"Mountains along one edge, with gentler land across the rest of the region.",
                World.RegionTerrainShape.OneIsland=>"One island completely surrounded by ocean, with generous water around it.",
                World.RegionTerrainShape.RiverAndBay=>"A broad winding river feeds a deep inland bay with islands and gently sloped riverbanks.",
                _=>"A group of small islands separated by open water."};
            const int n=160;if(!terrainShapeTexture){terrainShapeTexture=new Texture2D(n,n,TextureFormat.RGBA32,false);terrainShapeTexture.name="Region shape preview";terrainShapePreview.texture=terrainShapeTexture;}
            var colors=new Color[n*n];for(int z=0;z<n;z++)for(int x=0;x<n;x++){var p=new Vector2(x/(float)(n-1),z/(float)(n-1));float d=layout.LandDistance(p);var color=d<0?new Color(.12f,.31f,.40f):new Color(.43f,.47f,.285f);
                if(layout.HasOcean&&Mathf.Abs(d)<.007f)color=new Color(.70f,.62f,.43f);
                if(recipe.terrainShape==World.RegionTerrainShape.MountainBorder&&(recipe.version>=8?p.y>(recipe.version>=9?.75f:.70f):World.RegionTerrainLayout.Orient(p,recipe.seed).x>.75f))color=Color.Lerp(color,new Color(.68f,.60f,.48f),.8f);
                if(layout.RiverBay != null && d >= 0 &&
                    Mathf.Abs(p.x-layout.RiverBay.RiverCenter(p.y)) < layout.RiverBay.RiverHalfWidth)
                    color = new Color(.12f,.31f,.40f);
                if(x%20==0||z%20==0)color*=.85f;colors[z*n+x]=color;}
            terrainShapeTexture.SetPixels(colors);terrainShapeTexture.Apply();
        }
        void OnDestroy(){if(terrainShapeTexture)Destroy(terrainShapeTexture);}

        private void SetCreateRegionNavigationVisible(bool visible)
        {
            if (createRegionNavigation != null) createRegionNavigation.SetActive(visible);
            if (bodyViewport != null)
            {
                var offset = bodyViewport.offsetMin;
                offset.x = visible ? NavigationWidth : 0f;
                bodyViewport.offsetMin = offset;
            }
        }

        private static Rect PixelUv(float x, float y, float width, float height,
            float textureWidth, float textureHeight)
        {
            return new Rect(x / textureWidth, 1f - (y + height) / textureHeight,
                width / textureWidth, height / textureHeight);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}

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
        GameObject terrainShapePanel;Dropdown terrainShapeChoice;Button createMapButton;Text terrainShapeDescription;RawImage terrainShapePreview;Texture2D terrainShapeTexture;
        static readonly World.RegionTerrainShape[] TerrainShapes={World.RegionTerrainShape.Peninsula,World.RegionTerrainShape.Land,World.RegionTerrainShape.Coast,World.RegionTerrainShape.MountainBorder,World.RegionTerrainShape.OneIsland,World.RegionTerrainShape.SmallIslands};
        private GameObject savedRegionList;
        private const float NavigationWidth = 270f;
        private int selectedNavigationIndex;
        private int selectedEraIndex;
        private int selectedPeriodIndex;

        public CityForge.World.RegionSize SelectedSize => regionSizeInput != null ? (CityForge.World.RegionSize)regionSizeInput.value : CityForge.World.RegionSize.Medium;
        public void SetRegionSize(CityForge.World.RegionSize size) { if (regionSizeInput != null) regionSizeInput.value = (int)size; }

        public string RegionName => regionNameInput != null ? regionNameInput.text : string.Empty;
        public CityForgeTimeline.Era SelectedEra => CityForgeTimeline.Eras[selectedEraIndex];
        public CityForgeTimeline.Period SelectedPeriod => SelectedEra.Periods[selectedPeriodIndex];
        public bool BasicSettingsComplete => CountLetters(RegionName) >= 2;

        public void SetRegionName(string name)
        {
            if (regionNameInput != null) regionNameInput.text = name ?? string.Empty;
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
                    var selectedIndex = index;
                    if (navigationButtons[index] != null)
                        navigationButtons[index].onClick.AddListener(() => SelectCreateRegionSection(selectedIndex));
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
            if(index>2)return;
            if(index==2&&generationSeed&&!int.TryParse(generationSeed.text,out _))return;

            selectedNavigationIndex = index;
            if (createRegionBasicSettings != null)
                createRegionBasicSettings.SetActive(index == 0);
            if (createRegionTerrainIntro != null)
                createRegionTerrainIntro.SetActive(index == 1);
            if(terrainShapePanel)terrainShapePanel.SetActive(index==2);
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
            if (navigationButtons == null) return;
            for (var i = 0; i < navigationButtons.Length; i++)
            {
                var unlocked=i==0||(i==1&&nameIsValid)||(i==2&&canCreate);
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

        Dropdown generationAnimals, generationCamps;
        Dropdown TerrainChoice(string name) => createRegionTerrainIntro.transform.Find(name+"Dropdown").GetComponent<Dropdown>();
        public World.RegionCreationSettings ReadCreationSettings()
        {
            return new World.RegionCreationSettings { waterwayVersion=5,nativeCampVersion=generationCamps&&generationCamps.value==1?1:0,seed=generationSeed&&int.TryParse(generationSeed.text,out var seed)?seed:World.PrototypeRegionData.DefaultSeed,
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
            parent.Find("Land TypeLabel").GetComponent<Text>().text="VEGETATION";
            parent.Find("CoastalNote").GetComponent<Text>().text="One inland region, 64 connected districts.";
            parent.Find("SectionTitle").GetComponent<Text>().text="ENVIRONMENT";
            parent.Find("SectionTitle").GetComponent<RectTransform>().sizeDelta=new Vector2(240,38);
            parent.Find("SectionDescription").GetComponent<Text>().text="Climate, vegetation, waterways and relief.";
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
            generationCamps.AddOptions(new System.Collections.Generic.List<string>{"Off","On — 2–3 camps"});generationCamps.value=0;generationCamps.interactable=true;
            foreach(var choice in new[]{"Topography","Streams"}){
                parent.Find(choice+"Label").GetComponent<RectTransform>().anchoredPosition=new Vector2(choice=="Streams"?425:0,-154);
                TerrainChoice(choice).GetComponent<RectTransform>().anchoredPosition=new Vector2(choice=="Streams"?425:0,-188);
            }
            parent.Find("Land TypeLabel").GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-262);
            TerrainChoice("Land Type").GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-296);
            var climateNote=parent.Find("ClimateNote").GetComponent<Text>();climateNote.text="Tropical adds light aqua coastal water.";climateNote.rectTransform.sizeDelta=new Vector2(350,18);
            parent.Find("MountainNote").GetComponent<Text>().text="Overall relief; Terrain can add a mountain border.";
            var mountainNote=parent.Find("MountainNote").GetComponent<RectTransform>();mountainNote.anchoredPosition=new Vector2(0,-242);mountainNote.sizeDelta=new Vector2(350,18);
            var coastalNote=parent.Find("CoastalNote").GetComponent<RectTransform>();coastalNote.anchoredPosition=new Vector2(0,-350);coastalNote.sizeDelta=new Vector2(350,18);parent.Find("CoastalNote").GetComponent<Text>().text="Choose the region’s shape in Terrain.";
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
            terrainShapePanel=new GameObject("CreateRegionTerrainShape",typeof(RectTransform),typeof(LayoutElement));terrainShapePanel.transform.SetParent(createRegionTerrainIntro.transform.parent,false);terrainShapePanel.GetComponent<LayoutElement>().preferredHeight=456;
            Text CopyText(string template,string name,string value,Vector2 position,Vector2 size){var t=Instantiate(createRegionTerrainIntro.transform.Find(template).GetComponent<Text>(),terrainShapePanel.transform);t.name=name;t.text=value;t.rectTransform.anchoredPosition=position;t.rectTransform.sizeDelta=size;return t;}
            CopyText("SectionTitle","SectionTitle","TERRAIN",Vector2.zero,new Vector2(300,38));
            CopyText("SectionDescription","SectionDescription","Choose the outline of your region. Each map contains 64 connected districts.",new Vector2(0,-42),new Vector2(775,42));
            CopyText("StreamsLabel","ShapeLabel","REGION SHAPE",new Vector2(0,-100),new Vector2(350,30));
            terrainShapeChoice=Instantiate(TerrainChoice("Land Type"),terrainShapePanel.transform);terrainShapeChoice.name="RegionShapeDropdown";terrainShapeChoice.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-134);terrainShapeChoice.onValueChanged=new Dropdown.DropdownEvent();terrainShapeChoice.ClearOptions();terrainShapeChoice.AddOptions(new System.Collections.Generic.List<string>{"Peninsula","Land","Coast","Mountain Border","Pangea","Archipelago"});terrainShapeChoice.value=1;terrainShapeChoice.onValueChanged.AddListener(_=>RefreshTerrainPreview());
            terrainShapeDescription=CopyText("SectionDescription","ShapeDescription","",new Vector2(0,-202),new Vector2(350,140));terrainShapeDescription.alignment=TextAnchor.UpperLeft;terrainShapeDescription.fontSize=19;
            var preview=new GameObject("ShapePreview",typeof(RectTransform),typeof(RawImage));preview.transform.SetParent(terrainShapePanel.transform,false);terrainShapePreview=preview.GetComponent<RawImage>();var rect=preview.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(425,-100);rect.sizeDelta=new Vector2(280,280);terrainShapePreview.raycastTarget=false;
            createMapButton=Instantiate(terrainNextButton,terrainShapePanel.transform);createMapButton.name="CreateMapButton";createMapButton.GetComponentInChildren<Text>().text="CREATE MAP";createMapButton.onClick=new Button.ButtonClickedEvent();createMapButton.onClick.AddListener(CreatePrototypeRegion);terrainShapePanel.SetActive(false);RefreshTerrainPreview();
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
                _=>"A group of small islands separated by open water."};
            const int n=160;if(!terrainShapeTexture){terrainShapeTexture=new Texture2D(n,n,TextureFormat.RGBA32,false);terrainShapeTexture.name="Region shape preview";terrainShapePreview.texture=terrainShapeTexture;}
            var colors=new Color[n*n];for(int z=0;z<n;z++)for(int x=0;x<n;x++){var p=new Vector2(x/(float)(n-1),z/(float)(n-1));float d=layout.LandDistance(p);var color=d<0?new Color(.12f,.31f,.40f):new Color(.43f,.47f,.285f);
                if(layout.HasOcean&&Mathf.Abs(d)<.007f)color=new Color(.70f,.62f,.43f);
                if(recipe.terrainShape==World.RegionTerrainShape.MountainBorder&&(recipe.version>=8?p.y>(recipe.version>=9?.75f:.70f):World.RegionTerrainLayout.Orient(p,recipe.seed).x>.75f))color=Color.Lerp(color,new Color(.68f,.60f,.48f),.8f);
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

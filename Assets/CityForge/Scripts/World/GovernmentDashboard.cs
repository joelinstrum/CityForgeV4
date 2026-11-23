using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CityForge.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private Texture2D governmentDashboardArt,governmentIcons,governmentIllustrations;
        private Font governmentFont;
        private Material governmentArtMaterial;
        private RectTransform governmentContent,governmentPage;
        private Text governmentModeBadge,governmentGlance,governmentEventsText;
        private string governmentSection;
        private Button governmentPoliciesTab,governmentEventsTab,governmentPlayerMode,governmentDeveloperMode;
        private readonly Dictionary<string,Button> governmentNav=new();
        private static readonly Color GovernmentCream=new(1,.91f,.72f);
        private static readonly Color GovernmentMuted=new(.73f,.77f,.73f);

        private void LoadGovernmentArtwork()
        {
            // Load with the district HUD, never as a synchronous click-time import.
            governmentDashboardArt=Resources.Load<Texture2D>("UI/Gameplay/Government/Dashboard");
            governmentIcons=Resources.Load<Texture2D>("UI/Gameplay/Government/Icons");
            governmentIllustrations=Resources.Load<Texture2D>("UI/Gameplay/Government/Illustrations");
            if(!governmentFont){governmentFont=Font.CreateDynamicFontFromOSFont(new[]{"Georgia","Times New Roman"},24);if(governmentFont)Own(governmentFont);}
        }
        private Text GovernmentText(Transform parent,string text,Vector2 at,Vector2 size,int points,TextAnchor align=TextAnchor.MiddleLeft,bool serif=true)
        {
            var label=FounderText(parent,text,at,size,points);label.font=serif&&governmentFont?governmentFont:lotFont;
            label.alignment=align;label.color=GovernmentCream;label.horizontalOverflow=HorizontalWrapMode.Wrap;
            label.verticalOverflow=VerticalWrapMode.Truncate;return label;
        }
        private RectTransform GovernmentPanel(Transform parent,string name,Vector2 at,Vector2 size,bool outer=false)
        {
            var rect=FounderRect(parent,name,at,size);var image=rect.gameObject.AddComponent<Image>();
            image.sprite=outer?ZoneModalArtwork.Panel:ZoneModalArtwork.Section;image.type=Image.Type.Sliced;
            var border=rect.gameObject.AddComponent<Outline>();border.effectColor=new Color(.63f,.43f,.16f,.6f);border.effectDistance=new Vector2(1,-1);
            return rect;
        }
        private Button GovernmentButton(Transform parent,string text,Vector2 at,Vector2 size,UnityEngine.Events.UnityAction action,bool selected=false)
        {
            var rect=FounderRect(parent,text,at,size);var image=rect.gameObject.AddComponent<Image>();image.sprite=selected?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;image.type=Image.Type.Sliced;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.highlightedColor=new Color(1,.92f,.73f);colors.pressedColor=new Color(.77f,.64f,.38f);colors.disabledColor=Color.white;button.colors=colors;
            GovernmentText(rect,text,Vector2.zero,size-new Vector2(16,4),21,TextAnchor.MiddleCenter);
            button.onClick.AddListener(action);return button;
        }
        private void GovernmentArt(Transform parent,string name,Texture2D atlas,Rect crop,Vector2 sourceSize,Vector2 at,Vector2 box,bool matte=false)
        {
            if(!atlas)return;float fit=Mathf.Min(box.x/crop.width,box.y/crop.height);
            var art=TownArtwork(parent,name,atlas,new Rect(0,0,atlas.width,atlas.height),at,crop.size*fit);
            art.uvRect=new Rect(crop.x/sourceSize.x,1-(crop.y+crop.height)/sourceSize.y,crop.width/sourceSize.x,crop.height/sourceSize.y);
            if(matte){if(!governmentArtMaterial){var shader=Resources.Load<Shader>("UI/TownButtonArtwork");if(shader)governmentArtMaterial=Own(new Material(shader));}if(governmentArtMaterial)art.material=governmentArtMaterial;}
        }
        private void BuildGovernmentDashboard()
        {
            var panel=GovernmentPanel(gameplayToolsPanel,"Government & Policies",Vector2.zero,new Vector2(1600,900),true);
            gameplayToolsPanel.gameObject.AddComponent<GovernmentModalFit>().Initialize(panel);
            GovernmentArt(panel,"Harbor panorama",governmentDashboardArt,new Rect(854,34,725,111),new Vector2(1712,919),new Vector2(355,374),new Vector2(720,110));
            GovernmentArt(panel,"Government crest",governmentIcons,new Rect(40,18,300,255),new Vector2(2172,724),new Vector2(-717,374),new Vector2(100,94),true);
            GovernmentText(panel,"Government & Policies",new Vector2(-270,390),new Vector2(730,64),42);
            GovernmentText(panel,"Manage your settlement’s policies, growth, and priorities.",new Vector2(-235,341),new Vector2(800,34),21);
            GovernmentButton(panel,"×",new Vector2(750,397),new Vector2(50,50),CloseGameplayTools);
            var badge=GovernmentPanel(panel,"Current game mode",new Vector2(555,332),new Vector2(332,30));
            governmentModeBadge=GovernmentText(badge,"",Vector2.zero,new Vector2(312,28),18,TextAnchor.MiddleCenter);
            var rule=FounderRect(panel,"Header rule",new Vector2(0,307),new Vector2(1536,2));rule.gameObject.AddComponent<Image>().color=new Color(.7f,.5f,.22f);
            var sections=new[]{"Overview","Game Mode","Treasury","Taxes","Immigration","Labor & Rights","Trade","Public Services"};
            var crops=new[]{new Rect(365,65,213,190),new Rect(40,18,300,255),new Rect(605,25,240,240),new Rect(875,25,205,240),new Rect(1110,35,250,220),new Rect(1400,20,225,240),new Rect(1660,15,250,250),new Rect(1940,25,230,230)};
            for(int i=0;i<sections.Length;i++)
            {
                string section=sections[i];var button=GovernmentButton(panel,section,new Vector2(-628,260-i*68),new Vector2(278,59),()=>ShowGovernmentPage(section));
                button.GetComponentInChildren<Text>().rectTransform.anchoredPosition=new Vector2(25,0);button.GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(214,50);
                GovernmentArt(button.transform,section+" icon",governmentIcons,crops[i],new Vector2(2172,724),new Vector2(-105,0),new Vector2(43,43),true);
                governmentNav.Add(section,button);
            }
            var vertical=FounderRect(panel,"Navigation rule",new Vector2(-470,-30),new Vector2(1,650));vertical.gameObject.AddComponent<Image>().color=new Color(.6f,.46f,.24f,.6f);
            governmentContent=FounderRect(panel,"Policy content",new Vector2(145,-30),new Vector2(1190,650));
            governmentPoliciesTab=GovernmentButton(panel,"Policies",new Vector2(-135,-406),new Vector2(260,54),()=>ShowGovernmentPage("Overview"),true);
            governmentEventsTab=GovernmentButton(panel,"Events",new Vector2(135,-406),new Vector2(260,54),()=>ShowGovernmentPage("Events"));
            ShowGovernmentPage("Overview");
        }
        private void ShowGovernmentPage(string section)
        {
            if(!governmentContent)return;
            if(governmentPage){governmentPage.gameObject.SetActive(false);Destroy(governmentPage.gameObject);}
            immigrationSummary=immigrationStatus=immigrationToggleLabel=modeStatus=modeToggleLabel=null;
            governmentGlance=governmentEventsText=null;governmentPlayerMode=governmentDeveloperMode=null;governmentSection=section;
            foreach(var pair in governmentNav)pair.Value.image.sprite=pair.Key==section?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
            governmentPoliciesTab.image.sprite=section=="Events"?ZoneModalArtwork.Dark:ZoneModalArtwork.Gold;
            governmentEventsTab.image.sprite=section=="Events"?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
            governmentPage=FounderRect(governmentContent,section,Vector2.zero,new Vector2(1190,650));
            GovernmentText(governmentPage,section,new Vector2(0,292),new Vector2(1160,50),34);
            if(section=="Overview")BuildGovernmentOverview();
            else if(section=="Game Mode")BuildGovernmentMode();
            else if(section=="Immigration")BuildGovernmentImmigration();
            else if(section=="Events")BuildGovernmentEvents();
            else BuildGovernmentPlanned(section);
            RefreshGovernmentDashboard();
        }
        private RectTransform GovernmentCard(string name,string description,Rect crop,Vector2 at,UnityEngine.Events.UnityAction action,string actionText)
        {
            var card=GovernmentPanel(governmentPage,name,at,new Vector2(380,158));
            GovernmentArt(card,name+" illustration",governmentIllustrations,crop,new Vector2(1536,1024),new Vector2(-105,0),new Vector2(158,145));
            GovernmentText(card,name,new Vector2(78,49),new Vector2(208,37),23);
            GovernmentText(card,description,new Vector2(78,0),new Vector2(208,65),17);
            GovernmentButton(card,actionText,new Vector2(78,-54),new Vector2(194,33),action);
            return card;
        }
        private void BuildGovernmentOverview()
        {
            GovernmentText(governmentPage,"Your settlement’s active settings and priorities.",new Vector2(0,249),new Vector2(1160,32),19);
            var immigration=GovernmentCard("Immigration","Control arrivals of new settlers.",new Rect(20,30,380,335),new Vector2(-395,142),()=>SetImmigrationPaused(!ImmigrationPaused),"");
            immigrationToggleLabel=immigration.GetComponentInChildren<Button>().GetComponentInChildren<Text>();
            immigrationStatus=immigration.GetComponentsInChildren<Text>()[1];
            GovernmentCard("Labor & Rights","Labor policies and working conditions.",new Rect(420,40,345,330),new Vector2(0,142),()=>ShowGovernmentPage("Labor & Rights"),"Not yet available");
            GovernmentCard("Taxes","Tax rates and revenue sources.",new Rect(775,25,375,345),new Vector2(395,142),()=>ShowGovernmentPage("Taxes"),"Not yet available");
            GovernmentCard("Treasury","Income, expenses, and reserves.",new Rect(1165,40,365,330),new Vector2(-395,-32),()=>ShowGovernmentPage("Treasury"),"Not yet available");
            GovernmentCard("Trade","Commerce and trading access.",new Rect(5,397,400,355),new Vector2(0,-32),()=>ShowGovernmentPage("Trade"),"Not yet available");
            GovernmentCard("Public Services","Education, health, and safety.",new Rect(410,397,420,355),new Vector2(395,-32),()=>ShowGovernmentPage("Public Services"),"Not yet available");
            var mode=GovernmentPanel(governmentPage,"Play style",new Vector2(-302,-238),new Vector2(568,208));
            GovernmentText(mode,"Game Mode",new Vector2(0,70),new Vector2(520,38),27);
            modeStatus=GovernmentText(mode,"",new Vector2(0,10),new Vector2(520,76),19);
            modeToggleLabel=GovernmentButton(mode,"",new Vector2(0,-66),new Vector2(306,38),()=>SetDeveloperMode(!DeveloperMode),true).GetComponentInChildren<Text>();
            var glance=GovernmentPanel(governmentPage,"At a glance",new Vector2(300,-238),new Vector2(568,208));
            GovernmentText(glance,"At a Glance",new Vector2(0,70),new Vector2(520,38),27);
            governmentGlance=GovernmentText(glance,"",new Vector2(0,-15),new Vector2(520,124),20);
        }
        private void BuildGovernmentMode()
        {
            GovernmentText(governmentPage,"Choose how your settlement grows. Saved with this region.",new Vector2(0,242),new Vector2(1160,40),21);
            var player=GovernmentPanel(governmentPage,"Player Mode",new Vector2(-298,20),new Vector2(565,360));
            GovernmentText(player,"Player Mode",new Vector2(0,123),new Vector2(500,48),33);
            GovernmentText(player,"Build a settlement through immigration and demand.\n\nHomes begin construction when their assigned settlers reach the parcel. Empty zones wait for residents.",new Vector2(0,0),new Vector2(490,168),23);
            governmentPlayerMode=GovernmentButton(player,"Use Player Mode",new Vector2(0,-121),new Vector2(390,49),()=>SetDeveloperMode(false));
            var developer=GovernmentPanel(governmentPage,"Developer Mode",new Vector2(298,20),new Vector2(565,360));
            GovernmentText(developer,"Developer Mode",new Vector2(0,123),new Vector2(500,48),33);
            GovernmentText(developer,"Create and test your town freely.\n\nZoned buildings can grow without waiting for residents. Valid lots and road access are still required.",new Vector2(0,0),new Vector2(490,168),23);
            governmentDeveloperMode=GovernmentButton(developer,"Use Developer Mode",new Vector2(0,-121),new Vector2(390,49),()=>SetDeveloperMode(true));
            modeStatus=GovernmentText(governmentPage,"",new Vector2(0,-231),new Vector2(1150,63),22,TextAnchor.MiddleCenter);
        }
        private void BuildGovernmentImmigration()
        {
            GovernmentText(governmentPage,"Harbor arrivals and housing",new Vector2(0,240),new Vector2(1160,40),22);
            var art=GovernmentPanel(governmentPage,"New settlers",new Vector2(-365,-5),new Vector2(430,410));
            GovernmentArt(art,"Immigrants",governmentIllustrations,new Rect(20,30,380,335),new Vector2(1536,1024),Vector2.zero,new Vector2(406,380));
            immigrationStatus=GovernmentText(governmentPage,"",new Vector2(230,161),new Vector2(650,48),29);
            immigrationSummary=GovernmentText(governmentPage,"",new Vector2(230,31),new Vector2(650,180),22);
            immigrationToggleLabel=GovernmentButton(governmentPage,"",new Vector2(230,-133),new Vector2(370,49),()=>SetImmigrationPaused(!ImmigrationPaused),true).GetComponentInChildren<Text>();
            GovernmentText(governmentPage,"Pausing stops future arrivals. Ships and settlers already ashore continue.\nCommercial refuge remains unhoused until a residential parcel becomes available.",new Vector2(0,-275),new Vector2(1140,90),20,TextAnchor.MiddleCenter);
        }
        private void BuildGovernmentEvents()
        {
            GovernmentText(governmentPage,"Harbor immigration history for this region.",new Vector2(0,242),new Vector2(1160,40),21);
            var viewport=GovernmentPanel(governmentPage,"Event history",new Vector2(0,-30),new Vector2(1160,485));
            viewport.gameObject.AddComponent<RectMask2D>();var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;
            var content=FounderRect(viewport,"Events",Vector2.zero,new Vector2(1100,465));content.anchorMin=content.anchorMax=content.pivot=new Vector2(.5f,1);content.anchoredPosition=new Vector2(0,-18);
            governmentEventsText=GovernmentText(content,"",Vector2.zero,new Vector2(1080,450),23,TextAnchor.UpperLeft);
            governmentEventsText.rectTransform.anchorMin=governmentEventsText.rectTransform.anchorMax=governmentEventsText.rectTransform.pivot=new Vector2(.5f,1);
            scroll.viewport=viewport;scroll.content=content;scroll.movementType=ScrollRect.MovementType.Clamped;
        }
        private void BuildGovernmentPlanned(string section)
        {
            GovernmentText(governmentPage,"Not yet available",new Vector2(0,170),new Vector2(1100,62),32,TextAnchor.MiddleCenter);
            GovernmentText(governmentPage,$"{section} is reserved for a future gameplay system.\nNo policy or budget settings are active in this section yet.",new Vector2(0,50),new Vector2(960,150),25,TextAnchor.MiddleCenter);
            GovernmentButton(governmentPage,"Return to Overview",new Vector2(0,-125),new Vector2(350,50),()=>ShowGovernmentPage("Overview"));
        }
        private void RefreshGovernmentDashboard()
        {
            if(governmentPlayerMode){governmentPlayerMode.image.sprite=DeveloperMode?ZoneModalArtwork.Dark:ZoneModalArtwork.Gold;governmentPlayerMode.interactable=DeveloperMode;governmentPlayerMode.GetComponentInChildren<Text>().text=DeveloperMode?"Use Player Mode":"Player Mode · Active";}
            if(governmentDeveloperMode){governmentDeveloperMode.image.sprite=DeveloperMode?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;governmentDeveloperMode.interactable=!DeveloperMode;governmentDeveloperMode.GetComponentInChildren<Text>().text=DeveloperMode?"Developer Mode · Active":"Use Developer Mode";}
            if(governmentModeBadge)governmentModeBadge.text=DeveloperMode?"Developer Mode":"Player Mode";
            if(modeStatus)modeStatus.text=DeveloperMode?"Mode: Developer\nZoned buildings can grow without resident arrivals.":"Mode: Player\nHomes wait for settlers to reach their assigned parcels.";
            if(modeToggleLabel)modeToggleLabel.text=DeveloperMode?"Switch to Player":"Switch to Developer";
            if(immigrationStatus)immigrationStatus.text=ImmigrationPaused?"Immigration: Paused":"Immigration: Running";
            if(immigrationToggleLabel)immigrationToggleLabel.text=ImmigrationPaused?"Resume immigration":"Pause immigration";
            if(immigrationSummary)
            {
                var rule=RegionEventCatalog.Definitions.First(d=>d.trigger=="galleon-docked");
                immigrationSummary.text=$"Each galleon brings {(long)rule.immigrantsPerArrival*rule.populationPerRepresentative:N0} people, represented by {rule.immigrantsPerArrival} settlers.\n\nUnhoused people: {UnhousedPopulation:N0}\nAvailable residential parcels: {AvailableResidentialParcels:N0}\nReserved residential parcels: {ReservedResidentialParcels:N0}";
            }
            if(governmentGlance)governmentGlance.text=$"Population: {Towns.Sum(t=>t.development?.population??0):N0}\nUnhoused people: {UnhousedPopulation:N0}\nAvailable residential parcels: {AvailableResidentialParcels:N0}\nReserved residential parcels: {ReservedResidentialParcels:N0}";
            if(governmentEventsText)
            {
                var entries=RegionEvents.Take(50).Select(s=>$"Harbor immigration  ·  {s.occurrences:N0} arrivals{(s.cancelled?"  ·  Council removed":"")}\n{s.populationArrived:N0} people arrived  ·  {s.settledPopulation:N0} moved into homes\n");
                governmentEventsText.text=RegionEvents.Count==0?"No harbor arrivals recorded yet.\n\nPlace a Harbor Council and connect your settlement to welcome its first settlers.":string.Join("\n",entries);
                float height=Mathf.Max(450,governmentEventsText.preferredHeight+20);governmentEventsText.rectTransform.sizeDelta=new Vector2(1080,height);((RectTransform)governmentEventsText.transform.parent).sizeDelta=new Vector2(1100,height+36);
            }
        }
    }
}

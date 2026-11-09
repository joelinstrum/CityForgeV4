using System;
using UnityEngine;
using UnityEngine.UI;
using CityForge.World;
namespace CityForge.UI
{
    public enum NatureTool { None, Terrain, Water, Trees, Animals, Sun }
    /// <summary>Icon-only district nature categories. Mutation tools connect through ToolSelected.</summary>
    public sealed class NatureToolbar : MonoBehaviour
    {
        public NatureTool Selected {get;private set;}
        public event Action<NatureTool> ToolSelected;
        PrototypeRegionView view;RectTransform root,sunMenu;Texture2D atlas;
        readonly Button[] categories=new Button[5],seasons=new Button[3],weather=new Button[3];
        readonly Image[] marks=new Image[5],seasonMarks=new Image[3],weatherMarks=new Image[3];
        public NatureToolPopup Popup {get;private set;}
        public ForestRemovalBrush ForestBrush {get;private set;}
        public LakeOutlineTool LakeOutline {get;private set;}
        public RiverRouteTool RiverRoute {get;private set;}
        bool wasDistrict;
        public void Initialize(PrototypeRegionView source,Transform hud)
        {
            view=source;atlas=Resources.Load<Texture2D>("UI/NatureToolbarIcons");
            root=Rect(hud,"NatureToolbar",new Vector2(.5f,0),new Vector2(0,42),new Vector2(500,80));
            for(int i=0;i<5;i++){
                int index=i;categories[i]=Icon(root,((NatureTool)(i+1)).ToString(),i,new Vector2((i-2)*100,0),64,out marks[i]);
                categories[i].onClick.AddListener(()=>Select((NatureTool)(index+1)));
            }
            sunMenu=Rect(root,"SunMenu",new Vector2(.5f,0),new Vector2(0,100),new Vector2(430,74));
            for(int i=0;i<3;i++){
                int index=i;seasons[i]=Icon(sunMenu,"Season"+(RegionSeason)i,5+i,new Vector2((i-2.65f)*68,0),54,out seasonMarks[i]);
                seasons[i].onClick.AddListener(()=>{if(seasons[index].interactable)view.SetSeason((RegionSeason)index);});
                weather[i]=Icon(sunMenu,"Weather"+(DistrictWeather.WeatherKind)i,8+i,new Vector2((i+.65f)*68,0),54,out weatherMarks[i]);
                weather[i].onClick.AddListener(()=>{var district=Weather;if(district&&weather[index].interactable)district.SetWeather((DistrictWeather.WeatherKind)index);});
            }
            var window=Rect(root,"Popup",new Vector2(.5f,0),new Vector2(0,104),new Vector2(580,350));
            window.localScale=Vector3.one*2;
            Popup=window.gameObject.AddComponent<NatureToolPopup>();Popup.Initialize(atlas,()=>Dismiss());
            sunMenu.SetParent(Popup.SunHost,false);sunMenu.anchorMin=sunMenu.anchorMax=sunMenu.pivot=new Vector2(.5f,1);sunMenu.anchoredPosition=Vector2.zero;sunMenu.sizeDelta=new Vector2(406,88);
            for(int i=0;i<3;i++){
                seasons[i].GetComponent<RectTransform>().anchoredPosition=new Vector2((i-1)*106,22);
                weather[i].GetComponent<RectTransform>().anchoredPosition=new Vector2((i-1)*106,22);
            }
            Popup.OptionChanged+=option=>{if(Selected==NatureTool.Sun)for(int i=0;i<3;i++){seasons[i].gameObject.SetActive(option==0);weather[i].gameObject.SetActive(option==1);}};
            Popup.Show(NatureTool.None);sunMenu.gameObject.SetActive(false);ForestBrush=gameObject.AddComponent<ForestRemovalBrush>();ForestBrush.Initialize(view,this,hud);LakeOutline=gameObject.AddComponent<LakeOutlineTool>();LakeOutline.Initialize(view,this,hud);RiverRoute=gameObject.AddComponent<RiverRouteTool>();RiverRoute.Initialize(view,this,hud);gameObject.AddComponent<WaterRemovalTool>().Initialize(view,this);HideLegacy(hud);Refresh();
        }
        DistrictWeather Weather=>view&&view.ApprovedDistrict?view.ApprovedDistrict.GetComponent<DistrictWeather>():null;
        static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        Button Icon(Transform parent,string name,int tile,Vector2 position,float size,out Image mark)
        {
            var rect=Rect(parent,name,new Vector2(.5f,0),position,new Vector2(size,size));
            // The texture includes generous transparent padding; the full square is
            // the hit target, so thin silhouettes remain easy to click.
            var icon=rect.gameObject.AddComponent<RawImage>();icon.texture=atlas;icon.uvRect=new Rect(tile%4/4f,(2-tile/4)/3f,.25f,1f/3);icon.color=Color.white;
            var shadow=rect.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.02f,.025f,.015f,.8f);shadow.effectDistance=new Vector2(0,-2);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=icon;
            var colors=button.colors;colors.normalColor=new Color(.83f,.8f,.69f,.92f);colors.highlightedColor=new Color(1,.98f,.87f,1);colors.pressedColor=new Color(1,.9f,.62f,1);colors.selectedColor=colors.normalColor;colors.disabledColor=new Color(.42f,.43f,.4f,.38f);colors.fadeDuration=.12f;button.colors=colors;
            var marker=Rect(rect,"Selection",new Vector2(.5f,0),new Vector2(0,-4),new Vector2(14,2));mark=marker.gameObject.AddComponent<Image>();mark.color=new Color(.9f,.77f,.46f,.95f);mark.raycastTarget=false;mark.gameObject.SetActive(false);
            return button;
        }
        static void HideLegacy(Transform hud)
        {
            foreach(string name in new[]{"SeasonSummer","SeasonFall","SeasonWinter","TemporaryWeather"}){var item=hud.Find(name);if(item)item.gameObject.SetActive(false);}
        }
        public void Select(NatureTool tool)
        {
            if(!view||view.SelectedDistrict<0||view.DistrictLoading||view.TownConstructionVisible)return;
            bool reopen=Selected==tool&&!Popup.gameObject.activeSelf;Selected=Selected==tool&&!reopen?NatureTool.None:tool;Popup.Show(Selected);sunMenu.gameObject.SetActive(Selected==NatureTool.Sun);
            ToolSelected?.Invoke(Selected);Refresh();
        }
        public bool Dismiss()
        {
            if(Selected==NatureTool.None)return false;Selected=NatureTool.None;if(Popup)Popup.Show(NatureTool.None);if(sunMenu)sunMenu.gameObject.SetActive(false);ToolSelected?.Invoke(Selected);Refresh();return true;
        }
        void LateUpdate(){if(view&&root)Refresh();}
        void Refresh()
        {
            if(!view||!root)return;bool district=view.SelectedDistrict>=0&&!view.DistrictLoading;
            bool leaving=!district&&wasDistrict;wasDistrict=district;if(leaving)Dismiss();root.gameObject.SetActive(district&&!view.TownConstructionVisible);
            var controls=view.transform.Find("RegionHud/Controls");if(controls)controls.gameObject.SetActive(!district);
            for(int i=0;i<5;i++)marks[i].gameObject.SetActive(Selected==(NatureTool)(i+1));
            var current=Weather;
            if(Selected==NatureTool.Sun)Popup.SetStatus(Popup.Option==0?view.VisualSeason.ToString():current?current.Current.ToString():"");
            for(int i=0;i<3;i++){
                seasons[i].interactable=district&&(view.Climate!=RegionClimate.Tropical||i==0);
                seasonMarks[i].gameObject.SetActive(view.VisualSeason==(RegionSeason)i);
                weather[i].interactable=district&&current&&(i==0||(i==1?!current.Winter:current.Winter));
                weatherMarks[i].gameObject.SetActive(current&&current.Current==(DistrictWeather.WeatherKind)i);
            }
        }
    }
}

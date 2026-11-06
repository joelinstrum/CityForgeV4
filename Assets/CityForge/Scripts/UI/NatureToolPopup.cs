using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using CityForge.World;
namespace CityForge.UI
{
    /// <summary>Category options and transient editor settings; no terrain or population mutation.</summary>
    public sealed class NatureToolPopup : MonoBehaviour
    {
        public NatureTool Category {get;private set;}
        public int Option {get;private set;}
        public int WaterBody {get;private set;}=-1;
        public bool WaterTypesOpen {get;private set;}
        public event Action<bool,int> WaterBodySelected;
        public event Action WaterRemovalRequested;
        public RectTransform SunHost {get;private set;}
        public event Action<int> OptionChanged;
        public event Action<int> OptionInvoked;
        public event Action<string,float> SettingChanged;
        readonly Dictionary<NatureTool,int> selected=new();
        readonly Dictionary<string,float> values=new();
        readonly Dictionary<string,int> choices=new();
        readonly List<GameObject> optionObjects=new();
        Texture2D atlas;Font font;Text heading,title,explanation,status;RectTransform content;
        Action close;
        static readonly Color Gold=new Color(.9f,.8f,.58f),Muted=new Color(.76f,.78f,.7f);
        public void Initialize(Texture2D icons,Action dismiss)
        {
            atlas=icons;close=dismiss;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var background=gameObject.AddComponent<Image>();background.color=new Color(.065f,.078f,.057f,.97f);
            var outline=gameObject.AddComponent<Outline>();outline.effectColor=new Color(.55f,.46f,.27f,.6f);outline.effectDistance=new Vector2(1,-1);
            var shadow=gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.3f);shadow.effectDistance=new Vector2(0,-4);
            heading=Label(transform,"Heading","",new Vector2(22,-16),new Vector2(470,32),23,Gold);
            var closeButton=AtlasButton(transform,"Close",11,new Vector2(531,-8),new Vector2(36,36));closeButton.onClick.AddListener(()=>close());
            var line=Box(transform,"Divider",new Vector2(112,-65),new Vector2(1,258),new Color(.5f,.47f,.31f,.35f));line.raycastTarget=false;
            title=Label(transform,"OptionTitle","",new Vector2(140,-77),new Vector2(408,28),21,Gold);
            explanation=Label(transform,"Explanation","",new Vector2(140,-113),new Vector2(406,56),18,Muted);
            content=TopRect(transform,"Settings",new Vector2(140,-181),new Vector2(406,143));
            SunHost=TopRect(transform,"SunOptions",new Vector2(140,-183),new Vector2(406,92));
            status=Label(transform,"Status","",new Vector2(140,-292),new Vector2(406,26),17,Muted);
        }
        static RectTransform TopRect(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=size;return r;
        }
        static Image Box(Transform parent,string name,Vector2 position,Vector2 size,Color color){var r=TopRect(parent,name,position,size);var image=r.gameObject.AddComponent<Image>();image.color=color;return image;}
        Text Label(Transform parent,string name,string value,Vector2 position,Vector2 size,int fontSize,Color color)
        {
            var r=TopRect(parent,name,position,size);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=fontSize;t.text=value;t.color=color;t.raycastTarget=false;t.alignment=TextAnchor.UpperLeft;return t;
        }
        Button AtlasButton(Transform parent,string name,int tile,Vector2 position,Vector2 size)
        {
            var r=TopRect(parent,name,position,size);var image=r.gameObject.AddComponent<RawImage>();image.texture=atlas;image.uvRect=new Rect(tile%4/4f,(2-tile/4)/3f,.25f,1f/3);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var c=b.colors;c.normalColor=new Color(.85f,.83f,.73f);c.highlightedColor=Color.white;c.pressedColor=Gold;c.selectedColor=c.normalColor;b.colors=c;return b;
        }
        public void Show(NatureTool tool)
        {
            Category=tool;gameObject.SetActive(tool!=NatureTool.None);if(tool==NatureTool.None)return;
            foreach(var old in optionObjects){old.SetActive(false);Destroy(old);}optionObjects.Clear();
            heading.text=tool switch{NatureTool.Terrain=>"Terraforming",NatureTool.Water=>"Water",NatureTool.Trees=>"Forests",NatureTool.Animals=>"Animals",_=>"Weather & seasons"};
            int count=tool==NatureTool.Water||tool==NatureTool.Animals||tool==NatureTool.Sun?2:3;
            for(int i=0;i<count;i++){
                int index=i;var back=Box(transform,"Option"+i,new Vector2(22,-76-i*80),new Vector2(72,66),Color.clear);var button=back.gameObject.AddComponent<Button>();button.targetGraphic=back;
                var colors=button.colors;colors.highlightedColor=new Color(1,1,.9f);colors.selectedColor=Color.white;button.colors=colors;
                if(tool==NatureTool.Terrain){var glyph=Label(back.transform,"Icon",new[]{"^","v","="}[i],new Vector2(0,-5),new Vector2(72,56),38,Gold);glyph.fontStyle=FontStyle.Bold;glyph.alignment=TextAnchor.MiddleCenter;}
                else{
                    int tile=tool==NatureTool.Water?1:tool==NatureTool.Trees?2:tool==NatureTool.Animals?3:(i==0?6:4);
                    var icon=AtlasButton(back.transform,"Icon",tile,new Vector2(4,-1),new Vector2(64,64));icon.interactable=false;var c=icon.colors;c.disabledColor=Color.white;icon.colors=c;icon.GetComponent<RawImage>().raycastTarget=false;
                    if((tool==NatureTool.Trees&&i<2)||tool==NatureTool.Water||tool==NatureTool.Animals){var badge=Label(back.transform,"Badge",tool==NatureTool.Water?new[]{"+","×"}[i]:tool==NatureTool.Trees?new[]{"+","×","1"}[i]:i==0?"1":"+",new Vector2(51,-46),new Vector2(20,20),15,Gold);badge.fontStyle=FontStyle.Bold;}
                }
                button.onClick.AddListener(()=>{
                    SelectOption(index);
                    if(Category!=NatureTool.Water)OptionInvoked?.Invoke(index);
                    else if(index==0)OpenWaterTypes();
                    else WaterRemovalRequested?.Invoke();
                });optionObjects.Add(back.gameObject);
            }
            SelectOption(tool!=NatureTool.Water&&selected.TryGetValue(tool,out int prior)?prior:0);
        }
        public void SelectOption(int index)
        {
            int count=Category==NatureTool.Water||Category==NatureTool.Animals||Category==NatureTool.Sun?2:3;if(index<0||index>=count)return;
            Option=index;selected[Category]=index;WaterTypesOpen=false;WaterBody=-1;
            for(int i=0;i<optionObjects.Count;i++)optionObjects[i].GetComponent<Image>().color=i==index?new Color(.35f,.32f,.21f,.62f):new Color(.17f,.19f,.14f,.35f);
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}status.text="";SunHost.gameObject.SetActive(Category==NatureTool.Sun);
            if(Category==NatureTool.Terrain){
                title.text=new[]{"Raise terrain","Lower terrain","Level terrain"}[index];explanation.text=new[]{"Terraform hills & mountains.","Add valleys and deepen the landscape.","Level out an area."}[index];
                Slider("Radius","Brush size",5,100,25,0," m");Slider(index==2?"Blend":"Strength",index==2?"Blend strength":"Brush strength",1,20,5,70,"");
            }else if(Category==NatureTool.Water){
                title.text=index==0?"Add Water Bodies":"Remove Water Bodies";
                explanation.text=index==0?"Create lakes, streams and rivers.":"Click a player-added water body to restore the ground beneath it.";
                if(index==0)
                    TextButton(content,"ChooseWaterType","Choose a water body  >",Vector2.zero,new Vector2(400,42),OpenWaterTypes);
                TextButton(content,"RemoveWaterBody","Remove Water",new Vector2(0,index==0?-52:0),new Vector2(400,42),()=>WaterRemovalRequested?.Invoke());
            }else if(Category==NatureTool.Trees){
                title.text=new[]{"Add Woodlands","Remove Woodlands","Add trees"}[index];explanation.text=new[]{"Select the add icon, then drag to plant. Tab changes brush size.","Remove trees inside the red circle.","Place individual trees in the landscape."}[index];
                if(index==1){
                    Label(content,"RemovalSize","200 m circumference",new Vector2(0,0),new Vector2(400,28),20,Gold);
                    Label(content,"RemovalHint","Click or drag to remove trees.\nTab switches between 200 m and 400 m.",new Vector2(0,-36),new Vector2(400,56),17,Muted);
                }else if(index==0){
                    Label(content,"PlantingSize","200 m circumference",new Vector2(0,0),new Vector2(400,28),20,Gold);
                    var view=GetComponentInParent<PrototypeRegionView>();bool mixed=view&&view.ApprovedDistrict&&view.ApprovedDistrict.MixedForest;Choice("Species","Tree type",mixed?new[]{"Mixed woodland","Chestnut","Locust","Larch"}:new[]{"Native woodland"},32);
                }else{
                    Choice("Species","Tree type",new[]{"Mixed woodland","Chestnut","Locust","Larch"},0);
                }
                TextButton(content,"RemoveTrees","Remove Trees",new Vector2(0,-104),new Vector2(400,38),()=>OptionInvoked?.Invoke(1));
            }else if(Category==NatureTool.Animals){
                title.text=index==0?"Individual animal":"Animal herd";explanation.text=index==0?"Choose an animal to add to the district.":"Add a group of animals together.";
                var species=new List<string>();var library=Resources.Load<WildlifeLibrary>("Wildlife/TemperateWildlife");if(library)foreach(var animal in library.species)if(animal.prefab&&!species.Contains(animal.id))species.Add(animal.id.Replace('_',' '));
                if(species.Count==0)species.Add("Deer");Choice("Species","Animal",species.ToArray(),0);if(index==1)Slider("Count","Herd size",2,20,5,70,"",true);
            }else{
                title.text=index==0?"Seasons":"Weather";explanation.text=index==0?"Choose the season for this district.":"Choose clear skies, rain or snow.";
            }
            OptionChanged?.Invoke(index);
        }
        Button TextButton(Transform parent,string name,string text,Vector2 position,Vector2 size,Action clicked)
        {
            var back=Box(parent,name,position,size,new Color(.2f,.23f,.17f));var button=back.gameObject.AddComponent<Button>();button.targetGraphic=back;
            Label(back.transform,"Caption",text,new Vector2(10,-7),size-new Vector2(20,8),18,Gold);button.onClick.AddListener(()=>clicked());return button;
        }
        public void OpenWaterTypes()
        {
            if(Category!=NatureTool.Water||Option!=0)return;
            WaterTypesOpen=true;WaterBody=-1;
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            explanation.text="Choose the kind of water body to add.";
            status.text="";
            for(int i=0;i<3;i++){
                int kind=i;string label=new[]{"Lake / Pond","Stream","River"}[i];
                TextButton(content,"WaterBody"+i,label,new Vector2(0,-i*38),new Vector2(400,32),()=>{
                    WaterBody=kind;
                    foreach(Transform row in content)if(row.name.StartsWith("WaterBody"))row.GetComponent<Image>().color=row.name=="WaterBody"+kind?new Color(.35f,.32f,.21f,.9f):new Color(.2f,.23f,.17f);
                    status.text=label+" selected · Editing tools coming next";
                    WaterBodySelected?.Invoke(true,kind);
                });
            }
            TextButton(content,"WaterBack","<  Back",new Vector2(0,-118),new Vector2(110,30),()=>SelectOption(Option));
        }
        public float Value(string name,float fallback)=>values.TryGetValue(Category+"/"+Option+"/"+name,out float value)?value:fallback;
        public int ChoiceIndex(string key)=>choices.TryGetValue(Category+"/"+Option+"/"+key,out int index)?index:0;
        public void SetStatus(string value){if(status&&status.text!=value)status.text=value;}
        void Slider(string key,string label,float min,float max,float initial,float y,string unit,bool whole=false)
        {
            string id=Category+"/"+Option+"/"+key;Label(content,key+"Label",label,new Vector2(0,-y),new Vector2(260,24),17,Muted);
            var valueText=Label(content,key+"Value","",new Vector2(284,-y),new Vector2(120,24),17,Gold);valueText.alignment=TextAnchor.UpperRight;
            var r=TopRect(content,key+"Slider",new Vector2(0,-y-31),new Vector2(400,20));r.gameObject.AddComponent<Image>().color=Color.clear;var slider=r.gameObject.AddComponent<Slider>();
            var track=Box(r,"Track",new Vector2(0,-8),new Vector2(400,4),new Color(.32f,.34f,.26f));track.raycastTarget=false;
            var fillArea=TopRect(r,"FillArea",new Vector2(0,-8),new Vector2(400,4));var fill=Box(fillArea,"Fill",Vector2.zero,new Vector2(400,4),new Color(.64f,.55f,.34f));fill.raycastTarget=false;
            var handle=Box(r,"Handle",new Vector2(0,-2),new Vector2(9,16),Gold);fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.anchoredPosition=Vector2.zero;slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=min;slider.maxValue=max;slider.wholeNumbers=whole;
            void UpdateValue(float value){values[id]=value;valueText.text=value.ToString(whole||value>=10?"0":"0.#",CultureInfo.InvariantCulture)+unit;SettingChanged?.Invoke(id,value);}
            slider.SetValueWithoutNotify(values.TryGetValue(id,out float prior)?prior:initial);UpdateValue(slider.value);slider.onValueChanged.AddListener(UpdateValue);
        }
        void Choice(string key,string label,string[] options,float y)
        {
            string id=Category+"/"+Option+"/"+key;Label(content,key+"Label",label,new Vector2(0,-y),new Vector2(400,24),17,Muted);
            var back=Box(content,key+"Choice",new Vector2(0,-y-30),new Vector2(400,32),new Color(.2f,.23f,.17f));var button=back.gameObject.AddComponent<Button>();button.targetGraphic=back;
            int index=choices.TryGetValue(id,out int prior)?Mathf.Clamp(prior,0,options.Length-1):0;
            var text=Label(back.transform,"Value",options[index]+"  >",new Vector2(10,-5),new Vector2(380,24),17,Gold);
            button.onClick.AddListener(()=>{index=(index+1)%options.Length;choices[id]=index;text.text=options[index]+"  >";SettingChanged?.Invoke(id,index);});
        }
    }
}

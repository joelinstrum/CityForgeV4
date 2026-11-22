using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CityForge.UI
{
    internal sealed class PedestrianPathOptions
    {
        internal string materialId="weathered-red-brick-pavement";
        internal int width=2, feather=45, rotation, shape;
        internal bool curved=true;
        internal LotPathDepth depth;
        internal LotPathBorder border;
        internal LotPathBorderSides sides;
        internal static PedestrianPathOptions From(LotPathDefinition p)=>new(){materialId=p.materialId,width=p.width,feather=p.edgeBlend,rotation=p.textureRotation,curved=p.curved,depth=p.depth,border=p.border,sides=p.borderSides};
        internal void Apply(LotPathDefinition p){p.materialId=materialId;p.width=width;p.edgeBlend=feather;p.textureRotation=rotation;p.curved=curved;p.depth=depth;p.recessed=false;p.border=border;p.borderSides=sides;}
    }
    // Shared appearance controls; consumers retain their own geometry, persistence and undo.
    internal sealed class PedestrianPathModal:MonoBehaviour
    {
        static readonly Color Gold=new(.94f,.83f,.58f), Muted=new(.66f,.68f,.61f);
        static Font pathFont;
        RectTransform frame;Font font;PedestrianPathOptions state;Action undoKeys;
        readonly List<(Button button,Func<bool> selected,bool border)> choices=new();
        Text borderHint;readonly List<Action> captions=new();
        internal static GameObject Show(Transform parent,Font font,PedestrianPathOptions state,bool editing,Action accept,Action cancel,Action undo,Action delete=null,bool handleUndoKeys=false)
        {
            var root=new GameObject("Pedestrian path modal",typeof(RectTransform),typeof(Image),typeof(PedestrianPathModal));root.transform.SetParent(parent,false);
            var r=(RectTransform)root.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;root.GetComponent<Image>().color=new Color(0,0,0,.58f);
            var modal=root.GetComponent<PedestrianPathModal>();if(!pathFont)pathFont=Font.CreateDynamicFontFromOSFont("Georgia",20);modal.font=pathFont?pathFont:font;modal.state=state;modal.undoKeys=handleUndoKeys?undo:null;modal.Build(editing,accept,cancel,undo,delete);return root;
        }
        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        Text Label(Transform p,string text,float x,float y,float w,float h,int size=20,Color? color=null)
        {var r=Rect(p,text,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=color??Gold;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;}
        void Panel(RectTransform r){var image=r.gameObject.AddComponent<Image>();image.sprite=ZoneModalArtwork.Panel;image.type=Image.Type.Sliced;}
        Button Button(Transform p,string text,float x,float y,float w,float h,Action click,Func<bool> selected=null,bool border=false)
        {var r=Rect(p,text,x,y,w,h);var b=r.gameObject.AddComponent<Button>();var im=r.gameObject.AddComponent<Image>();im.sprite=ZoneModalArtwork.Dark;im.type=Image.Type.Sliced;b.targetGraphic=im;var t=Label(r,text,6,0,w-12,h,19);t.alignment=TextAnchor.MiddleCenter;b.onClick.AddListener(()=>{click?.Invoke();Refresh();});if(selected!=null)choices.Add((b,selected,border));else if(border)choices.Add((b,()=>false,true));return b;}
        void Thumb(Button b,Texture texture,float w,float h)
        {if(!texture)return;var r=Rect(b.transform,"Texture sample",8,7,w-16,h);var image=r.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;var t=b.GetComponentInChildren<Text>();t.rectTransform.anchoredPosition=new Vector2(4,-h-9);t.rectTransform.sizeDelta=new Vector2(w-8,30);t.fontSize=17;t.resizeTextForBestFit=true;t.resizeTextMinSize=12;t.resizeTextMaxSize=17;}
        void Build(bool editing,Action accept,Action cancel,Action undo,Action delete)
        {
            frame=Rect(transform,"Path settings",0,0,1240,840);frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(.5f,.5f);Panel(frame);
            var artwork=Resources.Load<Texture2D>("UI/PedestrianPathConcept");if(artwork){var banner=Rect(frame,"Town banner",8,7,1224,112).gameObject.AddComponent<RawImage>();banner.texture=artwork;banner.uvRect=new UnityEngine.Rect(0,.85f,1,.15f);banner.raycastTarget=false;}else Label(frame,"Create Pedestrian Path",260,16,850,60,38);
            var close=Button(frame,"×",1180,22,40,40,cancel);if(artwork){close.image.color=Color.clear;close.GetComponentInChildren<Text>().enabled=false;}
            if(editing)Label(frame,"Editing selected path",430,91,400,24,17);
            var surfaces=Rect(frame,"Surface",18,124,1204,220);Panel(surfaces);Label(surfaces,"Surface",24,9,150,38,29);Label(surfaces,"Select a texture for your path.",184,12,380,34,19);
            Label(surfaces,"Founders Era (1740–1819)",880,12,300,34,19);
            var area=Rect(surfaces,"Surface browser",20,62,1164,144);area.gameObject.AddComponent<Image>().color=new Color(.045f,.075f,.075f);area.gameObject.AddComponent<Mask>().showMaskGraphic=true;var scroll=area.gameObject.AddComponent<ScrollRect>();scroll.viewport=area;scroll.horizontal=true;scroll.vertical=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var preferred=new[]{"irregular-flagstone-pavement","weathered-red-brick-pavement","weathered-herringbone-pavement","early-macadam-gravel","rustic-wood-pavement","mossy-fieldstone",LotPathMaterial.Bluestone};
            var styles=LotPathMaterial.All.Where(s=>s.IsAvailable).OrderBy(s=>{int i=Array.IndexOf(preferred,s.Id);return i<0?100:i;}).ToArray();var content=Rect(area,"Path surfaces",0,0,styles.Length*158,136);scroll.content=content;
            var materialButtons=new List<(LotPathMaterial style,Button button)>();
            for(int i=0;i<styles.Length;i++){var style=styles[i];var b=Button(content,style.Label,i*158,0,148,134,()=>state.materialId=style.Id,()=>state.materialId==style.Id);Thumb(b,style.Texture,148,92);materialButtons.Add((style,b));}
            int current=Array.FindIndex(styles,s=>s.Id==state.materialId);content.anchoredPosition=new Vector2(-Mathf.Clamp((current-3)*158,0,Mathf.Max(0,content.sizeDelta.x-1164)),0);
            var search=DefaultControls.CreateInputField(new DefaultControls.Resources()).GetComponent<InputField>();search.transform.SetParent(surfaces,false);var sr=(RectTransform)search.transform;sr.anchorMin=sr.anchorMax=sr.pivot=new Vector2(0,1);sr.anchoredPosition=new Vector2(580,-15);sr.sizeDelta=new Vector2(270,30);search.textComponent.font=font;search.textComponent.color=Gold;search.textComponent.fontSize=17;search.GetComponent<Image>().sprite=ZoneModalArtwork.Dark;search.GetComponent<Image>().type=Image.Type.Sliced;var placeholder=(Text)search.placeholder;placeholder.font=font;placeholder.fontSize=17;placeholder.text="Search surfaces…";placeholder.color=Muted;
            search.onValueChanged.AddListener(query=>{int index=0;foreach(var entry in materialButtons){bool show=entry.style.Label.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0;entry.button.gameObject.SetActive(show);if(show)((RectTransform)entry.button.transform).anchoredPosition=new Vector2(index++*158,0);}content.sizeDelta=new Vector2(index*158,136);scroll.horizontalNormalizedPosition=0;});
            var borders=Rect(frame,"Border",18,356,1204,188);Panel(borders);Label(borders,"Border",24,9,150,38,29);borderHint=Label(borders,"",184,12,740,34,18);Label(borders,"Placement",994,10,190,36,24);
            for(int i=0;i<LotPathBorders.All.Length;i++){var value=LotPathBorders.All[i];var b=Button(borders,LotPathBorders.Label(value),20+i*134,62,124,108,()=>state.border=value,()=>state.border==value,true);Thumb(b,LotPathBorders.Texture(value),124,67);}
            foreach(var side in new[]{LotPathBorderSides.Both,LotPathBorderSides.Left,LotPathBorderSides.Right}){var value=side;Button(borders,side==LotPathBorderSides.Both?"Both sides":side+" side",994,56+(int)side*40,190,34,()=>state.sides=value,()=>state.sides==value,true);}
            var settings=Rect(frame,"Path settings controls",18,556,1204,180);Panel(settings);Label(settings,"Path Settings",24,6,400,32,27);Label(settings,"Width",24,46,90,30);
            for(int i=0;i<LotPathSurface.Widths.Length;i++){int value=LotPathSurface.Widths[i];Button(settings,value+" m",130+i*90,46,80,30,()=>state.width=value,()=>state.width==value);}
            var feather=Label(settings,"",24,88,255,30,19);captions.Add(()=>feather.text="Feathering   "+state.feather+"%");var slider=DefaultControls.CreateSlider(new DefaultControls.Resources()).GetComponent<Slider>();slider.transform.SetParent(settings,false);var sl=(RectTransform)slider.transform;sl.anchorMin=sl.anchorMax=sl.pivot=new Vector2(0,1);sl.anchoredPosition=new Vector2(280,-94);sl.sizeDelta=new Vector2(385,22);slider.minValue=0;slider.maxValue=100;slider.wholeNumbers=true;slider.value=state.feather;foreach(var image in slider.GetComponentsInChildren<Image>())image.color=image.name=="Fill"?Gold:new Color(.3f,.35f,.38f);slider.onValueChanged.AddListener(v=>{state.feather=(int)v;Refresh();});
            Label(settings,"Sink path",24,132,105,30,19);foreach(var depth in new[]{LotPathDepth.Flat,LotPathDepth.HalfFoot,LotPathDepth.OneFoot}){var d=depth;Button(settings,depth==LotPathDepth.Flat?"Ground level":depth==LotPathDepth.HalfFoot?"0.5 ft down":"1 ft down",130+(int)depth*180,130,166,32,()=>state.depth=d,()=>state.depth==d);}
            var facing=Button(settings,"",790,44,390,32,()=>state.rotation=(state.rotation+1)%4);captions.Add(()=>facing.GetComponentInChildren<Text>().text="Texture facing: "+new[]{"North","East","South","West"}[state.rotation]);
            var corners=Button(settings,"",790,87,390,32,()=>state.curved=!state.curved,()=>state.curved);captions.Add(()=>corners.GetComponentInChildren<Text>().text="Corners: "+(state.curved?"Curved":"Straight"));
            if(!editing){var shape=Button(settings,"",790,130,390,32,()=>state.shape=(state.shape+1)%4);captions.Add(()=>shape.GetComponentInChildren<Text>().text="Shape: "+new[]{"Freehand","Circle","Open rectangle","Half circle"}[state.shape]);}
            Button(frame,"Undo · Ctrl/Cmd Z",26,770,270,44,undo);if(delete!=null)Button(frame,"Delete path",316,770,220,44,delete);Button(frame,"Cancel",770,770,180,44,cancel);Button(frame,editing?"Apply changes":"Draw Path",970,766,244,52,accept,()=>true);Refresh();Fit();
        }
        void Refresh(){foreach(var choice in choices){bool enabled=!choice.border||state.depth!=LotPathDepth.Flat;choice.button.interactable=enabled;choice.button.image.sprite=choice.selected()?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;choice.button.image.color=enabled?Color.white:new Color(.45f,.45f,.45f);foreach(var image in choice.button.GetComponentsInChildren<RawImage>())image.color=enabled?Color.white:new Color(.4f,.4f,.4f);}borderHint.text=state.depth==LotPathDepth.Flat?"Sink the path to add borders.":"Choose a lining · Left/right follows the drawing direction.";foreach(var caption in captions)caption();}
        void Update()
        {
            if(undoKeys==null||EventSystem.current&&EventSystem.current.currentSelectedGameObject&&EventSystem.current.currentSelectedGameObject.GetComponent<InputField>())return;
            if(Input.GetKeyDown(KeyCode.Z)&&(Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl)||Input.GetKey(KeyCode.LeftCommand)||Input.GetKey(KeyCode.RightCommand)))undoKeys();
        }
        void LateUpdate()=>Fit();
        void Fit(){if(!frame)return;var p=(RectTransform)transform;float scale=Mathf.Min(1f,(p.rect.width-32)/1240f,(p.rect.height-32)/840f);frame.localScale=Vector3.one*Mathf.Max(.1f,scale);}
    }
}

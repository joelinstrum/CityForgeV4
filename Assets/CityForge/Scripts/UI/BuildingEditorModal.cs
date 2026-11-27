using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using CityForge.World;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.UI
{
    // Shared draft/preview UI; consumers own undo and the persistence scope.
    internal sealed class BuildingEditorModal : MonoBehaviour
    {
        static readonly Color Cream=new(1,.91f,.73f), Muted=new(.69f,.75f,.78f);
        RectTransform frame, content;
        Font font;
        bool ownsFont;
        Action cancel;
        Action<IReadOnlyDictionary<string,BuildingAppearanceSettings>> apply;
        readonly Dictionary<string,BuildingAppearanceSettings> drafts=new();
        readonly Dictionary<string,BuildingAppearanceSettings> originals=new();
        Button applyButton;
        Text status, tools;
        readonly List<(Button Button,string Id)> paintButtons=new();
        Text paintLabel,awningLabel,wallLabel,wallSaturationLabel;
        Slider wallSaturationSlider;
        readonly List<(Button Button,string Id)> wallButtons=new();
        readonly List<(Button Button,string Id)> awningButtons=new();
        BuildingEditorPreview preview;
        List<LotObjectDefinition> buildings;
        LotAssetCatalog catalog;
        Text buildingName;
        Button[] tabs;
        int current, tab, signMountIndex;
        bool night, showIncompatible;
        string signQuery="";
        BuildingSignMount browserMount;
        RectTransform signGrid;
        Text signCount, selectedSignLabel;
        Button originalSignButton, noSignButton;
        readonly List<(Button Button,string Id)> signButtons=new();
        readonly List<Sprite> slices=new();
        internal static GameObject Show(Transform parent,Font fallback,IEnumerable<LotObjectDefinition> objects,LotAssetCatalog catalog,string scope,Action cancel,string selectedId=null,Action<IReadOnlyDictionary<string,BuildingAppearanceSettings>> apply=null)
        {
            var items=objects.Where(o=>catalog.Find(o.assetId)?.category=="Buildings").ToList();
            if(items.Count==0)return null;
            var root=new GameObject("Edit Building modal",typeof(RectTransform),typeof(Image),typeof(BuildingEditorModal));root.transform.SetParent(parent,false);
            var r=(RectTransform)root.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;root.GetComponent<Image>().color=new Color(0,0,0,.7f);
            var m=root.GetComponent<BuildingEditorModal>();m.font=Font.CreateDynamicFontFromOSFont(new[]{"Georgia","Times New Roman"},24);m.ownsFont=m.font;if(!m.font)m.font=fallback;
            m.cancel=cancel;m.apply=apply;foreach(var item in items){m.originals[item.id]=BuildingAppearance.Copy(item.buildingAppearance);m.drafts[item.id]=BuildingAppearance.Copy(item.buildingAppearance);}m.catalog=catalog;m.buildings=items;m.current=Mathf.Max(0,items.FindIndex(i=>i.id==selectedId));m.Build(scope);return root;
        }
        static RectTransform Rect(Transform p,string name,float x,float y,float w,float h)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        Text Label(Transform p,string text,float x,float y,float w,float h,int size=22,Color? color=null)
        {var r=Rect(p,text,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=color??Cream;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;t.supportRichText=false;return t;}
        RectTransform Box(Transform p,string name,float x,float y,float w,float h,bool outer=false)
        {var r=Rect(p,name,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.sprite=outer?ZoneModalArtwork.Panel:ZoneModalArtwork.Section;image.type=Image.Type.Sliced;return r;}
        Button Button(Transform p,string text,float x,float y,float w,float h,Action action,bool gold=false)
        {var r=Rect(p,text,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.sprite=gold?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;image.type=Image.Type.Sliced;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var t=Label(r,text,8,0,w-16,h,22);t.alignment=TextAnchor.MiddleCenter;b.onClick.AddListener(()=>action?.Invoke());return b;}
        void Icon(Transform p,int index,float x,float y,float size)
        {
            var texture=Resources.Load<Texture2D>("UI/BuildingEditor/Icons");if(!texture)return;
            var rects=new[]{new UnityEngine.Rect(65,335,375,350),new UnityEngine.Rect(495,350,355,295),new UnityEngine.Rect(915,335,325,340),new UnityEngine.Rect(1285,340,355,340),new UnityEngine.Rect(1670,340,405,340)};
            var area=rects[index];area.x*=texture.width/2172f;area.width*=texture.width/2172f;area.y*=texture.height/724f;area.height*=texture.height/724f;
            var sprite=Sprite.Create(texture,area,Vector2.one*.5f,100);slices.Add(sprite);
            var im=Rect(p,"Gold icon",x,y,size,size).gameObject.AddComponent<Image>();im.sprite=sprite;im.preserveAspect=true;im.raycastTarget=false;
        }
        void Sample(Transform p,int index,bool color,float x,float y,float size)
        {
            var texture=Resources.Load<Texture2D>("UI/BuildingEditor/Swatches");if(!texture)return;
            var uv=new UnityEngine.Rect((10+index*216)/2172f,color?12f/724:248f/724,205f/2172,205f/724);
            var im=Rect(p,"Material swatch",x,y,size,size).gameObject.AddComponent<RawImage>();im.texture=texture;im.uvRect=uv;im.raycastTarget=false;
        }
        void Build(string scope)
        {
            frame=Box(transform,"Customize Building",0,0,1340,860,true);frame.anchorMin=frame.anchorMax=frame.pivot=Vector2.one*.5f;
            var crest=Box(frame,"CityForge crest",512,-16,316,68,true);var logo=Label(crest,"CityForge",0,0,316,68,36);logo.alignment=TextAnchor.MiddleCenter;
            Button(frame,"×",1262,17,54,48,cancel);
            preview=new BuildingEditorPreview();
            var thumbBox=Box(frame,"Building thumbnail",32,77,154,145);var thumb=Rect(thumbBox,"Thumbnail",7,7,140,131).gameObject.AddComponent<RawImage>();thumb.rectTransform.pivot=Vector2.one*.5f;thumb.texture=preview.Texture;thumb.raycastTarget=false;var thumbAspect=thumb.gameObject.AddComponent<AspectRatioFitter>();thumbAspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;thumbAspect.aspectRatio=624f/816;
            Label(frame,"Customize Building",210,68,640,66,43);
            Label(frame,"Modify the look and layout of this building.",214,133,650,34,22);
            Label(frame,scope,214,174,630,34,17,Muted);
            var hero=Box(frame,"Building preview",895,68,400,200);var raw=Rect(hero,"Live building preview",5,5,390,190).gameObject.AddComponent<RawImage>();raw.rectTransform.pivot=Vector2.one*.5f;raw.texture=preview.Texture;raw.uvRect=new UnityEngine.Rect(0,0,1,1);raw.raycastTarget=false;
            // Preserve the preview's aspect instead of stretching the building across the banner.
            var aspect=raw.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=624f/816;
            buildingName=Label(frame,"",214,216,590,40,22);
            if(buildings.Count>1){Button(frame,"‹",793,217,40,36,()=>Select(-1));Button(frame,"›",840,217,40,36,()=>Select(1));}
            var side=Box(frame,"Sections",24,282,232,470);
            tabs=new Button[3];var names=new[]{"Appearance","Structure","Signage"};var icons=new[]{0,2,3};
            for(int i=0;i<3;i++){int v=i;tabs[i]=Button(side,names[i],10,18+i*80,212,66,()=>{tab=v;ShowTab();});Icon(tabs[i].transform,icons[i],10,17,32);var t=tabs[i].GetComponentInChildren<Text>();t.rectTransform.anchoredPosition=new Vector2(50,0);t.rectTransform.sizeDelta=new Vector2(154,66);t.alignment=TextAnchor.MiddleLeft;t.fontSize=21;}
            tools=Label(side,"Trim tint\navailable now",22,337,190,68,18,Muted);
            content=Rect(frame,"Building controls",270,282,1046,470);
            Button(frame,"Cancel",715,786,224,48,cancel);
            applyButton=Button(frame,"Apply Changes",958,786,342,48,Accept,true);applyButton.interactable=false;
            status=Label(frame,"Preview trim tint, then apply your changes.",38,782,660,48,17,Muted);
            Button(frame,"‹",1052,230,42,34,()=>preview.Turn(-45));Button(frame,"›",1102,230,42,34,()=>preview.Turn(45));
            var time=Button(frame,"Day / Night",1152,230,143,34,()=>{night=!night;preview.SetTime(night);});time.GetComponentInChildren<Text>().fontSize=16;
            Select(0);ShowTab();Fit();
        }
        void Select(int step)
        {current=(current+step+buildings.Count)%buildings.Count;var asset=catalog.Find(buildings[current].assetId);buildingName.text=asset.label+(BuildingAppearance.IsPaintable(asset)?" · Paintable":BuildingAppearance.CanCustomize(asset)?" · Sign mounts":" · Original appearance")+(buildings.Count>1?$"   ({current+1} / {buildings.Count})":"");preview.Set(asset,drafts[buildings[current].id]);if(tabs!=null)ShowTab();}
        void Heading(Transform p,string text,int icon,float x,float y,float w)
        {Icon(p,icon,x,y+3,32);Label(p,text,x+44,y,w-44,40,27);}
        void ShowTab()
        {
            paintButtons.Clear();paintLabel=null;awningButtons.Clear();awningLabel=null;wallButtons.Clear();wallLabel=null;wallSaturationLabel=null;wallSaturationSlider=null;
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            for(int i=0;i<tabs.Length;i++)tabs[i].image.sprite=i==tab?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
            if(tab==0)
            {
                var walls=Box(content,"Wall Tint",0,0,568,152);Heading(walls,FixedBrick?"Brick Finish":"Wall Paint",0,20,10,520);
                BuildWalls(walls);
                var trim=Box(content,"Trim Tint",580,0,466,152);Heading(trim,"Trim Tint",1,20,10,425);
                BuildTrim(trim);
                var awnings=Box(content,"Awning Tint",0,164,568,182);Heading(awnings,"Awning Tint",2,20,10,520);
                BuildAwnings(awnings);
                Label(awnings,"Keep the original fabric texture and detail.",20,151,528,25,16,Muted);
                SignCard(content,580,164,466,182);
                var original=Box(content,"Original Materials",0,358,1046,112);
                Heading(original,"Original Textures",0,20,10,990);
                Label(original,FixedBrick?"Choose a brick-and-mortar texture for the wall surfaces.":"Tint changes color while keeping the building’s original surface detail.",24,55,990,28,20,Muted);
                Label(original,"Windows, signs, gold details and lighting keep their original colors.",24,82,990,24,17,Muted);
            }
            else if(tab==1)
            {
                var p=Box(content,"Structure",0,0,1046,470);Heading(p,"Building Height",4,24,18,970);
                Label(p,"Adjust the height of this building.",28,74,900,38,22,Muted);
                for(int i=0;i<5;i++){var b=Button(p,(i+1)+"",28+i*112,148,100,52,null,i==1);b.interactable=false;}
                Icon(p,4,690,130,240);Label(p,"Height controls will be added later.",28,253,620,44,21,Muted);
                Label(p,"The original building is shown in the preview.",28,305,620,44,19,Muted);
            }
            else
            {
                BuildSignage();
            }
            RefreshPaint();
        }
        BuildingAppearanceSettings Draft => drafts[buildings[current].id];
        bool Paintable => BuildingAppearance.SupportsTrim(catalog.Find(buildings[current].assetId));
        void TintPreview(Transform parent,float width,string message)
        {
            var original=Button(parent,"Original",width-142,12,120,32,null,true);original.interactable=false;original.GetComponentInChildren<Text>().fontSize=16;
            for(int i=0;i<BuildingAppearance.Colors.Length;i++)
            {
                var color=BuildingAppearance.Colors[i];var b=Button(parent,"",20+i*82,64,62,56,null);b.name="Tint preview · "+color.Label;b.interactable=false;
                var swatch=Rect(b.transform,"Tint swatch",7,7,48,42).gameObject.AddComponent<Image>();swatch.color=new Color(color.Color.r,color.Color.g,color.Color.b,.35f);swatch.raycastTarget=false;
            }
            Label(parent,message,20,126,width-40,24,16,Muted);
        }
        void BuildTrim(Transform parent)
        {
            var original=Button(parent,"Original",324,12,120,32,()=>ChooseTrim(""));original.name="Trim · Original";original.GetComponentInChildren<Text>().fontSize=16;
            paintButtons.Add((original,""));
            for(int i=0;i<BuildingAppearance.Colors.Length;i++)
            {
                var color=BuildingAppearance.Colors[i];
                var b=Button(parent,"",18+i*72,64,62,62,()=>ChooseTrim(color.Id));b.name="Trim · "+color.Label;
                var fill=Rect(b.transform,"Paint "+color.Label,7,7,48,48).gameObject.AddComponent<Image>();fill.color=color.Color;fill.raycastTarget=false;
                paintButtons.Add((b,color.Id));
            }
            paintLabel=Label(parent,"",20,128,424,22,15,Muted);
        }
        bool FixedBrick => BuildingAppearance.UsesBrickFinishes(catalog.Find(buildings[current].assetId));
        bool WallTintable => BuildingAppearance.SupportsWalls(catalog.Find(buildings[current].assetId));
        void BuildWalls(Transform parent)
        {
            var original=Button(parent,FixedBrick?"Original red":"Original",426,12,120,32,()=>ChooseWall(""));original.name="Wall · Original";original.GetComponentInChildren<Text>().fontSize=16;wallButtons.Add((original,""));
            var choices=FixedBrick?BuildingAppearance.BrickFinishes:BuildingAppearance.WallColors;
            for(int i=0;i<choices.Length;i++)
            {
                var color=choices[i];var b=Button(parent,"",20+i*(FixedBrick?88:66),55,FixedBrick?80:56,42,()=>ChooseWall(color.Id));b.name="Wall · "+color.Label;
                if(FixedBrick){var sample=Rect(b.transform,"Brick texture · "+color.Label,5,4,70,24).gameObject.AddComponent<RawImage>();sample.texture=BuildingAppearance.BrickFinishTexture(catalog.Find(buildings[current].assetId),color.Id);sample.uvRect=new UnityEngine.Rect(0,0,.4f,.4f);sample.raycastTarget=false;Label(b.transform,color.Label,0,25,80,16,12,Color.white);}
                else{var fill=Rect(b.transform,"Tint swatch",7,7,42,28).gameObject.AddComponent<Image>();fill.color=color.Color;fill.raycastTarget=false;}wallButtons.Add((b,color.Id));
            }
            wallLabel=Label(parent,"",20,100,528,22,16,Muted);
            if(FixedBrick){Label(parent,"Fixed brick textures · Original restores the red brick.",20,125,528,22,16,Muted);return;}
            wallSaturationLabel=Label(parent,"Saturation: 50%",20,125,148,22,16,Muted);
            var rail=Rect(parent,"Wall saturation",174,128,366,16);rail.gameObject.AddComponent<Image>().color=new Color(.12f,.17f,.20f);
            var fillRect=Rect(rail,"Saturation fill",0,0,366,16);fillRect.anchorMin=Vector2.zero;fillRect.anchorMax=Vector2.one;fillRect.offsetMin=Vector2.zero;fillRect.offsetMax=Vector2.zero;fillRect.gameObject.AddComponent<Image>().color=new Color(.72f,.52f,.21f);
            var handle=Rect(rail,"Saturation handle",0,-3,16,22);handle.gameObject.AddComponent<Image>().color=new Color(.93f,.84f,.62f);
            wallSaturationSlider=rail.gameObject.AddComponent<Slider>();wallSaturationSlider.direction=Slider.Direction.LeftToRight;wallSaturationSlider.minValue=0;wallSaturationSlider.maxValue=100;wallSaturationSlider.wholeNumbers=true;wallSaturationSlider.fillRect=fillRect;wallSaturationSlider.handleRect=handle;wallSaturationSlider.targetGraphic=handle.GetComponent<Image>();wallSaturationSlider.SetValueWithoutNotify(BuildingAppearance.WallSaturation(Draft)*100);
            wallSaturationSlider.onValueChanged.AddListener(ChooseWallSaturation);
        }
        void ChooseWall(string id)
        {
            if(!WallTintable)return;
            var state=Draft;state.wallColor=id;
            if(string.IsNullOrEmpty(id)){state.wallSaturationSet=false;state.wallSaturation=0;}
            else if(FixedBrick){state.wallSaturationSet=false;state.wallSaturation=0;}
            else if(!state.wallSaturationSet){state.wallSaturationSet=true;state.wallSaturation=.5f;}
            drafts[buildings[current].id]=state;preview.Paint(state);RefreshPaint();
        }
        void ChooseWallSaturation(float percent)
        {
            if(!WallTintable||string.IsNullOrEmpty(Draft.wallColor))return;
            var state=Draft;state.wallSaturationSet=true;state.wallSaturation=Mathf.Round(percent)/100f;
            drafts[buildings[current].id]=state;preview.Paint(state);RefreshPaint();
        }
        bool AwningTintable => BuildingAppearance.SupportsAwnings(catalog.Find(buildings[current].assetId));
        void BuildAwnings(Transform parent)
        {
            var original=Button(parent,"Original",426,12,120,32,()=>ChooseAwning(""));original.name="Awning · Original";original.GetComponentInChildren<Text>().fontSize=16;awningButtons.Add((original,""));
            for(int i=0;i<BuildingAppearance.Colors.Length;i++)
            {
                var color=BuildingAppearance.Colors[i];var b=Button(parent,"",20+i*82,64,62,56,()=>ChooseAwning(color.Id));b.name="Awning · "+color.Label;
                var fill=Rect(b.transform,"Tint swatch",7,7,48,42).gameObject.AddComponent<Image>();fill.color=color.Color;fill.raycastTarget=false;awningButtons.Add((b,color.Id));
            }
            awningLabel=Label(parent,"",20,126,528,24,16,Muted);
        }
        void ChooseAwning(string id)
        {
            if(!AwningTintable)return;
            var state=Draft;state.awningColor=id;drafts[buildings[current].id]=state;
            preview.Paint(state);RefreshPaint();
        }
        void ChooseTrim(string id)
        {
            if(!Paintable)return;
            var state=Draft;state.trimColor=id;drafts[buildings[current].id]=state;
            preview.Paint(state);RefreshPaint();
        }
        void RefreshPaint()
        {
            bool supported=Paintable;
            foreach(var choice in paintButtons)
            {
                choice.Button.interactable=supported;
                choice.Button.image.sprite=(Draft.trimColor??"")==choice.Id?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
                foreach(var image in choice.Button.GetComponentsInChildren<Image>())if(image!=choice.Button.image)image.color=new Color(image.color.r,image.color.g,image.color.b,supported?1:.35f);
            }
            foreach(var choice in awningButtons)
            {
                choice.Button.interactable=AwningTintable;
                choice.Button.image.sprite=(Draft.awningColor??"")==choice.Id?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
                foreach(var image in choice.Button.GetComponentsInChildren<Image>())if(image!=choice.Button.image)image.color=new Color(image.color.r,image.color.g,image.color.b,AwningTintable?1:.35f);
            }
            foreach(var choice in wallButtons)
            {
                choice.Button.interactable=WallTintable;
                choice.Button.image.sprite=(FixedBrick?BuildingAppearance.BrickFinishId(Draft.wallColor):Draft.wallColor??"")==choice.Id?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
                foreach(var image in choice.Button.GetComponentsInChildren<Image>())if(image!=choice.Button.image)image.color=new Color(image.color.r,image.color.g,image.color.b,WallTintable?1:.35f);
            }
            if(wallSaturationSlider){wallSaturationSlider.SetValueWithoutNotify(BuildingAppearance.WallSaturation(Draft)*100);wallSaturationSlider.interactable=WallTintable&&!string.IsNullOrEmpty(Draft.wallColor);}
            if(wallSaturationLabel)wallSaturationLabel.text="Saturation: "+Mathf.RoundToInt(BuildingAppearance.WallSaturation(Draft)*100)+"%";
            if(wallLabel)wallLabel.text=WallTintable?BuildingAppearance.WallLabel(catalog.Find(buildings[current].assetId))+": "+(string.IsNullOrEmpty(Draft.wallColor)?"Original":(FixedBrick?(BuildingAppearance.BrickFinishes.FirstOrDefault(c=>c.Id==BuildingAppearance.BrickFinishId(Draft.wallColor)).Label??"Original red"):BuildingAppearance.Find(Draft.wallColor).Label)):"Wall tint is not available for this building yet.";
            if(awningLabel)awningLabel.text=AwningTintable?"Awning tint: "+(string.IsNullOrEmpty(Draft.awningColor)?"Original":BuildingAppearance.Find(Draft.awningColor).Label):"Awning tint is not available for this building yet.";
            if(paintLabel)paintLabel.text=supported ? "Trim tint: "+(BuildingAppearance.Original(Draft)?"Original":BuildingAppearance.Find(Draft.trimColor).Label) : "Trim tint is not available for this building yet.";
            if(tools)tools.text=BuildingAppearance.IsPaintable(catalog.Find(buildings[current].assetId))?"Paintable\nSelected surfaces":"Defined sign\nmounts";
            applyButton.interactable=apply!=null&&drafts.Any(p=>!BuildingAppearance.Same(p.Value,originals[p.Key]));
            status.text=FixedBrick?"Choose a brick texture finish, then apply your changes.":supported||AwningTintable||WallTintable?"Preview tints, then apply your changes.":"Original materials retained. Tint controls are not available for this building yet.";
            if(tab==2){tools.text="Choose a sign\nand preview it";status.text=applyButton.interactable?"Preview changes, then apply to this building.":"Choose a mount and browse compatible signs.";}
        }
        void Accept()
        {
            var changed=drafts.Where(p=>!BuildingAppearance.Same(p.Value,originals[p.Key])).ToDictionary(p=>p.Key,p=>p.Value);
            if(apply==null||changed.Count==0)return;
            try{apply(changed);}
            catch(System.IO.InvalidDataException e){status.text=e.Message;}
        }
        RectTransform ScrollArea(Transform parent,string name,float x,float y,float w,float h,float contentHeight)
        {
            var viewport=Rect(parent,name,x,y,w,h);viewport.gameObject.AddComponent<Image>().color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            var body=Rect(viewport,name+" content",0,0,w,Mathf.Max(h,contentHeight));scroll.content=body;return body;
        }
        void BuildSignage()
        {
            signButtons.Clear();signGrid=null;browserMount=null;selectedSignLabel=null;
            var p=Box(content,"Sign browser",0,0,1046,470);Heading(p,"Signage",3,24,10,970);
            Label(p,"Choose an attachment point, then preview a compatible sign.",28,50,990,32,19,Muted);
            var asset=catalog.Find(buildings[current].assetId);
            if(!BuildingSignCompatibility.TryProfile(asset,out var profile,out var reason))
            {
                Label(p,reason,28,119,990,70,23,Muted);
                Label(p,"Front-facing signs lie along the facade. Projecting blade signs face sidewalk traffic on both sides.",28,212,960,100,22,Muted);
                Label(p,"A reviewed mount is required before adding or replacing a sign.",28,363,960,44,19,Muted);return;
            }
            signMountIndex=Mathf.Clamp(signMountIndex,0,profile.mounts.Length-1);browserMount=profile.mounts[signMountIndex];
            var left=Box(p,"Sign attachment",20,91,270,350);Label(left,"Attachment point",16,8,238,34,22);
            var mounts=ScrollArea(left,"Mount list",14,48,242,157,profile.mounts.Length*64);
            for(int i=0;i<profile.mounts.Length;i++)
            {
                int index=i;var mount=profile.mounts[i];var b=Button(mounts,(string.IsNullOrEmpty(mount.label)?mount.id:mount.label)+"\n"+BuildingSignCompatibility.KindLabel(mount.kind),0,i*64,234,58,()=>{signMountIndex=index;ShowTab();},i==signMountIndex);
                b.name="Sign mount · "+mount.id;b.GetComponentInChildren<Text>().fontSize=16;
            }
            originalSignButton=Button(left,"Original",14,218,114,36,()=>ChooseSign(""));originalSignButton.GetComponentInChildren<Text>().fontSize=17;
            noSignButton=Button(left,"No sign",140,218,114,36,()=>ChooseSign(BuildingSigns.None));noSignButton.GetComponentInChildren<Text>().fontSize=17;
            selectedSignLabel=Label(left,"",16,263,238,72,16,Muted);
            var right=Box(p,"Sign library",302,91,724,350);
            var search=DefaultControls.CreateInputField(new DefaultControls.Resources()).GetComponent<InputField>();search.transform.SetParent(right,false);
            var sr=(RectTransform)search.transform;sr.anchorMin=sr.anchorMax=sr.pivot=Vector2.up;sr.anchoredPosition=new Vector2(14,-12);sr.sizeDelta=new Vector2(300,32);
            search.image.sprite=ZoneModalArtwork.Dark;search.image.type=Image.Type.Sliced;search.textComponent.font=font;search.textComponent.fontSize=17;search.textComponent.color=Cream;
            var placeholder=(Text)search.placeholder;placeholder.font=font;placeholder.fontSize=17;placeholder.text="Search signs…";placeholder.color=Muted;
            search.SetTextWithoutNotify(signQuery);search.onValueChanged.AddListener(v=>{signQuery=v;RefreshSignGrid();});
            var toggle=Button(right,"",327,12,380,32,()=>{showIncompatible=!showIncompatible;ShowTab();});toggle.GetComponentInChildren<Text>().fontSize=16;
            toggle.GetComponentInChildren<Text>().text="Show incompatible: "+(showIncompatible?"On":"Off");
            signGrid=ScrollArea(right,"Sign thumbnail grid",14,55,674,279,279);
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources()).GetComponent<Scrollbar>();bar.transform.SetParent(right,false);
            var br=(RectTransform)bar.transform;br.anchorMin=br.anchorMax=br.pivot=Vector2.up;br.anchoredPosition=new Vector2(699,-55);br.sizeDelta=new Vector2(10,279);bar.direction=Scrollbar.Direction.BottomToTop;
            foreach(var image in bar.GetComponentsInChildren<Image>())image.color=image.name=="Handle"?new Color(.72f,.62f,.40f):new Color(.055f,.08f,.10f);
            signGrid.parent.GetComponent<ScrollRect>().verticalScrollbar=bar;
            signCount=Label(p,"",28,440,990,26,15,Muted);RefreshSignGrid();
        }
        void RefreshSignGrid()
        {
            if(!signGrid||browserMount==null)return;
            foreach(Transform t in signGrid){t.gameObject.SetActive(false);Destroy(t.gameObject);}signButtons.Clear();
            var all=BuildingSigns.All();var signs=all.Where(s=>(showIncompatible||BuildingSignCompatibility.Evaluate(browserMount,s).Eligible)&&
                ((s.label??s.id).IndexOf(signQuery,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
            signGrid.sizeDelta=new Vector2(674,Mathf.Max(279,Mathf.CeilToInt(signs.Length/3f)*164));
            for(int i=0;i<signs.Length;i++)
            {
                var sign=signs[i];var eligibility=BuildingSignCompatibility.Evaluate(browserMount,sign);
                var b=Button(signGrid,"",(i%3)*224,(i/3)*164,216,156,()=>ChooseSign(sign.id));b.name="Sign · "+sign.id;b.interactable=eligibility.Eligible;
                if(sign.thumbnail){float thumbHeight=Mathf.Min(90,160f*sign.thumbnail.height/sign.thumbnail.width);float thumbWidth=thumbHeight*sign.thumbnail.width/sign.thumbnail.height;var im=Rect(b.transform,"Sign thumbnail",(216-thumbWidth)/2,6+(90-thumbHeight)/2,thumbWidth,thumbHeight).gameObject.AddComponent<RawImage>();im.texture=sign.thumbnail;im.raycastTarget=false;im.color=eligibility.Eligible?Color.white:new Color(.45f,.45f,.45f);}
                var label=Label(b.transform,sign.label,6,98,204,25,16,eligibility.Eligible?Cream:Muted);label.alignment=TextAnchor.MiddleCenter;
                var why=Label(b.transform,eligibility.Eligible?BuildingSignCompatibility.KindLabel(sign.kind)+" · Preview":eligibility.Reason,7,125,202,28,12,Muted);why.alignment=TextAnchor.MiddleCenter;
                signButtons.Add((b,sign.id));
            }
            if(signs.Length==0)Label(signGrid,"No matching signs. Try another search or show incompatible signs.",12,16,640,100,20,Muted);
            signCount.text=signs.Length+" shown · "+all.Count(s=>BuildingSignCompatibility.Evaluate(browserMount,s).Eligible)+" compatible · Scroll to browse";
            RefreshSignSelection();
        }
        void RefreshSignSelection()
        {
            if(browserMount==null)return;string id=BuildingSigns.Choice(Draft,browserMount.id);
            if(originalSignButton)originalSignButton.image.sprite=id==""?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
            if(noSignButton)noSignButton.image.sprite=id==BuildingSigns.None?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
            foreach(var b in signButtons)b.Button.image.sprite=b.Id==id?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
            if(selectedSignLabel)selectedSignLabel.text=id==""?"Original sign and lighting.":id==BuildingSigns.None?"No sign. Mounting hardware stays in place.":"Selected: "+(BuildingSigns.All().FirstOrDefault(s=>s.id==id)?.label??("Missing sign · "+id));
        }
        void ChooseSign(string id)
        {
            if(browserMount==null)return;
            var state=BuildingSigns.Choose(Draft,browserMount.id,id);
            try{BuildingSigns.ValidateChange(catalog.Find(buildings[current].assetId),Draft,state);}
            catch(System.IO.InvalidDataException e){status.text=e.Message;return;}
            drafts[buildings[current].id]=state;preview.Paint(state);RefreshSignSelection();RefreshPaint();
        }
        void SignCard(Transform p,float x,float y,float w,float h)
        {
            var card=Box(p,"Signage choices",x,y,w,h);Heading(card,"Signage",3,18,10,w-36);
            var selected=(Draft.signs??Array.Empty<BuildingSignSelection>()).FirstOrDefault().signId;
            string name=string.IsNullOrEmpty(selected)?"Original sign":selected==BuildingSigns.None?"No sign":BuildingSigns.All().FirstOrDefault(s=>s.id==selected)?.label??"Unavailable sign";
            Label(card,name,22,59,w-44,42,22);
            var browse=Button(card,"Browse signs…",22,h-54,w-44,36,()=>{tab=2;ShowTab();});browse.GetComponentInChildren<Text>().fontSize=19;
        }
        void HeightCard(Transform p,float x,float y,float w,float h)
        {var card=Box(p,"Building Height",x,y,w,h);Heading(card,"Building Height",4,20,10,400);Label(card,"Original height",68,57,280,36,21,Muted);for(int i=0;i<5;i++){var b=Button(card,(i+1)+"",420+i*110,33,96,48,null);b.interactable=false;}}
        void Fit(){if(!frame)return;var r=(RectTransform)transform;float scale=Mathf.Min(1,(r.rect.width-36)/1340,(r.rect.height-52)/860);frame.localScale=Vector3.one*Mathf.Max(.1f,scale);}
        void Update(){if(Input.GetKeyDown(KeyCode.Escape))cancel?.Invoke();}
        void LateUpdate(){Fit();preview?.Render();}
        void OnDestroy(){preview?.Dispose();if(ownsFont&&font)Destroy(font);foreach(var slice in slices)if(slice)Destroy(slice);}
    }
}

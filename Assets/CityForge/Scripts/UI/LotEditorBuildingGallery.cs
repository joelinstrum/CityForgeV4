using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using CityForge.World;
using static CityForge.UI.LotEditorWorkspace;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CityForge.UI
{
    internal static class BuildingGalleryPreferences
    {
        internal const string CharacterFavoritesKey = "CityForge.Characters.Favorites", CharacterUsedKey = "CityForge.Characters.Used";
        internal const string LandscapingFavoritesKey = "CityForge.Landscaping.Favorites", LandscapingUsedKey = "CityForge.Landscaping.Used";
        internal const string FenceFavoritesKey = "CityForge.Fences.Favorites", FenceUsedKey = "CityForge.Fences.Used";
        internal const string TreeFavoritesKey = "CityForge.Trees.Favorites", TreeUsedKey = "CityForge.Trees.Used";
        internal const string FavoritesKey = "CityForge.Buildings.Favorites", UsedKey = "CityForge.Buildings.Used";
        internal static HashSet<string> Read(string key) => new(PlayerPrefs.GetString(key).Split(new[]{'|'}, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        internal static void Save(string key, HashSet<string> values) { PlayerPrefs.SetString(key, string.Join("|", values.OrderBy(id=>id,StringComparer.Ordinal))); PlayerPrefs.Save(); }
    }
    internal sealed class BuildingGalleryDoubleClick : MonoBehaviour, IPointerClickHandler
    {
        internal Action Activate;
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && data.clickCount == 2 &&
                gameObject.activeInHierarchy && GetComponent<Button>().IsInteractable()) Activate?.Invoke();
        }
    }
    internal sealed partial class LotEditorPanel
    {
        private LotAssetThumbnails buildingGalleryThumbnails;
        private sealed class BuildingGalleryState
        {
            internal string era = "founders", category = "residential", subcategory = "", query = "", selected;
            internal int wealth, sort, size, page, rotation, season, tool, density;
            internal bool list, recent, favorites;
        }
        private string buildingGalleryContext;
        private BuildingGalleryState buildingGalleryState, treeGalleryState, fenceGalleryState, landscapingGalleryState, characterGalleryState;

        private static string FenceFamily(LotAssetCatalogEntry asset) => LotPropStyles.Find(asset.id)?.family ?? (string.IsNullOrEmpty(asset.group)?"Other fences":asset.group);
        private static string FencePart(LotAssetCatalogEntry asset) => LotPropStyles.Find(asset.id)?.part ?? (asset.fenceSpan>.1f?"Section":"Individual");
        private static LotDefinition FencePreview(LotAssetCatalogEntry asset)
        {
            var lot=new LotDefinition{id="fence-gallery:"+asset.id,name=asset.label};
            lot.objects.Add(new LotObjectDefinition{assetId=asset.id,fenceLength=asset.fenceSpan>.1f?asset.FencePlacementLength:0,fenceEndPost=asset.fenceSpan>.1f});return lot;
        }
        private void OpenBuildingGallery(CityForgeTimeline.Era initialEra, LotContentCatalog.Category initialCategory, string query, bool editingInfo)
        {
            var lot=session.Lot;
            string context=lot.id+":"+lot.firstEraId+":"+lot.firstEraYear+":"+lot.category;
            if(buildingGalleryState==null||buildingGalleryContext!=context)
            {
                buildingGalleryState=new BuildingGalleryState{era=LotClassification.Era(lot)?.id??"",category=LotClassification.FindCategory(lot.category)?.id??""};
                buildingGalleryContext=context;
            }
            OpenCatalogGallery(false,initialEra,initialCategory,query,editingInfo);
        }

        private static string CharacterGroup(LotAssetCatalogEntry asset) => asset.group=="Founders Era"?"Founder's Era":asset.group??"Other characters";

        private void OpenCharacterGallery() => OpenCatalogGallery(false,null,null,null,false,false,false,true);

        private void OpenLandscapingGallery() => OpenCatalogGallery(false,null,null,null,false,false,true);

        private void OpenTreeGallery() => OpenCatalogGallery(true, null, null, null, false);

        private void OpenFenceGallery()
        {
            var state=fenceGalleryState??=new BuildingGalleryState{era="",category=""};
            if(fencePath.Finished)state.tool=3;
            else if(choosingFenceForPath)state.tool=1;
            else if(state.tool==3)state.tool=0;
            OpenCatalogGallery(false,null,null,null,false,true);
        }

        private void OpenCatalogGallery(bool treeMode, CityForgeTimeline.Era initialEra, LotContentCatalog.Category initialCategory, string query, bool editingInfo, bool fenceMode = false, bool landscapingMode = false, bool characterMode = false)
        {
            CloseAssetPopup(); CloseChoices();
            bool groupedMode=landscapingMode||characterMode;
            var state = characterMode ? characterGalleryState ??= new BuildingGalleryState { era="",category="" } : landscapingMode ? landscapingGalleryState ??= new BuildingGalleryState { era="",category="" } : fenceMode ? fenceGalleryState ??= new BuildingGalleryState { era="",category="" } : treeMode ? treeGalleryState ??= new BuildingGalleryState { era = "", category = "" } : buildingGalleryState ??= new BuildingGalleryState();
            string singular=characterMode?"character":landscapingMode?"landscaping item":fenceMode?"fence":treeMode?"tree":"building", plural=characterMode?"characters":landscapingMode?"landscaping items":fenceMode?"fences":treeMode?"trees":"buildings", prefix=characterMode?"Character":landscapingMode?"Landscaping":fenceMode?"Fence":treeMode?"Tree":"Building";
            string favoritesKey=characterMode?BuildingGalleryPreferences.CharacterFavoritesKey:landscapingMode?BuildingGalleryPreferences.LandscapingFavoritesKey:fenceMode?BuildingGalleryPreferences.FenceFavoritesKey:treeMode?BuildingGalleryPreferences.TreeFavoritesKey:BuildingGalleryPreferences.FavoritesKey;
            if(initialEra!=null)state.era=initialEra.id;
            if(initialCategory!=null)state.category=initialCategory.id;
            if(!string.IsNullOrEmpty(query))state.query=query;
            var favorites=BuildingGalleryPreferences.Read(favoritesKey);
            var entries = catalog ? catalog.entries.Where(a=>a!=null&&a.prefab&&a.category==(characterMode?"Characters":landscapingMode?"Landscaping":fenceMode?"Fences":treeMode?"Trees":"Buildings") && (!treeMode || !LotAssetCatalog.IsRetiredTree(a.id)) && (!(treeMode||fenceMode||groupedMode)||!PlayerMode||LotCustomizationService.IsCosmetic(catalog,a.id))).ToArray() : Array.Empty<LotAssetCatalogEntry>();
            var frame=CreateModal("",1640,900);frame.gameObject.name=prefix+" gallery";frame.GetComponent<Image>().sprite=ZoneModalArtwork.Panel;frame.GetComponent<Image>().type=Image.Type.Sliced;frame.GetComponent<Image>().color=Color.white;frame.GetComponent<Image>().pixelsPerUnitMultiplier=2;
            frame.gameObject.AddComponent<GrowableModalFit>().Initialize(new Vector2(1640,900));
            foreach(Transform child in frame)child.gameObject.SetActive(false);
            buildingGalleryThumbnails=new LotAssetThumbnails(192,transparent:true,frontView:true,tightFraming:true);
            propertyThumbnails=new LotAssetThumbnails(512,transparent:true,frontView:true,tightFraming:true);
            if(!propertyFont)propertyFont=Font.CreateDynamicFontFromOSFont(new[]{"Georgia","Times New Roman"},24);
            var serif=propertyFont?propertyFont:headingFont; var cream=new Color(1,.91f,.73f);var muted=new Color(.75f,.79f,.75f);
            var panels=new List<GameObject>();LotAssetCatalogEntry selectedAsset=null;
            var categoryButtons=new List<(string id,Button button)>();var cards=new List<GameObject>();
            var cardButtons=new Dictionary<string,Button>();
            bool rebuilding=false;
            Text breadcrumb=null,pageLabel=null,nameLabel=null,classification=null,description=null,footprint=null,scale=null,availability=null;
            Button grid=null,list=null,previous=null,next=null,favorite=null,select=null,edit=null;
            RawImage preview=null;Dropdown subcategory=null, buildingWealth=null;RectTransform results=null;int pageCount=1;string previewId=null;
            var categories=characterMode ? new[]{("","All characters")}.Concat(CityForgeTimeline.Eras.Select(e=>e.Name).Concat(entries.Select(CharacterGroup).Distinct().Where(g=>!CityForgeTimeline.Eras.Any(e=>e.Name==g)).OrderBy(g=>g)).Select(g=>(g,g))).ToArray() : landscapingMode ? new[]{("","All landscaping")}.Concat(entries.Select(a=>a.group??"").Where(g=>g.Length>0).Distinct().OrderBy(g=>g).Select(g=>(g,g))).ToArray() : fenceMode ? new[]{("","All families")}.Concat(entries.Select(a=>FenceFamily(a)).Distinct().OrderBy(f=>f).Select(f=>(f,f))).ToArray() : treeMode ? new[]{("","All trees")}.Concat(entries.Select(a=>a.group??"").Where(group=>group.Length>0).Distinct().OrderBy(group=>group).Select(group=>(group,group))).ToArray() : new[]{("","All categories")}.Concat(LotClassification.Categories.Select(c=>(c.id,c.name))).ToArray();
            Text Caption(Transform parent,string value,float x,float y,float width,float height=32,int size=20,bool heading=false)
            {
                var label=Label(parent,value,x,y,width,size);At(label.rectTransform,x,y,width,height);label.font=heading?serif:font;label.color=cream;label.supportRichText=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;label.raycastTarget=false;return label;
            }
            Image Box(string title,float x,float y,float w,float h)
            {
                var image=AddImage(title,frame,Color.white);image.sprite=ZoneModalArtwork.Section;image.type=Image.Type.Sliced;At(image.rectTransform,x,y,w,h);return image;
            }
            Button Action(Transform parent,string label,float x,float y,float w,float h,Action action,bool primary=false)
            {
                var button=Control(parent,label,x,y,w,action);At((RectTransform)button.transform,x,y,w,h);button.image.sprite=primary?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;button.image.type=Image.Type.Sliced;button.image.pixelsPerUnitMultiplier=3;button.image.color=Color.white;
                var text=button.GetComponentInChildren<Text>();text.font=serif;text.fontSize=20;text.color=cream;return button;
            }
            Dropdown Menu(string title,string[] labels,int value,float x,float y,float w,Action<int> change)
            {
                var obj=DefaultControls.CreateDropdown(new DefaultControls.Resources());obj.name=title;obj.transform.SetParent(frame,false);At((RectTransform)obj.transform,x,y,w,36);
                var menu=obj.GetComponent<Dropdown>();menu.image.sprite=ZoneModalArtwork.Dark;menu.image.type=Image.Type.Sliced;menu.image.pixelsPerUnitMultiplier=3;menu.image.color=Color.white;menu.ClearOptions();menu.AddOptions(labels.ToList());
                foreach(var text in obj.GetComponentsInChildren<Text>(true)){text.font=font;text.fontSize=18;text.color=cream;}
                foreach(var image in obj.GetComponentsInChildren<Image>(true))image.color=new Color(.07f,.14f,.18f);
                // DefaultControls' 20-pixel item is too short for this font: Unity
                // truncates the entire line. Give every cloned option a real text row.
                var popupImage=menu.template.GetComponent<Image>();popupImage.sprite=ZoneModalArtwork.Section;popupImage.type=Image.Type.Sliced;popupImage.pixelsPerUnitMultiplier=3;popupImage.color=Color.white;AddOutline(popupImage,Gold);
                var row=menu.template.GetComponentInChildren<Toggle>(true);
                var rowRect=(RectTransform)row.transform;rowRect.sizeDelta=new Vector2(rowRect.sizeDelta.x,36);
                var content=menu.template.GetComponent<ScrollRect>().content;content.sizeDelta=new Vector2(content.sizeDelta.x,36);
                menu.itemText.rectTransform.anchorMin=Vector2.zero;menu.itemText.rectTransform.anchorMax=Vector2.one;
                menu.itemText.rectTransform.offsetMin=new Vector2(30,0);menu.itemText.rectTransform.offsetMax=new Vector2(-8,0);
                menu.itemText.alignment=TextAnchor.MiddleLeft;menu.itemText.verticalOverflow=VerticalWrapMode.Overflow;
                if(row.graphic)row.graphic.color=Gold;
                var arrow=obj.transform.Find("Arrow");if(arrow)arrow.gameObject.SetActive(false);Caption(obj.transform,"▾",w-30,2,24,30,20);
                menu.template.sizeDelta=new Vector2(menu.template.sizeDelta.x,220);menu.SetValueWithoutNotify(Mathf.Max(0,value));menu.onValueChanged.AddListener(v=>{if(!rebuilding)change(v);});return menu;
            }
            RawImage Picture(Transform parent,string name,Texture texture,float x,float y,float w,float h)
            {
                var image=new GameObject(name,typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();image.transform.SetParent(parent,false);if(name!="Colonial harbor panorama"&&name!="Buildings emblem"){float side=Mathf.Min(w,h);At(image.rectTransform,x+(w-side)*.5f,y+(h-side)*.5f,side,side);}else At(image.rectTransform,x,y,w,h);image.texture=texture;if((treeMode||fenceMode||groupedMode)&&texture is RenderTexture)image.uvRect=buildingGalleryThumbnails.VisibleUV(texture);image.raycastTarget=false;image.color=texture?Color.white:Color.clear;return image;
            }
            Box("Gallery header",20,18,1600,92);
            var harbor=Resources.Load<Texture2D>(characterMode?"UI/LotCharacterGallery/Harbor":landscapingMode?"UI/LotLandscapingGallery/Landscape":fenceMode?"UI/LotFenceGallery/Landscape":treeMode?"UI/LotTreeGallery/Landscape":"UI/LotBuildingGallery/Harbor");var banner=Picture(frame,"Colonial harbor panorama",harbor,380,20,1220,88);banner.uvRect=new Rect(0,.34f,1,.24f);
            var veil=AddImage("Header shade",frame,new Color(.02f,.045f,.055f,.48f));At(veil.rectTransform,380,20,1220,88);
            var emblem=Picture(frame,"Buildings emblem",Resources.Load<Texture2D>(characterMode?"UI/LotCharacterGallery/Portrait":landscapingMode?"UI/LotLandscapingGallery/Garden":fenceMode?"UI/LotFenceGallery/Fence":treeMode?"UI/LotTreeGallery/Tree":"UI/LotBuildingGallery/Building"),34,24,85,76);if(fenceMode)emblem.uvRect=new Rect(0,0,.4f,1);
            Caption(frame,characterMode?"Characters":landscapingMode?"Landscaping":fenceMode?"Fences":treeMode?"Trees":"Buildings",136,26,690,48,43,true);Caption(frame,characterMode?"Choose the people who bring your lot to life.":landscapingMode?"Gardens, greenery, and finishing touches for your lot.":fenceMode?"Choose a fence, draw a run, or enclose your lot.":treeMode?"Select, filter, and plant trees for your lot.":"Select, filter, and place buildings for your lot.",138,76,1000,27,20);
            Action(frame,"×  Close",1460,36,140,52,CloseAssetPopup);
            Action(frame,"← Back",24,123,116,36,CloseAssetPopup);
            breadcrumb=Caption(frame,"",160,124,1420,34,23,true);
            Box("Building filters",20,174,300,704);Box("Building results",332,174,884,704);Box("Building details",1228,174,392,704);
            Caption(frame,characterMode?"CHARACTER ERA":landscapingMode?"LANDSCAPING GROUP":fenceMode?"PLACEMENT":treeMode?"PREVIEW SEASON":"LOT ERA",38,188,262,28,22,true);
            var eras=new[]{""}.Concat(CityForgeTimeline.Eras.Select(e=>e.id)).ToArray();
            if(groupedMode)Menu(characterMode?"Character era":"Landscaping group",categories.Select(c=>c.Item2).ToArray(),Array.FindIndex(categories,c=>c.Item1==state.category),38,223,264,v=>{state.category=categories[v].Item1;state.page=0;Render();});
            else if(fenceMode)Menu("Fence placement",fencePath.Finished?new[]{"Place individual piece","Draw fence run","Surround lot","Fill current guide"}:new[]{"Place individual piece","Draw fence run","Surround lot"},state.tool,38,223,264,v=>{state.tool=v;state.page=0;Render();});
            else if(treeMode)Menu("Tree preview season",new[]{"Summer","Autumn","Winter"},state.season,38,223,264,v=>{state.season=v;Render();});
            else Caption(frame,LotClassification.Era(session.Lot)?.Name??"Not specified",38,223,264,36,23);
            if(!groupedMode)Caption(frame,fenceMode?"FAMILY":treeMode?"TREE GROUP":"CATEGORY",38,274,262,30,22,true);
            var treeIcons=treeMode?Resources.Load<Texture2D>("UI/LotTreeGallery/Icons"):null;
            if(fenceMode)Menu("Fence family",categories.Select(c=>c.Item2).ToArray(),Array.FindIndex(categories,c=>c.Item1==state.category),38,310,264,v=>{state.category=categories[v].Item1;state.page=0;Render();});
            else if(!groupedMode)for(int i=0;i<categories.Length;i++)
            {
                var category=categories[i];var button=Action(frame,category.Item2,38,310+i*34,264,32,()=>{state.category=category.Item1;state.subcategory="";state.density=0;state.wealth=0;state.page=0;Render();});categoryButtons.Add((category.Item1,button));
                var text=button.GetComponentInChildren<Text>();text.alignment=TextAnchor.MiddleLeft;text.rectTransform.offsetMin=new Vector2(42,0);
                if(i>0)
                {
                    if(treeMode&&treeIcons)
                    {
                        int cell=category.Item1 switch { "Deadwood"=>0, "Evergreen"=>2, "Tropical"=>3, _=>1 };
                        var icon=Picture(button.transform,"Tree group icon",treeIcons,5,1,30,30);
                        var sourceRects=new[]{new Rect(100,135,220,310),new Rect(360,135,280,310),new Rect(660,135,240,310),new Rect(925,135,270,310)};
                        var source=sourceRects[cell];
                        // Normalize against the source atlas; Unity may rescale NPOT textures.
                        icon.uvRect=new Rect(source.x/1774f,(887-source.yMax)/887f,source.width/1774f,source.height/887f);
                    }
                    else{var glyph=treeMode?LotGlyph.Tree:new[]{LotGlyph.House,LotGlyph.Shop,LotGlyph.Catalog,LotGlyph.Industry,LotGlyph.Transport}[i-1];AddGlyph(button.transform,glyph,7,3,26);button.GetComponentInChildren<LotEditorIcon>().color=Gold;}
                }
            }
            if(groupedMode)
            {
                Caption(frame,"PLACEMENT",38,292,264,30,22,true);
                Caption(frame,characterMode?"Choose a character, then click on your lot to place them. Double-click a card to start placing immediately.":"Choose an item, then click on your lot to place it. Double-click a card to start placing immediately.",38,338,264,112,20);
                Caption(frame,characterMode?"Browse by era, search for a character, or save your favorites for next time.":"Browse the existing collection by group, search, or save your favorites for next time.",38,470,264,112,19);
            }
            else if(fenceMode)
            {
                var parts=new[]{""}.Concat(entries.Select(FencePart).Distinct().OrderBy(part=>part)).ToArray();
                Caption(frame,"PIECE TYPE",38,366,264,30,21,true);
                Menu("Fence piece",parts.Select(part=>part==""?"All pieces":part).ToArray(),Array.IndexOf(parts,state.subcategory),38,402,264,v=>{state.subcategory=parts[v];state.page=0;Render();});
                Caption(frame,"WEALTH / FINISH",38,458,264,30,21,true);
                Menu("Fence wealth",new[]{"All finishes","$ · Rustic","$$ · Plain","$$$ · Fancy"},state.wealth,38,494,264,v=>{state.wealth=v;state.page=0;Render();});
                Action(frame,"Select fences on this lot",38,555,264,36,SelectAllFenceSections);
                var clear=Action(frame,"Clear unbuilt guide",38,603,264,36,()=>{ClearFencePath();state.tool=0;OpenCatalogGallery(false,null,null,null,false,true);});
                clear.interactable=fencePath.Points.Count>0;
            }
            else if(treeMode)
            {
                Caption(frame,"PLACEMENT OPTIONS",38,500,264,30,21,true);
                var sizeVariation=Checkbox(frame,"Vary tree size","Vary placement size (±15%)",38,541,264,varyPlacementSize);
                sizeVariation.onValueChanged.AddListener(value=>varyPlacementSize=value);
                var randomize=Checkbox(frame,"Randomize tree group","Randomize within group\nwhen lot is placed in-game",38,582,264,randomizeTreeSpecies,56);
                randomize.onValueChanged.AddListener(value=>randomizeTreeSpecies=value);
            }
            else
            {
            Caption(frame,"DENSITY & WEALTH",38,524,264,29,20,true);
            subcategory=Menu("Building density",new[]{"All densities","One","Two","Three"},state.density,38,561,264,v=>{state.density=v;state.page=0;Render();});
            buildingWealth=Menu("Building wealth",new[]{"All wealth levels","Low wealth","Medium wealth","High wealth"},state.wealth,38,603,264,v=>{state.wealth=v;state.page=0;Render();});
            }
            var recent=Checkbox(frame,"Recently used "+plural,"Recently used",38,653,264,state.recent);recent.onValueChanged.AddListener(v=>{state.recent=v;state.page=0;Render();});
            var favoriteFilter=Checkbox(frame,"Favorite "+plural,"Favorites",38,691,264,state.favorites);favoriteFilter.onValueChanged.AddListener(v=>{state.favorites=v;state.page=0;Render();});
            bool buildingMode=!treeMode&&!fenceMode&&!groupedMode;
            if(buildingMode)
            {
                Caption(frame,"FILTER BY ERA",38,729,264,18,16,true);
                Menu("Building era",eras.Select(id=>string.IsNullOrEmpty(id)?"All eras":CityForgeTimeline.Find(id).Name).ToArray(),Array.IndexOf(eras,state.era),38,749,264,v=>{state.era=eras[v];state.page=0;Render();});
            }
            var variation=Checkbox(frame,"Vary placement size","Vary placement size (±15%)",38,buildingMode?789:767,264,varyPlacementSize);variation.onValueChanged.AddListener(v=>varyPlacementSize=v);variation.gameObject.SetActive(!treeMode&&!fenceMode&&!characterMode);
            Action(frame,"× Clear filters",38,826,264,36,()=>{state.era="";state.category="";state.subcategory="";state.query="";state.wealth=state.density=0;state.recent=state.favorites=false;state.page=0;OpenCatalogGallery(treeMode,null,null,null,editingInfo,fenceMode,landscapingMode,characterMode);});
            var search=Field(frame,"Search "+plural+"…",348,190,364);search.SetTextWithoutNotify(state.query);search.onValueChanged.AddListener(v=>{state.query=v;state.page=0;Render();});
            Menu(prefix+" sort",characterMode?new[]{"Name (A–Z)","Name (Z–A)","Recently used"}:fenceMode?new[]{"Name (A–Z)","Name (Z–A)","Recently used","Shortest sections","Longest sections"}:treeMode?new[]{"Name (A–Z)","Name (Z–A)","Recently used","Shortest trees","Tallest trees"}:new[]{"Name (A–Z)","Name (Z–A)","Recently used","Smallest footprint","Largest footprint"},state.sort,724,190,220,v=>{state.sort=v;state.page=0;Render();});
            grid=Action(frame,"▦",956,190,44,36,()=>{state.list=false;state.page=0;Render();});list=Action(frame,"☰",1006,190,44,36,()=>{state.list=true;state.page=0;Render();});
            var slider=DefaultControls.CreateSlider(new DefaultControls.Resources()).GetComponent<Slider>();slider.name=prefix+" thumbnail size";slider.transform.SetParent(frame,false);At((RectTransform)slider.transform,1070,198,122,22);slider.minValue=0;slider.maxValue=2;slider.wholeNumbers=true;slider.value=state.size;foreach(var image in slider.GetComponentsInChildren<Image>())image.color=image.name is "Handle" or "Fill"?Gold:new Color(.12f,.19f,.22f);slider.onValueChanged.AddListener(v=>{state.size=(int)v;state.page=0;Render();});
            Caption(frame,"Thumbnail size",1063,223,140,18,14);
            results=AddImage("Building cards",frame,Color.clear).rectTransform;At(results,348,252,852,548);
            pageLabel=Caption(frame,"",480,826,540,36,18);pageLabel.alignment=TextAnchor.MiddleCenter;
            previous=Action(frame,"‹ Previous",348,826,130,36,()=>{state.page--;Render();});next=Action(frame,"Next ›",1068,826,132,36,()=>{state.page++;Render();});
            preview=Picture(frame,"Selected "+singular+" preview",null,1248,194,352,252);
            nameLabel=Caption(frame,"Choose a "+singular,1248,455,352,53,28,true);
            classification=Caption(frame,"",1248,512,352,44,19);description=Caption(frame,"",1248,557,352,90,18);
            footprint=Caption(frame,"",1248,659,352,34,20);scale=Caption(frame,"",1248,698,352,30,19);
            availability=Caption(frame,"",1248,733,352,45,17);
            favorite=Action(frame,"☆ Favorite",1248,821,142,40,ToggleFavorite);
            select=Action(frame,characterMode?"Select Character":landscapingMode?"Select Item":fenceMode?"Place Piece":treeMode?"Select Tree":editingInfo&&!PlayerMode?"Edit Properties":"Select Building",1402,821,198,40,Select,true);
            edit=Action(frame,"Edit Building Properties…",1248,778,352,34,()=>{if(selectedAsset!=null)Properties();});edit.gameObject.SetActive(!fenceMode&&!treeMode&&!groupedMode&&!PlayerMode);
            Action(frame,"↶",1254,403,44,34,()=>Rotate(-1));Action(frame,"↷",1550,403,44,34,()=>Rotate(1));
            Action(frame,"↗",1558,200,36,34,Expand);
            Render();

            bool InEra(LotAssetCatalogEntry asset)
            {
                return groupedMode||fenceMode||treeMode||asset.IsAvailableInEra(state.era);
            }
            void Render()
            {
                rebuilding=true;
                bool densityApplies=state.category==""||state.category is "residential" or "commercial" or "industrial";
                if(!treeMode&&!fenceMode&&!groupedMode)
                {
                    subcategory.interactable=buildingWealth.interactable=densityApplies;
                    if(!densityApplies){state.density=state.wealth=0;subcategory.SetValueWithoutNotify(0);buildingWealth.SetValueWithoutNotify(0);}
                }
                foreach(var item in categoryButtons)
                {
                    int count=entries.Count(a=>InEra(a)&&(item.id==""||(fenceMode?FenceFamily(a):characterMode?CharacterGroup(a):(treeMode||landscapingMode)?a.group:LotClassification.FindCategory(a.group)?.id)==item.id));
                    item.button.GetComponentInChildren<Text>().text=(categories.First(c=>c.Item1==item.id).Item2)+" ("+count+")";item.button.image.sprite=item.id==state.category?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
                }
                var filtered=entries.Where(a=>InEra(a)&&(state.category==""||(fenceMode?FenceFamily(a):characterMode?CharacterGroup(a):(treeMode||landscapingMode)?a.group:LotClassification.FindCategory(a.group)?.id)==state.category)&&
                    (fenceMode?(state.subcategory==""||FencePart(a)==state.subcategory):(groupedMode||treeMode||state.density==0||a.Density==state.density))&&(groupedMode||treeMode||state.wealth==0||(fenceMode?(int)(LotPropStyles.Find(a.id)?.wealth??LotWealth.None):(int)a.growableWealth)==state.wealth)&&
                    (!fenceMode||state.tool==0||a.fenceSpan>.1f)&&
                    (!state.recent||recentAssets.Contains(a.id))&&(!state.favorites||favorites.Contains(a.id))&&
                    (string.IsNullOrWhiteSpace(state.query)||(a.label+" "+a.description+" "+(fenceMode?FenceFamily(a)+" "+FencePart(a):a.group)+" "+a.GrowableLabel+" "+LotClassification.SubcategoryName(state.category,a.subcategory)).IndexOf(state.query.Trim(),StringComparison.OrdinalIgnoreCase)>=0));
                filtered=state.sort switch {1=>filtered.OrderByDescending(a=>a.label,StringComparer.OrdinalIgnoreCase).ThenBy(a=>a.id),2=>filtered.OrderBy(a=>recentAssets.Contains(a.id)?recentAssets.IndexOf(a.id):int.MaxValue).ThenBy(a=>a.label),3=>filtered.OrderBy(a=>(fenceMode?(a.fenceSpan>.1f?a.FencePlacementLength:0):treeMode?LotRuntimeLoader.TreePlacementHeight(a):a.footprint.x*a.footprint.y*a.DefaultPlacementScale*a.DefaultPlacementScale)).ThenBy(a=>a.label),4=>filtered.OrderByDescending(a=>(fenceMode?(a.fenceSpan>.1f?a.FencePlacementLength:0):treeMode?LotRuntimeLoader.TreePlacementHeight(a):a.footprint.x*a.footprint.y*a.DefaultPlacementScale*a.DefaultPlacementScale)).ThenBy(a=>a.label),_=>filtered.OrderBy(a=>a.label,StringComparer.OrdinalIgnoreCase).ThenBy(a=>a.id)};
                var choices=filtered.ToArray();int cols=state.size==0?4:state.size==1?3:2;int rows=state.size==0?3:2;int perPage=state.list?9:cols*rows;pageCount=Mathf.Max(1,Mathf.CeilToInt(choices.Length/(float)perPage));state.page=Mathf.Clamp(state.page,0,pageCount-1);
                foreach(var card in cards){card.SetActive(false);Destroy(card);}cards.Clear();cardButtons.Clear();
                selectedAsset=choices.FirstOrDefault(a=>a.id==state.selected)??choices.FirstOrDefault();state.selected=selectedAsset?.id;
                var visible=choices.Skip(state.page*perPage).Take(perPage).ToArray();
                for(int i=0;i<visible.Length;i++)
                {
                    var asset=visible[i];float w=state.list?852:(852-(cols-1)*12)/(float)cols;float h=state.list?54:(548-(rows-1)*12)/(float)rows;float x=state.list?0:i%cols*(w+12),y=state.list?i*60:i/cols*(h+12);
                    var card=Action(results,"",x,y,w,h,()=>Choose(asset));cardButtons.Add(asset.id,card);card.gameObject.AddComponent<BuildingGalleryDoubleClick>().Activate=()=>{Choose(asset);Select();};card.gameObject.name=prefix+" card · "+asset.id;cards.Add(card.gameObject);card.image.sprite=ZoneModalArtwork.Dark;var outline=card.GetComponent<Outline>();if(outline){outline.effectColor=asset==selectedAsset?Gold:Stroke;outline.effectDistance=asset==selectedAsset?new Vector2(2,-2):new Vector2(1,-1);}
                    Picture(card.transform,asset.label,fenceMode?buildingGalleryThumbnails.GetLot(FencePreview(asset),catalog):buildingGalleryThumbnails.Get(asset,treeMode?(RegionSeason)state.season:RegionSeason.Summer),state.list?6:6,state.list?3:3,state.list?48:w-12,state.list?48:h-45);
                    Caption(card.transform,asset.label,state.list?68:10,state.list?5:h-43,state.list?w-130:w-20,state.list?24:23,state.list?19:16,true);
                    Caption(card.transform,fenceMode?FencePart(asset):characterMode?CharacterGroup(asset):(treeMode||landscapingMode)?asset.group:asset.growableZone==TownZone.None?LotClassification.FindCategory(asset.group)?.name:asset.GrowableLabel,state.list?68:10,state.list?30:h-20,state.list?w-130:w-53,22,14);
                    Action(card.transform,favorites.Contains(asset.id)?"★":"☆",w-36,h-29,30,26,()=>{if(!favorites.Add(asset.id))favorites.Remove(asset.id);BuildingGalleryPreferences.Save(favoritesKey,favorites);Render();});
                }
                if(choices.Length==0){var empty=Caption(results,characterMode&&state.category!=""&&!entries.Any(a=>CharacterGroup(a)==state.category)?"No characters are available for "+state.category+" yet.\nChoose another era or All characters.":fenceMode?"No compatible pieces match this mode.\nTry another family, piece type, or finish.":"No "+plural+" match these filters.\nTry another group, clear the search, or clear filters.",60,190,732,100,24);empty.alignment=TextAnchor.MiddleCenter;cards.Add(empty.gameObject);}
                breadcrumb.text=characterMode?"Characters  ›  "+(state.category==""?"All eras":state.category):landscapingMode?"Landscaping  ›  "+(state.category==""?"All groups":state.category):fenceMode?"Fences  ›  "+(state.category==""?"All families":state.category):treeMode?"Trees  ›  "+(string.IsNullOrEmpty(state.category)?"All groups":state.category):(CityForgeTimeline.Find(state.era)?.Name??"All eras")+"  ›  "+(LotClassification.FindCategory(state.category)?.name??"All categories");
                pageLabel.text=$"Page {state.page+1} of {pageCount}   ·   {choices.Length} {plural}";previous.interactable=state.page>0;next.interactable=state.page+1<pageCount;grid.image.sprite=!state.list?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;list.image.sprite=state.list?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
                if(fenceMode)select.GetComponentInChildren<Text>().text=new[]{"Place Piece","Draw Fence Run","Surround Lot","Fill Guide"}[state.tool];
                Details();rebuilding=false;
            }
            void Choose(LotAssetCatalogEntry asset)
            {
                state.selected=asset.id;state.rotation=0;selectedAsset=asset;
                // Keep the clicked card alive so Unity can recognize the second click.
                foreach(var pair in cardButtons)
                    if(pair.Value.TryGetComponent<Outline>(out var outline))
                    { outline.effectColor=pair.Key==asset.id?Gold:Stroke;outline.effectDistance=pair.Key==asset.id?new Vector2(2,-2):new Vector2(1,-1); }
                Details();
            }
            void Details()
            {
                select.interactable=favorite.interactable=edit.interactable=selectedAsset!=null;
                if(selectedAsset==null){preview.texture=null;preview.color=Color.clear;nameLabel.text="No "+singular+" selected";classification.text=description.text=footprint.text=scale.text=availability.text="";return;}
                if(previewId!=selectedAsset.id+":"+state.season){propertyThumbnails.Dispose();propertyThumbnails=new LotAssetThumbnails(512,transparent:true,frontView:true,tightFraming:true);previewId=selectedAsset.id+":"+state.season;}
                nameLabel.resizeTextForBestFit=true;nameLabel.resizeTextMinSize=18;nameLabel.resizeTextMaxSize=28;nameLabel.text=selectedAsset.label;classification.text=(LotClassification.FindCategory(selectedAsset.group)?.name??selectedAsset.group)+(selectedAsset.growableWealth==LotWealth.None?(selectedAsset.Density==0?"":" · Wealth unspecified"):" · "+selectedAsset.growableWealth+" wealth");
                description.text=string.IsNullOrWhiteSpace(selectedAsset.description)?"No description has been added for this building.":selectedAsset.description;
                if(fenceMode)
                {
                    var style=LotPropStyles.Find(selectedAsset.id);
                    classification.text=FenceFamily(selectedAsset)+(style==null?"":" · "+new string('$',(int)style.wealth));
                    description.text=state.tool==0?"Place one piece. Gates, corners and decorative panels are placed individually.":"Straight sections follow your guide. Gates and corners can be added individually afterward.";
                    footprint.text=selectedAsset.fenceSpan>.1f?$"Run section length   {selectedAsset.FencePlacementLength:0.##} m":"Individual placement piece";
                    scale.text="Piece type   "+FencePart(selectedAsset);
                    availability.text=state.tool==3?$"Ready guide · {fencePath.Points.Count} markers · "+(fencePath.Closed?"Closed loop":"Open run"):state.tool==2?"Uses the lot boundary with the existing sidewalk inset.":state.tool==1?"Click markers · Enter finishes an open run · Click first marker to close.":fencePath.Finished?"Place Piece clears the current unbuilt guide.":"Fence height stays authored; spans fit the selected route.";
                    favorite.GetComponentInChildren<Text>().text=favorites.Contains(selectedAsset.id)?"★ Favorited":"☆ Favorite";Rotate(0);return;
                }
                if(treeMode)
                {
                    classification.text=selectedAsset.group+(selectedAsset.IsSeasonalTree?" · Seasonal models":" · Authored model");
                    description.text=string.IsNullOrWhiteSpace(selectedAsset.description)?"Choose a spot on the lot, then click to plant this tree.":selectedAsset.description;
                    footprint.text=$"Height at placement   {LotRuntimeLoader.TreePlacementHeight(selectedAsset):0.##} m";
                    var size=LotRuntimeLoader.ObjectFootprintSize(new LotObjectDefinition{assetId=selectedAsset.id,scale=1},selectedAsset);
                    scale.text=$"Placement footprint   {size.x:0.##} × {size.y:0.##} m";
                    availability.text="Season preview only. Planted trees follow the district season.";
                    favorite.GetComponentInChildren<Text>().text=favorites.Contains(selectedAsset.id)?"★ Favorited":"☆ Favorite";Rotate(0);return;
                }
                if(characterMode)
                {
                    classification.text=CharacterGroup(selectedAsset);
                    description.text=string.IsNullOrWhiteSpace(selectedAsset.description)?"Select this character, then click on the lot to place them.":selectedAsset.description;
                    footprint.text="Placement   On-lot character";
                    scale.text="Starting pose   "+(string.IsNullOrEmpty(selectedAsset.idleState)?"Default":selectedAsset.idleState);
                    availability.text="Double-click to select · Use the arrows to inspect every side.";
                    favorite.GetComponentInChildren<Text>().text=favorites.Contains(selectedAsset.id)?"★ Favorited":"☆ Favorite";Rotate(0);return;
                }
                if(landscapingMode)
                {
                    classification.text=selectedAsset.group;
                    description.text=string.IsNullOrWhiteSpace(selectedAsset.description)?"Choose a spot on the lot, then click to place this item.":selectedAsset.description;
                    var size=LotRuntimeLoader.ObjectFootprintSize(new LotObjectDefinition{assetId=selectedAsset.id,scale=1},selectedAsset);
                    footprint.text=$"Placement footprint   {size.x:0.##} × {size.y:0.##} m";
                    scale.text=$"Default placement scale   {selectedAsset.DefaultPlacementScale*100:0.##}%";
                    availability.text="Double-click to place · Use the arrows to inspect every side.";
                    favorite.GetComponentInChildren<Text>().text=favorites.Contains(selectedAsset.id)?"★ Favorited":"☆ Favorite";Rotate(0);return;
                }
                var dimensions=selectedAsset.footprint*selectedAsset.DefaultPlacementScale;footprint.text=$"Footprint   {dimensions.x:0.##} × {dimensions.y:0.##} m";scale.text=$"Default placement scale   {selectedAsset.DefaultPlacementScale*100:0.##}%";
                var era=CityForgeTimeline.Find(selectedAsset.eraId);availability.text=(selectedAsset.additionalEraIds?.Length??0)>0?"Available in   "+string.Join(" · ",CityForgeTimeline.Eras.Where(e=>selectedAsset.IsAvailableInEra(e.id)).Select(e=>e.Name)):era==null?$"Available   {selectedAsset.fromYear}–{selectedAsset.toYear}":$"{era.Name} · {era.FirstYear}–{era.LastYear}";
                favorite.GetComponentInChildren<Text>().text=favorites.Contains(selectedAsset.id)?"★ Favorited":"☆ Favorite";Rotate(0);
            }
            void Rotate(int delta)
            {
                if(selectedAsset==null)return;state.rotation=(state.rotation+delta+8)%8;var lot=fenceMode?FencePreview(selectedAsset):new LotDefinition{id="gallery:"+selectedAsset.id};if(!fenceMode)lot.objects.Add(new LotObjectDefinition{assetId=selectedAsset.id});preview.texture=propertyThumbnails.GetLot(lot,catalog,state.rotation,treeMode?(RegionSeason)state.season:RegionSeason.Summer);preview.color=Color.white;preview.uvRect=(treeMode||fenceMode||groupedMode)?propertyThumbnails.VisibleUV(preview.texture):new Rect(0,0,1,1);
            }
            void ToggleFavorite(){if(selectedAsset==null)return;if(!favorites.Add(selectedAsset.id))favorites.Remove(selectedAsset.id);BuildingGalleryPreferences.Save(favoritesKey,favorites);Render();}
            void Properties(){var asset=selectedAsset;OpenBuildingProperties(asset,()=>OpenBuildingGallery(null,null,null,editingInfo));}
            void Select()
            {
                if(selectedAsset==null)return;
                if(fenceMode)
                {
                    if(state.tool!=0&&selectedAsset.fenceSpan<=.1f)return;
                    if(state.tool==1)ArmFenceRun(selectedAsset);
                    else if(state.tool==2)SurroundLotWithSelectedFence(selectedAsset);
                    else if(state.tool==3)FillFencePath(selectedAsset);
                    else{ClearFencePath();ArmAsset(selectedAsset);}
                    return;
                }
                if(editingInfo&&!PlayerMode)Properties();else ArmAsset(selectedAsset);
            }
            void Expand()
            {
                if(selectedAsset==null)return;var overlay=AddImage("Expanded building preview",frame,new Color(.01f,.025f,.035f,.97f));At(overlay.rectTransform,20,18,1600,860);overlay.raycastTarget=true;
                var picture=Picture(overlay.transform,"Expanded selected building",preview.texture,450,82,700,700);Caption(overlay.transform,selectedAsset.label,50,24,1300,48,34,true);
                Action(overlay.transform,"Close preview",1380,24,180,44,()=>Destroy(overlay.gameObject));Action(overlay.transform,"↶ Rotate",440,790,200,42,()=>{Rotate(-1);picture.texture=preview.texture;picture.uvRect=preview.uvRect;});Action(overlay.transform,"Rotate ↷",960,790,200,42,()=>{Rotate(1);picture.texture=preview.texture;picture.uvRect=preview.uvRect;});
            }
        }
    }
}

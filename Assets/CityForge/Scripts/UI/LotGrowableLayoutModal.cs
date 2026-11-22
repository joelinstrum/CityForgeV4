using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using CityForge.World;
using UnityEngine;
using UnityEngine.UI;
using static CityForge.UI.LotEditorWorkspace;
namespace CityForge.UI
{
    internal sealed partial class LotEditorPanel
    {
        private void OpenZoningLayout()
        {
            if (assetPopup || PlayerMode) return;
            CloseChoices();
            var lot = session.Lot;
            var layout = lot.zoningLayout;
            bool levelLot=lot.levelLot;
            int zone = layout.Configured ? (int)layout.zone - 1 : 0;
            int use = zone / 3, density = zone % 3, wealth = layout.wealth != LotWealth.None ? (int)layout.wealth - 1 : 0;
            var slot = layout.Configured ? layout.BuildingSlot : new Rect(lot.LandBounds.center - lot.LandBounds.size * .25f,
                lot.LandBounds.size * .5f);
            var building = Selection;
            var asset = building != null && catalog ? catalog.Find(building.assetId) : null;
            var selectedBuildings=lot.objects.Where(item=>(item==building||selectedGroup.Contains(item.id))&&catalog.Find(item.assetId)?.category=="Buildings").ToArray();
            const float modalWidth=1000;
            float extra=selectedBuildings.Length>0?42:0;
            var frame=CreateModal("Growable Lot Layout",modalWidth,1156+extra);
            var background=frame.GetComponent<Image>();
            background.sprite=ZoneModalArtwork.Panel;background.type=Image.Type.Sliced;background.color=new Color(.66f,.85f,1f);
            foreach(var outline in frame.GetComponents<Outline>())outline.enabled=false;
            frame.gameObject.AddComponent<GrowableModalFit>().Initialize(new Vector2(modalWidth,1156+extra));
            var title=frame.GetComponentsInChildren<Text>().First(text=>text.text=="Growable Lot Layout");
            At(title.rectTransform,36,13,740,48);title.fontSize=35;title.color=new Color32(247,234,205,255);
            var close=frame.GetComponentsInChildren<Button>().First(button=>button.name=="Close");
            At((RectTransform)close.transform,834,20,132,44);close.GetComponentInChildren<Text>().text="Close  ×";StyleButton(close,false,21);
            Label(frame,"Define how this lot grows in game.",40,66,850,22);

            Group("Building Type",24,126,952,230,230);
            Label(frame,"Zone Use",48,150,148,21);
            Choices(new[]{"Residential","Commercial","Industrial"},206,150,new[]{238,238,238},use,value=>use=value,
                new[]{LotGlyph.House,LotGlyph.Shop,LotGlyph.Industry});
            Rule(48,216,904);
            Label(frame,"Density",48,233,116,21);
            Choices(new[]{"1","2","3"},171,233,new[]{86,86,86},density,value=>density=value);
            VerticalRule(490,233,44);
            Label(frame,"Wealth",520,233,100,21);
            Choices(new[]{"$","$$","$$$"},637,233,new[]{94,94,94},wealth,value=>wealth=value);
            Rule(48,292,904);
            Label(frame,"Layout",48,308,148,21);
            bool buildingRow=layout.buildingRow;
            var singleOnly=new List<Selectable>();var rowOnly=new List<Selectable>();
            Text modeHint=null;
            Choices(new[]{"Single Building","Building Row"},206,307,new[]{346,382},buildingRow?1:0,
                value=>{buildingRow=value==1;RefreshMode();},new[]{LotGlyph.House,LotGlyph.BuildingRow});

            Group("Building Footprint",24,390,952,132+extra,278);
            Label(frame,"Building area, measured from the lot center (meters).",48,407,890,19);
            var widthField=Number("Width",48,141,457,116,slot.width,1);
            var depthField=Number("Depth",285,365,457,116,slot.height,1);
            VerticalRule(505,453,48);
            var xField=Number("Center X",528,625,457,112,slot.center.x,.5f);
            var zField=Number("Center Z",758,854,457,98,slot.center.y,.5f);
            bool replaceBuilding=false;
            if(selectedBuildings.Length>0)
                LayoutButton("Use selected buildings as footprint",48,505,904,34,()=>
                {
                    var bounds=LotRuntimeLoader.Footprint(selectedBuildings[0],catalog.Find(selectedBuildings[0].assetId));
                    foreach(var selectedBuilding in selectedBuildings.Skip(1)){
                        var nextBounds=LotRuntimeLoader.Footprint(selectedBuilding,catalog.Find(selectedBuilding.assetId));
                        bounds=Rect.MinMaxRect(Mathf.Min(bounds.xMin,nextBounds.xMin),Mathf.Min(bounds.yMin,nextBounds.yMin),Mathf.Max(bounds.xMax,nextBounds.xMax),Mathf.Max(bounds.yMax,nextBounds.yMax));
                    }
                    widthField.text=MeterText(bounds.width);depthField.text=MeterText(bounds.height);
                    xField.text=MeterText(bounds.center.x);zField.text=MeterText(bounds.center.y);replaceBuilding=true;
                },LotGlyph.Select,18);

            InputField entranceX=null,entranceZ=null,drivewayX=null,drivewayZ=null,rowGap=null;Text error=null;
            float middle=558+extra;
            Group("Orientation",24,middle,464,176,205);
            Group("Paths & Access",512,middle,464,176,254);
            var frontage=layout.frontage;
            Label(frame,"Road Edge",48,middle+34,122,20);
            var frontageMenu=DefaultControls.CreateDropdown(new DefaultControls.Resources());frontageMenu.name="Road edge dropdown";
            frontageMenu.transform.SetParent(frame,false);At(frontageMenu.GetComponent<RectTransform>(),181,middle+34,284,42);
            var frontageDropdown=frontageMenu.GetComponent<Dropdown>();frontageDropdown.ClearOptions();
            frontageDropdown.AddOptions(Enum.GetNames(typeof(LotFrontage)).ToList());frontageDropdown.SetValueWithoutNotify((int)frontage);
            frontageDropdown.onValueChanged.AddListener(value=>frontage=(LotFrontage)value);
            var dropdownImage=frontageMenu.GetComponent<Image>();dropdownImage.sprite=ZoneModalArtwork.Dark;dropdownImage.type=Image.Type.Sliced;dropdownImage.color=Color.white;
            foreach(var text in frontageMenu.GetComponentsInChildren<Text>(true)){text.font=font;text.fontSize=20;text.color=new Color32(239,230,210,255);}
            var arrow=frontageMenu.transform.Find("Arrow");arrow.GetComponent<Image>().enabled=false;AddGlyph(arrow,LotGlyph.ChevronDown,0,0,20);
            var menuBackground=frontageDropdown.template.GetComponent<Image>();menuBackground.color=ButtonColor;
            foreach(var image in frontageDropdown.template.GetComponentsInChildren<Image>(true))image.color=image.name.Contains("Handle")?Gold:ButtonColor;
            var itemToggle=frontageDropdown.template.GetComponentInChildren<Toggle>(true);var oldCheck=itemToggle.graphic;oldCheck.enabled=false;
            AddGlyph(oldCheck.transform,LotGlyph.Check,0,0,16);itemToggle.graphic=oldCheck.GetComponentInChildren<LotEditorIcon>();
            ((RectTransform)itemToggle.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,34);
            frontageDropdown.template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,154);
            Label(frame,"Building Faces",48,middle+98,122,18);
            bool faceAwayFromRoad=layout.faceAwayFromRoad;
            singleOnly.AddRange(Choices(new[]{"Face Road","Face Away"},181,middle+98,new[]{135,137},faceAwayFromRoad?1:0,
                value=>faceAwayFromRoad=value==1,new[]{LotGlyph.Path,LotGlyph.House},17));
            Label(frame,"Auto Paths",536,middle+34,145,20);
            bool autoPaths=layout.autoPaths;
            singleOnly.AddRange(Choices(new[]{"Off","On"},696,middle+34,new[]{120,126},autoPaths?1:0,value=>autoPaths=value==1));
            Label(frame,"Entrance Guide",536,middle+98,151,19);
            LayoutButton("Place Entry Marker",696,middle+98,256,44,()=>ApplyLayout(placeEntryMarker:true),LotGlyph.Select,18);
            modeHint=Label(frame,"",48,middle+180,904,16);

            float optional=778+extra;
            Group("Optional Offsets (meters)",24,optional,952,170,335);
            entranceX=Number("House X",48,160,optional+38,120,layout.hasEntrance?layout.entrance.x:(float?)null,.5f);
            entranceZ=Number("House Z",314,426,optional+38,120,layout.hasEntrance?layout.entrance.y:(float?)null,.5f);
            drivewayX=Number("Driveway X",48,160,optional+96,120,layout.hasDriveway?layout.driveway.x:(float?)null,.5f);
            drivewayZ=Number("Driveway Z",314,426,optional+96,120,layout.hasDriveway?layout.driveway.y:(float?)null,.5f);
            VerticalRule(576,optional+34,111);
            rowGap=Number("Row Gap",620,761,optional+38,191,layout.rowGap,.1f);rowOnly.Add(rowGap);
            foreach(var button in rowGap.GetComponentsInChildren<Button>())rowOnly.Add(button);
            var gapHint=Label(frame,"Space between buildings in a row\n(0 = buildings touch).",620,optional+92,330,17);
            At(gapHint.rectTransform,620,optional+92,330,55);
            Label(frame,"Lot terrain",48,968+extra,145,21);
            Choices(new[]{"Follow ground slope","Level lot"},206,960+extra,new[]{346,382},levelLot?1:0,value=>levelLot=value==1);
            error=Label(frame,"",36,1047+extra,928,16);At(error.rectTransform,36,1047+extra,928,42);error.color=Gold;
            LayoutButton("Eligible Buildings…",54,1090+extra,470,52,()=>
            {
                if(string.IsNullOrEmpty(lot.firstEraId)){error.text="Set the lot’s era in the Lot Editor before choosing buildings.";return;}
                ApplyLayout(openBuildingChoices:true);
            },LotGlyph.Catalog,22);
            StyleButton(LayoutButton("Apply Layout",656,1090+extra,296,52,()=>ApplyLayout(),LotGlyph.Check,22),true,22);
            RefreshMode();

            void RefreshMode()
            {
                foreach(var choice in singleOnly)choice.interactable=!buildingRow;
                foreach(var choice in rowOnly)choice.interactable=buildingRow;
                if(modeHint)modeHint.text=buildingRow?"Rows follow the road edge. The entry marker sets the front-step line.":"Optional guides can stay blank. Add an entry marker to align the building entrance.";
            }
            void Group(string caption,float x,float y,float width,float height,float legendWidth)
            {
                var image=AddImage(caption+" section",frame,Color.white);image.sprite=ZoneModalArtwork.Section;image.type=Image.Type.Sliced;
                At(image.rectTransform,x,y,width,height);image.transform.SetAsFirstSibling();image.raycastTarget=false;
                var tab=AddImage(caption+" heading",frame,Color.white);tab.sprite=ZoneModalArtwork.Dark;tab.type=Image.Type.Sliced;
                At(tab.rectTransform,x+18,y-14,legendWidth,39);tab.raycastTarget=false;
                var label=Label(tab.transform,caption,16,0,legendWidth-30,23);label.font=headingFont;label.color=new Color32(247,234,205,255);
            }
            void Rule(float x,float y,float width){var line=AddImage("Section divider",frame,new Color(.58f,.63f,.65f,.45f));At(line.rectTransform,x,y,width,1);line.raycastTarget=false;}
            void VerticalRule(float x,float y,float height){var line=AddImage("Column divider",frame,new Color(.58f,.63f,.65f,.45f));At(line.rectTransform,x,y,1,height);line.raycastTarget=false;}
            Button LayoutButton(string caption,float x,float y,float width,float height,Action action,LotGlyph glyph=LotGlyph.None,int size=21)
            {
                var button=Control(frame,caption,x,y,width,action,glyph);At((RectTransform)button.transform,x,y,width,height);
                StyleButton(button,false,size);
                var icon=button.GetComponentInChildren<LotEditorIcon>();if(icon)At(icon.rectTransform,12,(height-24)/2,24,24);
                return button;
            }
            void StyleButton(Button button,bool active,int size)
            {
                var image=button.GetComponent<Image>();image.sprite=active?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;image.type=Image.Type.Sliced;image.color=Color.white;
                foreach(var outline in button.GetComponents<Outline>())outline.enabled=false;
                button.GetComponentInChildren<Text>().fontSize=size;
                var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.15f,1.15f,1.1f);colors.pressedColor=new Color(.78f,.78f,.72f);colors.disabledColor=new Color(.45f,.45f,.45f);button.colors=colors;
            }
            Button[] Choices(string[] labels,float x,float y,int[] widths,int initial,Action<int> select,LotGlyph[] glyphs=null,int fontSize=21)
            {
                var buttons=new Button[labels.Length];
                for(int i=0;i<labels.Length;i++){
                    int value=i;buttons[i]=LayoutButton(labels[i],x,y,widths[i],44,()=>{select(value);for(int j=0;j<buttons.Length;j++)StyleButton(buttons[j],j==value,fontSize);},glyphs==null?LotGlyph.None:glyphs[i],fontSize);
                    StyleButton(buttons[i],i==initial,fontSize);x+=widths[i]+12;
                }
                return buttons;
            }
            InputField Number(string title,float labelX,float fieldX,float y,float width,float? value,float step)
            {
                Label(frame,title,labelX,y,fieldX-labelX-8,20);
                var field=Field(frame,"m",fieldX,y,width);field.gameObject.name=title+" input";
                At((RectTransform)field.transform,fieldX,y,width,42);field.contentType=InputField.ContentType.DecimalNumber;
                field.text=value.HasValue?MeterText(value.Value):"";field.textComponent.fontSize=21;
                var image=field.GetComponent<Image>();image.sprite=ZoneModalArtwork.Dark;image.type=Image.Type.Sliced;image.color=Color.white;
                foreach(var text in field.GetComponentsInChildren<Text>())Place(text.rectTransform,Vector2.zero,Vector2.one,new Vector2(13,0),new Vector2(-27,0));
                foreach(int direction in new[]{1,-1}){
                    var arrow=Control(field.transform,direction>0?"▴":"▾",width-24,direction>0?1:21,22,()=>{
                        float current=ReadMeters(field,out var number)?number:0;
                        field.text=MeterText(Mathf.Round((current+direction*step)*1000)/1000);
                    });
                    arrow.gameObject.name=title+(direction>0?" increase":" decrease");
                    At((RectTransform)arrow.transform,width-24,direction>0?1:21,22,20);arrow.GetComponent<Image>().color=Color.clear;
                    foreach(var outline in arrow.GetComponents<Outline>())outline.enabled=false;arrow.GetComponentInChildren<Text>().fontSize=14;
                }
                return field;
            }

            TownZone SelectedZone() => (TownZone)(use * 3 + density + 1);
            LotWealth SelectedWealth() => (LotWealth)(wealth + 1);

            void ApplyLayout(bool openBuildingChoices = false, bool placeEntryMarker = false)
            {
                if (!ReadMeters(widthField, out float w) || !ReadMeters(depthField, out float d) ||
                    !ReadMeters(xField, out float x) || !ReadMeters(zField, out float z))
                {
                    error.text = "Enter width, depth and center coordinates in meters.";
                    return;
                }
                if (!ReadAnchor(entranceX, entranceZ, out bool hasEntrance, out var entrance) ||
                    !ReadAnchor(drivewayX, drivewayZ, out bool hasDriveway, out var driveway))
                {
                    error.text = "Enter both coordinates for each entrance you want to use.";
                    return;
                }
                if(!ReadMeters(rowGap,out float gap)||gap<0||gap>4){error.text="Row gap must be between 0 and 4 metres.";return;}
                var next = new LotZoningLayout
                {
                    buildingRow=buildingRow,rowGap=gap,rowSeed=layout.rowSeed,
                    zone = SelectedZone(), wealth = SelectedWealth(), previewBuildingId = layout.previewBuildingId,
                    overrideBuildingFit = !buildingRow && layout.overrideBuildingFit,
                    restrictGrowthBuildings = layout.restrictGrowthBuildings,
                    growthBuildingIds = layout.growthBuildingIds,
                    buildingQuarterTurns = buildingRow ? 0 : layout.buildingQuarterTurns,
                    faceAwayFromRoad = !buildingRow && faceAwayFromRoad, autoPaths = !buildingRow && autoPaths, buildingScale = buildingRow ? 1 : layout.buildingScale,
                    buildingCenter = new Vector2(x, z), buildingSize = new Vector2(w, d),
                    frontage = frontage, hasEntrance = hasEntrance, entrance = entrance,
                    hasDriveway = hasDriveway, driveway = driveway
                };
                var fitProblem = next.Valid ? next.FitProblem(lot) : "Enter a positive width and depth for the building slot.";
                if (fitProblem != null)
                {
                    error.text = fitProblem;
                    return;
                }
                var remaining=lot;
                if(replaceBuilding){remaining=LotDefinition.Parse(JsonUtility.ToJson(lot));remaining.objects.RemoveAll(item=>selectedBuildings.Any(selectedBuilding=>selectedBuilding.id==item.id));}
                if (SlotOverlapsBuilding(remaining, next))
                {
                    error.text = "Move fixed buildings out of the variable slot.";
                    return;
                }
                session.Edit(item =>
                {
                    item.zoningLayout = next;
                    item.levelLot=levelLot;
                    if (replaceBuilding) item.objects.RemoveAll(value=>selectedBuildings.Any(selectedBuilding=>selectedBuilding.id==value.id));
                });
                if (replaceBuilding) {selected = null;selectedGroup.Clear();}
                CloseAssetPopup();
                if (placeEntryMarker)
                {
                    armed = null;
                    pickingLotEntrance = true;
                    Refresh();
                    return;
                }
                if (openBuildingChoices)
                {
                    OpenGrowthBuildingChoices();
                    return;
                }
                Refresh(next.buildingRow?"Building row footprint ready. Choose eligible buildings, then Preview Row.":$"{next.Label} growable layout ready to save.");
            }

            static bool ReadAnchor(InputField x, InputField z, out bool present, out Vector2 point)
            {
                point = default;
                present = !string.IsNullOrWhiteSpace(x.text) || !string.IsNullOrWhiteSpace(z.text);
                if (!present) return true;
                if (!ReadMeters(x, out float east) || !ReadMeters(z, out float north)) return false;
                point = new Vector2(east, north);
                return float.IsFinite(east) && float.IsFinite(north);
            }

            static string MeterText(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
            static bool ReadMeters(InputField field, out float value) =>
                float.TryParse(field.text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

    }
    internal sealed class GrowableModalFit : MonoBehaviour
    {
        Vector2 dimensions,lastParentSize;RectTransform rect,parent;
        internal void Initialize(Vector2 size){dimensions=size;rect=(RectTransform)transform;parent=(RectTransform)rect.parent;Fit();}
        void LateUpdate(){if(parent&&parent.rect.size!=lastParentSize)Fit();}
        void Fit(){lastParentSize=parent.rect.size;if(lastParentSize.x<=40||lastParentSize.y<=40)return;float scale=Mathf.Min(1,(lastParentSize.x-40)/dimensions.x,(lastParentSize.y-40)/dimensions.y);rect.localScale=Vector3.one*scale;}
    }
}

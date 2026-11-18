using System;
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
        private void OpenVehicleScript(LotObjectDefinition vehicle)
        {
            if(assetPopup)return;
            CloseChoices();
            var frame=CreateModal("Vehicle Behavior",1120,720);
            var settings=vehicle.vehicleScript;
            if(!settings.configured)
            {
                settings.configured=true;settings.enabled=true;
                settings.category=VehicleTaskCategory.Resource;settings.resource=VehicleResource.Timber;
                settings.when=RegionTimeOfDay.Morning;settings.destination=VehicleDestination.TownCenter;
            }
            var profile=VehicleProfile.Find(vehicle.assetId);
            Label(frame,catalog.Find(vehicle.assetId).label,24,49,740,18);
            var enabled=Checkbox(frame,"Behavior active","Active",870,47,180,settings.enabled);
            var strap=AddImage("Behavior guidance",frame,new Color32(215,185,130,255));
            At(strap.rectTransform,24,94,1072,38);
            var guidance=Label(strap.transform,"Configure what this vehicle does, when it does it, and where it goes.",16,0,1040,18);
            guidance.color=new Color32(36,27,17,255);
            var content=AddImage("Behavior settings",frame,new Color32(9,24,34,255)).rectTransform;
            At(content,288,150,808,389);AddOutline(content.GetComponent<Image>(),Gold);
            var panels=new RectTransform[3];var tabs=new Button[3];
            var names=new[]{"What","When","Where"};var questions=new[]{"What does this vehicle do?","When does this happen?","Where does this vehicle go?"};
            var categories=new Button[3];var dropdowns=new Dropdown[3];
            var times=PrototypeRegionView.DaylightChoices;var timeButtons=new Button[times.Length];
            Text preview=null,sectionTitle=null,sectionQuestion=null;
            Image sectionIcon=null;
            int page=0;
            for(int i=0;i<3;i++)
            {
                int tab=i;
                tabs[i]=Control(frame,names[i],24,150+i*138,244,()=>Show(tab));
                At((RectTransform)tabs[i].transform,24,150+i*138,244,124);
                var text=tabs[i].GetComponentInChildren<Text>();text.alignment=TextAnchor.MiddleLeft;
                text.rectTransform.offsetMin=new Vector2(88,0);text.font=headingFont;text.fontSize=25;
                Art(tabs[i].transform,names[i].ToLowerInvariant(),18,30,62,62);
                panels[i]=AddImage(names[i]+" page",content,Color.clear).rectTransform;
                At(panels[i],0,0,808,389);
            }
            sectionIcon=AddImage("Section icon",content,Color.white);At(sectionIcon.rectTransform,22,18,54,54);
            sectionIcon.preserveAspect=true;sectionIcon.raycastTarget=false;
            sectionTitle=Label(content,"",112,25,155,24);sectionTitle.font=headingFont;
            sectionQuestion=Label(content,"",282,25,506,20);
            for(int i=0;i<3;i++)
            {
                int category=i;
                categories[i]=Control(panels[0],"",22,99+i*72,212,()=>{settings.category=(VehicleTaskCategory)category;RefreshBehavior();});
                At((RectTransform)categories[i].transform,22,99+i*72,212,58);
                dropdowns[i]=Menu(panels[0],i==0?VehicleResources.All.Select(VehicleResources.Label).ToArray():new[]{"None"},252,105+i*72,532,46);
            }
            dropdowns[0].SetValueWithoutNotify((int)settings.resource);
            dropdowns[0].onValueChanged.AddListener(value=>{settings.resource=VehicleResources.All[value];settings.category=VehicleTaskCategory.Resource;RefreshBehavior();});
            dropdowns[1].interactable=dropdowns[2].interactable=false;
            Label(panels[0],"Load size",28,328,100,17);var capacity=Field(panels[0],"Units",133,323,66);
            capacity.contentType=InputField.ContentType.IntegerNumber;capacity.characterLimit=3;capacity.text=settings.Capacity.ToString();
            Label(panels[0],"Load / unload seconds",263,328,207,17);var handling=Field(panels[0],"Seconds",480,323,66);
            handling.contentType=InputField.ContentType.IntegerNumber;handling.characterLimit=3;handling.text=settings.HandlingSeconds.ToString();
            for(int i=0;i<times.Length;i++)
            {
                var phase=times[i];int x=21+i*154;
                timeButtons[i]=Control(panels[1],phase.ToString(),x,130,146,()=>{settings.when=phase;RefreshBehavior();});
                At((RectTransform)timeButtons[i].transform,x,130,146,190);
                var label=timeButtons[i].GetComponentInChildren<Text>();label.alignment=TextAnchor.LowerCenter;
                label.rectTransform.offsetMin=new Vector2(0,20);label.font=headingFont;label.fontSize=20;
                Art(timeButtons[i].transform,phase.ToString().ToLowerInvariant(),25,30,96,90);
            }
            Label(panels[2],"Destination",26,108,730,19);
            Menu(panels[2],new[]{"Town Center"},26,149,754,48);
            var whereNote=Label(panels[2],"Deliver to a connected lot marked Town Center, then return to this lot. Harbor Council House lots are Town Centers by default. Other lots can be designated through Receives.",28,227,745,18);
            At(whereNote.rectTransform,28,227,745,102);
            var previewPanel=AddImage("Behavior preview",frame,new Color32(215,185,130,255));
            At(previewPanel.rectTransform,288,557,808,76);
            preview=Label(previewPanel.transform,"",20,10,768,19);preview.color=new Color32(36,27,17,255);
            At(preview.rectTransform,20,10,768,58);
            var error=Label(frame,"",24,649,610,16);error.color=Gold;
            var cancel=Control(frame,"Cancel",735,653,140,CloseAssetPopup);At((RectTransform)cancel.transform,735,647,140,44);VehicleBehaviorArtwork.Style(cancel,false);
            var save=Control(frame,"Save behavior",890,653,206,()=>
            {
                if(!session.Lot.objects.Contains(vehicle))return;
                settings.enabled=enabled.isOn;
                if(!int.TryParse(capacity.text,out settings.capacity)||settings.capacity<1||settings.capacity>100||
                    !int.TryParse(handling.text,out settings.handlingSeconds)||settings.handlingSeconds<1||settings.handlingSeconds>120)
                {error.text="Load size 1–100 · Handling 1–120 seconds";return;}
                if(settings.enabled&&settings.category==VehicleTaskCategory.Resource&&profile?.supportsResourceFreight!=true)
                {error.text="This vehicle is not equipped to carry resources.";return;}
                settings.behavior=settings.enabled&&settings.category==VehicleTaskCategory.Resource?LotVehicleBehavior.ResourceDelivery:LotVehicleBehavior.None;
                session.Edit(_=>vehicle.vehicleScript=settings);
                CloseAssetPopup();Refresh("Vehicle behavior saved.");
            });At((RectTransform)save.transform,890,647,206,44);VehicleBehaviorArtwork.Style(save,true);
            enabled.onValueChanged.AddListener(_=>RefreshBehavior());
            Show(0);

            void Show(int selected)
            {
                page=selected;
                sectionIcon.sprite=VehicleBehaviorArtwork.Get(names[page].ToLowerInvariant());
                sectionTitle.text=names[page].ToUpperInvariant();sectionQuestion.text="— "+questions[page];
                sectionIcon.transform.SetAsLastSibling();sectionTitle.transform.SetAsLastSibling();sectionQuestion.transform.SetAsLastSibling();
                for(int i=0;i<3;i++){panels[i].gameObject.SetActive(i==page);VehicleBehaviorArtwork.Style(tabs[i],i==page);}
                RefreshBehavior();
            }
            void RefreshBehavior()
            {
                for(int i=0;i<3;i++)
                {
                    bool active=(int)settings.category==i;
                    categories[i].GetComponentInChildren<Text>().text=(active?"●  ":"○  ")+((VehicleTaskCategory)i);
                    VehicleBehaviorArtwork.Style(categories[i],active);
                }
                for(int i=0;i<times.Length;i++)VehicleBehaviorArtwork.Style(timeButtons[i],settings.when==times[i],true);
                preview.text=!enabled.isOn?"Behavior inactive. This vehicle stays on its lot.":settings.category==VehicleTaskCategory.Resource?
                    "Deliver "+VehicleResources.Label(settings.resource).ToLowerInvariant()+" to Town Center in the "+settings.when.ToString().ToLowerInvariant()+", then return. One trip each selected time period.":
                    settings.category+": None. No behavior will run until an option is available.";
            }
            Dropdown Menu(Transform parent,string[] options,int x,int y,int width,int height)
            {
                var obj=DefaultControls.CreateDropdown(new DefaultControls.Resources());obj.name="Behavior "+options[0];obj.transform.SetParent(parent,false);
                At((RectTransform)obj.transform,x,y,width,height);
                var menu=obj.GetComponent<Dropdown>();menu.ClearOptions();menu.AddOptions(options.ToList());
                foreach(var text in obj.GetComponentsInChildren<Text>(true)){text.font=font;text.color=new Color32(245,232,203,255);text.fontSize=18;}
                foreach(var image in obj.GetComponentsInChildren<Image>(true))image.color=new Color32(22,42,57,255);
                var colors=menu.colors;
                colors.normalColor=new Color32(22,42,57,255);colors.highlightedColor=new Color32(39,62,76,255);
                colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color32(55,72,80,255);
                colors.disabledColor=new Color32(25,37,47,255);menu.colors=colors;
                obj.GetComponent<Image>().color=Color.white;
                var arrow=obj.transform.Find("Arrow");if(arrow)arrow.gameObject.SetActive(false);
                var arrowText=Label(obj.transform,"▾",width-32,5,22,20);arrowText.raycastTarget=false;
                var itemToggle=menu.template.GetComponentInChildren<Toggle>(true);
                menu.itemText.verticalOverflow=VerticalWrapMode.Overflow;
                var contentRect=menu.template.GetComponent<ScrollRect>().content;
                contentRect.sizeDelta=new Vector2(contentRect.sizeDelta.x,28);
                if(itemToggle)
                {
                    var row=(RectTransform)itemToggle.transform;row.sizeDelta=new Vector2(row.sizeDelta.x,28);
                    if(itemToggle.graphic)itemToggle.graphic.gameObject.SetActive(false);
                    var ring=Label(itemToggle.transform,"○",2,0,16,15);At(ring.rectTransform,2,0,16,28);ring.color=Gold;ring.raycastTarget=false;
                    var dot=Label(itemToggle.transform,"●",2,0,16,9);At(dot.rectTransform,2,0,16,28);dot.color=Gold;dot.raycastTarget=false;
                    itemToggle.graphic=dot;
                }
                var scrollbar=menu.template.GetComponentInChildren<Scrollbar>(true);
                if(scrollbar&&scrollbar.targetGraphic)scrollbar.targetGraphic.color=Gold;
                AddOutline(menu.template.GetComponent<Image>(),Gold);
                AddOutline(obj.GetComponent<Image>(),Gold);
                var template=menu.template;template.sizeDelta=new Vector2(template.sizeDelta.x,Mathf.Min(300,options.Length*32+8));
                return menu;
            }
            void Art(Transform parent,string key,int x,int y,int width,int height)
            {
                var image=AddImage(key+" artwork",parent,Color.white);At(image.rectTransform,x,y,width,height);
                image.sprite=VehicleBehaviorArtwork.Get(key);image.preserveAspect=true;image.raycastTarget=false;
            }
        }
    }
}

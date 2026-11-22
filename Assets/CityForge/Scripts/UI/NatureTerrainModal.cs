using System;
using UnityEngine;
using UnityEngine.UI;
using CityForge.World;
namespace CityForge.UI
{
    public sealed partial class NatureToolPopup
    {
        RectTransform terrainModal;
        readonly TerraformBrushSettings terrainSettings=new();
        public event Action<TerraformBrushSettings> TerrainRequested;
        void CloseTerrainModal(){if(!terrainModal)return;terrainModal.gameObject.SetActive(false);Destroy(terrainModal.gameObject);terrainModal=null;}
        void OpenTerrainModal()
        {
            if(!waterFont)waterFont=Font.CreateDynamicFontFromOSFont(new[]{"Georgia","Times New Roman"},24);
            terrainModal=Center(GetComponentInParent<Canvas>().transform,"Terraforming modal",Vector2.zero,Vector2.zero);
            terrainModal.anchorMin=Vector2.zero;terrainModal.anchorMax=Vector2.one;terrainModal.offsetMin=terrainModal.offsetMax=Vector2.zero;
            terrainModal.gameObject.AddComponent<Image>().color=new Color(0,0,0,.5f);
            var panel=Center(terrainModal,"Terraforming",Vector2.zero,new Vector2(1500,790));var face=panel.gameObject.AddComponent<Image>();face.sprite=ZoneModalArtwork.Panel;face.type=Image.Type.Sliced;
            TextLabel(panel,"Terraforming",new Vector2(-380,325),new Vector2(640,70),46,true);
            Action(panel,"Close terraforming","×",new Vector2(705,335),new Vector2(45,45),()=>close());Rule(panel,new Vector2(0,264),new Vector2(1420,1));Rule(panel,new Vector2(-390,-35),new Vector2(1,550));
            string[] names={"Raise Land","Lower Land","Level Land","Smooth"};string[] glyphs={"∧","∨","=","≈"};
            for(int i=0;i<4;i++){int mode=i;var button=Action(panel,names[i],glyphs[i]+"   "+names[i],new Vector2(-555,194-i*91),new Vector2(300,79),()=>{terrainSettings.Action=(TerraformAction)mode;Reopen();});button.image.sprite=i==(int)terrainSettings.Action?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;}
            Action(panel,"Add Trees","Add Trees",new Vector2(-555,-172),new Vector2(300,74),()=>GetComponentInParent<NatureToolbar>().Select(NatureTool.Trees));
            Action(panel,"Add Animals","Add Animals",new Vector2(-555,-257),new Vector2(300,74),()=>GetComponentInParent<NatureToolbar>().Select(NatureTool.Animals));
            TextLabel(panel,names[(int)terrainSettings.Action],new Vector2(178,210),new Vector2(1020,60),40,true);
            string description=terrainSettings.Action switch{TerraformAction.Raise=>"Raise each drag to the brush average plus the elevation step.",TerraformAction.Lower=>"Lower each drag from the brush average. Cut from the shore to carve a bay.",TerraformAction.Smooth=>"Blend uneven ground toward the brush average, with soft natural banks.",_=>"Set an even height across the brush. A drag keeps its starting elevation."};
            TextLabel(panel,description,new Vector2(175,145),new Vector2(1020,74),24);
            if(terrainSettings.Action==TerraformAction.Level){
                TextLabel(panel,"Level Mode",new Vector2(170,76),new Vector2(1000,40),27,true);
                string[] modes={"Lowest Point","Brush Average","Clicked Height","Sea Level"};
                for(int i=0;i<4;i++){int mode=i;var b=Action(panel,modes[i],modes[i],new Vector2(-206+i*239,22),new Vector2(227,49),()=>{terrainSettings.Level=(TerraformLevel)mode;terrainSettings.Offset=mode==3?.2f:0;Reopen();});b.GetComponentInChildren<Text>().fontSize=21;b.image.sprite=i==(int)terrainSettings.Level?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;}
            }
            SliderRow("Brush size",-64,8,128,terrainSettings.Size," m",v=>terrainSettings.Size=Mathf.Round(v/4)*4,()=>terrainSettings.Size);
            if(terrainSettings.Action==TerraformAction.Level)SliderRow("Height offset",-156,-12,12,terrainSettings.Offset," m",v=>terrainSettings.Offset=Mathf.Round(v*10)/10,()=>terrainSettings.Offset);
            else if(terrainSettings.Action==TerraformAction.Smooth)SliderRow("Smoothing",-156,.1f,1,terrainSettings.Blend,"",v=>terrainSettings.Blend=v,()=>terrainSettings.Blend);
            else SliderRow("Elevation step",-156,.25f,12,terrainSettings.Step," m",v=>terrainSettings.Step=Mathf.Round(v*4)/4,()=>terrainSettings.Step);
            TextLabel(panel,"Clears trees and objects across the brush and banks.\nDrag to sculpt · Shift: double size · Tab: rotate · Esc: finish\nOcean shorelines update after each drag",new Vector2(150,-255),new Vector2(980,86),20);
            var start=Action(panel,"Sculpt terrain","Sculpt Terrain",new Vector2(540,-338),new Vector2(300,55),()=>{var copy=JsonUtility.FromJson<TerraformBrushSettings>(JsonUtility.ToJson(terrainSettings));CloseTerrainModal();TerrainRequested?.Invoke(copy);});start.image.sprite=ZoneModalArtwork.Gold;
            void Reopen(){CloseTerrainModal();OpenTerrainModal();}
            void SliderRow(string caption,float y,float min,float max,float value,string unit,Action<float> update,Func<float> read){
                TextLabel(panel,caption,new Vector2(-210,y),new Vector2(240,36),24,true);
                var label=TextLabel(panel,"",new Vector2(589,y),new Vector2(160,36),24);label.alignment=TextAnchor.MiddleRight;
                var host=Center(panel,caption+" slider",new Vector2(310,y),new Vector2(530,32));host.gameObject.AddComponent<Image>().color=Color.clear;var slider=host.gameObject.AddComponent<Slider>();
                var track=Center(host,"Track",Vector2.zero,new Vector2(530,5)).gameObject.AddComponent<Image>();track.color=new Color(.45f,.40f,.25f);track.raycastTarget=false;
                var handle=Center(host,"Handle",Vector2.zero,new Vector2(15,32)).gameObject.AddComponent<Image>();handle.color=Gold;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=min;slider.maxValue=max;slider.SetValueWithoutNotify(value);label.text=read().ToString("0.##")+unit;
                slider.onValueChanged.AddListener(v=>{update(v);slider.SetValueWithoutNotify(read());label.text=read().ToString("0.##")+unit;});
            }
        }
    }
}

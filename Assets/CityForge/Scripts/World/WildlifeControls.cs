using UnityEngine;
using UnityEngine.UI;
namespace CityForge.World
{
    public sealed class WildlifeControls:MonoBehaviour
    {
        PrototypeRegionView view;Text label;Button button;Button findButton,riverButton;
        public static void Build(PrototypeRegionView view,Transform hud,Font font){
            var host=new GameObject("Wildlife",typeof(RectTransform),typeof(Image),typeof(Button),typeof(WildlifeControls));host.transform.SetParent(hud,false);
            var rect=host.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-25,-546);rect.sizeDelta=new Vector2(205,40);
            host.GetComponent<Image>().color=new Color(.13f,.15f,.13f,.95f);
            var control=host.GetComponent<WildlifeControls>();control.view=view;control.button=host.GetComponent<Button>();
            var text=new GameObject("Caption",typeof(RectTransform),typeof(Text));text.transform.SetParent(host.transform,false);
            var tr=text.GetComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
            control.label=text.GetComponent<Text>();control.label.font=font;control.label.fontSize=16;control.label.color=new Color(.93f,.88f,.72f);control.label.alignment=TextAnchor.MiddleCenter;control.label.raycastTarget=false;
            control.button.onClick.AddListener(()=>view.SetAnimalsEnabled(!view.AnimalsEnabled));
            var find=Instantiate(host,hud,false);Destroy(find.GetComponent<WildlifeControls>());find.name="Find wildlife";
            find.GetComponent<RectTransform>().anchoredPosition=new Vector2(-25,-590);
            control.findButton=find.GetComponent<Button>();control.findButton.onClick=new Button.ButtonClickedEvent();
            find.GetComponentInChildren<Text>().text="FIND ANIMAL";
            control.findButton.onClick.AddListener(()=>view.ApprovedDistrict?.Wildlife?.FindAnimal());
            var river=Instantiate(find,hud,false);Destroy(river.GetComponent<WildlifeControls>());river.name="Find river life";river.GetComponent<RectTransform>().anchoredPosition=new Vector2(-25,-634);
            control.riverButton=river.GetComponent<Button>();control.riverButton.onClick=new Button.ButtonClickedEvent();river.GetComponentInChildren<Text>().text="FIND RIVER LIFE";
            control.riverButton.onClick.AddListener(()=>view.ApprovedDistrict?.Wildlife?.FindRiverLife());
            control.available=Resources.Load<TextAsset>("Wildlife/Available")!=null;
        }
        bool available;
        void Update(){if(!view)return;bool visible=view.SelectedDistrict>=0&&view.NaturalLandscape;button.image.enabled=visible;label.enabled=visible;button.interactable=visible&&available;
            var population=view.ApprovedDistrict?.Wildlife;
            if(findButton)findButton.gameObject.SetActive(visible&&population&&population.Animals.Count>0);
            bool riverLife=false;if(population)foreach(var a in population.Animals)if(a.species.habitat==WildlifeHabitat.RiverBankShallows||a.species.habitat==WildlifeHabitat.Freshwater||a.species.habitat==WildlifeHabitat.River){riverLife=true;break;}
            if(riverButton)riverButton.gameObject.SetActive(visible&&riverLife);
            label.text=!available?"ANIMALS: UNAVAILABLE":view.AnimalsEnabled?"ANIMALS: ON ("+(population?population.Animals.Count:0)+")":"ANIMALS: OFF";}
    }
}

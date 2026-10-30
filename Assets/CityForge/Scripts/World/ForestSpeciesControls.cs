using UnityEngine;
using UnityEngine.UI;
namespace CityForge.World
{
    /// <summary>Reversible forest species experiments. Only presentation preferences are saved.</summary>
    public sealed class ForestSpeciesControls:MonoBehaviour
    {
        public static readonly string[] Labels={"Silver Maple A","Silver Maple B","Canyon Live Oak A","Canyon Live Oak B","Southern Live Oak","Angel Oak","OakPilot A","OakPilot B"};
        PrototypeRegionView view;GameObject panel;Text caption;readonly Text[] rows=new Text[ForestTreeMix.SpeciesCount];int lastMask=-1;Button opener;
        public static void Build(PrototypeRegionView view,Transform hud,Font font)
        {
            var host=new GameObject("ForestSpecies",typeof(RectTransform),typeof(ForestSpeciesControls));host.transform.SetParent(hud,false);var hostRect=host.GetComponent<RectTransform>();hostRect.anchorMin=Vector2.zero;hostRect.anchorMax=Vector2.one;hostRect.offsetMin=hostRect.offsetMax=Vector2.zero;var controller=host.GetComponent<ForestSpeciesControls>();controller.view=view;
            controller.opener=controller.Button(host.transform,"Open",new Vector2(-25,-500),new Vector2(205,40),font,"EXTRA SPECIES: OFF",()=>controller.panel.SetActive(!controller.panel.activeSelf),out controller.caption);
            controller.panel=new GameObject("Panel",typeof(RectTransform),typeof(Image));controller.panel.transform.SetParent(host.transform,false);Place(controller.panel.GetComponent<RectTransform>(),new Vector2(-245,-182),new Vector2(280,445));controller.panel.GetComponent<Image>().color=new Color(.08f,.11f,.09f,.98f);
            var title=controller.Label(controller.panel.transform,font,18,"EXTRA NATIVE SPECIES");Local(title.rectTransform,new Vector2(14,-10),new Vector2(254,28));
            var note=controller.Label(controller.panel.transform,font,13,"Approved forest stays enabled.");Local(note.rectTransform,new Vector2(14,-40),new Vector2(254,25));
            for(int i=0;i<rowsLength;i++){int species=i;controller.Button(controller.panel.transform,"Species"+i,new Vector2(-10,-76-i*36),new Vector2(260,32),font,"",()=>controller.Set(ForestTreeMix.EnabledSpeciesMask^(1<<species)),out controller.rows[i]);}
            controller.Button(controller.panel.transform,"None",new Vector2(-145,-373),new Vector2(125,34),font,"NONE",()=>controller.Set(0),out _);
            controller.Button(controller.panel.transform,"All",new Vector2(-10,-373),new Vector2(125,34),font,"ALL",()=>controller.Set(ForestTreeMix.AllSpeciesMask),out _);
            controller.Button(controller.panel.transform,"Close",new Vector2(-10,-411),new Vector2(260,27),font,"CLOSE",()=>controller.panel.SetActive(false),out _);
            controller.panel.SetActive(false);controller.Refresh();
        }
        const int rowsLength=ForestTreeMix.SpeciesCount;
        void Set(int mask){ForestTreeMix.SetEnabledSpecies(mask,true);foreach(var land in Object.FindObjectsByType<MeadowDistrictLandscape>(FindObjectsInactive.Include,FindObjectsSortMode.None))land.RefreshForestSpecies();Refresh();}
        void Update(){if(!view)return;bool active=view.ApprovedDistrict&&view.ApprovedDistrict.gameObject.activeInHierarchy;opener.gameObject.SetActive(active);opener.interactable=active;if(!active)panel.SetActive(false);if(lastMask!=ForestTreeMix.EnabledSpeciesMask)Refresh();}
        void Refresh(){lastMask=ForestTreeMix.EnabledSpeciesMask;int count=0;for(int i=0;i<rows.Length;i++){bool enabled=(lastMask&(1<<i))!=0;if(enabled)count++;rows[i].text=(enabled?"[ ON ]  ":"[ OFF ]  ")+Labels[i];}caption.text=count==0?"EXTRA SPECIES: OFF":count==rows.Length?"EXTRA SPECIES: ALL":"EXTRA SPECIES: "+count+" / "+rows.Length;}
        Text Label(Transform parent,Font font,int size,string text){var go=new GameObject("Caption",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var label=go.GetComponent<Text>();label.font=font;label.fontSize=size;label.color=new Color(.93f,.88f,.72f);label.text=text;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;return label;}
        Button Button(Transform parent,string name,Vector2 position,Vector2 size,Font font,string text,UnityEngine.Events.UnityAction action,out Text label){var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),position,size);go.GetComponent<Image>().color=new Color(.17f,.2f,.17f,.98f);var button=go.GetComponent<Button>();button.onClick.AddListener(action);label=Label(go.transform,font,15,text);label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;return button;}
        static void Place(RectTransform rect,Vector2 position,Vector2 size){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,1);rect.anchoredPosition=position;rect.sizeDelta=size;}
        static void Local(RectTransform rect,Vector2 position,Vector2 size){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;}
    }
}

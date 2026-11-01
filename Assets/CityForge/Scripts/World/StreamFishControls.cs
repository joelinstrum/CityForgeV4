using UnityEngine;
using UnityEngine.UI;
namespace CityForge.World {
public sealed class StreamFishControls:MonoBehaviour {
 PrototypeRegionView view;Button button;Text caption;
 public static void Build(PrototypeRegionView view,Transform hud,Font font){
  var host=new GameObject("Fish option",typeof(RectTransform),typeof(Image),typeof(Button),typeof(StreamFishControls));host.transform.SetParent(hud,false);
  var rect=host.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-25,-678);rect.sizeDelta=new Vector2(205,40);
  host.GetComponent<Image>().color=new Color(.13f,.15f,.13f,.95f);
  var control=host.GetComponent<StreamFishControls>();control.view=view;control.button=host.GetComponent<Button>();
  var label=new GameObject("Caption",typeof(RectTransform),typeof(Text));label.transform.SetParent(host.transform,false);
  var tr=label.GetComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
  control.caption=label.GetComponent<Text>();control.caption.font=font;control.caption.fontSize=16;control.caption.color=new Color(.93f,.88f,.72f);control.caption.alignment=TextAnchor.MiddleCenter;control.caption.raycastTarget=false;
  control.button.onClick.AddListener(()=>{var option=control.Option;if(option)option.SetFishEnabled(!option.FishEnabled);});
 }
 StreamFishOption Option=>view&&view.ApprovedDistrict?view.ApprovedDistrict.GetComponent<StreamFishOption>():null;
 void Update(){var option=Option;bool visible=option;button.image.enabled=visible;button.interactable=visible;caption.enabled=visible;caption.text=option&&option.FishEnabled?"FISH: ON (ZOOM 0)":"FISH: OFF";}
}
}

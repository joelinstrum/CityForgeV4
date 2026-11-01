using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace CityForge.World
{
    /// <summary>Overview markers refer only to actual saved settlements; clicking opens their original district.</summary>
    public sealed class NativeCampMarkers:MonoBehaviour
    {
        readonly List<(NativeCampRecord camp,RectTransform rect)> markers=new();
        PrototypeRegionView view;Canvas canvas;RectTransform root;Camera camera;CanvasGroup group;
        public void Initialize(PrototypeRegionView source){
            view=source;canvas=view.transform.Find("RegionHud").GetComponent<Canvas>();camera=view.GetComponentInChildren<Camera>();
            var go=new GameObject("Settlement markers",typeof(RectTransform),typeof(CanvasGroup));root=go.GetComponent<RectTransform>();root.SetParent(canvas.transform,false);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;group=go.GetComponent<CanvasGroup>();
            foreach(var camp in view.NativeCamps){
                var marker=new GameObject("Native camp / district "+camp.district,typeof(RectTransform),typeof(Image),typeof(Button));marker.transform.SetParent(root,false);var rect=marker.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(56,24);
                marker.GetComponent<Image>().color=new Color(.15f,.17f,.12f,.9f);marker.GetComponent<Button>().onClick.AddListener(()=>view.RequestDistrict(camp.district));
                var caption=new GameObject("Caption",typeof(RectTransform),typeof(Text));caption.transform.SetParent(marker.transform,false);var text=caption.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text="△ Camp";text.fontSize=13;text.color=new Color(.97f,.87f,.63f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;var label=caption.GetComponent<RectTransform>();label.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;label.offsetMin=label.offsetMax=Vector2.zero;
                markers.Add((camp,rect));
            }
        }
        void LateUpdate(){
            bool visible=view.SelectedDistrict<0&&!view.DistrictLoading;root.gameObject.SetActive(visible);if(!visible)return;
            group.blocksRaycasts=!view.GetComponent<DistrictCombiner>().Active;
            foreach(var item in markers){var p=item.camp.center;float h=view.Data.SmoothHeight(p.x/view.MapSide+.5f,p.y/view.MapSide+.5f)*view.TerrainHeightRange;
                var screen=camera.WorldToScreenPoint(new Vector3(p.x,view.SeaLevel+(h-view.SeaLevel)*1.25f+2,p.y));item.rect.gameObject.SetActive(screen.z>0);
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var local))item.rect.anchoredPosition=local;
            }
        }
        void OnDestroy(){if(root)Destroy(root.gameObject);}
    }
}

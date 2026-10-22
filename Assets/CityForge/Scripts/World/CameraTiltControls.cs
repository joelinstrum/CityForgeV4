using UnityEngine;
using UnityEngine.UI;
using CityForge.CameraSystem;

namespace CityForge.World
{
    /// <summary>A continuous low-angle to isometric camera inspection control.</summary>
    public sealed class CameraTiltControls : MonoBehaviour
    {
        PrototypeRegionView view;CityForgeCameraController rig;Slider slider;Text angle;CanvasGroup group;Texture2D dotTexture;Sprite dotSprite;
        static readonly Color Ink=new Color(.93f,.88f,.72f);
        public Slider Slider => slider;
        public static void Build(PrototypeRegionView view,Transform hud,Font font)
        {
            var host=new GameObject("Camera tilt",typeof(RectTransform),typeof(Image),typeof(CanvasGroup),typeof(CameraTiltControls));host.transform.SetParent(hud,false);
            var rect=host.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(25,85);rect.sizeDelta=new Vector2(142,246);
            host.GetComponent<Image>().color=new Color(.13f,.15f,.13f,.94f);
            var control=host.GetComponent<CameraTiltControls>();control.view=view;control.group=host.GetComponent<CanvasGroup>();
            Text Label(string name,string value,Vector2 position,Vector2 size,int fontSize){var text=new GameObject(name,typeof(RectTransform),typeof(Text));text.transform.SetParent(host.transform,false);var label=text.GetComponent<Text>();label.font=font;label.fontSize=fontSize;label.color=Ink;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;label.text=value;var r=label.rectTransform;r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=position;r.sizeDelta=size;return label;}
            Label("Title","CAMERA TILT",new Vector2(4,215),new Vector2(134,25),14);
            Label("Top","ISOMETRIC",new Vector2(4,190),new Vector2(134,22),12);
            Label("Bottom","LOW ANGLE",new Vector2(4,8),new Vector2(134,24),12);
            control.angle=Label("Angle","",new Vector2(59,100),new Vector2(78,35),20);
            Label("Hint","DRAG DOT",new Vector2(59,78),new Vector2(78,23),11);
            var pole=new GameObject("Tilt slider",typeof(RectTransform),typeof(Image),typeof(Slider));pole.transform.SetParent(host.transform,false);var p=pole.GetComponent<RectTransform>();p.anchorMin=p.anchorMax=p.pivot=Vector2.zero;p.anchoredPosition=new Vector2(15,35);p.sizeDelta=new Vector2(40,155);pole.GetComponent<Image>().color=Color.clear;
            var rail=new GameObject("Pole",typeof(RectTransform),typeof(Image));rail.transform.SetParent(pole.transform,false);var r=rail.GetComponent<RectTransform>();r.anchorMin=new Vector2(.5f,0);r.anchorMax=new Vector2(.5f,1);r.sizeDelta=new Vector2(4,-16);r.anchoredPosition=Vector2.zero;rail.GetComponent<Image>().color=new Color(.68f,.65f,.5f);rail.GetComponent<Image>().raycastTarget=false;
            var area=new GameObject("Handle area",typeof(RectTransform));area.transform.SetParent(pole.transform,false);var a=area.GetComponent<RectTransform>();a.anchorMin=new Vector2(.5f,0);a.anchorMax=new Vector2(.5f,1);a.sizeDelta=new Vector2(18,-16);a.anchoredPosition=Vector2.zero;
            var dot=new GameObject("Dot",typeof(RectTransform),typeof(Image));dot.transform.SetParent(area.transform,false);var d=dot.GetComponent<RectTransform>();d.anchorMin=d.anchorMax=new Vector2(.5f,0);d.sizeDelta=new Vector2(0,18);var image=dot.GetComponent<Image>();control.dotTexture=new Texture2D(32,32,TextureFormat.RGBA32,false);var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++)pixels[y*32+x]=new Color(1,1,1,Mathf.Clamp01(14.5f-Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))));control.dotTexture.SetPixels(pixels);control.dotTexture.Apply(false,true);control.dotSprite=Sprite.Create(control.dotTexture,new Rect(0,0,32,32),Vector2.one*.5f);image.sprite=control.dotSprite;image.preserveAspect=true;image.color=Ink;
            control.slider=pole.GetComponent<Slider>();control.slider.direction=Slider.Direction.BottomToTop;control.slider.handleRect=d;control.slider.targetGraphic=image;control.slider.navigation=new Navigation{mode=Navigation.Mode.None};control.slider.onValueChanged.AddListener(value=>{if(control.rig)control.rig.SetViewTilt(value);});
        }
        void OnDestroy(){if(dotSprite)Destroy(dotSprite);if(dotTexture)Destroy(dotTexture);}
        void Update()
        {
            if(!view)return;bool visible=view.SelectedDistrict>=0&&view.ApprovedDistrict;
            group.alpha=visible?1:0;group.interactable=group.blocksRaycasts=visible;
            if(!visible)return;if(!rig)rig=view.GetComponentInChildren<CityForgeCameraController>();if(!rig)return;
            slider.SetValueWithoutNotify(rig.ViewTilt);angle.text=rig.ViewPitch.ToString("0.0")+"°";
        }
    }
}

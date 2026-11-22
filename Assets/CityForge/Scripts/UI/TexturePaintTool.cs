using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Geometry;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.UI
{
    internal sealed class TexturePaintTool : MonoBehaviour
    {
        internal GroundPaintData Data { get; private set; }
        private readonly PointWirePath boundary = new();
        private readonly HashSet<GroundPaintTile> dirty = new();
        private readonly Stack<GroundPaintData> undo = new();
        private GroundPaintRenderer rendering;
        private Func<Vector2?> pointer; private Func<Vector2, Vector2> project;
        private Func<Vector2, float> height; private Predicate<Vector2> contains; private Func<bool> canEdit;
        private Action<GroundPaintData> committed; private Action closed;
        private Rect? paintClip;
        private readonly List<LineRenderer> markers = new();
        private RectTransform panel; private Text hint, sizeText, featherText, modeText;
        private LineRenderer outline, brush; private Material lineMaterial;
        private float radius = 3, feather = 1f, nextDab;
        private bool erase, waiting = true, stroke, closing, freeSpray;
        private Button sprayButton, boundaryButton;
        private Vector2 last;
        private GroundPaintData before;
        private string texture = "packed-earth", group = "All";
        private ScrollRect paletteScroll;
        private Text paletteCount;
        private readonly Dictionary<string, Button> groupButtons = new();
        private RectTransform palette;
        private Button paintButton, eraseButton;
        private Font paintHeadingFont;
        private BrushFeedback feedback;
        internal void Initialize(Transform hud, GroundPaintData data, Func<Vector2?> getPointer, Func<Vector2, Vector2> screen,
            Func<Vector2, float> ground, Predicate<Vector2> allowed, Func<bool> editable, Action<GroundPaintData> changed,
            Action finished, Rect? clip = null)
        {
            paintClip = clip;
            Data = (data ?? new GroundPaintData()).Copy(); pointer = getPointer; project = screen; height = ground;
            contains = allowed; canEdit = editable; committed = changed; closed = finished;
            rendering = gameObject.AddComponent<GroundPaintRenderer>(); rendering.Initialize(Data, ground, clip);
            lineMaterial = new Material(Resources.Load<Shader>("UI/ForestRemovalCircle")); lineMaterial.SetColor("_Color", new Color(1,.86f,.52f,.96f));
            outline = Line("Temporary paint boundary", .1f); brush = Line("Texture brush radius", .08f); brush.loop = true;
            BuildPanel(hud); feedback=BrushFeedback.Create(hud); SetHint("Click around the area · Click the first marker to close · Backspace undoes a marker");
        }
        private LineRenderer Line(string title, float width)
        {
            var go = new GameObject(title); go.layer = gameObject.layer; go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = lineMaterial; line.useWorldSpace = false;
            line.widthMultiplier = width; line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false; return line;
        }
        private void BuildPanel(Transform hud)
        {
            panel = Rect(hud,"Texturize",new Vector2(24,-132),new Vector2(374,692));
            var skin=panel.gameObject.AddComponent<Image>();skin.sprite=ZoneModalArtwork.Panel;skin.type=Image.Type.Sliced;
            var title=Label(panel,"Paint Textures",new Vector2(16,-18),new Vector2(340,40),29);
            title.font=paintHeadingFont=Font.CreateDynamicFontFromOSFont(new[]{"Georgia","Times New Roman","Liberation Serif"},29);
            foreach(var pair in new[]{("All",18f),("Natural",132f),("Paving",246f)})
            {
                string name=pair.Item1;
                groupButtons[name]=Button(panel,name,new Vector2(pair.Item2,-62),new Vector2(108,34),()=>{group=name;Populate();paletteScroll.verticalNormalizedPosition=1;});
            }
            var window=Rect(panel,"Scroll path textures",new Vector2(18,-108),new Vector2(338,222));
            window.gameObject.AddComponent<Image>().color=new Color(.02f,.03f,.04f,.7f);
            window.gameObject.AddComponent<RectMask2D>();
            paletteScroll=window.gameObject.AddComponent<ScrollRect>();paletteScroll.viewport=window;
            paletteScroll.horizontal=false;paletteScroll.scrollSensitivity=28;paletteScroll.movementType=ScrollRect.MovementType.Clamped;
            palette=Rect(window,"All available ground textures",Vector2.zero,new Vector2(324,222));paletteScroll.content=palette;
            var bar=Rect(window,"Texture scrollbar",new Vector2(328,0),new Vector2(10,222));bar.gameObject.AddComponent<Image>().color=new Color(.19f,.23f,.25f);
            var scrollbar=bar.gameObject.AddComponent<Scrollbar>();var handle=Rect(bar,"Handle",Vector2.zero,Vector2.zero);handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.pivot=new Vector2(.5f,.5f);var grip=handle.gameObject.AddComponent<Image>();grip.sprite=ZoneModalArtwork.Gold;grip.type=Image.Type.Sliced;
            scrollbar.handleRect=handle;scrollbar.targetGraphic=grip;scrollbar.direction=Scrollbar.Direction.BottomToTop;paletteScroll.verticalScrollbar=scrollbar;
            paletteCount=Label(panel,"",new Vector2(18,-338),new Vector2(338,30),16);Populate();
            sizeText=Label(panel,"Brush size: 6 m",new Vector2(18,-378),new Vector2(338,26),19);
            Slider(panel,new Vector2(22,-416),1,32,6,v=>{radius=v*.5f;sizeText.text=$"Brush size: {v:0.0} m";});
            featherText=Label(panel,"Feather: 1.0 m",new Vector2(18,-440),new Vector2(338,26),19);
            Slider(panel,new Vector2(22,-478),0,8,1,v=>{feather=v;featherText.text=v<=0?"Feather: 0 m · Hard edge":$"Feather: {v:0.0} m";});
            var paint=paintButton=Button(panel,"Paint",new Vector2(18,-506),new Vector2(160,40),()=>{erase=false;Mode();});
            eraseButton=Button(panel,"Erase",new Vector2(194,-506),new Vector2(160,40),()=>{erase=true;Mode();});
            modeText=paint.GetComponentInChildren<Text>(); Mode();
            boundaryButton=Button(panel,"Boundary",new Vector2(18,-550),new Vector2(160,32),()=>SetFreeSpray(false));
            sprayButton=Button(panel,"Free spray",new Vector2(194,-550),new Vector2(160,32),()=>SetFreeSpray(true));
            Button(panel,"New boundary",new Vector2(18,-594),new Vector2(160,32),()=>{SetFreeSpray(false);boundary.Clear();});
            Button(panel,"Undo stroke",new Vector2(194,-594),new Vector2(160,32),Undo);
            Button(panel,"Done",new Vector2(18,-638),new Vector2(336,32),Close);
            hint=Label(hud,"",new Vector2(420,-92),new Vector2(950,70),19);
            SetFreeSpray(false);Canvas.ForceUpdateCanvases();paletteScroll.verticalNormalizedPosition=1;
        }
        private void SetFreeSpray(bool value)
        {
            FinishStroke();freeSpray=value;waiting=true;boundary.Clear();
            boundaryButton.image.sprite=value?ZoneModalArtwork.Dark:ZoneModalArtwork.Gold;
            sprayButton.image.sprite=value?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;Mode();
        }
        private void Mode(){if(modeText)modeText.text="Paint";if(paintButton)paintButton.image.sprite=erase?ZoneModalArtwork.Dark:ZoneModalArtwork.Gold;if(eraseButton)eraseButton.image.sprite=erase?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark; SetHint(freeSpray ? "Hold the mouse to spray anywhere · Feather softens the brush edge · Esc finishes" : erase?"Erase inside the boundary · Original ground shows through":"Outline an area, then hold the mouse to paint inside it · Esc finishes");}
        private void Populate()
        {
            foreach(Transform child in palette){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var styles=LotPathMaterial.All.Where(s=>s.IsAvailable&&(group=="All"||s.Group==group)).ToArray();
            palette.sizeDelta=new Vector2(324,Mathf.Max(222,Mathf.CeilToInt(styles.Length/3f)*112));
            foreach(var button in groupButtons)button.Value.image.sprite=button.Key==group?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
            if(paletteCount)paletteCount.text=$"{styles.Length} ground textures · Scroll to browse";
            foreach(var pair in styles.Select((style,index)=>(style,index)))
            {
                var style=pair.style;int i=pair.index;
                var button=Button(palette,style.Label,new Vector2(i%3*108,-(i/3)*112),new Vector2(102,102),()=>{texture=style.Id;erase=false;Populate();Mode();});
                var photo=Rect(button.transform,"Texture thumbnail",new Vector2(4,-4),new Vector2(94,94));
                var image=photo.gameObject.AddComponent<RawImage>(); image.texture=style.Texture; image.color=style.Texture?Color.white:style.Fallback; image.raycastTarget=false;
                var text=button.GetComponentInChildren<Text>();text.transform.SetAsLastSibling();text.rectTransform.anchoredPosition=new Vector2(0,-32);text.rectTransform.sizeDelta=new Vector2(102,38);text.fontSize=12;
                var backing=text.gameObject.AddComponent<Outline>();backing.effectColor=Color.black;backing.effectDistance=Vector2.one;
                if(style.Id==texture)button.GetComponent<Image>().sprite=ZoneModalArtwork.Gold;
            }
        }
        private void Update()
        {
            if(closing)return;
            if(!canEdit()){FinishStroke();outline.enabled=brush.enabled=false;feedback?.Hide();return;}
            if(!Application.isFocused){FinishStroke();feedback?.Hide();return;}
            if(Input.GetKeyDown(KeyCode.Escape)){Close();return;}
            if(Input.GetKeyDown(KeyCode.Backspace)&&!boundary.Closed)boundary.Undo();
            if(waiting){if(!Input.GetMouseButton(0))waiting=false;feedback?.Hide();return;}
            var point=pointer();
            if(!point.HasValue||!contains(point.Value)){FinishStroke();brush.enabled=false;feedback?.Hide();DrawBoundary(null);return;}
            DrawBoundary(point);
            if(!freeSpray&&!boundary.Closed)
            {
                brush.enabled=false;feedback?.Hide();
                if(Input.GetMouseButtonDown(0))
                {
                    bool close=boundary.Points.Count>=3&&Vector2.Distance(project(boundary.Points[0]),project(point.Value))<=24;
                    if(close)
                    {
                        if(!PointWireGeometry.IsSimple(boundary.Points)||Mathf.Abs(PointWireGeometry.Area(boundary.Points))<1){SetHint("Outline a simple area without crossing edges.");return;}
                        boundary.TryClose();waiting=true;SetHint("Hold the mouse to spray inside the boundary · Esc finishes");
                    }
                    else boundary.TryAdd(point.Value);
                }
                return;
            }
            DrawBrush(point.Value);
            if(!Input.GetMouseButton(0)){FinishStroke();return;}
            if(!stroke){before=Data.Copy();stroke=true;last=point.Value;nextDab=0;}
            if(Time.unscaledTime<nextDab&&Vector2.Distance(last,point.Value)<radius*.2f)return;
            nextDab=Time.unscaledTime+.085f;
            int steps=Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(last,point.Value)/Mathf.Max(.2f,radius*.2f)),1,64);
            dirty.Clear();string problem=null;
            for(int i=1;i<=steps;i++)problem=Data.Dab(Vector2.Lerp(last,point.Value,(float)i/steps),radius,feather,texture,freeSpray?null:boundary.Points,erase,contains,dirty)??problem;
            foreach(var tile in dirty)rendering.Refresh(tile);
            if(problem!=null)SetHint(problem);
            last=point.Value;
        }
        private void DrawBoundary(Vector2? pointerPoint)
        {
            int count=boundary.Points.Count;outline.enabled=count>0;outline.loop=boundary.Closed;
            outline.positionCount=count+(!boundary.Closed&&pointerPoint.HasValue?1:0);
            for(int i=0;i<count;i++)outline.SetPosition(i,Ground(boundary.Points[i],.3f));
            while(markers.Count<count){var marker=Line("Paint boundary marker",.07f);marker.loop=true;markers.Add(marker);}
            for(int i=0;i<markers.Count;i++)
            {
                var marker=markers[i];marker.enabled=i<count;if(i>=count)continue;
                var p=boundary.Points[i];float pixels=Vector2.Distance(project(p),project(p+Vector2.right));
                float r=Mathf.Clamp((i==0?9f:5f)/Mathf.Max(.01f,pixels),.08f,8f);marker.positionCount=20;
                for(int j=0;j<20;j++){float a=j*Mathf.PI*2/20;marker.SetPosition(j,Ground(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,.32f));}
            }
            if(outline.positionCount>count)outline.SetPosition(count,Ground(pointerPoint.Value,.3f));
        }
        private Vector3 Ground(Vector2 point,float lift)=>new(point.x,height(point)+lift,point.y);
        private void DrawBrush(Vector2 point)
        {
            brush.enabled=false;
            feedback?.Show(point,radius,project,Input.mousePosition);
        }
        private void OnDisable(){feedback?.Hide();}

        internal void FinishStroke()
        {
            if(!stroke)return;stroke=false;Data.PruneEmpty();Data.Flush();rendering.RemoveMissing(Data);
            if(JsonUtility.ToJson(before)!=JsonUtility.ToJson(Data))
            {
                if(undo.Count==12){var keep=undo.Take(11).Reverse().ToArray();undo.Clear();foreach(var snapshot in keep)undo.Push(snapshot);}
                undo.Push(before);committed?.Invoke(Data.Copy());
            }
            before=null;
        }
        private void Undo()
        {
            FinishStroke();if(undo.Count==0)return;Data=undo.Pop();
            foreach(Transform child in transform)if(child.name=="Paint weight tile"){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            Destroy(rendering);rendering=gameObject.AddComponent<GroundPaintRenderer>();rendering.Initialize(Data,height,paintClip);
            committed?.Invoke(Data.Copy());waiting=true;
        }
        internal void Close(){if(closing)return;FinishStroke();closing=true;gameObject.SetActive(false);if(panel)panel.gameObject.SetActive(false);closed?.Invoke();Destroy(gameObject);}
        private void OnDestroy(){if(feedback){feedback.Hide();Destroy(feedback.gameObject);}if(panel)Destroy(panel.gameObject);if(hint)Destroy(hint.gameObject);if(lineMaterial)Destroy(lineMaterial);if(paintHeadingFont)Destroy(paintHeadingFont);}
        private void SetHint(string text){if(hint)hint.text=text;}
        private static RectTransform Rect(Transform parent,string name,Vector2 at,Vector2 size)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=at;r.sizeDelta=size;return r;}
        private static Text Label(Transform parent,string text,Vector2 at,Vector2 size,int fontSize)
        {var r=Rect(parent,"Text",at,size);var t=r.gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=fontSize;t.color=new Color(1,.89f,.68f);t.text=text;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        private static Button Button(Transform parent,string name,Vector2 at,Vector2 size,UnityEngine.Events.UnityAction action)
        {var r=Rect(parent,name,at,size);var image=r.gameObject.AddComponent<Image>();image.sprite=ZoneModalArtwork.Dark;image.type=Image.Type.Sliced;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(action);var t=Label(r,name,Vector2.zero,size,17);t.rectTransform.anchorMin=t.rectTransform.anchorMax=t.rectTransform.pivot=new Vector2(.5f,.5f);return b;}
        private static void Slider(Transform parent,Vector2 at,float min,float max,float value,UnityEngine.Events.UnityAction<float> change)
        {
            var r=Rect(parent,"Brush slider",at,new Vector2(330,16));r.gameObject.AddComponent<Image>().color=new Color(.2f,.25f,.28f);
            var s=r.gameObject.AddComponent<UnityEngine.UI.Slider>();var h=Rect(r,"Handle",Vector2.zero,new Vector2(20,24));h.gameObject.AddComponent<Image>().sprite=ZoneModalArtwork.Gold;h.pivot=new Vector2(.5f,.5f);h.sizeDelta=new Vector2(20,8);s.handleRect=h;s.targetGraphic=h.GetComponent<Image>();s.minValue=min;s.maxValue=max;s.value=value;s.onValueChanged.AddListener(change);
        }
    }
}

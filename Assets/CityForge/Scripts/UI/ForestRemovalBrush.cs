using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CityForge.World;
namespace CityForge.UI
{
    /// <summary>Continuous click-and-drag forest removal and planting. Brush dimensions are circumference in physical world metres.</summary>
    public sealed class ForestRemovalBrush : MonoBehaviour
    {
        public bool Armed {get;private set;}
        public bool Large {get;private set;}
        public bool Adding {get;private set;}
        public int LastAdded {get;private set;}
        int species;
        public float Circumference=>Large?400:200;
        public float Radius=>Circumference/(2*Mathf.PI);
        public int LastRemoved {get;private set;}
        public bool Dragging {get;private set;}
        bool hasDragPoint;Vector2 lastDragPoint;readonly List<Vector2> dragSamples=new();
        PrototypeRegionView view;NatureToolbar toolbar;Camera cameraView;RectTransform cursor,canvasRect;Text sizeLabel,actionGlyph;LineRenderer ring;Material ringMaterial;
        TerrainCollider brushCollider;bool colliderWasEnabled;
        bool savedCursor,waitRelease,previewVisible;readonly List<RaycastResult> hits=new();PointerEventData pointer;
        public void Initialize(PrototypeRegionView source,NatureToolbar tools,Transform hud)
        {
            view=source;toolbar=tools;cameraView=view.GetComponentInChildren<Camera>();canvasRect=hud.GetComponent<RectTransform>();
            var go=new GameObject("ForestRemovalCursor",typeof(RectTransform));go.transform.SetParent(hud,false);cursor=go.GetComponent<RectTransform>();cursor.anchorMin=cursor.anchorMax=new Vector2(.5f,.5f);cursor.pivot=new Vector2(0,1);cursor.sizeDelta=new Vector2(56,64);
            var icon=new GameObject("Tree",typeof(RectTransform),typeof(RawImage),typeof(Shadow));icon.transform.SetParent(cursor,false);var r=icon.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(0,16);r.offsetMax=Vector2.zero;
            var image=icon.GetComponent<RawImage>();image.texture=Resources.Load<Texture2D>("UI/NatureToolbarIcons");image.uvRect=new Rect(.5f,2f/3,.25f,1f/3);image.raycastTarget=false;icon.GetComponent<Shadow>().effectDistance=new Vector2(1,-1);
            Text Label(string name,string text,int size,Color color){var child=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Shadow));child.transform.SetParent(cursor,false);var t=child.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=text;t.fontSize=size;t.color=color;t.raycastTarget=false;return t;}
            var x=Label("Remove","×",26,new Color(1,.25f,.18f));actionGlyph=x;x.fontStyle=FontStyle.Bold;x.verticalOverflow=VerticalWrapMode.Overflow;x.horizontalOverflow=HorizontalWrapMode.Overflow;x.rectTransform.anchorMin=x.rectTransform.anchorMax=new Vector2(1,1);x.rectTransform.pivot=Vector2.one;x.rectTransform.anchoredPosition=new Vector2(6,2);x.rectTransform.sizeDelta=new Vector2(32,38);
            sizeLabel=Label("Size","200 m",14,new Color(1,.7f,.6f));sizeLabel.alignment=TextAnchor.MiddleCenter;sizeLabel.rectTransform.anchorMin=Vector2.zero;sizeLabel.rectTransform.anchorMax=new Vector2(1,0);sizeLabel.rectTransform.pivot=new Vector2(.5f,0);sizeLabel.rectTransform.anchoredPosition=Vector2.zero;sizeLabel.rectTransform.sizeDelta=new Vector2(56,18);
            var circle=new GameObject("ForestRemovalCircle",typeof(LineRenderer));circle.transform.SetParent(transform,false);ring=circle.GetComponent<LineRenderer>();ring.loop=true;ring.useWorldSpace=true;ring.positionCount=96;ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;ring.receiveShadows=false;
            ringMaterial=new Material(Resources.Load<Shader>("UI/ForestRemovalCircle"));ring.sharedMaterial=ringMaterial;ring.enabled=false;cursor.gameObject.SetActive(false);
            toolbar.ToolSelected+=_=>Cancel();toolbar.Popup.OptionChanged+=_=>{if(Armed)Cancel();};
            toolbar.Popup.OptionInvoked+=option=>{if(toolbar.Selected==NatureTool.Trees&&option<=1)Arm(option==0);else Cancel();};
        }
        public void Arm(bool adding=false)
        {
            if(!view||view.SelectedDistrict<0||view.DistrictLoading)return;
            if(Armed)Cancel();Adding=adding;species=toolbar.Popup.ChoiceIndex("Species");actionGlyph.text=adding?"+":"×";actionGlyph.color=adding?new Color(.3f,1,.4f):new Color(1,.25f,.18f);ringMaterial.SetColor("_Color",adding?new Color(.2f,1,.3f,.95f):new Color(1,.12f,.08f,.9f));savedCursor=Cursor.visible;Armed=true;Large=false;sizeLabel.text="200 m";waitRelease=Input.GetMouseButton(0);brushCollider=view.ApprovedDistrict.DisplayTerrain.GetComponent<TerrainCollider>();colliderWasEnabled=brushCollider.enabled;brushCollider.enabled=true;toolbar.Popup.gameObject.SetActive(false);
        }
        public void ToggleSize(){Large=!Large;hasDragPoint=false;if(sizeLabel)sizeLabel.text=Circumference.ToString("0")+" m";}
        public void Cancel()
        {
            if(Armed){Cursor.visible=savedCursor;if(brushCollider)brushCollider.enabled=colliderWasEnabled;}brushCollider=null;Armed=false;EndDrag();if(cursor)cursor.gameObject.SetActive(false);previewVisible=false;if(ring)ring.enabled=false;
        }
        public int RemoveAt(Vector2 center)
        {
            if(!Armed||!view||!view.ApprovedDistrict||view.DistrictLoading||view.IsSaving)return 0;
            LastRemoved=view.ApprovedDistrict.RemoveForest(center,Radius);return LastRemoved;
        }
        public int AddAt(Vector2 center)
        {
            if(!Armed||!Adding||!view||!view.ApprovedDistrict||view.DistrictLoading||view.IsSaving)return 0;
            LastAdded=view.ApprovedDistrict.AddForest(center,Radius,species);return LastAdded;
        }
        int PaintAt(Vector2 center)=>Adding?AddAt(center):RemoveAt(center);
        public int BeginDrag(Vector2 center)
        {
            if(!Armed)return 0;Dragging=true;hasDragPoint=true;lastDragPoint=center;return PaintAt(center);
        }
        public int ContinueDrag(Vector2 center,bool finish=false)
        {
            if(!Armed||!Dragging||!view||!view.ApprovedDistrict)return 0;
            if(!hasDragPoint){hasDragPoint=true;lastDragPoint=center;return PaintAt(center);}
            float distance=Vector2.Distance(lastDragPoint,center),spacing=Radius*.25f;
            if(distance<spacing&&!finish)return 0;if(distance<.01f)return 0;
            dragSamples.Clear();var start=lastDragPoint;var direction=(center-start)/distance;
            int steps=Mathf.Min(1024,Mathf.FloorToInt(distance/spacing));if(steps==1024)spacing=distance/steps;
            for(int i=1;i<=steps;i++)dragSamples.Add(start+direction*(i*spacing));
            if(finish&&(dragSamples.Count==0||Vector2.Distance(dragSamples[dragSamples.Count-1],center)>.01f))dragSamples.Add(center);
            if(dragSamples.Count==0)return 0;lastDragPoint=dragSamples[dragSamples.Count-1];
            if(Adding){LastAdded=view.ApprovedDistrict.AddForest(dragSamples,Radius,species);return LastAdded;}
            LastRemoved=view.ApprovedDistrict.RemoveForest(dragSamples,Radius);return LastRemoved;
        }
        public void EndDrag(){Dragging=false;hasDragPoint=false;}
        bool OverUI()
        {
            if(!EventSystem.current)return false;if(pointer==null)pointer=new PointerEventData(EventSystem.current);
            pointer.position=Input.mousePosition;hits.Clear();EventSystem.current.RaycastAll(pointer,hits);return hits.Count>0;
        }
        void Update()
        {
            if(!Armed)return;
            if(!Application.isFocused){EndDrag();previewVisible=false;cursor.gameObject.SetActive(false);ring.enabled=false;Cursor.visible=savedCursor;return;}
            if(!view||view.SelectedDistrict<0||view.DistrictLoading||toolbar.Selected!=NatureTool.Trees){Cancel();return;}
            if(Input.GetKeyDown(KeyCode.Tab))ToggleSize();
            if(waitRelease){if(!Input.GetMouseButton(0))waitRelease=false;}
            var land=view.ApprovedDistrict;bool valid=land&&land.TreesEnabled&&!view.IsSaving&&!OverUI();RaycastHit hit=default;
            if(valid){var collider=land.DisplayTerrain.GetComponent<TerrainCollider>();valid=collider&&collider.enabled&&collider.Raycast(cameraView.ScreenPointToRay(Input.mousePosition),out hit,100000)&&land.Contains(new Vector2(hit.point.x,hit.point.z));}
            previewVisible=valid;cursor.gameObject.SetActive(valid);ring.enabled=valid;Cursor.visible=valid?false:savedCursor;
            if(!valid){hasDragPoint=false;if(!Input.GetMouseButton(0))EndDrag();return;}
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,Input.mousePosition,null,out var point);cursor.anchoredPosition=point+new Vector2(13,-10);
            ring.gameObject.SetActive(true);ring.widthMultiplier=Mathf.Clamp(cameraView.orthographicSize*.003f,.08f,1.2f);
            for(int i=0;i<96;i++){float angle=i*Mathf.PI*2/96;var p=new Vector2(hit.point.x+Mathf.Cos(angle)*Radius,hit.point.z+Mathf.Sin(angle)*Radius);ring.SetPosition(i,new Vector3(p.x,land.Height(p)+.15f,p.y));}
            if(waitRelease)return;var center=new Vector2(hit.point.x,hit.point.z);
            if(Input.GetMouseButtonDown(0))BeginDrag(center);
            else if(Dragging&&Input.GetMouseButton(0))ContinueDrag(center);
            else if(Dragging){ContinueDrag(center,true);EndDrag();}
        }
        void BeginCamera(UnityEngine.Rendering.ScriptableRenderContext context,Camera camera){if(ring)ring.enabled=previewVisible&&camera==cameraView;}
        void EndCamera(UnityEngine.Rendering.ScriptableRenderContext context,Camera camera){if(ring)ring.enabled=previewVisible;}
        void OnEnable(){UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering+=BeginCamera;UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=EndCamera;}
        void OnDisable(){UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering-=BeginCamera;UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=EndCamera;Cancel();}
        void OnDestroy(){Cancel();if(ringMaterial)Destroy(ringMaterial);if(cursor)Destroy(cursor.gameObject);}
    }
}

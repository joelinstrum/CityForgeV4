using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
namespace CityForge.UI
{
    /// <summary>Reusable surface marker/string interaction. Consumers own completion semantics.</summary>
    public sealed class PointWireTool : MonoBehaviour
    {
        public bool Armed {get;private set;}
        public bool Closed=>path.Closed;
        public bool Finished=>path.Finished;
        public bool OpenPath;public float RibbonWidth;
        public Color? RibbonFillColor;
        readonly List<Vector2> ribbonPoints = new();
        public bool HandleEscape = true;
        public bool RepeatOnCompletion;
        public bool RestrictUiToHud;
        public bool FinishOverUi;
        public Func<IReadOnlyList<Vector2>, string> LoopCompletionRequested;
        public bool DeferCompletion;public string ProcessingMessage="Creating…";
        public bool Processing {get;private set;}
        public event Action<bool> ProcessingChanged;
        RegionLoadingOverlay completionOverlay;bool acceptingCompletion;
        public Func<Vector2,Vector2> SnapPoint;
        public Func<IReadOnlyList<Vector2>, bool, IReadOnlyList<Vector2>> ProjectGuide;
        public string DrawingHint;
        public Func<IReadOnlyList<Vector2>,string> CompletionRequested;
        public string Label="Outline";public string CompletedHint="Outline complete · Esc to clear";public bool AllowClosedUndo=true;
        public event Action<IReadOnlyList<Vector2>> Completed;
        public IReadOnlyList<Vector2> Points=>points;
        readonly PointWirePath path=new();
        IReadOnlyList<Vector2> points=>path.Points;
        readonly List<LineRenderer> markers=new();
        readonly List<RaycastResult> hits=new();
        PointWireSurface surface;Camera cameraView;
        RectTransform canvasRect;Text cursor,hint;Material material;PointWireGraphic overlay;
        readonly List<Vector3> boundary=new(),preview=new();
        Collider terrainCollider;bool colliderWasEnabled,savedCursor,waitRelease;
        PointerEventData pointer;
        Color Blue;
        public void Initialize(Camera camera,Transform hud,PointWireSurface target,Color color)
        {
            Blue=color;surface=target;cameraView=camera;canvasRect=hud.GetComponent<RectTransform>();
            Text Text(string name,string value,Vector2 size){var go=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Shadow));go.transform.SetParent(hud,false);var t=go.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=value;t.color=Blue;t.fontSize=24;t.raycastTarget=false;t.rectTransform.sizeDelta=size;return t;}
            cursor=Text("PointWireCursor","↖",new Vector2(45,48));cursor.fontSize=40;cursor.fontStyle=FontStyle.Bold;cursor.rectTransform.pivot=new Vector2(0,1);
            hint=Text("PointWireHint","",new Vector2(900,65));hint.fontSize=19;hint.alignment=TextAnchor.MiddleCenter;hint.rectTransform.anchorMin=hint.rectTransform.anchorMax=new Vector2(.5f,1);hint.rectTransform.pivot=new Vector2(.5f,1);hint.rectTransform.anchoredPosition=new Vector2(0,-85);
            material=new Material(Resources.Load<Shader>("UI/ForestRemovalCircle"));material.SetColor("_Color",Blue);
            var drawing=new GameObject("PointWireOverlay",typeof(RectTransform),typeof(CanvasRenderer),typeof(PointWireGraphic));drawing.transform.SetParent(hud,false);overlay=drawing.GetComponent<PointWireGraphic>();overlay.raycastTarget=false;overlay.color=Blue;overlay.rectTransform.sizeDelta=canvasRect.rect.size;drawing.transform.SetSiblingIndex(cursor.transform.GetSiblingIndex());cursor.gameObject.SetActive(false);hint.gameObject.SetActive(false);
        }
        LineRenderer Line(string name){var go=new GameObject(name,typeof(LineRenderer));go.transform.SetParent(transform,false);go.layer=2;var line=go.GetComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=0;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;line.numCapVertices=4;return line;}
        public void Arm()
        {
            Cancel();if(surface==null||!surface.Available)return;
            terrainCollider=surface.Collider;if(!terrainCollider)return;
            savedCursor=Cursor.visible;Armed=true;waitRelease=Input.GetMouseButton(0);
            colliderWasEnabled=terrainCollider.enabled;terrainCollider.enabled=true;
            hint.gameObject.SetActive(true);Hint();
        }
        public void SetMessage(string message){hint.text=message;}
        void Hint(){hint.text=Finished?CompletedHint:!string.IsNullOrEmpty(DrawingHint)?DrawingHint:points.Count==0?Label+" · Click to place the first marker · Esc to cancel":"Click to add a marker · Click the first marker to close · Backspace to undo · Esc to cancel";}
        Vector3 Ground(Vector2 p)=>new Vector3(p.x,surface.Height(p)+.25f,p.y);
        public bool PlaceMarker(Vector2 p,bool closeToStart=false)
        {
            if(!Armed||Finished||Processing||surface==null||!surface.CanEdit)return false;
            if(SnapPoint!=null)p=SnapPoint(p);
            if(!surface.Contains(p))return false;
            if(closeToStart)return (!OpenPath || LoopCompletionRequested != null) && Complete(closeLoop: true);
            if(!path.TryAdd(p))return false;
            markers.Add(Line("PointWireMarker"+points.Count));Rebuild();Hint();return true;
        }
        public bool CompletePath()=>OpenPath&&Complete(closeLoop: false);
        bool Complete(bool closeLoop)
        {
            if(!Armed||Finished||Processing||!surface.CanEdit||points.Count<(closeLoop?3:2))return false;
            if(DeferCompletion){
                Processing=true;cursor.gameObject.SetActive(false);Cursor.visible=savedCursor;overlay.Clear();foreach(var marker in markers)marker.gameObject.SetActive(false);
                SetMessage(ProcessingMessage);hint.gameObject.SetActive(false);ProcessingChanged?.Invoke(true);
                completionOverlay=RegionLoadingOverlay.ShowOperation(ProcessingMessage);
                completionOverlay.BeginDistrict(CompleteDeferred(closeLoop),()=>{completionOverlay=null;Processing=false;if(hint)hint.gameObject.SetActive(Armed);ProcessingChanged?.Invoke(false);RearmAfterCompletion();},error=>{if(Armed){SetMessage("Could not create waterway. Please adjust the route and try again.");foreach(var marker in markers)marker.gameObject.SetActive(true);}Debug.LogException(error);},ProcessingMessage);
                return true;
            }
            return CompleteNow(closeLoop);
        }
        System.Collections.IEnumerator CompleteDeferred(bool closeLoop){CompleteNow(closeLoop);yield break;}
        bool CompleteNow(bool closeLoop)
        {
            var request = closeLoop && LoopCompletionRequested != null ? LoopCompletionRequested : CompletionRequested;
            string error=request?.Invoke(points);if(!string.IsNullOrEmpty(error)){SetMessage(error);foreach(var marker in markers)marker.gameObject.SetActive(true);return false;}
            if(closeLoop)path.TryClose();else path.TryFinishOpen();preview.Clear();Rebuild();Hint();acceptingCompletion=true;try{Completed?.Invoke(points);}finally{acceptingCompletion=false;}
            if (!Processing) RearmAfterCompletion();
            return true;
        }
        void RearmAfterCompletion()
        {
            if (RepeatOnCompletion && Armed && Finished) Arm();
        }

        public void UndoMarker()
        {
            if(!Armed||Processing||points.Count==0)return;
            if(Finished&&!AllowClosedUndo)return;
            bool closed=Finished;path.Undo();if(!closed){Destroy(markers[markers.Count-1].gameObject);markers.RemoveAt(markers.Count-1);}
            preview.Clear();Rebuild();Hint();
        }
        void Segment(List<Vector3> result,Vector2 a,Vector2 b)
        {
            int n=Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(a,b)/4),1,RibbonWidth>0?16:32);
            for(int i=0;i<n;i++)result.Add(Ground(Vector2.Lerp(a,b,(float)i/n)));
        }
        void Rebuild()
        {
            BuildGuide(boundary, ProjectGuide?.Invoke(points, Closed) ?? points, Closed);
            UpdateWidths();
        }
        void BuildGuide(List<Vector3> target, IReadOnlyList<Vector2> guide, bool closed)
        {
            target.Clear();
            for (int i = 1; i < guide.Count; i++) Segment(target, guide[i - 1], guide[i]);
            if (closed) Segment(target, guide[guide.Count - 1], guide[0]);
            if (guide.Count > 0) target.Add(Ground(closed ? guide[0] : guide[guide.Count - 1]));
        }
        void UpdateWidths()
        {
            var displayPoints = ProjectGuide?.Invoke(points, Closed) ?? points;
            float width=Mathf.Clamp(cameraView.orthographicSize*.003f,.1f,2);
            for(int i=0;i<markers.Count;i++){
                var marker=markers[i];marker.widthMultiplier=width*1.5f;marker.loop=true;marker.positionCount=24;
                float radius=width*(i==0?5:3);
                for(int j=0;j<24;j++){float a=j*Mathf.PI*2/24;marker.SetPosition(j,Ground(displayPoints[i]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius));}
            }
        }
        public void Cancel()
        {
            if(!acceptingCompletion&&completionOverlay){var running=completionOverlay;completionOverlay=null;Destroy(running.gameObject);}
            if(!acceptingCompletion&&Processing){Processing=false;ProcessingChanged?.Invoke(false);}
            if(Armed){Cursor.visible=savedCursor;if(terrainCollider)terrainCollider.enabled=colliderWasEnabled;}
            Armed=false;terrainCollider=null;path.Clear();foreach(var marker in markers)if(marker)Destroy(marker.gameObject);markers.Clear();
            boundary.Clear();preview.Clear();if(overlay)overlay.Clear();if(cursor)cursor.gameObject.SetActive(false);if(hint)hint.gameObject.SetActive(false);
        }
        bool OverUI()
        {
            if(!EventSystem.current)return false;if(pointer==null)pointer=new PointerEventData(EventSystem.current);
            pointer.position=Input.mousePosition;hits.Clear();EventSystem.current.RaycastAll(pointer,hits);
            if (!RestrictUiToHud) return hits.Count > 0;
            foreach (var hit in hits)
                if (hit.gameObject.transform.IsChildOf(canvasRect)) return true;
            return false;
        }
        void Update()
        {
            if(!Armed||Processing)return;
            if(surface==null||!surface.Available){Cancel();return;}
            if(HandleEscape&&Input.GetKeyDown(KeyCode.Escape)){Cancel();return;}
            if(Input.GetKeyDown(KeyCode.Backspace))UndoMarker();
            if(OpenPath&&Application.isFocused&&(FinishOverUi||!OverUI())&&(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)))CompletePath();
            if(!Armed||Processing)return;
            bool valid=Application.isFocused&&surface.CanEdit&&!OverUI();RaycastHit hit=default;
            if(valid)valid=terrainCollider&&terrainCollider.Raycast(cameraView.ScreenPointToRay(Input.mousePosition),out hit,100000)&&surface.Contains(new Vector2(hit.point.x,hit.point.z));
            cursor.gameObject.SetActive(valid);Cursor.visible=valid?false:savedCursor;
            if(waitRelease&&!Input.GetMouseButton(0))waitRelease=false;
            UpdateWidths();preview.Clear();if(!valid)return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,Input.mousePosition,null,out var local);cursor.rectTransform.anchoredPosition=local;if(Finished)return;
            bool snap=false;if((!OpenPath || LoopCompletionRequested != null)&&points.Count>=3){var first=cameraView.WorldToScreenPoint(Ground((ProjectGuide?.Invoke(points, false) ?? points)[0]));snap=first.z>0&&Vector2.Distance(new Vector2(first.x,first.y),Input.mousePosition)<=24;}
            var p=snap?points[0]:new Vector2(hit.point.x,hit.point.z);if(SnapPoint!=null)p=SnapPoint(p);
            if (ProjectGuide != null && points.Count > 0)
            {
                var guide = new List<Vector2>(points);
                if (!snap) guide.Add(p);
                BuildGuide(preview, ProjectGuide(guide, snap), snap);
            }
            else if(points.Count>0){Segment(preview,points[points.Count-1],p);preview.Add(Ground(p));}
            if(!waitRelease&&Input.GetMouseButtonDown(0))PlaceMarker(p,snap);
        }
        void LateUpdate()
        {
            if(!Armed||Processing||!overlay)return;
            overlay.rectTransform.sizeDelta=canvasRect.rect.size;overlay.Clear();
            void DrawSegment(Vector3 worldA,Vector3 worldB){
                var a=cameraView.WorldToScreenPoint(worldA);var b=cameraView.WorldToScreenPoint(worldB);if(a.z<=0||b.z<=0)return;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,a,null,out var start);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,b,null,out var end);overlay.Add(start,end);
            }
            void Draw(List<Vector3> line){for(int i=1;i<line.Count;i++)DrawSegment(line[i-1],line[i]);}
            bool replaceBoundary = ProjectGuide != null && preview.Count > 0;
            if (RibbonWidth > 0f && RibbonFillColor is Color fillColor)
            {
                overlay.color = fillColor;
                if (!replaceBoundary) DrawFilledRibbon(boundary);
                DrawFilledRibbon(preview);
                overlay.SetVerticesDirty();
                return;
            }

            if (!replaceBoundary) Draw(boundary);
            Draw(preview);
            if(RibbonWidth>0){
                void Ribbon(List<Vector3> line){for(int i=1;i<line.Count;i++){var a=new Vector2(line[i-1].x,line[i-1].z);var b=new Vector2(line[i].x,line[i].z);var d=b-a;if(d.sqrMagnitude<.0001f)continue;var normal=new Vector2(-d.y,d.x).normalized*RibbonWidth*.5f;DrawSegment(Ground(a-normal),Ground(b-normal));DrawSegment(Ground(a+normal),Ground(b+normal));}}
                if (!replaceBoundary) Ribbon(boundary);
                Ribbon(preview);
            }
            overlay.SetVerticesDirty();
        }
        void DrawFilledRibbon(List<Vector3> line)
        {
            if (line.Count < 2) return;
            ribbonPoints.Clear();
            foreach (var point in line)
                ribbonPoints.Add(new Vector2(point.x, point.z));

            bool loop = ribbonPoints.Count > 2 && ribbonPoints[0] == ribbonPoints[^1];
            if (loop) ribbonPoints.RemoveAt(ribbonPoints.Count - 1);
            int segments = loop ? ribbonPoints.Count : ribbonPoints.Count - 1;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % ribbonPoints.Count;
                var a = ribbonPoints[i];
                var b = ribbonPoints[next];
                if (a == b) continue;
                var sideA = Lots.LotPathSurface.RibbonSide(ribbonPoints, null, loop, i, RibbonWidth);
                var sideB = Lots.LotPathSurface.RibbonSide(ribbonPoints, null, loop, next, RibbonWidth);
                if (Project(a - sideA, out var leftA) && Project(a + sideA, out var rightA) &&
                    Project(b + sideB, out var rightB) && Project(b - sideB, out var leftB))
                    overlay.AddFill(leftA, rightA, rightB, leftB);
            }

            bool Project(Vector2 point, out Vector2 local)
            {
                local = default;
                var screen = cameraView.WorldToScreenPoint(Ground(point));
                return screen.z > 0f &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
            }
        }

        void OnDisable(){Cancel();}
        void OnDestroy(){Cancel();if(material)Destroy(material);if(cursor)Destroy(cursor.gameObject);if(hint)Destroy(hint.gameObject);if(overlay)Destroy(overlay.gameObject);}
    }
    public sealed class PointWireGraphic : MaskableGraphic
    {
        readonly List<Vector2> segments=new();
        readonly List<Vector2> fills = new();
        public void Clear(){segments.Clear();fills.Clear();SetVerticesDirty();}
        public void AddFill(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            if (fills.Count + segments.Count * 2 >= 60000) return;
            fills.Add(a);
            fills.Add(b);
            fills.Add(c);
            fills.Add(d);
        }
        public void Add(Vector2 a,Vector2 b){segments.Add(a);segments.Add(b);}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var fillColor = color;
            fillColor.a *= .35f;
            for (int i = 0; i < fills.Count; i += 4)
            {
                for (int j = 0; j < 4; j++) vh.AddVert(fills[i + j], fillColor, Vector2.zero);
                vh.AddTriangle(i, i + 1, i + 2);
                vh.AddTriangle(i, i + 2, i + 3);
            }
            for(int i=0;i+1<segments.Count;i+=2){var a=segments[i];var b=segments[i+1];var delta=b-a;if(delta.sqrMagnitude<.0001f)continue;var n=new Vector2(-delta.y,delta.x).normalized*1.25f;int v=vh.currentVertCount;
                vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);
            }
        }
    }
}

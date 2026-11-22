using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace CityForge.UI
{
    /// <summary>HUD-space, non-interactive brush outline and spray-bottle pointer.</summary>
    internal sealed class BrushFeedback : MaskableGraphic
    {
        readonly List<Vector2> ring = new(96);
        static readonly Vector2[] Droplets={new(13,12),new(18,15),new(18,9),new(23,18),new(23,6)};
        Vector2 pointer;
        bool shown, savedCursor, ownsCursor;
        public static BrushFeedback Create(Transform hud)
        {
            var go = new GameObject("Spray brush outline and cursor", typeof(RectTransform), typeof(CanvasRenderer), typeof(BrushFeedback));
            go.transform.SetParent(hud, false);
            var graphic = go.GetComponent<BrushFeedback>();
            var rect = graphic.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            graphic.raycastTarget = false; graphic.color = new Color(1,.86f,.52f,1);
            return graphic;
        }
        Vector2 Local(Vector2 screen)
        {
            var camera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, camera, out var local);
            return local;
        }
        public void Show(Vector2 center, float radius, Func<Vector2,Vector2> project, Vector2 screenPointer)
        {
            ring.Clear();
            for(int i=0;i<96;i++) { float a=i*Mathf.PI*2/96; ring.Add(Local(project(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius))); }
            pointer=Local(screenPointer)+new Vector2(18,-22);shown=true;
            if(!ownsCursor){savedCursor=Cursor.visible;ownsCursor=true;}
            Cursor.visible=false;transform.SetAsLastSibling();SetVerticesDirty();
        }
        public void Hide()
        {
            shown=false;ring.Clear();if(ownsCursor){Cursor.visible=savedCursor;ownsCursor=false;}SetVerticesDirty();
        }
        protected override void OnDisable(){Hide();base.OnDisable();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!shown)return;
            // Dark outer stroke keeps the circle readable over pale pavement and bright textures.
            for(int i=0;i<ring.Count;i++) Line(vh,ring[i],ring[(i+1)%ring.Count],4.5f,new Color(0,0,0,.85f));
            for(int i=0;i<ring.Count;i++) Line(vh,ring[i],ring[(i+1)%ring.Count],2.2f,color);
            // Bottle body, neck, nozzle and spray droplets; authored in HUD units.
            Quad(vh,pointer+new Vector2(-5,-10),pointer+new Vector2(7,-10),pointer+new Vector2(7,6),pointer+new Vector2(-5,6),new Color(.06f,.09f,.08f,1));
            Line(vh,pointer+new Vector2(-5,-10),pointer+new Vector2(7,-10),2,color);
            Line(vh,pointer+new Vector2(-5,-10),pointer+new Vector2(-5,6),2,color);
            Line(vh,pointer+new Vector2(7,-10),pointer+new Vector2(7,6),2,color);
            Line(vh,pointer+new Vector2(-5,6),pointer+new Vector2(7,6),2,color);
            Line(vh,pointer+new Vector2(1,6),pointer+new Vector2(1,12),3,color);
            Line(vh,pointer+new Vector2(-2,12),pointer+new Vector2(8,12),3,color);
            foreach(var offset in Droplets)
                Line(vh,pointer+offset-Vector2.right,pointer+offset+Vector2.right,2,color);
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Quad(vh,a-n,a+n,b+n,b-n,tint);}
        static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
        {int n=vh.currentVertCount;vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);vh.AddVert(c,tint,Vector2.zero);vh.AddVert(d,tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}

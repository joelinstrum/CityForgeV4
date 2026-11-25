using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityForge.UI
{
    internal sealed partial class LotEditorViewport
    {
        private TexturePaintTool texturePaintTool;
        private readonly List<RaycastResult> paintHits = new();
        private int texturePaintReleaseFrame = -1;
        internal bool TexturePainting => texturePaintTool || Time.frameCount == texturePaintReleaseFrame;
        internal void EndTexturePaint(){if(texturePaintTool)texturePaintTool.Close();}
        internal void BeginTexturePaint(Transform hud, LotDefinition lot, Action<GroundPaintData> changed, Action finished)
        {
            EndTexturePaint();
            foreach(var paint in content.GetComponentsInChildren<GroundPaintRenderer>())paint.gameObject.SetActive(false);
            var go=new GameObject("Lot Texturize tool");go.layer=PreviewLayer;go.transform.SetParent(stage,false);
            texturePaintTool=go.AddComponent<TexturePaintTool>();
            texturePaintTool.Initialize(hud,lot.groundPaint,Pointer,Project,_=>0,lot.LandBounds.Contains,
                ()=>previewLot==lot,changed,()=>{texturePaintReleaseFrame=Time.frameCount;texturePaintTool=null;finished();},lot.LandBounds,EraAppearance.EffectiveEra(lot,ResidentialGround.LotEra(lot)));
            Vector2? Pointer()
            {
                var canvas=image.canvas;var eventCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                if(!RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform,Input.mousePosition,eventCamera))return null;
                if(EventSystem.current)
                {
                    paintHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=Input.mousePosition},paintHits);
                    if(paintHits.Count>0&&!paintHits[0].gameObject.transform.IsChildOf(image.transform)&&paintHits[0].gameObject!=image.gameObject)return null;
                }
                return TryRay(Input.mousePosition,eventCamera,out var ray)&&TryPoint(ray,out var point)?point:null;
            }
            Vector2 Project(Vector2 point)
            {
                var v=cameraView.WorldToViewportPoint(Origin+new Vector3(point.x,0,point.y));var rect=image.rectTransform.rect;
                var world=image.rectTransform.TransformPoint(new Vector3(rect.xMin+v.x*rect.width,rect.yMin+v.y*rect.height));
                return RectTransformUtility.WorldToScreenPoint(image.canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:image.canvas.worldCamera,world);
            }
        }
    }
    internal sealed partial class LotEditorPanel
    {
        private void OpenTexturePaint()
        {
            if(PlayerMode)return;
            CloseAssetPopup();CloseChoices();StopOverlay();
            if(drawingPath)FinishGroundPath(false);if(drawingFence)FinishFencePath(false);
            // The tool panel is a sibling under the HUD; disable other editor controls
            // while leaving the preview surface available for brush hit testing.
            var controls=GetComponentsInChildren<Selectable>(true);
            var enabled=controls.Select(control=>control.interactable).ToArray();
            foreach(var control in controls)control.interactable=false;
            viewport.BeginTexturePaint(GetComponentInParent<Canvas>().transform,session.Lot,
                data=>session.Edit(lot=>lot.groundPaint=data),()=>{
                    for(int i=0;i<controls.Length;i++)if(controls[i])controls[i].interactable=enabled[i];
                    Refresh();
                });
        }
    }
}

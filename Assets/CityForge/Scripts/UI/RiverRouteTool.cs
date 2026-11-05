using System.Collections.Generic;
using UnityEngine;
using CityForge.World;
namespace CityForge.UI
{
    public sealed class RiverRouteTool : MonoBehaviour
    {
        public PointWireTool Wire {get;private set;}
        public bool Stream {get;private set;}
        PrototypeRegionView view;NatureToolbar toolbar;
        public void Initialize(PrototypeRegionView source,NatureToolbar tools,Transform hud)
        {
            view=source;toolbar=tools;Wire=gameObject.AddComponent<PointWireTool>();Wire.Initialize(view.GetComponentInChildren<Camera>(),hud,new Surface(view),new Color(.2f,.65f,1));Wire.OpenPath=true;Wire.AllowClosedUndo=false;Wire.DeferCompletion=true;
            bool viewEnabled=true,cameraEnabled=true;var controls=view.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>();
            Wire.ProcessingChanged+=busy=>{if(busy){viewEnabled=view.enabled;cameraEnabled=controls.enabled;view.enabled=false;controls.enabled=false;}else if(view){view.enabled=viewEnabled;controls.enabled=cameraEnabled;}};
            Wire.CompletionRequested=p=>view.ApprovedDistrict.CreatePlayerChannel(p,Stream);Wire.Completed+=_=>{Wire.Cancel();toolbar.Dismiss();};Wire.SnapPoint=p=>view.SnapChannelPoint(p,Wire.Points.Count==0,Stream);
            toolbar.ToolSelected+=_=>Wire.Cancel();toolbar.Popup.OptionChanged+=_=>Wire.Cancel();toolbar.Popup.WaterBodySelected+=(adding,kind)=>{if(adding&&kind>0)Arm(kind==1);else Wire.Cancel();};
        }
        public void Arm(bool stream)
        {
            Stream=stream;toolbar.ForestBrush.Cancel();toolbar.LakeOutline.Cancel();Wire.Label=stream?"Stream":"River";Wire.ProcessingMessage=stream?"Creating stream…":"Creating river…";Wire.RibbonWidth=stream?6:18;
            Wire.DrawingHint=(stream?"Stream":"River")+" · Click downstream · Enter to create · Backspace to undo · Esc to cancel\nFinish at a district edge to continue next door; start at a waterway's end to extend it.";
            Wire.Arm();if(Wire.Armed)toolbar.Popup.gameObject.SetActive(false);
        }
        void OnDisable(){if(Wire)Wire.Cancel();}
        sealed class Surface : PointWireSurface
        {
            readonly PrototypeRegionView view;public Surface(PrototypeRegionView source){view=source;}
            public override bool Available=>view&&view.SelectedDistrict>=0&&!view.DistrictLoading&&view.ApprovedDistrict;
            public override bool CanEdit=>Available&&!view.IsSaving;
            public override Collider Collider=>view.ApprovedDistrict.DisplayTerrain.GetComponent<TerrainCollider>();
            public override bool Contains(Vector2 p)=>view.ApprovedDistrict.Contains(p);
            public override float Height(Vector2 p)=>view.ApprovedDistrict.Height(p);
        }
    }
}

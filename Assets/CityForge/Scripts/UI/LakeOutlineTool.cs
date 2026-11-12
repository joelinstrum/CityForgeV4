using System.Collections.Generic;
using UnityEngine;
using CityForge.World;
namespace CityForge.UI
{
    /// <summary>Lake-specific adapter for the reusable PointWire interaction.</summary>
    public sealed class LakeOutlineTool : MonoBehaviour
    {
        public PointWireTool Wire {get;private set;}
        public bool Armed=>Wire&&Wire.Armed;
        public bool Closed=>Wire&&Wire.Closed;
        public IReadOnlyList<Vector2> Points=>Wire.Points;
        PrototypeRegionView view;NatureToolbar toolbar;
        public void Initialize(PrototypeRegionView source,NatureToolbar tools,Transform hud)
        {
            view=source;toolbar=tools;Wire=gameObject.AddComponent<PointWireTool>();
            Wire.Initialize(view.GetComponentInChildren<Camera>(),hud,new DistrictSurface(view),new Color(.18f,.7f,1));
            Wire.Label="Lake / pond";Wire.RepeatOnCompletion=true;Wire.AllowClosedUndo=false;
            Wire.CompletionRequested=points=>view.ApprovedDistrict.CreatePlayerLake(points);
            toolbar.ToolSelected+=_=>Cancel();toolbar.Popup.OptionChanged+=_=>Cancel();
            toolbar.Popup.WaterBodySelected+=(adding,kind)=>{if(adding&&kind==0)Arm();else Cancel();};
        }
        public void Arm(){toolbar.ForestBrush.Cancel();Wire.Arm();if(Armed)toolbar.Popup.gameObject.SetActive(false);}
        public void Cancel(){if(Wire)Wire.Cancel();}
        public bool PlaceMarker(Vector2 p,bool closeToStart=false)=>Wire.PlaceMarker(p,closeToStart);
        public void UndoMarker()=>Wire.UndoMarker();
        void OnDisable(){Cancel();}
        sealed class DistrictSurface : PointWireSurface
        {
            readonly PrototypeRegionView view;
            public DistrictSurface(PrototypeRegionView source){view=source;}
            public override bool Available=>view&&view.SelectedDistrict>=0&&!view.DistrictLoading&&view.ApprovedDistrict;
            public override bool CanEdit=>Available&&!view.IsSaving;
            public override Collider Collider=>view.ApprovedDistrict.DisplayTerrain.GetComponent<TerrainCollider>();
            public override bool Contains(Vector2 p)=>view.ApprovedDistrict.Contains(p);
            public override float Height(Vector2 p)=>view.ApprovedDistrict.Height(p);
        }
    }
}

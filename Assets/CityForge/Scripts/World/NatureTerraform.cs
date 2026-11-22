using System;
using System.Linq;
using UnityEngine;
using CityForge.UI;
namespace CityForge.World
{
    public enum TerraformAction { Raise,Lower,Level,Smooth }
    public enum TerraformLevel { Lowest,Average,Clicked,Sea }
    [Serializable]public sealed class TerraformBrushSettings
    {
        public TerraformAction Action=TerraformAction.Level;
        public TerraformLevel Level=TerraformLevel.Lowest;
        public float Size=32,Step=5,Offset=0,Blend=.35f;
    }
    public sealed partial class PrototypeRegionView
    {
        internal bool TerraformPainting=>levelerNature&&(levelerDragging||levelerTargets.Count>0);
        bool levelerNature;TerraformBrushSettings terraformSettings;int terraformStroke;
        TerrainCollider terraformCollider;bool terraformColliderEnabled;
        bool CanUseLeveler=>levelerNature?SelectedDistrict>=0&&!DistrictLoading&&!IsSaving&&!TownConstructionVisible&&!RemakeBlocksInput&&!playerLotWorkspace&&!transportModal&&GetComponent<NatureToolbar>() is var tools&&tools&&tools.Selected==NatureTool.Terrain&&!tools.Popup.Visible:CanDetonate;
        internal void ArmTerraform(TerraformBrushSettings settings)
        {
            if(SelectedDistrict<0||DistrictLoading||IsSaving||!ApprovedDistrict)return;
            SetLeveler(false);CloseTownSection();CloseFounderPicker();offeredFounderPicker=true;
            terraformSettings=settings;terraformSettings.Size=Mathf.Clamp(Mathf.Round(settings.Size/4)*4,8,128);
            terraformStroke=ZoneGroundPlanes.Count==0?0:ZoneGroundPlanes.Max(p=>p.stroke);
            levelerNature=true;terraformCollider=ApprovedDistrict.DisplayTerrain.GetComponent<TerrainCollider>();terraformColliderEnabled=terraformCollider.enabled;terraformCollider.enabled=true;
            SetLeveler(true);
        }
        internal void UpdateTerraformTerrain()
        {
            if(SelectedDistrict<0||DistrictLoading||IsSaving||!ApprovedDistrict)return;
            CancelTerraform();
            // Include earlier saved edits so Update Terrain also repairs existing shores.
            foreach(var plane in ZoneGroundPlanes.Where(p=>p.nature&&ActiveDistrictMembers.Contains(p.district))){
                var area=plane.Bounds;float margin=GroundPlaneBlend(plane);area.min-=Vector2.one*margin;area.max+=Vector2.one*margin;
                ApprovedDistrict.QueueTerraformCoast(area,true,plane);
            }
            ApprovedDistrict.FlushTerraformCoast();
            ShowDistrictMessage("Terrain updated · Water and shoreline refreshed");
        }
        internal void CancelTerraform(){if(levelerNature)SetLeveler(false);}
        void ConfigureTerraformPlane(ZoneGroundPlane plane)
        {
            if(!levelerNature)return;plane.nature=true;plane.stroke=terraformStroke;plane.strength=terraformSettings.Action==TerraformAction.Smooth?Mathf.Clamp(terraformSettings.Blend,.1f,1):1;
        }
        float TerraformTarget(ZoneGroundPlane plane,DistrictLotGrading grading)
        {
            var stats=grading.Statistics(plane.Bounds,p=>plane.Contains(p)&&InActiveDistrict(p),CurrentLotTerrainHeight(plane.center));
            float target=terraformSettings.Action switch{
                TerraformAction.Raise=>stats.Mean+terraformSettings.Step,
                TerraformAction.Lower=>stats.Mean-terraformSettings.Step,
                TerraformAction.Smooth=>stats.Mean,
                _=>terraformSettings.Level switch{TerraformLevel.Average=>stats.Mean,TerraformLevel.Clicked=>CurrentLotTerrainHeight(plane.center),TerraformLevel.Sea=>SeaLevel,_=>stats.Min}+terraformSettings.Offset};
            return Mathf.Max(0,target);
        }
        internal bool GradeIncludesPoint(ZoneGroundPlane plane,Vector2 p)=>DistrictMembers(plane.district).Contains(DistrictAtWorld(new Vector3(p.x,0,p.y)));
        internal bool TerraformClears(Vector2 p)=>ZoneGroundPlanes.Any(edit=>edit.leveler&&edit.Contains(p,GroundPlaneBlend(edit))&&GradeIncludesPoint(edit,p));
        string LevelerCaption(int tiles)=>levelerNature?$"{terraformSettings.Action} land · {tiles*4} m brush · Drag to sculpt · Shift: double size · Tab: rotate · Esc: finish":$"Level ground · {tiles} × {tiles} tiles · Click and drag · Shift: double size · Tab: rotate · Esc: exit";
    }
}

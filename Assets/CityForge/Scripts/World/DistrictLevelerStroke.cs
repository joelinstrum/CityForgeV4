using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        bool levelerDragging;
        float? levelerStrokeHeight;
        Vector2? levelerStrokeEnd;
        readonly List<Vector2> levelerTargets=new();
        void CancelLevelerStroke(){FinishLevelerDestruction();levelerDragging=false;levelerStrokeHeight=null;levelerStrokeEnd=null;levelerTargets.Clear();}
        void BeginLevelerStroke(){CancelLevelerStroke();if(levelerNature)terraformStroke++;levelerDragging=true;}
        void ChangeLevelerStrokeShape(bool held){levelerTargets.Clear();levelerStrokeEnd=null;levelerDragging=held;}
        void QueueLevelerStroke(Vector2 p,int tiles,bool final)
        {
            Vector2? last=levelerTargets.Count>0?levelerTargets[levelerTargets.Count-1]:levelerStrokeEnd;
            if(last.HasValue&&Vector2.Distance(last.Value,p)<(final?.01f:tiles*DistrictGridCellMeters*.25f))return;
            if(levelerTargets.Count>=2){var a=levelerTargets[levelerTargets.Count-2];var b=levelerTargets[levelerTargets.Count-1];if(Vector2.Dot((b-a).normalized,(p-b).normalized)>.99999f)levelerTargets.RemoveAt(levelerTargets.Count-1);}
            levelerTargets.Add(p);
        }
        void ProcessLevelerStroke(int tiles)
        {
            if(levelerTargets.Count==0){if(!levelerDragging){levelerStrokeHeight=null;FinishLevelerDestruction();}return;}
            var target=levelerTargets[0];var start=levelerStrokeEnd??target;
            var end=Vector2.MoveTowards(start,target,32f);
            var plane=new ZoneGroundPlane{center=(start+end)*.5f,sweep=end-start,size=Vector2.one*(tiles*DistrictGridCellMeters),cornerRadius=DistrictGridCellMeters,rotation=levelerRotationSteps*45f,leveler=true,roughEdges=true,district=SelectedDistrict};
            bool smoothing=levelerNature&&terraformSettings.Action==TerraformAction.Smooth;if(smoothing){terraformStroke++;levelerStrokeHeight=null;}
            ConfigureTerraformPlane(plane);var problem=CommitLeveler(plane);
            if(problem!=null){CancelLevelerStroke();ShowDistrictMessage(problem);return;}
            levelerStrokeHeight=smoothing?null:plane.height;levelerStrokeEnd=end;
            if(Vector2.Distance(end,target)<.01f)levelerTargets.RemoveAt(0);
        }
        static bool LevelerPolygonsOverlap(IList<Vector2> a,IList<Vector2> b)
        {
            bool Separated(IList<Vector2> source){for(int i=0;i<source.Count;i++){var d=source[(i+1)%source.Count]-source[i];var axis=new Vector2(-d.y,d.x);float amin=float.PositiveInfinity,amax=float.NegativeInfinity,bmin=amin,bmax=amax;foreach(var p in a){float v=Vector2.Dot(p,axis);amin=Mathf.Min(amin,v);amax=Mathf.Max(amax,v);}foreach(var p in b){float v=Vector2.Dot(p,axis);bmin=Mathf.Min(bmin,v);bmax=Mathf.Max(bmax,v);}if(amax<=bmin+.001f||bmax<=amin+.001f)return true;}return false;}
            return !Separated(a)&&!Separated(b);
        }
        void MergeLevelerSweep(ZoneGroundPlane plane,ZoneGroundPlane prior,DistrictLotGrading grading)
        {
            if(prior==null||prior==plane||ZoneGroundPlanes.LastOrDefault()!=plane||!prior.leveler||!prior.roughEdges||prior.nature!=plane.nature||prior.stroke!=plane.stroke||prior.strength!=plane.strength||prior.size!=plane.size||prior.rotation!=plane.rotation||prior.cornerRadius!=plane.cornerRadius||Mathf.Abs(prior.height-plane.height)>.001f)return;
            var a=prior.center-prior.sweep*.5f;var joint=prior.center+prior.sweep*.5f;var b=plane.center-plane.sweep*.5f;
            if(Vector2.Distance(joint,b)>.001f||plane.sweep.sqrMagnitude<.0001f)return;
            if(prior.sweep.sqrMagnitude>.0001f&&Vector2.Dot(prior.sweep.normalized,plane.sweep.normalized)<.999999f)return;
            var end=plane.center+plane.sweep*.5f;prior.center=(a+end)*.5f;prior.sweep=end-a;ZoneGroundPlanes.RemoveAt(ZoneGroundPlanes.Count-1);grading.AppliedPlanes.Remove(plane);
        }
    }
}

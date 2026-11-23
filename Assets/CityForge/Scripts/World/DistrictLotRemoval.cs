using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private void RemoveDistrictLotViews(IEnumerable<DistrictLotRecord> records)
        {
            var removed=records.ToHashSet();if(removed.Count==0)return;
            var buildings=lotBuildingViews.Where(p=>removed.Contains(p.Key.Lot)).Select(p=>p.Value).ToHashSet();
            var matrices=roadBuildings.Where(b=>buildings.Contains(b.Instance)).Select(b=>b.ObstacleMatrix).ToHashSet();
            var roots=removed.Where(districtLotRoots.ContainsKey).Select(l=>districtLotRoots[l]).Where(t=>t).ToArray();
            bool Owned(Transform t)=>t&&roots.Any(t.IsChildOf);
            eagleLotTrees.RemoveAll(t=>!t||Owned(t));
            lotTreePositions.Clear();lotTreePositions.AddRange(eagleLotTrees.Where(t=>t).Select(t=>new Vector2(t.position.x,t.position.z)));
            roadBuildings.RemoveAll(b=>buildings.Contains(b.Instance));eagleBuildingBounds.RemoveAll(b=>matrices.Contains(b.worldToLocal));
            foreach(var key in lotBuildingViews.Keys.Where(k=>removed.Contains(k.Lot)).ToArray())lotBuildingViews.Remove(key);
            foreach(var key in dockWorkerHomes.Keys.Where(k=>removed.Contains(k.lot)).ToArray())dockWorkerHomes.Remove(key);
            foreach(var list in platformCells.Values)list.RemoveAll(p=>removed.Contains(p.Lot));
            delayedLotCharacters.RemoveAll(p=>removed.Contains(p.lot));
            foreach(var lot in removed)
            {
                if(districtLotRoots.Remove(lot,out var root)&&root){root.gameObject.SetActive(false);Destroy(root.gameObject);}
                lotPopulationAfter.Remove(lot);populatedTrafficLots.Remove(lot);
            }
            for(int i=pedestrians.Count-1;i>=0;i--)if(removed.Contains(pedestrians[i].OriginLot))
            {var actor=pedestrians[i];if(actor.Body){actor.Body.gameObject.SetActive(false);Destroy(actor.Body.gameObject);}pedestrians.RemoveAt(i);}
            if(lotInstances)lotInstances.GetComponent<ResidentialGroundBatches>()?.Remove(removed.Select(l=>l.id).ToHashSet());
            if(selectedDistrictLot!=null&&removed.Contains(selectedDistrictLot))SelectDistrictLot(null);
            LotPresentationBuilds++;pedestrianRoutesDirty=true;MarkImmigrationSitesChanged();
            RestoreRemovedLotGround(removed);
            InvalidateLotGridHeights(removed);
            RefreshZoneOverlay();QueueDistrictSidewalkRefresh();
            if(ApprovedDistrict&&removed.Any(l=>l.definition.IsWaterfront||l.definition.AttractsVessels))ApprovedDistrict.RefreshHarborStones();
        }

        private void InvalidateLotGridHeights(IEnumerable<DistrictLotRecord> records)
        {
            Rect? area=null;
            foreach(var lot in records)
            {
                var b=new DistrictLotFootprint(lot).Bounds;
                area=area.HasValue?Rect.MinMaxRect(Mathf.Min(area.Value.xMin,b.xMin),Mathf.Min(area.Value.yMin,b.yMin),
                    Mathf.Max(area.Value.xMax,b.xMax),Mathf.Max(area.Value.yMax,b.yMax)):b;
            }
            if(area.HasValue)InvalidateDistrictGridHeights(area);
        }

        private void RestoreRemovedLotGround(HashSet<DistrictLotRecord> removed)
        {
            var terrain=ApprovedDistrict?ApprovedDistrict.DisplayTerrain:null;
            if(!terrain||!terrain.TryGetComponent<DistrictLotGrading>(out var grading))return;
            var survivors=PlacedLots.Where(l=>!removed.Contains(l)&&InActiveDistrict(l.center)).Select(l=>new DistrictLotFootprint(l)).ToArray();
            Rect? affected=null;
            foreach(var lot in removed.Where(l=>l.cutToLowest&&!l.definition.IsWaterfront))
            {
                var footprint=new DistrictLotFootprint(lot);
                // Most zoned homes use a shared block grade. Deleting the building
                // does not undo that authored land, so no terrain work is needed.
                if(ZoneGroundPlanes.Any(p=>Enumerable.Range(0,4).All(i=>p.Contains(footprint.WorldPoint(new Vector2(
                    (i&1)==0?-lot.definition.Size.x*.5f:lot.definition.Size.x*.5f,
                    (i&2)==0?-lot.definition.Size.y*.5f:lot.definition.Size.y*.5f))))))continue;
                grading.Cut(footprint.Bounds,p=>footprint.Contains(p),0,
                    p=>TryZoneGroundHeight(p,out _)||survivors.Any(f=>f.Contains(p))||LotGradeRoadPoint(p),
                    exact:true,targetHeight:grading.NaturalHeight);
                var b=footprint.Bounds;
                affected=affected.HasValue?Rect.MinMaxRect(Mathf.Min(affected.Value.xMin,b.xMin),Mathf.Min(affected.Value.yMin,b.yMin),
                    Mathf.Max(affected.Value.xMax,b.xMax),Mathf.Max(affected.Value.yMax,b.yMax)):b;
            }
            if(affected.HasValue&&grading.Finish())
            {
                InvalidateDistrictGridHeights(affected);
                GetComponentInChildren<DistrictWeather>()?.RefreshGround(affected);
            }
        }
    }
}

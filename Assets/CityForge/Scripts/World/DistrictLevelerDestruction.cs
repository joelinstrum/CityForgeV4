using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        bool levelerRoadsChanged,levelerPathsChanged,levelerZonesChanged,levelerPaintChanged,levelerActorsChanged;
        void DestroyLevelerContents(ZoneGroundPlane plane,Rect area,float feather)
        {
            bool Inside(Vector2 p)=>area.Contains(p)&&plane.Contains(p,feather)&&InActiveDistrict(p);
            void Erase(GameObject go){if(go){go.SetActive(false);Destroy(go);}}
            // Destructive earthwork removes intersecting records whole, never rebuilds them
            // to fit, splits roads, preserves foundations or projects imported geometry.
            var removedLots=new HashSet<DistrictLotRecord>(PlacedLots.Where(l=>ActiveDistrictMembers.Contains(l.district)&&new DistrictLotFootprint(l).Bounds.Overlaps(area)));
            if(removedLots.Count>0){
                var removedBounds=removedLots.Select(l=>new DistrictLotFootprint(l).Bounds).ToArray();var buildingMatrices=new HashSet<Matrix4x4>();
                foreach(var lot in removedLots){if(districtLotRoots.Remove(lot,out var root)&&root){foreach(Transform child in root)buildingMatrices.Add(Matrix4x4.Rotate(Quaternion.Inverse(child.rotation))*Matrix4x4.Translate(-child.position));Erase(root.gameObject);}lotPopulationAfter.Remove(lot);populatedTrafficLots.Remove(lot);}
                PlacedLots.RemoveAll(removedLots.Contains);
                foreach(var list in platformCells.Values)list.RemoveAll(p=>removedLots.Contains(p.Lot));
                foreach(var key in dockWorkerHomes.Keys.Where(k=>removedLots.Contains(k.lot)).ToArray())dockWorkerHomes.Remove(key);
                delayedLotCharacters.RemoveAll(p=>removedLots.Contains(p.lot));
                roadBuildings.RemoveAll(b=>removedBounds.Any(b.Overlaps));eagleBuildingBounds.RemoveAll(b=>buildingMatrices.Contains(b.worldToLocal));
                lotTreePositions.RemoveAll(p=>removedBounds.Any(b=>b.Contains(p)));eagleLotTrees.RemoveAll(t=>!t||!t.gameObject.activeInHierarchy);
                if(lotInstances)lotInstances.GetComponent<ResidentialGroundBatches>()?.Remove(new HashSet<string>(removedLots.Select(l=>l.id)));
                if(selectedDistrictLot!=null&&removedLots.Contains(selectedDistrictLot))SelectDistrictLot(null);
                levelerActorsChanged=true;
            }
            bool HitsLine(IReadOnlyList<Vector2> line,bool closed,float margin){for(int i=0;i<(closed?line.Count:line.Count-1);i++){var a=line[i];var b=line[(i+1)%line.Count];var bounds=Rect.MinMaxRect(Mathf.Min(a.x,b.x)-margin,Mathf.Min(a.y,b.y)-margin,Mathf.Max(a.x,b.x)+margin,Mathf.Max(a.y,b.y)+margin);if(!area.Overlaps(bounds))continue;int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/4));for(int j=0;j<=n;j++)if(plane.Contains(Vector2.Lerp(a,b,j/(float)n),feather+margin))return true;}return false;}
            var roads=districtRoads.Where(r=>ActiveDistrictMembers.Contains(r.district)&&HitsLine(RoadCenterline(r),r.closed,r.width*.5f+2f)).ToHashSet();
            if(roads.Count>0){foreach(var road in roads)if(districtRoadMeshes.Remove(road,out var filter)&&filter)Erase(filter.gameObject);districtRoads.RemoveAll(roads.Contains);undoRoadEdits.RemoveAll(e=>roads.Contains(e.Road));redoRoadEdits.RemoveAll(e=>roads.Contains(e.Road));if(districtSidewalkRoot)districtSidewalkRoot.gameObject.SetActive(false);levelerRoadsChanged=true;levelerActorsChanged=true;}
            if(parkPaths.RemoveAll(r=>ActiveDistrictMembers.Contains(r.district)&&HitsLine(r.path.points,r.path.closed,r.path.width*.5f))>0){if(parkPathRoot)parkPathRoot.SetActive(false);levelerPathsChanged=true;levelerActorsChanged=true;}
            foreach(var cell in townZones.Keys.Where(c=>area.Contains(ZoneCorner(c)+Vector2.one*(DistrictGridCellMeters*.5f))&&InActiveDistrict(ZoneCorner(c))).ToArray()){townZones.Remove(cell);zoneWealth.Remove(cell);residentialFacing.Remove(cell);zoneLotSizes.Remove(cell);zoneLotAnchors.Remove(cell);localGrowthCells.Remove(cell);levelerZonesChanged=true;}
            PlacedActors.RemoveAll(actor=>{if(!Inside(actor.position))return false;if(placedTreeViews.Remove(actor,out var tree))Erase(tree);if(streetscapeViews.Remove(actor,out var prop))Erase(prop);if(actor.pedestrian||!actor.tree&&actor.streetscape==null)levelerActorsChanged=true;return true;});
            for(int i=pedestrians.Count-1;i>=0;i--){var actor=pedestrians[i];if(!actor.Body||removedLots.Contains(actor.OriginLot)||Inside(new Vector2(actor.Body.position.x,actor.Body.position.z))){if(actor.Body)Erase(actor.Body.gameObject);pedestrians.RemoveAt(i);}}
            if(placedChildrenRoot)foreach(Transform child in placedChildrenRoot)if(Inside(new Vector2(child.position.x,child.position.z)))Erase(child.gameObject);
            ForestAdditions=ForestAdditions.Where(t=>!Inside(new Vector2(t.x,t.z))).ToArray();additionIndex=null;WoodlandRevision++;
            foreach(var tile in districtGroundPaint.tiles){if(!area.Overlaps(new Rect(tile.Minimum,Vector2.one*GroundPaintData.TileMeters)))continue;var bytes=tile.Read();bool changed=false;
                for(int z=0;z<GroundPaintData.Resolution;z++)for(int x=0;x<GroundPaintData.Resolution;x++){var point=tile.Minimum+new Vector2(x+.5f,z+.5f)*GroundPaintData.PixelMeters;if(!Inside(point))continue;int offset=(z*GroundPaintData.Resolution+x)*GroundPaintData.Layers;for(int i=0;i<GroundPaintData.Layers;i++)if(bytes[offset+i]!=0){bytes[offset+i]=0;changed=true;}}
                if(changed){tile.editing=bytes;tile.Flush();levelerPaintChanged=true;}
            }
            if(levelerPaintChanged&&districtPaintRendering)districtPaintRendering.gameObject.SetActive(false);
            pedestrianRoutesDirty|=levelerActorsChanged;
        }
        void FinishLevelerDestruction()
        {
            // Only touched systems refresh, once on release/cancel, never per drag stamp.
            bool roads=levelerRoadsChanged,paths=levelerPathsChanged,zones=levelerZonesChanged,paint=levelerPaintChanged,actors=levelerActorsChanged;
            levelerRoadsChanged=levelerPathsChanged=levelerZonesChanged=levelerPaintChanged=levelerActorsChanged=false;
            if(roads){RebuildDistrictRoads();BuildDetonateRoadRuns();}
            else if(paths)RebuildParkPaths();
            if(zones)RefreshZoneOverlay();
            if(paint){districtGroundPaint.PruneEmpty();RebuildGroundPaint();}
            if(actors)RebuildPlacedPedestrians();
            if(ApprovedDistrict&&!ApprovedDistrict.TerraformCoastDeferred)ApprovedDistrict.FlushTerraformCoast();
        }
    }
}

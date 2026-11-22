using System;
using CityForge.Lots;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        // Spatial lookup keeps streamed ground-cover tests independent of town size.
        readonly Dictionary<Vector2Int,List<DistrictLotFootprint>> developedSites = new();
        int developedCount = -1, developedFrame = -1;
        internal bool LotClearsForest(Vector2 point)
        {
            if (developedCount != PlacedLots.Count || developedFrame != Time.frameCount)
            {
                developedFrame = Time.frameCount;
                int hash = 17;
                foreach (var lot in PlacedLots) hash = unchecked(hash * 31 + System.HashCode.Combine(lot.center, lot.definition.Size, lot.FacingDegrees));
                if (developedCount != PlacedLots.Count || hash != developedHash)
                {
                    developedCount = PlacedLots.Count; developedHash = hash; developedSites.Clear();
                    foreach (var lot in PlacedLots)
                    {
                        var footprint = new DistrictLotFootprint(lot); var bounds = footprint.Bounds;
                        var lo = Vector2Int.FloorToInt(bounds.min / 32); var hi = Vector2Int.FloorToInt(bounds.max / 32);
                        for(int z=lo.y;z<=hi.y;z++)for(int x=lo.x;x<=hi.x;x++)
                        {var key=new Vector2Int(x,z);if(!developedSites.TryGetValue(key,out var list))developedSites[key]=list=new();list.Add(footprint);}
                    }
                }
            }
            if(developedSites.TryGetValue(Vector2Int.FloorToInt(point/32),out var sites))foreach(var site in sites)if(site.Contains(point))return true;
            return false;
        }
        int developedHash;
        private void ClearDevelopedSites(IReadOnlyList<DistrictLotRecord> records)
        {
            if(records.Count==0)return;
            developedCount=-1;
            var footprints=records.Select(r=>new DistrictLotFootprint(r)).ToArray();
            bool Contains(Vector2 p){foreach(var site in footprints)if(site.Contains(p))return true;return false;}
            if(ApprovedDistrict){ApprovedDistrict.ClearTreesUnderLot(Contains,true);foreach(var wildlife in ApprovedDistrict.GetComponentsInChildren<DistrictWildlife>())wildlife.ClearUnderDevelopment(Contains);}
            bool pedestrians=false;
            PlacedActors.RemoveAll(actor=>
            {
                if(actor.zoningStreetlamp||!Contains(actor.position))return false;
                if(placedTreeViews.Remove(actor,out var tree)&&tree){tree.SetActive(false);Destroy(tree);}
                if(streetscapeViews.Remove(actor,out var prop)&&prop){prop.SetActive(false);Destroy(prop);}
                pedestrians|=actor.pedestrian;
                return true;
            });
            ForestAdditions=ForestAdditions.Where(t=>!Contains(new Vector2(t.x,t.z))).ToArray();additionIndex=null;WoodlandRevision++;
            // Clear district paint and path segments within the developed parcel; authored lot details live separately.
            foreach(var tile in districtGroundPaint.tiles)
            {
                if(!footprints.Any(f=>f.Bounds.Overlaps(new Rect(tile.Minimum,Vector2.one*GroundPaintData.TileMeters))))continue;
                var bytes=tile.Read();bool dirty=false;
                for(int z=0;z<GroundPaintData.Resolution;z++)for(int x=0;x<GroundPaintData.Resolution;x++)
                {var p=tile.Minimum+new Vector2(x+.5f,z+.5f)*GroundPaintData.PixelMeters;if(!Contains(p))continue;Array.Clear(bytes,(z*GroundPaintData.Resolution+x)*GroundPaintData.Layers,GroundPaintData.Layers);dirty=true;}
                if(dirty){tile.editing=bytes;tile.Flush();}
            }
            districtGroundPaint.PruneEmpty();RebuildGroundPaint();
            bool pathsChanged=false;
            foreach(var lot in records)
            {
                var site=new DistrictLotFootprint(lot);var kept=new List<DistrictParkPathRecord>();
                var rot=Quaternion.Euler(0,-lot.FacingDegrees,0);
                Vector2 Local(Vector2 p){var v=rot*new Vector3(p.x-lot.center.x,0,p.y-lot.center.y);return new Vector2(v.x,v.z);}
                int remaining=parkPaths.Count;
                foreach(var record in parkPaths)
                {
                    remaining--;var path=record.path;var world=PathDisplayPoints(path);
                    if(!TerrainGeography.Pad(site.Bounds,path.width*.5f).Overlaps(new Rect(world.Aggregate(Vector2.Min),world.Aggregate(Vector2.Max)-world.Aggregate(Vector2.Min)))){kept.Add(record);continue;}
                    var line=world.Select(Local).ToArray();var proxy=new DistrictRoadRecord{district=record.district,points=line,closed=path.closed,width=path.width};
                    var half=lot.definition.Size*.5f+Vector2.one*(path.width*.5f);var pieces=AreaDemolition.Outside(proxy,line,new Rect(-half,half*2),MaxRoadPoints);
                    if(pieces.Count==1&&pieces[0]==proxy){kept.Add(record);continue;}
                    pathsChanged=true;
                    // Reserve one record per remaining path so clipping cannot invalidate a full saved region.
                    int available=512-kept.Count-remaining;
                    if(pieces.Count>available)pieces=pieces.OrderByDescending(piece=>piece.points.Zip(piece.points.Skip(1),(a,b)=>Vector2.Distance(a,b)).Sum()).Take(Mathf.Max(0,available)).ToList();
                    foreach(var piece in pieces){var copy=JsonUtility.FromJson<LotPathDefinition>(JsonUtility.ToJson(path));copy.points=piece.points.Select(site.WorldPoint).ToArray();copy.closed=piece.closed;copy.curved=false;kept.Add(new DistrictParkPathRecord{district=record.district,path=copy});}
                }
                parkPaths.Clear();parkPaths.AddRange(kept);
            }
            if(pathsChanged){pathUndo.Clear();CancelParkPath();RebuildParkPaths();pedestrianRoutesDirty=true;}
            if(pedestrians||pathsChanged)RebuildPlacedPedestrians();
        }
    }
    public sealed partial class DistrictWildlife
    {
        internal void ClearUnderDevelopment(Predicate<Vector2> contains)
        {
            animals.RemoveAll(a=>{if(!a.root)return true;var p=a.root.position;if(!contains(new Vector2(p.x,p.z))&&!contains(a.home))return false;a.root.gameObject.SetActive(false);Destroy(a.root.gameObject);return true;});
            renderers=GetComponentsInChildren<Renderer>(true);
        }
    }
}

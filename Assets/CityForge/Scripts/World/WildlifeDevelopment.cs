using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        readonly HashSet<Vector2Int> wildlifeObstacles=new();
        NavigationState wildlifeObstacleState;bool wildlifeObstaclesReady;
        internal bool WildlifeObstructed(Vector2 p)
        {
            var state=new NavigationState(this);
            if(!wildlifeObstaclesReady||!state.Equals(wildlifeObstacleState))
            {
                wildlifeObstaclesReady=true;wildlifeObstacleState=state;wildlifeObstacles.Clear();
                foreach(var lot in PlacedLots)
                {
                    var footprint=new DistrictLotFootprint(lot);var bounds=footprint.Bounds;
                    var min=DistrictGridCell(bounds.min-Vector2.one*2);var max=DistrictGridCell(bounds.max+Vector2.one*2);
                    for(int y=min.y;y<=max.y;y++)for(int x=min.x;x<=max.x;x++)
                    {
                        var cell=new Vector2Int(x,y);
                        if(footprint.OverlapsCircle(ZoneCorner(cell)+Vector2.one*(DistrictGridCellMeters*.5f),DistrictGridCellMeters*.8f))wildlifeObstacles.Add(cell);
                    }
                }
                foreach(var road in districtRoads)
                {
                    if(road.points==null)continue;
                    for(int i=0;i<road.points.Length-(road.closed?0:1);i++)
                    {
                        var a=road.points[i];var b=road.points[(i+1)%road.points.Length];
                        int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/2));int reach=Mathf.CeilToInt((road.width*.5f+2)/DistrictGridCellMeters);
                        for(int j=0;j<=steps;j++)
                        {
                            var cell=DistrictGridCell(Vector2.Lerp(a,b,j/(float)steps));
                            for(int y=-reach;y<=reach;y++)for(int x=-reach;x<=reach;x++)wildlifeObstacles.Add(cell+new Vector2Int(x,y));
                        }
                    }
                }
            }
            return wildlifeObstacles.Contains(DistrictGridCell(p));
        }
        internal bool WildlifeDeveloped(Vector2 p)
        {
            if(WildlifeObstructed(p))return true;
            var cell=DistrictGridCell(p);
            for(int y=-2;y<=2;y++)for(int x=-2;x<=2;x++)
            {
                var near=cell+new Vector2Int(x,y);
                if(townZones.ContainsKey(near)||wildlifeObstacles.Contains(near))return true;
            }
            return false;
        }
    }
}

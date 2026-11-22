using UnityEngine;
namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        // Ploppable footprints are not zoning cells. Let them reach the real pavement edge
        // between four-meter grid lines, without treating sidewalks as occupied pavement.
        private Vector2 SnapLotToRoadEdge(DistrictLotRecord lot)
        {
            var center=lot.center;Vector2 previousNormal=Vector2.zero;
            var rotation=Quaternion.Euler(0,lot.FacingDegrees,0);var axis=rotation*Vector3.right;
            var right=new Vector2(axis.x,axis.z);var forward=new Vector2(-right.y,right.x);var half=lot.definition.Size*.5f;
            for(int pass=0;pass<2;pass++)
            {
                float best=1.5f;Vector2 chosen=center,chosenNormal=Vector2.zero;
                foreach(var road in districtRoadMeshes.Keys)
                {
                    var line=RoadCenterline(road);int count=road.closed?line.Count:line.Count-1;
                    for(int i=0;i<count;i++)
                    {
                        var a=line[i];var delta=line[(i+1)%line.Count]-a;float length=delta.magnitude;if(length<2)continue;
                        var along=delta/length;var normal=Vector2.Perpendicular(along);
                        if(Mathf.Abs(Vector2.Dot(normal,previousNormal))>.9f)continue;
                        if(Mathf.Max(Mathf.Abs(Vector2.Dot(normal,right)),Mathf.Abs(Vector2.Dot(normal,forward)))<.985f)continue;
                        if(Vector2.Dot(center-a,normal)<0)normal=-normal;
                        float radius=Mathf.Abs(Vector2.Dot(normal,right))*half.x+Mathf.Abs(Vector2.Dot(normal,forward))*half.y;
                        float across=Mathf.Abs(Vector2.Dot(along,right))*half.x+Mathf.Abs(Vector2.Dot(along,forward))*half.y;
                        float station=Vector2.Dot(center-a,along);if(station<across-.1f||station>length-across+.1f)continue;
                        float shift=road.width*.5f+radius+.005f-Vector2.Dot(center-a,normal);
                        if(Mathf.Abs(shift)>=best||Mathf.Abs(shift)<.001f)continue;
                        var candidate=new DistrictLotRecord{id=lot.id,district=lot.district,center=center+normal*shift,
                            definition=lot.definition,rotation=lot.rotation,diagonalRotation=lot.diagonalRotation};
                        if(LotOverlapsDistrictRoad(candidate))continue;
                        var footprint=new DistrictLotFootprint(candidate);bool occupied=false;
                        foreach(var other in PlacedLots)if(other!=movingDistrictLot&&other.id!=lot.id&&footprint.Overlaps(new DistrictLotFootprint(other))){occupied=true;break;}
                        if(occupied)continue;
                        best=Mathf.Abs(shift);chosen=candidate.center;chosenNormal=normal;
                    }
                }
                if(chosenNormal==Vector2.zero)break;center=chosen;previousNormal=chosenNormal;
            }
            return center;
        }
    }
}

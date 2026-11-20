#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using CityForge.Geometry;
using CityForge.Lots;
namespace CityForge.World
{
    public static class StreetJunctionChecks
    {
        static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
        static DistrictRoadRecord Road(string id, params Vector2[] points)=>new(){surfaceId=id,width=4,points=points};
        public static void Run()
        {
            string dirt="founders-dirt-street",brick="founders-brick-street";
            var through=Road(dirt,new(-20,0),new(20,0));
            var branch=Road(brick,new(0,-20),new(0,0));
            var nodes=StreetJunctions.Build(new[]{through,branch},r=>r.points);
            Need(nodes.Count==1&&nodes[0].Arms.Count==3,"T topology");
            var outline=StreetJunctions.Outline(nodes[0]);
            Need(outline.Count>20&&PointWireGeometry.IsSimple(outline)&&PointWireGeometry.Contains(outline,Vector2.zero),"Rounded T boundary invalid");
            Need(outline.Any(p=>p.x>2.05f&&p.y< -2.05f),"Right T corner has no graceful flare");
            Need(outline.Any(p=>p.x< -2.05f&&p.y< -2.05f),"Left T corner has no graceful flare");
            branch.points[^1]=new Vector2(0,20);
            nodes=StreetJunctions.Build(new[]{through,branch},r=>r.points);
            Need(nodes.Count==1&&nodes[0].Arms.Count==4,"Crossing topology");
            Need(PointWireGeometry.IsSimple(StreetJunctions.Outline(nodes[0])),"Crossing boundary invalid");
            foreach(float angle in new[]{30f,45f,60f,90f,120f,135f})
            {
                var direction=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
                branch.points=new[]{direction*20,Vector2.zero};
                nodes=StreetJunctions.Build(new[]{through,branch},r=>r.points);
                Need(nodes.Count==1&&nodes[0].Arms.Count==3,"Angled T topology "+angle);
                Need(PointWireGeometry.IsSimple(StreetJunctions.Outline(nodes[0])),"Angled boundary invalid "+angle);
            }
            var end=Road(brick,new(20,0),new(40,0));
            nodes=StreetJunctions.Build(new[]{through,end},r=>r.points);
            Need(nodes.Count==1&&nodes[0].Arms.Count==2,"Collinear texture join missing");
            var elbow=Road(dirt,new(-20,0),Vector2.zero,new(0,20));
            nodes=StreetJunctions.Build(new[]{elbow},r=>r.points);
            Need(nodes.Count==1&&nodes[0].Arms.Count==2&&PointWireGeometry.IsSimple(StreetJunctions.Outline(nodes[0])),"Extended street elbow missing");
            Need(StreetJunctions.Build(new[]{through,Road("dirt",new(0,-20),new(0,20))},r=>r.points).Single().Arms.Count==4,"Road-to-street blend missing");
            Need(StreetJunctions.Build(new[]{Road("dirt",new(-20,0),new(20,0)),Road("brick",new(0,-20),new(0,20))},r=>r.points).Count==0,"Unrelated road-to-road policy changed");
            var receiving = Road(dirt, new(-20,0), Vector2.zero, new(0,-20));
            var overlapping = Road(dirt, new(0,-2), new(0,20));
            var overlapNode = StreetJunctions.Build(new[]{receiving,overlapping},r=>r.points).Single();
            Need(overlapNode.Arms.Single(a=>a.Direction.y<-.99f).Length>=19.9f,"Short overlap stub truncated the receiving arm");
            Need(PointWireGeometry.IsSimple(StreetJunctions.Outline(overlapNode)),"Overlapping T outline invalid");
            receiving.curved = true;
            nodes = StreetJunctions.Build(new[]{receiving, overlapping}, road =>
            {
                if (!road.curved) return road.points;
                var samples = new System.Collections.Generic.List<Vector2>();
                LotPathSurface.RoundCorners(road.points, null, road.closed, road.width, samples);
                return samples;
            });
            Need(nodes.Any(n=>n.Arms.Count==3 && n.Point.sqrMagnitude<.001f),"Curved receiving corner lost authored T connection");
            var restored=JsonUtility.FromJson<DistrictRoadRecord>(JsonUtility.ToJson(end));
            Need(StreetJunctions.Build(new[]{through,restored},r=>r.points).Count==1,"Save roundtrip lost junction");
            Need(StreetJunctions.Build(new[]{through},r=>r.points).Count==0,"Demolition left phantom junction");
            Need(FreightRoadGraph.TryRoute(new[]{(through.points,false),(branch.points,false)},new Vector2(-10,0),branch.points[0],.1f,.1f,out var route)&&route.Any(p=>p.sqrMagnitude<.001f),"Routing does not recognize join");
            Debug.Log("STREET JUNCTION CHECKS PASS: T/cross/diagonal/collinear/elbow topology, bilateral rounded outlines, short-overlap union, curved authored connections, road-to-street blends, saved reconstruction, demolition cleanup and connected routing.");
        }
    }
}
#endif

using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        // Match the resolved placed-lot objects, including customization, rotation and scale.
        private readonly List<RoadBuildingFootprint> roadBuildings = new();

        private readonly struct RoadBuildingFootprint
        {
            internal readonly Transform Instance;
            internal readonly Matrix4x4 ObstacleMatrix;
            private readonly Vector2 center, half, right, forward;
            private readonly Rect bounds;

            public RoadBuildingFootprint(Transform instance, Vector2 size)
            {
                Instance=instance;ObstacleMatrix=Matrix4x4.Rotate(Quaternion.Inverse(instance.rotation))*Matrix4x4.Translate(-instance.position);
                center = new Vector2(instance.position.x, instance.position.z);
                half = size * .5f;
                right = new Vector2(instance.right.x, instance.right.z);
                forward = new Vector2(instance.forward.x, instance.forward.z);
                var extent = new Vector2(Mathf.Abs(right.x), Mathf.Abs(right.y)) * half.x +
                    new Vector2(Mathf.Abs(forward.x), Mathf.Abs(forward.y)) * half.y;
                bounds = new Rect(center - extent, extent * 2f);
            }

            public bool Contains(Vector2 point, float padding)
            {
                var local = Local(point);
                return Mathf.Abs(local.x) <= half.x + padding && Mathf.Abs(local.y) <= half.y + padding;
            }

            public bool Overlaps(Rect rect) => bounds.Overlaps(rect);

            public bool OverlapsTriangle(Vector2 a, Vector2 b, Vector2 c)
            {
                a = Local(a);
                b = Local(b);
                c = Local(c);
                var extent = half;
                return OverlapsAxis(Vector2.right) && OverlapsAxis(Vector2.up) &&
                    OverlapsAxis(Vector2.Perpendicular(b - a)) &&
                    OverlapsAxis(Vector2.Perpendicular(c - b)) &&
                    OverlapsAxis(Vector2.Perpendicular(a - c));

                bool OverlapsAxis(Vector2 axis)
                {
                    if (axis.sqrMagnitude < .0000001f) return true;
                    var projected = new Vector3(Vector2.Dot(a, axis), Vector2.Dot(b, axis), Vector2.Dot(c, axis));
                    float radius = Mathf.Abs(axis.x) * extent.x + Mathf.Abs(axis.y) * extent.y;
                    // Millimeter tolerance, as in lot/road clearance: miter arithmetic can
                    // otherwise report a collision when a trimmed edge only touches a wall.
                    float tolerance = .001f * axis.magnitude;
                    return Mathf.Min(projected.x, Mathf.Min(projected.y, projected.z)) < radius - tolerance &&
                        Mathf.Max(projected.x, Mathf.Max(projected.y, projected.z)) > -radius + tolerance;
                }
            }

            private Vector2 Local(Vector2 world)
            {
                var offset = world - center;
                return new Vector2(Vector2.Dot(offset, right), Vector2.Dot(offset, forward));
            }
        }

        private const float LotRoadEdgeTolerance = .025f;
        private readonly System.Runtime.CompilerServices.ConditionalWeakTable<Mesh,RoadGeometry> carriageways = new();
        private RoadGeometry CarriagewayGeometry(DistrictRoadRecord road,Mesh rendered)
        {
            return carriageways.GetValue(rendered,_=>
            {
                // The saved guide width is pavement only; sidewalks, shoulders and presentation aprons do not reserve land.
                var line=RoadCenterline(road);
                var mesh=LotPathSurface.BuildMesh(new LotPathDefinition{points=System.Linq.Enumerable.ToArray(line),closed=road.closed,width=road.width},Vector2.one,0);
                var result=new RoadGeometry(mesh);Destroy(mesh);return result;
            });
        }

        // Empty ground at a rounded street corner may be clipped, at most one grid cell
        // and one quarter of the lot's short side. Buildings remain strictly outside pavement.
        private static float GrowableRoadCornerCut(LotDefinition lot) =>
            Mathf.Min(DistrictGridCellMeters,Mathf.Min(lot.Size.x,lot.Size.y)*.25f);
        private bool LotOverlapsDistrictRoad(DistrictLotRecord lot, float inset = 0f, float cornerCut = 0f)
        {
            var footprint = new DistrictLotFootprint(lot);
            var half = lot.definition.Size * .5f - Vector2.one * LotRoadEdgeTolerance;
            var corners = new List<Vector2>();
            float cut = Mathf.Clamp(cornerCut,0,Mathf.Min(half.x,half.y)*.5f);
            if (cut > 0)
            {
                corners.Add(footprint.WorldPoint(new Vector2(-half.x+cut,-half.y)));
                corners.Add(footprint.WorldPoint(new Vector2(half.x-cut,-half.y)));
                corners.Add(footprint.WorldPoint(new Vector2(half.x,-half.y+cut)));
                corners.Add(footprint.WorldPoint(new Vector2(half.x,half.y-cut)));
                corners.Add(footprint.WorldPoint(new Vector2(half.x-cut,half.y)));
                corners.Add(footprint.WorldPoint(new Vector2(-half.x+cut,half.y)));
                corners.Add(footprint.WorldPoint(new Vector2(-half.x,half.y-cut)));
                corners.Add(footprint.WorldPoint(new Vector2(-half.x,-half.y+cut)));
            }
            else { corners.Add(footprint.WorldPoint(-half));corners.Add(footprint.WorldPoint(new Vector2(half.x,-half.y)));corners.Add(footprint.WorldPoint(half));corners.Add(footprint.WorldPoint(new Vector2(-half.x,half.y))); }
            return PolygonOverlapsDistrictRoad(corners,footprint.Bounds);
        }
        private bool GrowableBuildingsOverlapRoad(DistrictLotRecord lot)
        {
            var catalog=LotAssetCatalog.Load();var footprint=new DistrictLotFootprint(lot);
            foreach(var item in lot.definition.objects)
            {
                var asset=catalog.Find(item.assetId);if(asset?.category!="Buildings")continue;
                var bounds=LotRuntimeLoader.Footprint(item,asset);
                var corners=new[]{footprint.WorldPoint(bounds.min),footprint.WorldPoint(new Vector2(bounds.xMax,bounds.yMin)),footprint.WorldPoint(bounds.max),footprint.WorldPoint(new Vector2(bounds.xMin,bounds.yMax))};
                if(PolygonOverlapsDistrictRoad(corners,footprint.Bounds))return true;
            }
            return false;
        }
        private bool PolygonOverlapsDistrictRoad(IReadOnlyList<Vector2> corners,Rect bounds)
        {
            foreach(var pair in districtRoadMeshes)
            {
                var mesh=pair.Value.sharedMesh;var road=mesh.bounds;
                if(!bounds.Overlaps(Rect.MinMaxRect(road.min.x,road.min.z,road.max.x,road.max.z)))continue;
                var geometry=CarriagewayGeometry(pair.Key,mesh);
                for(int i=0;i<geometry.Triangles.Length;i+=3)
                {
                    var a=Planar(geometry.Vertices[geometry.Triangles[i]]);var b=Planar(geometry.Vertices[geometry.Triangles[i+1]]);var c=Planar(geometry.Vertices[geometry.Triangles[i+2]]);
                    bool intersects=Overlap(Vector2.Perpendicular(b-a))&&Overlap(Vector2.Perpendicular(c-b))&&Overlap(Vector2.Perpendicular(a-c));
                    for(int j=0;j<corners.Count&&intersects;j++)intersects=Overlap(Vector2.Perpendicular(corners[(j+1)%corners.Count]-corners[j]));
                    if(intersects)return true;
                    bool Overlap(Vector2 axis)
                    {
                        if(axis.sqrMagnitude<.000001f)return true;axis.Normalize();float low=float.PositiveInfinity,high=float.NegativeInfinity;
                        foreach(var corner in corners){float value=Vector2.Dot(corner,axis);low=Mathf.Min(low,value);high=Mathf.Max(high,value);}
                        float p=Vector2.Dot(a,axis),q=Vector2.Dot(b,axis),r=Vector2.Dot(c,axis);
                        return Mathf.Min(p,q,r)<high-.001f&&Mathf.Max(p,q,r)>low+.001f;
                    }
                }
            }
            return false;
            static Vector2 Planar(Vector3 point)=>new Vector2(point.x,point.z);
        }

        private bool RoadHitsBuilding(Mesh mesh, Vector3[] vertices)
        {
            var bounds = mesh.bounds;
            var footprint = Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            int[] triangles = null;
            foreach (var building in roadBuildings)
            {
                if (!building.Overlaps(footprint)) continue;
                triangles ??= mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                    if (building.OverlapsTriangle(Planar(vertices[triangles[i]]), Planar(vertices[triangles[i+1]]),
                        Planar(vertices[triangles[i+2]]))) return true;
            }
            return false;

            static Vector2 Planar(Vector3 point) => new Vector2(point.x, point.z);
        }
    }
}

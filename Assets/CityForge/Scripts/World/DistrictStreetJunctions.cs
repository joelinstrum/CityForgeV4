using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private Transform streetJunctionRoot;
        private readonly List<Object> streetJunctionAssets = new();
        private List<StreetJunctions.Node> streetJunctionNodes = new();

        private Vector2 SnapStreetPlacementPoint(Vector2 point)
        {
            var source = roadWire.Points.Count == 0 ? null : FindRoadExtensionEnd(roadWire.Points[0], .001f, out _);
            // Stable candidate order, with hysteresis at the exact chosen coordinate.
            if (streetSnapTarget != null && districtRoadMeshes.ContainsKey(streetSnapTarget) &&
                (point - streetSnapPoint).sqrMagnitude <= 3.1f * 3.1f) return streetSnapPoint;
            streetSnapTarget = null;
            float best = float.PositiveInfinity;
            foreach (var road in districtRoads)
            {
                if (road == source || !StreetJunctions.IsStreet(road.surfaceId) || !districtRoadMeshes.ContainsKey(road)) continue;
                var candidate = ClosestRoadPoint(road, point);
                if (road.curved)
                    for (int corner = road.closed ? 0 : 1; corner < road.points.Length - (road.closed ? 0 : 1); corner++)
                        if (Vector2.Distance(candidate, road.points[corner]) <= road.width * .5f + .15f)
                        { candidate = road.points[corner]; break; }
                if (!road.closed)
                {
                    if (Vector2.Distance(candidate, road.points[0]) < 1.25f) candidate = road.points[0];
                    else if (Vector2.Distance(candidate, road.points[^1]) < 1.25f) candidate = road.points[^1];
                }
                float distance = (candidate - point).sqrMagnitude;
                float reach = road.width * .5f + .8f;
                if (distance > reach * reach || distance >= best) continue;
                best = distance; streetSnapTarget = road; streetSnapPoint = candidate;
            }
            if (streetSnapTarget != null) return streetSnapPoint;
            if (roadWire.Points.Count>0)
            {
                var proposal=new List<Vector2>(roadWire.Points){point};
                if(PreviewStreetConnection(proposal).HasValue)return point;
            }
            if (!snapNewRoadsToTileCenters) return point;
            float origin = -MapSide * .5f;
            return new Vector2(origin + (Mathf.Floor((point.x - origin) / DistrictGridCellMeters) + .5f) * DistrictGridCellMeters,
                origin + (Mathf.Floor((point.y - origin) / DistrictGridCellMeters) + .5f) * DistrictGridCellMeters);
        }

        private Vector2? PreviewStreetConnection(IReadOnlyList<Vector2> points)
        {
            if (points.Count == 0) return null;
            if (streetSnapTarget != null) return streetSnapPoint;
            if (points.Count < 2) return null;
            var a = points[^2]; var b = points[^1];
            Vector2? nearest = null; float distance = float.PositiveInfinity;
            foreach (var road in districtRoads)
            {
                if (!StreetJunctions.IsStreet(road.surfaceId) || !districtRoadMeshes.ContainsKey(road)) continue;
                var line = RoadCenterline(road);
                for (int i = 0; i < line.Count - (road.closed ? 0 : 1); i++)
                {
                    if (!StreetJunctions.Intersection(a, b, line[i], line[(i + 1) % line.Count], out var p) || Vector2.Distance(a,p)<.1f) continue;
                    float d = (p-b).sqrMagnitude;
                    if (d < distance) { nearest=p;distance=d; }
                }
            }
            return nearest;
        }

        private void ClearStreetJunctionPresentation()
        {
            if(streetJunctionRoot){streetJunctionRoot.gameObject.SetActive(false);Destroy(streetJunctionRoot.gameObject);streetJunctionRoot=null;}
            foreach(var asset in streetJunctionAssets)Destroy(asset);
            streetJunctionAssets.Clear();streetJunctionNodes.Clear();
        }
        private void RefreshStreetJunctions()
        {
            ClearStreetJunctionPresentation();
            streetJunctionNodes = StreetJunctions.Build(districtRoads.Where(districtRoadMeshes.ContainsKey), RoadCenterline);
            if(streetJunctionNodes.Count==0)return;
            streetJunctionRoot=new GameObject("Connected street junctions").transform;
            streetJunctionRoot.SetParent(transform,false);
            streetJunctionRoot.gameObject.SetActive(!showRoadPaths);
            foreach(var node in streetJunctionNodes)
            {
                if(node.Arms.Count<2)continue;
                var outline=StreetJunctions.Outline(node);
                if(outline.Count<3 || !CityForge.Geometry.PointWireGeometry.IsSimple(outline))continue;
                var surfaces=node.Arms.Select(a=>a.Road.surfaceId).Distinct().Take(4).ToArray();
                var mat=new Material(Resources.Load<Shader>("UI/StreetJunction")){renderQueue=DistrictLotSurfaces.SidewalkRenderQueue+1};
                BindRoadRecess(mat);
                mat.SetFloat("_RecessBorder", surfaces.Any(id => IsMasonryRoad(id) || id is "blacktop" or "paved-street") ? 1 : 0);
                mat.SetTexture("_RecessDirt", Resources.Load<Texture2D>("Lots/PathMaterials/DirtRoadEarth"));
                for(int i=0;i<4;i++)
                {
                    string id=surfaces[Mathf.Min(i,surfaces.Length-1)];
                    var source=districtRoadMaterials[id];
                    var earth = id is FoundersStreetId or "dirt" ? Resources.Load<Texture2D>("Lots/PathMaterials/DirtRoadEarth") : null;
                    mat.SetTexture("_Layer"+i, earth ? earth : id == FoundersStreetId ? DistrictRoadStyle(id).Texture : source.GetTexture("_BaseMap"));
                    mat.SetColor("_Tint"+i,source.GetColor("_BaseColor"));
                }
                var vertices=new List<Vector3>();var weights=new List<Color>();var alpha=new List<Vector2>();
                var uv=new[]{new List<Vector2>(),new List<Vector2>(),new List<Vector2>(),new List<Vector2>()};
                var triangles=new List<int>();
                // Radial subdivisions follow terrain and smoothly converge arm textures at the center.
                Add(node.Point,0);
                const int rings=10;
                for(int ring=1;ring<=rings;ring++)
                for(int j=0;j<outline.Count;j++)Add(Vector2.Lerp(node.Point,outline[j],ring/(float)rings),ring/(float)rings);
                for(int j=0;j<outline.Count;j++)
                {
                    int next=(j+1)%outline.Count;
                    triangles.Add(0);triangles.Add(1+next);triangles.Add(1+j);
                    for(int ring=1;ring<rings;ring++)
                    {
                        int a=1+(ring-1)*outline.Count+j,b=1+(ring-1)*outline.Count+next;
                        int c=a+outline.Count,d=b+outline.Count;
                        triangles.Add(a);triangles.Add(b);triangles.Add(d);triangles.Add(a);triangles.Add(d);triangles.Add(c);
                    }
                }
                var mesh=new Mesh{name="Rounded connected street surface"};
                mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(weights);
                for(int i=0;i<4;i++)mesh.SetUVs(i,uv[i]);
                mesh.SetUVs(4,alpha);mesh.RecalculateNormals();
                // A flare is optional where nearby buildings, water or the district boundary leave no room.
                if(SelectedDistrict>=0 && RoadSiteProblem(mesh)!=null){Destroy(mesh);Destroy(mat);continue;}
                var go=new GameObject("Street junction · "+node.Arms.Count+" branches",typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(streetJunctionRoot,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.sortingOrder=MaxDistrictRoads+2;renderer.shadowCastingMode=ShadowCastingMode.Off;
                streetJunctionAssets.Add(mesh);streetJunctionAssets.Add(mat);

                void Add(Vector2 p,float radial)
                {
                    vertices.Add(new Vector3(p.x,LotHeight(p)+RoadSurfaceLift+.012f,p.y));
                    var w=new float[4];float total=0;float endAlpha=1;
                    foreach(var arm in node.Arms)
                    {
                        var offset=p-node.Point;
                        float along=Mathf.Max(0,Vector2.Dot(offset,arm.Direction));
                        float side=Vector2.Distance(offset,arm.Direction*along);
                        float value=1f/Mathf.Pow(.8f+side,3);
                        int slot=System.Array.IndexOf(surfaces,arm.Road.surfaceId);if(slot<0)slot=surfaces.Length-1;w[slot]+=value;total+=value;
                        float reach=StreetJunctions.Reach(node,arm);
                        if(side<=arm.Road.width*.5f+.02f)endAlpha=Mathf.Min(endAlpha,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(Mathf.Max(.15f, reach-3.5f),reach,along)));
                    }
                    weights.Add(new Color(w[0]/total,w[1]/total,w[2]/total,w[3]/total));alpha.Add(new Vector2(endAlpha,0));
                    for(int i=0;i<4;i++)
                    {
                        var road=node.Arms.First(a=>a.Road.surfaceId==surfaces[Mathf.Min(i,surfaces.Length-1)]).Road;
                        uv[i].Add(StreetSurfaceUV(road,p));
                    }
                }
            }
        }
        private static Vector2 StreetSurfaceUV(DistrictRoadRecord road, Vector2 point)
        {
            // Junction wear uses seamless earth rather than stretching wagon ruts or
            // repeating the wagon albedo's grassy shoulders through bends and crossings.
            return point / (road.surfaceId is FoundersStreetId or "dirt" ? Vector2.one * 4.5f : DistrictRoadStyle(road.surfaceId).TileSize);
        }
    }
}

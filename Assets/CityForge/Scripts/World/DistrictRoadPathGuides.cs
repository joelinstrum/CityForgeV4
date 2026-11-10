using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private readonly Dictionary<DistrictRoadRecord, MeshFilter> roadPathGuides = new();
        private Transform roadPathGuideRoot;
        private Material[] roadPathGuideMaterials;
        private static readonly Color RoadGuideBlue = new(.20f, .65f, 1f);
        private static readonly Color RoadGuideGold = new(1f, .78f, .31f);

        private void RefreshRoadPathGuide(DistrictRoadRecord road)
        {
            if (!roadPathGuideRoot)
            {
                roadPathGuideRoot = new GameObject("Road path visualization").transform;
                roadPathGuideRoot.SetParent(transform, false);
                roadPathGuideMaterials ??= new[]
                {
                    GuideMaterial(RoadGuideBlue),
                    GuideMaterial(new Color(.53f, .81f, 1f, .72f))
                };
            }

            if (!roadPathGuides.TryGetValue(road, out var filter))
            {
                var guide = new GameObject("Road path guide", typeof(MeshFilter), typeof(MeshRenderer));
                guide.transform.SetParent(roadPathGuideRoot, false);
                filter = guide.GetComponent<MeshFilter>();
                filter.sharedMesh = new Mesh { name = "Road path guide", indexFormat = IndexFormat.UInt32 };
                roadPathGuides.Add(road, filter);
                var renderer = guide.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = roadPathGuideMaterials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var line = RoadCenterline(road);
            var vertices = new List<Vector3>();
            var centers = new List<int>();
            var edges = new List<int>();
            int segments = road.closed ? line.Count : line.Count - 1;
            var sides = new Vector2[line.Count];
            for (int i = 0; i < line.Count; i++)
                sides[i] = LotPathSurface.RibbonSide(line, null, road.closed, i, road.width);
            float leftDistance = 0, rightDistance = 0;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % line.Count;
                AddLine(line[i], line[next], centers, .10f);
                AddDashed(line[i] - sides[i], line[next] - sides[next], ref leftDistance);
                AddDashed(line[i] + sides[i], line[next] + sides[next], ref rightDistance);
            }

            var mesh = filter.sharedMesh;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.subMeshCount = 2;
            mesh.SetIndices(centers, MeshTopology.Triangles, 0);
            mesh.SetIndices(edges, MeshTopology.Triangles, 1);

            void AddLine(Vector2 start, Vector2 end, List<int> indices, float width)
            {
                var delta = end - start;
                if (delta.sqrMagnitude < .000001f) return;
                var side = Vector2.Perpendicular(delta.normalized) * (width * .5f);
                int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude));
                int first = vertices.Count;
                for (int i = 0; i <= steps; i++)
                {
                    var point = Vector2.Lerp(start, end, (float)i / steps);
                    vertices.Add(RoadGuideGround(point - side));
                    vertices.Add(RoadGuideGround(point + side));
                    if (i == 0) continue;
                    int a = first + (i - 1) * 2;
                    indices.Add(a);
                    indices.Add(a + 1);
                    indices.Add(a + 3);
                    indices.Add(a);
                    indices.Add(a + 3);
                    indices.Add(a + 2);
                }
            }

            void AddDashed(Vector2 start, Vector2 end, ref float distance)
            {
                const float period = 2f;
                const float dash = 1.05f;
                float length = Vector2.Distance(start, end);
                if (length < .0001f) return;
                for (float from = -(distance % period); from < length; from += period)
                {
                    float to = Mathf.Min(length, from + dash);
                    if (to <= 0) continue;
                    AddLine(Vector2.Lerp(start, end, Mathf.Max(0, from) / length),
                        Vector2.Lerp(start, end, to / length), edges, .07f);
                }
                distance += length;
            }
        }

        private Material GuideMaterial(Color color)
        {
            var material = Own(new Material(Shader.Find("Universal Render Pipeline/Unlit")));
            LotGroundTexture.ConfigureTransparentLayer(material, 3);
            material.SetColor("_BaseColor", color);
            return material;
        }

        private void ClearRoadPathGuides()
        {
            if (roadPathGuideRoot)
            {
                roadPathGuideRoot.gameObject.SetActive(false);
                Destroy(roadPathGuideRoot.gameObject);
                roadPathGuideRoot = null;
            }
            foreach (var filter in roadPathGuides.Values)
                if (filter) Destroy(filter.sharedMesh);
            roadPathGuides.Clear();
        }

        private Vector3 RoadGuideGround(Vector2 point) =>
            new(point.x, LotHeight(point) + .27f, point.y);

        private bool ProjectRoadPoint(Vector2 point, out Vector2 gui)
        {
            var screen = roadPathCamera.WorldToScreenPoint(RoadGuideGround(point));
            gui = new Vector2(screen.x, Screen.height - screen.y);
            return screen.z > 0;
        }

        private void DrawRoadPathHandles(Event pointer)
        {
            if (!showRoadPaths || !roadPathCamera || pointer.type != EventType.Repaint) return;
            var previous = GUI.color;
            var matrix = GUI.matrix;
            foreach (var road in districtRoadMeshes.Keys)
                for (int i = 0; i < road.points.Length; i++)
                {
                    bool selected = road == selectedRoadPath && i == selectedRoadPoint;
                    var preview = road == selectedRoadPath ? roadPointPreview : null;
                    var point = preview?[i] ?? road.points[i];
                    if (!ProjectRoadPoint(point, out var screen)) continue;
                    float radius = selected ? 7 : 4;
                    GUI.color = new Color(.05f, .12f, .20f);
                    GUI.DrawTexture(new Rect(screen.x - radius - 1, screen.y - radius - 1,
                        (radius + 1) * 2, (radius + 1) * 2), Texture2D.whiteTexture);
                    GUI.color = selected ? RoadGuideGold : RoadGuideBlue;
                    GUI.DrawTexture(new Rect(screen.x - radius, screen.y - radius, radius * 2, radius * 2),
                        Texture2D.whiteTexture);
                    if (selected) DrawAlignmentGuide(point, road.width);
                    if (preview != null && (i + 1 < preview.Length || road.closed))
                    {
                        GUI.color = RoadGuideGold;
                        DrawLine(point, preview[(i + 1) % preview.Length]);
                    }
                }
            GUI.color = previous;

            void DrawAlignmentGuide(Vector2 point, float width)
            {
                float half = width * .5f;
                float x = GuideCoordinate(point.x);
                float z = GuideCoordinate(point.y);
                GUI.color = new Color(RoadGuideGold.r, RoadGuideGold.g, RoadGuideGold.b, .65f);
                for (int i = -4; i < 4; i++)
                {
                    DrawLine(new Vector2(x, point.y + i * 2), new Vector2(x, point.y + i * 2 + 1));
                    DrawLine(new Vector2(point.x + i * 2, z), new Vector2(point.x + i * 2 + 1, z));
                }
                GUI.color = RoadGuideGold;

                float GuideCoordinate(float coordinate)
                {
                    float lower = NearestRoadGridLine(coordinate - half);
                    float upper = NearestRoadGridLine(coordinate + half);
                    return Mathf.Abs(coordinate - half - lower) <= Mathf.Abs(coordinate + half - upper) ? lower : upper;
                }
            }

            void DrawLine(Vector2 a, Vector2 b)
            {
                if (!ProjectRoadPoint(a, out var start) || !ProjectRoadPoint(b, out var end)) return;
                var delta = end - start;
                GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
                GUI.DrawTexture(new Rect(start.x, start.y, delta.magnitude, 2), Texture2D.whiteTexture);
                GUI.matrix = matrix;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.Lots
{
    [Serializable]
    public sealed class LotPathDefinition
    {
        internal const int MaxPaths = 128;
        public Vector2[] points;
        public bool closed;
        public bool curved;
        public int width;
        public int textureRotation;
        public int edgeBlend;
        public bool recessed;

        internal const int MaxEdgeBlend = 100;
        internal float BlendMeters(int value) => width * .45f * value / MaxEdgeBlend;
        public string materialId;
    }

    internal sealed class LotPathMaterial
    {
        internal const string ResidentialFootpath = "residential-footpath";

        public static readonly LotPathMaterial[] All =
        {
            new LotPathMaterial("cobblestone", "Cobblestone", "Paving", new Color32(122, 116, 107, 255), 2f),
            new LotPathMaterial("cobblestone-road-9", "Cobblestone Road 9", "Paving",
                new Color32(122, 116, 107, 255), 4f, requiresPbr: true),
            new LotPathMaterial("cobblestone-road-9-wet", "Cobblestone Road 9 with Puddles", "Paving",
                new Color32(106, 102, 97, 255), 4f, requiresPbr: true),
            new LotPathMaterial("brick", "Red Brick", "Paving", new Color32(151, 84, 61, 255), 2f),
            new LotPathMaterial("damaged-brick", "Damaged Brick", "Paving",
                new Color32(139, 83, 65, 255), 4f, requiresPbr: true),
            new LotPathMaterial("timber-deck", "Timber Deck", "Paving", new Color32(111, 79, 50, 255), 4f),
            new LotPathMaterial("concrete", "Concrete", "Paving", new Color32(168, 167, 159, 255), 3f),
            new LotPathMaterial("gravel", "Gravel", "Natural", new Color32(135, 124, 105, 255), 3f, 1f),
            new LotPathMaterial("packed-earth", "Packed Earth", "Natural", new Color32(130, 97, 67, 255), 3f),
            new LotPathMaterial(ResidentialFootpath, "Soft Dirt Footpath", "Natural", new Color32(130, 110, 82, 255), 3f),
            new LotPathMaterial("rich-brown-garden-soil", "Rich Brown Garden Soil", "Natural",
                new Color32(106, 76, 53, 255), 3f, requiresTexture: true),
            new LotPathMaterial("tilled-garden-soil", "Tilled Garden Soil", "Natural",
                new Color32(127, 96, 66, 255), 3f, requiresTexture: true),
            new LotPathMaterial("garden-soil", "Realistic Garden Soil", "Natural",
                new Color32(114, 85, 60, 255), 3f, requiresTexture: true)
        };

        public readonly string Id, Label, Group;
        public readonly Color Fallback;
        public readonly Vector2 TileSize;

        private readonly bool requiresPbr, requiresTexture;
        private Material PbrMaterial => Resources.Load<Material>("Lots/PathMaterials/PBR/" + Id);
        public bool IsAvailable => (!requiresPbr || PbrMaterial) && (!requiresTexture || ColorTexture);

        private LotPathMaterial(string id, string label, string group, Color fallback, float tileX, float tileZ = 0f,
            bool requiresPbr = false, bool requiresTexture = false)
        {
            Id = id;
            Label = label;
            Group = group;
            Fallback = fallback;
            this.requiresPbr = requiresPbr;
            this.requiresTexture = requiresTexture;
            TileSize = new Vector2(tileX, tileZ > 0f ? tileZ : tileX);
        }

        private static Texture2D deckTexture;
        private Texture2D ColorTexture => Id == "timber-deck"
            ? deckTexture ? deckTexture : (deckTexture = CreateDeckTexture())
            : Resources.Load<Texture2D>("Lots/PathMaterials/" + (Id == ResidentialFootpath ? "packed-earth" : Id));

        private static Texture2D CreateDeckTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, true)
            {
                name = "Timber deck",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };
            var pixels = new Color32[size * size];
            for (int z = 0; z < size; z++)
            for (int x = 0; x < size; x++)
            {
                int row = z / 8;
                int plankX = (x + row * 23) % size;
                int joint = plankX % 64;
                int grain = Mathf.RoundToInt(4f * Mathf.Sin((z * 3f + Mathf.Sin(x * Mathf.PI / 64f)) * 1.7f));
                int shade = 108 + (row * 17 + plankX / 64 * 9) % 24 + grain;
                if (z % 8 == 0 || joint == 0) shade -= 48;
                pixels[z * size + x] = new Color32((byte)shade, (byte)(shade * .77f), (byte)(shade * .55f), 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        public Texture2D Texture
        {
            get
            {
                var pbr = PbrMaterial;
                return pbr && pbr.mainTexture is Texture2D texture ? texture : ColorTexture;
            }
        }

        public Material CreateMaterial()
        {
            var pbr = PbrMaterial;
            if (pbr) return new Material(pbr);

            var texture = ColorTexture;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", texture ? Color.white : Fallback);
            material.SetFloat("_Smoothness", .08f);
            if (texture) material.SetTexture("_BaseMap", texture);
            return material;
        }

        public static void ApplyEdgeTreatment(Material material, LotPathDefinition path, int value, int order)
        {
            var shader = Resources.Load<Shader>("Region/LotPathBlend");
            if (!shader) return;
            if (material.shader != shader)
            {
                var keywords = material.shaderKeywords;
                material.shader = shader;
                material.shaderKeywords = keywords;
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + LotGroundOverlay.MaxCount + 10 + order;
            }
            material.SetFloat("_EdgeBlend", path.BlendMeters(value));
            material.SetFloat("_PathOpacity", path.materialId == ResidentialFootpath ? .58f : 1f);
            material.SetFloat("_Recessed", path.recessed ? 1f : 0f);
        }

        public static LotPathMaterial Find(string id) => Array.Find(All, style => style.Id == id);
    }

    internal static class LotPathSurface
    {
        public static readonly int[] Widths = { 1, 3, 6, 9, 12 };

        // Shared by the editor guide and the paved mesh. The optional last point is the live mouse preview.
        public static Vector2 RibbonSide(IReadOnlyList<Vector2> points, Vector2? end, bool loop, int index, float width)
        {
            int count = points.Count + (end.HasValue ? 1 : 0);
            var point = PointAt(index);
            if (!loop && index == 0) return Normal(point, PointAt(1)) * (width * .5f);
            if (!loop && index == count - 1) return Normal(PointAt(index - 1), point) * (width * .5f);

            var incoming = Normal(PointAt((index + count - 1) % count), point);
            var outgoing = Normal(point, PointAt((index + 1) % count));
            var bisector = (incoming + outgoing).normalized;
            return bisector * (width * .5f / Mathf.Max(.5f, Vector2.Dot(bisector, outgoing)));

            Vector2 PointAt(int i) => i < points.Count ? points[i] : end.GetValueOrDefault();
            Vector2 Normal(Vector2 a, Vector2 b) => Vector2.Perpendicular((b - a).normalized);
        }

        public static void RoundCorners(IReadOnlyList<Vector2> points, Vector2? end, bool loop,
            float width, List<Vector2> rounded)
        {
            const int steps = 6;
            rounded.Clear();
            int count = points.Count + (end.HasValue ? 1 : 0);
            for (int i = 0; i < count; i++)
            {
                var corner = PointAt(i);
                if (!loop && (i == 0 || i == count - 1))
                {
                    rounded.Add(corner);
                    continue;
                }

                var before = PointAt((i + count - 1) % count) - corner;
                var after = PointAt((i + 1) % count) - corner;
                float trim = Mathf.Min(Mathf.Max(1.5f, width * 1.25f), .45f * Mathf.Min(before.magnitude, after.magnitude));
                if (trim < .001f || Mathf.Abs(Vector2.Dot(before.normalized, after.normalized)) > .999f)
                {
                    rounded.Add(corner);
                    continue;
                }

                var start = corner + before.normalized * trim;
                var finish = corner + after.normalized * trim;
                for (int step = 0; step <= steps; step++)
                {
                    float t = (float)step / steps;
                    rounded.Add(Vector2.Lerp(Vector2.Lerp(start, corner, t), Vector2.Lerp(corner, finish, t), t));
                }
            }

            Vector2 PointAt(int i) => i < points.Count ? points[i] : end.GetValueOrDefault();
        }

        public static Mesh BuildMesh(LotPathDefinition path, Vector2 tileSize, float height, Func<Vector2, float> ground = null)
        {
            IReadOnlyList<Vector2> points = path.points;
            if (path.curved)
            {
                var rounded = new List<Vector2>();
                RoundCorners(points, null, path.closed, path.width, rounded);
                points = rounded;
            }

            int count = points.Count;
            int segments = path.closed ? count : count - 1;
            var vertices = new Vector3[count * 2];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            for (int i = 0; i < count; i++)
            {
                var point = points[i];
                var side = RibbonSide(points, null, path.closed, i, path.width);
                AddVertex(i * 2, point - side);
                AddVertex(i * 2 + 1, point + side);
            }
            for (int i = 0; i < segments; i++)
            {
                int a = i * 2;
                int b = (i + 1) % count * 2;
                int t = i * 6;
                triangles[t] = a;
                triangles[t + 1] = a + 1;
                triangles[t + 2] = b + 1;
                triangles[t + 3] = a;
                triangles[t + 4] = b + 1;
                triangles[t + 5] = b;
            }

            var distance = new float[count];
            for (int i = 1; i < count; i++) distance[i] = distance[i - 1] + Vector2.Distance(points[i - 1], points[i]);
            float length = distance[count - 1];
            if (path.closed) length = -(length + Vector2.Distance(points[count - 1], points[0]));
            if (ground != null) return BuildTerrainRibbon(vertices, segments, tileSize, height, ground, path.width, distance, length, path.textureRotation);
            var ribbon = new List<Vector4>(vertices.Length);
            foreach (float along in distance)
            {
                ribbon.Add(new Vector4(0, path.width, along, length));
                ribbon.Add(new Vector4(path.width, path.width, along, length));
            }
            var mesh = new Mesh { name = "Lot path surface", vertices = vertices, normals = normals, uv = uv, triangles = triangles };
            mesh.SetUVs(1, ribbon);
            mesh.RecalculateTangents();
            return mesh;

            void AddVertex(int index, Vector2 point)
            {
                vertices[index] = new Vector3(point.x, height, point.y);
                normals[index] = Vector3.up;
                uv[index] = LotGroundOverlay.QuarterTurn(point, -path.textureRotation) / tileSize;
            }
        }
        private static Mesh BuildTerrainRibbon(Vector3[] edges, int segments, Vector2 tileSize, float lift,
            Func<Vector2, float> ground, float width, float[] distance, float length, int textureRotation)
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var ribbon = new List<Vector4>();
            var triangles = new List<int>();
            int across = 1;
            for (int i = 0; i < edges.Length; i += 2)
                across = Mathf.Max(across, Mathf.CeilToInt(Vector3.Distance(edges[i], edges[i + 1])));
            int stride = across + 1;
            for (int segment = 0; segment < segments; segment++)
            {
                int a = segment * 2;
                int b = (a + 2) % edges.Length;
                float endDistance = b == 0 ? -length : distance[b / 2];
                int along = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Vector3.Distance(edges[a], edges[b]),
                    Vector3.Distance(edges[a + 1], edges[b + 1]))));
                for (int row = segment == 0 ? 0 : 1; row <= along; row++)
                {
                    float t = (float)row / along;
                    var left = Vector3.Lerp(edges[a], edges[b], t);
                    var right = Vector3.Lerp(edges[a + 1], edges[b + 1], t);
                    int start = vertices.Count;
                    for (int column = 0; column <= across; column++)
                    {
                        var point = Vector3.Lerp(left, right, (float)column / across);
                        var planar = new Vector2(point.x, point.z);
                        point.y = ground(planar) + lift;
                        vertices.Add(point);
                        uv.Add(LotGroundOverlay.QuarterTurn(planar, -textureRotation) / tileSize);
                        ribbon.Add(new Vector4((float)column / across * width, width,
                            Mathf.Lerp(distance[a / 2], endDistance, t), length));
                    }
                    if (start == 0) continue;
                    for (int column = 0; column < across; column++)
                    {
                        int next = start + column;
                        int prev = next - stride;
                        triangles.Add(prev);
                        triangles.Add(prev + 1);
                        triangles.Add(next + 1);
                        triangles.Add(prev);
                        triangles.Add(next + 1);
                        triangles.Add(next);
                    }
                }
            }
            var mesh = new Mesh { name = "Lot path surface" };
            if (vertices.Count > ushort.MaxValue) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetUVs(1, ribbon);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}

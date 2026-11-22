using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private const float RoadRecessDepth = .1524f;
        private Texture2D roadRecessField;
        private Vector4 roadRecessRect;
        private ulong roadRecessSignature;
        private double roadRecessBuildMilliseconds;

        private void RefreshRoadRecess(StreetSurfaceMask footprint, DistrictRoadRecord[] roads)
        {
            ulong signature = 1469598103934665603UL;
            void Hash(int value) { signature = unchecked((signature ^ (uint)value) * 1099511628211UL); }
            foreach (var road in roads)
            {
                Hash(road.width); Hash(road.closed ? 1 : 0); Hash(road.curved ? 1 : 0);
                foreach (char c in road.surfaceId) Hash(c);
                foreach (var point in road.points) { Hash(point.x.GetHashCode()); Hash(point.y.GetHashCode()); }
            }
            if (roadRecessField && signature == roadRecessSignature) return;
            ClearRoadRecess(); roadRecessSignature = signature;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var edges = footprint.BoundaryEdges();
            if (edges.Count == 0) return;
            var min = edges[0].A; var max = min;
            var index = new Dictionary<Vector2Int, List<(Vector2 A, Vector2 B)>>();
            const float range = 2f, cellSize = 8f;
            foreach (var edge in edges)
            {
                min = Vector2.Min(min, Vector2.Min(edge.A, edge.B)); max = Vector2.Max(max, Vector2.Max(edge.A, edge.B));
                var first = Vector2Int.FloorToInt((Vector2.Min(edge.A, edge.B) - Vector2.one * range) / cellSize);
                var last = Vector2Int.FloorToInt((Vector2.Max(edge.A, edge.B) + Vector2.one * range) / cellSize);
                for (int y = first.y; y <= last.y; y++) for (int x = first.x; x <= last.x; x++)
                {
                    var key = new Vector2Int(x, y);
                    if (!index.TryGetValue(key, out var list)) index.Add(key, list = new());
                    list.Add(edge);
                }
            }
            min -= Vector2.one * range; max += Vector2.one * range;
            var size = max - min;
            int width = Mathf.Clamp(Mathf.CeilToInt(size.x * 2), 8, 1024);
            int height = Mathf.Clamp(Mathf.CeilToInt(size.y * 2), 8, 1024);
            var values = new float[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                var p = min + Vector2.Scale(size, new Vector2((x + .5f) / width, (y + .5f) / height));
                float distance = range * range;
                if (index.TryGetValue(Vector2Int.FloorToInt(p / cellSize), out var candidates))
                    foreach (var edge in candidates)
                    {
                        var d = edge.B - edge.A;
                        var nearest = edge.A + d * Mathf.Clamp01(Vector2.Dot(p - edge.A, d) / d.sqrMagnitude);
                        distance = Mathf.Min(distance, (p - nearest).sqrMagnitude);
                    }
                values[y * width + x] = Mathf.Sqrt(distance) * (footprint.Contains(p) ? 1 : -1);
            }
            roadRecessField = new Texture2D(width, height, TextureFormat.RFloat, false, true)
            { name = "Shared road recess boundary", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            roadRecessField.SetPixelData(values, 0); roadRecessField.Apply(false, true);
            roadRecessRect = new Vector4(min.x, min.y, 1 / size.x, 1 / size.y);
            foreach (var material in districtRoadMaterials.Values) BindRoadRecess(material);
            foreach (var asset in streetJunctionAssets)
                if (asset is Material material) BindRoadRecess(material);
            timer.Stop(); roadRecessBuildMilliseconds = timer.Elapsed.TotalMilliseconds;
        }
        private void BindRoadRecess(Material material)
        {
            material.SetFloat("_BorderTextured",0);
            material.SetFloat("_BorderSides",0);
            material.SetFloat("_RecessDepth", RoadRecessDepth);
            material.SetFloat("_RecessUseFootprint", roadRecessField ? 1 : 0);
            if (roadRecessField) material.SetTexture("_RecessFootprint", roadRecessField);
            material.SetVector("_RecessFootprintRect", roadRecessRect);
        }
        private void ClearRoadRecess()
        {
            if (roadRecessField) Destroy(roadRecessField);
            roadRecessField = null;
        }
    }
}

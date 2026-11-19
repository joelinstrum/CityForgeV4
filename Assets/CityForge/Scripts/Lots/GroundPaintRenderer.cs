using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.Lots
{
    internal sealed class GroundPaintRenderer : MonoBehaviour
    {
        private sealed class Controls { internal Texture2D A, B; internal int Users; }
        private static readonly Dictionary<string, Controls> shared = new();
        private sealed class TileView
        {
            internal GameObject Object;
            internal Mesh Mesh; internal Material Material; internal Controls Maps; internal string Key;
        }
        private readonly Dictionary<GroundPaintTile, TileView> views = new();
        private Func<Vector2, float> height;
        private Predicate<Vector2> contains;
        private Rect? clip;
        internal void Initialize(GroundPaintData data, Func<Vector2, float> ground, Rect? bounds = null, Predicate<Vector2> visible = null)
        {
            height = ground; clip = bounds; contains = visible;
            if (data != null) foreach (var tile in data.tiles) Refresh(tile);
        }
        internal void Refresh(GroundPaintTile tile)
        {
            var bounds = new Rect(tile.Minimum, Vector2.one * GroundPaintData.TileMeters);
            if (clip.HasValue && !clip.Value.Overlaps(bounds)) return;
            if (!views.TryGetValue(tile, out var view))
            {
                var go = new GameObject("Paint weight tile", typeof(MeshFilter), typeof(MeshRenderer));
                go.layer = gameObject.layer; go.transform.SetParent(transform, false);
                view = new TileView { Object = go, Mesh = Build(tile), Material = new Material(Resources.Load<Shader>("UI/TexturePaintGround")) };
                view.Material.renderQueue = (int)RenderQueue.Transparent + LotGroundOverlay.MaxCount + 1;
                go.GetComponent<MeshFilter>().sharedMesh = view.Mesh;
                var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = view.Material;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = true;
                views.Add(tile, view);
            }
            if (tile.editing != null)
            {
                if (view.Key != null || view.Maps == null) { Release(view); view.Maps = NewMaps(); }
                Upload(view.Maps, tile.Read());
            }
            else if (view.Maps == null)
            {
                var key = tile.weights ?? "";
                if (!shared.TryGetValue(key, out var maps)) { maps = NewMaps(); Upload(maps, tile.Read()); shared.Add(key, maps); }
                maps.Users++; view.Maps = maps; view.Key = key;
            }
            var material = view.Material;
            material.SetTexture("_WeightsA", view.Maps.A); material.SetTexture("_WeightsB", view.Maps.B);
            for (int i = 0; i < GroundPaintData.Layers; i++)
            {
                var style = i < tile.materials.Length ? LotPathMaterial.Find(tile.materials[i]) : null;
                material.SetTexture("_Layer" + i, style?.Texture ?? Texture2D.whiteTexture);
                material.SetColor("_Tint" + i, style == null || style.Texture ? Color.white : style.Fallback);
                var size = style?.TileSize ?? Vector2.one;
                material.SetVector("_Repeat" + i, new Vector4(1 / size.x, 1 / size.y, 0, 0));
            }
        }
        private Mesh Build(GroundPaintTile tile)
        {
            const int n = 32;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var uv2 = new List<Vector2>(); var triangles = new List<int>();
            for (int z = 0; z < n; z++) for (int x = 0; x < n; x++)
            {
                var corner = tile.Minimum + new Vector2(x, z);
                var middle = corner + Vector2.one * .5f;
                if (clip.HasValue && !clip.Value.Contains(middle) || contains != null && !contains(middle)) continue;
                int start = vertices.Count;
                Add(corner); Add(corner + Vector2.up); Add(corner + Vector2.one); Add(corner + Vector2.right);
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            var mesh = new Mesh { name = "Shared texture paint terrain chunk" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetUVs(1, uv2); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
            void Add(Vector2 point)
            {
                vertices.Add(new Vector3(point.x, height(point) + .0825f, point.y));
                uv.Add((point - tile.Minimum) / GroundPaintData.TileMeters); uv2.Add(point);
            }
        }
        private static Controls NewMaps() => new() { A = Map(), B = Map() };
        private static Texture2D Map() => new(GroundPaintData.Resolution, GroundPaintData.Resolution, TextureFormat.RGBA32, false, true)
            { name = "Paint weights", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        private static void Upload(Controls maps, byte[] bytes)
        {
            int pixels = GroundPaintData.Resolution * GroundPaintData.Resolution;
            var a = new Color32[pixels]; var b = new Color32[pixels];
            for (int i = 0; i < pixels; i++)
            {
                int j = i * 8; a[i] = new Color32(bytes[j], bytes[j+1], bytes[j+2], bytes[j+3]);
                b[i] = new Color32(bytes[j+4], bytes[j+5], bytes[j+6], bytes[j+7]);
            }
            maps.A.SetPixels32(a); maps.A.Apply(false); maps.B.SetPixels32(b); maps.B.Apply(false);
        }
        private static void Release(TileView view)
        {
            if (view.Maps == null) return;
            if (view.Key == null || --view.Maps.Users == 0)
            { Destroy(view.Maps.A); Destroy(view.Maps.B); if (view.Key != null) shared.Remove(view.Key); }
            view.Maps = null; view.Key = null;
        }
        internal void RemoveMissing(GroundPaintData data)
        {
            var removed = new List<GroundPaintTile>();
            foreach (var pair in views)
                if (!data.tiles.Contains(pair.Key))
                {
                    var view = pair.Value; Release(view); Destroy(view.Mesh); Destroy(view.Material);
                    if (view.Object) { view.Object.SetActive(false); Destroy(view.Object); }
                    removed.Add(pair.Key);
                }
            foreach (var key in removed) views.Remove(key);
        }
        private void OnDestroy()
        {
            foreach (var view in views.Values) { Release(view); Destroy(view.Mesh); Destroy(view.Material); }
            views.Clear();
        }
    }
}

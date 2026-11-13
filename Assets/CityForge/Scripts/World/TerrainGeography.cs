using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    // One continuous control lattice per displayed terrain. Samples include both
    // edges, so adjacent control lattices can share the exact boundary values.
    internal sealed class TerrainGeography : MonoBehaviour
    {
        const int Chunk = 16;
        const float MoistReach = 44f;
        MeadowDistrictLandscape land;
        Terrain terrain;
        Texture2D first, second;
        int resolution;
        float step;
        bool ready, diagnostic, enabledBlend = true;
        readonly HashSet<Vector2Int> dirty = new();
        readonly List<(RegionWaterBody Body, Rect Bounds, List<Vector2[]> Contours)> freshwater = new();
        readonly Dictionary<Vector2Int, List<Rect>> settlements = new();
        readonly HashSet<Rect> previousLots = new();
        MaterialPropertyBlock properties;

        public IEnumerator Initialize(MeadowDistrictLandscape district)
        {
            land = district;
            properties = new MaterialPropertyBlock();

            terrain = land.DisplayTerrain;
            resolution = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(land.Side / 5f)), 64, 256) + 1;
            step = land.Side / (resolution - 1);
            first = CreateMap("Meadow / dry / woodland / moist");
            second = CreateMap("Disturbed / rocky");
            RefreshWater();
            RefreshSettlements();
            dirty.Clear();
            var timer = System.Diagnostics.Stopwatch.StartNew();
            for (int z = 0; z < resolution; z += Chunk)
            for (int x = 0; x < resolution; x += Chunk)
            {
                WriteChunk(new Vector2Int(x / Chunk, z / Chunk));
                if (timer.Elapsed.TotalMilliseconds >= 3)
                {
                    yield return null;
                    timer.Restart();
                }
            }
            Upload();
            ready = true;
            Bind();
        }

        Texture2D CreateMap(string label) => new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true)
        { name = label, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };

        [ContextMenu("Toggle geography diagnostics")]
        void ToggleDiagnostic() => SetDiagnostic(!diagnostic);

        [ContextMenu("Toggle geography blend")]
        void ToggleBlend() => SetBlendEnabled(!enabledBlend);

        public void SetDiagnostic(bool show)
        {
            diagnostic = show;
            if (ready) Bind();
        }

        public void SetBlendEnabled(bool value)
        {
            enabledBlend = value;
            if (ready) Bind();
        }

        void Bind()
        {
            terrain.GetSplatMaterialPropertyBlock(properties);
            properties.SetTexture("_GeographyWeights0", first);
            properties.SetTexture("_GeographyWeights1", second);
            properties.SetVector("_GeographyGrid", new Vector4(land.Origin.x, land.Origin.y,
                (resolution - 1f) / (resolution * land.Side), .5f / resolution));
            properties.SetFloat("_GeographyMode", !enabledBlend ? 0 : diagnostic ? 2 : 1);
            BindPalette();
            terrain.SetSplatMaterialPropertyBlock(properties);
        }

        TerrainGeographyPalette palette;
        void BindPalette()
        {
            if (!palette) palette = Resources.Load<TerrainGeographyPalette>("Region/TerrainGeographyPalette");
            if (!palette || palette.surfaces == null || palette.surfaces.Length != 6)
            {
                if (!diagnostic) properties.SetFloat("_GeographyMode", 0);
                return;
            }

            for (int i = 0; i < palette.surfaces.Length; i++)
            {
                var surface = palette.surfaces[i];
                if (surface == null || !surface.albedo)
                {
                    properties.SetVector("_GeographySurface" + i, Vector4.zero);
                    continue;
                }
                properties.SetTexture("_GeographyAlbedo" + i, surface.albedo);
                properties.SetVector("_GeographyAlbedoBias" + i, surface.albedoBias);
                properties.SetVector("_GeographyAlbedoMean" + i, surface.distantAlbedoMean);
                if (surface.normal) properties.SetTexture("_GeographyNormal" + i, surface.normal);
                properties.SetVector("_GeographySurface" + i, new Vector4(
                    1f / Mathf.Max(1f, surface.tileMeters), surface.smoothness,
                    surface.normal ? surface.normalStrength : 0f, surface.albedoScale));
            }
        }

        internal void RefreshWater()
        {
            freshwater.Clear();
            float margin = MoistReach / land.View.MapSide;
            var district = new Rect(land.Origin / land.View.MapSide + Vector2.one * .5f,
                Vector2.one * (land.Side / land.View.MapSide));
            foreach (var body in land.View.Data.WaterBodies)
            {
                if (body.kind != WaterBodyKind.River && body.kind != WaterBodyKind.Lake && body.kind != WaterBodyKind.Pond)
                    continue;
                var bounds = body.Bounds;
                bounds.min -= Vector2.one * margin;
                bounds.max += Vector2.one * margin;
                if (!bounds.Overlaps(district, true)) continue;
                freshwater.Add((body, bounds, body.centerline is { Length: > 1 } ? null : new List<Vector2[]>(body.Contours)));
            }
        }

        internal void RefreshSettlements()
        {
            var current = new HashSet<Rect>();
            foreach (var lot in land.View.PlacedLots)
            {
                if (lot.definition == null || !land.View.InActiveDistrict(lot.center)) continue;
                // Small apron surrounding each footprint. Lot overlays still draw above this map.
                Vector2 size = lot.definition.Size;
                if (lot.rotation % 2 != 0) size = new Vector2(size.y, size.x);
                var footprint = new Rect(lot.center - size * .5f, size);
                if (!current.Add(footprint)) continue;
                if (!previousLots.Contains(footprint)) UpdateFootprint(footprint, true);
            }
            foreach (var old in previousLots)
                if (!current.Contains(old)) UpdateFootprint(old, false);
            previousLots.Clear();
            previousLots.UnionWith(current);
        }

        private void UpdateFootprint(Rect footprint, bool add)
        {
            var padded = Pad(footprint, 9f);
            for (int z = Mathf.FloorToInt(padded.yMin / 32); z <= Mathf.FloorToInt(padded.yMax / 32); z++)
            for (int x = Mathf.FloorToInt(padded.xMin / 32); x <= Mathf.FloorToInt(padded.xMax / 32); x++)
            {
                var cell = new Vector2Int(x, z);
                if (add)
                {
                    if (!settlements.TryGetValue(cell, out var lots)) settlements[cell] = lots = new List<Rect>();
                    lots.Add(footprint);
                }
                else if (settlements.TryGetValue(cell, out var lots))
                {
                    lots.Remove(footprint);
                    if (lots.Count == 0) settlements.Remove(cell);
                }
            }
            Invalidate(padded);
        }

        internal static Rect Pad(Rect bounds, float padding) => Rect.MinMaxRect(
            bounds.xMin - padding, bounds.yMin - padding, bounds.xMax + padding, bounds.yMax + padding);

        internal void Invalidate(Rect world)
        {
            var min = (world.min - land.Origin) / step;
            var max = (world.max - land.Origin) / step;
            if (max.x < 0 || max.y < 0 || min.x >= resolution || min.y >= resolution) return;
            int lowX = Mathf.Max(0, Mathf.FloorToInt(min.x) - 1) / Chunk;
            int lowZ = Mathf.Max(0, Mathf.FloorToInt(min.y) - 1) / Chunk;
            int highX = Mathf.Min(resolution - 1, Mathf.CeilToInt(max.x) + 1) / Chunk;
            int highZ = Mathf.Min(resolution - 1, Mathf.CeilToInt(max.y) + 1) / Chunk;
            for (int z = lowZ; z <= highZ; z++)
            for (int x = lowX; x <= highX; x++)
                dirty.Add(new Vector2Int(x,z));
        }

        void LateUpdate()
        {
            if (!ready || dirty.Count == 0) return;
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            long budget = System.Diagnostics.Stopwatch.Frequency / 500;
            do
            {
                Vector2Int cell;
                using (var iterator = dirty.GetEnumerator())
                {
                    iterator.MoveNext();
                    cell = iterator.Current;
                }
                dirty.Remove(cell);
                WriteChunk(cell);
            }
            while (dirty.Count > 0 && System.Diagnostics.Stopwatch.GetTimestamp() - start < budget);
            Upload();
        }

        void Upload()
        {
            first.Apply(false);
            second.Apply(false);
        }

        void WriteChunk(Vector2Int chunk)
        {
            int x0 = chunk.x * Chunk, z0 = chunk.y * Chunk;
            int width = Mathf.Min(Chunk, resolution - x0), height = Mathf.Min(Chunk, resolution - z0);
            var primary = first.GetPixelData<Color32>(0);
            var extra = second.GetPixelData<Color32>(0);
            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                var point = land.Origin + new Vector2(x0 + x, z0 + z) * step;
                Sample(point, out var a, out var b);
                int index = (z0 + z) * resolution + x0 + x;
                primary[index] = a;
                extra[index] = b;
            }
        }

        void Sample(Vector2 point, out Color32 primary, out Color32 extra)
        {
            var uv = point / land.View.MapSide + Vector2.one * .5f;
            var biome = land.View.Data.Biomes?.At(uv);
            float height = land.Height(point);
            float slope = land.MountainSlope(point);
            // Offset the geographic thresholds slightly at two scales. These
            // signals shape an existing edge and never pick a surface by themselves.
            float broadEdge = land.Noise(point, 37f, 19) - .5f;
            float edgeDetail = land.Noise(point, 17f, 47) - .5f;
            float irregular = broadEdge * .15f + edgeDetail * .065f;
            var local = point - land.Origin;
            float edge = Mathf.Min(Mathf.Min(local.x, local.y), Mathf.Min(land.Side - local.x, land.Side - local.y));
            // Use the shared regional height at the district boundary so a
            // clamped local slope cannot introduce a control-map seam.
            if (edge < 18f)
            {
                float heightScale = terrain.terrainData.size.y;
                float d = 2f / land.View.MapSide;
                var data = land.View.Data;
                float sharedHeight = data.SmoothHeight(uv.x,uv.y) * heightScale;
                float dx = (data.SmoothHeight(uv.x+d,uv.y)-data.SmoothHeight(uv.x-d,uv.y)) * heightScale / 4f;
                float dz = (data.SmoothHeight(uv.x,uv.y+d)-data.SmoothHeight(uv.x,uv.y-d)) * heightScale / 4f;
                float interior = Ease(0, 18, edge);
                height = Mathf.Lerp(sharedHeight,height,interior);
                slope = Mathf.Lerp(Mathf.Atan(Mathf.Sqrt(dx*dx+dz*dz))*Mathf.Rad2Deg,slope,interior);
            }
            float woodland = land.Woodland(point);
            woodland = Mathf.Lerp(woodland, land.GeographyCanopy(point), Ease(0, 20, edge));
            float forest = Ease(.22f, .68f, woodland + irregular);
            float rock = Ease(25, 44, slope + irregular * 27);
            float exposed = Ease(12, 75, height - land.View.SeaLevel);
            float dry = Ease(.29f,.66f,(biome?.dryness ?? 0) * .7f + exposed * .46f + irregular)
                * (1 - forest) * (1 - rock) * .70f;
            float moist = (1 - Ease(7, MoistReach, FreshwaterDistance(uv) + irregular * 35)) * (1 - rock) * .86f;
            float soil = 0;
            if (settlements.TryGetValue(Vector2Int.FloorToInt(point / 32), out var lots))
                foreach (var footprint in lots)
                    soil = Mathf.Max(soil, (1 - Ease(0, 9, Distance(point, footprint))) * .52f);
            soil *= Ease(0, 9, edge);
            forest *= (1 - rock) * (1 - soil) * (1 - moist * .38f);
            float meadow = Mathf.Max(.08f, 1 - Mathf.Max(Mathf.Max(forest, dry), Mathf.Max(moist, Mathf.Max(soil, rock))));
            float inverse = 1f / (meadow + dry + forest + moist + soil + rock);
            primary = new Color(meadow * inverse, dry * inverse, forest * inverse, moist * inverse);
            extra = new Color(soil * inverse, rock * inverse, 0, 0);
        }

        float FreshwaterDistance(Vector2 uv)
        {
            float best = MoistReach;
            foreach (var entry in freshwater)
            {
                if (!entry.Bounds.Contains(uv)) continue;
                var body = entry.Body;
                if (body.centerline is { Length: > 1 })
                {
                    float distance = body.IndexedCenterlineDistance(uv) - body.halfWidthNormalized;
                    best = Mathf.Min(best, distance * land.View.MapSide);
                    continue;
                }
                foreach (var contour in entry.Contours)
                for (int i = 0; i < contour.Length; i++)
                {
                    var start = contour[i];
                    var delta = contour[(i + 1) % contour.Length] - start;
                    float t = Mathf.Clamp01(Vector2.Dot(uv - start, delta) / Mathf.Max(delta.sqrMagnitude, 1e-12f));
                    best = Mathf.Min(best, Vector2.Distance(uv, start + delta * t) * land.View.MapSide);
                }
            }
            return best;
        }

        static float Ease(float low, float high, float value) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(low, high, value));
        static float Distance(Vector2 point, Rect rect) => Vector2.Distance(point,
            new Vector2(Mathf.Clamp(point.x, rect.xMin, rect.xMax), Mathf.Clamp(point.y, rect.yMin, rect.yMax)));

        void OnDestroy()
        {
            if (first) Destroy(first);
            if (second) Destroy(second);
        }
    }
}

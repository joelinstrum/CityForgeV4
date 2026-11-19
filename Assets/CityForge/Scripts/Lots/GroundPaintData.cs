using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CityForge.Geometry;
using UnityEngine;

namespace CityForge.Lots
{
    [Serializable] public sealed class GroundPaintTile
    {
        public int x, z;
        public string[] materials = Array.Empty<string>();
        public string weights;
        [NonSerialized] internal byte[] editing;
        internal Vector2 Minimum => new(x * GroundPaintData.TileMeters, z * GroundPaintData.TileMeters);
        internal byte[] Read()
        {
            if (editing != null) return editing;
            var bytes = new byte[GroundPaintData.ByteCount];
            if (string.IsNullOrEmpty(weights)) return bytes;
            try
            {
                using var input = new MemoryStream(Convert.FromBase64String(weights));
                using var zip = new DeflateStream(input, CompressionMode.Decompress);
                int count = 0, read;
                while (count < bytes.Length && (read = zip.Read(bytes, count, bytes.Length - count)) > 0) count += read;
                if (count != bytes.Length || zip.ReadByte() != -1) throw new InvalidDataException("Invalid paint weight map size.");
                return bytes;
            }
            catch (FormatException error) { throw new InvalidDataException("Invalid paint weights.", error); }
        }
        internal void Flush()
        {
            if (editing == null) return;
            using var stream = new MemoryStream();
            using (var zip = new DeflateStream(stream, System.IO.Compression.CompressionLevel.Fastest, true)) zip.Write(editing, 0, editing.Length);
            weights = Convert.ToBase64String(stream.ToArray());
        }
    }

    [Serializable] public sealed class GroundPaintData
    {
        internal const int Resolution = 128, Layers = 8, MaxTiles = 256;
        internal const float TileMeters = 32f, PixelMeters = TileMeters / Resolution;
        internal const int ByteCount = Resolution * Resolution * Layers;
        public List<GroundPaintTile> tiles = new();
        public void Flush() { foreach (var tile in tiles) tile.Flush(); }
        internal void PruneEmpty() => tiles.RemoveAll(tile => !tile.Read().Any(value => value != 0));
        internal GroundPaintData Copy() { Flush(); return JsonUtility.FromJson<GroundPaintData>(JsonUtility.ToJson(this)); }
        internal static void Validate(GroundPaintData data, int limit = MaxTiles)
        {
            if (data == null) return;
            if (data.tiles == null || data.tiles.Count > limit) throw new InvalidDataException("Too many texture paint tiles.");
            var seen = new HashSet<Vector2Int>();
            foreach (var tile in data.tiles)
            {
                if (tile == null || Math.Abs((long)tile.x) > 65536 || Math.Abs((long)tile.z) > 65536 ||
                    !seen.Add(new Vector2Int(tile.x, tile.z)) || tile.materials == null || tile.materials.Length > Layers ||
                    tile.materials.Distinct().Count() != tile.materials.Length ||
                    tile.materials.Any(id => !LotPathMaterial.All.Any(material => material.Id == id)) ||
                    (tile.weights?.Length ?? 0) > ByteCount * 2) throw new InvalidDataException("Invalid texture paint tile.");
                var bytes = tile.Read();
                for (int pixel = 0; pixel < Resolution * Resolution; pixel++)
                {
                    int sum = 0;
                    for (int layer = 0; layer < Layers; layer++)
                    {
                        int value = bytes[pixel * Layers + layer]; sum += value;
                        if (layer >= tile.materials.Length && value != 0) throw new InvalidDataException("Unknown paint layer.");
                    }
                    if (sum > 255) throw new InvalidDataException("Invalid texture paint coverage.");
                }
            }
        }
        internal string Dab(Vector2 center, float radius, float falloff, string material,
            IReadOnlyList<Vector2> polygon, bool erase, Predicate<Vector2> contains, HashSet<GroundPaintTile> dirty)
        {
            if (!float.IsFinite(center.x) || !float.IsFinite(center.y) || !float.IsFinite(radius) || radius < .25f || radius > 16f ||
                !float.IsFinite(falloff) || falloff < 0 || falloff > 1 || polygon == null || polygon.Count < 3)
                return "Choose a valid boundary and brush size.";
            if (!erase && !LotPathMaterial.All.Any(item => item.Id == material)) return "Choose a path texture.";
            var low = Vector2Int.FloorToInt((center - Vector2.one * radius) / TileMeters);
            var high = Vector2Int.FloorToInt((center + Vector2.one * radius) / TileMeters);
            string problem = null;
            for (int tz = low.y; tz <= high.y; tz++) for (int tx = low.x; tx <= high.x; tx++)
            {
                var tile = tiles.Find(item => item.x == tx && item.z == tz);
                int slot = tile == null ? -1 : Array.IndexOf(tile.materials, material);
                byte[] bytes = null;
                bool changed = false;
                int minX = Mathf.Clamp(Mathf.FloorToInt((center.x - radius - tx * TileMeters) / PixelMeters), 0, Resolution - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt((center.x + radius - tx * TileMeters) / PixelMeters), 0, Resolution - 1);
                int minY = Mathf.Clamp(Mathf.FloorToInt((center.y - radius - tz * TileMeters) / PixelMeters), 0, Resolution - 1);
                int maxY = Mathf.Clamp(Mathf.CeilToInt((center.y + radius - tz * TileMeters) / PixelMeters), 0, Resolution - 1);
                for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
                {
                    var point = new Vector2(tx * TileMeters + (x + .5f) * PixelMeters, tz * TileMeters + (y + .5f) * PixelMeters);
                    float distance = Vector2.Distance(point, center);
                    if (distance >= radius || (contains != null && !contains(point)) || !PointWireGeometry.Contains(polygon, point)) continue;
                    if (tile == null)
                    {
                        if (erase) continue;
                        if (tiles.Count >= MaxTiles) { problem = "This paint surface has reached its area limit."; continue; }
                        tile = new GroundPaintTile { x = tx, z = tz }; tiles.Add(tile);
                    }
                    bytes ??= tile.editing ??= tile.Read();
                    if (!erase && slot < 0)
                    {
                        // Reclaim erased layers before allocating another texture slot.
                        for (int layer = 0; layer < tile.materials.Length && slot < 0; layer++)
                        {
                            bool used = false;
                            for (int pixel = layer; pixel < bytes.Length && !used; pixel += Layers) used = bytes[pixel] != 0;
                            if (!used) { slot = layer; tile.materials[layer] = material; }
                        }
                        if (slot < 0)
                        {
                            if (tile.materials.Length == Layers) { problem = "This area already uses eight textures. Erase one before adding another."; goto NextTile; }
                            slot = tile.materials.Length; tile.materials = tile.materials.Concat(new[] { material }).ToArray();
                        }
                    }
                    float strength = .38f * Mathf.SmoothStep(0, 1, (1 - distance / radius) / Mathf.Max(.02f, falloff));
                    int index = (y * Resolution + x) * Layers;
                    int sum = 0;
                    for (int layer = 0; layer < Layers; layer++)
                    {
                        byte value = (byte)Mathf.FloorToInt(bytes[index + layer] * (1 - strength));
                        changed |= value != bytes[index + layer]; bytes[index + layer] = value; sum += value;
                    }
                    if (!erase)
                    {
                        int addition = Mathf.Min(255 - sum, Mathf.RoundToInt(255 * strength));
                        bytes[index + slot] += (byte)addition; changed |= addition > 0;
                    }
                }
                NextTile:
                if (changed) dirty.Add(tile);
            }
            return problem;
        }
        internal void RotateClockwise()
        {
            foreach (var tile in tiles)
            {
                var source = tile.Read(); var target = new byte[ByteCount];
                for (int y = 0; y < Resolution; y++) for (int x = 0; x < Resolution; x++)
                    Buffer.BlockCopy(source, (y * Resolution + x) * Layers, target, ((Resolution - 1 - x) * Resolution + y) * Layers, Layers);
                (tile.x, tile.z) = (tile.z, -tile.x - 1); tile.editing = target; tile.Flush();
            }
        }
    }
}

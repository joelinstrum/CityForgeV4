using System;
using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    internal sealed class ResidentialGroundBatches : MonoBehaviour
    {
        private const float ChunkSize = 96f;
        private const string TextureFolder = "Lots/GroundTextures/Residential/";
        private readonly Dictionary<(string albedo, string mask), Material> materials = new();
        private readonly Dictionary<(Material material, int x, int z), Batch> batches = new();
        private readonly HashSet<Batch> dirtyBatches = new();

        internal void Add(LotDefinition lot, string id, string era, Transform placement,
            Func<Vector2, float> height, bool showFootprint = false, float lift = .052f)
        {
            if (!ResidentialGround.TrySelect(lot, id, era, out var selection)) return;
            if (selection.Strength <= 0 && !showFootprint) return;
            var variant = selection.Variation;
            var textures = (variant.albedo, variant.mask);
            if (!materials.TryGetValue(textures, out var material))
            {
                var shader = Resources.Load<Shader>("Region/ResidentialGround");
                var albedo = Resources.Load<Texture2D>(TextureFolder + variant.albedo);
                var mask = Resources.Load<Texture2D>(TextureFolder + variant.mask);
                if (shader && albedo && mask)
                {
                    material = new Material(shader) { name = variant.label };
                    material.SetTexture("_Albedo", albedo);
                    material.SetTexture("_Mask", mask);
                }
                materials.Add(textures, material);
            }
            if (!material) return;

            var center = placement.position;
            var key = (material, Mathf.FloorToInt(center.x / ChunkSize), Mathf.FloorToInt(center.z / ChunkSize));
            if (!batches.TryGetValue(key, out var batch))
            {
                batch = new Batch { Material = material };
                batches.Add(key, batch);
            }
            dirtyBatches.Add(batch);
            var area = lot.LandBounds;
            int columns = Mathf.CeilToInt(area.width);
            int rows = Mathf.CeilToInt(area.height);
            int stride = columns + 1;
            float smallLot = 1f - Mathf.InverseLerp(LotDefinition.TileMeters, 4f * LotDefinition.TileMeters,
                Mathf.Min(area.width, area.height));
            for (int z = 0; z <= rows; z++)
            for (int x = 0; x <= columns; x++)
            {
                var uv = new Vector2((float)x / columns, (float)z / rows);
                var point = area.min + Vector2.Scale(uv, area.size);
                var world = placement.TransformPoint(new Vector3(point.x, 0, point.y));
                world.y = height(new Vector2(world.x, world.z)) + lift;
                int index = batch.Vertices.Count;
                batch.Vertices.Add(transform.InverseTransformPoint(world));
                var sample = LotGroundOverlay.QuarterTurn(uv - Vector2.one * .5f, -selection.Turns);
                if (selection.FlipX) sample.x = -sample.x;
                if (selection.FlipY) sample.y = -sample.y;
                batch.SampleUV.Add(sample + Vector2.one * .5f);
                batch.FootprintUV.Add(uv);
                batch.Parameters.Add(new Vector4(selection.Strength, selection.Tint, showFootprint ? 1 : 0, smallLot));
                if (x == columns || z == rows) continue;
                batch.Indices.Add(index);
                batch.Indices.Add(index + stride);
                batch.Indices.Add(index + 1);
                batch.Indices.Add(index + 1);
                batch.Indices.Add(index + stride);
                batch.Indices.Add(index + stride + 1);
            }
        }

        internal void Build()
        {
            foreach (var batch in dirtyBatches)
            {
                if (!batch.Mesh)
                {
                    var view = new GameObject("Residential ground · " + batch.Material.name, typeof(MeshFilter), typeof(MeshRenderer));
                    view.transform.SetParent(transform, false);
                    view.layer = gameObject.layer;
                    batch.Mesh = new Mesh { name = batch.Material.name };
                    batch.Mesh.MarkDynamic();
                    view.GetComponent<MeshFilter>().sharedMesh = batch.Mesh;
                    var renderer = view.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = batch.Material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }

                var mesh = batch.Mesh;
                mesh.indexFormat = batch.Vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(batch.Vertices);
                mesh.SetUVs(0, batch.SampleUV);
                mesh.SetUVs(1, batch.FootprintUV);
                mesh.SetUVs(2, batch.Parameters);
                mesh.SetTriangles(batch.Indices, 0);
                mesh.RecalculateNormals();
            }
            dirtyBatches.Clear();
        }

        private void OnDestroy()
        {
            foreach (var batch in batches.Values)
                if (batch.Mesh) Release(batch.Mesh);
            foreach (var material in materials.Values)
                if (material) Release(material);

            static void Release(UnityEngine.Object asset)
            {
                if (Application.isPlaying) Destroy(asset);
                else DestroyImmediate(asset);
            }
        }

        private sealed class Batch
        {
            public Material Material;
            public Mesh Mesh;
            public readonly List<Vector3> Vertices = new();
            public readonly List<Vector2> SampleUV = new(), FootprintUV = new();
            public readonly List<Vector4> Parameters = new();
            public readonly List<int> Indices = new();
        }
    }
}

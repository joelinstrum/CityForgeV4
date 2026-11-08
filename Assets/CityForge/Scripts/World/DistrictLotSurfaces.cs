using System;
using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    internal sealed class DistrictLotSurfaces : MonoBehaviour
    {
        private readonly List<UnityEngine.Object> assets = new();

        internal void Build(LotDefinition lot, Func<Vector2, float> terrainHeight)
        {
            var bounds = new Rect(-lot.Size * .5f, lot.Size);
            if (!string.IsNullOrEmpty(lot.baseTexture) && LotGroundTexture.Find(false, lot.baseTexture) is { } ground)
            {
                var texture = ground.Texture;
                AddSurface("Lot base", Grid(bounds, p => LotGroundTexture.Repeat(p - bounds.min, lot.baseTextureSpan), .035f),
                    texture, texture ? Color.white : ground.Fallback);
            }

            AddOverlay(lot.overlayTexture, lot.overlayTextureSpan, bounds, bounds.center, 0, 0);
            for (int i = 0; i < lot.overlays.Count; i++)
            {
                var overlay = lot.overlays[i];
                AddOverlay(overlay.texture, overlay.span, overlay.Bounds, overlay.TextureCenter, overlay.rotation, i + 1);
            }

            for (int i = 0; i < lot.paths.Count; i++)
            {
                var path = lot.paths[i];
                var style = LotPathMaterial.Find(path.materialId);
                var texture = style.Texture;
                AddSurface(style.Label + " path", LotPathSurface.BuildMesh(path, style.TileSize, .10f + i * .0005f, Height),
                    texture, texture ? Color.white : style.Fallback);
            }

            float Height(Vector2 point)
            {
                var world = transform.TransformPoint(new Vector3(point.x, 0, point.y));
                return terrainHeight(new Vector2(world.x, world.z)) - transform.position.y;
            }

            void AddOverlay(string id, int span, Rect area, Vector2 center, int rotation, int order)
            {
                var texture = LotGroundTexture.Find(true, id)?.Texture;
                if (!texture) return;
                AddSurface("Lot overlay", Grid(area, p => Vector2.one * .5f +
                    LotGroundTexture.Repeat(LotGroundOverlay.QuarterTurn(p - center, -rotation), span), .065f),
                    texture, Color.white, order);
            }

            Mesh Grid(Rect area, Func<Vector2, Vector2> textureCoordinate, float lift)
            {
                int columns = Mathf.CeilToInt(area.width);
                int rows = Mathf.CeilToInt(area.height);
                int stride = columns + 1;
                var vertices = new Vector3[stride * (rows + 1)];
                var uv = new Vector2[vertices.Length];
                var indices = new int[columns * rows * 6];
                int next = 0;
                for (int z = 0; z <= rows; z++)
                for (int x = 0; x <= columns; x++)
                {
                    var point = area.min + new Vector2(x * area.width / columns, z * area.height / rows);
                    int index = z * stride + x;
                    vertices[index] = new Vector3(point.x, Height(point) + lift, point.y);
                    uv[index] = textureCoordinate(point);
                    if (x == columns || z == rows) continue;
                    indices[next++] = index;
                    indices[next++] = index + stride;
                    indices[next++] = index + 1;
                    indices[next++] = index + 1;
                    indices[next++] = index + stride;
                    indices[next++] = index + stride + 1;
                }
                var mesh = new Mesh { vertices = vertices, uv = uv, triangles = indices };
                mesh.RecalculateNormals();
                return mesh;
            }
        }

        private void AddSurface(string label, Mesh mesh, Texture2D texture, Color color, int overlayOrder = -1)
        {
            var surface = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
            surface.transform.SetParent(transform, false);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .08f);
            if (texture) material.SetTexture("_BaseMap", texture);
            if (overlayOrder >= 0) LotGroundTexture.ConfigureTransparentLayer(material, overlayOrder);
            surface.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            assets.Add(mesh);
            assets.Add(material);
        }

        private void OnDestroy()
        {
            foreach (var asset in assets) Destroy(asset);
        }
    }
}

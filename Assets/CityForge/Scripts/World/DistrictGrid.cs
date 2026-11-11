using System;
using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        internal const float DistrictGridCellMeters = LotDefinition.TileMeters;
        private bool showDistrictGrid;
        private MeshFilter districtGrid;

        private void ToggleDistrictGrid()
        {
            showDistrictGrid = !showDistrictGrid;
            RefreshDistrictGrid();
        }

        private void RefreshDistrictGrid()
        {
            if (SelectedDistrict < 0 || !showDistrictGrid)
            {
                if (districtGrid) districtGrid.gameObject.SetActive(false);
                return;
            }

            if (!districtGrid)
            {
                var root = new GameObject("District construction grid", typeof(MeshFilter), typeof(MeshRenderer));
                root.transform.SetParent(transform, false);
                districtGrid = root.GetComponent<MeshFilter>();
                districtGrid.sharedMesh = Own(new Mesh { name = root.name, indexFormat = IndexFormat.UInt32 });

                var material = Own(new Material(Shader.Find("Universal Render Pipeline/Unlit")));
                material.SetColor("_BaseColor", new Color(.92f, .94f, .85f, .30f));
                LotGroundTexture.ConfigureTransparentLayer(material, 2);
                var renderer = root.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var vertices = new List<Vector3>();
            var indices = new List<int>();
            const float spacing = DistrictGridCellMeters;
            int cells = Mathf.RoundToInt(DistrictSide / spacing);
            var members = ActiveDistrictMembers;
            foreach (int district in members)
            {
                var origin = DistrictOrigin(district);
                bool eastNeighbor = district % GridSide < GridSide - 1 &&
                    Array.IndexOf(members, district + 1) >= 0;
                bool northNeighbor = Array.IndexOf(members, district + GridSide) >= 0;
                for (int line = 0; line <= cells; line++)
                {
                    float offset = line * spacing;
                    if (line < cells || !eastNeighbor) AddLine(origin + Vector2.right * offset, Vector2.up);
                    if (line < cells || !northNeighbor) AddLine(origin + Vector2.up * offset, Vector2.right);
                }
            }

            var mesh = districtGrid.sharedMesh;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            districtGrid.gameObject.SetActive(true);

            void AddLine(Vector2 start, Vector2 direction)
            {
                for (int sample = 0; sample <= cells; sample++)
                {
                    var point = start + direction * (sample * spacing);
                    int index = vertices.Count;
                    vertices.Add(new Vector3(point.x, LotHeight(point) + .24f, point.y));
                    if (sample == 0) continue;
                    indices.Add(index - 1);
                    indices.Add(index);
                }
            }
        }
    }
}

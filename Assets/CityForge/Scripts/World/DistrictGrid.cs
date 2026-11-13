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
        private MeshFilter roadStartTile;
        private Vector2Int? highlightedRoadCell;
        private readonly Vector3[] roadStartTileVertices = new Vector3[9];
        private static readonly int[] RoadStartTileTriangles =
        {
            0, 3, 1, 1, 3, 4, 1, 4, 2, 2, 4, 5,
            3, 6, 4, 4, 6, 7, 4, 7, 5, 5, 7, 8
        };

        private void HighlightRoadStartTile(Vector2? point)
        {
            if (!point.HasValue)
            {
                if (roadStartTile && roadStartTile.gameObject.activeSelf)
                    roadStartTile.gameObject.SetActive(false);
                highlightedRoadCell = null;
                return;
            }

            var cell = DistrictGridCell(point.Value);
            if (highlightedRoadCell == cell) return;
            if (!roadStartTile)
            {
                var root = new GameObject("Road start tile highlight", typeof(MeshFilter), typeof(MeshRenderer));
                root.transform.SetParent(transform, false);
                roadStartTile = root.GetComponent<MeshFilter>();
                roadStartTile.sharedMesh = Own(new Mesh { name = root.name });
                var material = Own(new Material(Shader.Find("Universal Render Pipeline/Unlit")));
                material.SetColor("_BaseColor", new Color(1f, .75f, .28f, .52f));
                LotGroundTexture.ConfigureTransparentLayer(material, LotGroundOverlay.MaxCount + 20);
                var renderer = root.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var corner = ZoneCorner(cell);
            const float inset = .05f;
            const float step = (DistrictGridCellMeters - 2f * inset) * .5f;
            for (int z = 0; z < 3; z++)
            for (int x = 0; x < 3; x++)
            {
                var sample = corner + new Vector2(inset + x * step, inset + z * step);
                roadStartTileVertices[z * 3 + x] = new Vector3(sample.x, LotHeight(sample) + .25f, sample.y);
            }
            var mesh = roadStartTile.sharedMesh;
            mesh.vertices = roadStartTileVertices;
            mesh.triangles = RoadStartTileTriangles;
            roadStartTile.gameObject.SetActive(true);
            highlightedRoadCell = cell;
        }

        private Vector2Int DistrictGridCell(Vector2 point) =>
            Vector2Int.FloorToInt((point + Vector2.one * (MapSide * .5f)) / DistrictGridCellMeters);

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

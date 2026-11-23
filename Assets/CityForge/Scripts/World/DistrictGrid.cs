using System;
using System.Collections.Generic;
using System.Collections;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        internal const float DistrictGridCellMeters = LotDefinition.TileMeters;
        private bool showDistrictGrid;
        private bool districtGridDirty=true,districtGridReady;
        private int districtGridVersion,gridHeightDistrict=-1;
        private Coroutine districtGridJob;
        private readonly Dictionary<Vector2Int,float> districtGridHeights=new();
        internal int DistrictGridBuilds {get;private set;}
        private void InvalidateDistrictGridHeights(Rect? area=null)
        {
            if(area.HasValue)
            {
                var bounds=area.Value;bounds.min-=Vector2.one*.01f;bounds.max+=Vector2.one*.01f;
                var keys=new List<Vector2Int>();foreach(var key in districtGridHeights.Keys)
                    if(bounds.Contains((Vector2)key/1000f))keys.Add(key);
                foreach(var key in keys)districtGridHeights.Remove(key);
            }
            else districtGridHeights.Clear();
            RefreshDistrictGrid();
        }
        private float GridHeight(Vector2 point)
        {
            var key=Vector2Int.RoundToInt(point*1000);
            if(!districtGridHeights.TryGetValue(key,out var height))districtGridHeights[key]=height=LotHeight(point);
            return height;
        }
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
            if(!showDistrictGrid){if(districtGrid)districtGrid.gameObject.SetActive(false);return;}
            if(districtGridReady&&!districtGridDirty&&gridHeightDistrict==SelectedDistrict)
            {districtGrid.gameObject.SetActive(true);return;}
            RefreshDistrictGrid();
        }

        private void RefreshDistrictGrid()
        {
            districtGridDirty=true;districtGridVersion++;
            if(gridHeightDistrict!=SelectedDistrict)
            {
                gridHeightDistrict=SelectedDistrict;districtGridHeights.Clear();districtGridReady=false;
                if(districtGrid)districtGrid.gameObject.SetActive(false);
            }
            if(SelectedDistrict<0){if(districtGrid)districtGrid.gameObject.SetActive(false);return;}
            // Prewarm once after district loading; hidden preview changes only dirty
            // the cache. No terrain sampling or whole-mesh rebuild on a cached toggle.
            if(districtGridJob==null && (showDistrictGrid||!districtGridReady))
                districtGridJob=StartCoroutine(BuildDistrictGridQueue());
        }

        private IEnumerator PrepareDistrictGridStages()
        {
            // Finish first-use work under the district loading overlay, so the first
            // G press is a cached visibility change too, not just subsequent presses.
            if(districtGridJob!=null){StopCoroutine(districtGridJob);districtGridJob=null;}
            if(SelectedDistrict<0 || districtGridReady&&!districtGridDirty)yield break;
            districtGridDirty=false;
            yield return BuildDistrictGridMesh(districtGridVersion);
        }

        private IEnumerator BuildDistrictGridQueue()
        {
            yield return null;
            while(districtGridDirty && SelectedDistrict>=0)
            {
                while(DistrictLoading)yield return null;
                districtGridDirty=false;
                yield return BuildDistrictGridMesh(districtGridVersion);
            }
            districtGridJob=null;
        }

        private IEnumerator BuildDistrictGridMesh(int version)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();
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

            var parcels = CollectGridParcels();
            var buckets = new Dictionary<Vector2Int, List<int>>();
            for (int i = 0; i < parcels.Count; i++)
            {
                var bounds = parcels[i].Bounds;
                var min = DistrictGridCell(bounds.min); var max = DistrictGridCell(bounds.max);
                for (int z = min.y; z <= max.y; z++)
                for (int x = min.x; x <= max.x; x++)
                {
                    var key = new Vector2Int(x, z);
                    if (!buckets.TryGetValue(key, out var list)) buckets.Add(key, list = new List<int>());
                    list.Add(i);
                }
            }
            var intervals = new List<Vector2>();
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
                    if (line < cells || !eastNeighbor){var work=AddLine(origin+Vector2.right*offset,Vector2.up);while(work.MoveNext())yield return work.Current;}
                    if(version!=districtGridVersion)yield break;
                    if (line < cells || !northNeighbor){var work=AddLine(origin+Vector2.up*offset,Vector2.right);while(work.MoveNext())yield return work.Current;}
                    if(version!=districtGridVersion)yield break;
                }
            }

            // Cardinal parcel edges already coincide with surviving lattice edges.
            // Rotated cells need their own edges, in this same grid mesh/material.
            var edges = new HashSet<(Vector2Int, Vector2Int)>();
            foreach (var parcel in parcels)
                if (parcel.Diagonal)
                    for (int side = 0; side < 4; side++)
                    {
                        var a = parcel.Corner(side); var b = parcel.Corner((side + 1) % 4);
                        var ka = Vector2Int.RoundToInt(a * 1000); var kb = Vector2Int.RoundToInt(b * 1000);
                        if (!edges.Add((ka, kb))) continue;
                        edges.Add((kb, ka));
                        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / spacing));
                        for (int step = 0; step < steps; step++)
                            AddSegment(Vector2.Lerp(a, b, (float)step / steps), Vector2.Lerp(a, b, (float)(step + 1) / steps));
                    }

            if(version!=districtGridVersion)yield break;
            var mesh = districtGrid.sharedMesh;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            districtGridReady=true;DistrictGridBuilds++;
            districtGrid.gameObject.SetActive(showDistrictGrid);

            IEnumerator AddLine(Vector2 start, Vector2 direction)
            {
                for (int sample = 0; sample < cells; sample++)
                {
                    if(timer.Elapsed.TotalMilliseconds>=4){yield return null;timer.Restart();if(version!=districtGridVersion)yield break;}
                    var a = start + direction * (sample * spacing);
                    var b = a + direction * spacing;
                    intervals.Clear();
                    if (buckets.TryGetValue(DistrictGridCell((a + b) * .5f), out var nearby))
                        foreach (int index in nearby)
                            if (parcels[index].InteriorInterval(a, b, out var interval)) intervals.Add(interval);
                    intervals.Sort((x, y) => x.x.CompareTo(y.x));
                    float cursor = 0;
                    foreach (var interval in intervals)
                    {
                        if (interval.x > cursor + .00001f) AddSegment(Vector2.Lerp(a, b, cursor), Vector2.Lerp(a, b, interval.x));
                        cursor = Mathf.Max(cursor, interval.y);
                    }
                    if (cursor < .99999f) AddSegment(Vector2.Lerp(a, b, cursor), b);
                }
            }
            void AddSegment(Vector2 a, Vector2 b)
            {
                int index = vertices.Count;
                vertices.Add(new Vector3(a.x, GridHeight(a) + .24f, a.y));
                vertices.Add(new Vector3(b.x, GridHeight(b) + .24f, b.y));
                indices.Add(index); indices.Add(index + 1);
            }
        }
    }
}

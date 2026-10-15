using System.Collections.Generic;
using CityForge.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    /// <summary>Deterministic, lazily built meadow tufts, batched into frustum-culled tiles.</summary>
    public sealed class MeadowGrassRenderer : MonoBehaviour
    {
        private const float TileSide = 32f;
        private sealed class Tile { public Mesh mesh; public GameObject root; public int count; }
        private readonly Dictionary<Vector2Int, Tile> tiles = new();
        private readonly List<Vector2Int> visible = new();
        private readonly List<Vector2Int> evict = new();
        private PrototypeRegionView view;
        private CityForgeCameraController control;
        private Camera camera;
        private Terrain terrain;
        private Material material;
        private Matrix4x4 lastCamera;
        private float lastSize;
        private int lastDistrict = -2;
        private bool lastVisible, pending;
        private float lastDetailMode = -1f;
        private int woodlandRevision = -1;
        public int VisibleTufts { get; private set; }
        public int VisibleBatches { get; private set; }
        public bool IsVisible { get; private set; }

        private void Start()
        {
            view = GetComponent<PrototypeRegionView>();
            control = GetComponentInChildren<CityForgeCameraController>();
            camera = control.GetComponent<Camera>();
            terrain = GetComponentInChildren<Terrain>();
            material = new Material(Resources.Load<Material>("Region/MeadowGrass"));
        }

        private void LateUpdate()
        {
            if (woodlandRevision != view.WoodlandRevision)
            {
                foreach (var tile in tiles.Values) { Destroy(tile.mesh); Destroy(tile.root); }
                tiles.Clear(); pending = true; woodlandRevision = view.WoodlandRevision;
            }
            var detailMode = control.ZoomLevel <= 2 ? 1f : 0f;
            if (detailMode != lastDetailMode)
            {
                Shader.SetGlobalFloat("_RegionMeadowDetailEnabled", detailMode);
                lastDetailMode = detailMode;
            }
            IsVisible = control.ZoomLevel <= 2 && !GetComponent<RegionPreviewDiagnostics>().IsEnabled;
            var pose = camera.transform.localToWorldMatrix;
            if (!pending && pose == lastCamera && camera.orthographicSize == lastSize
                && view.SelectedDistrict == lastDistrict && IsVisible == lastVisible) return;
            lastCamera = pose; lastSize = camera.orthographicSize;
            lastDistrict = view.SelectedDistrict; lastVisible = IsVisible; pending = false;
            VisibleTufts = VisibleBatches = 0;
            foreach (var tile in tiles.Values) tile.root.SetActive(false);
            if (!IsVisible) return;
            visible.Clear();
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var half = view.MapSide * .5f;
            var n = Mathf.CeilToInt(view.MapSide / TileSide);
            // Tile bounds include the full vertical height range: no grass disappears on hills.
            var height = terrain.terrainData.size.y;
            for (var z = 0; z < n; z++) for (var x = 0; x < n; x++)
            {
                var center = new Vector3(-half + (x + .5f) * TileSide, height * .5f, -half + (z + .5f) * TileSide);
                if (view.SelectedDistrict >= 0 && PrototypeRegionView.DistrictAt(center) != view.SelectedDistrict) continue;
                if (GeometryUtility.TestPlanesAABB(planes, new Bounds(center, new Vector3(TileSide, height + 2, TileSide))))
                    visible.Add(new Vector2Int(x, z));
            }
            var built = 0;
            foreach (var key in visible)
            {
                if (!tiles.TryGetValue(key, out var tile))
                {
                    if (built >= 8) { pending = true; continue; } // Spread first-view preparation across frames.
                    tile = BuildTile(key); tiles.Add(key, tile); built++;
                }
                tile.root.SetActive(true);
                VisibleTufts += tile.count;
                if (tile.count > 0) VisibleBatches++;
            }
            // Bound memory while panning; positions regenerate identically on return.
            if (tiles.Count > 512)
            {
                evict.Clear();
                foreach (var pair in tiles)
                    if (!pair.Value.root.activeSelf) evict.Add(pair.Key);
                foreach (var key in evict)
                {
                    if (tiles.Count <= 512) break;
                    Destroy(tiles[key].mesh); Destroy(tiles[key].root); tiles.Remove(key);
                }
            }
        }

        private Tile BuildTile(Vector2Int key)
        {
            var random = new System.Random(unchecked(view.Data.Seed ^ key.x * 73856093 ^ key.y * 19349663));
            float Next() => (float)random.NextDouble();
            var vertices = new List<Vector3>(); var colors = new List<Color>();
            var uv = new List<Vector2>(); var indices = new List<int>();
            var half = view.MapSide * .5f;
            var origin = new Vector3(-half + key.x * TileSide, 0, -half + key.y * TileSide);
            var count = 0;
            // Small clusters, with ample gaps; the ground material does most of the work.
            for (var z = 0; z < 13; z++) for (var x = 0; x < 13; x++)
            {
                var px = origin.x + (x + Next()) * TileSide / 13;
                var pz = origin.z + (z + Next()) * TileSide / 13;
                var u = (px + half) / view.MapSide; var v = (pz + half) / view.MapSide;
                var cover = MeadowCoverFields.Sample(new Vector2(px, pz), view.MapSide);
                var woodland = view.SampleWoodland(new Vector2(px,pz));
                cover.g = Mathf.Max(cover.g, woodland.y * (1f-woodland.x) * .85f);
                var density = (.32f + cover.g * .50f - cover.a * .24f) * (1f-woodland.x*.78f);
                if (u > 1 || v > 1 || Next() > density) continue;
                var ix = Mathf.Clamp(Mathf.RoundToInt(u * 512), 0, 512);
                var iz = Mathf.Clamp(Mathf.RoundToInt(v * 512), 0, 512);
                var h = terrain.SampleHeight(new Vector3(px, 0, pz));
                if (h < view.SeaLevel + .6f || view.Data.WaterDistance[iz, ix] * view.MapScale < 9
                    || terrain.terrainData.GetSteepness(u, v) > 18) continue;
                var baseColor = Color.Lerp(new Color(.20f, .29f, .10f), new Color(.30f, .36f, .14f), Next());
                baseColor = Color.Lerp(baseColor, new Color(.43f, .39f, .21f), cover.b * .8f);
                for (var blade = 0; blade < 5; blade++)
                {
                    var angle = Next() * Mathf.PI * 2;
                    var right = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    var lean = new Vector3(-right.z, 0, right.x) * (.12f + Next() * .20f);
                    var root = new Vector3(px - origin.x, h - .015f, pz - origin.z) + right * (Next() - .5f) * .4f;
                    var bladeHeight = Mathf.Lerp(.18f + Next() * .24f, .50f + Next() * .55f, cover.g);
                    bladeHeight = Mathf.Lerp(bladeHeight, .32f + Next() * .40f, cover.b);
                    var width = .035f + Next() * .045f;
                    var i = vertices.Count;
                    vertices.Add(root - right * width); vertices.Add(root + right * width);
                    vertices.Add(root + Vector3.up * bladeHeight * .55f + lean * .35f + right * width * .45f);
                    vertices.Add(root + Vector3.up * bladeHeight + lean);
                    uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, .55f)); uv.Add(new Vector2(.5f, 1));
                    for (var c = 0; c < 4; c++) colors.Add(baseColor);
                    indices.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
                }
                count++;
            }
            var mesh = new Mesh { name = "Sparse meadow tufts", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            var rootObject = new GameObject("Meadow tile " + key, typeof(MeshFilter), typeof(MeshRenderer));
            rootObject.transform.SetParent(transform, false); rootObject.transform.position = origin;
            rootObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = rootObject.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return new Tile { mesh = mesh, root = rootObject, count = count };
        }

        private void OnDestroy()
        {
            foreach (var tile in tiles.Values) Destroy(tile.mesh);
            if (material != null) Destroy(material);
        }
    }
}

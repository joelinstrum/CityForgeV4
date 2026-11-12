using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.Lots;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

internal static class PlaceableSeasonalTrees
{
    private const string Trees = "Assets/CityForge/Resources/Trees/";
    private const string Poplars = "Assets/CityForge/Art/Environment/MeadowNativeURP/Showcase/Flora/";
    private const string Seasons = "Assets/CityForge/Resources/Seasons/";
    private const string Output = "Assets/CityForge/Resources/Lots/SeasonalTrees";
    private const int TriangleLimit = 20000;

    private static readonly (string Id, string Label, string Summer, string Fall, string Winter, bool Evergreen)[] Sources =
    {
        ("silver-maple", "Silver Maple A", "ForestBroadleaf/SilverMapleA/Summer", "ForestBroadleaf/SilverMapleA/Fall", "ForestBroadleaf/SilverMapleA/Winter", false),
        ("silver-maple-b", "Silver Maple B", "ForestBroadleaf/SilverMapleB/Summer", "ForestBroadleaf/SilverMapleB/Fall", "ForestBroadleaf/SilverMapleB/Winter", false),
        ("oak-pilot-a", "Oak A", "ForestBroadleaf/OakPilotSummer/Summer", "ForestBroadleaf/OakPilotSummer/Fall", "ForestBroadleaf/OakPilotSummer/Winter", false),
        ("oak-pilot-b", "Oak B", "ForestBroadleaf/OakPilotVariantBSummer/Summer", "ForestBroadleaf/OakPilotVariantBSummer/Fall", "ForestBroadleaf/OakPilotVariantBSummer/Winter", false),
        ("siberian-elm-young", "Siberian Elm · Young", "SiberianElm/SiberianElmYoung", null, null, false),
        ("siberian-elm", "Siberian Elm", "SiberianElm/SiberianElmA", null, null, false),
        ("siberian-elm-large", "Siberian Elm · Large", "SiberianElm/SiberianElmLarge", null, null, false),
        ("london-plane-c", "London Plane C", "LondonPlaneC", null, null, false),
        ("london-plane-d", "London Plane D", "LondonPlaneD", null, null, false),
        ("balsam-fir", "Balsam Fir", "MountainEvergreen/BalsamFir", null, null, true),
        ("fraser-fir", "Fraser Fir", "MountainEvergreen/FraserFir", null, null, true),
        ("cilician-fir", "Cilician Fir", "MountainEvergreen/CilicianFir", null, null, true)
    };

    internal static IEnumerable<LotAssetCatalogEntry> CatalogEntries()
    {
        var poplars = Enumerable.Range(1, 8).Select(variant =>
        {
            string name = $"prefab_Baked_{variant:00}_Poplar";
            return (Id: variant == 1 ? "poplar" : $"meadow-poplar-{variant:00}",
                Label: $"Meadow Poplar {variant:00}", Summer: Poplars + name, Fall: Seasons + name + " Fall",
                Winter: Seasons + name + " Winter", Evergreen: false);
        });
        foreach (var source in Sources.Concat(poplars))
        {
            string folder = $"{Output}/{source.Id}";
            var summer = LoadOutput(folder, "Summer");
            var fall = LoadOutput(folder, "Fall");
            var winter = LoadOutput(folder, "Winter");
            if (!summer || !fall || !winter)
            {
                var summerSource = LoadSource(source.Summer);
                var fallSource = source.Fall == null ? summerSource : LoadSource(source.Fall);
                var winterSource = source.Winter == null ? summerSource : LoadSource(source.Winter);
                if (!summerSource || !fallSource || !winterSource) continue;
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
                summer = summer ? summer : Build(folder, "Summer", summerSource, false, false, source.Evergreen);
                fall = fall ? fall : Build(folder, "Fall", fallSource, source.Fall == null, false, source.Evergreen);
                winter = winter ? winter : Build(folder, "Winter", winterSource, false, source.Winter == null, source.Evergreen);
            }

            if (!summer || !fall || !winter) continue;
            var bounds = summer.GetComponent<LODGroup>().size;
            yield return new LotAssetCatalogEntry
            {
                id = source.Id, label = source.Label, category = "Trees",
                group = source.Evergreen ? "Evergreen" : "Deciduous", playerPlaceable = true,
                prefab = summer, autumnPrefab = fall, winterPrefab = winter,
                footprint = Vector2.one * Mathf.Clamp(bounds * .35f, 3f, 8f)
            };
        }
    }

    private static GameObject LoadSource(string path) => AssetDatabase.LoadAssetAtPath<GameObject>((path.StartsWith("Assets/", StringComparison.Ordinal) ? path : Trees + path) + ".prefab");
    private static GameObject LoadOutput(string folder, string season)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{season}.prefab");
        if (!prefab || !prefab.TryGetComponent<LODGroup>(out var group)) return null;
        var lods = group.GetLODs();
        return lods.Length > 0 && lods.All(lod => lod.renderers.Length > 0) ? prefab : null;
    }

    private static GameObject Build(string folder, string season, GameObject source, bool tintFall, bool createWinter, bool evergreen)
    {
        var original = source.GetComponentInChildren<LODGroup>();
        var candidates = original
            ? original.GetLODs().Select(lod => lod.renderers)
            : new[] { source.GetComponentsInChildren<Renderer>() };
        var tiers = candidates.Select(renderers => renderers.OfType<MeshRenderer>().Where(IsTreeMesh).ToArray())
            .Where(renderers => renderers.Length > 0 && renderers.Sum(CountTriangles) < TriangleLimit).ToArray();
        if (tiers.Length == 0) return null;

        var root = new GameObject(source.name + " · " + season);
        try
        {
            var lods = new List<LOD>();
            var seasonalMaterials = new Dictionary<Material, Material>();
            int removedLeaves = 0;
            foreach (var tier in tiers)
            {
                var renderers = new List<Renderer>();
                foreach (var originalRenderer in tier)
                {
                    var mesh = originalRenderer.GetComponent<MeshFilter>().sharedMesh;
                    var materials = originalRenderer.sharedMaterials;
                    if (createWinter && !evergreen)
                    {
                        var slots = Enumerable.Range(0, mesh.subMeshCount)
                            .Where(i => i < materials.Length && IsFoliage(materials[i])).ToArray();
                        if (slots.Length == mesh.subMeshCount)
                        {
                            removedLeaves += CountTriangles(originalRenderer);
                            continue;
                        }
                        if (slots.Length > 0)
                        {
                            mesh = Object.Instantiate(mesh);
                            foreach (int slot in slots)
                            {
                                removedLeaves += (int)mesh.GetIndexCount(slot) / 3;
                                mesh.SetIndices(Array.Empty<int>(), MeshTopology.Triangles, slot, false);
                            }
                            AssetDatabase.CreateAsset(mesh, $"{folder}/{season}-{lods.Count}-{renderers.Count}.asset");
                        }
                    }

                    for (int i = 0; i < materials.Length; i++)
                    {
                        var material = materials[i];
                        bool tint = tintFall && !evergreen && IsFoliage(material);
                        bool frost = createWinter && evergreen && material &&
                            !material.name.Contains("bark", StringComparison.OrdinalIgnoreCase);
                        if (!tint && !frost) continue;
                        if (!seasonalMaterials.TryGetValue(material, out var variant))
                        {
                            variant = new Material(material);
                            string color = variant.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                            if (variant.HasProperty(color))
                                variant.SetColor(color, variant.GetColor(color) * (tint
                                    ? new Color(1.25f, .70f, .32f) : new Color(.82f, .92f, 1.07f)));
                            AssetDatabase.CreateAsset(variant, $"{folder}/{season}-{seasonalMaterials.Count}.mat");
                            seasonalMaterials.Add(material, variant);
                        }
                        materials[i] = variant;
                    }

                    var child = new GameObject(originalRenderer.name, typeof(MeshFilter), typeof(MeshRenderer));
                    child.transform.SetParent(root.transform, false);
                    child.transform.SetPositionAndRotation(originalRenderer.transform.position, originalRenderer.transform.rotation);
                    child.transform.localScale = originalRenderer.transform.lossyScale;
                    child.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = child.GetComponent<MeshRenderer>();
                    renderer.sharedMaterials = materials;
                    renderer.shadowCastingMode = originalRenderer.shadowCastingMode;
                    renderer.receiveShadows = originalRenderer.receiveShadows;
                    renderers.Add(renderer);
                }
                if (renderers.Count > 0) lods.Add(new LOD(0f, renderers.ToArray()));
            }

            if (lods.Count == 0 || (createWinter && !evergreen && removedLeaves == 0)) return null;
            for (int i = 0; i < lods.Count; i++)
            {
                var lod = lods[i];
                lod.screenRelativeTransitionHeight = i == lods.Count - 1 ? .002f :
                    Mathf.Lerp(.14f, .025f, (float)i / Mathf.Max(1, lods.Count - 2));
                lods[i] = lod;
            }
            var group = root.AddComponent<LODGroup>();
            group.SetLODs(lods.ToArray());
            group.RecalculateBounds();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{folder}/{season}.prefab");
            Debug.Log($"Placeable tree: {folder}/{season} · {string.Join(", ", lods.Select(lod => lod.renderers.Cast<MeshRenderer>().Sum(CountTriangles)))} triangles per LOD");
            return prefab;
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static bool IsTreeMesh(MeshRenderer renderer) =>
        renderer && renderer.GetComponent<MeshFilter>() is { sharedMesh: { } mesh } &&
        mesh && CountTriangles(renderer) > 8 &&
        !renderer.name.Contains("billboard", StringComparison.OrdinalIgnoreCase) &&
        !renderer.name.Contains("impostor", StringComparison.OrdinalIgnoreCase) &&
        !renderer.name.Contains("_cross_", StringComparison.OrdinalIgnoreCase);

    private static int CountTriangles(MeshRenderer renderer)
    {
        var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
        return Enumerable.Range(0, mesh.subMeshCount).Sum(i => (int)mesh.GetIndexCount(i) / 3);
    }

    private static bool IsFoliage(Material material)
    {
        if (!material) return false;
        string name = material.name.ToLowerInvariant();
        return name.Contains("leaf") || name.Contains("leaves") || name.Contains("foliage") || name.Contains("needles");
    }
}

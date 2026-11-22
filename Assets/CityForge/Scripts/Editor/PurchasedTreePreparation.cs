using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

internal static class PurchasedTreePreparation
{
    private static bool Leaf(Material material) => material &&
        (material.name.IndexOf("leaves", StringComparison.OrdinalIgnoreCase) >= 0 ||
         material.name.IndexOf("leaf", StringComparison.OrdinalIgnoreCase) >= 0);
    private static Mesh MeshOf(Renderer renderer) => renderer is SkinnedMeshRenderer skinned
        ? skinned.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;

    [MenuItem("CityForge/Import/Prepare purchased animated trees")]
    internal static void Prepare()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave Play before preparing tree assets.");
        PrepareMaterials();
        string source = PurchasedTreeCatalog.Root + "/Source/Prefabs";
        if (!Directory.Exists(source)) throw new InvalidOperationException("Purchased tree source prefabs are missing.");
        foreach (var path in Directory.GetFiles(source + "/Animated", "*.prefab", SearchOption.AllDirectories).OrderBy(p => p))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string id = PurchasedTreeCatalog.Id(name);
            bool deciduous = name.StartsWith("Maple") || name.StartsWith("Oak") || name.StartsWith("Birch") ||
                name.StartsWith("Beech") || name.StartsWith("Sycamore");
            var staticSource = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace("/Animated/", "/Static/"));
            if (!staticSource) throw new InvalidOperationException("Missing static source: " + name);
            var reference = Object.Instantiate(staticSource);
            var referenceLods = reference.GetComponentInChildren<LODGroup>().GetLODs();
            var ground = referenceLods[0].renderers.Min(r => r.bounds.min.y);
            Object.DestroyImmediate(reference);
            foreach (string season in new[] { "Summer", "Fall", "Winter" })
            foreach (bool animated in new[] { true, false })
            {
                string folder = PurchasedTreeCatalog.Prepared + "/" + id + (animated ? "" : "/Static");
                if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); AssetDatabase.Refresh(); }
                string destination = folder + "/" + season + ".prefab";
                var original = AssetDatabase.LoadAssetAtPath<GameObject>(animated ? path : path.Replace("/Animated/", "/Static/"));
                var root = new GameObject(name + " · " + season);
                try
                {
                    var model = Object.Instantiate(original, root.transform);
                    model.name = name;
                    model.transform.localPosition = (model.transform.localPosition - Vector3.up * ground) * .6f;
                    model.transform.localScale *= .6f;
                    foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                    foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                    {
                        animator.applyRootMotion = false;
                        animator.cullingMode = AnimatorCullingMode.CullCompletely;
                    }
                    var sourceGroup = root.GetComponentInChildren<LODGroup>();
                    var lods = sourceGroup.GetLODs();
                    var keep = new List<LOD>();
                    var changedMaterials = new Dictionary<Material, Material>();
                    for (int tier = 0; tier < lods.Length; tier++)
                    {
                        var renderers = new List<Renderer>();
                        foreach (var renderer in lods[tier].renderers)
                        {
                            var mesh = MeshOf(renderer);
                            // Authored cross-card billboards contain summer foliage. Retain the reduced
                            // 3D tiers instead, including for winter, so crowns and shadows stay coherent.
                            if (!mesh || renderer.sharedMaterials.All(m => m && m.name.IndexOf("billboard", StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                renderer.enabled = false;
                                continue;
                            }
                            var materials = renderer.sharedMaterials;
                            if (deciduous && season == "Winter")
                            {
                                var leaves = Enumerable.Range(0, mesh.subMeshCount)
                                    .Where(slot => slot < materials.Length && Leaf(materials[slot])).ToArray();
                                if (leaves.Length == mesh.subMeshCount) { renderer.enabled = false; continue; }
                                if (leaves.Length > 0)
                                {
                                    var bare = Object.Instantiate(mesh); bare.name = name + " winter LOD" + tier;
                                    foreach (int slot in leaves) bare.SetIndices(Array.Empty<int>(), MeshTopology.Triangles, slot, false);
                                    string meshPath = folder + "/Winter-LOD" + tier + "-" + renderers.Count + ".asset";
                                    Store(bare, meshPath);
                                    var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                                    if (renderer is SkinnedMeshRenderer skinned) skinned.sharedMesh = saved;
                                    else renderer.GetComponent<MeshFilter>().sharedMesh = saved;
                                }
                            }
                            if (deciduous && season == "Fall")
                            {
                                for (int slot = 0; slot < materials.Length; slot++)
                                {
                                    var material = materials[slot];
                                    if (!Leaf(material)) continue;
                                    if (!changedMaterials.TryGetValue(material, out var autumn))
                                    {
                                        autumn = new Material(material) { name = material.name + " Autumn" };
                                        autumn.SetColor("_BaseColor", name.StartsWith("Maple") ? new Color(1f,.48f,.14f) : new Color(.9f,.62f,.25f));
                                        string matPath = folder + "/Fall-" + changedMaterials.Count + ".mat";
                                        Store(autumn, matPath); autumn = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                                        changedMaterials[material] = autumn;
                                    }
                                    materials[slot] = autumn;
                                }
                                renderer.sharedMaterials = materials;
                            }
                            renderer.shadowCastingMode = ShadowCastingMode.On;
                            renderer.receiveShadows = true;
                            renderers.Add(renderer);
                        }
                        if (renderers.Count > 0) keep.Add(new LOD(0, renderers.ToArray()));
                    }
                    if (keep.Count < 2) throw new InvalidOperationException("No reduced 3D tier: " + name + " / " + season);
                    for (int tier = 0; tier < keep.Count; tier++)
                    {
                        var level = keep[tier];
                        level.screenRelativeTransitionHeight = tier == 0 ? .20f : tier == keep.Count - 1 ? .001f : .06f;
                        keep[tier] = level;
                    }
                    Object.DestroyImmediate(sourceGroup);
                    var group = root.AddComponent<LODGroup>(); group.SetLODs(keep.ToArray()); group.RecalculateBounds();
                    PrefabUtility.SaveAsPrefabAsset(root, destination);
                }
                finally { Object.DestroyImmediate(root); }
            }
        }
        AssetDatabase.SaveAssets();
        typeof(LotCatalogSetup).GetMethod("EnsureCatalog", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
        Debug.Log("PURCHASED TREE PREPARATION COMPLETE: 30 animated/static seasonal tree sets.");
    }
    internal static void PrepareMaterials()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { PurchasedTreeCatalog.Root }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (!material) continue;
            string path = AssetDatabase.GetAssetPath(material);
            // Persist the green-leaf palette in shared assets. Autumn and dry leaves
            // retain their authored colors; bark and approved native forests are untouched.
            bool green = Leaf(material) && path.Contains("/Source/") &&
                !material.name.Contains("maple_leaves_1") && !material.name.ToLowerInvariant().Contains("dry");
            if (green && material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", new Color(.61f, .58f, .55f, 1f));
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();
    }
    private static void Store<T>(T asset, string path) where T : Object
    {
        var previous = AssetDatabase.LoadAssetAtPath<T>(path);
        if (previous) { EditorUtility.CopySerialized(asset, previous); Object.DestroyImmediate(asset); EditorUtility.SetDirty(previous); }
        else AssetDatabase.CreateAsset(asset, path);
    }
}

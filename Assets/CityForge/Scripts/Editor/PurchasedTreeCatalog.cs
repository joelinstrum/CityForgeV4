using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.Lots;
using UnityEditor;
using UnityEngine;

internal static class PurchasedTreeCatalog
{
    internal const string Root = "Assets/CityForge/Resources/Lots/Trees/PurchasedAnimatedTrees";
    internal const string Prepared = Root + "/Prepared";
    internal static string Id(string name) => name switch
    {
        "Maple 1" => "silver-maple", "Maple 2" => "silver-maple-b",
        "Oak 1" => "oak-pilot-a", "Oak 2" => "oak-pilot-b",
        _ => "animated-tree-" + name.ToLowerInvariant().Replace(' ', '-')
    };
    internal static bool HasReplacement(string id) =>
        id is "silver-maple" or "silver-maple-b" &&
        AssetDatabase.LoadAssetAtPath<GameObject>($"{Prepared}/{id}/Summer.prefab");
    internal static IEnumerable<LotAssetCatalogEntry> CatalogEntries()
    {
        if (!Directory.Exists(Prepared)) yield break;
        var labels = Directory.GetFiles(Root + "/Source/Prefabs/Animated", "*.prefab", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension).ToDictionary(Id, name => name, StringComparer.Ordinal);
        foreach (var folder in Directory.GetDirectories(Prepared).OrderBy(path => path, StringComparer.Ordinal))
        {
            // Restore the previous oak assets under their existing saved IDs.
            if (Path.GetFileName(folder) is "oak-pilot-a" or "oak-pilot-b") continue;
            var summer = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/Summer.prefab");
            var fall = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/Fall.prefab");
            var winter = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/Winter.prefab");
            if (!summer || !fall || !winter) continue;
            string label = labels[Path.GetFileName(folder)];
            string group = label.StartsWith("Acacia") ? "Tropical" :
                label.StartsWith("Pine") || label.StartsWith("Fir") || label.StartsWith("Juniper") || label.StartsWith("Spruce") ? "Evergreen" :
                label.StartsWith("Dry Tree") ? "Deadwood" : "Deciduous";
            yield return new LotAssetCatalogEntry
            {
                id = Path.GetFileName(folder), label = label, category = "Trees", group = group,
                prefab = summer, autumnPrefab = fall, winterPrefab = winter,
                playerPlaceable = !LotAssetCatalog.IsRetiredTree(Path.GetFileName(folder)), preserveTreeMaterials = true,
                footprint = Vector2.one * Mathf.Clamp(summer.GetComponent<LODGroup>().size * summer.transform.localScale.x * .35f, 3, 8)
            };
        }
    }
}

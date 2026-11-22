#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.Lots;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace CityForge.EditorTools
{
    /// <summary>Prepared private plane-tree pack. Source geometry stays outside Assets.</summary>
    internal static class PlaneTreePreparation
    {
        internal const string Root = "Assets/CityForge/Resources/Lots/Trees/PlanePack/";
        [MenuItem("CityForge/Import/Prepare Plane Tree Pack")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave Play Mode before preparing tree assets.");
            AssetDatabase.Refresh();
            var bark = Material("Bark", false, false);
            var leaves = Material("Leaves", true, false);
            var fall = Material("Leaves-Fall", true, true);
            for (int i = 1; i <= 6; i++)
            foreach (string season in new[] { "Summer", "Fall", "Winter" })
            {
                string folder = Root + $"Plane{i:00}/";
                var root = new GameObject($"Plane Tree {i:00} · {season}");
                try
                {
                    var levels = new List<LOD>();
                    foreach (string file in new[] { "Near.fbx", "Distant.fbx" })
                    {
                        string path = folder + file;
                        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                        if (importer && (importer.materialImportMode != ModelImporterMaterialImportMode.None || !importer.isReadable))
                        {
                            importer.materialImportMode = ModelImporterMaterialImportMode.None;
                            importer.isReadable = true;
                            importer.SaveAndReimport();
                        }
                        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (!source) throw new InvalidOperationException("Missing prepared tree: " + path);
                        var model = Object.Instantiate(source, root.transform);
                        model.name = Path.GetFileNameWithoutExtension(file);
                        var renderers = new List<Renderer>();
                        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                        {
                            bool leaf = renderer.name.StartsWith("Leaves", StringComparison.Ordinal);
                            if (leaf && season == "Winter") { Object.DestroyImmediate(renderer.gameObject); continue; }
                            renderer.sharedMaterials = Enumerable.Repeat(leaf ? season == "Fall" ? fall : leaves : bark, renderer.sharedMaterials.Length).ToArray();
                            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                            renderers.Add(renderer);
                        }
                        levels.Add(new LOD(levels.Count == 0 ? .12f : .001f, renderers.ToArray()));
                    }
                    var lod = root.AddComponent<LODGroup>();
                    lod.SetLODs(levels.ToArray()); lod.RecalculateBounds();
                    PrefabUtility.SaveAsPrefabAsset(root, folder + season + ".prefab");
                }
                finally { Object.DestroyImmediate(root); }
            }
            AssetDatabase.SaveAssets();
            typeof(LotCatalogSetup).GetMethod("EnsureCatalog", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
            Debug.Log("PLANE TREE PACK PREPARED");
        }
        static Material Material(string name, bool leaf, bool fall)
        {
            string color = Root + (leaf ? "Leaves.png" : "Bark.png");
            if (AssetImporter.GetAtPath(color) is TextureImporter importer)
            {
                importer.alphaIsTransparency = leaf; importer.mipmapEnabled = true;
                importer.mipMapsPreserveCoverage = leaf; importer.alphaTestReferenceValue = .4f;
                importer.maxTextureSize = 2048; importer.SaveAndReimport();
            }
            string path = Root + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(color));
            // Saved asset tint, independent of camera effects. Summer retains the pack's muted green.
            mat.SetColor("_BaseColor", fall ? new Color(.85f,.58f,.34f) : Color.white);
            mat.SetFloat("_Smoothness", .1f); mat.SetFloat("_Metallic", 0);
            mat.SetFloat("_Cull", leaf ? 0 : 2); mat.enableInstancing = true;
            if (leaf)
            {
                string normal = Root + "Leaves-Normal.png";
                if (AssetImporter.GetAtPath(normal) is TextureImporter ni && ni.textureType != TextureImporterType.NormalMap)
                { ni.textureType = TextureImporterType.NormalMap; ni.SaveAndReimport(); }
                mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                mat.SetFloat("_BumpScale", .5f); mat.EnableKeyword("_NORMALMAP");
                mat.SetFloat("_AlphaClip", 1); mat.SetFloat("_Cutoff", .4f);
                mat.EnableKeyword("_ALPHATEST_ON"); mat.renderQueue = 2450; mat.doubleSidedGI = true;
            }
            EditorUtility.SetDirty(mat); return mat;
        }
        internal static IEnumerable<LotAssetCatalogEntry> CatalogEntries()
        {
            for (int i = 1; i <= 6; i++)
            {
                string folder = Root + $"Plane{i:00}/";
                var summer = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "Summer.prefab");
                if (!summer) continue;
                yield return new LotAssetCatalogEntry
                {
                    id = $"plane-pack-{i:00}", label = $"Plane Tree {i:00}", category = "Trees", group = "Deciduous",
                    prefab = summer, autumnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "Fall.prefab"),
                    winterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "Winter.prefab"),
                    playerPlaceable = true, preserveTreeMaterials = true, footprint = Vector2.one * 5
                };
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CityForge.Lots;

namespace CityForge.EditorTools
{
    // Explicit masonry mappings. Never infer wall regions from body atlases or material names.
    internal static class ArtDecoWallPaintSetup
    {
        const string Buildings = "Assets/CityForge/Resources/Lots/RebuiltBuildings/";
        const string Limestone = "Assets/CityForge/PreparedComponents/LimestoneTownhouseDetails/";

        [MenuItem("CityForge/Buildings/Install Art Deco Wall Paint Mappings")]
        internal static void Install()
        {
            foreach (string tier in new[] { "Near", "Medium" })
            {
                Apply("LimestoneTownhouse", tier, "Masonry walls",
                    Limestone + "LTH-Wall-Aged__Shared.mat", Limestone + "LTH-CanonicalBrickSkin__Shared.mat");
                Apply("SteppedBrickTownhouse01", tier, "Brick walls", Local("SteppedBrickTownhouse01", "CFW_e8638725059f12.mat"));
                Apply("MetroBank", tier, "Masonry walls", Local("MetroBank", tier + "-MetroBank-Brick__" + tier + ".mat"),
                    Local("MetroBank", tier + "-MetroBank-Limestone__" + tier + ".mat"));
                Apply("DryGoods", tier, "Stone walls", Local("DryGoods", tier + "-DryGoods-Limestone__" + tier + ".mat"));
                Apply("CartierCorner", tier, "Stone walls", Local("CartierCorner", tier + "-CartierCorner-Limestone__" + tier + ".mat"));
                Apply("GothicTheater", tier, "Stone walls", Local("GothicTheater", tier + "-CF_Detail_Limestone.mat"),
                    Local("GothicTheater", tier + "-CF_Detail_Shadow_stone.mat"));
            }
            AssetDatabase.SaveAssets();
        }

        static string Local(string folder, string material) => Buildings + folder + "/" + material;
        static void Apply(string folder, string tier, string label, params string[] paths)
        {
            string path = Buildings + folder + "/" + tier + ".prefab";
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(path))
                throw new InvalidOperationException("Install the prepared building before adding Wall Paint: " + path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                var walls = paths.Select(materialPath =>
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (!material || !material.HasProperty("_BaseMap") || !material.HasProperty("_BaseColor") ||
                        material.IsKeywordEnabled("_EMISSION") ||
                        material.HasProperty("_Surface") && material.GetFloat("_Surface") >= .5f ||
                        material.GetColor("_BaseColor").a < .999f ||
                        !renderers.Any(r => r.sharedMaterials.Contains(material)))
                        throw new InvalidOperationException("Wall Paint material is missing, unsafe, or unused: " + materialPath);
                    var albedo = material.GetTexture("_BaseMap") as Texture2D;
                    if (!albedo) throw new InvalidOperationException("Missing wall albedo: " + materialPath);
                    return new BuildingTintBinding { region = "walls", material = material, neutralAlbedo = albedo,
                        finishes = root.GetComponent<BuildingTintProfile>()?.bindings?
                            .FirstOrDefault(b => b != null && b.region == "walls" && b.material == material)?.finishes
                            ?? Array.Empty<BuildingWallFinish>() };
                }).ToArray();
                var profiles = root.GetComponentsInChildren<BuildingTintProfile>(true);
                if (profiles.Length > 1 || profiles.Length == 1 && profiles[0].gameObject != root)
                    throw new InvalidOperationException("Review nested paint profiles before updating: " + path);
                var profile = root.GetComponent<BuildingTintProfile>() ?? root.AddComponent<BuildingTintProfile>();
                profile.wallLabel = label;
                profile.bindings = (profile.bindings ?? Array.Empty<BuildingTintBinding>())
                    .Where(b => b != null && b.region != "walls").Concat(walls).ToArray();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
#endif

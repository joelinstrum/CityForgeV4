using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CityForge.Lots
{
    [Serializable]
    public sealed class LotCustomization
    {
        public int schema = 1;
        public List<LotObjectDefinition> objects = new List<LotObjectDefinition>();
    }

    public static class LotCustomizationService
    {
        public static bool IsCosmetic(LotAssetCatalog catalog, string id)
        {
            var asset = catalog ? catalog.Find(id) : null;
            return asset != null && asset.playerPlaceable && asset.category != "Buildings";
        }

        public static LotDefinition Apply(LotDefinition definition, LotCustomization customization, LotAssetCatalog catalog)
        {
            var result = LotDefinition.Parse(JsonUtility.ToJson(definition));
            if (customization == null) return result;
            if (customization.schema != 1 || customization.objects == null || customization.objects.Count > 512)
                throw new InvalidDataException("Invalid landscaping customization.");
            var authored = new Dictionary<string, LotObjectDefinition>();
            foreach (var item in result.objects) authored.Add(item.id, item);
            result.objects.RemoveAll(item => IsCosmetic(catalog, item.assetId));
            foreach (var item in customization.objects)
            {
                if (item == null || !IsCosmetic(catalog, item.assetId) ||
                    authored.TryGetValue(item.id ?? "", out var original) && original.assetId != item.assetId)
                    throw new InvalidDataException("This lot object is protected.");
                result.objects.Add(item);
            }
            result = LotDefinition.Parse(JsonUtility.ToJson(result));
            foreach (var item in result.objects)
            {
                var asset = catalog ? catalog.Find(item.assetId) : null;
                if (asset != null && !LotRuntimeLoader.Fits(result, item, asset))
                    throw new InvalidDataException("A lot object lies outside the lot.");
            }
            return result;
        }

        public static LotCustomization CreateOverride(LotDefinition original, LotDefinition working, LotAssetCatalog catalog)
        {
            var changes = new LotCustomization();
            foreach (var item in working.objects)
                if (IsCosmetic(catalog, item.assetId)) changes.objects.Add(item);
            var permitted = Apply(original, changes, catalog);
            // Compare protected data and objects without relying on cosmetic draw order.
            working = LotDefinition.Parse(JsonUtility.ToJson(working));
            working.objects.RemoveAll(item => IsCosmetic(catalog, item.assetId));
            permitted.objects.RemoveAll(item => IsCosmetic(catalog, item.assetId));
            if (JsonUtility.ToJson(working) != JsonUtility.ToJson(permitted))
                throw new InvalidDataException("Protected lot properties cannot be changed.");
            return changes;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.UI;
using CityForge.World;
using UnityEngine;

namespace CityForge.Lots
{
    [Serializable]
    internal sealed class LotBuildingProperties
    {
        public string id, name, description, firstEra, category, subcategory;
        public TownZone zone;
        public LotWealth wealth;
        public float defaultScale = 1;
        public int firstYear, lastYear;

        internal static LotBuildingProperties From(LotAssetCatalogEntry asset) => new()
        {
            id = asset.id, name = asset.label, description = asset.description ?? "", firstEra = asset.eraId,
            category = LotClassification.FindCategory(asset.group)?.id ?? asset.group,
            subcategory = asset.subcategory ?? "", zone = asset.growableZone, wealth = asset.growableWealth,
            defaultScale = asset.DefaultPlacementScale, firstYear = asset.fromYear, lastYear = asset.toYear
        };
        internal LotBuildingProperties Copy() => JsonUtility.FromJson<LotBuildingProperties>(JsonUtility.ToJson(this));
        internal string Problem()
        {
            if (string.IsNullOrWhiteSpace(id)) return "The building reference is missing.";
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100 || name.Any(char.IsControl)) return "Enter a building name of 1–100 characters.";
            if ((description ?? "").Length > 600) return "Keep the description within 600 characters.";
            if (CityForgeTimeline.Find(firstEra) == null) return "Choose a first era.";
            if (LotClassification.FindCategory(category) == null) return "Choose a building category.";
            if (!string.IsNullOrEmpty(subcategory) && !LotClassification.ForCategory(category).Any(s => s.id == subcategory)) return "Choose a use within this category.";
            if (!Zones(category).Contains(zone)) return "Choose a growable type that matches the category.";
            if (!Enum.IsDefined(typeof(LotWealth), wealth)) return "Choose a wealth category.";
            if (zone != TownZone.None && wealth == LotWealth.None) return "Growable buildings need a wealth category.";
            if (!ResidentialWealthPolicy.Allows(zone, wealth, firstEra)) return "Low-wealth R3 buildings are unavailable in this era.";
            if (!float.IsFinite(defaultScale) || defaultScale < .1f || defaultScale > 5) return "Default scale must be between 10% and 500%.";
            return null;
        }
        internal static TownZone[] Zones(string category) => category switch
        {
            "residential" => new[] { TownZone.None, TownZone.Residential1, TownZone.Residential2, TownZone.Residential3 },
            "commercial" => new[] { TownZone.None, TownZone.Commercial1, TownZone.Commercial2, TownZone.Commercial3 },
            "industrial" => new[] { TownZone.None, TownZone.Industrial1, TownZone.Industrial2, TownZone.Industrial3 },
            _ => new[] { TownZone.None }
        };
        internal void Apply(LotAssetCatalogEntry asset)
        {
            asset.label = name; asset.description = description ?? ""; asset.eraId = firstEra;
            asset.group = category; asset.subcategory = subcategory; asset.growableZone = zone;
            asset.growableWealth = wealth; asset.defaultBuildingScale = defaultScale;
            asset.fromYear = firstYear; asset.toYear = lastYear;
            var era = CityForgeTimeline.Find(firstEra);
            if (era != null) { asset.fromYear = era.FirstYear; asset.toYear = era.LastYear; }
        }
    }

    internal static class LotBuildingPropertiesStore
    {
        [Serializable] private sealed class Document { public int version = 1; public LotBuildingProperties[] buildings = Array.Empty<LotBuildingProperties>(); }
        // Injected only by isolated QA; normal sessions use the application data directory.
        internal static string DirectoryOverride;
        private static string loadedPath, readProblem;
        private static Dictionary<string, LotBuildingProperties> saved = new(StringComparer.Ordinal);
        private static readonly Dictionary<LotAssetCatalogEntry, LotBuildingProperties> originals = new();
        private static LotAssetCatalog appliedCatalog;
        private static LotAssetCatalogEntry[] appliedEntries;
        private static int revision, appliedRevision = -1;
        private static string PathName => Path.Combine(DirectoryOverride ?? Application.persistentDataPath, "BuildingProperties", "catalog.json");
        private static void Load()
        {
            if (loadedPath == PathName) return;
            loadedPath = PathName; saved = new(StringComparer.Ordinal); readProblem = null; revision++;
            try
            {
                if (!File.Exists(loadedPath)) return;
                var doc = JsonUtility.FromJson<Document>(File.ReadAllText(loadedPath));
                if (doc == null || doc.version != 1 || doc.buildings == null) throw new ArgumentException("Unsupported building properties file.");
                foreach (var record in doc.buildings)
                    if (record != null && !string.IsNullOrEmpty(record.id)) saved[record.id] = record;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { readProblem = "Existing building properties could not be read. " + e.Message; Debug.LogWarning(readProblem); }
        }
        internal static void Apply(LotAssetCatalog catalog)
        {
            Load();
            if (catalog == appliedCatalog && ReferenceEquals(catalog.entries, appliedEntries) && appliedRevision == revision) return;
            foreach (var asset in catalog.entries ?? Array.Empty<LotAssetCatalogEntry>())
            {
                if (asset == null || asset.category != "Buildings") continue;
                if (!originals.ContainsKey(asset)) originals[asset] = LotBuildingProperties.From(asset);
                originals[asset].Apply(asset);
                if (saved.TryGetValue(asset.id, out var record) && record.Problem() == null) record.Apply(asset);
            }
            appliedCatalog = catalog; appliedEntries = catalog.entries; appliedRevision = revision;
        }
        internal static LotBuildingProperties Original(LotAssetCatalogEntry asset)
        {
            if (!originals.TryGetValue(asset, out var value)) originals[asset] = value = LotBuildingProperties.From(asset);
            return value.Copy();
        }
        internal static void Save(LotAssetCatalog catalog, LotBuildingProperties record)
        {
            Load();
            if (record.Problem() is { } problem) throw new ArgumentException(problem);
            if (readProblem != null) throw new IOException(readProblem);
            var asset = catalog.Find(record.id);
            if (asset == null || asset.category != "Buildings") throw new ArgumentException("This building is no longer in the catalog.");
            var next = new Dictionary<string, LotBuildingProperties>(saved, StringComparer.Ordinal) { [record.id] = record.Copy() };
            Write(next); saved = next; revision++; Apply(catalog);
        }
        internal static void Reset(LotAssetCatalog catalog, string id)
        {
            Load(); if (readProblem != null) throw new IOException(readProblem);
            var next = new Dictionary<string, LotBuildingProperties>(saved, StringComparer.Ordinal); next.Remove(id);
            Write(next); saved = next; revision++; Apply(catalog);
        }
        private static void Write(Dictionary<string, LotBuildingProperties> records)
        {
            var path = PathName; Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(new Document { buildings = records.Values.OrderBy(b => b.id, StringComparer.Ordinal).ToArray() }, true));
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
    }
}

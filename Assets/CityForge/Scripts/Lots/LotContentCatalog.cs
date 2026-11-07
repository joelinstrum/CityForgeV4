using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.UI;
using UnityEngine;

namespace CityForge.Lots
{
    [Serializable]
    internal sealed class LotContentCatalog
    {
        [Serializable]
        internal sealed class Subcategory
        {
            public string id, name, legacyName;
        }

        [Serializable]
        internal sealed class Category
        {
            public string id, name, legacyName, categoryType;
            public Subcategory[] subcategories;
        }

        [Serializable]
        internal sealed class CategoryType
        {
            public string id;
            public Condition[] conditions;
        }

        [Serializable]
        internal sealed class Condition
        {
            public string type, key, value;
        }

        public int schema;
        public CityForgeTimeline.Era[] eras;
        public Category[] categories;
        public CategoryType[] categoryTypes;
        private static LotContentCatalog current;

        public static LotContentCatalog Current => current ??= Load(
            Resources.Load<TextAsset>("Config/ContentCatalog").text,
            Path.Combine(Application.persistentDataPath, "Mods", "ContentCatalog"));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => current = null;

        internal static LotContentCatalog Load(string builtIn, string modFolder)
        {
            var result = Merge(new LotContentCatalog(), Parse(builtIn));
            try
            {
                if (!Directory.Exists(modFolder)) return result;
                foreach (var file in Directory.GetFiles(modFolder, "*.json").OrderBy(path => path, StringComparer.Ordinal))
                {
                    try { result = Merge(result, Parse(File.ReadAllText(file))); }
                    catch (Exception error) when (error is IOException || error is InvalidDataException ||
                        error is UnauthorizedAccessException || error is ArgumentException)
                    {
                        Debug.LogWarning($"Content catalog ignored {file}: {error.Message}");
                    }
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Could not read content catalog mods: {error.Message}");
            }
            return result;
        }

        private static LotContentCatalog Parse(string json)
        {
            var document = JsonUtility.FromJson<LotContentCatalog>(json);
            if (document == null || document.schema != 1) throw new InvalidDataException("Expected content catalog schema 1.");
            return document;
        }

        private static LotContentCatalog Merge(LotContentCatalog source, LotContentCatalog patch)
        {
            var merged = new LotContentCatalog
            {
                schema = 1,
                eras = MergeById(source.eras, patch.eras, item => item.id, (a, b) => new CityForgeTimeline.Era
                {
                    id = a.id, name = b.name ?? a.name, periods = b.periods ?? a.periods
                }),
                categories = MergeById(source.categories, patch.categories, item => item.id, (a, b) => new Category
                {
                    id = a.id, name = b.name ?? a.name, legacyName = b.legacyName ?? a.legacyName,
                    categoryType = b.categoryType ?? a.categoryType,
                    subcategories = MergeById(a.subcategories, b.subcategories, item => item.id, (old, edit) =>
                        new Subcategory { id = old.id, name = edit.name ?? old.name, legacyName = edit.legacyName ?? old.legacyName })
                }),
                categoryTypes = MergeById(source.categoryTypes, patch.categoryTypes, item => item.id, (a, b) =>
                    new CategoryType { id = a.id, conditions = b.conditions ?? a.conditions })
            };
            Validate(merged);
            merged.eras = merged.eras.OrderBy(era => era.FirstYear).ToArray();
            for (int i = 1; i < merged.eras.Length; i++)
                if (merged.eras[i - 1].LastYear >= merged.eras[i].FirstYear)
                    throw new InvalidDataException("Era year ranges must not overlap.");
            return merged;
        }

        private static T[] MergeById<T>(T[] source, T[] patch, Func<T, string> id, Func<T, T, T> merge)
            where T : class
        {
            var result = new List<T>(source ?? Array.Empty<T>());
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in patch ?? Array.Empty<T>())
            {
                if (item == null || !IsId(id(item)) || !seen.Add(id(item)))
                    throw new InvalidDataException("Catalog IDs must be unique within each list and use lowercase letters, digits, or hyphens.");
                int index = result.FindIndex(old => id(old) == id(item));
                if (index < 0) result.Add(item);
                else result[index] = merge(result[index], item);
            }
            return result.ToArray();
        }

        private static void Validate(LotContentCatalog catalog)
        {
            if (catalog.eras.Length == 0) throw new InvalidDataException("At least one era is required.");
            foreach (var era in catalog.eras)
            {
                if (!IsName(era.Name) || era.periods == null || era.periods.Length == 0)
                    throw new InvalidDataException($"Era {era.id} needs a name and periods.");
                int lastYear = 0;
                foreach (var period in era.periods)
                {
                    if (period == null || !IsName(period.Name) || period.FirstYear <= lastYear || period.LastYear < period.FirstYear)
                        throw new InvalidDataException($"Era {era.id} needs ordered, nonoverlapping periods with valid years.");
                    lastYear = period.LastYear;
                }
            }
            foreach (var category in catalog.categories)
            {
                if (!IsName(category.name) || !Array.Exists(catalog.categoryTypes, type => type.id == category.categoryType))
                    throw new InvalidDataException($"Category {category.id} needs a name and a defined categoryType.");
                var children = new HashSet<string>(StringComparer.Ordinal);
                foreach (var child in category.subcategories ?? Array.Empty<Subcategory>())
                    if (child == null || !IsId(child.id) || !IsName(child.name) || !children.Add(child.id))
                        throw new InvalidDataException($"Category {category.id} has an invalid or duplicate subcategory.");
            }
        }

        private static bool IsId(string id) => !string.IsNullOrEmpty(id) && id.Length <= 80 &&
            id.All(c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '-');

        private static bool IsName(string name) => !string.IsNullOrWhiteSpace(name) && name.Length <= 80 && !name.Any(char.IsControl);
    }
}

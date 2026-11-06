using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CityForge.World
{
    internal sealed class PlaceNameGenerator
    {
        [Serializable]
        sealed class NameList
        {
            public string[] names;
        }

        internal static readonly IReadOnlyList<string> Suffixes = Array.AsReadOnly(new[] { "county", "valley", "province", "territory", "Parish" });
        static readonly int SuffixSpace = 1 + Suffixes.Max(suffix => suffix.Length);
        readonly string[] names;
        readonly bool randomSuffix;
        readonly System.Random random = new System.Random();

        public PlaceNameGenerator(string listName, int characterLimit, bool randomSuffix = true)
        {
            this.randomSuffix = randomSuffix;
            int maxBaseLength = characterLimit - (randomSuffix ? SuffixSpace : 0);
            var pool = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var builtIn = Resources.Load<TextAsset>("Region/" + listName);
            if (builtIn) AddNames(pool, builtIn.text, builtIn.name, maxBaseLength);

            var folder = Path.Combine(Application.persistentDataPath, "Mods", listName);
            try
            {
                if (Directory.Exists(folder))
                {
                    foreach (var path in Directory.GetFiles(folder, "*.json"))
                    {
                        try { AddNames(pool, File.ReadAllText(path), path, maxBaseLength); }
                        catch (IOException error) { Debug.LogWarning(error.Message); }
                        catch (UnauthorizedAccessException error) { Debug.LogWarning(error.Message); }
                    }
                }
            }
            catch (IOException error) { Debug.LogWarning(error.Message); }
            catch (UnauthorizedAccessException error) { Debug.LogWarning(error.Message); }

            names = pool.ToArray();
        }

        static void AddNames(HashSet<string> pool, string json, string source, int maxBaseLength)
        {
            try
            {
                var entries = JsonUtility.FromJson<NameList>(json)?.names;
                if (entries == null)
                {
                    Debug.LogWarning($"Name list {source} needs a names array.");
                    return;
                }

                foreach (var entry in entries)
                {
                    var name = entry?.Trim();
                    if (!string.IsNullOrEmpty(name) && name.Length <= maxBaseLength && !name.Any(char.IsControl))
                        pool.Add(name);
                }
            }
            catch (ArgumentException error)
            {
                Debug.LogWarning($"Could not load names from {source}. {error.Message}");
            }
        }

        public string Next(string current)
        {
            if (names.Length == 0) return null;
            int count = randomSuffix ? names.Length * Suffixes.Count : names.Length;
            int choice = random.Next(count);
            var name = Format(choice);
            if (count > 1 && string.Equals(name, current, StringComparison.OrdinalIgnoreCase))
                name = Format((choice + random.Next(1, count)) % count);
            return name;
        }

        string Format(int choice) =>
            randomSuffix ? names[choice / Suffixes.Count] + " " + Suffixes[choice % Suffixes.Count] : names[choice];
    }
}

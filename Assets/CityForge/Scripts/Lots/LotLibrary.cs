using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CityForge.Lots
{
    internal sealed class LotLibrary
    {
        internal sealed class Entry
        {
            public LotDefinition Lot { get; }
            public DateTime SavedUtc { get; }

            public Entry(LotDefinition lot, DateTime savedUtc)
            {
                Lot = lot;
                SavedUtc = savedUtc;
            }
        }

        private const string Extension = ".cflot";
        private readonly string folder;
        private string LegacyPath => Path.Combine(folder, "Draft" + Extension);

        public LotLibrary(string folder) => this.folder = folder;

        public static LotLibrary Local => new LotLibrary(Path.Combine(Application.persistentDataPath, "Lots"));

        public Entry[] List(LotType type = LotType.Playable)
        {
            if (!Directory.Exists(folder)) return Array.Empty<Entry>();
            var entries = new List<Entry>();
            foreach (var file in Directory.EnumerateFiles(folder, "*" + Extension))
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                bool legacy = filename == "Draft";
                if (!legacy && !Guid.TryParseExact(filename, "N", out _)) continue;
                var lot = TryRead(file);
                if (lot == null || lot.type != type || (!legacy && lot.id != filename) || legacy && File.Exists(PathFor(lot.id))) continue;
                entries.Add(new Entry(lot, File.GetLastWriteTimeUtc(file)));
            }
            return entries.OrderByDescending(entry => entry.SavedUtc).ThenBy(entry => entry.Lot.id).ToArray();
        }

        public LotDefinition Load(string id)
        {
            var lot = Read(Resolve(id));
            if (lot.id != id) throw new InvalidDataException("Saved lot ID does not match its file.");
            return lot;
        }

        public LotDefinition LoadLatest()
        {
            var latest = List().FirstOrDefault();
            if (latest == null) throw new FileNotFoundException("No saved lot was found.");
            return latest.Lot;
        }

        public LotDefinition SaveAs(LotDefinition lot, LotType type)
        {
            if (!Enum.IsDefined(typeof(LotType), type)) throw new ArgumentOutOfRangeException(nameof(type));
            if (type == LotType.Zoning && (!lot.zoningLayout.Configured || !lot.zoningLayout.Valid || !lot.zoningLayout.Fits(lot)))
                throw new InvalidDataException("Zoning lots require a building slot inside the lot.");
            if (lot.type != type)
            {
                lot = LotDefinition.Parse(JsonUtility.ToJson(lot));
                lot.type = type;
                lot.id = Guid.NewGuid().ToString("N");
            }

            lot.harborHouseScaleUpdated = true;
            Directory.CreateDirectory(folder);
            string destination = Resolve(lot.id);
            string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, JsonUtility.ToJson(lot, true));
                if (File.Exists(destination)) File.Replace(temporary, destination, null);
                else File.Move(temporary, destination);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
            return lot;
        }

        public void Delete(string id) => File.Delete(Resolve(id));

        private string Resolve(string id)
        {
            string file = PathFor(id);
            if (File.Exists(file)) return file;
            return TryRead(LegacyPath)?.id == id ? LegacyPath : file;
        }

        private string PathFor(string id)
        {
            if (!Guid.TryParseExact(id, "N", out _)) throw new ArgumentException("Invalid lot ID.", nameof(id));
            return Path.Combine(folder, id + Extension);
        }

        private static LotDefinition Read(string file)
        {
            var lot = LotDefinition.Parse(File.ReadAllText(file));
            if (lot.harborHouseScaleUpdated ||
                !lot.objects.Any(item => item.assetId == LotCharacterDeck.HarborHouse && item.scale < 1.5f))
                return lot;

            var catalog = LotAssetCatalog.Load();
            var houseAsset = catalog ? catalog.Find(LotCharacterDeck.HarborHouse) : null;
            if (houseAsset == null) return lot;

            // Older saved copies used the original size even though new placements
            // already use the corrected size. Grow the lot only where needed to fit.
            var workersMoved = new HashSet<string>();
            foreach (var house in lot.objects)
            {
                if (house.assetId != LotCharacterDeck.HarborHouse || house.scale >= houseAsset.DefaultPlacementScale) continue;
                float previous = house.scale;
                var workers = lot.objects.Where(item => !workersMoved.Contains(item.id) &&
                    catalog.Find(item.assetId)?.IsCharacter == true && LotCharacterDeck.TryHeight(house, item.position, out _)).ToArray();
                house.scale = houseAsset.DefaultPlacementScale;
                var bounds = LotRuntimeLoader.Footprint(house, houseAsset);
                foreach (var worker in workers)
                {
                    var asset = catalog.Find(worker.assetId);
                    var footprint = LotRuntimeLoader.Footprint(worker, asset);
                    var center = house.position + (worker.position - house.position) * (house.scale / previous);
                    var moved = new Rect(center - footprint.size * .5f, footprint.size);
                    bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, moved.xMin), Mathf.Min(bounds.yMin, moved.yMin),
                        Mathf.Max(bounds.xMax, moved.xMax), Mathf.Max(bounds.yMax, moved.yMax));
                }
                int width = TilesFor(bounds.xMin, bounds.xMax), length = TilesFor(bounds.yMin, bounds.yMax);
                if (width > LotDefinition.MaxTiles || length > LotDefinition.MaxTiles)
                {
                    house.scale = previous;
                    continue;
                }
                lot.width = Mathf.Max(lot.width, width);
                lot.length = Mathf.Max(lot.length, length);
                foreach (var worker in workers)
                {
                    worker.position = house.position + (worker.position - house.position) * (house.scale / previous);
                    workersMoved.Add(worker.id);
                }
            }
            lot.harborHouseScaleUpdated = true;
            return lot;

            static int TilesFor(float min, float max) =>
                Mathf.CeilToInt((2 * Mathf.Max(-min, max) - .001f) / LotDefinition.TileMeters);
        }

        private static LotDefinition TryRead(string file)
        {
            try { return Read(file); }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is ArgumentException)
            {
                return null;
            }
        }
    }
}

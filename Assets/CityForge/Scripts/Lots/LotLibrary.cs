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

        public Entry[] List()
        {
            if (!Directory.Exists(folder)) return Array.Empty<Entry>();
            var entries = new List<Entry>();
            foreach (var file in Directory.EnumerateFiles(folder, "*" + Extension))
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                bool legacy = filename == "Draft";
                if (!legacy && !Guid.TryParseExact(filename, "N", out _)) continue;
                var lot = TryRead(file);
                if (lot == null || (!legacy && lot.id != filename) || legacy && File.Exists(PathFor(lot.id))) continue;
                entries.Add(new Entry(lot, File.GetLastWriteTimeUtc(file)));
            }
            return entries.OrderByDescending(entry => entry.SavedUtc).ThenBy(entry => entry.Lot.id).ToArray();
        }

        public LotDefinition Load(string id)
        {
            var lot = LotDefinition.Parse(File.ReadAllText(Resolve(id)));
            if (lot.id != id) throw new InvalidDataException("Saved lot ID does not match its file.");
            return lot;
        }

        public LotDefinition LoadLatest()
        {
            var latest = List().FirstOrDefault();
            if (latest == null) throw new FileNotFoundException("No saved lot was found.");
            return latest.Lot;
        }

        public void Save(LotDefinition lot)
        {
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

        private static LotDefinition TryRead(string file)
        {
            try { return LotDefinition.Parse(File.ReadAllText(file)); }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is ArgumentException)
            {
                return null;
            }
        }
    }
}

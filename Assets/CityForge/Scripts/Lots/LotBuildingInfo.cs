using System;
using System.IO;
using UnityEngine;

namespace CityForge.Lots
{
    internal enum LotEntryType { Pedestrian, Vehicle }

    [Serializable]
    internal sealed class LotBuildingInfo
    {
        public Vector2 entrance;
        public LotFrontage facing;
        public LotEntryType entryType;
        public float width;

        private static string PathFor(string id) => Path.Combine(Application.persistentDataPath,
            "BuildingInfo", Uri.EscapeDataString(id) + ".json");

        internal static LotBuildingInfo Load(string id)
        {
            try
            {
                var path = PathFor(id);
                if (!File.Exists(path)) return null;
                var info = JsonUtility.FromJson<LotBuildingInfo>(File.ReadAllText(path));
                return info != null && float.IsFinite(info.entrance.x) && float.IsFinite(info.entrance.y) &&
                    info.facing >= LotFrontage.South && info.facing <= LotFrontage.East ? info : null;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Debug.LogWarning($"Could not read building info for {id}: {error.Message}");
                return null;
            }
        }

        internal void Save(string id)
        {
            var path = PathFor(id);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(this, true));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
    }
}

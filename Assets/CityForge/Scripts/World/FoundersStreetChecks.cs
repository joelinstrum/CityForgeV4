#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using CityForge.World;
using UnityEngine;
namespace CityForge.Editor
{
    public static class FoundersStreetChecks
    {
        public static void Run()
        {
            var type = typeof(PrototypeRegionView);
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            object Call(string method, string id) => type.GetMethod(method, flags).Invoke(null, new object[] { id });
            var old = new[] { "dirt", "wagon", "gravel", "macadam", "cobblestone", "brick", "blacktop", "paved-street", "founders-dirt-street" };
            for (int i = 0; i < old.Length; i++)
                if ((int)Call("FindRoadSurface", old[i]) != i) throw new Exception("Existing surface index changed: " + old[i]);
            var records = new[] { "founders-dirt-street", "founders-cobblestone-street", "founders-brick-street" }.Select((id, i) =>
            {
                if ((int)Call("DefaultRoadWidth", id) != 4 || !(bool)Call("HasSidewalks", id) || !(bool)Call("SupportsCommercialFrontage", id))
                    throw new Exception("Street width, sidewalk or commercial frontage missing: " + id);
                if ((int)Call("FoundersStreetSlot", id) != i) throw new Exception("Founders street menu order changed");
                var record = new DistrictRoadRecord { surfaceId = id, district = 0, width = 4, points = new[] { new Vector2(-20, i * 10), new Vector2(20, i * 10) } };
                var restored = JsonUtility.FromJson<DistrictRoadRecord>(JsonUtility.ToJson(record));
                if (restored.surfaceId != id || restored.width != 4) throw new Exception("Street save roundtrip failed");
                return restored;
            }).ToArray();
            PrototypeRegionView.ValidateDistrictRoads(records, 64, 1024);
            if (!(bool)Call("IsMasonryRoad", records[1].surfaceId) || !(bool)Call("IsMasonryRoad", records[2].surfaceId))
                throw new Exception("Masonry street classification missing");
            Debug.Log("FOUNDERS STREET CHECKS PASS: three one-tile street types, commercial frontage and sidewalks, stable old IDs/indices, saved-record validation and roundtrip.");
        }
    }
}
#endif

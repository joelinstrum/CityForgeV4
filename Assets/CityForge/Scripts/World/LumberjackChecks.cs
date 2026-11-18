#if UNITY_EDITOR
using System;
using System.Reflection;
using CityForge.Lots;
using CityForge.World;
using UnityEngine;

namespace CityForge.Editor
{
    public static class LumberjackChecks
    {
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        public static void Run()
        {
            var root = new GameObject("Disposable lumberjack checks");
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(200, 20, 200) };
            GameObject axeman = null;
            try
            {
                var view = root.AddComponent<PrototypeRegionView>();
                var land = root.AddComponent<MeadowDistrictLandscape>();
                var terrain = root.AddComponent<Terrain>(); terrain.terrainData = data;
                Set(land, "<View>k__BackingField", view);
                Set(land, "<DisplayTerrain>k__BackingField", terrain);
                Set(land, "<TreesEnabled>k__BackingField", true);
                Set(land, "<Side>k__BackingField", 200f);
                Set(land, "terrainData", data);
                var baseline = ScriptableObject.CreateInstance<MeadowRuntimeBaseline>();
                baseline.treePrefabs = new GameObject[11];
                Set(land, "<Baseline>k__BackingField", baseline);
                Set(land, "fullTreeInstances", new[] {
                    new TreeInstance { prototypeIndex = 0, position = new Vector3(.02f,0,.02f) },
                    new TreeInstance { prototypeIndex = 3, position = new Vector3(.05f,0,.05f) },
                    new TreeInstance { prototypeIndex = 3, position = new Vector3(.1f,0,.1f) },
                    new TreeInstance { prototypeIndex = 3, position = new Vector3(.9f,0,.9f) }
                });
                var a = root.AddComponent<DistrictLumberjack>();
                var b = root.AddComponent<DistrictLumberjack>();
                Require(land.TryClaimTimber(a, Vector2.zero, _ => true, out var first) && first == new Vector2(10,10), "Nearest timber/ground-cover exclusion failed");
                Require(land.TryClaimTimber(b, Vector2.zero, _ => true, out var second) && second == new Vector2(20,20), "Workers claimed the same tree");
                Require(!land.TryClaimTimber(b, Vector2.zero, _ => true, out _), "80m leash failed");
                land.ReleaseTimber(b, first);
                Require(!land.TryClaimTimber(b, Vector2.zero, _ => true, out _), "Nonowner released a claim");
                land.ReleaseTimber(a, first);
                land.ForgetTimberSearch(a); // A different reachability policy starts a new search.
                Require(!land.TryClaimTimber(a, Vector2.zero, _ => false, out _), "Unreachable tree claimed");
                int searches = land.TimberCandidateSearches;
                Require(!land.TryClaimTimber(a, Vector2.zero, _ => throw new Exception("Unchanged rejected route evaluated twice"), out _), "Rejected route was not remembered");
                Require(land.TimberCandidateSearches == searches, "Tree alternatives were gathered again without a forest edit");
                land.ForgetTimberSearch(a);
                Require(land.TryClaimTimber(a, Vector2.zero, _ => true, out var reclaimed) && reclaimed == first, "Claim release failed");
                land.ReleaseTimber(a, first); land.ReleaseTimber(b, second);
                searches = land.TimberCandidateSearches;
                var originalTrees = land.CanonicalTreeInstances;
                Set(land,"fullTreeInstances",new[]{originalTrees[0],originalTrees[2],originalTrees[3]});
                Require(land.TryClaimTimber(a,Vector2.zero,_=>true,out var remembered)&&remembered==second&&land.TimberCandidateSearches==searches,
                    "Harvesting discarded remembered alternatives or retained a removed tree");
                Set(land,"fullTreeInstances",originalTrees);
                Require(!land.TimberRouteClear(Vector2.zero, new Vector2(15,15)), "Route crossed a tree trunk");
                Require(land.TimberRouteClear(Vector2.zero, new Vector2(8,0)), "Clear route was blocked");
                var settings = new LotCharacterScript { configured = true, scriptedBehavior = LotScriptedBehavior.Lumberjack };
                Require(JsonUtility.FromJson<LotCharacterScript>(JsonUtility.ToJson(settings)).scriptedBehavior == LotScriptedBehavior.Lumberjack && settings.IsValid, "Lumberjack settings roundtrip failed");
                Require((int)LotScriptedBehavior.Shopkeeper == 2 && (int)LotScriptedBehavior.Lumberjack == 3, "Existing saved behavior IDs changed");
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Resources/Lots/V3Characters/Axeman/Character.prefab");
                Require(prefab, "Local axeman missing");
                axeman = UnityEngine.Object.Instantiate(prefab);
                var animator = axeman.GetComponentInChildren<Animator>();
                animator.Rebind(); animator.Update(0);
                foreach (var state in new[] { "Idle", "Walk", "Chop" })
                    Require(animator.HasState(0, Animator.StringToHash(animator.GetLayerName(0) + "." + state)), "Missing axeman animation: " + state);
                UnityEngine.Object.DestroyImmediate(baseline);
                Debug.Log("LUMBERJACK CHECKS PASS: claims, ownership, release, range, reachability, trunk safety, saved role compatibility, settings roundtrip, local Idle/Walk/Chop states.");
            }
            finally
            {
                if (axeman) UnityEngine.Object.DestroyImmediate(axeman);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }
    }
}
#endif

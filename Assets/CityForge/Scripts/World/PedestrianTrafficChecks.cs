#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.Editor
{
    public static class PedestrianTrafficChecks
    {
        private static void CheckWalking(LotAssetCatalogEntry asset)
        {
            var instance = LotRuntimeLoader.InstantiateObject(new LotObjectDefinition { assetId = asset.id, scale = 1 }, asset, null);
            try
            {
                var animator = instance.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Play(Animator.StringToHash(animator.GetLayerName(0) + ".Walk"), 0, 0);
                animator.Update(0);
                var clips = animator.GetCurrentAnimatorClipInfo(0);
                if (clips.Length != 1 || !clips[0].clip.isLooping) throw new Exception(asset.id + " active Walk does not loop");
                float length = clips[0].clip.length;
                for (int i = 0; i < 180; i++) animator.Update(length * 2.2f / 180);
                var bones = instance.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r => r.bones).Distinct().ToArray();
                var before = bones.Select(b => b.position).ToArray();
                for (int i = 0; i < 20; i++) animator.Update(length * .22f / 20);
                if (bones.Select((b, i) => Vector3.Distance(before[i], b.position)).DefaultIfEmpty(0).Max() < .02f)
                    throw new Exception(asset.id + " walk stopped moving after two cycles");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        public static void Run()
        {
            var source = LotAssetCatalog.Load();
            var catalog = ScriptableObject.CreateInstance<LotAssetCatalog>();
            // Exclude vehicles from this disposable fixture; production catalog assets remain unchanged.
            catalog.entries = source.entries.Where(a => a.IsCharacter || a.category == "Buildings").ToArray();
            var expected = new HashSet<string> { "v3-founders-cook", "v3-founders-stout-townsperson",
                "v3-founders-townsperson", "v3-founders-privateer" };
            var building = catalog.entries.First(a => a.category == "Buildings" && a.prefab);
            foreach (var id in expected)
            {
                var animator = catalog.Find(id)?.prefab?.GetComponentInChildren<Animator>();
                if (!animator || !animator.runtimeAnimatorController) throw new Exception("Missing pedestrian " + id);
            }
            try
            {
                foreach (var id in expected.Append("v3-hooligan")) CheckWalking(catalog.Find(id));
                foreach (var zone in new[] { World.TownZone.Residential1, World.TownZone.Residential2,
                    World.TownZone.Residential3, World.TownZone.Commercial1 })
                {
                    var root = new GameObject("Disposable " + zone + " low-wealth traffic");
                    var data = new TerrainData { heightmapResolution = 33, size = new Vector3(256, 10, 256) };
                    var ground = Terrain.CreateTerrainGameObject(data);
                    ground.transform.SetParent(root.transform);
                    var view = root.AddComponent<World.PrototypeRegionView>();
                    var type = typeof(World.PrototypeRegionView);
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    type.GetField("terrain", flags).SetValue(view, ground.GetComponent<Terrain>());
                    type.GetProperty("SelectedDistrict").SetValue(view, 0);
                    var roots = (Dictionary<World.DistrictLotRecord, Transform>)type.GetField("districtLotRoots", flags).GetValue(view);
                    try
                    {
                        for (int i = 0; i < 32; i++)
                        {
                            var lot = new World.DistrictLotRecord { center = new Vector2(20 + i * 2, 20),
                                definition = new LotDefinition { width = 3, length = 3, category = "residential", firstEraId = "founders",
                                    zoningLayout = new LotZoningLayout { zone = zone, wealth = LotWealth.Low },
                                    objects = new List<LotObjectDefinition> { new LotObjectDefinition { assetId = building.id } } } };
                            roots.Add(lot, root.transform);
                        }
                        type.GetMethod("RebuildAmbientTraffic", flags).Invoke(view, new object[] { catalog });
                        var actors = (IList)type.GetField("pedestrians", flags).GetValue(view);
                        var seen = new HashSet<string>();
                        foreach (var actor in actors)
                        {
                            var id = (string)actor.GetType().GetField("AssetId").GetValue(actor);
                            if (!expected.Contains(id)) throw new Exception(zone + " spawned fallback " + id);
                            seen.Add(id);
                        }
                        if (actors.Count != 32 || !seen.SetEquals(expected)) throw new Exception(zone + " lost four-character variety");
                        // An unrelated layout refresh must preserve established actors and their identities.
                        var first = actors[0];
                        type.GetMethod("RebuildAmbientTraffic", flags).Invoke(view, new object[] { catalog });
                        if (actors.Count != 32 || !ReferenceEquals(first, actors[0])) throw new Exception("Traffic refresh reset actors");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(data); }
                }
                Debug.Log("PEDESTRIAN TRAFFIC CHECKS PASS: R1/R2/R3/C1 low-wealth Founders lots spawn all four supplied characters, no Hooligan fallback, unchanged traffic preserved.");
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }
    }
}
#endif

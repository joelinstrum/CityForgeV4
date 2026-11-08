using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CityForge.World;
using UnityEditor;
using UnityEngine;

namespace CityForge.EditorTools
{
    public static class RegionalResourceChecks
    {
        [MenuItem("CityForge/Checks/Regional Resources")]
        public static void Run()
        {
            var settings = new RegionCreationSettings
            {
                version = 3, seed = 34781, rivers = 1, topography = 4, treeCoverageVersion = 1
            };
            var snapshot = RegionMapGenerator.Generate("Survey checks", RegionSize.Medium, settings);
            var repeat = RegionMapGenerator.Generate("Repeat survey", RegionSize.Medium, settings);
            Require(JsonUtility.ToJson(snapshot.info.resources) == JsonUtility.ToJson(repeat.info.resources), "Same seed");
            settings.seed++;
            var other = RegionMapGenerator.Generate("Different survey", RegionSize.Medium, settings);
            Require(!snapshot.info.resources.layers.Select(layer => layer.samples)
                .SequenceEqual(other.info.resources.layers.Select(layer => layer.samples)), "Different seed");

            settings.seed--;
            settings.resourceAbundance = ResourceAbundance.VeryAbundant;
            var abundant = RegionMapGenerator.Generate("Abundant survey", RegionSize.Medium, settings);
            byte[] Layer(SavedRegionSnapshot region, string type) =>
                Convert.FromBase64String(region.info.resources.layers.Single(layer => layer.type == type).samples);
            Require(Layer(abundant, "timber").Sum(value => (int)value) > Layer(snapshot, "timber").Sum(value => (int)value),
                "Abundance increases resource strength");
            Require(Layer(snapshot, "fish").Zip(Layer(abundant, "fish"), (normal, high) => normal != 0 || high <= 1).All(value => value),
                "Abundance does not create inland fish");

            settings.resourceAbundance = ResourceAbundance.Normal;
            settings.resourceDistribution = ResourceDistribution.Balanced;
            var balanced = RegionMapGenerator.Generate("Balanced survey", RegionSize.Medium, settings);
            settings.resourceDistribution = ResourceDistribution.HighlySpecialized;
            var specialized = RegionMapGenerator.Generate("Specialized survey", RegionSize.Medium, settings);
            Require(!Layer(balanced, "timber").SequenceEqual(Layer(specialized, "timber")) &&
                balanced.data.Heights.Cast<float>().SequenceEqual(specialized.data.Heights.Cast<float>()),
                "Distribution changes resources while preserving terrain");
            settings.resourceDiscovery = ResourceDiscovery.RevealedAtStart;
            var revealed = RegionMapGenerator.Generate("Revealed survey", RegionSize.Medium, settings);
            Require(revealed.info.resources.discoveries.Length > 0 &&
                revealed.info.resources.discoveries.SequenceEqual(specialized.info.resources.deposits.Select(item => item.id)) &&
                Layer(specialized, "timber").SequenceEqual(Layer(revealed, "timber")),
                "Discovery setting changes initial visibility independently of geography");

            var coast = RegionMapGenerator.Generate("Coastal survey", RegionSize.Medium,
                new RegionCreationSettings { seed = 4, terrainShape = RegionTerrainShape.Coast }, new float[513, 513]);
            var coastalMap = new ResourceMap(coast.info.resources);
            var ocean = coastalMap.At(new Vector2(.06f, .5f));
            var inland = coastalMap.At(new Vector2(.8f, .5f));
            Require(!ocean.ContainsKey("timber") && !ocean.ContainsKey("fertile-soil") &&
                !inland.ContainsKey("fish") && !inland.ContainsKey("fresh-water"), "Coastal and inland constraints");
            Require(Convert.FromBase64String(coast.info.resources.layers.Single(layer => layer.type == "fish").samples)
                .Any(value => value > 0), "Coastal fish habitat");

            var data = snapshot.info.resources;
            var map = new ResourceMap(data);
            var rules = JsonUtility.FromJson<ResourceModifierRules>(
                Resources.Load<TextAsset>("Config/RegionalResourceModifiers").text);
            var deposit = data.deposits.First();
            Require(!map.At(deposit.position).ContainsKey(deposit.type), "Undiscovered deposit absent from lookup");
            var hidden = map.Evaluate(deposit.position, 1900, rules);
            Require(!hidden.visibleResources.ContainsKey(deposit.type), "Industrial year does not discover deposit");
            Require(map.Discover(deposit.id) && !map.Discover(deposit.id), "Discovery is explicit and idempotent");
            var early = map.Evaluate(deposit.position, 1750, rules);
            Require(early.visibleResources.ContainsKey(deposit.type) && !early.economicAdvantages.Contains(deposit.type),
                "Discovered deposit precedes economic availability");
            var late = map.Evaluate(deposit.position, 1900, rules);
            Require(late.economicAdvantages.Contains(deposit.type) &&
                late.scores[(int)SuitabilityCategory.Industry] > hidden.scores[(int)SuitabilityCategory.Industry],
                "Discovery affects industrial suitability");
            var gated = new ResourceModifierRules
            {
                modifiers = new[] { new ResourceModifier { type = deposit.type, category = SuitabilityCategory.Industry,
                    weight = 20, requiredTechnology = "survey-tech", effect = "industrial-productivity", effectScale = .1f } }
            };
            Require(!map.Evaluate(deposit.position, 1900, gated).economicAdvantages.Contains(deposit.type) &&
                map.Evaluate(deposit.position, 1900, gated, id => id == "survey-tech").passiveModifiers["industrial-productivity"] > 0,
                "Technology conditions affect availability");
            var atRiver = map.Evaluate(new Vector2(.5f, .35f), 1750, rules);
            var upland = map.Evaluate(new Vector2(.95f, .35f), 1750, rules);
            Require(atRiver.scores.Any(value => value > 0) && !atRiver.scores.SequenceEqual(upland.scores), "Spatial suitability");
            Require(map.InDistrict(0, 8).Count > 0, "District query");

            string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".cfregion");
            var root = new GameObject("Regional resource checks");
            try
            {
                var view = root.AddComponent<PrototypeRegionView>();
                typeof(PrototypeRegionView).GetProperty("Waterless").SetValue(view, true);
                Invoke(view, "RestoreSettlementData", snapshot.info);
                Require(Invoke(view, "AddTown", new Vector2(.04f, .04f), "First", 1750) == null, "First town");
                Require(Invoke(view, "AddTown", new Vector2(.06f, .06f), "Second", 1753) == null, "Second town");
                Require(view.Capital == view.Towns[0] && view.TownsInDistrict(0).Length == 2, "Capital and shared district");
                snapshot.info.towns = view.Towns;
                snapshot.info.development = new RegionDevelopmentState
                {
                    achievements = new[] { new LegacyAchievement { pathId = "agrarian", rankId = "first", earnedYear = 1800 } }
                };
                SavedRegionStore.Write(snapshot, path);
                var loaded = SavedRegionStore.Load(path);
                Require(JsonUtility.ToJson(data) == JsonUtility.ToJson(loaded.info.resources), "Survey and discoveries round trip");
                Require(JsonUtility.ToJson(snapshot.info) == JsonUtility.ToJson(loaded.info), "Settlement metadata round trip");
                Require(snapshot.data.Heights.Cast<float>().SequenceEqual(loaded.data.Heights.Cast<float>()), "Terrain round trip");
                Invoke(view, "RestoreSettlementData", loaded.info);
                Require(view.Capital.id == snapshot.info.towns[0].id && view.Towns[1].establishedYear == 1753,
                    "Stable town identity and founding year");

                snapshot.info.resources = null;
                SavedRegionStore.Write(snapshot, path);
                Require(SavedRegionStore.Load(path).info.resources == null, "Legacy save without resource data");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                File.Delete(path);
                File.Delete(path + ".bak");
            }

            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
                map.Evaluate(new Vector2(i % 100 / 100f, i / 100 / 100f), 1900, rules);
            UnityEngine.Debug.Log($"Regional resource checks passed. 10,000 suitability evaluations in {watch.Elapsed.TotalMilliseconds:F1} ms.");
        }

        static object Invoke(PrototypeRegionView view, string method, params object[] args) =>
            typeof(PrototypeRegionView).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, args);

        static void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
        }
    }
}

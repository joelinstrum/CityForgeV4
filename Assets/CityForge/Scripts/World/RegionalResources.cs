using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.UI;
using UnityEngine;

namespace CityForge.World
{
    [Serializable]
    public sealed class ResourceLayer
    {
        public string type;
        public string samples;
    }

    [Serializable]
    public sealed class MineralDeposit
    {
        public string id, type;
        public Vector2 position;
        public float abundance, radius;
    }

    [Serializable]
    public sealed class RegionalResourceData
    {
        public int version = 1;
        public int resolution;
        public ResourceLayer[] layers;
        public MineralDeposit[] deposits;
        public string[] discoveries;

        internal void Validate()
        {
            if (version != 1 || resolution < 2 || resolution > 128 || layers == null ||
                layers.Length > 64 || deposits == null || deposits.Length > 1024)
                throw new InvalidDataException("Invalid regional resources.");
            var types = new HashSet<string>(StringComparer.Ordinal);
            var buffer = new byte[resolution * resolution];
            foreach (var layer in layers)
            {
                if (layer == null || string.IsNullOrEmpty(layer.type) || !types.Add(layer.type) ||
                    layer.samples == null || layer.samples.Length != 4 * ((resolution * resolution + 2) / 3) ||
                    !Convert.TryFromBase64String(layer.samples, buffer, out int length) || length != buffer.Length)
                    throw new InvalidDataException("Invalid resource layer.");
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var deposit in deposits)
                if (deposit == null || string.IsNullOrEmpty(deposit.id) || !ids.Add(deposit.id) ||
                    string.IsNullOrEmpty(deposit.type) || !ResourceMap.Contains(deposit.position) ||
                    !(deposit.abundance > 0 && deposit.abundance <= 1) || !(deposit.radius > 0 && deposit.radius <= .25f))
                    throw new InvalidDataException("Invalid mineral deposit.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in discoveries ?? Array.Empty<string>())
                if (!ids.Contains(id) || !seen.Add(id)) throw new InvalidDataException("Invalid resource discovery.");
        }
    }

    public enum SuitabilityCategory { Agriculture, Industry, Commerce, PopulationGrowth, Culture }

    [Serializable]
    public sealed class ResourceModifier
    {
        public string type;
        public string firstEra, lastEra;
        public string requiredTechnology;
        public SuitabilityCategory category;
        public float weight;
        public string effect;
        public float effectScale;
    }

    [Serializable]
    public sealed class ResourceModifierRules
    {
        public ResourceModifier[] modifiers;
    }

    public sealed class SettlementSuitability
    {
        public readonly float[] scores = new float[Enum.GetValues(typeof(SuitabilityCategory)).Length];
        public readonly Dictionary<string, float> visibleResources = new Dictionary<string, float>(StringComparer.Ordinal);
        public readonly Dictionary<string, float> passiveModifiers = new Dictionary<string, float>(StringComparer.Ordinal);
        public readonly HashSet<string> economicAdvantages = new HashSet<string>(StringComparer.Ordinal);
        public readonly List<string> geography = new List<string>();
        public readonly List<string> limitations = new List<string>();
    }

    public sealed class ResourceMap
    {
        const int BucketSide = 16;
        readonly RegionalResourceData data;
        readonly Dictionary<string, byte[]> layers = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        readonly List<MineralDeposit>[] buckets = new List<MineralDeposit>[BucketSide * BucketSide];
        readonly HashSet<string> known;
        readonly HashSet<string> depositIds;

        public ResourceMap(RegionalResourceData data)
        {
            this.data = data;
            known = new HashSet<string>(data?.discoveries ?? Array.Empty<string>(), StringComparer.Ordinal);
            depositIds = new HashSet<string>(StringComparer.Ordinal);
            if (data == null) return;
            foreach (var layer in data.layers) layers.Add(layer.type, Convert.FromBase64String(layer.samples));
            foreach (var deposit in data.deposits)
            {
                depositIds.Add(deposit.id);
                var reach = Vector2.one * deposit.radius;
                var low = deposit.position - reach;
                var high = deposit.position + reach;
                for (int z = Bucket(low.y); z <= Bucket(high.y); z++)
                for (int x = Bucket(low.x); x <= Bucket(high.x); x++)
                    (buckets[z * BucketSide + x] ??= new List<MineralDeposit>()).Add(deposit);
            }
        }

        internal static bool Contains(Vector2 p) => p.x >= 0 && p.x <= 1 && p.y >= 0 && p.y <= 1;
        static int Bucket(float value) => Mathf.Min(BucketSide - 1, (int)(Mathf.Clamp01(value) * BucketSide));

        public bool Discover(string id)
        {
            if (!depositIds.Contains(id) || !known.Add(id)) return false;
            data.discoveries = known.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            return true;
        }

        internal RegionalResourceData Capture() => data == null ? null : new RegionalResourceData
        {
            version = data.version, resolution = data.resolution, layers = data.layers,
            deposits = data.deposits, discoveries = data.discoveries
        };

        internal bool HasSurvey => data != null;
        internal int DiscoveryCount => known.Count;
        internal IEnumerable<MineralDeposit> KnownDeposits()
        {
            if (data == null) yield break;
            foreach (var deposit in data.deposits)
                if (known.Contains(deposit.id)) yield return deposit;
        }

        internal IEnumerable<(Vector2 position, float strength)> SurveySites(string type)
        {
            if (!layers.TryGetValue(type, out var values)) yield break;
            int side = data.resolution;
            for (int i = 0; i < values.Length; i++)
                if (values[i] > 0)
                    yield return (new Vector2(i % side, i / side) / (side - 1), values[i] / 255f);
        }

        public Dictionary<string, float> At(Vector2 position) => SampleAt(position, false);

#if UNITY_EDITOR
        public Dictionary<string, float> DebugSurveyAt(Vector2 position) => SampleAt(position, true);
#endif

        Dictionary<string, float> SampleAt(Vector2 position, bool includeUndiscovered)
        {
            var result = new Dictionary<string, float>(StringComparer.Ordinal);
            if (data == null || !Contains(position)) return result;
            foreach (var layer in layers)
            {
                float value = Sample(layer.Value, position);
                if (value > 0) result.Add(layer.Key, value);
            }
            var local = buckets[Bucket(position.y) * BucketSide + Bucket(position.x)];
            if (local == null) return result;
            foreach (var deposit in local)
            {
                if (!includeUndiscovered && !known.Contains(deposit.id)) continue;
                float value = deposit.abundance * Mathf.Clamp01(1 - Vector2.Distance(position, deposit.position) / deposit.radius);
                if (value <= 0) continue;
                result.TryGetValue(deposit.type, out float previous);
                result[deposit.type] = Mathf.Max(previous, value);
            }
            return result;
        }

        public Dictionary<string, float> InDistrict(int index, int grid)
        {
            if (grid < 2 || grid > 16 || index < 0 || index >= grid * grid)
                throw new ArgumentOutOfRangeException(nameof(index));
            var bounds = RegionBiomeMap.DistrictBounds(index, grid);
            var result = new Dictionary<string, float>(StringComparer.Ordinal);
            // Summarize peak local influence on a bounded grid, including narrow districts.
            int steps = Mathf.Max(4, (data?.resolution ?? 0) / grid);
            foreach (var layer in layers)
            {
                float peak = 0;
                for (int z = 0; z < steps; z++)
                for (int x = 0; x < steps; x++)
                    peak = Mathf.Max(peak, Sample(layer.Value, bounds.min + new Vector2(x + .5f, z + .5f) * (bounds.width / steps)));
                if (peak > 0) result.Add(layer.Key, peak);
            }
            for (int z = Bucket(bounds.yMin); z <= Bucket(bounds.yMax); z++)
            for (int x = Bucket(bounds.xMin); x <= Bucket(bounds.xMax); x++)
            {
                var local = buckets[z * BucketSide + x];
                if (local == null) continue;
                foreach (var deposit in local)
                {
                    if (!known.Contains(deposit.id)) continue;
                    var nearest = new Vector2(Mathf.Clamp(deposit.position.x, bounds.xMin, bounds.xMax),
                        Mathf.Clamp(deposit.position.y, bounds.yMin, bounds.yMax));
                    float strength = deposit.abundance * Mathf.Clamp01(1 - Vector2.Distance(nearest, deposit.position) / deposit.radius);
                    result.TryGetValue(deposit.type, out float previous);
                    if (strength > previous) result[deposit.type] = strength;
                }
            }
            return result;
        }

        float Sample(byte[] values, Vector2 point)
        {
            int n = data.resolution;
            float x = point.x * (n - 1), z = point.y * (n - 1);
            int ix = Mathf.Min(n - 2, (int)x), iz = Mathf.Min(n - 2, (int)z);
            int index = iz * n + ix;
            return Mathf.Lerp(Mathf.Lerp(values[index], values[index + 1], x - ix),
                Mathf.Lerp(values[index + n], values[index + n + 1], x - ix), z - iz) / 255f;
        }

        public SettlementSuitability Evaluate(Vector2 position, int year, ResourceModifierRules rules,
            Func<string, bool> hasTechnology = null)
        {
            var result = new SettlementSuitability();
            if (data == null || !Contains(position))
            {
                result.limitations.Add("Regional resource survey unavailable.");
                return result;
            }
            foreach (var pair in At(position)) result.visibleResources.Add(pair.Key, pair.Value);
            foreach (var modifier in rules?.modifiers ?? Array.Empty<ResourceModifier>())
            {
                if (modifier == null || string.IsNullOrEmpty(modifier.type) ||
                    (uint)modifier.category >= result.scores.Length || float.IsNaN(modifier.weight) ||
                    float.IsInfinity(modifier.weight) ||
                    !result.visibleResources.TryGetValue(modifier.type, out float strength)) continue;
                if (!string.IsNullOrEmpty(modifier.firstEra))
                {
                    var era = CityForgeTimeline.Find(modifier.firstEra);
                    if (era == null || year < era.FirstYear) continue;
                }
                if (!string.IsNullOrEmpty(modifier.lastEra))
                {
                    var era = CityForgeTimeline.Find(modifier.lastEra);
                    if (era == null || year > era.LastYear) continue;
                }
                if (!string.IsNullOrEmpty(modifier.requiredTechnology) &&
                    !(hasTechnology?.Invoke(modifier.requiredTechnology) ?? false)) continue;
                result.economicAdvantages.Add(modifier.type);
                result.scores[(int)modifier.category] += strength * modifier.weight;
                if (!string.IsNullOrEmpty(modifier.effect) &&
                    !float.IsNaN(modifier.effectScale) && !float.IsInfinity(modifier.effectScale))
                {
                    result.passiveModifiers.TryGetValue(modifier.effect, out float previous);
                    result.passiveModifiers[modifier.effect] = previous + strength * modifier.effectScale;
                }
            }
            for (int i = 0; i < result.scores.Length; i++) result.scores[i] = Mathf.Clamp(result.scores[i], 0, 100);
            if (result.visibleResources.TryGetValue("fresh-water", out float fresh) && fresh > .1f)
                result.geography.Add("Near freshwater");
            else result.limitations.Add("Limited freshwater access");
            if (result.visibleResources.TryGetValue("natural-harbor", out float harbor) && harbor > .1f)
                result.geography.Add("Sheltered coastal water");
            return result;
        }
    }
}

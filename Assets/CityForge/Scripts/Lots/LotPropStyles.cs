using System;
using System.Linq;
using CityForge.World;
using CityForge.UI;
using UnityEngine;

namespace CityForge.Lots
{
    internal static class LotPropStyles
    {
        internal static int BuildApplications { get; private set; }
        internal const float SectionLength = 2.4f, PartialLength = 1.2f;
        [Serializable] internal sealed class Fence
        {
            public string assetId, family, part, era;
            public LotWealth wealth;
            internal bool Straight => part is "Section" or "Partial" or "End";
            internal float Length => part is "Partial" or "End" ? PartialLength : SectionLength;
            internal string Label => $"{new string('$', (int)wealth)} · {family} · {part}";
        }
        [Serializable] internal sealed class PathStyle
        {
            public string materialId, era, family;
            public string label, textureResource, firstEra, lastEra;
            public float tileSizeMeters;
            internal bool AllowsEra(string id)
            {
                if(string.IsNullOrEmpty(firstEra)&&string.IsNullOrEmpty(lastEra))return true;
                var eras=CityForgeTimeline.Eras;int current=Array.FindIndex(eras,e=>e.id==id);
                int first=string.IsNullOrEmpty(firstEra)?0:Array.FindIndex(eras,e=>e.id==firstEra);
                int last=string.IsNullOrEmpty(lastEra)?eras.Length-1:Array.FindIndex(eras,e=>e.id==lastEra);
                return current>=0&&first>=0&&last>=first&&current>=first&&current<=last;
            }
            internal bool VariationEra(string id)=>era==id||!string.IsNullOrEmpty(firstEra)&&AllowsEra(id);
            public LotWealth wealth;
            public LotWealth[] wealths;
            internal bool AllowsWealth(LotWealth value) => wealth == value || (wealths != null && Array.IndexOf(wealths, value) >= 0);
            internal LotWealth SelectionWealth(LotDefinition lot) => AllowsWealth(Wealth(lot)) ? Wealth(lot) : wealth;
        }
        [Serializable] internal sealed class GroundStyle
        {
            public string textureId;
            public bool overlay;
            public LotWealth wealth;
        }
        [Serializable] private sealed class Config
        {
            public Fence[] fences;
            public PathStyle[] paths;
            public GroundStyle[] ground;
        }
        private static Config config;
        private static Config Catalog => config ??= Load();
        private static Config Load()
        {
            var source = Resources.Load<TextAsset>("Config/LotPropStyles");
            return source ? JsonUtility.FromJson<Config>(source.text) : new Config
                { fences = Array.Empty<Fence>(), paths = Array.Empty<PathStyle>() };
        }
        internal static Fence[] Fences => Catalog.fences ?? Array.Empty<Fence>();
        internal static PathStyle[] Paths => Catalog.paths ?? Array.Empty<PathStyle>();
        internal static GroundStyle[] Ground => Catalog.ground ?? Array.Empty<GroundStyle>();
        internal static GroundStyle FindGround(bool overlay, string id) => Array.Find(Ground, item => item.overlay == overlay && item.textureId == id);
        internal static string TextureWealthLabel(LotWealth wealth) => wealth switch
        {
            LotWealth.Low => "$ · Dirt / worn ground",
            LotWealth.Medium => "$$ · Plain",
            LotWealth.High => "$$$ · Fancy",
            _ => "None / clear"
        };
        internal static Fence Find(string id) => Array.Find(Fences, item => item.assetId == id);
        internal static LotWealth Wealth(LotDefinition lot) => lot.zoningLayout.Configured
            ? lot.zoningLayout.wealth : lot.wealth;
        internal static uint Hash(string text)
        {
            uint hash = 2166136261;
            foreach (char c in text ?? "") hash = unchecked((hash ^ c) * 16777619);
            return hash;
        }

        internal static bool FenceWordsMatch(LotAssetCatalogEntry source, LotAssetCatalogEntry candidate)
        {
            foreach(var word in new[]{"section","partial"})
            {
                bool Has(LotAssetCatalogEntry entry)=>(entry.id+" "+entry.label).IndexOf(word,StringComparison.OrdinalIgnoreCase)>=0;
                if(Has(source)!=Has(candidate))return false;
            }
            return true;
        }

        // Called once on a detached lot definition at construction. Saved definitions
        // hold the result, so reopening and repainting wealth cannot reroll a lot.
        internal static void Apply(LotDefinition lot, string instanceId, LotAssetCatalog catalog)
        {
            if (lot.type != LotType.Zoning || !catalog) return;
            BuildApplications++;
            string era = ResidentialGround.LotEra(lot);
            VaryFences();
            // Capture source groups before changing IDs to avoid cascading replacements.
            // One seed per wealth/surface pool keeps compatible paths uniform lot-wide.
            foreach (var group in lot.paths.Where(path => !path.generatedResidentialEntry && !path.materialLocked)
                .GroupBy(path => path.materialId).ToArray())
            {
                var source = FindPath(group.Key);
                if (source == null || source.wealth == LotWealth.None || !source.VariationEra(era)) continue;
                var poolWealth = source.SelectionWealth(lot);
                var choices = Paths.Where(style => style.AllowsWealth(poolWealth) && style.family == source.family &&
                    style.VariationEra(era) && style.AllowsEra(era) && LotPathMaterial.Find(style.materialId)?.IsAvailable == true)
                    .OrderBy(style => style.materialId, StringComparer.Ordinal).ToArray();
                if (choices.Length == 0) continue;
                string selected = choices[Hash(instanceId + ":path:" + poolWealth + ":" + source.family) % (uint)choices.Length].materialId;
                foreach (var path in group) path.materialId = selected;
            }

            void VaryFences()
            {
                // Treat every authored fence as one set. An unknown shape or mixed
                // wealth prevents substitution rather than leaving a mixed result.
                var pieces = lot.objects.Where(item => catalog.Find(item.assetId)?.category == "Fences").ToArray();
                if (pieces.Length == 0 || pieces.Any(item => Find(item.assetId) == null)) return;
                var fenceWealth = Find(pieces[0].assetId).wealth;
                if (fenceWealth == LotWealth.None || pieces.Any(item => Find(item.assetId).wealth != fenceWealth)) return;
                var shapes = pieces.Select(item => Find(item.assetId).part).Distinct().ToArray();
                var families = Fences.Where(s => s.wealth == fenceWealth && s.era == era)
                    .Select(s => s.family).Distinct().Where(family => shapes.All(shape => Fences.Any(s =>
                        s.family == family && s.wealth == fenceWealth && s.era == era && s.part == shape &&
                        catalog.Find(s.assetId) is { prefab: not null } asset && (!s.Straight || asset.fenceSpan > 0))))
                    .OrderBy(s => s, StringComparer.Ordinal).ToArray();
                if (families.Length == 0) return;
                int first = (int)(Hash(instanceId + ":fence") % (uint)families.Length);
                for (int index = 0; index < families.Length; index++)
                {
                    string family = families[(first + index) % families.Length];
                    var replacements = pieces.Select(item => Replacement(item, family)).ToArray();
                    if (replacements.Any(pair => pair.asset == null || !Safe(pair.item, pair.asset, pair.original)) ||
                        !SafeNeighbors(replacements)) continue;
                    for (int i = 0; i < pieces.Length; i++)
                    {
                        pieces[i].assetId = replacements[i].asset.id;
                        pieces[i].fenceLength = replacements[i].item.fenceLength;
                        pieces[i].yaw = replacements[i].item.yaw;
                    }
                    break;
                }
            }
            (LotObjectDefinition item, LotAssetCatalogEntry asset, LotObjectDefinition original) Replacement(LotObjectDefinition source, string family)
            {
                var old = catalog.Find(source.assetId);
                var style = Find(source.assetId);
                var match = Fences.Where(s => s.family == family && s.part == style.part && s.wealth == style.wealth && s.era == era)
                    .Select(s => catalog.Find(s.assetId))
                    .FirstOrDefault(a => a != null && a.prefab && FenceWordsMatch(old,a) && (!style.Straight || a.fenceSpan > 0));
                var copy = JsonUtility.FromJson<LotObjectDefinition>(JsonUtility.ToJson(source));
                copy.assetId = match?.id ?? source.assetId;
                if (style.Straight && match != null && match.id != source.assetId)
                {
                    copy.fenceLength = source.fenceLength > 0 ? source.fenceLength : old.fenceSpan;
                    if (source.fenceLength <= 0 && old.fenceAlongZ) copy.yaw = Mathf.Repeat(copy.yaw + 90f, 360f);
                }
                return (copy, match, source);
            }
            bool SafeNeighbors((LotObjectDefinition item, LotAssetCatalogEntry asset, LotObjectDefinition original)[] replacements)
            {
                for (int a = 0; a < replacements.Length; a++)
                for (int b = a + 1; b < replacements.Length; b++)
                {
                    var first = replacements[a]; var second = replacements[b];
                    if (SharedEnd(first.item, second.item)) continue;
                    float before = Intersection(LotRuntimeLoader.Footprint(first.original, catalog.Find(first.original.assetId)),
                        LotRuntimeLoader.Footprint(second.original, catalog.Find(second.original.assetId)));
                    float after = Intersection(LotRuntimeLoader.Footprint(first.item, first.asset), LotRuntimeLoader.Footprint(second.item, second.asset));
                    if (after > before + .001f) return false;
                }
                return true;
            }
            bool Safe(LotObjectDefinition candidate, LotAssetCatalogEntry asset, LotObjectDefinition original)
            {
                if (!LotRuntimeLoader.Fits(lot, candidate, asset)) return false;
                var before = LotRuntimeLoader.Footprint(original, catalog.Find(original.assetId));
                var after = LotRuntimeLoader.Footprint(candidate, asset);
                foreach (var obstacle in lot.objects)
                {
                    var obstacleAsset = catalog.Find(obstacle.assetId);
                    if (obstacle.id == original.id || obstacleAsset == null || obstacleAsset.IsCharacter ||
                        obstacleAsset.category == "Fences") continue;
                    var rect = LotRuntimeLoader.Footprint(obstacle, obstacleAsset);
                    if (Intersection(after, rect) > Intersection(before, rect) + .001f) return false;
                }
                return true;
            }
        }
        internal static PathStyle FindPath(string materialId) => Array.Find(Paths, style => style.materialId == materialId);
        internal static string PathGroup(LotPathMaterial material)
        {
            var style = FindPath(material.Id);
            return style == null ? material.Group : style.wealths?.Length > 1 ? $"$ / $$ / $$$ · Shared · {style.family}" : $"{TextureWealthLabel(style.wealth)} · {style.family}";
        }
        private static bool SharedEnd(LotObjectDefinition a, LotObjectDefinition b)
        {
            Vector2 Direction(LotObjectDefinition item)
            {
                float yaw = item.yaw * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(yaw), -Mathf.Sin(yaw)) * (item.fenceLength * item.scale * .5f);
            }
            var da = Direction(a); var db = Direction(b);
            for (int sa = -1; sa <= 1; sa += 2)
            for (int sb = -1; sb <= 1; sb += 2)
                if ((a.position + da * sa - b.position - db * sb).sqrMagnitude < .0004f) return true;
            return false;
        }
        private static float Intersection(Rect a, Rect b) => Mathf.Max(0, Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin)) *
            Mathf.Max(0, Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin));
    }
}

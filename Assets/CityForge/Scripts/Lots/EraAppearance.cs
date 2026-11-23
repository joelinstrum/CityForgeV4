using System;
using System.IO;
using System.Linq;
using CityForge.UI;
using UnityEngine;

namespace CityForge.Lots
{
    public enum EraPathRole { Automatic, General, Entrance, Courtyard, Driveway }

    // Appearance is independent of building eligibility and the simulation calendar.
    [Serializable]
    public sealed class EraAppearanceSettings
    {
        public string eraId = "";
        public bool preserveAuthored;
        public bool pavingConfigured;
        public string generalMaterial = "", entranceMaterial = "", courtyardMaterial = "", drivewayMaterial = "";
        public string[] hiddenProps = Array.Empty<string>();
        public string[] keptProps = Array.Empty<string>();
    }

    [Serializable]
    internal sealed class EraAppearanceProfile
    {
        public string eraId, generalMaterial, entranceMaterial, courtyardMaterial, drivewayMaterial;
        public bool filterVehicles;
        public string[] hiddenProps;
        // Reserved content reference. Audio banks are not applied by this appearance pass.
        public string soundSetId;
    }

    internal static class EraAppearance
    {
        [Serializable] private sealed class ProfileCatalog { public EraAppearanceProfile[] profiles; }
        private static EraAppearanceProfile[] profiles;
        internal static EraAppearanceProfile Profile(string era)
        {
            if (profiles == null)
            {
                var source = Resources.Load<TextAsset>("Config/EraAppearanceProfiles");
                profiles = source ? JsonUtility.FromJson<ProfileCatalog>(source.text)?.profiles ?? Array.Empty<EraAppearanceProfile>() : Array.Empty<EraAppearanceProfile>();
            }
            return profiles.FirstOrDefault(profile => profile.eraId == era);
        }
        internal static string EraForYear(int year) => CityForgeTimeline.Eras.FirstOrDefault(e => year >= e.FirstYear && year <= e.LastYear)?.id ?? "founders";
        internal static string EffectiveEra(LotDefinition lot, string fallback) =>
            string.IsNullOrEmpty(lot.eraAppearance?.eraId) ? fallback : lot.eraAppearance.eraId;
        internal static bool Modern(string era) => era is "modern" or "postwar" or "contemporary";
        internal static void Validate(EraAppearanceSettings settings)
        {
            if (settings == null) return;
            if (!string.IsNullOrEmpty(settings.eraId) && CityForgeTimeline.Find(settings.eraId) == null)
                throw new InvalidDataException("Unknown appearance era.");
            foreach (var ids in new[] { settings.hiddenProps, settings.keptProps })
                if (ids != null && (ids.Length > 512 || ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Length))
                    throw new InvalidDataException("Invalid era prop exceptions.");
            if ((settings.hiddenProps ?? Array.Empty<string>()).Intersect(settings.keptProps ?? Array.Empty<string>()).Any())
                throw new InvalidDataException("An era prop cannot be both kept and hidden.");
            foreach (var id in new[] { settings.generalMaterial, settings.entranceMaterial, settings.courtyardMaterial, settings.drivewayMaterial })
                if (!string.IsNullOrEmpty(id) && LotPathMaterial.Find(id) == null)
                    throw new InvalidDataException("Unknown era paving material.");
        }
        internal static bool Allows(LotDefinition lot, LotAssetCatalogEntry asset, string fallback)
        {
            if (asset == null || asset.category == "Buildings") return true;
            var settings = lot.eraAppearance;
            if (settings?.hiddenProps?.Contains(asset.id) == true) return false;
            if (settings?.keptProps?.Contains(asset.id) == true || settings?.preserveAuthored == true) return true;
            string era = EffectiveEra(lot, fallback);
            // Historical authored lots keep their original appearance by default. Modern
            // transport follows its catalog availability, including static hitched horses.
            var vehicle = VehicleProfile.Find(asset.id);
            var profile = Profile(era);
            if (profile?.hiddenProps?.Contains(asset.id) == true) return false;
            return profile?.filterVehicles != true || vehicle == null || vehicle.AvailableIn(era);
        }
        internal static EraPathRole Role(LotPathDefinition path) => path.eraRole != EraPathRole.Automatic ? path.eraRole :
            path.generatedResidentialEntry ? EraPathRole.Entrance : path.use == LotPathUse.Vehicle ? EraPathRole.Driveway : EraPathRole.General;
        internal static string MaterialId(LotDefinition lot, LotPathDefinition path, string fallback)
        {
            if (path.materialLocked || lot.eraAppearance?.preserveAuthored == true) return path.materialId;
            var settings = lot.eraAppearance;
            string era = EffectiveEra(lot, fallback);
            string id = null;
            if (settings?.pavingConfigured == true)
                id = Role(path) switch
                {
                    EraPathRole.Entrance => settings.entranceMaterial,
                    EraPathRole.Courtyard => settings.courtyardMaterial,
                    EraPathRole.Driveway => settings.drivewayMaterial,
                    _ => settings.generalMaterial
                };
            else
            {
                var profile = Profile(era);
                id = Role(path) switch
                {
                    EraPathRole.Entrance => profile?.entranceMaterial,
                    EraPathRole.Courtyard => profile?.courtyardMaterial,
                    EraPathRole.Driveway => profile?.drivewayMaterial,
                    _ => profile?.generalMaterial
                };
            }
            // Never disguise a missing private texture with an unrelated replacement.
            return !string.IsNullOrEmpty(id) && LotPathMaterial.Find(id)?.IsAvailable == true && LotPathMaterial.Find(id).IsAvailableInEra(era) ? id : path.materialId;
        }
        internal static bool OnlyAppearanceChanged(LotDefinition before, LotDefinition after)
        {
            // Landscaping serialization may move cosmetic objects to the end. Draw-list
            // order alone must not replace a building when only its profile was edited.
            var a = LotDefinition.Parse(JsonUtility.ToJson(before));
            var b = LotDefinition.Parse(JsonUtility.ToJson(after));
            a.eraAppearance = b.eraAppearance;
            a.objects = a.objects.OrderBy(item => item.id, StringComparer.Ordinal).ToList();
            b.objects = b.objects.OrderBy(item => item.id, StringComparer.Ordinal).ToList();
            return JsonUtility.ToJson(a) == JsonUtility.ToJson(b);
        }
        internal static LotDefinition Resolve(LotDefinition lot, string fallback)
        {
            var result = LotDefinition.Parse(JsonUtility.ToJson(lot));
            for (int i = 0; i < result.paths.Count; i++)
                result.paths[i].materialId = MaterialId(lot, lot.paths[i], fallback);
            return result;
        }
    }
}

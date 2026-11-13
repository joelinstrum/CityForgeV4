using System;
using UnityEngine;

namespace CityForge.Lots
{
    public enum ResidentialGroundMode { Automatic, Disabled }

    [Serializable]
    public struct ResidentialGroundSettings
    {
        public ResidentialGroundMode mode;
        public string category;
        public string[] allowedVariations;
        public string fixedVariation;
        public bool overrideStrength;
        public float strength;
        public bool overrideRotation;
        public int rotation;

        internal bool Valid => (mode == ResidentialGroundMode.Automatic || mode == ResidentialGroundMode.Disabled) &&
            rotation >= 0 && rotation < 4 && float.IsFinite(strength) && strength >= 0 && strength <= 1;
    }

    internal static class ResidentialGround
    {
        [Serializable]
        internal sealed class Variation
        {
            public string id, label, albedo, mask;
        }

        [Serializable]
        internal sealed class Library
        {
            public string category, era;
            public LotWealth wealth;
            public float strength;
            public Variation[] variations;
        }

        [Serializable]
        private sealed class Catalog
        {
            public Library[] libraries;
        }

        internal readonly struct Selection
        {
            public readonly Variation Variation;
            public readonly int Turns;
            public readonly bool FlipX, FlipY;
            public readonly float Strength, Tint;

            public Selection(Variation variation, int turns, bool flipX, bool flipY, float strength, float tint)
            {
                Variation = variation;
                Turns = turns;
                FlipX = flipX;
                FlipY = flipY;
                Strength = strength;
                Tint = tint;
            }
        }

        private static Library[] libraries;

        internal static Library FindLibrary(LotDefinition lot, string era)
        {
            var zoning = lot.zoningLayout;
            if (zoning.zone < World.TownZone.Residential1 || zoning.zone > World.TownZone.Residential3)
                return null;

            if (libraries == null)
            {
                var config = Resources.Load<TextAsset>("Config/ResidentialGround");
                libraries = config ? JsonUtility.FromJson<Catalog>(config.text)?.libraries : null;
                libraries ??= Array.Empty<Library>();
            }
            return Array.Find(libraries, item => item.wealth == zoning.wealth && item.era == era &&
                (string.IsNullOrEmpty(lot.residentialGround.category) || item.category == lot.residentialGround.category));
        }

        internal static string LotEra(LotDefinition lot) =>
            LotClassification.Era(lot)?.id ??
            (string.IsNullOrEmpty(lot.firstEraId) && lot.firstEraYear == 0 ? "founders" : lot.firstEraId);

        internal static bool TrySelect(LotDefinition lot, string persistentId, string era, out Selection selection)
        {
            selection = default;
            var settings = lot.residentialGround;
            if (settings.mode == ResidentialGroundMode.Disabled) return false;
            var library = FindLibrary(lot, era);
            if (library?.variations == null) return false;
            uint seed = StableHash(persistentId ?? lot.id);
            int count = 0;
            foreach (var variation in library.variations)
                if (Eligible(variation)) count++;
            if (count == 0) return false;

            int index = (int)(seed % (uint)count);
            foreach (var variation in library.variations)
            {
                if (!Eligible(variation) || index-- != 0) continue;
                int turns = settings.overrideRotation ? settings.rotation : (int)((seed >> 10) & 3);
                bool flipX = !settings.overrideRotation && (seed & (1u << 14)) != 0;
                bool flipY = !settings.overrideRotation && (seed & (1u << 15)) != 0;
                float strength = settings.overrideStrength ? settings.strength : library.strength;
                float tint = Mathf.Lerp(.94f, 1.04f, (seed >> 24) / 255f);
                selection = new Selection(variation, turns, flipX, flipY, Mathf.Clamp01(strength), tint);
                return true;
            }
            return false;

            bool Eligible(Variation item)
            {
                if (!string.IsNullOrEmpty(settings.fixedVariation)) return item.id == settings.fixedVariation;
                return settings.allowedVariations == null || settings.allowedVariations.Length == 0 ||
                    Array.IndexOf(settings.allowedVariations, item.id) >= 0;
            }
        }

        private static uint StableHash(string id)
        {
            uint hash = 2166136261;
            if (id == null) return hash;
            foreach (char character in id)
                hash = unchecked((hash ^ character) * 16777619);
            return hash;
        }
    }
}

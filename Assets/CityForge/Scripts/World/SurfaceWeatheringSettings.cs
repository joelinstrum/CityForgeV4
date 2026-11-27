using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    [DisallowMultipleComponent]
    public sealed class SurfaceWeatheringSettings : MonoBehaviour
    {
        [Serializable] public sealed class Override
        {
            [Tooltip("LotPathMaterial ID. Empty matches all materials.")] public string materialId;
            [Tooltip("EraAppearance ID. Empty matches all eras.")] public string eraId;
            public bool sidewalk;
            public SurfaceWeatheringProfile profile;
        }
        public SurfaceWeatheringLibrary library;
        [Tooltip("Optional session override of the shared Library default.")]
        public SurfaceWeatheringProfile roadProfile, sidewalkProfile;
        public List<Override> overrides = new();
        struct Binding { public Material material; public string id, era; public bool sidewalk; public Shader originalShader; }
        readonly List<Binding> bindings = new();
        int revision = -1;
        bool dirty = true;
        void OnValidate() { dirty = true; }
        void LateUpdate()
        {
            if (!dirty && revision == SurfaceWeatheringProfile.Revision) return;
            dirty = false; revision = SurfaceWeatheringProfile.Revision;
            for (int i = bindings.Count - 1; i >= 0; i--)
            {
                if (!bindings[i].material) { bindings.RemoveAt(i); continue; }
                Apply(bindings[i]);
            }
        }
        public void Bind(Material material, string materialId, string eraId, bool sidewalk, Shader originalShader = null)
        {
            if (!library) library = Resources.Load<SurfaceWeatheringLibrary>("SurfaceWeathering/Library");
            var b = new Binding { material = material, id = materialId, era = eraId, sidewalk = sidewalk, originalShader = originalShader };
            bindings.RemoveAll(x => !x.material || x.material == material);
            bindings.Add(b); Apply(b);
        }
        static bool Matches(Override entry, Binding b) => entry != null && entry.sidewalk == b.sidewalk &&
            (string.IsNullOrEmpty(entry.materialId) || entry.materialId == b.id) &&
            (string.IsNullOrEmpty(entry.eraId) || entry.eraId == b.era);
        void Apply(Binding b)
        {
            var profile = b.sidewalk ? sidewalkProfile : roadProfile;
            if (!profile) profile = library ? (b.sidewalk ? library.sidewalkProfile : library.roadProfile)
                : Resources.Load<SurfaceWeatheringProfile>(b.sidewalk ? "SurfaceWeathering/Sidewalk" : "SurfaceWeathering/Road");
            if (library)
                foreach (var entry in library.overrides)
                    if (Matches(entry,b)) profile = entry.profile;
            // Last matching override wins, allowing a generic era followed by a material exception.
            foreach (var entry in overrides)
                if (Matches(entry,b)) profile = entry.profile;
            if (b.originalShader)
            {
                var shader = profile && profile.intensity > 0 ? Resources.Load<Shader>("Region/LotPathBlend") : b.originalShader;
                if (shader && b.material.shader != shader)
                {
                    int queue = b.material.renderQueue;
                    var keywords = b.material.shaderKeywords;
                    b.material.shader = shader; b.material.shaderKeywords = keywords; b.material.renderQueue = queue;
                }
            }
            if (profile) {
                profile.Apply(b.material,b.sidewalk);
                // The face regions belong specifically to the approved RGB bluestone image.
                // Never reinterpret brick, plank, concrete or authored PBR maps as this atlas.
                if(!b.sidewalk || b.id!=CityForge.Lots.LotPathMaterial.Bluestone)
                    b.material.SetVector("_WeatherPavers",Vector4.zero);
            }
            else if (b.material.HasProperty("_WeatherIntensity"))
            {
                b.material.SetFloat("_WeatherIntensity", 0);
                b.material.DisableKeyword("_SURFACE_WEATHERING");
            }
        }
    }
}

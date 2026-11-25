using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.AssetPreparation
{
    [Serializable]
    public sealed class ArtDecoSignChannel
    {
        public string material = "";
        public int letterIndex = -1;
        public bool housing;
        public bool blink;
        public float[] keyTimes = Array.Empty<float>();
        public float[] keyValues = Array.Empty<float>();
        public float cycleSeconds;
        public float blinkOnSeconds = 1f;
        public float blinkOffSeconds = 1f;
        public float blinkPhaseOffsetSeconds;
        public float intensity = 3.5f;
    }

    [Serializable]
    public sealed class ArtDecoSignSettings
    {
        public bool animateLetters;
        public int letterCount;
        public float stepSeconds = 1f;
        public float fullHoldSeconds = 8f;
        public float darkResetSeconds = 1f;
        public ArtDecoSignChannel[] channels = Array.Empty<ArtDecoSignChannel>();
    }

    [Serializable]
    public sealed class ArtDecoSignAnimationDocument
    {
        public ArtDecoSignSettings runtime;
    }

    // Import-time setup must assign the emission map and enable the shader's
    // emission keyword. This component changes per-renderer intensity only.
    [DisallowMultipleComponent]
    public sealed class ArtDecoSignLighting : MonoBehaviour
    {
        public TextAsset animationJson;
        public Renderer[] targetRenderers = Array.Empty<Renderer>();
        [Range(0f, 1f)] public float nightFactor;
        public float phaseOffsetSeconds;
        public bool externalClock;
        [Min(0f)] public float nativeEmissionGain = 1f;
        public string emissionColorProperty = "_EmissionColor";

        private sealed class Binding
        {
            public Renderer renderer;
            public int slot;
            public ArtDecoSignChannel channel;
            public float lastIntensity = float.NaN;
            public readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        }

        private readonly List<Binding> bindings = new List<Binding>();
        private ArtDecoSignSettings settings = new ArtDecoSignSettings();
        private int emissionId;

        private void OnEnable()
        {
            RebuildBindings();
            Apply(Time.time + phaseOffsetSeconds);
        }

        public void SetNightFactor(float value)
        {
            nightFactor = Mathf.Clamp01(value);
            Apply(Time.time + phaseOffsetSeconds);
        }

        public void RebuildBindings()
        {
            bindings.Clear();
            emissionId = Shader.PropertyToID(emissionColorProperty);
            if (animationJson != null)
            {
                var document = JsonUtility.FromJson<ArtDecoSignAnimationDocument>(animationJson.text);
                settings = document != null && document.runtime != null
                    ? document.runtime : new ArtDecoSignSettings();
            }
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in targetRenderers)
            {
                if (renderer == null) continue;
                var materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    var material = materials[slot];
                    if (material == null || !material.HasProperty(emissionId)) continue;
                    string name = material.name.Replace(" (Instance)", "");
                    foreach (var channel in settings.channels ?? Array.Empty<ArtDecoSignChannel>())
                    {
                        if (channel == null || string.IsNullOrEmpty(channel.material)) continue;
                        if (name != channel.material &&
                            !name.StartsWith(channel.material + "__", StringComparison.Ordinal)) continue;
                        bindings.Add(new Binding { renderer = renderer, slot = slot, channel = channel });
                        break;
                    }
                }
            }
        }

        private void Update() { if (!externalClock) Apply(Time.time + phaseOffsetSeconds); }

        public void ApplyAtTime(float seconds, float night)
        {
            nightFactor = Mathf.Clamp01(night);
            Apply(seconds + phaseOffsetSeconds);
        }

        private void Apply(float time)
        {
            int lit = ArtDecoSignTiming.LitLetterCount(time, settings.letterCount,
                settings.stepSeconds, settings.fullHoldSeconds,
                settings.darkResetSeconds, settings.animateLetters);
            foreach (var binding in bindings)
            {
                var channel = binding.channel;
                bool on = !channel.housing &&
                    (channel.letterIndex < 0 || !settings.animateLetters || channel.letterIndex < lit);
                on = on && (!channel.blink || ArtDecoSignTiming.IsBlinkOn(
                    time + channel.blinkPhaseOffsetSeconds, channel.blinkOnSeconds, channel.blinkOffSeconds));
                float value = on ? Mathf.Clamp01(nightFactor) * Mathf.Max(0f, channel.intensity) *
                    ArtDecoSignTiming.KeyedIntensity(time, channel.cycleSeconds, channel.keyTimes, channel.keyValues) : 0f;
                value *= nativeEmissionGain;
                if (Mathf.Abs(value - binding.lastIntensity) < .00001f) continue;
                binding.lastIntensity = value;
                if (binding.renderer == null) continue;
                binding.renderer.GetPropertyBlock(binding.block, binding.slot);
                binding.block.SetColor(emissionId, Color.white * value);
                binding.renderer.SetPropertyBlock(binding.block, binding.slot);
            }
        }
    }
}

using System;
using UnityEngine;

namespace CityForge.World
{
    /// <summary>
    /// User-approved refined C color recipe. Opt-in authoring helpers only;
    /// loading a district does not automatically change its native materials.
    /// Shader targets are luminance-normalized linear chroma, not final lit pixels.
    /// </summary>
    public static class ApprovedForestColors
    {
        public const string ShaderName = "CityForge/Trees/Approved Forest Canopy";
        public const float PaletteMix = .9f;
        public const float LinearLuminanceGain = 1.18f;
        public const float PaleHueShift = -.018f;
        public const float PaleSaturation = .97f;
        public const float PaleValue = .935f;
        public const float GrassDesaturation = .125f;

        public static Vector4 PaletteTarget(int index)
        {
            Color color;
            switch (index)
            {
                case 0: color = new Color32(0x45, 0x55, 0x2B, 255); break;
                case 1: color = new Color32(0x61, 0x71, 0x3D, 255); break;
                case 2: color = new Color32(0x84, 0x94, 0x56, 255); break;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
            Color linear = color.linear;
            float luminance = Luminance(linear);
            return new Vector4(linear.r / luminance, linear.g / luminance,
                linear.b / luminance, LinearLuminanceGain);
        }

        /// <summary>Warm native healthy/dry tints; retain their lighter appearance and alpha.</summary>
        public static Color WarmPale(Color original)
        {
            Color.RGBToHSV(original, out float hue, out float saturation, out float value);
            Color result = Color.HSVToRGB(Mathf.Repeat(hue + PaleHueShift, 1),
                saturation * PaleSaturation, value * PaleValue);
            result.a = original.a;
            return result;
        }

        /// <summary>
        /// Input is the existing sRGB grass texel multiplied by its current terrain tint.
        /// Bake this result to a separate texture and use a unit diffuse-remap scale.
        /// Preserve alpha (the native textures store smoothness there), normals and tiling.
        /// </summary>
        public static Color EarthyGrass(Color compositeSrgb)
        {
            Color linear = compositeSrgb.linear;
            float luminance = Luminance(linear);
            Color earthy = Color.Lerp(linear, new Color(luminance, luminance, luminance, 1),
                GrassDesaturation);
            earthy.r *= 1.035f;
            earthy.b *= .96f;
            float adjusted = Luminance(earthy);
            if (adjusted > 1e-6f) earthy *= luminance / adjusted;
            Color result = earthy.gamma;
            result.a = compositeSrgb.a;
            return result;
        }

        /// <summary>Use on an owned leaf material, never on shared native bark/foliage assets.</summary>
        public static void ConfigureLeafMaterial(Material material, bool refineCrowns = true)
        {
            if (!material || !material.shader || material.shader.name != ShaderName)
                throw new ArgumentException("Use the approved authored canopy shader.", nameof(material));
            material.SetFloat("_PaletteMix", PaletteMix);
            material.SetFloat("_CrownRefine", refineCrowns ? 1 : 0);
            material.enableInstancing = true;
        }

        static float Luminance(Color color) =>
            .2126f * color.r + .7152f * color.g + .0722f * color.b;
    }
}

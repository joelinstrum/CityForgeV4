using UnityEngine;

namespace CityForge.World
{
    [CreateAssetMenu(menuName="CityForge/Meadow grass appearance")]
    public sealed class MeadowGrassSettings : ScriptableObject
    {
        [Tooltip("Disable to restore the preceding grass treatment exactly.")]
        public bool applyRefinement=true;
        [Range(.5f,1.5f)] public float saturation=1.12f;
        [Tooltip("Perceptual (sRGB) brightness relative to the preceding treatment.")]
        [Range(.75f,1.25f)] public float brightness=.94f;
        [ColorUsage(false)] public Color tint=new Color(.95f,.99f,1f,1);
        [Tooltip("Strength of the existing fresh/olive layer color difference. Does not alter painted coverage.")]
        [Range(0,2)] public float variationStrength=1;
    }
}

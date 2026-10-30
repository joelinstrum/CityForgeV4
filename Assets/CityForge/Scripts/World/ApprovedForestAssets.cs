using UnityEngine;

namespace CityForge.World
{
    /// <summary>Locally built from the immutable, verified private checkpoint.</summary>
    public sealed class ApprovedForestAssets : ScriptableObject
    {
        // The locked replay adds its own canopy and cumulative color stages.
        public static bool CheckpointReplay=>!Application.isEditor&&System.Environment.GetEnvironmentVariable("CITYFORGE_LARGE_FOREST")=="1";
        public Mesh[] meshes; // variant * 10 + lod * 2 + wood/leaf
        public Material wood, leaf;
        public Texture2D grass1, grass2;
        public TextAsset layout;
        public GameObject[] nativeSummer;
    }
}

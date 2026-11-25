using UnityEngine;
namespace CityForge.Lots
{
    [CreateAssetMenu(menuName="CityForge/Buildings/Sign descriptor")]
    public sealed class BuildingSignAsset : ScriptableObject
    {
        public string id, label, mountingInterface;
        public SignMountKind kind;
        public Vector3 size;
        public float outwardProjection, minimumGroundClearance;
        public bool doubleSided, nativeReviewed;
        public GameObject nearPrefab, mediumPrefab;
        public Texture2D thumbnail;
        // Prefabs retain authored emission/animation. Compatibility does not retime or relight them.
    }
}

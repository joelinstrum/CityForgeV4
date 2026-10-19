using UnityEngine;
namespace CityForge.World
{
    /// <summary>Inert compatibility component for retired prototype-tree diagnostics.</summary>
    public sealed class DistantTreeRenderer : MonoBehaviour
    {
        public int QuadTreeCount => 0;
        public int FarTreeCount => 0;
        public bool IsFar => false;
        public int BatchCount => 0;
        public int FarTriangleCount => 0;
        private void OnEnable() { enabled=false; }
        public void Refresh() { }
    }
}

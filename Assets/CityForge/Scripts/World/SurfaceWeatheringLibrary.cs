using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    [CreateAssetMenu(menuName="CityForge/Surface Weathering Library")]
    public sealed class SurfaceWeatheringLibrary : ScriptableObject
    {
        public SurfaceWeatheringProfile roadProfile, sidewalkProfile;
        public List<SurfaceWeatheringSettings.Override> overrides = new();
        void OnValidate() { unchecked { SurfaceWeatheringProfile.Revision++; } }
    }
}

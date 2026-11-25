using System.Runtime.CompilerServices;
using CityForge.AssetPreparation;
using UnityEngine;

namespace CityForge.World
{
    // Fixed authored sign. Selection/appearance variants and a new save schema are deliberately separate.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class AuthoredSignLighting : MonoBehaviour
    {
        public ArtDecoSignLighting lighting;
        public float phaseOffsetSeconds;
        sealed class AnimationClock { public float seconds; public int frame = -1; }
        static readonly ConditionalWeakTable<Transform, AnimationClock> clocks = new();
        AnimationClock clock;
        PrototypeRegionView region;
        bool preview;
        RegionTimeOfDay time;
        public float ElapsedSeconds => Clock.seconds;
        AnimationClock Clock
        {
            get
            {
                if (clock != null) return clock;
                var building = GetComponentInParent<AuthoredBuildingLighting>();
                // Both native tiers share the lot-object wrapper, even while one tier is hidden.
                var owner = building ? (building.transform.parent ? building.transform.parent : building.transform) : transform;
                return clock = clocks.GetValue(owner, _ => new AnimationClock());
            }
        }
        void OnEnable()
        {
            if (!lighting) lighting = GetComponent<ArtDecoSignLighting>();
            if (!lighting) return;
            lighting.externalClock = true;
            lighting.RebuildBindings();
            SetTime(RegionTimeOfDay.Noon);
        }
        void LateUpdate()
        {
            if (!lighting) return;
            if (!region) region = GetComponentInParent<PrototypeRegionView>();
            if (!preview && region) time = region.TimeOfDay;
            bool paused = !Application.isPlaying || (!preview && (!region || region.ClockPaused ||
                PrototypeRegionView.ZoningSimulationPaused || region.SelectedDistrict < 0 || region.DistrictLoading || region.IsSaving));
            if (Clock.frame != Time.frameCount)
            {
                Clock.frame = Time.frameCount;
                if (!paused) Clock.seconds += Time.unscaledDeltaTime;
            }
            Apply();
        }
        public void SetPreviewTime(RegionTimeOfDay value) { preview = true; SetTime(value); }
        public void SetTime(RegionTimeOfDay value) { time = value; Apply(); }
        void Apply()
        {
            if (!lighting) return;
            float night = time == RegionTimeOfDay.Night ? 1f : time == RegionTimeOfDay.Evening ? .6f : 0f;
            lighting.ApplyAtTime(Clock.seconds + phaseOffsetSeconds, night);
        }
    }
}

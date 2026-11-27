using UnityEngine;
namespace CityForge.CameraSystem
{
    [CreateAssetMenu(menuName="CityForge/Driving Cockpit Settings")]
    public sealed class DrivingCockpitSettings : ScriptableObject
    {
        [Header("Art Deco engine audio")]
        public bool engineAudio=true;
        [Tooltip("Optional override; defaults to the existing car-model-t MP3.")]
        public AudioClip engineSound;
        [Range(0,1)] public float engineVolume=.12f;
        [Range(.05f,3)] public float engineFadeSeconds=.5f;
        [Header("Layout and motion limits (1080p reference pixels)")]
        [Range(.95f,1.03f)] public float imageScale=1f;
        [Range(0,20)] public float edgeInsetSourcePixels=12f;
        public Color edgeColor=new(.025f,.018f,.014f,1f);
        [Range(0,10)] public float maximumOffset=4.5f;
        [Range(0,.5f)] public float maximumRoll=.15f;
        [Range(1,20)] public float response=8f;
        [Header("Engine vibration — while driving mode is running, including idle")]
        public bool engineVibration=true;
        [Range(0,2)] public float engineIntensity=.18f;
        [Range(1,20)] public float engineFrequency=12f;
        [Header("Turning sway")]
        public bool turningSway=true;
        [Range(0,6)] public float turningIntensity=2.2f;
        [Range(0,.3f)] public float turningRoll=.08f;
        [Header("Road vibration — motion and sampled ground-height changes")]
        public bool roadVibration=true;
        [Range(0,3)] public float roadIntensity=.45f;
        [Range(1,15)] public float roadFrequency=8f;
        [Range(0,2)] public float groundResponse=.25f;
        [Header("Acceleration and braking inertia")]
        public bool inertia=true;
        [Range(0,4)] public float inertiaIntensity=1.2f;
        [Header("Time-of-day interior lighting")]
        public bool timeOfDayLighting=true;
        [Range(0,1.5f)] public float dayBrightness=1f;
        [Range(0,1.5f)] public float morningBrightness=.85f;
        [Range(0,1.5f)] public float eveningBrightness=.60f;
        [Range(0,1)] public float nightBrightness=.30f;
        public Color daylightTint=Color.white;
        public Color nightTint=new(.78f,.86f,1f,1f);
    }
}

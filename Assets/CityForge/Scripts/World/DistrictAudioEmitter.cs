using System;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    public enum CitySound { None, Walking, ConcreteWalking, Horse, WoodenWagon, Children, Commercial, Stream, Nature, NightNature }

    // Passive registration only: one manager samples motion and budgets all voices.
    public sealed class DistrictAudioEmitter : MonoBehaviour
    {
        public CitySound Sound { get; private set; }
        public float Speed { get; private set; }
        internal DistrictSoundscape owner;
        private Vector3 previous;
        private float lastProbe = -1f;
        private ChildrenAtPlay children;
        internal float Phase { get; private set; }

        public static CitySound Classify(string id, bool character, bool transport, int variation = 0)
        {
            if (character)
                return ChildrenAtPlay.IsChild(id) ? CitySound.Children : (variation & 1) == 0 ? CitySound.Walking : CitySound.ConcreteWalking;
            if (string.IsNullOrEmpty(id) || !transport) return CitySound.None;
            if (id.Contains("lumber-wagon", StringComparison.Ordinal) || id.Contains("food-wagon", StringComparison.Ordinal) ||
                id.Contains("wooden-cart", StringComparison.Ordinal) || id.Contains("cargo-cart", StringComparison.Ordinal)) return CitySound.WoodenWagon;
            var profile = VehicleProfile.Find(id);
            return profile?.team == true || id.Contains("horse", StringComparison.Ordinal) || id.Contains("mounted-", StringComparison.Ordinal) ||
                id is "covered-wagon" or "food-wagon" or "lumber-wagon" ? CitySound.Horse : CitySound.None;
        }

        internal static void Attach(GameObject instance, LotAssetCatalogEntry asset)
        {
            if (!asset.IsCharacter && asset.category != LotAssetCatalog.Transport) return;
            var manager = instance.GetComponentInParent<DistrictSoundscape>();
            if (!manager) return; // Lot Editor previews and thumbnails stay silent.
            uint seed = unchecked((uint)instance.GetInstanceID());
            seed ^= seed >> 16; seed *= 0x7feb352d; seed ^= seed >> 15;
            var sound = Classify(asset.id, asset.IsCharacter, asset.category == LotAssetCatalog.Transport, (int)seed);
            if (sound == CitySound.None) return;
            var emitter = instance.AddComponent<DistrictAudioEmitter>();
            emitter.Sound = sound;
            emitter.Phase = (seed & 65535) / 65536f;
            emitter.children = instance.GetComponent<ChildrenAtPlay>();
            emitter.owner = manager;
            manager.Register(emitter);
        }

        private void OnEnable()
        {
            lastProbe = -1f;
            if (owner) owner.Register(this);
        }
        private void OnDisable() { if (owner) owner.Unregister(this); }
        internal void ResetMotion() { lastProbe = -1f; Speed = 0f; }
        internal bool Probe(float now)
        {
            if (!isActiveAndEnabled) return false;
            if (Sound == CitySound.Children) return !children || children.ScheduledVisible;
            var position = transform.position;
            if (lastProbe < 0f) { previous = position; lastProbe = now; Speed = 0f; return false; }
            float delta = Mathf.Max(.001f, now - lastProbe);
            var offset = position - previous; offset.y = 0f;
            Speed = offset.magnitude / delta;
            previous = position; lastProbe = now;
            return Speed >= .12f;
        }
    }
}

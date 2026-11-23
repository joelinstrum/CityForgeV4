using System.Collections.Generic;
using CityForge.CameraSystem;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    [DefaultExecutionOrder(100)]
    public sealed class DistrictSoundscape : MonoBehaviour
    {
        public const int VoiceLimit = 13;
        private const int ActorStart = 5;
        private const int ActorVoices = VoiceLimit - ActorStart;
        private sealed class Voice
        {
            internal AudioSource source;
            internal DistrictAudioEmitter emitter;
            internal float gain, pan, phase;
        }
        private readonly List<DistrictAudioEmitter> emitters = new();
        private readonly DistrictAudioEmitter[] selected = new DistrictAudioEmitter[ActorVoices];
        private readonly float[] selectedGain = new float[ActorVoices];
        private readonly List<CommercialSoundArea> commercial = new();
        private readonly AudioCanopyIndex canopy = new();
        private readonly Voice[] voices = new Voice[VoiceLimit];
        private readonly AudioClip[] clips = new AudioClip[10];
        private PrototypeRegionView region;
        private CityForgeCameraController rig;
        private Camera viewCamera;
        private LotAssetCatalog catalog;
        private MeadowDistrictLandscape indexedLand;
        private TreeInstance[] indexedTrees;
        private int woodlandRevision = -1, sampledDistrict = int.MinValue;
        private float scanWait, areasWait, clock;
        private bool wasDetail;
        public Vector2 ListeningPoint { get; private set; }
        public int RegisteredEmitterCount => emitters.Count;
        public int ActiveVoiceCount
        { get { int count = 0; foreach (var voice in voices) if (voice != null && voice.source.isPlaying) count++; return count; } }
        public float NatureTargetGain => voices[0]?.gain ?? 0f;

        public void Initialize(PrototypeRegionView source, CityForgeCameraController controller)
        {
            region = source; rig = controller; viewCamera = rig.GetComponent<Camera>(); catalog = LotAssetCatalog.Load();
            string[] names = { "", "walking-1", "walking-on-concrete", "horse-and-wagon", "wooden-wagon",
                "children-playing", "commercial-area", "stream-longer", "nature", "night-time-summer" };
            for (int i = 1; i < clips.Length; i++)
            {
                clips[i] = Resources.Load<AudioClip>("Audio/CitySounds/" + names[i]);
                if (clips[i]) clips[i].LoadAudioData();
                else Debug.LogWarning("Missing city sound: " + names[i]);
            }
            for (int i = 0; i < voices.Length; i++)
            {
                var go = new GameObject("City audio voice " + i); go.transform.SetParent(transform, false);
                var audio = go.AddComponent<AudioSource>();
                audio.playOnAwake = false; audio.loop = true; audio.volume = 0f;
                audio.spatialBlend = 0f; audio.dopplerLevel = 0f;
                audio.bypassEffects = true; audio.bypassListenerEffects = true; audio.bypassReverbZones = true;
                audio.priority = i >= ActorStart ? 96 : 160;
                voices[i] = new Voice { source = audio };
            }
            // Existing menu audio controls remain authoritative. Reuse its listener.
            bool listener = false;
            foreach (var existing in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (existing.isActiveAndEnabled) { listener = true; break; }
            if (!listener) viewCamera.gameObject.AddComponent<AudioListener>();
        }
        internal void Register(DistrictAudioEmitter emitter) { if (!emitters.Contains(emitter)) emitters.Add(emitter); }
        internal void Unregister(DistrictAudioEmitter emitter) { emitters.Remove(emitter); }
        private void LateUpdate() { if(PrototypeRegionView.ZoningSimulationPaused)return; Tick(Mathf.Min(Time.unscaledDeltaTime, .1f)); }
        public static float DistanceGain(float distance, float radius)
        { return 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance / radius)); }
        public static bool DetailEnabled(int zoom, bool riding) => zoom <= 1 || riding;

        public static float ZoomGain(int zoom, bool riding) => riding || zoom == 0 ? 1f : zoom == 1 ? .5f : 0f;
        public static float NatureZoomGain(int zoom,bool riding) => riding?1f:zoom is >=0 and <=2?1f:zoom is 3 or 4?.5f:0f;
        public static float NightAmbience(float hour, RegionSeason season) => season is RegionSeason.Summer or RegionSeason.Fall
            ? hour >= 12f ? Mathf.SmoothStep(0,1,Mathf.InverseLerp(18.5f,21f,hour)) : 1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(5f,7f,hour)) : 0f;

        public void Tick(float delta)
        {
            if (!region || !rig || voices[0] == null) return;
            clock += delta;
            bool ready = region.isActiveAndEnabled && region.Data != null && region.SelectedDistrict >= 0 && !region.DistrictLoading && AudioListener.volume > .0001f && !AudioListener.pause;
            bool detail = ready && region.SelectedDistrict >= 0 && viewCamera.enabled && DetailEnabled(rig.ZoomLevel, rig.Riding);
            if (detail && !wasDetail) foreach (var emitter in emitters) if (emitter) emitter.ResetMotion();
            if (detail != wasDetail) scanWait = 0f;
            wasDetail = detail;
            if (ready) AdvanceCanopy();
            scanWait -= delta;
            if (scanWait <= 0f)
            {
                scanWait = .1f;
                Scan(ready, detail);
            }
            for (int i = 0; i < voices.Length; i++)
            {
                var voice = voices[i]; var audio = voice.source;
                float zoomGain=ZoomGain(rig.ZoomLevel,rig.Riding);
                float target = ready ? voice.gain * (i==0||i==4?NatureZoomGain(rig.ZoomLevel,rig.Riding):zoomGain) : 0f;
                float gain = Mathf.MoveTowards(audio.volume, target, delta * (i == 0 ? .1f : .75f));
                if (audio.volume != gain) audio.volume = gain;
                audio.panStereo = Mathf.MoveTowards(audio.panStereo, voice.pan, delta * 3f);
                if (gain <= .0001f) { if (audio.isPlaying) audio.Stop(); continue; }
                if (!audio.isPlaying && audio.clip && audio.clip.loadState == AudioDataLoadState.Loaded)
                {
                    audio.Play();
                    if (voice.phase > 0f) audio.time = voice.phase * Mathf.Max(0f, audio.clip.length - .1f);
                }
            }
        }

        private void AdvanceCanopy()
        {
            var land = region.ApprovedDistrict;
            if (!land || !land.DisplayTerrain || land.FullTreeCount == 0) return;
            var trees = land.CanonicalTreeInstances;
            if (land != indexedLand || !ReferenceEquals(trees, indexedTrees) || woodlandRevision != region.WoodlandRevision)
            {
                indexedLand = land; indexedTrees = trees; woodlandRevision = region.WoodlandRevision;
                var terrain = land.DisplayTerrain; var size = terrain.terrainData.size;
                canopy.Begin(trees, new Vector2(terrain.transform.position.x, terrain.transform.position.z),
                    new Vector2(size.x, size.z), land.Baseline ? land.Baseline.treePrefabs.Length : 15);
            }
            canopy.Advance(2048); // Bound district-entry work instead of scanning a forest each frame.
        }
        private void SetAmbient(int slot, CitySound sound, float gain, float pan = 0f)
        { if (voices[slot].source.clip != clips[(int)sound]) voices[slot].source.clip = clips[(int)sound]; voices[slot].gain = gain; voices[slot].pan = pan; }
        private float Pan(Vector2 point)
        {
            var right = viewCamera.transform.right; var delta = point - ListeningPoint;
            return Mathf.Clamp(Vector2.Dot(delta, new Vector2(right.x, right.z)) / 28f, -.8f, .8f);
        }
        private void Scan(bool ready, bool detail)
        {
            if (sampledDistrict != region.SelectedDistrict)
            { sampledDistrict = region.SelectedDistrict; commercial.Clear(); areasWait = 0f; }
            if (ready) ListeningPoint = region.SoundListeningPoint(rig, viewCamera);
            bool forest = ready && indexedLand == region.ApprovedDistrict && indexedLand && indexedLand.TreesEnabled &&
                canopy.OverCanopy(ListeningPoint) && !region.IsForestRemoved(ListeningPoint);
            float night=NightAmbience(region.CurrentClockHour,region.VisualSeason);
            float nature=ready?.08f*(forest?2f:1f):0f;
            SetAmbient(0, CitySound.Nature, nature*(1f-night));
            SetAmbient(4, CitySound.NightNature, nature*night);
            SetAmbient(1, CitySound.Commercial, 0f); SetAmbient(2, CitySound.Stream, 0f); SetAmbient(3, CitySound.Children, 0f);
            for (int i = 0; i < ActorVoices; i++) { selected[i] = null; selectedGain[i] = 0f; }
            for (int i = ActorStart; i < voices.Length; i++) voices[i].gain = 0f;
            if (!detail) return;
            areasWait -= .1f;
            if (areasWait <= 0f) { areasWait = 1f; region.CollectCommercialSoundAreas(commercial, catalog); }
            float commercialGain = 0f, commercialPan = 0f;
            foreach (var area in commercial)
            {
                var nearest = area.Closest(ListeningPoint);
                float gain = .18f * DistanceGain(Vector2.Distance(nearest, ListeningPoint), 80f);
                if (gain <= commercialGain) continue;
                commercialGain = gain; commercialPan = Pan(nearest);
            }
            SetAmbient(1, CitySound.Commercial, commercialGain, commercialPan);
            SetAmbient(2, CitySound.Stream, .26f * DistanceGain(region.AudioStreamDistance(ListeningPoint, 85f), 85f));
            float childrenGain = 0f, childrenPan = 0f;
            foreach (var emitter in emitters)
            {
                if (!emitter || !emitter.Probe(clock)) continue;
                var position = emitter.transform.position; var point = new Vector2(position.x, position.z);
                bool child = emitter.Sound == CitySound.Children;
                float radius = child ? 45f : emitter.Sound is CitySound.Horse or CitySound.WoodenWagon ? 65f : 32f;
                float gain = (child ? .20f : .24f) * DistanceGain(Vector2.Distance(point, ListeningPoint), radius);
                if (child)
                {
                    if (CharacterScriptHours.For(LotScriptedBehavior.ChildrenAtPlay).Contains(region.CurrentClockHour) && gain > childrenGain)
                    { childrenGain = gain; childrenPan = Pan(point); }
                    continue;
                }
                for (int i = 0; i < ActorVoices; i++)
                {
                    if (gain <= selectedGain[i]) continue;
                    for (int j = ActorVoices - 1; j > i; j--) { selected[j] = selected[j - 1]; selectedGain[j] = selectedGain[j - 1]; }
                    selected[i] = emitter; selectedGain[i] = gain; break;
                }
            }
            SetAmbient(3, CitySound.Children, childrenGain, childrenPan);
            for (int i = 0; i < ActorVoices; i++)
            {
                var emitter = selected[i]; if (!emitter) continue;
                Voice voice = null;
                for (int j = ActorStart; j < voices.Length; j++) if (voices[j].emitter == emitter) { voice = voices[j]; break; }
                if (voice == null)
                {
                    for (int j = ActorStart; j < voices.Length; j++)
                        if (voices[j].gain == 0f && voices[j].source.volume <= .0001f) { voice = voices[j]; break; }
                    if (voice == null) continue;
                    voice.emitter = emitter; voice.source.clip = clips[(int)emitter.Sound]; voice.phase = emitter.Phase;
                    voice.source.pitch = Mathf.Lerp(.96f, 1.04f, emitter.Phase);
                }
                voice.gain = selectedGain[i];
                var p = emitter.transform.position; voice.pan = Pan(new Vector2(p.x, p.z));
            }
        }
        private void OnDisable()
        { foreach (var voice in voices) if (voice != null && voice.source) { voice.source.Stop(); voice.source.volume = 0f; } }
    }
}

# Camera-centered city soundscape — November 21, 2026

Nine user-supplied MP3s are stored in private Resources/Audio/CitySounds. Source hashes were compared with effects-exports after synchronization. No public artwork, catalog or player-save changes are needed. CitySoundsImport provides repeatable import settings: mono22.05kHz preloaded/decompressed actor effects, and stereo44.1kHz compressed-in-memory ambience, with background loading. Runtime encoding is controlled by Unity import settings; the source assets remain MP3.

Mappings:

| Source | Playback |
| --- | --- |
| walking-1 / walking-on-concrete | Different adult pedestrians receive different variants. Only actual movement triggers playback. |
| horse-and-wagon | Moving horses, hitched horses, mounted riders, covered wagons and buggies. Stationary hoof sounds stop. |
| wooden-wagon | Open food/lumber cargo wagons and compatible wooden/cargo-cart IDs; this takes precedence over horse assignment. |
| children-playing | One shared schoolyard layer near visible children,09:00–15:00 only. |
| commercial-area (commericial-louder MP3) | One layer near commercial lots that contain a registered building. Empty commercial zoning is silent. |
| nature | District ambience at L0–L4; regional overview and L5 are silent. Target gain.08 normally and exactly.16 over forest canopy. |
| night-time-summer | Replaces the nature bed at night in summer and fall only. Crossfades18:30–21:00 and05:00–07:00; winter retains the existing nature bed. |
| stream-longer | Near native streams, generated rivers and player-created channels, using indexed world-centerline distance. Lakes/oceans are excluded. |

All proximity layers play at zoom0 and zoom1, plus ground-level ride mode. Actor/area voices retain half gain at L1. Day/night nature beds use full gain at L0/L1/L2, half gain at L3/L4 and zero at L5; riding remains full. The district nature/night bed uses the wider L0–L4 range. Master mute/pause remains authoritative, and loading/menu exit silences playback. Existing AUDIO controls retain their saved volume.

DistrictSoundscape owns thirteen reusable AudioSources: day nature, night nature, commercial, stream and children layers plus at most eight nearby moving actors. Passive DistrictAudioEmitter tags register only character/transport objects; they have no per-frame Update and create no AudioSource. A10Hz scan samples motion and chooses the nearest/loudest actors. Gains and stereo pan ease every frame; out-of-range, idle or disabled actors fade to zero and stop. Independent clip phases avoid synchronized footsteps. Ambience groups use their strongest nearby contribution rather than adding one loop per shop/child/stream.

Distance and panning use the ground beneath the view center, with a saved-focus fallback. Ride mode uses the camera's ground position. This avoids attenuating close city sounds based on a high orthographic camera's actual altitude. Sources use inexpensive2D playback with explicit center-distance attenuation and stereo panning; no per-source spatializer, reverb or occlusion processing is added.

Forest canopy uses a spatial grid built from mature canonical tree positions, with bounded2048-tree indexing steps. It follows district/tree-array/woodland revisions and respects tree visibility/removals without altering forest data. A nearby cluster of at least three mature trees and a canopy center within8m raises nature to2×; isolated street trees do not create a forest layer. Native/purchased/approved mixed forest presentation is retained.

Isolated runtime QA played the actual imported clips through Unity's audio engine and captured a three-second stereo mix (peak.258, RMS.034, no clipping). Checks passed all mappings, both walking variants,501 passive emitters with a twelve-source cap, populated/empty commercial lots, near/far/zoom/ride fading,09:00–15:00 schoolyard sound, stationary actors, native/player stream distance, still-water exclusion,2× canopy nature, and master mute. A500-moving-emitter control stress test measured median.177ms /p95.189ms per scan+fade step, with zero managed allocations across300 iterations. These figures measure controller overhead in the QA fixture, not whole-city FPS or DSP decoding cost. Evidence: November20 ple/outputs/City-Sounds.

All audio state is transient. No source/voice/timing state enters region or lot saves. Missing local clips warn once during initialization and leave other sounds available. Main source and private clips synced outside Play; final main compilation/import verification is recorded in HANDOFF.

2026-11-21: Actual Candlewick disposable-copy verification passed looping pooled voices, full L0/half L1 gain, summer night clip handoff and winter exclusion. The louder commercial MP3 replaces the old recording under its existing resource path/GUID. Actor voice budget remains eight. No player saves changed.

2026-11-21 follow-up: ambient nature zoom policy is shared by day and summer/fall night voices. No actor/area volume policy change.

# Stream rock study — October 31, 2026

## Current accepted state

Current fixture is **Stream Rock Study — Pass 17**, ID `stream-rock-study-pass17-2026-10-31`, northeast district3. The dated sections below preserve the iteration history; their earlier counts and limitations are not the final state. Current landscape combines49 scanned clusters with564 individual stones (ten variants;98 individual bounds above water), muted stream water, narrow varied dirt banks with matte grass blending, and deeper open bed pockets. Displayed center depth is.32–.83m and wet width5.8–8.1m. Core bank relief is24.2% of the pre-lowering profile. Rock shelves remain seated. This treatment is gated to explicit study IDs, not ordinary new-map generation.

Fish are optional and default OFF. The right HUD FISH switch remembers `CityForge.StreamFish.Enabled` separately from saved regions. OFF never initializes/loads the fish population; disabling removes instances, unsubscribes zoom events and stops animation/movement. ON stocks46 bluegills in the current fixture, visible and simulated only at Zoom0, with offscreen culling. Licensed assets are retained privately. Ordinary wildlife remains unchanged.

Camera zoom navigation globally magnifies the closest two settings (displayed0 and1) by1.25; levels2–5 and explicit saved-state restoration remain unchanged. Study base24 yields orthographic sizes9.6/19.2/48/96/192/384.

Latest validation: Unity compilation and normal menu/overview/district landscape checks passed; default OFF, UI enable (46 valid fish), wider-zoom culling, OFF teardown and no respawn at closest zoom passed. All49 existing region files remained unchanged. No player build or sustained CPU/GPU benchmark was run. Private assets are required to run the fixture; a source-only clone cannot reproduce its visuals without them.

## Original study and iteration history

User requested importing the purchased CGTrader river rocks and a region for stream testing, rather than major rivers. This is an explicit, opt-in fixture, not a revision to ordinary MapMagic recipes.

## Private source and preparation

Original downloads are `/Users/joelinstrum/Downloads/rocks`. The supplied Blender file contains one connected open scanned patch, approximately 7.73 × 7.84 × 0.69 m, with 7,654 vertices and 14,645 triangles. Loose-part inspection did not find individual stone objects. Keep it as a patch; do not claim it supplies separate rocks. Texture archive has diffuse and normal maps. Example scene contains a Blender scene and large baked fluid cache; neither is needed for gameplay water.

Preserved source copies live in ignored `SourceAssets/RiverRocks`. Run `Tools/Rocks/prepare_stream_rocks.py` in background Blender with `CITYFORGE_PROJECT` set, then `CityForge.Editor.StreamRockAssetBuilder.Build()` in Unity Edit mode. Both preparation steps refuse to overwrite their existing prepared output. Resources/StreamRocks contains private imported FBX, textures, metre-scale mesh, instanced URP Lit material and asset references. Purchased/generated art stays ignored and must not be published.

## Test region

`StreamRockTestRegion.Id` selects only `stream-rock-study-2026-10-31`, shown as **Stream Rock Study** in Open Region. A 1,024 m explicit shared heightfield uses 2 m geographic samples, four 512 m districts, and a shallow meandering stream. Open-meadow vegetation keeps the test reach visible; the normal approved forest assets/palette are retained. These smaller study dimensions deliberately provide enough regional height samples to resolve a narrow channel. Normal new-map grid8 defaults, stored maps, version12 ocean shelf, and generic river carving are unchanged. This is an authored analytic fixture, not a MapMagic-generated drainage network.

Open Region shows the overview; click northeast district3 to enter the stream reach. The check leaves it framed close to the stream. Rocks use seeded instanced 8 m scanned patches at varied rotation/scale and terrain seating; irregular upper stones can emerge through the flat water. Approved native willow/maple shrubs populate banks and margins. Rock instances are derived on each load. Bank shrubs use the approved native Terrain/seasonal prototype path and become part of this new test district’s vegetation cache. Deterministic position checks avoid duplicating shrubs when restoring that cache. The fixture enables its small plants so the understory remains visible. No existing region’s trees are modified. The fixture narrows the existing soil-bank blend; normal regions retain their original values. Water uses the approved native Crest river/current/glint integration without global water, camera, lighting or quality changes.

`CityForge.Editor.StreamRockStudyChecks.Run(output)` creates this unique local save only if absent and exercises normal saved-region loading, deterministic field/save equivalence, displayed depth/width and rock/shrub presence. Existing saves are not replaced. It captures actual Unity frames for visual review. A matching existing study is reopened; a modified existing study causes a field mismatch failure rather than being overwritten.

## Limits

Patches repeat a connected scan and can reveal repeated arrangements at close view. Individual closed stones, physical flow around obstacles, foam/wakes, stream-specific water styling, performance qualification remain further work. Current water is the accepted flat river renderer with animated shading/current, not the vendor fluid-cache simulation. No build or sustained performance claim follows from editor checks.

## Current validation

Unity recompile passed with zero errors/warnings. Normal menu → saved study overview → northeast district entry passed. Explicit field generation is deterministic and the saved field matches exactly. Actual displayed stream samples measure 0.31–0.54 m center depth and 4.5–6.2 m wet width. District has 80 scanned patches (58 with bounds extending above water) and 45 seeded bank shrubs in the native Terrain prototype path. Rock/submerged-bed/shore captures were visually inspected at zoom1/2. Shrub placement counts are verified; their close visual presentation still needs user feedback. Existing11 saved-region files remain byte-identical. Whitespace check passed. No commit, PR, release build or sustained performance qualification performed.

Current Unity view is Play mode, northeast district of Stream Rock Study, summer/clear, heading45, zoom1. To reopen later: Open Region → Stream Rock Study → northeast cell. The new test save is kept locally; private artwork and screenshots remain outside public source control.

## Pass 2 — user-directed width, banks, coverage and color

The user accepted the critique of continuous rock coverage and saturated blue shallow water, requesting approximately25% wider streams and25% narrower banks. A new local **Stream Rock Study — Pass 2** (`stream-rock-study-pass2-2026-10-31`) preserves the original study save for comparison. `Applies` recognizes both IDs; `Revised` gates only the second pass. `Create(false)` reproduces the original study geography. No ordinary generation version changes.

Pass2 targets1.25× the original displayed wet width and0.75× the exposed slope. Cross-section coefficients compensate for the2m shared field and monotone interpolation; measured results, rather than raw profile coefficients, determine acceptance. Soil-bank paint reaches and the fixture-owned baseline shoreline-width parameter also shrink to75%. The2m shared samples and native interpolation mean displayed measurements are approximate. Depth, route, current speed, lighting and inspection camera remain unchanged.

A deterministic multi-frequency cluster selection leaves longer gaps between rock patches; surviving patches have a wider size range toward smaller footprints. The source is still the one connected scan, not extracted closed individual rocks. The approved Crest river material is cloned by its existing owner; after ordinary configuration, Pass2 alone uses absorption(.35,.28,.24) and muted gray-green scattering(.12,.16,.135). Normal rivers and oceans keep their established profiles. Reflections, evolving glints and downstream current remain; no obstacle-fluid simulation is added.

Validation and screenshots for this revision are recorded in the current chat outputs/stream-pass-2. First-pass results above are historical.

Final Pass2 verification: Unity compilation passed with zero errors/warnings; deterministic generation, authoritative file round trip and normal menu→district load passed. Actual displayed samples:5.5–7.7m wet width and0.32–0.54m depth; mean wet width1.221× first pass and exposed slope0.800× first pass. Soil-bank paint widths are0.75×. The2m shared sampling limits exact percentages across individual cross-sections.49 patches remain vs80,27 with bounds above water; muted material values checked. Final zoom1/2 captures visually inspected. All13 pre-existing save/backup files remain byte-identical. No commit/publication, player build or performance claim.

## Pass 3 — teal contrast, softer banks and edge rocks

User approved the recommendation to retain Pass2 width while restoring muted teal/reflection contrast, reducing pale bank uniformity, and seating smaller rocks along alternating edges. New local ID `stream-rock-study-pass3-2026-10-31` preserves the previous two studies. Shared heights, water-distance field, nominal width/depth and route are identical to Pass2.

Pass3-only water absorption(.72,.50,.38) and scattering(.045,.105,.11) reduce the pale cloudiness while restoring a restrained teal. Planar reflection intensity rises.25→.35; ripple/glint/current speed, global lighting and ocean materials stay intact. Owned per-district material only.

Existing cluster gaps remain. Surviving scanned patches use narrower footprints, align approximately lengthwise, alternate banks in groups, and bury their bases. Positions are conservatively offset so projected patch bounds leave at least1m each side of the centre route (sampled along the curved centreline). This is a presentation clearance, not a physical fluid-navigation guarantee or new current simulation. Scan source is still one connected surface, not individual closed rocks. Terrain can hide part of each patch, especially on the outer bank.

For this ID only, the existing soil blend is reduced and varied spatially on exposed banks, allowing more native grass through. The underlying profile and width are retained; no new terrain-material texture or color edits. Previous study IDs retain their prior water/rock/bank treatments. Validation/captures are in the current chat outputs/stream-pass-3.

Pass3 verification: Unity compile passed with zero errors/warnings. Deterministic field and authoritative save round trip, normal menu→district load,49 patch placements, muted teal/reflection settings, and at least1m conservative centre clearance per side passed. Actual depth0.32–0.54m and wet width5.5–7.7m match Pass2. Both saved height and water-distance arrays were independently compared to the Pass2 file and are exactly equal. Zoom1/2 frames visually inspected: grass blends into banks, central water stays open, smaller partially buried patches follow edges. Patch bounds can extend above water while parts are buried in terrain;49 bounds-above-water is not a count of individually visible stones. Existing save/backup hashes preserved. No commit/publication or performance/build qualification.

## Pass 4 — restored clusters plus individual stones

User rejected the narrow edge-rock appearance in Pass3 and requested clusters plus new individual rocks in the gaps. New source downloads are `/Users/joelinstrum/Downloads/river-rocks` (River_Rocks01_FBX.zip and Textures.zip). Preserved private extracted source: SourceAssets/IndividualRiverRocks. The source FBX contains a Rocks_low mesh with ten disconnected stone components, plus a separate scene/backdrop mesh. Only the ten stones are prepared; scene/HDRI/displacement are not imported into gameplay. Ten variants total4,992 triangles (294–770 per stone), retaining source UVs. Preparation lays each on its shortest axis, sets base origin and normalizes footprint to1m for deterministic physical scaling.

`Tools/Rocks/prepare_individual_rocks.py` exports private Resources/IndividualStreamRocks/IndividualStones.fbx using background Blender. `IndividualStreamRockBuilder.Build()` runs once in Unity Edit mode, imports2K color/normal/roughness, packs inverse roughness into a linear smoothness-map alpha with zero metallic, and creates ten private meshes with a shared instanced URP Lit material. Existing prepared output is protected against accidental replacement. Commercial textures/meshes and generated assets remain ignored.

New save **Stream Rock Study — Pass 4** (`stream-rock-study-pass4-2026-10-31`) retains Pass3's water, banks, width/depth and shared fields. Fuller uniformly scaled scan clusters from Pass2 are restored; rejected compressed edge fragments remain only in the preserved Pass3 comparison. `IndividualStreamRockScatter` places the new stones only on submerged terrain outside conservatively expanded cluster rectangles, with randomized variant/rotation/size and partially buried bases. Most stones are small submerged cobbles, with fewer larger protruding rocks. Draws use instanced batches, not individual GameObjects/colliders. No obstacle-fluid simulation or new normal-map water treatment.

Earlier study IDs retain their own treatments and files. Current verification/captures belong in chat outputs/stream-pass-4. User feedback is required before calling the new appearance approved; no commit/publish authorization.

Pass4 validation passed: Unity compile zero errors/warnings; normal saved-region→district loading and authoritative deterministic field round trip.49 full patches plus564 individual stones; ten variants actually present in draw batches, zero expanded-cluster-footprint overlaps.98 individual bounds extend above water (not a guarantee each stone is fully visible). Actual displayed depth0.32–0.54m and width5.5–7.7m retained; saved height/water-distance arrays exactly equal Pass3. Final zoom1/2 rendered frames visually inspected. All15 pre-existing save/backup files byte-identical. No commit/publication, build or sustained performance claim.

## Pass 5 — bank blend and shoulder rounding

User explicitly approves Pass4's cluster/individual-rock mix and asks to remove green triangular-looking banks and blend them into grass. Preserve that rock mix. A new Pass5 study (`stream-rock-study-pass5-2026-10-31`) retains the same canonical shared height/water-distance fields and water material. Earlier comparisons remain preserved.

For this ID only, `StreamBankBlend` rounds convex dry upper shoulders using a bounded local height average. The first metre outside each precise wet boundary remains unchanged; wet terrain is untouched. Changes fade out within8m of the boundary and only lower upper dry shoulders. This is deterministic district-owned presentation/collision geometry, not a rewrite of saved geography. Approved rock placement is checked at49 patches/564 individual stones,98 individual bounds above water.

Bank shore-distance sampling increases from257 to1025 samples over the512m district. Soil weighting now uses a continuous wet-to-dry feather with spatial variation, replacing the abrupt positive/negative-distance multiplier. Native grass textures show through the edge progressively. The global terrain shader, original layers/palette and other regions are unchanged. The physical dry shoulder can become gentler while the channel width/depth stay fixed; the old Pass2 exposed-slope-width assertion is therefore not applicable to this refinement.

Current validation and rendered evidence are in chat outputs/stream-pass-5. Previous passes' rock rejection notes do not apply to the now accepted Pass4 mix.

Pass5 verification passed compilation (zero errors/warnings), deterministic field/save round trip, normal menu→district entry and exactly49 clusters/564 singles/98 single bounds above water. Actual depth0.32–0.54m and wet width5.5–7.7m retained. Saved canonical height/water-distance arrays equal Pass4. Dry shoulder lowering reaches~0.31m. Comparing wet heights directly to analytic source initially showed the existing Terrain16-bit quantization; a corrected comparison against a temporary original TerrainData at the same resolution yielded maximum wet-height difference0. Shore field1025 and pixel error.25 verified. Final zoom1/2 frames visually inspected; all16 pre-existing save/backup files unchanged. No commit/publish/build/performance claim.

## Pass 6 — damp brown dirt banks

User requested a narrow damp-earth band fading irregularly into grass while retaining Pass5 shape and the approved rock mix. New study ID `stream-rock-study-pass6-2026-10-31` preserves the earlier candidates. Soft-bank geometry, wet field,49 clusters/564 individual stones and water material are unchanged.

Only this district ID adds an owned clone of the existing RiverBeds/RichSoil layer (Soil01), with muted brown diffuse remap(.38,.29,.20), low specular/smoothness and existing normal texture. Original layers/materials and surrounding meadow/roads retain their textures. A dedicated extra terrain channel paints the new soil near the wet margin, with a varied~1.5–2.8m feather and grass tongues. Underwater bed remains on its prior layers except a short wet-edge feather. Layer weights recover their base proportions when restoring a dirt-bank cache, avoiding cumulative soil buildup.

This is a texture-only successor to Pass5. Current evidence belongs in chat outputs/stream-pass-6. The bank appearance is a candidate pending user review; the mixed rocks remain approved.

Pass6 verification passed compilation (zero errors/warnings), normal saved-region→district entry, deterministic field round trip, dedicated owned dirt layer and approved rock counts. Soil weight peaks~.98 with sampled weight-sum error≤1/255 (terrain-map quantization). Original RichSoil tint remains(.44,.35,.26); owned layer alone uses the damp-brown remap. Canonical height/water-distance arrays exactly equal Pass5; same soft-bank geometry path and measured water dimensions. Final zoom1/2 frames visually inspected; all17 pre-existing save/backup files unchanged. No commit/publication, build or performance qualification.

## Stream study Pass7 — October31, 2026

User liked the dirt direction but requested removing scorched contrast and repeated rectangles. Current candidate **Stream Rock Study — Pass 7** (`stream-rock-study-pass7-2026-10-31`) replaces the short-repeat photographic bank diffuse with owned, seamless, low-contrast procedural earth grain (19.7m tile; muted brown .40/.32/.23; no soil normal map). Native grass, source RichSoil and global terrain shader remain unchanged. A study-only two-dimensional upper-shoulder filter reduces angular shading; old displayed heights at/below12.25m are protected, with a feather above that level. Sampled protected height difference0 across2708 points; maximum dry adjustment~.41m. Rock seating uses a private owned copy of the pre-refinement quantized heightmap to retain approved placements; shrubs follow the displayed terrain. This is presentation geometry; canonical saved fields remain unchanged. Previous study IDs preserve their earlier looks. Candidate awaits visual feedback.


Compilation, normal menu/district loading, deterministic field/save round trip and approved rock checks passed:49 clusters/564 individuals/98 individual bounds above water, depth.32–.54m and width5.5–7.7m retained. Actual zoom1/2 captures inspected in current chat outputs/stream-pass-7; previous saves/backups unchanged. Canonical fields equal Pass6. Left Pass7 open at heading45/zoom1, summer/clear. No commit/PR/build/performance qualification.


## Stream study Pass8 — October31, 2026

User approved the water/rocks and rejected the continuous marker-like bank strip, requesting dirt that blends into grass plus higher/lower bank reaches. Current candidate **Stream Rock Study — Pass 8** (`stream-rock-study-pass8-2026-10-31`) varies both banks independently with broad seeded low-margin reaches. Low shoulders approach water level, alternating with existing higher shoulders. Wet/lower geometry at or below12.05m stays exact; terrain collider follows displayed terrain. Canonical saved height/water fields remain equal to Pass7. Preserve prior study IDs and appearance.

Bank paint now breaks up both the dirt overlay and the continuous pale-soil underlay, with grass reaching the water in some reaches and irregular, softly feathered dirt pockets elsewhere. Noise operates at multiple scales; lower banks favor grass. Submerged bed keeps the previous soil treatment so the bank blend does not recolor shallow water. Previous procedural earth grain/source soil/native grass/global shaders remain unchanged. Approved rock seating remains on the private pre-refinement quantized heightmap. Bank appearance is a candidate awaiting user review, not an approved global generator change.


Compilation, deterministic save round trip, normal menu/district loading and rock checks passed:49 clusters/564 individual stones/98 bounds above water. Depth.32–.54m; width5.5–7.8m. Displayed near-bank samples at nominal wet half-width+1m range12.223–12.943m (water12m), on independently varied sides. Dirt weights at those samples0–.396;239/370 samples below.05 and25 above.25. Layer-weight sum error≤1/255. Earlier saved height/water fields equal, protected lower height samples unchanged. Final zoom1/2 captures inspected in current chat outputs/stream-pass-8. All39 pre-existing save/backup/support files unchanged. Pass8 open at45 degrees/zoom1, summer/clear. No commit/PR/build/performance qualification.


## Stream study Pass9 — October31, 2026

User liked Pass8's direction but requested substantially narrower bank footprints. Current candidate **Stream Rock Study — Pass 9** (`stream-rock-study-pass9-2026-10-31`) keeps the immediate.75m dry edge fixed and compresses the outer dry cross-section by3, while retaining Pass8's independent high/low reaches. Dirt and underlying soil paint also use doubled positive shore distance so their widths narrow together. Submerged paint and wet vertices remain unchanged; approved rock seating stays on the same pre-refinement quantized heightmap. Canonical saved fields match Pass8; earlier study IDs preserve their treatments.

Sampled dry-shoulder footprint (12.1–13.4m) measures.429× Pass8; maximum wet-height sample difference0. This is a scoped study presentation, not ordinary map generation. Candidate awaits user review.


Compilation, normal saved-region/district loading and checks passed. Actual displayed exposed-slope metric.724 vs Pass8 1.582 (~.458×, about54% less footprint); width5.5–7.8m/depth.32–.54m retained. Same49 clusters/564 individuals/98 individual bounds above water. Actual zoom1/2 frames inspected in current chat outputs/stream-pass-9. All40 pre-existing save/backup/support files unchanged. Left Pass9 open at45 degrees/zoom1, summer/clear. No commit/PR/build/performance qualification.


## Stream study Pass10 — October31, 2026

User accepts occasional steep/shaded banks and requested alternating steep sections and gentler pullbacks. Current candidate **Stream Rock Study — Pass 10** (`stream-rock-study-pass10-2026-10-31`) smoothly varies dry-bank compression1.25–3 along independent17m-scale reaches on each side. Retains Pass9's protected immediate.75m edge, narrow dirt/soil paint, earlier independent low/high margins, unchanged water and approved rock seating. Previous study IDs preserve appearance; no ordinary recipe changes.

Sampled dry shoulder footprint.631× Pass8, wet-height sample difference0, tight-bank control reaches full0–1 range. Candidate awaits user visual review.


Compilation, deterministic field/save round trip, normal menu/district loading and approved rocks passed:49 clusters/564 individuals/98 bounds above water; depth.32–.54m and width5.5–7.8m unchanged. Actual exposed-slope metric1.026 vs Pass8 1.582 (~.649×); narrower overall with selected gentler pullbacks. Final actual zoom1/2 captures inspected in current chat outputs/stream-pass-10. All41 pre-existing save/backup/support files unchanged. Left Pass10 open at45 degrees/zoom1, summer/clear. No commit/PR/build/performance qualification.


## Stream study Pass11 — October31, 2026

User supplied a close-up of the approved bank section and requested deeper water without touching banks. Current candidate **Stream Rock Study — Pass 11** (`stream-rock-study-pass11-2026-10-31`) lowers open center-bed pockets by up to.30m, tapering to zero toward shallow margins and beneath existing rocks. StreamBedDepthStudy runs after the unchanged Pass10 terrain/rock placement; conservative scan-footprint and individual-stone shelves preserve seating for all approved rocks. No water-level/material, bank shape/paint or rock-transform changes. Crest depth probes repopulate after SetHeights so absorption/depth reflects the deeper terrain. Canonical stored fields remain equal to Pass10; the depth pass derives displayed terrain/collider on each fixture load, with no accumulation. Previous studies preserve appearance. Candidate awaits visual feedback.

Compilation, deterministic save round trip, normal menu/district loading and depth checks passed.4,898 vertices lowered, maximum.30m; actual dry-height readback difference0m. Displayed center depth.32–.83m vs Pass10.32–.54m; width5.5–7.8m and exposed-slope metric1.026 unchanged. Same49 clusters/564 stones/98 individual bounds above water. Actual zoom1/2 frames inspected in current chat outputs/stream-pass-11. All42 pre-existing save/backup/support files unchanged. Pass11 open at45 degrees/zoom1, summer/clear. No commit/PR/build/performance qualification.


## Stream study Pass12 — October31, 2026

User explicitly requested80% lower stream banks. Current candidate **Stream Rock Study — Pass 12** (`stream-rock-study-pass12-2026-10-31`) scales dry bank relief about the fixed12m water plane to20% of Pass11, fully within3m of the old wet edge, feathering into neighboring meadow over3–10m to avoid a new outer ridge. Narrow paint coverage, earlier slope/side variation, approved rock seating and deeper bed are retained. Wet vertices stay unchanged; lowering adjacent dry vertices changes interpolated wet-edge position slightly. Earlier study IDs preserve appearance; ordinary generation unchanged.

Compilation, normal menu/district loading and checks passed.148 profile samples meet20% target within.000001m; actual near-bank samples~.049–.264m above water. Same4,898 bed vertices lowered by up to.30m; depth samples.32–.83m. Wet width5.8–8.1m vs prior5.5–7.8m due terrain interpolation at the lowered edge. Same49 clusters/564 individuals/98 bounds above water. Actual zoom1/2 captures inspected in current chat outputs/stream-pass-12. All43 pre-existing save/backup/support files unchanged. Left Pass12 open at45 degrees/zoom1, summer/clear. Candidate awaits user visual review; no commit/PR/build/performance qualification.


## Stream study Pass13 — October31, 2026

User liked Pass12, requested10% higher banks and removal of bright white bank patches. Current candidate **Stream Rock Study — Pass 13** (`stream-rock-study-pass13-2026-10-31`) raises core relief from20% to22% of the pre-lowering bank profile (110% of Pass12, not10 percentage points). Outer meadow feather remains. Live diagnostic confirmed white streaks were specular soil glare: procedural RGBA earth alpha255 was consumed as terrain smoothness despite the layer slider. Pass13-only earth alpha8/255 makes it matte while retaining identical RGB/grain and coverage. Old study IDs retain their old textures/profile. Water material/glints, rock seating and deeper bed unchanged.

Compilation, normal menu/district loading, deterministic save round trip, rock/depth checks passed.148 profile samples meet110% of Pass12 within.000002m. Depth.32–.83m, width5.8–8.1m, same49 clusters/564 stones/98 bounds above water. Final zoom1/2 captures inspected: bank glare removed, water glints retained. All44 pre-existing save/backup/support files unchanged. Pass13 open at45 degrees/zoom1, summer/clear. Candidate awaits user review; no commit/PR/build/performance qualification. Outputs in current chat outputs/stream-pass-13.


## Stream study Pass14 — October31, 2026

User authorized another10% increase over Pass13 bank height. Current candidate **Stream Rock Study — Pass 14** (`stream-rock-study-pass14-2026-10-31`) uses24.2% of pre-lowering core bank relief (110% of Pass13's22%). Retains matte alpha8/255 soil, existing footprint/side variation, deeper bed, approved water settings and rock seating. Earlier studies preserve their treatments.

Compilation, normal menu/district loading, deterministic save round trip and checks passed.148 profile samples meet110% of Pass13 within.000002m. Same depth.32–.83m, width5.8–8.1m,49 clusters/564 individual stones/98 bounds above water. Actual close frame inspected; zoom1/2 captures in current chat outputs/stream-pass-14. All45 pre-existing save/backup/support files unchanged. Pass14 open at45 degrees/zoom1, summer/clear. Candidate awaits review; no commit/PR/build/performance qualification.


## Stream study Pass15 — bluegill stocking, October31, 2026

User requested trying the supplied fish from `/Users/joelinstrum/Downloads/fish`. Current candidate **Stream Rock Study — Pass 15** (`stream-rock-study-pass15-2026-10-31`) retains Pass14 landscape/water/rocks and adds48 bluegills in six groups in deeper open reaches. Exact-ID trial only; existing DistrictWildlife/TemperateWildlife and ordinary rivers are unchanged. Fish are24–32cm long, swim at.13–.25m/s, remain submerged and use checked open-water segments. Conservative cluster rectangles and individual-stone shelf masks keep paths away from rocks; terrain depth must exceed.44m. No per-fish collider or gameplay population persistence; deterministic stocking derives on fixture load and is destroyed with the district.

Private original BLUEGILL.fbx and textures archive preserved in ignored SourceAssets/Bluegill. Original supplied model has34 bones and no animation actions. Tools/Fish/prepare_bluegill.py normalizes/applies rig transforms and derives a40-frame/30fps looping spine/tail cycle; StreamFishAssetBuilder packs supplied color+fin alpha, imports normal, creates URP cutout material and loop controller. Prepared assets remain private Resources/StreamFish. Runtime prefab is BluegillFish (distinct from Bluegill.fbx resource name); wrapper corrects forward/up axes, Animator remains on model child. Original source/reflect/subsurface textures preserved; runtime uses color/alpha/normal with restrained fixed gloss. Do not publish artwork or generated FBX/textures/prefab/controller.

Compilation, ordinary fixture menu/district loading and landscape checks passed: same49 clusters/564 stones/98 individual bounds above water; depth.32–.83m/width5.8–8.1m. Fish validation:48 spawned,48 moved,0 invalid habitat/rock positions;48 looping animators, spine bend3.88 degrees across sampled cycle; maximum renderer top11.881m below fixed12m surface. Actual asset preview and in-water close frame inspected. Current chat outputs/stream-fish includes checks/captures. All46 pre-existing save/backup/support files unchanged. Pass15 left open in a close bluegill view (heading45, base size6, zoom1, near z165), summer/clear. This is a visual/motion trial, not a performance or whole-river ecology qualification. No commit/PR/build/publish.


## Stream study Pass16 — fish visibility, October31, 2026

User requested50% larger bluegills swimming a little nearer the surface. Current candidate **Stream Rock Study — Pass 16** (`stream-rock-study-pass16-2026-10-31`) uses1.5× fish length (.36–.48m), with size-dependent center depth max(.18,length*.42+.035)m vs Pass15's.24m. Larger fish remain submerged; conservative cluster path clearance increases.26→.39m. Existing stone clearance and habitat/path checks remain. Crowded stocking slots are skipped;46 fish fit versus48 previously. Old Pass15 retains its size, depth and paths. Water clarity/materials, banks, bed and rocks unchanged.

Compilation, normal menu/district load and landscape checks passed.46 fish moved,0 invalid habitat/rock positions, measured sizes.362–.480m, maximum renderer top11.951m below12m surface, minimum center depth.187m. Actual normal and close fish frames inspected in current chat outputs/stream-fish-pass-16. All47 pre-existing save/backup/support files unchanged. Left normal study camera at heading45/base size24/zoom1, centerz170 for visibility comparison; close image uses base size6. No commit/PR/build/performance qualification; candidate awaits user feedback.


## Stream study Pass17 — closest-only fish and closer camera, October31, 2026

User requested fish only at closest zoom and25% stronger zoom for the two closest settings. User explicitly clarified UI levels0 and1 (not displayed level2). CityForgeCameraController.SetZoomLevel now divides the old orthographic size by1.25 only for0/1; levels2–5 keep old sizes. Global zoom navigation changes; explicit saved camera-state restoration remains authoritative and files are unchanged.

Current **Stream Rock Study — Pass 17** (`stream-rock-study-pass17-2026-10-31`) retains Pass16 landscape/46 larger fish. StreamFishStudy listens to ZoomChanged: only0 activates fish roots and enables the movement component;1–5 deactivate every fish and disable movement updates. At0, offscreen fish movement skips Renderer.isVisible=false; Animator.CullCompletely stops offscreen rig evaluation. Event unsubscribes on district destruction. This visibility/performance policy also applies when opening earlier bluegill-study IDs; their size/depth variants remain intact. No global wildlife-library changes.

Compilation/normal menu+district loading/landscape checks passed. Six-level assertions passed for standard study base24: sizes9.6/19.2/48/96/192/384, fish active46 only at0,0 active and component disabled at1–5. Fish positions stayed frozen while hidden; returning0 resumed46 active fish,8 on screen,7 moved,0 invalid habitats. Closest frame inspected in current chat outputs/stream-fish-pass-17. All48 pre-existing save/backup/support files unchanged. Left Pass17 at closest0/heading45/base24/summer-clear. No FPS/GPU benchmark, commit/PR/build/publish; candidate awaits feedback.

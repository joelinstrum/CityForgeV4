# CityForge V4 — main development handoff

Updated 2026-10-20. Read this before changing terrain, foliage, map creation, or rendering.

## Start here — current accepted state, October 19

This summary supersedes historical four-district and earlier palette descriptions below.

- New wizard regions have 64 districts (8×8); legacy saves/test regions retain their original grid. Main-menu opens the relief overview; clicking a cell loads only its detailed native meadow biome behind the preloader, at zoom5.
- Wizard seed, topography, rivers/streams, moisture, vegetation and size influence shared geography/ecology. Era/period are metadata only. Wizard oceans are not implemented; coastline tests remain separate.
- Current new geography recipe is v5: MapMagic Noise → CityForge range constraints → MapMagic erosion, then connected river carving and biome composition. Generation/composition run on workers. Stored heightfields remain authoritative.
- Accepted regional appearance: midpoint olive greens (grass 0.43/0.47/0.285; woodland 0.265/0.325/0.18), warm exposed rock, deep-blue rivers with NO river border. New thin tan beaches apply only to semantic ocean outlines. The map now has dark earth slab sides; framing includes their bounds. Overview mesh513², atlas1025², presentation-only height exaggeration1.25×.
- Preserve the native MeadowRuntimeBaseline / MeadowDistrictLandscape / SeasonalTreeLibrary. Retired prototype trees stay absent. Both Silver Maples are enabled locally (mask3); six optional oaks off. Firs remain restricted to Hills and Mountain Test. Public clone excludes licensed assets.
- Preserve VSync, temporary movement quality reduction, close pan factor2.5× at zoom1–3, and district entry zoom5. Preserve approved ocean surf and clock0.45 (~17seconds).
- Save/load preserves creation recipe, grid and one last detailed district cache. Independent edited-district persistence is still future work. Delete on Open Region archives recoverable saves locally.
- Latest RegionalReliefChecks passed shader/slab checks, all64 picks, shared edge, native seasons, district isolation, cache and temporary save round trip; coastal slab preview inspected. Earlier MapMagic/district-loading checks passed before the last presentation refinements. Do not describe them as freshly rerun on every palette change.
- District river repair (October 19): v5 detailed rivers now use a stationary bounded calm surface reaching the saved terrain shoreline, with native soil banks. October20 adds optical lengthwise downstream currents in the bounded shader, following centerline bends; no displaced waves or shore-lapping foam. Runtime water mapping/capture uniforms now have hidden shader properties so Unity reload retains them. The first current trial was too subtle; river-only scene-depth absorption makes underwater animals more readable. User rejected the painted-current appearance even at27.5% strength and supplied a fine-ripple river reference. The analytic-wave trial was rejected as television static. Latest provisional river treatment uses the existing local Crest normal texture with mipmaps, two-phase coherent downstream advection, broader low-frequency detail and restrained reflection glints. Four variants were inspected at gameplay zoom1/2; the broader variant was selected. User reported it is a lot better and requested faster movement; river advection is now doubled from0.275m/s to0.55m/s, with appearance unchanged. Future work should address river reflections/lighting and riverbed/shore presentation, rather than claim more small shader tweaks will achieve the image. See the latest visual-review notes in Regional_Relief_Map.md. Ocean, lake and pond styles are unchanged. Orthographic cameras retreat to avoid terrain clipping and compensate forest culling distances. No regional geography/palette or ocean settings changed. Focused checks and visual review are described in Regional_Relief_Map.md; user reported the river repair is much better.
- District slope blending (October 19): v5 detail now uses shared monotone cubic heights, terrain pixel error1, matching native collision, vertically re-seated cached trees and finely tessellated road ribbons. Saved heights, approved materials and regional relief remain intact. User approved the smoother result.
- Summer maples (October 20): both Silver Maple summer leaf tints changed from RGB(.25,.45,.28) to (.20,.32,.25) for darker, more muted forest green. Only six GreenLeaf materials and the import palette changed; autumn/winter, bark and other species remain intact. User approved the darker result.
- Temperate wildlife (October 20): new-region Animals option defaults to Temperate wildlife; None disables it. Existing saves default off and can opt in through the district ANIMALS button; Save Region persists the choice. Ten locally imported Animal Pack Deluxe species (including user-requested bank/shallow-water crabs) plus seven Living Birds species (ground and flight variants) use habitat-limited placement and safe idle/walk/swim loops. Farm, tropical and marine animals are excluded. WildlifeChecks passed new/legacy regions, wizard controls, habitat/movement, animation, deterministic placement and temporary save round trips. Player saves were unchanged. Missing legacy creation recipes now remain null instead of acquiring current grid/scale defaults. See Documentation/03_Technical/District_Wildlife.md.
- Remaining map work: wizard coastline integration, more natural confluences and drainage networks, settlement data/markers, and independent per-district edited storage. Wildlife now renders only at close zoom0–2, with population count and FIND ANIMAL inspection control; script reload retains the population list. Land wildlife density is now about3× the initial trial, plus a larger bird population in openings and above woodland. Installed packs contain no squirrels. Further wildlife visual feedback is pending.

Work directly in the existing project and preserve local assets/work. This public repository is source-only. Never publish commercial art/packages or player saves. Read AGENTS.md before editing. No automatic save regeneration.

## Current geography generator — MapMagic2 trial (October 19)

New recipes default to **version 5**, using MapMagic2 Noise → CityForge range/valley constraints → MapMagic2 Erosion for the shared regional elevations. The earlier version4 Noise → Levels → Erosion trial is preserved for legacy recipes. See `Documentation/03_Technical/MapMagic_Regional_Geography.md`. This supersedes the earlier custom-noise generator as the default for new maps, while version1–3 maps and all stored heightfields remain intact. MapMagic does not own terrain rendering, trees or materials: retain RegionReliefMap and the approved native district biome. Generation runs on a worker behind the creation preloader; districts still use their own preloader and start at zoom5. Existing river routing is retained and is not MapMagic-generated drainage. The new terrain is a trial awaiting visual feedback. MapMagic/provider and full expanded-biome integration checks passed. Do not rerun the historical MapMagicMeadowStudy authoring tool as a gameplay baseline.

## Current regional architecture — October 19, 2026

**New regions now use 64 districts (8×8), with a separate relief-map overview.** This supersedes the earlier four-district/full-meadow-overview descriptions below. Main-menu loading starts at the regional map; click a cell for its approved detailed biome. Only that district is rendered. Older saves retain their original 2×2 layout and dimensions. Medium new regions are 8,192m across, with 1,024m districts. Keep the approved native biome inside districts; do not put individual trees back into the regional map.

See `Documentation/03_Technical/Regional_Relief_Map.md` for grid/save contracts and limits. City/town markers await actual settlement data. Current persistence retains one last district cache; independent edited district storage is a future requirement before multi-district construction. RegionBiomeChecks and RegionalReliefChecks passed; no player saves were rewritten. Both Silver Maples are enabled locally, oaks remain off, VSync and movement quality remain in place. Ocean presentation is unchanged.

## Approved biome and appearance

The current biome is the approved **temperate native meadow / poplar woodland**, rendered by `MeadowDistrictLandscape`, with the user-authorized maple/oak forest mix described below. It is not the early prototype forest.

Authoritative local assets:

- `Assets/CityForge/Resources/Region/MeadowRuntimeBaseline.asset`: approved terrain material, terrain layers, vegetation prototypes, lighting, camera and water templates.
- `Assets/CityForge/Art/Environment/MeadowNativeURP/Integration/Polish/Materials/Natural Transitions Terrain.mat`: approved meadow terrain material. Preserve its grass colors and texture treatment.
- `Assets/CityForge/Art/Environment/MeadowNativeURP/Integration/Polish/Layers/`: approved meadow soil, quiet grass and leaf-litter terrain layers.
- `Assets/CityForge/Art/Environment/MeadowNativeURP/Showcase/Flora/`: approved scanned poplars, young poplars, grey willow and maple bushes, plus meadow flora.
- `Assets/CityForge/Resources/Seasons/SeasonalTrees.asset`: matching Summer, Fall and Winter prototype arrays. Use `SeasonalTreeLibrary` and `SetSeason`; retain tree positions and prototype indices.

Generated version-2 biome regions use these assets in BOTH the full-region overview and each district. Overview and district views must not silently switch to prototype trees or ground. Existing test maps retain their established terrain/water behavior. If an unsupported older view lacks approved foliage, leave its retired trees absent rather than restoring the old forest.

## Retired tree: do not restore

The old runtime forest selected `Trees/CityForgeBroadleaf` through `ForestPresentation.json`. Its prefabs have been moved outside Unity to `SourceAssets/RetiredPrototypeTrees/`; that archive is local and excluded from the public source repository. `PrototypeRegionView.BuildTrees` and `AddTrees` are intentionally inert, and `DistantTreeRenderer` is an inert compatibility shell. Old saves do not authorize restoring these trees. Historical tree importers/evaluation scripts are not the current visual baseline and must not be used to select gameplay foliage.

Do not “fix” missing approved assets by reconnecting `ForestPresentationSettings`, prototype broadleaf/elm packs, or distant prototype silhouettes. Report missing assets and restore the approved local baseline instead. The public repository is source-only; a fresh clone lacks the licensed assets.

## Current map generation

One seeded continuous 513 × 513 heightfield is divided into Southwest, Southeast, Northwest and Northeast quadrants. Wizard seed, topography, rivers and streams affect geography; temperate moisture and vegetation choices affect the continuous ecology field. Medium creates four 1,024 × 1,024 m districts. See `Documentation/03_Technical/Region_Generation.md` for algorithm, persistence and scope.

Checks cover option influence, four shared district edges, determinism, recipe save/load, approved overview materials/seasonal trees, transitions and camera state. River visibility in generated regions still needs visual acceptance; an active water component is not sufficient evidence.

## Preserve completed work

- Lakes/ponds, rivers, grass hills, textured mountains, larger districts and seasonal trees are approved foundations.
- `I` toggles isometric / lower camera views.
- Grass/quilt experiments were rolled back. Do not reapply experimental shader patches from old outputs.
- Oceans are complete for now: preserve the Coastline Test footprint (~70% land / 30% ocean), brighter deep blue in isometric view, beachward waves and advancing/spreading/receding/fading shoreline foam. Keep ocean clock speed 0.45 and surf cycle about 17 seconds.
- Work directly in the existing project; preserve unrelated work. Do not publish third-party assets or Crest source. Do not push without user authorization.

## Verification workflow

In the Unity editor, write `integration` to `Library/RegionBiomes.request` to run `RegionBiomeChecks`. Inspect the rendered overview and a district as well as the report. Check Summer/Fall/Winter and return-to-overview transitions. Legacy tree regressions must assert no `RandomTrees`, no attached distant-tree renderer, and no retired broadleaf Resources prefabs.

## Saved region deletion

The main Open Region list has a Delete action on each row. First click shows the named-region confirmation; Cancel retains the save, Confirm removes it and refreshes the list. `SavedRegionStore.Delete` moves the selected `.cfregion` and its `.bak` into a unique folder under `Regions/Deleted` inside `Application.persistentDataPath`. These files remain locally recoverable; they are not listed as active saves. A removal marker prevents the bundled meadow test region from reinstalling itself after deletion. Other regions are untouched. Deletion rejects paths outside the active Regions directory.

`Library/RegionDelete.request` runs the editor regression check using only a disposable generated save; it checks confirmation/cancel, removal, archive readability, backup preservation, list refresh and path rejection.

## Evergreen mix in Hills and Mountain Test

The user authorized an experimental evergreen mix for the **Hills and Mountain Test**. `MountainEvergreenMix` appends four stable slots after the 15 approved seasonal prototypes: index 15 Evergreen Pilot, 16 Balsam fir, 17 Fraser fir, and 18 Cilician fir. Runtime prefabs are under `Trees/MountainEvergreen/`. The pilot’s species is unspecified; the three named firs are distinct 3D sources. The seasonal poplar/understory library and shared meadow baseline asset remain unchanged.

Only this named test region uses the mix. It deterministically substitutes a small, clustered share of mature poplars, preserving total placement count, positions, rotations, terrain, road and ground texture cache. Existing evergreen indices 15–18 are retained unchanged, making the substitution idempotent. The original pilot selection remains stable; the three new firs use a separate selection band among remaining mature poplars. The evergreen stays green in Summer/Fall/Winter and remains visible with small plants disabled. Save validation permits the fir slots 15–18 only for this test region; the later broadleaf slots 19–26 are valid across native regions. Existing on-disk player saves are not rewritten automatically; normal Save Region remains manual.

The preview showed sparse foliage; the review material now uses a lower alpha cutoff (0.15) to retain more needles. Treat this as an experimental visual addition for the user's testing, not final foliage approval. Do not replace the approved biome based on this trial. `Library/MountainEvergreen.request` runs persistence/season/cache/placement checks and opens the saved test district. See `Documentation/Migration/V3Evergreens.md` for the V3 source inventory. V3 originals remain read-only and imported art stays out of the public repository.

Evergreen color revision (2026-10-17): needle materials now use a deeper green multiplier `(0.38, 0.56, 0.44)` and smoothness `0.04`, with specular/environment highlights disabled. This addresses the pale yellow-green source needles without changing meadow/poplar materials, bark, lighting, alpha coverage or placement. `V3EvergreenImport.NeedlePalette` preserves the settings on reimport. User visual acceptance is still pending.

Three-fir addition: see `Documentation/Migration/MountainFirSpecies.md` for source lineage, three mesh detail levels, scale, material and save contracts. All three additions use genuine 3D source meshes and keep green needles through the seasonal controls. The existing meadow/poplar library remains the biome foundation; no retired prototype broadleaf trees are reintroduced. Fir appearance remains subject to user review.

## Open Region loading progress

Main-menu Open actions now show an independent `RegionLoadingOverlay` with a named region, stage text, activity dots and a progress bar. `SavedRegionStore.Load` runs on a background Task; scene construction stays on Unity's main thread and yields through `PrototypeRegionView.OpenStages` / `MeadowDistrictLandscape.BuildStages`. Terrain height sampling and painting yield in row batches. HUD setup must not call the synchronous landscape builder during initial loading: defer it until the saved district/overview is selected. This removes a redundant full-region build before opening a saved district.

Existing synchronous editor/test entry points drain the same staged implementation; old asset-study tools use `OpenSavedRegionImmediatelyForTools`. Preserve these compatibility paths. Duplicate Open is blocked; failure cleans up the partial region and returns to the menu; loading never writes the player's save. Stage weights are estimates, not an ETA. Some native asset/GPU operations can still pause a frame. See `Documentation/03_Technical/Region_Loading.md`; verify with `Library/RegionLoading.request`.

Loading verification after the redundant-overview fix: cached / rebuilt Hills and Mountain loads observed maximum editor-update gaps of 531 / 640 ms (previously roughly 4.5 seconds). Missing-file recovery, duplicate clicks, progress completion, four-district biome integration and all mountain fir persistence/season checks passed. These are editor observations; cold native/GPU asset operations may still pause.

## Integrated maple and oak forest mix (2026-10-17)

The user authorized the optimized trees in game. `ForestTreeMix` now substitutes 24% of eligible remaining mature poplar placements in **all native meadow region/district forests**, including generated overviews and existing test maps. The approved meadow/poplar terrain and understory remain the biome foundation. This does not reintroduce retired prototype trees. New resource prefabs are `Trees/ForestBroadleaf/<species>/<season>`.

Keep append-only slots: 0–14 approved seasonal library, 15–18 the four existing mountain firs (placement still Hills and Mountain Test only), 19–26 Silver Maple A/B, Canyon Live Oak A/B, Southern Live Oak, Angel Oak, OakPilot A/B. All native regions reserve 15–18 so broadleaf IDs are the same everywhere. Do not reorder or resize the original `SeasonalTreeLibrary`. The fir mix runs first, broadleaf second; both retain existing species slots, total placements, positions and rotations. Save validation accepts broadleaf slots in ordinary regions and keeps fir slots restricted to the mountain test. No automatic player-save rewrite occurs.

Seasons: maples use derived green Summer, source golden/orange Fall, and bare Winter geometry; the source's GreenLeaf albedos needed a green conversion. OakPilots use separate genuine seasonal FBX exports. Live oaks stay evergreen, including Angel Oak moss. All have three 3D mesh LODs. Matte URP cutout/normal materials and `ForestTreePalette` affect only these new trees; the terrain, poplars, fir palette and ocean settings remain intact. Nominal species heights are 13–18m. See `Documentation/Migration/ForestBroadleafIntegration.md` for exact placement, season, source, material and rebuild contracts, and `BroadleafOptimization.md` for the original geometry study. The earlier staged-only status is superseded by this integration.

Unity checks passed for all eight species (1,081 new broadleaf placements / 15,243 total in Hills and Mountain Test), seasons, three textured mesh LODs, mature-tree visibility, deterministic repeat, temporary save/load and terrain-cache restore. The original player save stayed untouched. Four-district generation integration passed. Visual acceptance of individual new trees remains with the user. Use `Library/ForestTrees.request` to rerun the forest checks, `Library/MountainEvergreen.request` for the fir regression, and `Library/BroadleafImport.request` to rebuild imported prefabs while stopped. Source-only Python intake tools are in `Tools/Trees/BroadleafIntegration/`; all licensed/generated art stays local and ignored.

## Forest performance isolation (2026-10-17; current behavior)

The user approved the new trees' appearance, then reported a performance regression. Keep their geometry, materials and seasonal palettes intact. The eight latest maple/oak additions now default OFF via `ForestTreeMix.EnabledSpeciesMask` (PlayerPrefs `CityForge.Forest.SpeciesMask`, default 0). In a district, **NEW TREES: OFF** opens eight independent switches plus NONE/ALL. Selection persists globally and can be cumulative. Native poplars and the existing mountain firs stay enabled.

Canonical generation still assigns append-only species IDs 19–26. Disabled additions display native seasonal poplars at the same positions instead; canonical IDs, sizes and rotations remain in memory and saves, allowing later re-enabling without regenerating or rewriting player saves. Fallback poplar sizes use the native baseline; this is not an exact reconstruction of each original poplar subtype. Only enabled broadleaf prefabs load through the normal runtime palette. Never remove the new assets or change the approved meadow ground to address this regression.

Fixed-camera editor comparison at 3434×1728: wide native poplar/fir presentation 16.11 ms/frame versus all eight additions 30.91 ms; draw calls 14,928 versus 34,855. Individual species were 16.50–18.83 ms. Close comparison was 17.44 versus 33.56 ms. Combined rendering cost is the finding; this does not establish a specific CPU/GPU bottleneck. Many oaks use 12–13 materials. Start reintroduction with Maple A/B or Angel Oak, then measure cumulative combinations. See `Documentation/03_Technical/Forest_Performance.md`. `Library/ForestPerformance.request`, `Library/ForestSwitches.request`, and `Library/ForestTrees.request` run the comparative, UI/persistence, and seasonal/asset checks respectively. The latter temporarily enables all species and restores the previous preference afterward.

### Current forest trial: Silver Maple A only

The user authorized reintroducing **Silver Maple A** first. The local persisted species mask is now `1`; the other seven additions stay off. The default for a fresh preference remains off. Poplars/firs and approved materials are unchanged. Use NEW TREES to adjust the experiment; do not automatically enable the other species.

### October 19: Maple isolation and close panning

The user noticed panning “shaking” after enabling Silver Maple A. The local persisted species preference is back to mask 0 for comparison; this does not prove that model caused the shaking. Keep all eight optional additions off pending comparison. Native poplars/firs remain. Region camera keyboard and middle-mouse pan speeds are now exactly double their prior values at ZoomLevel 1, 2 and 3. Levels 0, 4 and 5 and non-region cameras retain prior speeds; Shift still applies its existing multiplier. Unity compilation passed. No terrain/art or saved maps were changed.

### October 19: navigation quality trial

The user clarified the issue as uneven camera motion, not visual shaking, and authorized reducing detail while moving. Terrain SetHeights/SetAlphamaps occur during build/cache restore, not panning. `CameraMotionQuality`, attached by the camera controller, detects translation, rotation and zoom after camera updates. During motion it temporarily uses 75% of the previous URP render scale (about 56% of pixel count) and 50% of the previous LOD bias. After 0.2 seconds without movement, or on disable/destroy, it restores the exact captured settings. Screen-space UI stays native resolution. It does not alter terrain data, trees, materials, ocean clock, surf settings or saved maps. Silver Maple remains off. This is a trial requiring user smoothness/visual review; no measured frame-time improvement is claimed yet. Render-target changes and LOD transitions may themselves need profiling if the trial does not help.

Unity compilation and `Library/CameraMotionQuality.request` lifecycle checks passed for translation/zoom activation and exact idle/disable restoration. The check is not a performance benchmark. Source: `Assets/CityForge/Scripts/Camera/CameraMotionQuality.cs`. The policy applies to cameras with `CityForgeCameraController`; it assumes the game's single active navigation camera because URP render scale and LOD bias are shared quality settings.

### October 19: panning frame pacing investigation

Scripted pan comparison found uneven intervals under the software 60 FPS cap even when average was ~60 FPS. Native streaming suspension had little effect in this run; motion quality reduced load but did not remove cadence variation. Unity documentation warns desktop targetFrameRate timing can microstutter. Normal pacing now uses vSyncCount=1 and targetFrameRate=-1; HUD says FRAME PACE: VSYNC. It follows monitor refresh. Existing FramePacing60 preference/method names remain compatibility aliases; do not revert synchronized mode to a software cap. Uncapped still available. No change to ocean/terrain/species appearance.

See Documentation/03_Technical/Camera_Panning.md and CameraPanChecks. Editor timing cannot prove display smoothness or identify every GPU/CPU stall; user comparison and standalone capture remain needed if symptoms persist. Optional maples/oaks remain off.

### Current trial: both Silver Maples with VSync (October 19)

Following the user's report that VSync improved panning, both Silver Maple A and B are enabled in the local persisted mask `3`. All six oak additions remain off. Native poplars/firs and seasonal palettes remain unchanged. Close panning at ZoomLevel 1–3 is now another 25% faster: the existing 2x factor becomes 2.5x, including keyboard and middle-mouse dragging. Levels 0, 4, 5 retain prior speeds. VSync and temporary movement-quality mode remain enabled. User will evaluate this two-maple combination; no performance acceptance is assumed.

### District preloader restored — October 19

Gameplay map clicks and district buttons now call `RequestDistrict`, which immediately shows the existing `RegionLoadingOverlay` with the region/district name. It presents two frames before advancing `EnterDistrictStages` and the nested `UpdateLandscapeModeStages`/native biome builder. Camera and region input are held during loading; duplicate requests are ignored. Progress reports named stages and completes before restoring input. Failure cleans partial detailed terrain and returns to the relief map. Native asset/GPU calls can still pause a frame; progress weights are estimates.

The synchronous `EnterDistrict` wrapper remains for editor/asset checks and drains the same staged routine. Do not wire gameplay clicks back to that immediate wrapper. `RegionLoadingOverlay` shares its nested-iterator/error runner between menu region loads and district loads. `Library/DistrictLoading.request` passed: immediate overlay/input lock, duplicate rejection, 504 rendered frames through 14 stages in the observed run, monotonic progress, successful district load, input restoration, overlay removal, return to relief and disposable failure cleanup. Player saves untouched.

### Regional framing and district entry zoom — October 19

Regional FrameRegion now fits the actual relief renderer bounds around their center, projecting all eight bounding corners into the isometric camera with a 25% margin for HUD clearance. Initial staged region loading refits after the relief mesh exists. This avoids relying on the old prototype framing assumption for the map's lower point. Detailed district entry now finishes at zoom level 5 (including the pre-existing coastline path); closer zoom speeds and approved meadow/ocean settings are unchanged. DistrictLoadingChecks verifies every relief vertex lies inside viewport and near/far clip planes, plus zoom5 after staged district loading.

### Reference-driven ridge/valley revision — verified trial (October 19)

The user supplied a shaded relief reference with mountain ranges, a broad winding river and tributaries. New recipes now default to version5, preserving version1–4 saved geography. RegionalLandformNode supplies seeded range/valley constraints inside the MapMagic graph before erosion. RegionalRiverNetwork creates one trunk and the requested connected tributaries, with shared water-distance/height carving. This is constrained procedural routing, not a physical drainage simulation.

A major scale issue was found: the old prototype ratio reduced an 8km map's vertical range to ~86m. Version5 stores a physical terrainHeightRange=1200 and sea level12; older saved dimensions remain unchanged. The overview now uses a 257×257 mesh and a 513-square terrain-derived shaded atlas, olive cover patches, warm stone, shallow/deep water and shore colors. Atlas creation yields in row batches. The current intended display exaggeration is 1.25x; district geometry uses physical saved heights. Both seasonal maples, approved native meadow materials and all ocean assets/clock stay unchanged.

The first version5 MapMagic run passed determinism, seed variation, erosion, stored height round trip, connected tributary and worker-creation checks. Its inspected render overemphasized tall wall-like ranges. The latest code lowers/broadens ranges and varies crest heights; it also fixes bank distance sampling over neighboring curve segments and moves water/biome composition to the worker. The Mac was subsequently unlocked. The final revision compiled and its screenshot was inspected: ridges are lower/broader, foothills greener, river banks smoother. The output is still visibly procedural, especially river profiles, and is a trial awaiting user visual acceptance. Do not claim an exact match to the reference. MapMagic generation checks, expanded-grid/season/save checks, and district preloader/framing/zoom5 checks passed. Player saves were untouched. The updated preview is Regional-Ridge-Valley-Preview.png in this chat outputs.

### Regional palette feedback — October 19

The user approved the ridge/valley direction and requested deeper, richer greens and removal of the light blue river border. RegionReliefMap now uses a uniform deep blue for overview water, with no cyan shallow-water rim; land cover blends richer grass green into deep woodland green. Terrain geometry, river routing, warm mountain stone, district biome materials and approved oceans are unchanged. These are overview palette changes only.

### Second relief reference refinement — October 19

The user supplied a second reference for finer ridges/gullies and natural river/land transitions. Current trial uses a 513-square overview mesh, 1025-square atlas, continuous green cover, restrained curvature shading, small ridged range modulation before erosion and broader valley transitions. River distance checks all curve segments; version5 cartographic water follows channel distance rather than painting the entire low valley blue. Keep deeper greens and no cyan rim. Existing stored heights are preserved. See Regional_Ridge_Valley_Trial.md; visual fidelity remains a trial, not an exact illustration match.

Final second-reference preview inspected; RegionalReliefChecks passed with the channel mask. Fine drainage networks from the illustration are not yet implemented, and some confluences remain geometric. The current preview is open as an unsaved test region.

### Regional palette correction — October 19

The user requested removal of all river borders, including the tan shoreline band, and muted army/olive greens rather than blue-green woodland. RegionReliefMap now has no painted bank edging and uses olive grass (0.43,0.48,0.25) through woodland (0.25,0.33,0.14). Keep this overview palette separate from approved district biome and ocean materials.

### Regional green saturation follow-up — October 19

The user preferred the border-free olive map but found greens still too vibrant. Overview grass/woodland endpoints are now (0.43,0.46,0.32) and (0.28,0.32,0.22), reducing saturation while retaining similar brightness. Preserve border-free rivers and the approved district/ocean appearance.

### Regional palette midpoint — October 19

After rebuilding, the user found the latest desaturation too muted; earlier feedback had used an older render. Current overview palette is the midpoint between the two latest olive revisions: grass (0.43,0.47,0.285), woodland (0.265,0.325,0.18). Keep rivers border-free. Review after recompilation and reopening the same region; atlas colors are built on load. District biome and ocean appearance are unchanged.

### Ocean beach and regional slab — October 19

The latest midpoint greens were accepted. The user requested a thin tan ocean beach and visible slab thickness. RegionReliefMap now paints a narrow 0.0025-normalized tan band only beside semantic Ocean outline segments, excluding map-boundary segments. Rivers remain unbordered. Regional slab sides follow the entire surface perimeter down to a flat base 2.5% of map width below the lowest displayed surface; brown vertex colors darken toward the bottom. Slab sides have no picking collider and hide with the overview. Regional framing includes side bounds. These changes affect overview presentation only; district heights, native biome, coastal surf resources and 0.45 clock remain intact.

Slab/beach verification: Unity compiled both shaders; RegionalReliefChecks passed slab bounds, all64 picks, detailed shared edge/seasons/cache and temporary save/load. Coastline Test overview screenshot inspected; dark slab sides fit in frame. No player saves written.

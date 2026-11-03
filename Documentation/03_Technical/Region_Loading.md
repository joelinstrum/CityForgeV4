# Opening saved regions with progress

The main Open Region list calls `MainMenuController.OpenSavedRegion`. It creates an independent screen overlay immediately and blocks duplicate Open actions. The overlay owns its loading coroutine so hiding the menu Canvas cannot interrupt loading.

Saved-file decompression, metadata/array parsing and validation run on a background Task through `SavedRegionStore.Load`; this path uses value data and JsonUtility rather than scene objects or resource loading. All Unity terrain, mesh, material, vegetation, water and camera work stays on the main thread.

`PrototypeRegionView.OpenStages` and `MeadowDistrictLandscape.BuildStages` yield between setup stages. Native height sampling and terrain painting yield after batches of rows when the batch timer reaches about 12 ms. Terrain calculations and seeded placement rules are unchanged. Existing synchronous editor/test entry points drain these same enumerators through `RegionBuildSteps.Complete`.

The overlay shows the region name, current stage, a progress bar, and animated activity dots. Bar positions are weighted stage milestones, not a file-byte percentage or an ETA. The final complete state is displayed before the overlay closes. A failed read/build disposes the staged enumerators, removes the partial region, restores the menu and resets the Open guard. Saves are read-only throughout loading.

Some indivisible Unity operations (resource/shader import, native terrain uploads and GPU/water initialization) can still stall a frame, particularly on a cold editor cache. This change reduces long application-side blocking; it cannot guarantee that macOS will never show a wait cursor. The application does not hide or override the OS busy indication.

Verification trigger: `Library/RegionLoading.request`. The editor check exercises a missing-file recovery, cached and uncached district opening, progress completion, frame advances during loading, duplicate-open protection, preserved firs/native terrain and an untouched original save. The uncached check uses a disposable file. Maximum observed editor-update gaps are diagnostic observations, not a standalone performance guarantee.

## Verified editor results (2026-10-17)

Cached and uncached Hills and Mountain Test loads passed, with 119 / 291 rendered-frame advances respectively during loading. Largest observed editor-update gaps were 531 / 640 ms; longest measured main-thread build steps were 449 / 604 ms. Before removing an unintended synchronous overview build from HUD setup, the same checks observed about 4.1–4.5 seconds without an editor update. The HUD now defers landscape construction until the actual saved view is selected, and the region controls stay hidden while the overlay is active.

Missing-file recovery, duplicate Open prevention, monotonic stage progress, completion, native terrain/fir presence and read-only save behavior passed. These are short editor observations on this machine, not a guarantee for cold imports or every map/device. Historical editor asset-study commands that require an immediate result use the explicit editor-only synchronous compatibility entry point; normal player Open actions always use the progress overlay.

Follow-up compatibility checks passed: the existing regional-biome wizard/four-district/edge/save-load integration suite, plus the mountain evergreen suite (1,422 evergreens, all four evergreen types, seasonal slots, unchanged placements and disk round trip). Unity was left in the Hills and Mountain Test district for interactive testing.

## District entry progress

Map cells and district buttons now use RequestDistrict and the same RegionLoadingOverlay as menu Open. The overlay renders before work starts; EnterDistrictStages yields through the native landscape builder. Camera/input and duplicate entry are locked until completion. Failed entry returns to overview. Immediate EnterDistrict remains an editor/tool compatibility path. Verify with Library/DistrictLoading.request. Native asset/GPU work can still stall individual frames.

## Loading investigation — November 2, 2026

Current runtime profiles log total wall time plus per-stage wall time and time
inside main-thread iterator steps. Stage wall time includes worker waits and
rendered frames; it is not CPU time. Creation separately logs MapMagic and
composition. Relief sampling, mesh/atlas work, generated bank painting and
stream decoration have separate stage labels. The existing short meadow log
omits several of these costs and must not be treated as total load time.

Read-only Unity probes of the open Medium/MountainBorder recipe, seed482402525,
Few streams (two routes), measured MapMagic0.579s and composition25.886s before
optimization. Exact segment indexing reduced the same composition to2.098s.
These are stage observations with the existing district still rendering, not
end-to-end creation benchmarks. No stored map was regenerated or written.

Generated-route BankDistance now uses an immutable segment hierarchy, preserving
exact segment projection rather than interpolating an approximate distance
texture. Legacy non-channel routes keep their previous implementation. The
index is transient and is rebuilt when the centerline array changes; generated
centerlines must remain immutable in place, as required by the existing bounds
cache. Distances across21600 probes (versions1/4/5, three physical sizes and
Land/MountainBorder) differ from brute force by at most5.960464e-8 normalized.
The indexed sample batch took81ms versus966ms for brute force in one run.

Generated inland district base control-map sampling uses up to eight workers (leaving two logical CPUs available)
with copied road/canopy dictionaries and scalar settings. Resource access,
TerrainData, painting overrides and texture uploads remain on the Unity thread.
The old serial path remains for other cases. One biome sample supplies both
woodland and dryness. A65536-texel check against the original serial formula
(including canopy, road, noise and river-bank terms) produced identical weights;
worker38.3ms versus reference140.8ms is a small sample, not a district-time claim.

Generated bank painting builds conservative per-row spans from individual route
segments, expanded by width+5m for ground-following routes (16m for older graded
channels). The corresponding bank formulas are zero outside these bounds. The
independent stream-study fixtures retain a full scan. A complete2048-square
comparison against the full scan produced identical stored weights in every layer.
Control-map resolution, terrain heights, approved material palettes, water and
foliage remain unchanged.

Explicit diagnostic trigger: write an absolute output directory to
Library/LoadingPerformance.request while in Play mode. With a region open it
runs read-only generation/query/painting probes and preserves the scene. At the
main menu it additionally runs real creation and first district entry on a
disposable unsaved region and saves per-stage profiles/screenshots. It never
closes an unrelated region. Fresh scene builds are required to qualify complete
loading targets; script reload can leave transient region data unavailable in
an already-rendered preview, so read-only district probes skip that state.

### End-to-end Unity qualification

Medium/MountainBorder/Few streams, seed482402525, grid8 (8192m region,
1024m districts): first full profiled optimized run8.229s creation /23.743s new
district. The next valid optimization pass was13.773s district; final runs were
8.055s creation /9.415s new district, then7.566s /8.555s. Repeat validation also
measured5.185s cache re-entry and4.091s for a previously unopened summit district.
These are editor Play-mode observations on this machine with local assets,
including the normal overlay/scene construction, not standalone-player or cold
asset-import guarantees. Diagnostic background execution is restored afterward.

The old forest-stage label included summit snow, not just forest rendering.
Actual authored-tree setup plus grass grading was about0.1s. Lowland districts
were constructing a1025-square snow mesh/mask and issuing water queries even
though all selected summits were elsewhere. MountainSummitSnow now rejects
non-overlapping summit influence bounds before allocating the mesh, and only
queries wetness for samples whose snow coverage can pass the existing threshold.
Actual mountain snow geometry, resolution and coverage formula remain unchanged.

The transient route index also supplies exact nearest-segment current coordinates,
with original prefix-length accumulation and segment/route tie ordering. Across
21600 comparison points the indexed current vectors match the original full scan
exactly. This reduced initial fallback-water setup from2.336s to0.700s and complete
water preparation from6.190s to2.869s in the matching district. The remaining water
cost includes authoritative clip fields, ground-level fields and native Crest/GPU
setup. No water-quality/resolution or reflection setting was reduced.

The complete bank-mask comparison matched the full scan at every2048x2048 texel,
all layers. Cache reopen retained terrain, stone/patch/tree counts and added no
duplicate shrubs. Summit-district rendering retained its snow cap. Approved forest
LateUpdate now also skips transient missing data/batches on script reload, avoiding
an exception/error-pause while diagnostic scripts are imported.

Small/Many streams/seed8472 validation also passed:9.350s region creation,3.623s
new512m stream district,3.036s cached re-entry,0.951s new summit district with caps.
Its complete1024-square bank map also matches the full scan exactly. All42 existing
saved regions were byte-identical after the suite. No save files were written.
Benchmark output directories containing medium-few select the Medium reproduction;
other names select Small/Many streams. Include validation to add full bank-mask,
cache, summit and close-view checks. The runner may replace its own disposable
Loading performance trial, but preserves unrelated open regions.

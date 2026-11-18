# Town panning performance — November 18, 2026

The Holly Brook fixture is the saved Guilford region, district 19. Performance validation
uses a development standalone at 3434×1818, the town's 54 lots, the saved heading and
average placed-lot center. The clock and lot growth are frozen for comparison; gameplay
actors and water remain active. Extra small plants match the user's enabled preference.
VSync is uncapped for the measurements. Each stage warms for two seconds, then samples
idle or a repeatable horizontal camera sweep at zooms 0–5. Raw results and visual captures
remain outside the source-only repository. Player regions are loaded without saving.

## Collision work

Previously, moving lumberjacks revalidated their entire remaining route on each tick.
Every sampled point scanned every lot object and used a capturing Array.Find predicate
for every catalog lookup. Offscreen actors repeated this work on their half-second
cadence; wider views made more actors update each frame. It produced large allocation
bursts and long stalls even with the camera at a close zoom.

LotAssetCatalog now indexes stable IDs once per catalog entries array and invalidates
the lookup on editor validation/array replacement. Lookups retain first-match and ordinal
ID behavior and do not allocate per call. Worker/freight collision geometry caches each
lot's local building/fence rectangles and transform with a coarse bounds rejection.
The cache invalidates on lot presentation revision, lot count, or catalog array changes.
Existing rotation, scaling, span footprints and clearance semantics are retained.

A lumberjack validates the complete approach when choosing a tree, then sweeps only the
next movement step while walking or returning. Terrain, water, grade and building/fence
checks remain active. Timber claims, forest removal, inventory and save behavior remain
unchanged. The tree/trunk spatial index continues to handle timber route queries.

## Temporary navigation detail

CameraMotionQuality emits an event only when reduced quality starts or ends. Imported
building/prop detail listeners choose their existing reduced models while moving and
restore the correct zoom tier after the existing 0.2-second settle interval. The camera
controller reference is cached rather than looked up on each movement-quality update.

Close panning retains the accepted 75% render scale and 50% LOD bias. At zooms 3–5,
enhanced navigation uses 65% render scale, 40% LOD bias and at most two shadow cascades,
with a half-distance two-cascade split. Original render scale, LOD bias, cascade count
and cascade split are restored exactly on settling, camera disable or destruction.
The approved stationary shadow resolution, four-cascade setup and light settings remain
authoritative. Screen-space UI keeps its native resolution.

Mixed summer woodland can use its existing spatial instancing while navigating at
orthographic sizes of at least 75m. Stationary views keep the approved 200m crossover.
The same meshes, materials, bounds, shadow proxies and cached draw groups are reused;
there is no terrain regeneration, new forest family or per-frame grouping rebuild for
an orthographic pan. At panning sizes of 175m and 350m, spatial draw cells coalesce from
128m into 256m and 512m cells respectively. This reduces submissions when much of the
district is visible while preserving the same instances, mesh/material choices and
conservative bounds. Cell coalescing is cached and the original fine cells, detailed
source renderers and LODGroups restore after settling.
Optional streamed ground cover remains suspended during movement as before.

## Verification

TownNavigationChecks.Run includes the lot/path/texture suite, all three shared pebble
wealth levels, 2,000 reference-versus-cached collision queries on rotated buildings and
fences, cache reuse/move/remove invalidation, 10,000 allocation-free catalog queries,
close/moving/settled/distant detail states, no repeated movement event, wide-panning
reduction and exact quality restoration. ExtractionCaptureChecks.Run includes worker,
freight route/save/resume/delivery, cargo progress, geometry and resource capture checks.

The explicit standalone harness also records terrain texture callbacks, lot presentation
builds, finish applications, streamed tiles and collision-cache builds. These distinguish
normal frame rendering from actual terrain painting, geometry rebuilding and construction
choices. Build/rebuild counters have no per-frame scanning cost. The optional internal
navigation flag permits same-binary comparisons with the previous panning policy.

Subsequent actor route caching is documented in `Established_Actor_Routes.md`. It removes even the local clearance probes during unchanged established travel, preserves pedestrian circuits across layout refreshes, and retains tree candidates and delivery round trips.

# Approved mixed woodland — November 3, 2026

New temperate regions created through the wizard or default main-menu creation use
`forestAppearanceVersion=1`. Missing/zero retains the previous forest on existing
saves. Geography remains version12. The recipe flag survives the normal region
save/cache format; no player maps are migrated or rewritten.

## Appearance and placement

The private version-one library contains five approved chestnut shapes, one locust,
and tall/medium larches. The seeded mix is56% chestnut,16% locust,28% larch. Prefabs
retain the accepted darker foliage, fuller distant crowns, original leaf shading,
axis conversions and model scale. Distant shadows use the cheaper approved meshes.

`MixedDistrictForest` selects canonical mature woodland positions deterministically,
keeping one candidate per10m world cell. It uses the region's existing ecology,
not the fixed70% stress layout. Water, road, steep-slope, camp-clearing and active
combined-district exclusions remain authoritative. Canonical native positions,
prototype IDs and rotations remain stored for painting, saves and seasonal display.
The displayed mixed trees are reseated on the current district terrain.

The mixed family is the summer presentation. Existing native fall/winter trees
remain authoritative. Tropical palms and existing mountain fir slots are unchanged.
The Trees toggle hides both terrain and mixed trees; understory remains independent.
Regional relief and distant silhouettes are unchanged. Older regions keep their
previous approved authored/native forest family.

## Rendering

`SpatialForestRenderer` groups matching meshes/materials in128m spatial cells,
with at most511 matrices per draw. It selects each tree's existing LOD and leaves
per-camera bounds culling to Unity, including shadow/reflection submissions.
It reuses selections while a top-down camera pans; perspective distance changes
still rebuild them. Distant views use instancing; close views retain individual
LODGroups. The accepted cutoff is view-size200. Disabling the renderer restores
its source renderers and LODGroups. Captured forests are static; moved/edited trees
require rebuilding their capture.

Cheap distant shadow meshes are submitted separately from the unchanged visible
crowns. This reduced long-frame spikes in the8,165-tree fixture without thinning
the main foliage. Reflections retain all-camera submissions, but water-reflection
visual QA was not part of that waterless stress fixture.

`PerspectiveMeadowBatches` batches existing selected grass matrices across nearby
runtime tiles. Despite its historical name, it supports both camera projections.
It changes submissions, not density or placement, and retains navigation throttling.
Showcase renderers that do not opt into `combineCompatibleBatches` remain unchanged.

## Private asset setup

The repository is public and source-only. Purchased models, textures, prefabs,
material variants and the library asset are ignored local content.

1. Restore the privately preserved approved art under
   `Assets/CityForge/Art/Trees/ForestTrees1Trial/` (fuller library, original optimized
   shadow library, accepted480-tree source prefab, and larch assets).
2. Run **CityForge / Forest / Build approved mixed forest library**.
3. The builder writes compact canonical prefabs and the asset at
   `Assets/CityForge/Resources/Trees/ApprovedMixedForest/V1/Library.asset`.

A private `.unitypackage` exported from that library with dependencies is another
way to restore the runtime art. Do not add that package or its art to public Git.
The creation flow checks for the library before generating a new temperate region;
missing art gives an explicit error rather than silently replacing the approved trees.
Future appearance revisions should use a new versioned library and recipe flag.

## Verification

`MixedForestIntegrationChecks.Begin()` explicitly creates a disposable small region
through the normal main-menu loading flow. It checks all eight library entries,
new/missing/unsupported recipe versions, grounded and footprint-contained placement,
road/water/camp clearance, visibility and seasons, unchanged canonical planar ecology,
neighboring/cached districts, disk round trip, legacy behavior and an L-shaped union.
Reports and snapshots are written only under ignored `Library/`; player saves are
not modified. Unity may re-seat native tree Y coordinates against quantized terrain
on cache reopening, so ecology comparisons preserve planar positions, species,
rotations and scales while separately verifying displayed tree grounding.

November3 acceptance run passed. Overview creation8.82s; first512m district5.01s
with2,260 mixed trees; deterministic placement signature788453d361dc1471.
These are local editor observations, not performance guarantees for every recipe.

The larger private fixture remains separate:8,165 trees occupying70% of sampled
woodland planting cells in a1024m district. Spatial instancing improved distant
median submission performance but did not help close views. Cheap distant shadows
reduced repeated L5 panning p95 from92.95ms to42.63ms while median remained~39ms.
Preserve the accepted appearance during subsequent profiling; do not compare
absolute frame times across editor sessions, resolutions or different camera paths.

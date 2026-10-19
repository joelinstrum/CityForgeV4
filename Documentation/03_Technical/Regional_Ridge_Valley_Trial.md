# Ridge and river-valley regional map trial

October 19, 2026. The user's visual reference calls for connected mountain ranges, legible shaded slopes, broad winding rivers and tributaries, olive/green lowlands, and subtle district lines. This revision moves toward that reference through coherent geography and terrain-derived cartography. It is an initial visual trial requiring user acceptance, not an exact match to the reference illustration.

## Geography version 5

New recipes default to version5. MapMagicRegionGeography links MapMagic Noise200 into the CityForge RegionalLandformNode, then MapMagic Erosion200 and its height output. The custom node defines four seeded range segments around a shared valley axis, varying crest heights and widths and adding the MapMagic noise field. Topography selects amplitude; flat maps remain flat. Noise and erosion remain actual installed MapMagic generators; ridge placement is CityForge's explicit constraint, not an off-the-shelf drainage algorithm. Version4's graph and version1–3's old generation behavior remain available for legacy recipes.

RegionalRiverNetwork uses a sinuous shared trunk and branches whose first point joins it. Rivers/Streams options control body count and widths. Bank distances check all polyline segments to avoid artifacts when the nearest segment changes. Heights are lowered into the same river channels and broad bank transition used by the map's water mask. This is deterministic procedural routing and carving, not a simulated watershed, sediment transport system, or complete braided-river generator.

The old prototype horizontal ratio accidentally compressed a Medium region's height range to roughly 86m. New version5 snapshots explicitly store a physical height range of 1200m, with water at 12m; old saves retain their previous dimensions. Actual generated peaks occupy only a portion of that range. Version5 woodland attenuation occurs higher in the normalized elevation range so foothills can remain wooded; it affects both the summary and district biome field, not a decorative forest painted only on the overview.

## Regional presentation

The map mesh is 513×513 (UInt32 indices). A 1025×1025 atlas derives deep green terrain cover, warm rock, hill shading, terrain occlusion, shore color and uniform deep-blue river color from the actual shared fields. The grid is subtler. Atlas rows yield during loading. Map vertical relief is exaggerated by 1.25× for readability; detailed district heights remain physically unexaggerated. Picking still uses the relief mesh's collider and shared district coordinates.

The first physically corrected render was too tall and cliff-like. The current revision lowers/broadens ranges, adds peak variation and reduces display exaggeration; rock coverage and river color balance have also been revised. Some ridge profiles and channel shapes remain procedural and need visual feedback. No imported reference image is projected onto the terrain.

Creation performs MapMagic generation and subsequent river/biome composition on worker tasks behind the preloader. Scene construction and texture/mesh upload stay on Unity's main thread. Stored shared heights remain authoritative on reopening. Approved district materials, seasonal assets, enabled Silver Maples, native fir mix, ocean resources and clock settings are unchanged. No player saves are automatically regenerated or overwritten.

## Verification

MapMagicRegionChecks passed against version5: actual MapMagic nodes, deterministic repeat, seed variation, erosion effect, connected tributaries, corrected physical-height metadata, stored height round trip, and the real worker-backed Create Region flow. Subsequent ridge-fold revisions require a fresh generation check before claiming those earlier generation results for the revised output.

RegionBiomeChecks/RegionalReliefChecks verify the expanded grid, option influence, shared biome edges, district ray picking, matching rendered terrain edge, seasonal library, isolated detail loading, overview transitions, cache retention and temporary save/load. DistrictLoadingChecks verifies the preloader, input locks, zoom5 and complete map framing. See the current reports in the chat outputs folder for final execution status.

Final verification after unlocking: generation checks passed; RegionBiomeChecks data/live passed with the new woodland cutoff; RegionalReliefChecks passed all 64 picks, shared rendered edge, seasons and persistence; DistrictLoadingChecks passed complete viewport/depth fit, zoom5, 702 rendered frames over 14 loading stages, duplicate rejection and cleanup. Final preview inspected. Source formatting check passed. Visual acceptance remains with the user.

## Second reference refinement

The user requested finer landforms like the second reference. Range height now includes low-amplitude multiscale ridged modulation before MapMagic erosion. The initial high-amplitude detail was visually rejected during inspection and reduced. Valley transitions are broader (.065 normalized). Overview mesh and atlas now sample more densely; continuous cover blending and restrained terrain-curvature shading improve slope detail. Version5 overview river color follows its stored channel distance rather than flooding all low valley terrain blue. Water remains deep blue without cyan edging. Full polyline distance evaluation replaces the nearest-row approximation on sharp bends. Stored maps are not regenerated; district assets and ocean settings are untouched.

Second-reference verification: Unity compilation and RegionalReliefChecks passed with the final channel mask: all64 picks, three detailed districts, shared edge, seasonal prototypes, cache retention and temporary save/load. Final native Unity render inspected. Some tributary junctions and mountain profiles remain visibly procedural; the reference's fine drainage networks are not implemented.

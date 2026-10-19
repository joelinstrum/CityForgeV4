# MapMagic regional geography trial

Current default: version5. See Regional_Ridge_Valley_Trial.md for the physical height correction, constrained ranges/connected rivers, 257-square mesh and terrain atlas. Earlier version4/129-sample descriptions below are historical.

October 19, 2026. New regions now default to recipe version 4. This version uses the locally installed MapMagic2 matrix/graph pipeline for regional elevations. Version 1–3 generation paths and all existing saved heightfields remain authoritative and unchanged.

## Pipeline

MapMagicRegionGeography constructs a bounded runtime Graph: Noise200 → Levels200 → Erosion200 → HeightOutput200. Noise is seeded from the region recipe, uses Perlin with detail/turbulence, and scales feature size with region width. Levels shapes the relief amplitude and gamma from the wizard's topography option; Flat has zero relief. Four erosion iterations run for non-flat maps, with a 16-sample margin around the 513×513 active field. The result is cropped into the existing shared regional height array. The output node is used to drive actual MapMagic graph evaluation; CityForge consumes the erosion matrix directly without applying MapMagic Terrain tiles.

MapMagic creates no scene terrain, vegetation, trees, materials, water or infinite streaming object. There is one shared geography field for the entire region. RegionMapGenerator retains its waterway channels and biome-field composition after MapMagic elevation generation. Rivers are therefore still the existing authored procedural routes carved through the elevation, not an erosion-derived drainage network. This is an initial geography trial, not a full hydrology replacement.

The relief renderer and detailed districts both sample the resulting stored field. The 8×8 grid, isolated district loading, seasonal biome, map bounds fit and district zoom5 remain unchanged. Approved meadow/poplar materials, both enabled Silver Maples, fir mix, ocean resources and the 0.45 ocean clock are untouched.

## Creation, loading and persistence

New-map creation now shows RegionLoadingOverlay. Graph objects are constructed on Unity's main thread; MapMagic matrix generation runs in Task.Run, while the overlay remains active. Waterway/biome composition and OpenStages continue on the main thread. Nested iterator handling and exceptions use the existing loading runner. Failure releases the menu guard and displays a creation error. Stage progress is an estimate, not a measurement of individual erosion work.

GenerateStages is the interactive path; Generate remains synchronous for checks/tools and uses the same provider. Existing saves open from their serialized height arrays, with no MapMagic generation on load. The metadata's recipe version identifies this initial graph configuration; changing geography algorithms later requires a new recipe version. No MapMagic package or licensed asset source was copied into the public repository. The authored graph recipe is C# source rather than a runtime-edited asset.

The regional field remains 513×513; district terrain interpolation does not add new geographic samples. Finer terrain detail, graph tuning, drainage, coast generation and settlement placement remain future work. Existing approved coastal test districts retain their existing geography and appearance.

## Verification

Library/MapMagicRegion.request runs MapMagicRegionChecks: actual installed node types, repeatable heights, alternate seed variation, erosion on/off differences, version3 legacy path, authoritative saved height round trip, and the actual Create Region menu path with worker generation and loading overlay. The observed run passed with 1,595 rendered frames during asynchronous creation; frame count is not a generation-time guarantee. Immediate and worker paths produced identical heights.

Library/RegionBiomes.request with integration passed against the new default version4: moisture/vegetation influence, all shared biome edges, determinism and recipe persistence, 64 clickable cells, isolated detailed districts 0/1/63, shared rendered terrain edge, seasonal tree-library references, overview transitions, cache retention and expanded save IDs. The overview screenshot was visually inspected. Existing player saves were not rewritten. User visual acceptance of the new terrain remains pending.

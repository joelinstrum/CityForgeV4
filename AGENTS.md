# CityForge V4 project instructions

Read `HANDOFF.md` before changing map generation, terrain, foliage, or rendering. It identifies the approved native meadow/poplar biome and its local baseline assets.

Never restore the retired prototype broadleaf forest or its distant silhouettes as a fallback. Use `MeadowRuntimeBaseline`, `MeadowDistrictLandscape`, and `SeasonalTreeLibrary`; preserve approved material/shader appearance and ocean settings. Historical asset evaluation/import scripts are not gameplay baselines.

The Git repository is public and source-only. Do not add licensed artwork, imported asset packs, Crest package source, builds, or saved maps.

## Regional overview and expanded grid

Read Documentation/03_Technical/Regional_Relief_Map.md. New version-three recipes use 8×8 districts. Never hard-code quadrant IDs or restore the full-detail meadow overview. Main-menu loads use the relief map; detailed native biome rendering is isolated to the selected district. Older saves retain grid side 2. Keep terrain/biome coordinates shared and preserve the last district cache when saving from overview. City/town markers require actual settlement data. Library/RegionBiomes.request with integration now runs the expanded-grid checks; earlier full-meadow-overview assertions are superseded.

## MapMagic geography

Default new recipes are version12 and use MapMagicRegionGeography plus the Terrain shape plan. Read Documentation/03_Technical/Region_Terrain_Shapes.md. Preserve version1–11 generation and stored maps. Version11 adds seeded one-summit wall / two-summit / fuller ridge variation within the approved natural rear-quarter footprint. Version10 raises only the upper summits and adds shared permanent summit snow caps; preserve the approved green foothills. Version9 compresses the natural Mountain Border into the rear25% of the region. Version8 Mountain Border reuses the approved river-flanking massif shapes along the north/back edge. The exaggerated version7 range was rejected; do not restore it as the current baseline. Other shapes retain version6 geography. Read Documentation/03_Technical/MapMagic_Regional_Geography.md. Preserve stored maps and version1–4 behavior. MapMagic generates only the shared heightfield; approved district textures/trees and the relief renderer remain authoritative. Creation must use GenerateStages/worker generation behind the preloader. Do not use the old MapMagicMeadowStudy authoring scene as the biome baseline.

Version5 uses RegionalLandformNode and RegionalRiverNetwork; see Documentation/03_Technical/Regional_Ridge_Valley_Trial.md. Preserve explicit physical height metadata and the 1.25× overview-only exaggeration. Never copy the reference illustration into the map as a texture.

## Combined districts

CombinedDistrictGroup retains original grid cell IDs. Use DistrictMembers/ActiveDistrictMembers and InActiveDistrict rather than assuming every playable district is one square. Enforce2–4 original edge-connected cells, including when expanding existing groups; never fill an L-shaped union’s excluded corner. Group metadata is saved with the region; canonical shared geography is unchanged. Preserve native terrain holes, footprint-filtered trees/roads/snow/water and group-anchor cache identity.

Version12 adds the Tropical ocean shelf and sea30m datum, with all land/water surfaces shifted together. Other recipes keep sea12m. See TropicalOceanDepth. Keep shared-region water colors and union-aware district slab edges; do not regenerate older stored maps.

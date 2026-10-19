# CityForge V4 project instructions

Read `HANDOFF.md` before changing map generation, terrain, foliage, or rendering. It identifies the approved native meadow/poplar biome and its local baseline assets.

Never restore the retired prototype broadleaf forest or its distant silhouettes as a fallback. Use `MeadowRuntimeBaseline`, `MeadowDistrictLandscape`, and `SeasonalTreeLibrary`; preserve approved material/shader appearance and ocean settings. Historical asset evaluation/import scripts are not gameplay baselines.

The Git repository is public and source-only. Do not add licensed artwork, imported asset packs, Crest package source, builds, or saved maps.

## Regional overview and expanded grid

Read Documentation/03_Technical/Regional_Relief_Map.md. New version-three recipes use 8×8 districts. Never hard-code quadrant IDs or restore the full-detail meadow overview. Main-menu loads use the relief map; detailed native biome rendering is isolated to the selected district. Older saves retain grid side 2. Keep terrain/biome coordinates shared and preserve the last district cache when saving from overview. City/town markers require actual settlement data. Library/RegionBiomes.request with integration now runs the expanded-grid checks; earlier full-meadow-overview assertions are superseded.

## MapMagic geography

Default new recipes are version5 and use MapMagicRegionGeography. Read Documentation/03_Technical/MapMagic_Regional_Geography.md. Preserve stored maps and version1–4 behavior. MapMagic generates only the shared heightfield; approved district textures/trees and the relief renderer remain authoritative. Creation must use GenerateStages/worker generation behind the preloader. Do not use the old MapMagicMeadowStudy authoring scene as the biome baseline.

Version5 uses RegionalLandformNode and RegionalRiverNetwork; see Documentation/03_Technical/Regional_Ridge_Valley_Trial.md. Preserve explicit physical height metadata and the 1.25× overview-only exaggeration. Never copy the reference illustration into the map as a texture.

# Regional relief map and district grid

Current default: version5. See Regional_Ridge_Valley_Trial.md for the physical height correction, constrained ranges/connected rivers, 257-square mesh and terrain atlas. Earlier version4/129-sample descriptions below are historical.

October 19, 2026. The user requested regional cartography rather than a forest rendered at a distant camera position, and explicitly chose expansion to many districts now.

## Current behavior

Main-menu Open and creation load the relief overview first, even when a save last recorded a district camera. Click a grid cell to enter its detailed district; Region Overview or Escape returns to the map. The map uses the saved shared elevation and biome fields, green woodland/meadow colors, brown/gray uplands, blue water, and a district grid. It has no tree instances, ground-cover streaming, or water simulation. A 129×129 mesh replaces detailed biome rendering at this scale. District-only seasonal/species controls are hidden on the map.

New version-three recipes default to an 8×8 grid, 64 districts. Grid size is recorded in the recipe and validated (power-of-two sides 2–16 supported by the current 513-sample field). There is no grid-size selector yet. Region size continues to select district scale:

| Wizard size | Region side | District side | District count |
|---|---:|---:|---:|
| Small | 4,096 m | 512 m | 64 |
| Medium | 8,192 m | 1,024 m | 64 |
| Large | 16,384 m | 2,048 m | 64 |

Version-one/two recipes and saved test regions retain their original two-by-two grid and dimensions. Nothing expands old player maps automatically. All views now use the separate relief overview. The synchronous editor Open compatibility wrapper can still resume a saved district for historical asset/check tools; gameplay uses OpenStages and starts at the map.

Only the selected district is rendered with MeadowDistrictLandscape. Other districts and the relief mesh are hidden while it is open. There is no adjacent district editing through this view. Height sampling, district IDs, picking, mesh slicing and biome descriptions use the same grid coordinates. The approved native meadow/poplar biome remains the detailed foundation; both Silver Maples remain selected, oaks off. Ocean resources, shaders and clock remain unchanged.

## Persistence and limits

The recipe holds grid size; JSON metadata accepts the corresponding district ID range. Binary height arrays remain 513×513. Increasing region width therefore reduces regional height-sample density; district Terrain interpolation does not add geographic information. This is a working regional foundation, not a finished large-world geography generator.

Returning to overview captures the last detailed district cache before unloading it. Saving from overview preserves that cache instead of discarding it. The prototype still persists one detailed district cache, not an independent edited save for every district. A per-district persistence system is required before adding persistent construction/terrain edits across many districts. Existing player saves are not rewritten automatically.

There are no actual settlement records yet. No fictional city/town dots have been generated. Settlement markers should come from persistent settlement data, with size/population controlling dot versus miniature representation.

## Verification

Write integration to Library/RegionBiomes.request. RegionBiomeChecks checks option influence, every shared biome edge, seed determinism, summaries and recipe persistence, then runs RegionalReliefChecks. The latter checks all 64 ray-picked cells, 8km/1km scale, no overview tree instances, Metal shader compilation, isolated detailed districts 0/1/63, correct shared height sampling, a rendered adjacent edge, Summer/Fall/Winter library references, overview transitions, retained last cache, temporary save/load and high ID63, plus version-two four-district compatibility. Player saves remain untouched. The earlier Hills-specific ForestTreeChecks could not run because its expected saved region file was absent; no replacement player save was created.

The first preview was visually inspected. This is initial relief presentation; marker styling, richer cartography, geography resolution and per-district edit storage remain future work. Region generation currently produces inland terrain; existing coastal maps are blue on the relief view without changing their approved district ocean appearance.

## District entry progress

Map cells and district buttons now use RequestDistrict and the same RegionLoadingOverlay as menu Open. The overlay renders before work starts; EnterDistrictStages yields through the native landscape builder. Camera/input and duplicate entry are locked until completion. Failed entry returns to overview. Immediate EnterDistrict remains an editor/tool compatibility path. Verify with Library/DistrictLoading.request. Native asset/GPU work can still stall individual frames.

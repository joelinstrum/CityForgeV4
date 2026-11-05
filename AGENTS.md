# CityForge V4 project instructions

Read `HANDOFF.md` before changing map generation, terrain, foliage, or rendering. It identifies the approved native meadow/poplar biome and its local baseline assets.

Never restore the retired prototype broadleaf forest or its distant silhouettes as a fallback. Use `MeadowRuntimeBaseline`, `MeadowDistrictLandscape`, and `SeasonalTreeLibrary`; preserve approved material/shader appearance and ocean settings. Historical asset evaluation/import scripts are not gameplay baselines.

New wizard/default main-menu temperate recipes use forestAppearanceVersion1 and the
approved private MixedForestLibrary at Trees/ApprovedMixedForest/V1/Library. Preserve
its chestnut/locust/larch palette, fuller distance crowns, cheap distant shadows and
hybrid near/instanced rendering. Missing/zero forest versions keep existing forests;
native fall/winter and tropical vegetation remain authoritative. New wizard/default
recipes also opt into treeCoverageVersion1: per-original-district 25/50/75%
plantable-ground woodland footprints, default50%, approximately9.48m internal
spacing. Missing/zero coverage versions retain existing ecology/planting. Combined
districts keep individual member choices; do not apply the legacy10m thinning to
coverage-version-one mixed trees. Read
Documentation/03_Technical/Approved_Mixed_Forest.md before changing this integration.

The Git repository is public and source-only. Do not add licensed artwork, imported asset packs, Crest package source, builds, or saved maps.

## PointWire and player lakes

Use PointWirePath/PointWireSurface/PointWireTool and PointWireGeometry for reusable
marker/string outlines; keep consumer mutations in adapters. Read
Documentation/03_Technical/PointWire.md and Player_Lakes.md. User lake footprints,
levels/depth/bank widths persist in RegionWaterBody. Replay basins before district
terrain application; filter trees after original-ground coverage ranking, including
stale caches and explicit additions. Preserve original lake/water rendering. Closing
a valid lake guide commits water; cancelling its sketch does not undo the lake.

Player streams/rivers use RegionWaterBody.playerChannel and shared normalized
centerlines plus world-height station grades. Read Player_Waterways.md. Keep them
out of native generated-water evaluators and coverage ranking. Rebuild bounded
channels in any intersecting district from the same record; never independently
regenerate a boundary's level. Extensions append to the same ID and preserve old
stations. PointWire OpenPath/CompletePath is the Enter-to-finish interaction.

Generated riverbank PAINT must require HasNaturalRiverBanks, not merely a generated
watery district; otherwise lakes inherit a broad soil halo. Keep water renderer routing
separate. LakeMeadowRepair handles polluted lake caches locally before bed capture;
preserve native banks, camps, roads, channel paint and authoritative geography.

## Forest editing

Forest removal is persisted in additive SavedRegionInfo.forestRemovals metadata, scoped
by original district IDs. Preserve masks through Capture/OpenStages and filter stale
caches/fresh generation so cleared trees cannot regrow; never re-rank coverage after
clearing. RemoveForest synchronizes canonical/native/mixed trees and spatial batches.
Brush sizes200/400m are circumference, not diameter. Hold/drag uses distance-sampled
positions and a single batched mutation per frame; do not reintroduce per-frame
stationary edits or one terrain rebuild per sample. Read
Documentation/03_Technical/Forest_Removal.md before changing forest editing.

Preserve SavedRegionInfo.forestAdditions through capture/open. Merge explicit planted
records after base removal masks; additions override older removals, and later removal
deletes addition records. Cached/fresh merges must be idempotent. Preserve existing
forest objects when appending. Read Documentation/03_Technical/Forest_Planting.md.

## Regional overview and expanded grid

Read Documentation/03_Technical/Regional_Relief_Map.md. New version-three recipes use 8×8 districts. Never hard-code quadrant IDs or restore the full-detail meadow overview. Main-menu loads use the relief map; detailed native biome rendering is isolated to the selected district. Older saves retain grid side 2. Keep terrain/biome coordinates shared and preserve the last district cache when saving from overview. City/town markers require actual settlement data. Library/RegionBiomes.request with integration now runs the expanded-grid checks; earlier full-meadow-overview assertions are superseded.

## MapMagic geography

Default new recipes are version12 and use MapMagicRegionGeography plus the Terrain shape plan. Read Documentation/03_Technical/Region_Terrain_Shapes.md. Preserve version1–11 generation and stored maps. Version11 adds seeded one-summit wall / two-summit / fuller ridge variation within the approved natural rear-quarter footprint. Version10 raises only the upper summits and adds shared permanent summit snow caps; preserve the approved green foothills. Version9 compresses the natural Mountain Border into the rear25% of the region. Version8 Mountain Border reuses the approved river-flanking massif shapes along the north/back edge. The exaggerated version7 range was rejected; do not restore it as the current baseline. Other shapes retain version6 geography. Read Documentation/03_Technical/MapMagic_Regional_Geography.md. Preserve stored maps and version1–4 behavior. MapMagic generates only the shared heightfield; approved district textures/trees and the relief renderer remain authoritative. Creation must use GenerateStages/worker generation behind the preloader. Do not use the old MapMagicMeadowStudy authoring scene as the biome baseline.

Version5 uses RegionalLandformNode and RegionalRiverNetwork; see Documentation/03_Technical/Regional_Ridge_Valley_Trial.md. Preserve explicit physical height metadata and the 1.25× overview-only exaggeration. Never copy the reference illustration into the map as a texture.

## Combined districts

CombinedDistrictGroup retains original grid cell IDs. Use DistrictMembers/ActiveDistrictMembers and InActiveDistrict rather than assuming every playable district is one square. Enforce2–4 original edge-connected cells, including when expanding existing groups; never fill an L-shaped union’s excluded corner. Group metadata is saved with the region; canonical shared geography is unchanged. Preserve native terrain holes, footprint-filtered trees/roads/snow/water and group-anchor cache identity.

Version12 adds the Tropical ocean shelf and sea30m datum, with all land/water surfaces shifted together. Other recipes keep sea12m. See TropicalOceanDepth. Keep shared-region water colors and union-aware district slab edges; do not regenerate older stored maps.

## Native settlements — new-region content

Geography remains version12. New wizard recipes expose an Indigenous Camps option (Off by default); enabling it sets nativeCampVersion1; missing/zero in older recipes stays off. Persist NativeCampRecord arrays through SavedRegionInfo/PrototypeRegionView/Capture; never generate settlements when opening an old save. Target a seeded two or three camps per new region, prefer water-adjacent dry sites, and skip unusable ground. Camp records retain original district IDs across combinations; use ActiveDistrictMembers and district-contained footprints. Overview Camp markers must refer to actual records.

NativeCampDistrict renders5–9 horseshoe teepees, one upright central totem and bounded ambient residents using shared NPC actions. Keep residents within their camp leash and structure-safe connected navigation. Preserve the FBX axis conversion on the totem's model child; rotate its wrapper for camp heading. Original/source art, prepared models, materials, textures, controllers, definitions and prefabs remain private/ignored. Existing NPC transient activity is not persistent city population state. See Documentation/03_Technical/Native_Settlements.md.

Persistent shadow default: CityForge_URP uses4096 main-light shadows, four cascades with splits(.50,.68,.85), and Hard directional shadows through all seasons. Preserve camera shadow-distance fitting and navigation quality throttling; do not reintroduce a temporary live-preview pipeline as a gameplay dependency.

Generated narrow streams (isStream/channelVersion>=2) use GeneratedStreamAppearance to carry the approved Pass17 scanned rocks, ten individual stone variants, native bank shrubs and blended damp-earth appearance onto saved regional routes. Preserve this integration and its cache padding/deduplication. Do not use the study-only fixed Center(z),1024m domain or12m water plane as generated geography; use route-local coordinates and GroundWaterLevel. Preserve original Pass17 fixture gates and current shallow-bank/width/mountain-avoidance rules.

Land wildlife uses WildlifeWaterAccess (actual lake/ocean levels and saved player-channel
grades) as a physical movement barrier. Preserve per-step and full-path checks, aquatic /
flying exceptions, and close-zoom simulation pause. Deer freshwater visits use the local
head-down Drink clip, dry bank targets, stationary drinking and return-to-roaming states.
Do not let habitat exceptions for drinking bypass water or tree/road safety. See
Documentation/03_Technical/Wildlife_Water_Behavior.md. No persistent animal activity schema.

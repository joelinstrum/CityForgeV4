## Tropical shelf depth and thicker map edges — October31

New default recipe version12 gives Tropical ocean layouts a gradually deepening continental shelf: sea level30m, seabed reaching6m (24m depth) over12% of region width. Land and all explicit water surfaces shift upward18m together, preserving relative relief, river depths and shoreline location. Non-ocean and Temperate generation remains unchanged; versions1–11 and stored heightfields remain authoritative. Existing Tropical maps receive the offshore color presentation without regenerating their geography.

Tropical Crest scattering uses an owned256-square texture sampled in shared regional coordinates, transitioning from aqua nearshore to richer blue offshore. CoastalWaterPresentation applies its existing projection-dependent color multipliers to the input and retains adaptive sun-glare smoothness. Waves, reflection settings, shoreline foam and clipping are retained. Regional relief uses the same shelf blend for ocean colors. The depth probe captures the entire new seabed range.

Native districts now have an earth-colored exterior slab. Edge generation follows original member-cell adjacency, suppresses internal walls, preserves concave L-shaped boundaries and uses one common bottom. Owned meshes/materials are released with the landscape. Regional overview slab depth increases from2.5% to4.5% of map width. These edges are presentation only and do not alter saved terrain or gameplay colliders.

## Seeded Mountain Border variation — October31

New default recipe version11 adds three seed-selected silhouettes within the approved rear-quarter natural range: a higher wall with one dominant summit, a two-summit ridge, or the approved fuller segmented ridge. Seed controls summit positions/widths and existing noise detail. Cross-range profile, foothill blending and restrained upper-slope uplift remain. Snow caps follow the silhouette (up to1/2/3 separated summits), shared by region and district views. Other shapes and versions1–10 remain unchanged; stored geography is authoritative.

Create Region now assigns a fresh visible seed each time the creator is opened, so consecutive new regions vary without manual seed edits. Navigating wizard pages retains it. Entering the same seed reproduces the same geography; changing an existing save or regenerating on load is not involved.

## Raised summits with permanent snow caps — October31

Default recipe version10 preserves the approved compact rear-quarter range. After MapMagic erosion, only Mountain Border samples above156m of relief receive a30% lift of the amount above that threshold. All lower height samples stay exactly identical to version9; crest placement, reach and foothill transitions remain unchanged. Other shapes and versions1–9 retain their prior geography.

`MountainSummitSnow` chooses up to3 separated high local summits from the stored canonical field, then applies height-limited, irregular snow caps. Overview atlas and detailed terrain overlay share that selection/mask. Caps leave water and lower slopes clear, receive shadows in districts, and remain when temporary weather is cleared. They change presentation only; no canonical map/tree data are painted or rewritten. Runtime meshes/materials/masks are owned and destroyed with their district. Existing saved maps remain untouched.

## Compact mountain reach — October31

Default recipes are version9. The approved natural north/back Mountain Border is compressed only across its depth: the previous40%-wide taper envelope is mapped into the rear25% of the region, retaining its along-range shape and amplitude. The creator preview uses the same rear quarter. Versions1–8 and stored maps retain their original geography; other terrain shapes are unchanged.

## Natural Mountain Border correction — October31

The user rejected the exaggerated version7 ranges. Default recipes now use version8: Mountain Border reuses the approved version5 river-flanking massif formula (same ridge widths, taper, peak variation, mild ribs and normal amplitude), translating the two sections from one side of the river to the north/back edge. It does not use the rejected tall triple-ridge formula. The rest of the map remains gentle; the Terrain preview marks the same north edge. Flat Environment still provides a moderate mountain border. Other terrain shapes are unchanged. Version1–7 and all stored fields remain readable and authoritative.

New version8 districts retain the normal approved meadow presentation used by Test Region 1; version7-only rock/scree and framing changes are isolated to version7. No source save is regenerated or changed.

## Rugged Mountain Border — October31

New recipes default to version7. Only Mountain Border geography changes: three wandering ridge divides with high summits, deep intervening valleys, restrained rocky ribs and lower foothills, all confined to the seeded edge and fading out by32% of the region width. Other shapes retain their version6 geography; versions1–6 and stored maps remain authoritative. MapMagic erosion, worker-backed creation and existing physical1200m terrain range are retained.

New Mountain Border districts use the approved mountain world-projected rock/scree surface layers (runtime baseline version107), with meadow lowlands retained. Entry frames the actual district height range rather than the meadow comparison point, so tall peaks fit. No licensed assets or saved terrain are rewritten.

## Peninsula north-to-south orientation — October31
Verified normal Create Map -> north-attached Peninsula -> coastal district61; Crest/palms/dry-road checks and persisted direction passed. Actual overview/preview screenshots reviewed in chat outputs/peninsula-north. Source compiled and whitespace checks passed; existing save hashes unchanged.

New wizard Peninsula maps attach to the north edge and extend south, in both the preview and actual geography. `peninsulaNorthToSouth=true` is stored in new wizard recipes; older recipes without the flag retain their original seeded orientation and stored fields. Coast/Mountain Border orientation remains seed-selected. Terrain choices feed normal worker-backed creation and detailed districts, not just previews. Earlier peninsula seed-orientation statements below are historical.

## Peninsula and Archipelago correction — October31
Peninsula follow-up validation passed: one land component and exactly one land-connected boundary, sampled shoreline/height agreement, deterministic generation and save round trips for all eight serialized shapes, six current UI labels/mappings, normal Peninsula and renamed Archipelago creation, and a real Peninsula coastal district with Crest/palms/dry-road assertions. Unity captures reviewed; compilation and whitespace checks passed. Private evidence: chat outputs/peninsula. Creator left on Peninsula with Environment defaults restored.


Current creator order: Peninsula, Land, Coast, Mountain Border, Pangea, Archipelago. Peninsula replaces the temporary four-large-island choice, with one connected landmass touching exactly one map edge and water on the other three; the seed chooses its orientation. Archipelago is now the renamed nine-small-island option. Pangea remains the renamed single-island option. Serialized IDs are preserved: Peninsula appends7, Archipelago UI maps to SmallIslands=5, and the previous four-island Archipelago=6 remains readable but hidden. No existing terrain/save is reinterpreted. Earlier choice lists below are historical.

## Naming correction — October31

Follow-up verification passed: current six-choice order/mappings, four-island Archipelago creation, renamed single-island Pangea creation with detailed coastal/palm/dry-road checks, all seven serialized shape definitions including hidden legacy Pangea, deterministic fields and temporary save round trips. Compilation and source whitespace checks passed; existing save hashes are unchanged. Current private evidence is in this chat outputs/terrain-naming.

Current creator order: **Archipelago, Land, Coast, Mountain Border, Pangea, Small Islands**. The user replaced the visible One Island label with Pangea and removed the former broad-continent Pangea choice. Current Pangea uses the former OneIsland single-island plan. Archipelago is a new four-larger-island plan, distinct from the nine-small-island option. The first item is Archipelago; Land remains the default.

Serialized enum IDs remain stable: OneIsland=4 backs the current Pangea label; the former Pangea=1 remains readable for existing maps but is absent from the creator; Archipelago is appended as6. No saved terrain or old shape definition is rewritten. The original implementation and validation notes below are historical where naming differs.

# Region creator: Terrain shapes

October 31, 2026. New recipes default to version6. Stored heightfields and version1–5 geography remain authoritative; opening an older map never regenerates it.

## Creator

The working flow is Basic Settings → Environment → Terrain → Create Map. Environment keeps the existing climate/moisture, vegetation, relief, rivers, streams, seed and wildlife choices. Terrain adds a separate persisted `RegionTerrainShape` and a seeded outline preview. It does not reuse vegetation as a landform enum. Land is the default, retaining the existing inland behavior. Name/seed validation gates progression and final creation; later unfinished sections remain disabled.

- Pangea: one broad connected continent, roughly three quarters land for the checked seed, surrounded by ocean.
- Land: inland terrain across the region, with the existing relief/waterway settings.
- Coast: about one quarter open ocean along one seeded edge, with a winding shoreline.
- Mountain Border: mountains restricted to one seeded edge; the interior stays gentle. This mode supplies a mountain edge even when Environment relief is Flat.
- One Island: a smaller connected island with ocean all around, roughly one third land for the checked seed.
- Small Islands: nine separate smaller islands with seeded sizes/shoreline variation, about one sixth land for the checked seed.

The seed determines shoreline variation and which edge the Coast/Mountain Border occupies. There is no separate edge-direction or island-count control yet. The preview is an outline plan; it does not promise exact mountain/river placement.

## Shared generation and water semantics

`RegionTerrainLayout` supplies the same land/ocean plan to worker-backed map composition, semantic ocean descriptors and the wizard preview. The existing MapMagic Noise → landform constraint → Erosion graph remains the elevation provider. Version6 Mountain Border constrains its range to one edge, with height fading out before the interior; other shapes retain the existing relief provider. Composition smoothly lowers terrain to the12 m sea level at coasts and a2 m seabed offshore. Rivers are still the existing procedural network, not a new drainage simulation.

The canonical regional water-distance field combines signed ocean distance and the existing river-bank distances. Ecology, tree exclusions and native terrain sample those shared fields. Detailed districts retain native meadow materials, refined-C canopy clusters, Tropical coconut palms, seasons and the established coastal water presentation. New ocean-layout pilot roads retain only their longest connected dry stretch, including intermediate-segment checks; no automatic bridge across islands/ocean is implied. Existing cached/legacy roads are not rewritten.

Island oceans use an outer water polygon with serialized land holes (`RegionWaterContour`). `RegionWaterBody.Contains` and `PrototypeRegionData.WaterBodyAt` exclude those islands. Ocean intersection testing recognizes fully inland districts despite the enclosing ocean bounds. Beach edges, palm coastal bands and outline surf iterate both outer contours and land-hole contours; map-boundary segments remain excluded. The relief renderer uses the version6 plan for efficient shoreline-distance queries. Ocean-hole JSON can exceed the former64 KiB metadata cap, so the bounded read/write cap is now1 MiB; the existing save schema and old files remain readable.

Shared heights, ocean outlines/holes, distance arrays and the recipe are stored. Load uses those arrays; it does not recreate islands from the latest generator. No existing player saves, artwork or approved color/lighting recipe is migrated.

## Verification

`CityForge.Editor.RegionTerrainShapeChecks.Run(privateOutputFolder)` runs in Editor Play mode from the normal MainMenu. It checks all six component counts, deterministic heights/distance fields, ocean/land descriptor agreement, one-sided mountain confinement, v5-compatible Land output and actual temporary-file save round trips. It then exercises the real three-page wizard, all six selection mappings, invalid-seed guards and staged worker creation for all six shapes, capturing actual Unity regional views. Coastal/island district checks verify actual Crest water, Tropical palms and authored coastal quotas, plus dry-road placement. Temporary snapshots use the OS temporary cache; test regions remain unsaved. Screenshots/builds belong outside public Git.

This is a procedural first implementation. Shorelines/range profiles can be visually refined with user feedback; hydrologically derived rivers, arbitrary island counts/direction controls, settlement placement and sustained performance qualification remain separate work. The original fixed coastline test retains its existing swash geometry and settings.

October 31 final verification passed: six deterministic shape recipes and temporary save/load round trips; one connected landmass for Land/Pangea/Coast/Mountain Border/One Island, nine for Small Islands; sampled ocean/land/height agreement; mountain confinement; v6 Land matches unchanged v5 heights. Checked seed730421 land fractions:1.000 /0.740 /0.742 /1.000 /0.319 /0.166 in enum order. Normal worker-backed creator generated all six64-district regions; navigation/choice mapping and invalid-seed gating passed. Real Tropical Coast district19 had801 approved slots,809 native palms and300 authored palms; One Island district30 had168 approved slots,1,106 native palms and64 authored palms. Both passed actual ocean/palm shoreline and dry-road assertions. Actual Unity screenshots were reviewed. A release ARM64 player build completed with zero errors. git diff --check passed and all three existing Editor save hashes remained unchanged. Test regions were unsaved; the creator was left at Terrain step3, Land selected, with normal default Environment settings restored. No sustained performance claim is made by this functional validation.

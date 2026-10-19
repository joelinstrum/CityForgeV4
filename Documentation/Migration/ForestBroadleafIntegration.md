# Forest maple and oak integration — 2026-10-17

The user authorized adding the optimized 3D maple/oak trees to the game's forests. All native `MeadowDistrictLandscape` regions and districts, including generated region overviews and existing saved test maps, now mix in the eight variants. The approved meadow/poplar terrain and understory remain the foundation. Existing mountain fir placement remains specific to Hills and Mountain Test. No retired prototype broadleaf or whole-tree billboard assets are used.

## Seasons

| Species | Summer | Fall | Winter |
|---|---|---|---|
| Silver Maple A/B | Green foliage/material variant | Golden/orange source foliage subset | Bare branch geometry |
| OakPilot A/B | Optimized original Summer FBX | Optimized original Autumn FBX | Optimized original Winter FBX |
| Canyon Live Oak A/B | Evergreen | Same foliage | Same foliage |
| Southern Live Oak | Evergreen | Same foliage | Same foliage |
| Angel Oak | Evergreen with Spanish moss | Same foliage | Same foliage |

Maple green/yellow mesh subsets are filtered into distinct seasonal meshes. The nominally GreenLeaf source textures were yellow-green; Summer albedos receive an HSV green conversion while preserving source opacity/detail. Fall uses the source YellowLeaf texture subset. Winter removes all leaf submeshes and compacts away unused leaf vertices. These are derived seasonal versions, not three separate artist-authored maple tree models. OakPilot uses the actual separate seasonal source exports. No new snow-covered foliage or bark textures were invented. Existing Summer/Fall/Winter buttons select the corresponding append-only prototype palette without moving trees.

## Runtime contract

`ForestTreeMix` replaces 24% of eligible remaining mature poplar placements (indices 3–10), selected deterministically by position and seed. It retains placement count, position, rotation, understory, roads, ground-cover cache, terrain and water. It leaves existing fir and broadleaf indices unchanged. New broadleaf trees use a stable uniform scale range of 0.8–1.12, with nominal species heights 13–18 m. Forest selection remains governed by the existing biome/vegetation placement fields; the new species do not populate open ground independently.

Prototype slots must never be reordered:

- 0–14: approved seasonal poplar / understory library.
- 15–18: existing Evergreen Pilot, Balsam fir, Fraser fir, Cilician fir; placements remain Hills and Mountain Test only. Other native regions reserve these slots to keep broadleaf indices identical everywhere.
- 19–26: Silver Maple A, Silver Maple B, Canyon Live Oak A, Canyon Live Oak B, Southern Live Oak, Angel Oak, OakPilot A, OakPilot B.

`MeadowDistrictLandscape.TreePrototypes` assembles these palettes through `ForestTreeMix.Prototypes`. The original baseline asset and `SeasonalTreeLibrary` arrays remain unchanged. Apply the fir mix first on the mountain test, then the broadleaf mix. Reapplying either mix is stable. Mature broadleaf trees remain visible when small plants are disabled. Seasonal swaps preserve every prototype index and saved placement.

`SavedRegionStore` validates the new slots in any native region; reserved fir slots remain valid only in the mountain test. The binary layout is unchanged. Opening a pre-integration save deterministically adds the mix in memory; files are not rewritten automatically. Normal Save Region persists the new indices. Old builds that predate these slots cannot read new species indices.

## Assets, import, and LODs

Runtime prefabs: `Assets/CityForge/Resources/Trees/ForestBroadleaf/<species>/<season>.prefab`. Live oaks share their Summer prefab across all buttons. There are 16 prefab palettes total: three each for the two maples and two OakPilots, plus four evergreen live oaks. Every prefab uses three genuine mesh LODs and a culling threshold; no whole-tree sprite/billboard LOD was added.

Prepared local art, compact Unity mesh assets, material recipes and textures: `Assets/CityForge/Art/Trees/ForestBroadleaf/`. Separate source opacity maps are packed into albedo alpha. Imported normals use Unity normal-map settings. URP/Lit materials are matte, instanced, with clipped two-sided foliage and bark back-face culling. `ForestTreePalette` applies deeper neutral/green multipliers to only these new trees, addressing washed-out source foliage under the approved lighting. Angel Oak's original bark, leaves and moss are mapped to the corresponding V3 repair color/opacity maps rather than importing its unsupported V-Ray shaders.

Summer maple geometry is cheaper after removing the alternate Fall leaf set: Maple A 22,789 / 7,517 / 3,068 triangles; Maple B 29,748 / 9,650 / 4,109. Winter maple has about 8–10k near, 2.5–2.7k medium, 1–1.3k distant triangles. Most oaks remain near 59–60k / 20–23k / 8–10k triangles. The Unity import report contains exact seasonal counts. This is geometry accounting, not an FPS benchmark; large forests and multiple leaf materials still incur rendering cost.

Rebuild the base geometry first using `Tools/Trees/BroadleafOptimization/run.py`. Then run `Tools/Trees/BroadleafIntegration/run.py` with Python/Pillow and Blender 5.1.2; this prepares the additional actual OakPilot seasonal exports, reads used material slots and prepares the local Unity texture/model recipes. Optional arguments select the Blender binary, project root, V3 root and local work folder. In Unity run `CityForge/Import/Optimized Forest Broadleaf Trees`, or write `build` to `Library/BroadleafImport.request` after compilation. The importer requires stopped Play mode and preserves existing asset GUIDs on rebuild. V3 originals remain read-only. Art, mesh/prefab binaries, texture recipes and models remain excluded from the public Git repository.

## Verification

`Library/ForestTrees.request` runs `ForestTreeChecks`: eight variants, stable substitution and saved positions/rotations, retained firs and original seasonal library, all seasonal prototype mappings, bare winter maple, evergreen live oaks, three textured 3D mesh levels, small-plants visibility, temporary disk round trip, cache restore, and untouched original player save. It captures all three forest seasons, a wide view, and one specimen of each new species. The current Hills and Mountain Test had 1,081 new broadleaf trees (134 / 137 / 149 / 118 / 154 / 125 / 132 / 132 by slot) among 15,243 total placements.

The existing four-district wizard/biome integration check passed with the new palette. Mountain evergreen regression checks also remain part of this change's validation. Technical checks and inspected Unity captures are not final user approval of every tree's appearance. The ground and ocean appearance/settings were not changed by this integration.

## Forest performance isolation (2026-10-17; current behavior)

The user approved the new trees' appearance, then reported a performance regression. Keep their geometry, materials and seasonal palettes intact. The eight latest maple/oak additions now default OFF via `ForestTreeMix.EnabledSpeciesMask` (PlayerPrefs `CityForge.Forest.SpeciesMask`, default 0). In a district, **NEW TREES: OFF** opens eight independent switches plus NONE/ALL. Selection persists globally and can be cumulative. Native poplars and the existing mountain firs stay enabled.

Canonical generation still assigns append-only species IDs 19–26. Disabled additions display native seasonal poplars at the same positions instead; canonical IDs, sizes and rotations remain in memory and saves, allowing later re-enabling without regenerating or rewriting player saves. Fallback poplar sizes use the native baseline; this is not an exact reconstruction of each original poplar subtype. Only enabled broadleaf prefabs load through the normal runtime palette. Never remove the new assets or change the approved meadow ground to address this regression.

Fixed-camera editor comparison at 3434×1728: wide native poplar/fir presentation 16.11 ms/frame versus all eight additions 30.91 ms; draw calls 14,928 versus 34,855. Individual species were 16.50–18.83 ms. Close comparison was 17.44 versus 33.56 ms. Combined rendering cost is the finding; this does not establish a specific CPU/GPU bottleneck. Many oaks use 12–13 materials. Start reintroduction with Maple A/B or Angel Oak, then measure cumulative combinations. See `Documentation/03_Technical/Forest_Performance.md`. `Library/ForestPerformance.request`, `Library/ForestSwitches.request`, and `Library/ForestTrees.request` run the comparative, UI/persistence, and seasonal/asset checks respectively. The latter temporarily enables all species and restores the previous preference afterward.

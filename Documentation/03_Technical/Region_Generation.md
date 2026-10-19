# Region generation and four-district biomes

The creation wizard generates one continuous 513 × 513 map. Its four districts are Southwest, Southeast, Northwest, and Northeast, each occupying an exact half-width/half-height quadrant. They share terrain and water data. Version-2 generated regions use the approved meadow landscape in both the full-region overview and individual districts, including the existing terrain material and seasonal tree library. The overview is cached while visiting districts; legacy prototype ground and trees are disabled for these regions.

Region sides remain 1,024 / 2,048 / 4,096 m for Small / Medium / Large. District sides are 512 / 1,024 / 2,048 m respectively. Choose Medium for four 1,024 m districts.

## Creation choices

- Seed, topography, rivers, and streams drive the existing inland terrain/water generator.
- Temperate moisture can be balanced, dry, or wet.
- Vegetation can favor a meadow/woodland mixture, open meadow, or woodland.
- Era and period remain saved provenance; they do not change ecology or advance a simulation.

This is a temperate inland ecology slice. Tropical, desert, snow biomes, islands, coastline generation, lakes, custom district boundaries, and connected drainage networks are not implemented by these options.

## Shared ecology

`RegionBiomeMap` samples a deterministic regional field in normalized coordinates. Moisture depends on the chosen preset, seeded regional variation, and distance from waterways. Woodland potential also responds to vegetation choice, regional patchiness, and elevation. Underwater samples suppress woodland; high terrain reduces it. Local habitat labels distinguish meadow, dry meadow, woodland, riverside, upland, and water.

The same field drives approved overview and district woodland/tree placement, ground coverage, and dry ground contribution. Detailed plant distribution also responds indirectly through the resulting canopy. Existing vegetation assets, seasons, materials, shader sources, and water presentation are reused.

Four district descriptors summarize the field on a fixed sample grid. Each stores its identity, normalized bounds, dominant habitat, average woodland potential, average moisture, estimated woodland fraction, and water fraction. The overview offers four district-entry buttons. “% woodland” is a sampled habitat estimate (woodland potential above 0.55), not measured tree-canopy coverage.

New biome maps use a fixed reference for tree-density normalization. Normalizing by each map's woodland mean would otherwise cancel the user's vegetation choice. The legacy fixed Northeast stand packing is also bypassed for these maps.

## Persistence and compatibility

Creation recipe version 2 adds moisture and vegetation fields. Terrain samples and semantic water descriptors remain authoritative in saves. District summaries are included in save metadata; runtime summaries/fields are deterministically rebuilt from the saved recipe and samples. The existing binary format is unchanged.

Version-1 created maps and test maps without a creation recipe keep their previous terrain ecology and renderer selection, but the retired prototype tree spawner is removed globally. Unsupported legacy views may therefore show no trees; they must not resurrect the old broadleaf forest. Biome version-2 logic must remain stable for existing saves; future ecology algorithms should introduce a new recipe version. Only the currently cached detailed district is serialized, matching existing save behavior; other districts regenerate deterministically when entered.

## Verification

`RegionBiomeChecks` covers option influence, deterministic sampling, four complete district bounds, all shared biome edges, metadata save/load, and version-1 behavior. Its live check drives the wizard, enters all four districts through overview buttons, compares their rendered terrain edges, and reopens a saved detailed district with its terrain cache and recipe intact. It also checks the approved overview material, seasonal tree prefabs, overview reuse, camera state, and absence of legacy ground/trees. Water activation is checked structurally; river visibility still requires visual acceptance. Tests write only temporary region files. `RegionCreationChecks` retains the original terrain/water-generation checks.

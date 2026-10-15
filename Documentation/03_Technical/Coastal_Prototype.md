# Temporary coastal region prototype

Terrain's Next button now opens a generated 3D region. The region name is used for the heading; Era, Period, Climate, Topography, Rivers, Streams and Land Type do not affect this prototype.

The reference is Joe's October 4 discussion in ChatGPT, “Design modal concepts,” beginning “This is more like it, but let's reduce all of the interesting terrain features.” The requested direction is buildable plains, few hills, no marsh, a southern coastline, and one continuous regional map with four district divisions.

The fixed default seed is 730421. The 28.45 km square is about 25 miles across its diagonal. One 513 × 513 Unity Terrain heightfield provides the region geography. Broad plains dominate; two localized hills sit toward the north. A curved main river and tributary are physically carved into the terrain, connecting to the southern ocean. These are authored procedural paths, not a watershed simulation. Water is a simple opaque URP surface. Forest cover is terrain color, without individual trees. Terrain layers and water are temporary materials, not the finished PBR target.

The four districts currently appear as two continuous boundary lines. District loading, building, saving, hydrological simulation, detailed assets, and final terrain resolution remain future work.

Camera: WASD pan, mouse wheel zoom, right mouse drag or Q/E rotate, Home restore regional framing, P toggle projection. Escape or Return to Menu restores the opening menu. Generated terrain, textures, layers and materials are released when leaving the prototype.

Editor commands:

- **Validate Coastal Prototype** checks ten seeds for reproducibility, 18–33% water coverage, and at least 70% of land under a two-degree slope.
- **Preview Generated Coastal Region** requires Play mode and invokes the real Terrain Next button.


## Fixed-region presentation pass — 2026-10-04

The default seed remains 730421. The geometry generator was not edited during this pass.
`PrototypeRegionData.cs` SHA-256: `307921622584c3d0053ee39c5200216ee7aa77caa0cfd63af2decd6083d84fda`.

- Six land-cover albedos created individually with the built-in image generation tool. Exact prompts live in `Assets/CityForge/Art/Environment/RegionPBR/GenerationManifest.json`.
- Each material has independently authored, restrained tangent normals and a linear packed mask: R metallic (0), G AO, B optional height, A smoothness. These are deliberately subtle regional surface responses, not photographically reconstructed ground microdetail.
- `RegionLandCover.shadergraph` is an editable URP Lit graph. Its custom HLSL blends the six array layers using elevation, slope, water distance, moisture and forest coloration, with rotated, differently scaled world-space samples and 730–4700 m macro masks.
- A mesh renders the exact original 513 × 513 heights; Unity Terrain retains collision. No hill, coast or river coordinates were changed.
- The water plane now uses depth coloration, analytical sky reflection, Fresnel, small moving surface normals, sunlight highlights and shallower shore color. It does not reflect scene objects or use underwater refraction.
- Riverbanks blend through fertile wet grass/soil; coastal sand/soil blends into grass. The original 55.6 m geometry spacing still limits coastline silhouette smoothness.
- Directional late-morning sun, restrained ambient skylight, extended regional shadow distance and subtle linear haze replace even lighting. Shadow distance and fog settings restore on return; shadow distance also restores on editor Play exit.
- No trees or district UI changes were made.

### Verification

- Built all six PBR arrays and imported Shader Graph without shader errors.
- Exercised the actual Terrain Next listener in Play mode and returned with Escape.
- Existing deterministic/buildability checks passed for ten seeds; fixed seed has 84.8% of land below 2 degrees and 24.8% water.
- Captured and visually inspected `Screenshots/Region_PBR.png`: muted broad land-cover coloration, no identifiable blades/stones/flowers, no obvious texture repeat, dark water and soft soil/sand transitions. The very gentle fixed geometry produces restrained relief at this map scale.

Regenerate assets with **CityForge → Build Region PBR Kit** outside Play mode.
Preview with **CityForge → Preview Generated Coastal Region** in Play mode.
Capture with **CityForge → Capture Region Presentation** while the region is open.


## Oblique presentation experiment — 2026-10-04

Region preview and Home reset now use orthographic pitch 60° below horizontal and yaw 40°.
Framing projects the full square into camera space, includes its existing height range and fits both screen axes with 12% margin.
Sun pitch is 38° and yaw 220°, opposite the camera's azimuth; existing light intensity and soft-shadow strength remain unchanged. Region fog is disabled for this experiment.

Verified in Unity Play mode through the Terrain Next button: full four-district region visible with margin. Captured `Screenshots/Region_Oblique.png`; preserved the prior screenshot as `Screenshots/Region_PBR_Previous.png`.
All 35 sampled generator, texture, shader and material asset files retained their SHA-256 hashes. Terrain heights, coastline, water/river surface and district construction code were left unchanged. The oblique view still shows very restrained relief in the current fixed region; this pass adds no geometry to exaggerate it.


## Macro relief experiment — 2026-10-04

Seed 730421 remains fixed. Added three deterministic broad elevation masks: a dominant NW upland (roughly 9 km / 5.6 miles across at half amplitude), an elongated northern ridge and a low western rise. Existing noise amplitude/frequency are unchanged. Relief tapers toward the rivers over 550–6500 m and toward the southern coastal plain over 1000–8500 m, leaving the immediate bank and shoreline bands intact.

Expanded TerrainData's vertical range from 600 to 1200 m to fit the heights without clipping. Kept the material field's existing 600 m encoding, preserving the water shader's physical depth calculation. Camera, light settings, textures, shader/material assets, waterways and district layout code are unchanged.

Unity validation: peak 970 m; NW sample 943 m; SE plain sample 48 m; 74.8% of land below 2°; 24.8% water. Every one of 263169 water footprint and distance samples matches the baseline. Determinism/buildability checks passed for ten seeds. Camera/material assets retained all 35 recorded hashes.

Captured and inspected `Screenshots/Region_MacroRelief.png` from the unchanged Region Preview camera. Previous same-camera baseline remains in `Screenshots/Region_Oblique.png`. The constructor's `includeMacroRelief=false` option supports baseline regression checks; the game's default always includes this pass.


## Region relief diagnostic

Region Preview has a **Gray Relief** toggle beneath Return to Menu. Press **G** to toggle it, and use the **1× / 2× / 4×** buttons or number keys **1 / 2 / 4** while enabled. The preview starts with the original PBR terrain material at true 1× elevation; gray relief remains available for comparison. Disabling the diagnostic restores the original material and true elevation.

The diagnostic uses a temporary neutral gray URP Lit material. Exaggeration affects only the land rendering transform, anchored at the existing 12 m water level; district guides follow the displayed surface. Generated heights, mesh vertices, collision heights, river/coast intersections, water rendering, camera and lighting remain unchanged. Existing terrain material assets are preserved.

Unity **CityForge → Validate Region Diagnostics** exercises all three scale buttons and restoration, checking the source mesh and TerrainData as well as the camera, sun and water transforms. Validation passed. References: `Screenshots/Region_Diagnostic_1x.png` and `Screenshots/Region_Diagnostic_4x.png`.


## Structured macro formations (Seed 730421)

Replaces the previous additive Gaussian macro relief with explicit flat-topped upland masks and elongated ridge masks. The major layer consists of a northward elevation gradient, a dominant NW plateau (about 7.6 × 8.0 miles across its outer shoulders), a northeastern low upland, a western rolling area, two low ridges, and a broad central basin. Smooth drainage shoulders around both existing rivers extend to 4.3 km from their centerlines, creating valley corridors up to approximately 8.6 km wide. The southern coastal plain transitions over 1–4.8 km inland. Only 3 m of low-frequency Perlin irregularity remains in the new inland base; the old inland noise and hills are replaced rather than amplified. Original channel/bank and coastal intersection bands are retained.

Unity validation: fixed seed has **72.8% of land below 2°**, peak **1,027 m**, NW sample **959 m**, SE sample **51 m**. All 263,169 water footprint/distance samples match the preserved original geography. Ten deterministic seed checks pass (72.0–72.8% gentle land). Diagnostic 1×/2×/4× and restoration checks pass. Camera, lighting, district layout, shaders and texture/material assets are unchanged.

Preview is left in neutral gray at true 1× elevation. Reference: `Screenshots/Region_StructuredRelief_1x.png`.


## Geographic fields replacement (Seed 730421)

Supersedes the preceding radial mask experiment. Active inland elevation now derives from the existing drainage network and two irregular Catmull–Rom upland axes. True point-to-polyline distances are cached at every heightfield sample. River valleys rise smoothly from 600 m out to a capped 9 km distance; the northward gradient also descends over a broad 6 km valley transition. The dominant western axis and lower eastern axis continue beyond the region edges, avoiding circular endpoint formations. Cross-axis falloffs provide broad crests and shoulders. The southern coastal plain lowers the geographic fields over 1–5 km inland. A final noise term spans only 3 m and cannot define the major landforms.

The earlier inland radial/Gaussian landforms and straight ridge masks are no longer used. The frozen original height calculation survives only to preserve the immediate shore/channel intersection bands and provide the original water-footprint validation baseline.

Validation passed: **73.5% of land below 2°**, **983 m peak**, **976 m NW sample**, **109 m SE sample**. Broad river-valley cross-section checks pass, as do ten-seed determinism/buildability checks (73.2–73.9% gentle land). All 263,169 wet footprint and water-distance samples are unchanged. Camera, lighting, district layout, diagnostic material, terrain textures and shaders are unchanged. Preview remains gray at 1×; textures have not been restored.

Reference: `Screenshots/Region_GeographicFields_1x.png`.


## Branching uplands and connected city land

The western crest now varies in width and elevation along its Catmull–Rom axis, including a broad saddle. Two tapering shoulder axes connect to it and descend toward the valley. The eastern upland has a lower varying profile. Branches share the maximum elevation envelope rather than summing their heights. Secondary shoulders slope continuously toward their axes; the main upland retains a broad crest. Axis profiles are averaged over 1.8 km to eliminate ledges caused by hard nearest-segment switches around bends. Existing broad drainage and coastal plain fields are preserved.

Fixed seed 730421 validation: **73.9% of land below 2°**, peak **982 m**, NW sample **895 m**, SE sample **109 m**. A new four-neighbour flood fill checks gentle land connectivity: the two largest components measure **204.7 km²** and **125.4 km²**. These are contiguous slope-qualified areas, not guarantees of a rectangular build site or final gameplay suitability. The largest must cover at least 20% of land, preventing a fragmented landscape from passing on slope percentage alone. All 263,169 water-footprint/distance samples are preserved. Ten seeds pass (73.7–74.3% gentle land).

The 1× gray preview was visually reviewed and remains open. Camera, lighting, waterways, coast and district layout are unchanged. No texture restoration or vegetation pass yet: this is the major-geometry review stage. Reference: `Screenshots/Region_BranchingUplands_1x.png`.


## Broad foothills and softer drainage transitions

Reviewed the unchanged branching terrain at diagnostic 2× before editing. The western main upland and its two shoulder axes now have rounded cross-sections extending **2.4×** their former widths. The broad profile uses 25% of the former axis height contribution; the composite fixed-region peak falls from **982 m to 682 m** (31%). This larger contribution reduction was needed to keep the widened slopes gentle. Overlapping western fields use a smooth maximum over an 80 m height band, softening the old branch-envelope creases. The replacement blends across the western/central region over normalized x=0.20–0.58.

The western valley shoulder rise increases from 180 m to up to 220 m, tapering back to the previous strength toward the east. Upland drainage transitions extend to 7 km rather than 4.5 km. Coast and immediate riverbank intersection bands remain frozen. Every height sample at x≥0.58 is identical to the preceding branching pass, preserving the entire eastern/southeastern plain.

Unity validation passed for ten seeds. Seed **730421**: **73.8%** of land below 2°, **24.8%** water, peak **682 m**, NW sample **462 m**, SE sample **109 m**. The largest two connected gentle areas are **206.5 km²** and **204.7 km²**. All **263,169** water footprint/distance samples are preserved; eastern height equality and broad valley cross-section checks pass. Diagnostic 1×/2×/4× and restoration checks pass. The 35 protected camera, shader, texture and material files retain their recorded hashes. Region view, lighting and district-line code are unchanged.

Visually inspected gray relief at 1× and 2×, then restored the existing six-layer PBR material at true elevation. Region Preview now starts in PBR; G and 1/2/4 retain the gray comparison controls. No new materials or scene features were added. Broad relief is deliberately restrained at 1×; 2× is useful for judging its shape.

References: `Screenshots/Region_Foothills_Gray_1x.png`, `Screenshots/Region_Foothills_Gray_2x.png`, `Screenshots/Region_Foothills_PBR.png`.


## City tile sizes and isometric preview — 2026-10-05

Basic Settings now includes Small (1,024 m), Medium (2,048 m, default), and Large (4,096 m). The selected size and trimmed region name travel through Terrain Next into the live preview. These dimensions apply to the entire current four-district map. The frozen seed 730421 heightfield is reused at all three sizes, with horizontal and vertical geometry scaled together to preserve slopes and the coastline/channel footprint. Resolution remains 513 × 513; these are physical size choices, not SC4 heightmap import formats. Procedural generation remains unchanged.

Region camera is orthographic at pitch 35.264° and yaw 45°. Q/E rotate in 90° increments while preserving the angle. Five discrete zoom stops halve the visible span between steps: level 5 fits the entire region with margin, level 1 is closest (16× relative to level 5). Mouse wheel and HUD buttons select these stops; Home resets framing and zoom 5. The terrain diagnostic continues to use separate 1×/2×/4× elevation factors, anchored at each map's scaled sea level.

Terrain and water shaders now sample the region field using the selected map width. Reduced the water's direct specular peak from 0.5 to 0.06 because the lower camera angle aligned with the existing sun reflection and washed the water out. Existing textures, sun settings and district layout remain intact.

Unity integration check **CityForge → Validate Region Wizard And Zoom** passed actual wizard Terrain Next creation for all three sizes, verifying name transfer, physical map dimensions, orthographic camera pitch, all five HUD zoom stops, and grayscale exaggeration/restoration. Basic Settings layout and the final PBR medium preview were visually inspected. Reference: `Screenshots/Region_Isometric_Medium.png`.


## District entry and tree experiment — 2026-10-05

Click terrain in the four-district overview to enter that quarter. The view reuses the original mesh vertices, heightfield, waterways and terrain material; only the visible triangles and water extent are restricted. Five zoom levels are framed around the selected quarter. Escape or Region Overview returns to the whole region; the region name is retained. Mouse picking uses event coordinates and ignores UI hit targets.

320 trees are scattered deterministically using the region seed. They avoid water, banks and slopes above 18 degrees, with at least 12 metres spacing. Two Himalayan birch variants from the supplied Blender archive were exported into Art/Trees and saved as reusable Resources/Trees prefabs. A wrapper preserves FBX orientation and unit conversion, aligns the lowest point with the ground, and gives approximately 15-metre nominal height, with random rotation and 0.75–1.25 scale. Gray diagnostic mode hides trees. Other tree archives remain available for later species imports.

Validation: Unity compilation succeeded; all four screen-ray picks, quarter mesh triangle counts, neighbouring tree visibility, overview restoration and 320 grounded tree placements passed. Actual mouse entry and Escape restoration were verified in Play mode. Tree scattering is a first asset integration pass; foliage density and shadow appearance need later visual refinement. Tree export preparation is in Tools/export_region_trees.py; CityForge / Import Region Trees rebuilds materials and prefabs.


## Shadows, panning and creation performance — 2026-10-05

Arrow keys now pan in screen directions alongside WASD (Shift still doubles speed). Shadow resolution is 4096; the camera clipping range and runtime shadow distance fit the visible terrain instead of using 70 km. Light normal bias is 0.1. The existing directional light and tree alpha-cutout geometry cast the shadows; no blob decals are used. Verified in Play mode with visibly detailed silhouettes and arrow-key movement.

Measured build stages on this Mac: before, geographic fields/heights 15,533 ms + terrain/surface 372 ms + trees 8 ms; after, 248 ms + 376 ms + 9 ms. The creation stall was fixed geographic field calculation, not tree polygon count. Trees remain expensive geometry for drawing and will benefit from later LOD work.

Resources/Region/GeographicFields.bytes stores the three fixed 513×513 float fields, independent of region seed and size. Every serialized sample was verified equal to the generated float. The generator still computes seeded residual noise and final heights normally, and falls back to computing fields if the cache is absent or its revision/resolution differs. When geographic controls or field equations change, increment FieldRevision and run CityForge / Bake Geographic Fields. Bake takes roughly 16 seconds once in the editor; ordinary region creation avoids that work.

## Fuller canopies and mixed tree species — 2026-10-05

The scatter now uses seven reusable variants: two Himalayan birches, four London planes and one Fraser fir, drawn from all four supplied archives. Original bark, leaf and needle textures are retained, including alpha cutouts and normal maps. Birch exports retain all leaf placements instead of discarding most of the canopy. Each prefab has two mesh detail levels; distant birches retain 70% of leaf geometry. Foliage texture mipmaps preserve alpha coverage at the material's 0.3 cutoff, keeping small leaves visible farther away.

Tools/export_tree_library.py exports the four source packs; the older export_region_trees.py command delegates to its birch export. RegionTreeImporter maps materials by their original names, handles Unity's split FBX meshes, normalizes model height and aligns both detail levels to the same ground point. Placement remains the same deterministic 320-tree experiment with randomized variant, rotation and scale. Nominal plane height is 18 m, birch/fir 15 m.

Unity validation passed all four district picks, isolated meshes, overview restoration, and 320 grounded trees from all seven textured variants with two detail levels. Play mode visual review confirmed fuller green crowns, varied silhouettes and detailed shadows. The birch assets still contain substantial geometry; this pass adds distance detail switching without claiming a measured frame-rate improvement. Reference: Screenshots/Trees_Mixed.png.

## Tree density performance experiment — 2026-10-05

New previews start with 640 trees. The temporary Tree Performance panel adds 320 at a time and measures average FPS / milliseconds per frame over five seconds after two seconds of warmup. Added trees continue the same seeded random sequence, preserve existing placements and follow the same water, slope and spacing rules. Counts are for the whole region; district entry enables only its trees. Measurements include the full Editor frame, not isolated CPU/GPU timing or a standalone build benchmark. Keep the camera fixed during each sample.

On this Mac, with the medium region at full overview zoom 5 and the same framing: 640 trees averaged 54.9 FPS (18.2 ms/frame), 960 averaged 41.0 FPS (24.4 ms/frame), and 1,280 averaged 30.1 FPS (33.2 ms/frame). Adding the two batches of 320 took 7 ms and 11 ms. Initial 640-tree creation measured 250 ms fields/heights + 380 ms surface + 29 ms tree stage. Drawing cost increases substantially at overview density; these results justify further tree rendering optimization before dense forests.


## Distant tree rendering at zoom 4/5 — 2026-10-05

Zoom levels 4 and 5 replace all detailed tree renderers with baked tree images merged into one mesh per variant. Each tree uses two display triangles plus two shadow triangles, with seven display batches and seven shadow batches for the current library. Levels 1–3 restore the original two-level 3D tree prefabs. Tree placement, scale, seed, terrain and lighting controls are unchanged. District filtering and grayscale hiding also apply to the distant batches.

CityForge / Bake Distant Trees renders eight azimuths per variant at the fixed 35.264° region camera pitch into a 2048×256 transparent atlas. The images retain the original foliage/material appearance and bake the current prototype sunlight/ambient settings. Shadows use separate light-facing alpha-cutout cards, avoiding the thin line shadows cast by camera-facing cards. These approximate distant shadows; detailed geometry still casts near-view shadows. Re-bake after changing tree assets or the fixed presentation lighting. Import Region Trees preserves existing baked metadata.

The runtime rebuilds batches when the tree count, district, diagnostic state or camera rotation changes, rather than each frame. Q/E rotations choose the closest baked view relative to each tree's randomized yaw. No image-generation model or replacement tree artwork is used.

Final 1,280-tree comparison at the same medium-region overview / zoom 5: **350.1 FPS (2.9 ms/frame)** with distant silhouettes and shadow proxies, versus **29.6–30.1 FPS (33.2–33.8 ms/frame)** with the prior 3D meshes. Five-second Editor samples on this Mac; this is not a standalone performance guarantee. Total far tree geometry is 5,120 triangles including shadows. Automated checks cover all five zoom levels, detailed-renderer disabling, near LOD restoration, all four districts and diagnostic hiding. Rotation and zoom 4/3 appearance were visually reviewed in Play mode.


## Meadow ground material in URP — 2026-10-05

Manor Lords ground reference guides the close-view coverage: muted green turf, straw variation and irregular earth showing through. The first landscape concept guides the broader palette. This pass uses the existing six albedo layers in the URP Lit Shader Graph; it adds no grass geometry or other props.

Ground albedo now blends rotated samples at 24 m, 41 m and 173 m instead of stretching each texture over kilometers. Separate warped coverage fields at 290 m, 67 m and 14 m control dry grass and thin/bare patches. Base grass has a restrained green tint, and exposed earth uses the existing FertileSoil layer. Existing coast, bank and slope masks retain their geographic encoding. Terrain heights, water geometry, seed, tree placements, camera and scene lighting are unchanged.

RegionPbrKitBuilder authors seamless turf/soil normal response and low smoothness masks. Normal detail is deliberately subtle and fades by screen footprint at distant views. Materials remain nonmetallic. Rebuild with CityForge / Build Region PBR Kit after changing authored normal/mask generation; ordinary blending edits are in Rendering/Region/RegionSurface.hlsl. Screenshots: Meadow_Before.png, Meadow_Close.png and Meadow_Overview.png.

Verification: PBR kit rebuild completed and the material was reviewed in Play mode at overview zoom 5 and district zoom 2. The final normal strength is 0.14, removing the pronounced rippling of the initial experiment. With 1,280 trees at medium-region overview zoom 5, a five-second Editor sample averaged 223.3 FPS (4.5 ms/frame), compared with the preceding material's 350.1 FPS (2.9 ms/frame). The additional material work costs roughly 1.6 ms in these samples; standalone and close-view performance have not been measured in this pass.

## Sparse meadow tufts in URP — 2026-10-05

MeadowGrassRenderer adds small five-blade tufts (0.35–0.8 m tall) at zoom 1/2. Deterministic jittered placement leaves gaps, excludes water/banks and steep slopes, and filters to the selected district. The URP Meadow Grass shader uses the ground material's identical warped meadow/earth coverage fields to thin blades over bare patches. Muted green/straw colors vary per tuft; blade tips brighten subtly. Existing directional lighting and tree shadows illuminate the grass. Grass itself does not cast shadows in this initial sparse-detail pass.

Tufts are combined in 32 m tiles, generated only when visible, with at most eight tiles prepared per frame and a bounded inactive cache. Cached tiles regenerate identically after eviction. Frustum visibility updates on camera or district changes; zoom 3–5 and grayscale diagnostics hide all tufts. Ground materials, terrain generation, waterways, tree scattering and camera settings remain unchanged. CityForge / Validate Meadow Grass checks shader compilation, close detail, distant and diagnostic hiding, source height preservation and camera restoration.

Verified in Unity Play mode: close grass at zoom 1, distant hiding at zoom 3, grayscale hiding at 2×, shader compilation and source region/camera preservation. Northeast district zoom 1 generated 3,881 candidate tufts across 42 tiles (38,810 triangles before coverage clipping). A five-second Editor sample with 640 total trees / 193 district trees enabled averaged 250.4 FPS (4.0 ms/frame). This is the complete close-view frame, not an isolated grass cost or a standalone benchmark. The Resources material retains the grass shader for builds. Reference: Screenshots/Meadow_Tufts_Close.png.

After reloading the final Resources material, the same close-view sample averaged 274.9 FPS (3.6 ms/frame). Preview Meadow Detail opens the Northeast district at zoom 1 for repeatable inspection. Both samples are Editor measurements and naturally vary.

## Rich turf detail and quieter ground coverage — 2026-10-05

Added an original AI-generated grass/clover albedo, MeadowTurf_Detail_v2.png, from the built-in image generation tool. The supplied Manor Lords image guides the desired richness; no artwork or texture was extracted from that game. The new source is saved in Art/Environment/RegionPBR and bound through TerrainKit. Existing six regional albedos are retained. Turf uses two rotated detail samples at 8.5 m and 13.7 m (72%/28%), with small 2.7 m tonal variation. Its fine color and normal response fade with screen footprint from 0.18–0.85 m per pixel. This slightly enlarged art scale keeps turf structure readable from the fixed gameplay camera.

Dry coverage amplitude is reduced from 0.52 to 0.30, exposed-earth patches from 0.55 to 0.16, and forest-cover tint from 0.12 to 0.06. Broad tone variation is now 0.95–1.05 with secondary 0.98–1.02, avoiding large dark splotches. Existing bank/coast/rock geography masks remain intact. Terrain geometry, waterways, tree placement, grass meshes, seed, camera and lighting are unchanged in this texture comparison.

Build Region PBR Kit imports the source with mipmaps and anisotropic filtering, and authors a 1024² linear micro normal asset from small albedo-luminance gradients. This is an artistic approximation to blade relief, not measured PBR height data. The first detail test used 3.5/5.7 m texture periods but appeared too uniform from the camera; the final larger scale and deeper green retain more visible form. Screenshots: Rich_Turf_Close.png and Rich_Turf_Wide.png.

Unity PBR kit rebuild and shader validation passed. Grass zoom/diagnostic checks also passed. Final close-view five-second Editor sample: 175.4 FPS (5.7 ms/frame), 640 total trees / 193 district trees enabled, versus the preceding ground pass's 250–275 FPS (3.6–4.0 ms/frame) samples. The detail texture adds rendering work; this is an Editor comparison, not a standalone guarantee. Preview Meadow Coverage provides a repeatable zoom-4 comparison and verifies both bound detail resources.

### Daylight presentation pass — 2026-10-05

- Region sun: elevation 32°, yaw 205°, intensity 1.55, warm RGB (1, .94, .82); soft shadows, strength .72, existing bias retained.
- Replaced flat ambient with sky/equator/ground colors and an explicit diffuse spherical-harmonic sky probe for this runtime-generated world. Cool sky fill and muted warm ground bounce retain shaded foliage detail.
- Enabled soft shadows in the URP asset (previously disabled despite soft-shadow lights), and assigned URP's bundled PostProcessData to the renderer (previously missing).
- Region camera uses HDR and post-processing. A region-owned global Volume applies ACES, +.35 EV exposure, neutral contrast, and -3 saturation. The initial contrast +4 / exposure +.15 pass crushed foliage shadows; the final pass lifts fill and removes added contrast.
- Runtime profile and components are cleaned up with the preview. Ambient colors, intensity, probe and fog settings restore when returning to the menu or destroying the preview.
- Terrain generation, seed, textures, tree placement and camera framing are preserved. No water implementation changes; the scene-wide tone mapping naturally also affects its displayed color.
- Compare `Screenshots/Rich_Turf_Close.png` with `Screenshots/Daylight_Close.png`, and `Rich_Turf_Wide.png` with `Daylight_Wide.png` at the same Northeast district and zooms 1 / 4.
- Verification: C# compilation and region creation succeeded; close and wide views reviewed and captured; existing coverage menu confirms grass resource bindings and URP shader validity. Final close-view Editor sample: 156.6 FPS / 6.4 ms with 640 total trees, 193 enabled, Northeast district zoom 1. This is a presentation sample, not a standalone benchmark or a guarantee of comparable frame time on other machines.

### Coherent meadow coverage — 2026-10-05

- Added `MeadowCoverFields`: a deterministic 257² linear RGBA map of short turf, rough meadow, dry grass, and sparse soil. A few long vegetation bands and narrower sparse areas leave large open turf clearings; weak boundary warping softens their edges. This is an authored prototype distribution shared across region sizes, not a biome/ecology simulation.
- Ground shading now uses that coverage in place of unrelated dry/bare noise patches. Existing clover turf, dry-grass, soil and woodland albedos are reused at different surface scales; mixed meadow has coarser interwoven grass, dry areas expose straw, sparse areas blend soil with surviving turf.
- Grass geometry uses the same coverage: shorter and fewer tufts in turf clearings, taller and denser grass in rough meadow, straw-toned blades in dry areas, fewer blades over sparse soil. Placement remains deterministic and uses existing tile budgets, water/slope exclusions, culling and zoom hiding.
- Additional fine surface samples execute only at zooms 1–2; zooms 3–5 retain the regional coverage/material blends. This avoids paying for close texture variety at district overview scale. The transition follows the existing discrete zoom system.
- Lighting, seed, heightfield, camera, waterways, coastline, district geometry and tree placement unchanged. Forest edge composition and props remain future work.
- Comparison captures: `Screenshots/Meadow_Cover_Close.png` and `Screenshots/Meadow_Cover_Wide.png`, against the prior `Daylight_Close.png` / `Daylight_Wide.png`.
- Verification: close and wide views reviewed and captured, ground shader resource checks passed, and Meadow Grass validation passed with 4,826 candidate tufts across 42 visible tiles at zoom 1; distant hiding, diagnostic hiding, source-height preservation and camera restoration all passed. Initial wide Editor sample with fine sampling everywhere was 61.9 FPS / 16.1 ms; the first sample after distance gating overlapped asynchronous shader preparation and is not a reliable comparison benchmark. Reassess steady-state performance separately if needed.

### Woodland edge and quieter meadow — 2026-10-05

- Lowered independent rough, dry and sparse ground weights and narrowed their palette differences. Reduced broad and fine tonal noise to avoid camouflage-like patches.
- Kept 640 trees / seven existing variants but biased placement toward three loose stands, including a compact Northeast stand beside the test clearing. Within stands minimum spacing is 6.5 m, transitioning to 12 m outside; water and slope exclusions unchanged.
- A 513² canopy/edge texture derives from actual tree locations using a spatial grid. It drives modest canopy litter/darker ground and a softer rough-meadow edge. The radii are prototype cover approximations, not measured canopy bounds.
- The same spatial queries thin grass beneath trees and increase its height/density around the outer edge. Added trees rebuild the coverage and invalidate grass tile caches so cover remains aligned.
- Existing tree-root name preserved because distant rendering and checks depend on it. No shrubs or new assets added. Lighting, camera, seed and terrain/water geometry unchanged.

Verification: Unity Play mode passed Validate Distant Trees (all five zoom levels, seven batches, all districts, diagnostic hiding) and Validate Meadow Grass (close detail, distant/diagnostic hiding, region and camera preservation). Reviewed the Northeast district at zoom 4; quieter meadow surrounds a loose elongated stand. Saved `Screenshots/Woodland_Edge_Wide.png`.

### Tree lighting consistency — 2026-10-06

Raised explicit ambient probe fill from (.23,.26,.30) to (.32,.35,.39), with modest warmer ground bounce. Kept sun direction/color/intensity and exposure unchanged. Close shadow strength is .64; far atlas proxies use .50 to compensate for denser silhouette coverage. Rebuilt all seven eight-view atlases using the runtime sun and sky-probe settings; baker restores the previous probe. An experimental dithered shadow was rejected due to visible shadow-map patterning and removed. Ground assets, placement, waterways and heightfield unchanged.

Reviewed zoom 1/2 and 4 in Play mode; all-five-level distant tree validation passed. Main keyboard minus was also exercised from zoom 1 through 4. Bark remains naturally dark on some variants; this is an incremental fill/shadow pass rather than a bark texture replacement.

### One-stand canopy experiment — 2026-10-06

WoodlandCanopyRenderer derives 90 NE deciduous crown positions/heights from existing TreeImpostor transforms. Fir trees and outliers retain their existing representations. Zooms 1–2 restore original meshes; 3–5 use one combined crown mesh plus one simple trunk mesh for this stand. Three irregular ellipsoidal lobes per crown, procedural albedo/normal texture, URP Lit, real shadows. Geometry: 33,480 triangles for the stand, versus 360 image/proxy triangles previously. This is an appearance experiment, not a claimed geometry reduction. Same crown mesh at 3–5 currently; reduced region-view geometry and fading remain future work. No change to seed, placement, terrain, water, grass or light settings.

All-five-zoom distant tree check passed, including restoration, districts and grayscale hiding. Play-mode NE zoom 4 sample: 120.8 FPS / 8.3 ms per frame in Editor (not a controlled before/after benchmark). Screenshot: `Screenshots/Canopy_Prototype.png`. Visual assessment: greater overlapping crown coverage, but smooth lobes still read too sculpted; foliage silhouettes/surface shading need further art work before expanding this experiment to other stands.


### Siberian elm-only woodland — 2026-10-06

- Active scatter now loads only `Resources/Trees/SiberianElm`: three summer shapes (A, Large, Young). Existing birch, plane and fir assets remain available for later reintroduction. Existing seed, scatter density/positions, grass, terrain, waterways and scene lighting are retained.
- Imported nine prepared FBX meshes with original UVs and ground-centered origins. URP Lit bark/foliage materials use vendor albedo, normal, roughness and AO; foliage opacity is packed into base alpha with two-sided alpha clipping and mip coverage preservation. Leaf ARS blue specular is not treated as metallic.
- Zoom 1 selects LOD0 (9,785 / 15,126 / 9,451 triangles); zoom 2 selects LOD1 (8,171 / 12,476 / 7,130); zoom 3 selects LOD2 (5,333 / 7,917 / 4,542). Exactly one mesh tier renders per tree. Automatic screen-size LOD selection is disabled during the discrete zoom presentation. Zooms 4–5 use three eight-view atlases and real alpha-tested directional shadow proxies, 4 triangles per represented tree including its shadow proxy.
- Removed the procedural canopy experiment from active rendering; distant views now use actual elm silhouettes. The earlier canopy source remains dormant for reference.
- Repeatable pipeline: run `Tools/import_elm_assets.py`, then `CityForge/Import Siberian Elms` outside Play mode. This builds three prefabs and bakes only their atlases. `CityForge/Preview Elm Woodland` enters Northeast district and frames its stand at zoom 2.
- Verification: Unity compiled; all-five-zoom/district/diagnostic distant tree validation passed (640 represented trees, 3 atlas batches, 2,560 far triangles). District entry/grounding/material/scale validation passed for 640 trees from only the 3 elm variants, each with 3 mesh tiers.
- Editor sample, five seconds: NE district, zoom 2, 170 active trees / 640 total, 102.3 FPS / 9.8 ms per frame. An earlier whole-region zoom 5 sample was 195 FPS / 5.1 ms. Different framing/detail levels are not a controlled before/after comparison.
- Art assessment: fuller irregular crowns are a better basis for consistent deciduous woodland. Current URP leaf shading has pale sunlit surfaces and dark undersides/trunks; matching the reference foliage lighting remains a later material pass. Autumn variants remain prepared in SourceAssets but are not part of this summer-only test.

### Elm crown and forest presentation pass — 2026-10-06

- Added `CityForge/Forest Foliage`, a reusable URP cutout shader. Original leaf geometry/opacity remains intact. Mesh-space crown normals guide broad diffuse lighting, with restrained transmission, sky fill and shadow fill; low smoothness removes pale specular card highlights. Each elm shape receives its own crown bounds/material. Bark and the scene lighting are unchanged. The previous `Leaf.mat` remains available for comparison.
- Zooms 1–3 now retain the full, unthinned LOD0 crown for this experiment. Zooms 4–5 use freshly baked eight-view images of those crowns and existing batched shadow proxies. Lower mesh tiers remain prepared for later performance tuning.
- Packed existing trees within the Northeast test stand to 40% of their original distance from the stand center, then resampled terrain height. No additional trees: 640 total, 170 active in this district. Other woodland stands and scattered outliers retain their positions. Seed, geography, grass and camera setup are unchanged. The increased crown overlap also updates the existing woodland ground-cover field.
- `Resources/Trees/ForestPresentation.json` selects the active prefab folder, full-crown zoom range and test-stand compression. Placement, zoom selection and distant rendering consume these settings; the generic Bake Distant Trees command now bakes only the selected pack.
- A replacement pack can retain this code. Import grounded prefabs with LODGroups, map the leaf atlas/materials using `ForestFoliageMaterials.Create` with mesh-space **foliage** bounds, select its resource folder, then bake its distant images. Material tuning and asset import remain pack-specific; changing the folder alone does not prepare an unconfigured pack.
- Unity compiled and imported the shader/materials. All five zoom levels, far batch counts (3 batches, 640 trees / 2,560 triangles including proxies), district isolation, grayscale hiding, tree scale, grounding and textured material checks passed.
- Review captures: `Screenshots/Forest_Crown_Pass_Zoom1.png`, `Forest_Crown_Pass_Zoom3.png`, `Forest_Crown_Pass_Zoom4.png`. Use CityForge → Forest Canopy → Close / Middle / Distant to reproduce the views.
- Assessment: the middle-distance stand reads more as overlapping crown masses. The elm still has jagged card silhouettes, visible branches and very dark bark up close. This pass improves the existing pack without claiming a complete match to the reference forest.
- Five-second Editor samples at zoom 3: 101.9 FPS / 9.8 ms per frame before the final mesh-space bounds correction; 95.5 FPS / 10.5 ms per frame on the final revision. Both had 170 active / 640 total trees. These are local samples, not a controlled benchmark or a guarantee for larger forests.


### Summer forest FBX assessment and full-density maple test — 2026-10-06

Inspected `summer_forest02.rar`. It contains one combined `summer_forest_FBX_mm.fbx`, two 3ds Max scenes, 15 separate texture maps and three scatter screenshots. The FBX holds four independent tree mesh objects, plus bush and grass objects; it contains no baked forest scatter instances or separate per-tree FBX files. Extracted source FBX/maps remain in `SourceAssets/Trees/SummerForest/Original`. Millimetre source coordinates convert to metre dimensions on import. No archive terrain, grass or bush geometry was introduced into the region.

| Source object | Polygons / triangles | Approx. width × depth × height (m) | Assessment |
| --- | ---: | --- | --- |
| tree_birch | 406,990 | 6.50 × 5.84 × 20.13 | Narrow; not selected for rounded canopy test |
| tree_elm | 570,613 | 13.86 × 14.35 × 16.55 | Broad alternative elm; prepared for comparison |
| tree_linder | 627,292 | 10.74 × 10.54 × 16.25 | More upright linden; prepared for comparison |
| tree_maple | 789,923 | 16.83 × 17.13 × 18.41 | Broadest rounded crown; selected for active test |
| tree_bush | 117,165 | 6.23 × 5.49 × 4.79 | Not imported into active region |
| grass_area | 6,000 | 2.48 × 2.66 × 0.39 | Not imported into active region |

The supplied FBX uses Corona procedural diffuse colors and compound material links. There is no ordinary leaf color bitmap to transfer directly. Unity leaf base textures approximate that green procedural color with supplied mask/translucency detail, and retain the supplied opacity maps. Bark albedos and normals are supplied maps. The simplified bark uses one texture family per tree rather than reconstructing every Corona trunk/branch material. The existing Forest Foliage shader and per-species foliage bounds remain in use; scene lighting and shader source are untouched.

The initial aggressive whole-leaf reduction was rejected visually: crowns became airy and exposed branches at zoom 3. A full-population mixed test was substantially denser but expensive (NE zoom 1: 28.6 FPS / 35.0 ms in a five-second Editor sample). Maple was selected for a second test. Each of its 34,107 modeled leaves is fitted to an affine UV plane and represented by one alpha-tested quad. LOD0 retains every leaf position without enlarging or thinning leaves. This flattens each leaf's modeled curvature; it preserves the spatial leaf population, not every original vertex. Fine disconnected twig components are removed and main bark/branches reduced. Original source geometry is preserved.

| Prepared tree | LOD0 triangles | LOD1 triangles | LOD2 triangles |
| --- | ---: | ---: | ---: |
| SummerMaple (fitted cards) | 82,645 | 53,347 | 32,834 |
| SummerLinden | 422,666 | 274,322 | 168,462 |
| SummerElm | 349,343 | 226,861 | 139,355 |

Active `ForestPresentation.json`: `Trees/SummerMaple`, full-crown through zoom 3, existing test-stand compression 0.4. Exactly 640 trees remain, with 170 enabled in the Northeast district. The existing seed 730421, scatter rules and test-stand packing are retained. Zooms 1–3 render the fitted maple LOD0; lower tiers are prepared but dormant in this full-crown test. Zooms 4–5 use the existing eight-view atlas and batched shadow proxies: one variant batch and 2,560 total represented triangles for 640 trees including proxies. The mixed three-species pack and prior Siberian elms are selectable through `CityForge / Summer Forest`; changing the pack requires leaving and recreating Play mode's region.

Visual assessment: the maple-only zoom-3 stand reads as dense overlapping rounded crown masses, substantially closer to the requested broccoli canopy direction than the jagged elm pass. The same pattern continues at close and distant views. Close leaves still show pale highlights, dark trunk/underside areas and fine cutout breakup. One repeated maple shape limits species/silhouette variety. Distant images remain visibly softer than mesh crowns. This is a selected appearance test, not a claim of a final Manor Lords match or production forest performance.

Five-second Editor samples, same NE stand and 170 enabled / 640 total trees: fitted maple zoom 3 **64.3 FPS / 15.5 ms**; fitted maple zoom 1 **58.2 FPS / 17.2 ms**, with a later sample after the final script reload at **72.7 FPS / 13.7 ms**. No asset geometry or rendering settings changed between those two close samples; the variation reinforces the limits of these Editor measurements. Compare the earlier elm zoom-3 sample of 95.5 FPS / 10.5 ms cautiously: these sequential Editor samples are not controlled standalone benchmarks. Future optimization should retain crown coverage, rather than repeat the rejected aggressive leaf thinning.

Verification: Unity compiled/imported the pack; existing all-five-zoom/district/diagnostic distant-tree validation passed, as did district picks, grounding, nominal scale, material bindings and three-tier checks. The variant assertion now checks the selected pack rather than hard-coding three Siberian elms. Meadow Grass validation passed close/distant/diagnostic visibility and source region/camera preservation. All 61 protected terrain/environment/grass/water/shader/camera files retain their pre-pass SHA-256 hashes (`SourceAssets/Trees/SummerForest/protected_asset_hashes.json`). The only PrototypeRegionView edit is the species-neutral distant-tree performance label; no generation, placement, grass or camera behavior was edited.

Rebuild sequence: Blender `Tools/prepare_summer_forest.py`, Blender `Tools/fit_summer_maple_cards.py`, Python/Pillow `Tools/import_summer_forest_textures.py`, then Unity `CityForge / Summer Forest / Import And Bake`. Select Maple Only, enter Play, and use existing `Forest Canopy / Close`, `Middle`, `Distant` views. Capture through `Summer Forest / Capture Current View` to avoid overwriting the prior elm comparison screenshots.

References: `Screenshots/Summer_Forest_Zoom1.png` (full-population mixed test), `Summer_Maple_Zoom1.png`, `Summer_Maple_Zoom3.png`, `Summer_Maple_Zoom4.png`. Final preview is restored to the existing Northeast close framing at zoom 1.

### Summer maple forest lighting consistency — 2026-10-06

The accepted maple crown shape was retained. The user reported that zooming out made the foliage bright green. Full meshes receive stand shadows; distant trees use an unlit atlas baked from an isolated tree, so the atlas does not retain neighboring canopy occlusion.

Summer leaf tint changed from RGB (0.72, 0.86, 0.62) to (0.50, 0.68, 0.43), sky fill from 1.15 to 1.0, transmission from 0.18 to 0.10; shadow fill remains 0.14. Maple's eight distant views were rebaked. The reusable Distant Tree shader now exposes a stand lighting tint, default white for other assets. Summer impostor materials use (0.55, 0.65, 0.55), selected after comparing zoom 3 and 4. This is an artistic correction for isolated bake lighting, not dynamic canopy occlusion.

Settings persist in SummerForestImporter: Import And Bake reapplies them; Tune Forest Lighting updates leaf materials and rebakes the active maple-only prefab; Apply Distant Lighting Tint adjusts saved summer impostor materials. New screenshots are Summer_Maple_Lighting_Zoom1/3/4.png, preserving the earlier captures.

Validation passed all five zoom levels, districts and grayscale hiding, one image batch and 640 represented trees / 2,560 far triangles including shadow proxies. Terrain, grass, water, camera and world-source hashes remain unchanged; the only protected-manifest difference is the intentional DistantTree.shader tint control. Seed 730421, maple-only pack, 640 total trees, full crowns through zoom 3 and stand compression 0.4 are retained. Unity left in Play at the close Northeast forest view. Distant image silhouettes still show a representation change, but the bright-green jump is reduced.

### CityForge authored broadleaf with Easy Tree — 2026-10-06

Built using Easy Tree's actual Simple Tree Generator nodes for branching, with custom CityForge foliage distribution, leaf-cluster atlas and bark texture. The drag-and-drop installation was not present in Blender's extension folders, so the official repository's assets.blend was loaded directly. No extension UI installation is required to edit the saved branching node modifiers.

Three variants A/B/C use deterministic authoring seeds 730421–730423; world Seed 730421 remains unchanged. Crown: eight overlapping asymmetric ellipsoidal foliage lobes, 7,600 cluster cards per full tree. Approximate authored size 15m wide and 15m tall. Exposed twigs outside crown envelopes are pruned. Current shapes are dense and rounded but still too regular; trunk shape and lighting are first-pass work.

Triangles (LOD0 / LOD1 / LOD2):
A: 31,010 / 19,777 / 11,735
B: 29,880 / 19,072 / 11,311
C: 29,111 / 18,656 / 11,103
This is about 62–65% fewer close triangles than the 82,645-triangle imported maple. It is not an equivalent-performance claim: alpha overdraw also matters.

Active forest folder Trees/CityForgeBroadleaf. Zoom 1 uses LOD0, zoom 2 LOD1, zoom 3 LOD2, zooms 4–5 eight-view baked images. Existing tree placement, compression 0.4, nominal scale, 640 total / 170 Northeast trees are retained. Terrain, grass, water and camera sources unchanged. No shared shader change in this prototype pass.

Unity verification passed all five zooms, district isolation, grayscale hiding, three distant material batches, 640 represented trees / 2,560 far triangles including shadow proxies. Grounding/materials/three detail levels and Meadow Grass checks passed. Local five-second Editor sample: Northeast zoom 3, 91.2 FPS / 11.0ms; not a controlled standalone benchmark.

Authoring source: SourceAssets/Trees/CityForgeBroadleaf/CityForge_Broadleaf.blend. Variant A is visible; B/C and editable Easy Tree branch generators are retained hidden. Export meshes are editable; custom foliage is rebuilt through Tools/build_cityforge_broadleaf.py, not an exposed Easy Tree leaf-density slider. Branch generator's original units are normalized and widened during export, so edit/rebuild through the script to keep final geometry coherent. Our two used textures are packed in the blend file; unused built-in Easy Tree leaf texture references are optional and absent.

Rebuild:
1. python3 Tools/build_cityforge_leaf_atlas.py (Pillow).
2. Blender --factory-startup --background --python Tools/build_cityforge_broadleaf.py.
3. Unity CityForge / Custom Broadleaf / Import Bake And Select, outside Play mode.
4. Preview via Forest Canopy / Close, Middle, Distant; capture via Custom Broadleaf / Capture Broadleaf View.
5. Optional Blender Tools/render_cityforge_broadleaf.py prepares the packed authoring scene and isolated render. Its render destination can be changed in the script.

Saved close/middle/distant captures and a packed source snapshot accompany this report. Existing Summer Forest / Select Maple Only restores the earlier comparison pack outside Play mode. Unity remains at the close Northeast forest view; Blender has the editable source open in material preview.


### Architecture-first broadleaf review — 2026-10-06

The user rejected the prior rounded crown as a finished asset and requested ordinary mature northeastern/midwestern woodland architecture, evaluated at 40–60 degrees elevation. Created a separate review source, not an update to the active Unity pack: `SourceAssets/Trees/CityForgeBroadleaf/ArchitectureReview/Broadleaf_Architecture_Review.blend`. Rebuild script: `Tools/build_broadleaf_architecture_review.py`.

Replaced the Easy Tree pole/umbrella architecture with authored crooked tapered trunk paths, modest root buttresses, nine uneven rising scaffold branches and organically divided secondary/terminal limbs. Voxel union softens branch junctions; tube corrugation and procedural bump provide bark irregularity. Dense leaf-cluster cards are retained, but 41 irregular overlapping crown masses follow terminal and intermediate branch paths with unequal heights and reach. Original cluster atlas reused. Three restrained green material depths separate exterior/middle/interior. The first render revealed excessive spaces between clumps; added branch-following intermediate and inner leader foliage, and reduced root flare.

Saved orthographic review at 50 degrees above horizon. This is a Blender neutral-light review, not the current Unity lighting. Current Unity forest, placement, Seed 730421, terrain, grass, water and camera were not modified. This architecture mesh is unoptimized authoring geometry: no new LOD or atlas/Unity export yet. Evaluate the elevated crown silhouette before additional art refinement or production preparation. Source and render snapshots are in the chat outputs.


### Controlled broadleaf refinement 01 — 2026-10-06

Preserved architecture review in `Baseline_2026-10-06` with hash manifest. New separate `Refinement01/Broadleaf_Refinement01.blend`, rebuilt with `Tools/refine_broadleaf_baseline.py`. Custom procedural architecture, not Easy Tree generator. Four rounded buttress flares replace five pointed root tubes; base radii .60/.49m. Scaffold paths retained. Five selected subcrown radii vary by 4–6%; six transitional masses add 2,989 cards (15.93%), totaling 21,748 cards. Materials and neutral lighting retained. Wood 68,272 + foliage 43,496 =111,768 triangles, unoptimized. Four review views: 50-degree orthographic, closer same angle, 8-degree front/side and top-down. Exact controls, historical Easy Tree overrides and material values recorded in `Refinement01/recipe.txt`; all crown centers/radii and scaffold coordinates in `recipe_geometry.json`. Fine twig fragmentation from voxel union remains visible in branch-only diagnostic; bark and distinct top-view lobes remain review limitations. No Unity replacement, LOD optimization or changes to Seed730421, terrain, grass, water or gameplay camera.


### Crown art-direction lock — 2026-10-06

User explicitly confirmed the rounded broccoli/cauliflower crown character is intentional. Preserve the lower-left, lower-right, central and upper major lobes, overall width, current density, small leaf scale, restrained greens and dark interior. Keep deeper valleys and selected visual separation; do not bridge everything into a homogeneous crown. Subsequent permitted silhouette refinements are minor perimeter protrusions, shallow indentations, uneven leaf sprays, small height differences and selective boundary overlap. Do not flatten, stretch, fragment or substantially irregularize major lobes. No major structure changes before evaluating this exact Refinement01 design through the CityForge gameplay camera. Existing 50-degree Blender renders are neutral-light review images, not Unity captures. Camera controller has a 50-degree default but region framing sets 35.264 degrees; inspect the active runtime view for the actual evaluation. This direction supersedes any earlier suggestion to reduce the major crown lobes merely because they remain visually distinct. No geometry, material or Unity environment changes in this art-direction lock.


### Frozen broadleaf Unity evaluation — 2026-10-06

Exported exact Refinement01 geometry to temporary `Assets/CityForge/Art/Trees/FrozenBroadleafEvaluation`, retaining111,768 triangles/card density; source SHA256 unchanged. Bark UV/normal bake is export-only. Separate URP PBR material translation retains linear source values and roughness; project is Gamma, leaf alpha port uses4x4 ordered dithering. No artistic shader fills or LODs. Source audit reveals interior material is unused (11,390 outer /10,358 middle cards); not corrected. Temporary tests use normal seed730421 medium NE terrain, sun, grass, water and35.264-degree gameplay camera;1,25 and150 trees, rotations and.90–1.10 scales. Same150-tree patch captured atzoom1/3/5. Natural25-tree disk; dense150-tree jittered6.5m grid. Full meshes at every zoom, no pack selection or saved scene change.

Zoom3 evaluation: rounded outlines overlap into a continuous patch; overly dark shading and visible ordered stipple confound internal-valley assessment. Repeated upper puff and perimeter trunks remain noticeable. Material-port/distance coverage needs review before treating these as geometry/palette defects. No automatic artistic corrections. Editor2048x1334 approximate six-second samples:25 trees zoom3 194.0FPS/5.15ms,162 draws,5,850,976 rendered triangles;150 zoom3 69.68FPS/14.35ms,912 draws,33,792,976 rendered triangles including shadows/environment.150 zoom1 53.99FPS/18.52ms;zoom5 98.36FPS/10.17ms. Environment-only zoom3 326.68FPS/3.06ms,12 draws. Sequential Editor results, not standalone/GPU benchmarks. Raw measurements, all captures, small-pan GIFs, source audit, preservation manifest and complete report saved to chat outputs/UnityFrozenEvaluation. Unity left on temporary150-tree zoom3 test; exit Play removes it. Reproduce outside Play with CityForge/Frozen Broadleaf Evaluation/Run Frozen Tests. Protected terrain/grass/water/camera hashes match historical manifest except pre-existing DistantTree.shader change from earlier lighting pass.


### Frozen tree distance coverage and shadow fix — 2026-10-06

Resolved thin distance representation in the frozen150-tree evaluation atzoom4/5 without changing authored geometry/material assets. Prior shared4x4 screen dither aligned transparency holes across all foliage layers. `FrozenLeaf.shader` now has a default-off distance path with per-triangle stable world-seeded thresholds; new `ForestDistancePresentation` applies it through renderer property blocks atzoom4/5 and disables only those trees' shadow casting. Atzoom1–3 original coverage/property blocks/shadow modes are restored. No global sun/camera/URP quality changes, no LODs, no retint/added cards.

Comparison harness: CityForge/Frozen Broadleaf Evaluation/Compare Distance Rendering. Uses exact prior150-tree placements, Seed730421, normal35.264-degree camera, terrain, grass, water and lighting. Before, shadow-only fix, coverage-only fix and combined captures atzoom4/5 confirm both contributions. New distant silhouette is substantially continuous and rounded; broad ground tree shadow removed. Palette remains very dark and3-to4 discrete transition remains visible; not automatically corrected. Editor five-second samples:zoom4 before71.12FPS/14.06ms, after137.07FPS/7.30ms;zoom5 before95.05FPS/10.52ms, after159.85FPS/6.26ms. Draws912->462; rendered triangles33,792,976->17,027,776 by removing shadow passes; source16,765,200 patch triangles unchanged. Sequential Editor measurements, not standalone benchmark. Frozen source blend/FBX/4 materials/texture+import metadata/camera/world/URP/distant renderer hashes unchanged. This policy applies to the frozen test, not globally to legacy impostor packs. Report, all captures, magnified comparison, pan GIFs, measurements and verification saved to chat outputs/UnityDistancePass. Unity left on finalzoom4 view.


### Frozen foliage lighting evaluation — 2026-10-06

Foliage lighting evaluation — 2026-10-06

Exact frozen Refinement01 tree; 150 identical source instances at prior placements, Seed 730421. Actual gameplay camera pitch 35.264 degrees; zooms 1,3,4,5. Terrain, grass, water, sun, camera, base palette, leaf atlas/import settings and material assets unchanged. Previous zoom4/5 coverage and shadow policy enabled in both Before and After.

Changed shading only:
1. Generated position-indexed lighting guides from the 47 existing crown masses in recipe_geometry.json. For each leaf vertex choose the mass with lowest squared normalized ellipsoid radius. Normal = normalize((position-center)/(radius*radius)). Exposure = smoothstep on clamped (normalized_radius-.45)/.50.
2. Runtime clone of shared foliage mesh adds UV1=(guide normal XYZ, exposure). Original vertices, geometric normals and triangle indices asserted identical. 86,992 guide vertices matched, zero missing. Wood untouched; shared original FBX unchanged.
3. _FoliageLighting=1, _LobeNormalBlend=.70: blend two-sided leaf normal toward outward crown normal for diffuse/specular lighting.
4. _DiffuseSkyFill=.65: add diffuse GI .65 * lerp(.10,1,exposure) * RGB(.95,1,1.04). This is a shading fill, not emission or albedo retint. Interior fill is lower than exposed foliage. Existing shadow receiving remains active.
5. Values applied via renderer property blocks in evaluation; shader lighting defaults disabled. No Easy Tree parameter changes. Frozen authored .blend, FBX, material assets and protected environment hashes verified unchanged.

Outcome: outer rounded masses are brighter and more readable, while recessed areas retain dark depth. Zoom4/5 remain continuous rather than thin and no longer cast broad ground tree shadows. The forest remains deliberately darker than open grass. At zoom3 the inherited ordered alpha stippling remains obvious and obscures some fine foliage depth; this lighting pass does not solve aliasing. Repeated crown structure and visible perimeter trunks remain. No geometry redesign, optimization or LODs.

Reproduce: exit Play; CityForge > Frozen Broadleaf Evaluation > Compare Foliage Lighting. Runner leaves final improved zoom3 view active. Temporary evaluation only; not a production forest replacement.

111,768 triangles/tree, 16,765,200 source triangles/150 trees. Editor five-second sequential samples, 2048x1334: zoom3 before57.22/after57.32FPS (912draws), zoom4 before123.99/after123.17FPS (462draws), zoom5 before161.83/after160.85FPS (462draws). No material draw/triangle increase; small differences are sample noise, not a GPU benchmark. Captures1920x1080, GIFs960x540. Comparison image uses identical enlarged crops, no color correction.


### Far canopy coverage and contrast — 2026-10-06

Far canopy coverage and contrast — 2026-10-06

Zoom4/5 only: _FarCanopyPresentation=1. Per-triangle alpha threshold range .01–.99 becomes .01–.35, retaining more distant mip fragments and filling gaps between layers. This changes rendered coverage, not authored foliage density, leaf atlas, source mesh or importer.
Crown lighting normal blend .70 -> .95 at distance. Diffuse fill exposure floor .10 -> .55; outer fill unchanged at1.0, sky-fill amount unchanged .65. Reduces near-black crease contrast while retaining rounded lobe shading. Source colors unchanged. Tree-only shadow casting remains Off at4/5. Nearblack areas in these captures were canopy shading rather than these trees' ground shadows; unrelated regional ground shading remains.
Zoom1–3 shader paths unchanged. Distance component now modifies only its owned coverage parameters when returning near, preserving foliage lighting property blocks across zoom changes.

150 trees, exact baseline placements, seed730421, actual35.264-degree camera, terrain/grass/water/lighting unchanged. Frozen .blend/FBX/material/texture and environment hashes verified. 111,768 triangles/tree; no LOD or geometry changes.
Captures confirm a fuller continuous distant canopy with softer dark regions. Near before/after captures checked in verification.json. Performance measurements.txt contains sequential Editor estimates; not standalone benchmark. Zoom2 shares the unchanged near path but was not separately captured.

Reproduce outside Play: CityForge > Frozen Broadleaf Evaluation > Compare Far Canopy. Final test remains atzoom3; zoom4/5 activate new profile. Temporary frozen tree evaluation only, not legacy production forest packs. Previous Before captures retain the prior lighting fix.


### Bark brightness pass — 2026-10-06

Bark brightness pass — 2026-10-06

Blender bark Principled Base Color linear RGB (.115,.091,.067) -> (.26,.235,.205), alpha1. A lighter, less saturated gray-brown. Saved separate SourceAssets/Trees/CityForgeBroadleaf/Refinement01/BarkBrightness01/Broadleaf_BarkBrightness01.blend; original frozen source preserved. Geometry, foliage, bark procedural texture, bump and roughness unchanged. No Easy Tree parameters changed. Rebuild Tools/lighten_broadleaf_bark.py; material_changes.json records exact Blender edit.

Unity FrozenBark.mat BaseColor and legacy Color now matching sRGB conversion (.546854,.522062,.490685). Smoothness.05, normal map and bump strength1 unchanged. Brighter albedo alone left deeply shaded trunks near black, so enabled URP Lit _EMISSION with RGB (.10,.092,.082), alpha1 as a small constant readability fill. This is deliberately a Unity emission approximation for indirect fill; it reduces shaded bark contrast and does not reproduce physically computed ambient bounce. No emission map, lighting bake, global ambient change or new light. Blender variant contains albedo edit only, not this Unity-specific shading fill.

Actual150-tree gameplay comparison, seed730421, exact placements, zoom1/3/4/5; previous foliage lighting and distant coverage/shadow fixes enabled in both controls. Before uses original albedo with zero fill; After uses new albedo and fill. Perimeter trunks now read as muted gray-brown rather than black silhouettes. Bark remains darker than foliage. Source FBX/.blend, foliage assets, camera/terrain/grass/water hashes unchanged; only protected bark material differs as authorized. Draw geometry unchanged111,768 triangles/tree. Reproduce CityForge/Frozen Broadleaf Evaluation/Compare Bark Brightness. Final test left improvedzoom3.

Measurements are sequential Editor samples; see measurements.txt. Preview uses same cropped image areas and magnification with no color correction. Export/import the Blender variant later using matching Unity material; do not replace runtime foliage guides or regenerate tree geometry for this edit.


### Organic320 placement-only evaluation — 2026-10-06

Organic320 placement-only evaluation — 2026-10-06

Exactly320 instances of the current accepted tree. No variants or LODs created. Geometry, materials, bark fill, foliage lighting, distance coverage and zoom4/5 tree shadow policy unchanged. Seed730421, terrain/grass/water/camera source unchanged. Actual gameplay camera captures atzoom1,3,5; same focus and normal zoom steps. Zoom5 retains the accepted broccoli rendering path.

Placement seed730421. Irregular ellipse nominal radii72x60m, boundary variation8% sin(3angle+.7)+5.5% cos(5angle-1.2). 14 cluster centers, 58% clustered Gaussian candidates (sigma9m), remainder area-uniform. Two9m/7m radius clearings, minimum trunk spacing2.8m, random Y0–360degrees, scale.90–1.10. Terrain height resampled pertree. placements.json records all320 transforms. Nominal footprint13,572m² (~42.4m²/tree), matching earlier6.5m nominal grid spacing (~42.25m²/tree). Prior frozen capture actually contained150 trees; this requested320 test expands area rather than doubling local density. Crown overlap hides some clearings from above, while trunk spacing remains irregular.

Observed: plantation rows and rectangular stand outline removed; zoom3 massing reads more naturally. Existing leaf stippling and repeated individual lobe architecture remain observable; no correction applied. Zoom5 continuous broccoli canopy preserved. Screenshots unmodified gameplay captures1920x1080. Full source mesh at all distances111,768 triangles/tree,35,765,760 triangles for320; not a production LOD strategy implementation. measurements.txt contains approximate sequential Editor frame timing.

Reproduce: CityForge/Frozen Broadleaf Evaluation/Preview Organic320 Scatter; temporary test leaves organic stand atzoom3. Runtime shader guide metadata remains identical. Current tree assets unchanged; original preservation baseline differs only in bark material from previous authorized pass.

Future strategy recorded, not implemented: zoom1 detailedLOD0, zoom2 individualLOD1, zoom3 volume-preservingLOD2 with tree variation, zoom4 canopyLOD3, zoom5 cheapLOD/cluster preserving accepted appearance. No six-variant family until this scatter is evaluated.


### Gameplay broadleaf LOD implementation — 2026-10-06

Gameplay broadleaf LOD implementation — 2026-10-06

Installed in the organic320-tree preview and supplied a reusable runtime component/library. No six-variant family created. Exact320 placements, rotations and.90–1.10 scales retained, Seed730421. Accepted authored .blend and FBX are preserved; optimization writes separate derived game meshes. Terrain, grass, water, camera controller, region lighting and foliage source palette unchanged. Preserved prior bark fill and near/far lighting policy. Existing legacy production tree packs have not been replaced.

Zoom map and exact derived controls:
1 -> LOD0:58,514 triangles/tree:15,018 wood +43,496 foliage. Blender collapse decimation wood ratio.22; all21,748 leaf cards, original leaf positions/size retained. UVs and bark normal map preserved; minor wood simplification is a derived geometry change, not an edit of the authored source.
2 -> LOD1:34,290 triangles/tree:6,144 wood +28,146 foliage. Wood ratio.09; deterministic65% card retention (14,073 cards), sprays scaled1.20 around existing card centers to compensate for reduced layers.
3 -> LOD2:12,780 triangles/tree:2,048 wood +10,732 foliage. Wood ratio.03; deterministic25% retention (5,366 cards), sprays scaled1.80 around existing centers. Retains branch-organized crown volume and lobe lighting. No variation assets added; repeated architecture and stippling remain.
4 -> LOD3:256px per-tree impostors, 8 tree yaw views x4 actual camera orientations, 2048x1024 atlas.320 quads combined into one renderer,640 tree triangles, shadows off. Baked from accepted full source with far coverage and fill. Display gain.70 compensates for the brighter isolated high-resolution bake relative to coarse gameplay leaf mips; original foliage colors unchanged. Alpha cutoff.15, mip alpha coverage preservation enabled. This is a visual approximation, not pixel-identical geometry.
5 -> LOD4:whole organic stand cluster, one quad/two triangles, one renderer. Four views baked from actual320-tree arrangement at approximately accepted zoom5 world units/pixel. This keeps the overlapping broccoli forest rather than displaying the brighter isolated-tree approximation. The stand texture is cached; changes to placement/lighting require rebaking. A100m camera-depth bias prevents a large flat quad intersecting terrain; orthographic screen position stays unchanged. Flat impostors have approximate world depth and may need depth-aware treatment when mixed with future buildings. Cluster fallback uses per-tree impostors if count/stand center do not match the cached320 stand.

Runtime:
BroadleafGameplayLOD selects discrete tiers on the existing ZoomChanged event; no mesh generation/baking on button clicks. Near meshes/materials shared; Graphics.RenderMeshInstanced batches by material/submesh with1023-instance chunks and two-sided shadow casting. Shader UV1 lighting guides registered withzero missing vertices for86,992/56,292/21,464 vertices. Source geometry positions/normals/indices were unchanged when adding shading metadata to each derived mesh. Near frustum tests use source bounds expanded20m for shadow safety; static tree matrices are cached. Baking/startup and loading costs are not included in steady-state FPS. Camera-pose changes update billboard orientation; four normal region Q/E rotations are supported. Baked far lighting assumes current sun/ambient; no dynamic-light rebake on zoom.

Validation:
All five Before/After gameplay captures and actualEditor frame measurements completed,1920x1080 captures and3434x1728 GameView. Three cycles through levels1–5 checked nonempty geometry and640/2 far triangle counts. Captured camera yaw45,135,225,315 atzoom5; restored45. GUI plus/minus tested in interactive preview. Compare measurements.txt for final values; source controls are full111,768-triangle tree with accepted materials. Draw/triangle statistics include terrain, grass and shadow passes. Five-second sequential Editor samples aftertwo-second warmup, not GPU or standalone build benchmarks.

Important performance limit:zoom1 remains around22FPS because its detailed leaf cards and shadow overdraw are still expensive. Zoom2 around32FPS, zoom3 around65FPS, zoom4 around149FPS and zoom5 around225FPS. LOD switch CPU timing is under.5ms in repeated checks; this does not imply zero input latency. The pass significantly improves middle/far rendering and reduces near geometry/draw overhead, but does not finish close-view overdraw optimization.

Files and use:
Tools/build_broadleaf_gameplay_lods.py generates separateFBX/lighting guides and recipe.json. BroadleafGameplayLODBaker translates materials, registers shading metadata and bakes accepted far views using actual scene lighting. Assets/CityForge/Resources/BroadleafGameplayLOD.asset contains mesh tiers; Organic320Canopy.asset contains this stand's cluster. Runtime BroadleafGameplayLOD can be initialized on a forest root with camera rig, library and optional matching cluster. Matrices/stand bake assume static placements.
CityForge/Frozen Broadleaf Evaluation/Evaluate Gameplay LODs runs Before/After captures and checks.
CityForge/Frozen Broadleaf Evaluation/Preview Optimized Forest opens the optimized320-tree interactive stand with +/− controls, starting atzoom3. Legacy performance panel disabled only in this temporary preview so it does not show the original640-tree count. Unity left in this optimized interactive preview. Exit Play removes the temporary stand; assets remain saved. No saved region scene or global production forest selection changed.

### Dense Forest Trees: controlled seasonal family — 2026-10-06

The accepted rounded broadleaf treatment is now categorized as **Dense Forest Trees**, distinct from future individual-lot trees. Four related shapes, each with Spring/Summer (one shared asset), Fall and Winter presentations. Original Refinement01 geometry and the accepted original organic320 stand/zoom5 cluster are retained. New assets live under `Assets/CityForge/Art/Trees/DenseForestTrees`, resource prefabs under `Resources/Trees/DenseForestTrees`, and category/family metadata in `Resources/DenseForestTrees.asset`. Saved authored Blender sources are in `SourceAssets/Trees/DenseForestTrees`. These are authored family assets and a temporary mixed preview; legacy production regional forest selection and a global seasonal calendar have not been changed.

Exact authoring controls, reproducible with `Tools/build_dense_forest_family.py`:

| Variant | X/Y crown factors | Crown lift | Upper leader adjustment | Angular bulge amplitude |
| --- | --- | --- | --- | --- |
| A Rounded | 1.00 / 1.00 | 0m | 0m | 0 |
| B Tall | .94 / .97 | +.65m | +.55m | .025 |
| C Full | 1.07 / 1.03 | −.35m | −.25m | .035 |
| D High Crown | .97 / 1.01 | +1.80m | +.20m | .025 |

These are Blender Z-up coordinates. Every wood and foliage vertex receives the same smooth field: `t=clamp((z−2)/5)`, `s=t²(3−2t)`; horizontal coordinates multiply by their table factor and `1+bulge*sin(3*atan2(y,x)+.8)*s`; height adds `lift*s + upper*clamp((z−10)/6)`. Root position, flare, branch connections, original card count, broadly rounded lobes and their valleys are retained. This is a controlled family from the accepted tree rather than four independently generated species. No Easy Tree generator parameters changed: this source is custom procedural Blender geometry. No leaf cards added or removed in authored summer/fall sources. Spring and summer deliberately share the same assets.

Fall: separate grayscale leaf atlas derived from the source texture using linear luminance `.2126R+.7152G+.0722B`, original alpha retained. Summer atlas is unchanged. Linear tints: muted ochre `(.44,.34,.13)`, orange brown `(.33,.20,.105)`, retained olive `(.29,.32,.18)`; roughness .93. Card material assignment follows spatial patches: `a=sin(.63x+.43z+variantIndex)+.65cos(.72y−.35z)`; ochre for a>.15, orange brown for a<−.55, otherwise olive. This avoids random confetti leaves and retains green patches. Unity's custom leaf shader takes **linear** tint vectors even in this Gamma project; no gamma conversion on these tint values. Accepted lobe blend .70 and sky fill .65 retained.

Winter: remove foliage, retain the same deformed trunk/scaffolds. Add connected terminal twig fans to the 41 original crown-mass attachment areas, choosing the closest existing woody vertex as the attachment. Seed730421, four fans per mass, tapered five-sided tubes: initial radius .024m, end .20 of radius; length .75–1.55m, upward direction Z .35–.85; two secondary twigs per fan radius .012m. The addition is winter-specific fine structure that was obscured by summer foliage, not a crown redesign. Snow is separate geometry copied from upward-facing woody polygons where face center Z>=3.5m and face normal Z>=.5. Offset upward `.045+.035*(.5+.5*sin(2x+1.7y))`m (4.5–8cm). Snow linear color `(.72,.77,.80)`, roughness .98. Vertical trunks are not coated. No terrain snow or winter lighting/environment changes in this evaluation. Snow coverage is intentionally light; at gameplay distance it is subtle, and fine bare twigs alias visibly against grass.

Bark retains approved linear `(.26,.235,.205)`, roughness .95, original noise scale7/detail3/bump strength.35/distance.065 in Blender; Unity uses the existing normal texture and readability emission `(.10,.092,.082)`. Runtime bark override is now restricted to materials named Bark, so it cannot accidentally recolor new snow meshes. No global light or camera changes.

Derived game mesh settings match the accepted LOD strategy: wood collapse .22/.09/.03; foliage deterministic keep1/.65/.25 with spray scale1/1.20/1.80 and seed730421. Summer/fall triangles:58,514 /34,290 /12,780 per tree. Winter wood+snow triangles: A18,803/7,691/2,966; B18,568/7,595/2,882; C18,980/7,763/2,962; D18,541/7,584/2,940. Lighting guides rebuilt against each deformed crown, all imported foliage vertices matched, zero missing. Each seasonal library has a separate 8-yaw x4-camera-view atlas baked with the existing gameplay baker, near assets and far impostors consumed by existing `BroadleafGameplayLOD`. New mixed forests currently use per-tree impostors at zoom4/5; a mixed seasonal whole-stand zoom5 cluster has not yet been baked. The accepted original320-tree cluster asset is not overwritten.

Evaluation: `CityForge > Dense Forest Trees > Build And Preview Seasonal Family` builds if the saved family is absent, then loads it for preview. Actual Seed730421 region terrain/grass/water/sun and35.264-degree camera. Four isolated variants and a32-tree organic mixed cluster, random Y and .90–1.10 scale, all three seasons. Close captures at normal zoom1; additional original zoom3 captures and summer zoom4/5 saved in chat outputs/DenseForestTrees. Near crowns remain rounded; D reveals more trunk; fall is muted and dark under existing lighting, with olive retained. Winter twigs/snow are much thinner at distance than summer canopy. No new performance benchmark conducted. Interactive buttons switch seasons; view returns to summer zoom3 after automated captures. Standard resource prefabs contain three conventional LODGroup tiers; forest zoom4/5 should use the library/runtime component rather than the standard prefab alone. Static atlas lighting limitations still apply.

`family_recipe.json` contains exact variant controls, palettes, source hash and mesh counts. No source Easy Tree parameters were changed. The authoring script, imported family metadata, libraries, Blender files and captures form the reproducible first seasonal family for review.

### Dense Forest Trees v2: distinct architectures — 2026-10-06

The first seasonal family was rejected as visually too similar. Its small deformation controls above are historical and **superseded for B/C/D**. A retains the accepted rounded baseline. B/C/D now have separately generated scaffold paths, different branch counts, crown heights, foliage mass arrangements and authoring seeds; they are no longer deformations of the same branch/leaf mesh. Shared leaf scale, dense crown treatment, bark and seasonal materials remain.

| Stable asset key / display name | Main scaffolds | Secondary limbs | Leader height | Branch reach | Branch-tip height range | Foliage cards | Authoring seed |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A_Rounded / Rounded baseline | 9 | 27 | original | original | original | 21,748 | 730421 |
| B_Tall / Tall oval | 7 | 21 | 16m | 2.30–3.25m | 10.5–13.8m | 22,499 | 731434 |
| C_Full / Broad spreading | 5 | 20 | 12m | 4.30–5.55m | 7.0–9.4m | 25,002 | 732447 |
| D_HighCrown / Uneven multi-leader | 8 | 24 | 15.4m | 3.00–4.50m before side adjustments | 8.0–13.3m before side adjustments | 23,500 | 733460 |

B has higher branch origins (6.1+.43*i metres), narrower reach, and three overlapping upper/central lobes rather than the baseline's recognizable top puff. C has five heavier major limbs starting at3.5+.42*i metres, four secondary limbs each, a broader/lower crown, and fewer large crown masses. D begins at4.4+.85*(i%4) metres; scaffold reach expands25% for limbs0/1/6 and contracts20% for the others; limbs2/3 gain1.1m height, limbs5/6 lose1.0m. Unequal groups of2/3/4 secondary limbs and four offset inner/upper masses give D a skewed, uneven outline. Branch angles use evenly distributed sectors plus seeded ±.28rad jitter and variant*.42rad rotation; secondary angles have their own ±.12rad jitter. Trunk lean and major limb thickness also differ. The existing stable asset keys are retained to preserve Unity references; inspector display names describe the revised shapes.

New main lobe radii B(1.55,1.75,2.1), C(2.35,2.15,1.8), D(1.8,1.9,1.85)m, scaled independently .92–1.10; secondary masses .68–.90 of their major lobe. All crowns remain densely composed of irregular rounded local masses, but overall silhouettes differ substantially. Card budgets distributed by lobe volume, small spray size .46–.70m retained. Radius distribution now cbrt(uniform(.18,1)), permitting a genuine dark interior layer in B/C/D. Outer/middle/interior tints remain accepted summer values; color was not used to fake geometry variation. No Easy Tree parameters changed. Exact scaffold polylines and foliage masses are recorded in updated family_recipe.json; full algorithm is `Tools/dense_forest_architecture.py`, called by `Tools/build_dense_forest_family.py`. V1 authoring script retained separately under SourceAssets/Trees/DenseForestTrees/family_v1_authoring.py.

Summer/fall LOD triangles: A58,514/34,290/12,780; B56,432/33,808/12,650; C61,664/37,162/13,988; D61,510/36,354/13,594. Winter: A18,803/7,691/2,966; B13,427/5,492/2,024; C14,560/5,955/2,024; D16,919/6,920/2,426. Winter source skeletons, snow caps, lighting guides, all derived mesh tiers and seasonal atlases rebuilt from their corresponding new architecture. Same fall palette/snow policy and same gameplay LOD thinning as v1. Saved family/mesh asset GUIDs preserved during rebuild through CopySerialized; prefabs updated in place. The builder menu now explicitly rebuilds before previewing, avoiding stale cached family assets.

Evaluation uses the actual region camera and unchanged seed730421 environment, isolated four-tree comparison plus mixed32-tree cluster. Compare new captures at gameplay zoom1 and summer far views. New silhouettes are intentionally substantially different at the user's request, superseding the earlier instruction to preserve identical major lobes across family members. Accepted A, original source, and original organic320 zoom5 cluster are preserved. New mixed-family zoom5 continues to use per-tree impostors until a new stand cluster is approved/baked; no production regional forest replacement or new performance claim in this pass.

V2 validation completed after restarting the Unity Editor: the long-running prior graphics session returned zero pixels even for a white Graphics.Blit test. Startup deferral alone did not resolve that session fault. A fresh Editor produced populated atlases and normal gameplay captures using the existing URP SingleCameraRequest path. All12 atlas alpha channels and all three seasonal close comparisons verified nonempty; source Refinement01 SHA256 remains cd5d4b5ca42f1c38334e47d6399ca99cb3722862599eb63d4f5eb0c90abdbadf. The baker now rejects an empty atlas before replacing the texture, and updates existing impostor materials in place. `Preview Saved Seasonal Family` provides a capture/interactive preview without rebuilding assets; `Build And Preview Seasonal Family` explicitly rebuilds. The revised summer/fall/winter comparison is outputs/DenseForestTrees/DistinctFamily_Unity_Preview.png; v2_verification.json records populated atlases and source preservation. New Editor log /tmp/cityforge-dense-family-v2-editor.log. No additional artistic corrections after these captures.

### macOS player build repair — 2026-10-06

The player build failed on Metal in RegionLandCover Shader Graph MotionVectors: `BuildVertexDescriptionInputs` was undeclared. The graph had an empty vertex context. Added the default VertexDescription.Position output through RegionPbrKitBuilder.BuildGraph and regenerated only the graph (no texture/material kit rebuild). The generator now preserves this output on future regeneration. No terrain surface calculations or forest assets changed.

Verified a complete StandaloneOSX player build with both enabled build scenes (MainMenu and VisualTest): Succeeded, 0 errors, 0 warnings. Output: `Builds/Verification/CityForge.app`; log: `/tmp/cityforge-player-verification.log`. Reusable menu: CityForge > Verify macOS Player Build.

### Separate Close LOD0 treatment — 2026-10-06

Candidate assets live in Art/Trees/DenseForestTrees/CloseLOD0 and SourceAssets/Trees/DenseForestTrees/CloseLOD0, with a separate DenseForestTreesClose resource library. The established family is untouched. Each close library replaces meshTiers[0] only and keeps the original meshTiers[1], meshTiers[2], impostor material, center and span. No atlas rebake, no placement changes.

Authoring: Tools/build_dense_forest_close_lod0.py opens each saved family source and retains every foliage vertex exactly (SHA256 checked). Wood reduction is .65 rather than .22, preserving more authored junction and secondary branch geometry. Lower-trunk cross section receives 6.5% five-fold and 3.5% nine-fold irregularity, with a five-root flare contribution up to36% below.75m. Bark geometry relief is9mm and5mm in two frequencies. Original curvature and taper remain. The secondary branch pass adds curved branch fans beneath alternating existing foliage masses: a .045m base-radius support and three .021m tapered offshoots, each using9 rings/8 sides and sinusoidal curvature. Existing canopy openings are retained; no new openings are cut.

1024px seamless procedural hardwood bark maps provide warm gray brown albedo, longitudinal fissure/plate normal detail (strength .65), and roughness .82–.98 through inverse roughness in the URP metallic map alpha. Metallic remains0. Emission fill is(.16,.145,.125), revised after the first Unity capture showed black trunks; textured albedo is white-tinted. UV mapping uses cylindrical trunk and projected limb coordinates; close inspection is required for junction texture distortion.

Close leaf material retains existing atlas and linear summer/fall palettes, but reduces lobe normal blend .70→.35, sky fill .65→.50, adds .88–1.08 spatial spray color variation at .5m scale, and replaces screen Bayer alpha dithering with .32 cutout at Zoom1. Leaves are not increased or made smaller; the source sprays are preserved. Easy Tree settings unchanged: these remain custom authored derived assets.

Menus: Build Close LOD0 Treatment; Preview Close LOD0 Treatment. Candidate only; not wired into normal regional production forest selector.

Validation: twelve close libraries imported; all summer/fall crown lighting guides registered with zero missing vertices. Foliage source SHA256 is unchanged, including the final secondary branch pass. All236 frozen LOD1/LOD2 and far asset files match recorded hashes. Actual CityForge gameplay camera35.264 degrees, Seed730421, unchanged lineup/mixed32 placements and environment. Zoom4/5 capture mean pixel differences below.001 out of255 versus prior family captures. CapturedZoom1,3,4,5 and all seasons. macOS player build succeeded with0 errors/0 warnings after close shader import.

Observed: leaf sprays read more distinctly after replacing Bayer stippling; canopy valleys remain dark. Trunks read brown gray after increasing the fill. Bark relief is subtle at actual gameplay scale, and projected limb UVs remain a limitation at junctions. No claim that this is final art approval; review the actual Unity comparison. LOD0 is intentionally heavier; close treatment performance has not been benchmarked. Recipes include exact geometry changes; follow build_dense_forest_close_lod0.py, add_close_branch_hierarchy.py, then Unity Build Close LOD0 Treatment to regenerate. Preview remains separate from the normal regional tree selector.

### One-tree foliage hierarchy study — 2026-10-06

User directed one representative tree only, at Zoom1–3, with no propagation or distant changes. Separate assets: DenseForestTrees/HierarchyStudy. Preview menu: CityForge > Dense Forest Trees > Preview Single Tree Hierarchy Study. Subject A_Rounded_Summer only at terrain-grounded(632,160), yaw0, scale1, Seed730421, actual district3 camera35.264 degrees and existing lighting/environment. Six full1920x1080 Before/After captures in chat outputs/HierarchyStudy.

Geometry: no new vertices, cards, branches, topology, crown dimensions, source normals or alpha textures. Study prefabs share the existing close LOD0 and original LOD1/2 meshes, including crown UV1 metadata. Library retains exact original far atlas/center/span;236 frozen far/LOD assets match their hashes. Other family members and season assets untouched.

Foliage: separate SoftClusterLeaf.shader. Removes per-card face normal lighting and specular response from this treatment, retaining guide normals from crown lobes. Adds a smooth coherent3D medium field at1.15m and1.95m (70/30 blend), attached to metric mesh coordinates. Tangential field gradient offsets lobe normals to suggest overlapping medium clusters without new geometry or changing silhouette. Leaf RGB detail is mixed toward restrained(.40,.51,.29) at22%,12%,5% texture weight across Zoom1,2,3; existing linear palette multiplies afterward. Cluster normal strengths .65,.55,.35; contrasts .26,.22,.16. Medium shading ranges from1−.7*contrast to1+.3*contrast. Sky fill .52 still exposure-weighted .10–1, preserving darker crown interiors. Main-light shadow uses five samples: center weight .28 and fourX/Z offsets±.34m weight .18 each, softening leaf-scale self-shadow fragments while retaining lobe valleys. ShadowCaster and alpha shape unchanged from close .32 cutoff. Current study uses main directional light plus ambientSH; additional point-light response is not implemented in the study shader.

Bark: same textured close wood geometry/maps; emission(.21,.19,.165), white texture tint, bump.65 and authored roughness. Identified Unity material validation removing_EMISSION despite nonzero emission color. New library opt-in honorAuthoredBarkMaterial allows runtime to re-enable authored emission without older global bark recoloring. Only the study library opts in; established libraries retain prior behavior. Bark fill now renders instead of leaving the trunk black. No Easy Tree parameter edits.

This is a review study, not family-wide approval. Medium clusters are shading organization on the existing geometry, not new physical branch-cluster placement. No polygon increase. Added shadow filtering has shader cost that has not been benchmarked at forest scale.

Study validation: exact mesh references match before/after at all three tiers; no geometry increase. Actual Unity captures show reduced leaf-fragment contrast and stippling atZoom1–3, preserved lobe separation and dark valleys, and a readable muted brown/gray trunk. This is candidate shading organization for review, not final art approval. Full macOS Metal player build includes the isolated Resources/DenseForestHierarchyStudy asset and its shader: Succeeded,0 errors,0 warnings. No propagation to other family members.

### 2026-10-06 — Single-tree physical branch-spray review candidate

The reference requested readable overlapping leaf sprays at gameplay Zoom 1. This separate study replaces the rejected shader-noise approach with actual organized foliage geometry on A_Rounded only. It is not approved or integrated into the default region generator. Preview menu: **CityForge > Dense Forest Trees > Preview Single Tree Branch Spray Study**. Seed 730421, original gameplay camera/terrain/lighting, one tree at (632, terrain height, 160), rotation zero, scale one. Zoom 1/2 use this candidate; Zoom 3 references the existing CloseLOD0 A tier 2 directly, and Zoom 4/5 retain the existing impostor references. No forest placements were edited. All 236 entries in CloseLOD0_FrozenFarHashes.json were verified unchanged.

No Easy Tree sliders were changed for this study. Authoring is reproducible through `Tools/build_branch_spray_study.py`, opening `SourceAssets/Trees/DenseForestTrees/CloseLOD0/A_Rounded_Summer.blend`. Output blend: `SourceAssets/Trees/DenseForestTrees/BranchSprayStudy/A_Rounded_BranchSprays.blend`. Export/materials/recipe: `Assets/CityForge/Art/Trees/DenseForestTrees/BranchSprayStudy/`.

Exact geometry controls: retain the original 47 crown envelopes and global bounds (-7.34825,-7.55815,4.29477) to (7.22512,7.82700,15.98650), in Blender axes. Select 2,226 anchors from original foliage centers with ellipsoid squared radius 0.48–0.95; anchor target is max(12, round(radiusX*radiusY*radiusZ*9)). Each anchor has a 0.64m shoot axis, five alternating multi-leaf sprig cards, 0.72–0.94m long and width 0.68–0.88 times length. Cards share coherent fan orientation, small curl, and local offsets. Clip vertices to the local lobe envelope at 1.02 radius and original global bounds. Keep 45% of old cards at squared radius below 0.45 for dark interior depth. Outer/middle/interior material thresholds are 0.84/0.65 radius. Final foliage: 14,811 cards / 29,622 triangles. Wood: 44,376; new terminal shoots: 16,770; existing secondary branch fans: 6,656; total 97,424 triangles. This is a visual candidate, not an optimized LOD or a forest performance result.

Leaf texture: `SummerSpray_Albedo.png`, a transparent photographic-style hardwood sprig atlas generated with the built-in imagegen tool. Exact prompt saved in `exact_generation_prompt.txt`. Outer tint linear RGB (0.92,1.00,0.88), middle (0.72,0.80,0.68); interior retains the prior atlas/tint. Import: sRGB, alpha transparency, mipmaps with coverage preservation at 0.32, anisotropy 8, max 2048, uncompressed. New separate shader `CityForge/Trees/Branch Spray Leaves`: alpha cutoff 0.32, lobe normal blend 0.50, diffuse sky fill 0.65, smoothness 0.025, specular 0.015, 12% atlas color compression toward (0.40,0.51,0.29). No procedural foliage noise or derivative relief remains.

Bark retains the prior irregular trunk/root flare/branch geometry and albedo, normal (strength 0.65), and roughness maps. Candidate bark texture scale is (0.33,0.70); emission uses the bark albedo map with linear multiplier (0.45,0.405,0.35) to preserve surface variation in shaded trunk areas. Only this library enables honorAuthoredBarkMaterial. The trunk still needs stronger natural bark/junction quality to match the supplied reference; do not call it a finished production asset.

Actual URP captures: chat workspace `outputs/BranchSprayStudy/Before_Zoom1.png` through `After_Zoom3.png`, plus `After_Zoom1_4K.png` and native-pixel crop `Zoom1_Detail.png`. Crown silhouette and valleys survive; larger sprigs improve readability compared with the initial tiny-card pass, but the overall surface remains more regular than the reference. Review before any propagation to B/C/D or seasonal variants.

Verification: full StandaloneOSX player build succeeded with zero errors and zero warnings after the final candidate shader/material changes. Candidate metadata is saved in outputs/BranchSprayStudy/Validation.json.

### 2026-10-07 — Restore dense crown; isolated LOD0 surface treatment

The physical BranchSprayStudy was rejected: retaining bounding envelopes did not retain the approved density, overlap, or continuous crown. It is superseded for evaluation by **RestoredCrownStudy**. The reference controls surface/wood appearance, not crown architecture or foliage spacing. No family propagation or production region selection change.

Preview menu: CityForge > Dense Forest Trees > Preview Restored Dense Crown. This clones CloseLOD0/A_Rounded_Summer and shares its exact three mesh references at LOD0 (wood, original dense foliage, existing secondary branch fans). No vertex, normal, UV, topology, position, scale, branch path, card count or mass placement changes. Existing dense foliage coverage is retained through a dedicated `_CoverageMap` pointing at the original atlas; all forward/depth/shadow passes use its original alpha and 0.32 cutoff. Generated texture alpha cannot open gaps. LOD1/2, impostor material, span and center reference the original library directly. All 236 recorded medium/far files verified unchanged.

Only LOD0 materials are replaced. New DenseLayeredCrown shader preserves 85% leaf RGB detail with 15% compression toward (0.40,0.51,0.29); existing summer tints are retained. It blends card and crown normals at 0.60, adds a restrained coherent medium-scale shading field (1.15m/1.95m, 70/30 weights), normal perturbation 0.20, contrast 0.12, and exposure-weighted sky fill 0.55. Five directional shadow samples use a 0.12m radius, center 0.28 plus four offsets of 0.18. This softens tiny self-shadow fragments without replacing lobe valleys. The field is a surface shading treatment, not new cluster geometry. This study's lighting uses the main directional light and ambient SH.

`LayeredLeaf_Albedo.png` is a built-in imagegen edit of the original ten-leaf Forge_Leaf atlas. Exact prompt: RestoredCrownStudy/generation_prompt.txt. Layout/sizes/orientations were requested unchanged. Original alpha remains authoritative independently of generated layout. Generated surface RGB receives a 1.45 brightness compensation, clamped at 0.90, before blending; this compensates its darker photographic albedo without changing coverage. Where the new texture is transparent, RGB falls back to the original atlas. No alpha or source atlas was edited. Materials, shader, copied bark textures and local resource library are under Assets/CityForge/Art/Trees/DenseForestTrees/RestoredCrownStudy and Resources/DenseForestRestoredCrownStudy.asset.

Bark uses the existing SummerForest/Maple_Bark.jpg and matching normal map, copied into the study. Original mature hardwood geometry/root flare/scaffolds/secondary branches are restored, without new architecture. Bark normal strength 0.60; texture scale (0.55,0.32); white tint; original high roughness map retained. Texture-modulated readability emission linear (0.90,0.82,0.72) prevents uniform black shaded wood. honorAuthoredBarkMaterial enabled only for this library. No Easy Tree parameters changed.

Evaluation is one representative A at (632, terrain height, 160), yaw zero, unit scale, Seed730421. Actual CityForge district3 Zoom1 camera, original lighting, terrain, grass and water. Captures Before/After Zoom1–3, 4K Zoom1 and native-pixel crop are in chat outputs/RestoredCrownStudy. Other family members, seasons and forest placements are untouched. This is an unapproved close surface treatment; material/shadow filtering cost has not been benchmarked at forest scale.

Restored-crown verification: full StandaloneOSX/Metal player build succeeded, zero errors/zero warnings. Exact LOD0 mesh references and frozen-file hashes recorded in outputs/RestoredCrownStudy/Validation.json.


### Fresh American elm — Easy Tree reset, Zoom1 only (2026-10-07)

User requested a complete fresh start. New source, generator package and recipe: `SourceAssets/Trees/AmericanElmFresh`; new Unity asset: `Assets/CityForge/Art/Trees/AmericanElmFresh/AmericanElm.prefab`. No previous tree assets or procedural settings are reused. Easy Tree 1.0.1 creates the native branch and leaf placements; stock URP SpeedTree8 foliage and Lit bark use freshly acquired bundled photographic textures. Height14m, width11.9–12.4m, 72,505 triangles. Exact sockets, geometry adjustments, texture provenance and Unity values are documented in the source README and recipe.json.

One temporary Seed730421 district3 Zoom1 test uses the actual gameplay camera/lighting, with renderer-local delivery of the authored sky-fill SH. Captures are in chat outputs/AmericanElmFresh. No LODs, other zoom changes, forest tests, or production forest replacements. Candidate is for review; foliage remains flatter than reference, and forest-scale performance has not been measured.

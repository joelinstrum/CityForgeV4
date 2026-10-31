# Tropical coastal palms

October 30, 2026. The user approved Tropical aqua water, requested palms, then supplied an eight-model coconut pack and approved increasing the mix to 30%, rising to 50% near the coast. This supersedes the initial 20% V3 date-palm trial.

## Current placement

`MeadowDistrictLandscape` creates `TropicalPalmForest` for every detailed Tropical district, including inland and waterless districts, excluding frozen checkpoint replay. Temperate districts retain their no-palm presentation. This October31 correction supersedes the original coastal-only gate: the user expects the base Tropical palm mix throughout generated regions, even when there is no ocean. Only the increase to50% requires a semantic ocean shoreline.

The near-coast band is 75 metres from actual semantic Ocean outline segments, measured in horizontal world metres. Polygon closing edges are included; map-boundary edges are excluded. Rivers and district edges do not create a coastal band. Each native/authored mature population is split into near and inland pools after diagnostic density filtering: `floor(nearCount * .5)` and `floor(inlandCount * .3)` are replaced. Seedlings/shrubs/understory are excluded. This means 30% away from shore plus 50% near it, not a globally constrained 30% budget. Small pools can differ slightly from their percentages because of integer rounding.

Native palms replace terrain-tree presentation instances; authored palms replace selected summer canopy draws. Existing positions and rotations are reused, with deterministic selection among eight coconut variants and modest scale variation. All canonical native arrays, prototype indices and terrain caches remain unchanged. Save capture retains canonical trees; climate recreates the palm presentation on loading. No palm prototype IDs enter saved arrays. Selection remains stable through understory/species refreshes. Native palms stay evergreen; supplemental legacy authored palms follow the existing summer-only supplemental forest lifecycle. Generated authored replacement palms stay evergreen at fixed positions, while non-palm replacements return to native seasonal trees outside summer. See the October31 correction in Approved_District_Integration.md. Trees-off hides palms and navigation LOD bias affects them alongside existing trees.

## Purchased coconut models and local preparation

User-supplied archives: `/Users/joelinstrum/Downloads/trees/palms/FBX.rar` and `Texture.rar`. Extracted private source: `SourceAssets/TropicalPalm/CoconutPack/Source/`. The FBX contains Coconut_01 through Coconut_08, approximately 13.5–20.2 m tall, including leaning and multi-trunk silhouettes. It contains static meshes, no actions, armatures or animation tracks. The listing's wind-animation description does not establish that animation is present in this FBX; wind is not implemented by this integration.

Preparation, with `CITYFORGE_PROJECT` pointing to V4:

1. Extract the FBX and texture archives into `SourceAssets/TropicalPalm/CoconutPack/Source/`, retaining its Texture subfolder.
2. Run `Tools/Trees/TropicalPalm/atlas_coconuts.py` with Python/Pillow.
3. Run `Tools/Trees/TropicalPalm/prepare_coconuts.py` with Blender background mode. It refuses an existing near-LOD output.
4. In Unity Edit mode run `CityForge/Forest/Prepare tropical palm assets`. It refuses an existing output directory.

The private runtime resources are `Assets/CityForge/Resources/TropicalCoconut/`. Eight variants each have four LODs: near 24,956–30,000 triangles, then approximately 12,000 / 4,000 / 1,200. Ground bases are protected during simplification. FBX object transforms are baked into owned Y-up meshes; normals use inverse-transpose transforms and tangents are recalculated. No downloaded source is modified.

The eight original material slots are consolidated into one shared matte, double-sided URP Lit material. Diffuse and normal atlases are 2048-square vertical columns with gutters. UV U maps into each column while V remains repeatable, preserving source trunk tiling. The colored `_tran` textures are translucency maps, not opacity masks; they are not incorrectly used for alpha clipping. No global lighting or approved native-tree colors are changed. Shadows use LOD2 or coarser. Rendering uses instancing, camera-frustum culling and the existing global LOD bias; no per-palm GameObjects or per-frame terrain-array rewrites are added.

All purchased art, atlases, meshes and generated materials/resources remain Git-ignored. Only authored C#/Python preparation and integration source is public-source eligible. The old V3 date-palm runtime resources are preserved outside Assets in `SourceAssets/TropicalPalm/DatePalmResources-2026-10-30/`; V3 source is untouched. Missing coconut resources produce an explicit Tropical-coast load error, rather than falling back to the former palm.

## Validation

Compilation passed with zero errors/warnings. In the existing 50%-density coastal test, 3,563 presented native mature trees split into 23 near-shore candidates (11 palms) and 3,540 inland candidates (1,062 palms). The 300 presented authored placements split into 76 near-shore candidates (38 palms) and 224 inland candidates (67 palms). Total: 1,178 palms among 3,863 mature placements. A multi-trunk model remains one placement.

Live checks passed the 30%/50% pool counts, fixed total population, 75 m band samples and map-boundary exclusion, all eight variants, deterministic matrices, summer/fall/winter preservation, understory on/off, species refresh, canonical save capture, Tropical climate retention, null legacy recipe, single-submesh atlases, and grounded meter-scale bounds in all 32 LODs. The isometric coastal render was visually inspected. The former coastal-only gate had passed Tropical-inland exclusion checks at that time; those assertions are superseded by the October31 user-requested inland Tropical mix.

Evidence is in the current chat's `outputs/coconut-palms/`. Original valley and coastal-save SHA256 hashes remain unchanged. No standalone build or sustained frame-time comparison was run for this revision; instancing and LOD preparation are not performance qualification. Coconut appearance awaits user review.

## Close-zoom wind — October 30

The user approved the coconut appearance and requested animation at the two closest zoom levels. The supplied FBX still has no animation tracks; this follow-up adds authored procedural GPU wind, not a recovered vendor clip. `CityForgeCameraController.ZoomLevel` 0 and 1 request wind. Weight fades over 0.35 seconds on zoom transitions; levels 2–5 settle to exactly zero, and the component stops advancing its wind clock when the weight reaches zero. Hidden trees also skip the wind/render work.

`TropicalPalmWind.hlsl` adds slow trunk bending, per-tree phase from the world anchor, and lighter crown/frond motion restricted to leaf texture-atlas columns 4, 5 and 7. The root stays fixed because both bend and crown weights are zero at the base. Peak added displacement is bounded around 0.34 m horizontally and 0.035 m vertically in the configured profile. Palm matrices, variants, placement selection, textures, palette, terrain and water remain unchanged. Culling tests include a conservative wind margin. No per-tree GameObjects, skeleton updates or CPU vertex-buffer rebuilds are added.

`TropicalPalmBuilder.PrepareWindShader` generates an ignored local derivative of the installed URP Lit shader, wrapping its forward, GBuffer, shadow, depth-only and depth-normal vertex entry points with the same deformation. Installed URP lighting and texture code remain authoritative; generation fails explicitly if the expected pass names change. The normal map and instancing remain enabled. Per-draw property blocks supply wind weight, time and source variant height to both visible and coarse-shadow submissions. The normal adjustment approximates the small trunk bend; this is artistic wind rather than a physical frond simulation. Baked meta and motion-vector paths are not animated; current gameplay uses the existing forward rendering configuration.

Preparing new palm assets now prepares the wind shader automatically. For existing private resources, use `CityForge/Forest/Prepare tropical palm wind shader` in Edit mode. The public authored helper lives under Rendering/Region; the generated shader stays under ignored Resources/TropicalCoconut. No licensed source or purchased assets are published.

Rendered isolated-palm controls confirm wind changes geometry over time, disabled wind is pixel-identical at time0 and time7, and disabled wind matches the original URP Lit material exactly in that fixed preview. The entire tree fits the final control captures; preliminary captures that read only a high-DPI viewport quadrant were replaced and are not visual evidence. The helper uses identical deformation in shadow/depth passes. Live zoom results and preview frames are recorded in this chat's `outputs/palm-wind/`. No standalone timing or performance qualification is claimed.

### Canopy preservation — October 31

Approved broadleaf replacements now grow as crown-connected patches. Palm quotas are unchanged; `PresentAuthored` receives actual approved crown radii and favors low-connectivity fringe/opening positions over dense interiors. Native palm selection and the closest-two-zoom wind treatment are unchanged. The same edge preference applies to the approved legacy supplemental grove. Canonical vegetation and saved climate remain authoritative; there is no save migration.

### Tropical year-round presentation — October31

Tropical districts now keep native summer prototypes, approved canopy and all palms throughout calendar Fall/Winter. `VisualSeason` controls presentation while `Season` continues to persist the calendar. Fall/Winter controls are disabled. Temperate seasonal behavior and canonical vegetation remain unchanged. This supersedes summer-only canopy/fall native-fallback statements for Tropical districts.

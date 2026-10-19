# V3 evergreen intake — 2026-10-17

Source project is read-only. This intake does not copy V3 UI or rendering code.

Found complete 3D candidates:

- `Authoring/Flora/SpeedTree/EvergreenPilot/v01_export/CF_Flora_EvergreenPilot_v01.fbx`, with bark, cap and three needle-cluster texture sets. Ported as a separate review prefab under `Assets/CityForge/Art/Trees/V3Evergreen/`. Source species is unspecified; do not label it as a verified spruce/fir species.
- `Authoring/Flora/IncomingVarious/cilician-fir/v01_source/Blender-Abies cilicica/Abies cilicica.blend`: available for a subsequent deliberate port.
- SprucePilot and ChristmasTree SpeedTree `.spm` sources are present, but no corresponding exported 3D mesh was found in those source folders.

V3 runtime MediumConifersV01 (Balsam fir, Fraser fir, Blue spruce), DouglasFirV01 and CilicianFirRealisticV01 contain image-based tree representations; these are not native 3D prefab candidates. V4 already has a separate Balsam fir study and Fraser fir assets from earlier investigations; neither is automatically part of the approved meadow baseline.

The evergreen preview derivative combines source color and opacity into RGBA, uses URP Lit double-sided cutout needles and bark, and has three mesh LODs (23,843 / 12,310 / 7,095 triangles). Source V3 artwork is unchanged. The V4 review prefab is normalized to 18 m tall. Assets stay local and excluded from the public source repository. The editor import utility is source-only.

This intake prepares a separate visual review group. It does not replace approved poplars, change the seasonal prototype arrays, or alter existing saves or generated ecology. Distribution preference and visual acceptance remain pending before adding evergreen prototypes to the generated biome.

Visual status: initial automatic-distance preview showed sparse/inconsistent foliage. This candidate is not approved for generated woodland. The same sparse foliage remains in the full-detail diagnostic preview, so the import/material presentation needs further work; distance simplification is not the sole cause. Structural import checks alone are not visual acceptance.

## Authorized mountain trial

The user subsequently requested mixing this tree into Hills and Mountain Test. Runtime prefab: `Resources/Trees/MountainEvergreen/EvergreenPilot`. `MountainEvergreenMix` appends index 15 after the unchanged approved seasonal array, substitutes only mature poplar placements using a stable position/seed hash, and leaves existing saves untouched until manual Save Region. The seasonal slot always references the same evergreen prefab. Other regions retain their existing mix. The lower needle cutoff is 0.15; visual approval remains pending the user's test. Earlier preview failure notes above describe the intake and remain relevant as a limitation, not a prohibition on the authorized trial.

Color review revision: the user found the mixed evergreens too bright/pale. The three needle materials now use a deeper green multiplier `(0.38, 0.56, 0.44)`, smoothness 0.04, and disabled specular/environment highlights. Reimport reapplies this palette through `V3EvergreenImport.NeedlePalette`. Bark, alpha cutoff, grass, approved seasonal foliage, lighting and placements are unchanged. The mountain integration checks pass after the palette update; visual acceptance remains with the user.

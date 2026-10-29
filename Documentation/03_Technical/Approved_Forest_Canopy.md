# Approved forest canopy — refined C

The user approved **refined palette C** on October 28, 2026 and requested a locked checkpoint, PR and handoff. Preserve this appearance instead of reopening the earlier tree-shape or blue-green palette experiments.

This is an approved **local visual/replay baseline**, not automatic production forest placement. Normal district loading still uses the native `MeadowRuntimeBaseline`, `MeadowDistrictLandscape` and `SeasonalTreeLibrary`. Do not restore the retired downloaded maple or prototype broadleaf trees.

## Approved appearance

- Seven authored broadleaf variants; taller/shorter forms, touching crowns and restrained per-tree variation. The accepted leaf size is 10% smaller and crown spread approximately 30% larger than the earlier checkpoint. Keep the approved trunk/branch shape.
- C targets: deep `#45552B`, medium `#61713D`, lighter `#849456`. These guide luminance-normalized linear chroma, not final lit screenshot pixels. Retain the existing per-tree palette distribution, 90% remap and 1.18 linear luminance gain.
- Pale native foliage: hue shift −0.018 turns toward yellow, saturation ×0.97, value ×0.935. Apply consistently to owned healthy/dry leaf and distant cross/billboard materials; preserve bark, alpha and lighter-tree variation.
- Within broadleaf crowns, existing leaf normals and vertex occlusion guide restrained warm upper highlights and neutral-green recesses. No random tree-wide hue changes, new geometry or high-frequency color noise. Maximum highlight multiplier is linear RGB `(1.045, 1.015, 0.965)`; recesses blend at most 7% toward neutral green and dim at most 1.5%.
- Grass layers 1 and 2: 12.5% linear desaturation, restrained earthy warmth, preserve linear luminance and diffuse alpha/smoothness. Bake separate local textures with existing terrain tints included, then use unit remap scale on cloned layers. Preserve normals, tiling, blend weights, soil, litter, roads and water.
- Geometry, sun/exposure/contrast, ambient SH and native meadow post-processing remain fixed. Keep the project in Gamma; the authored color remap computes in linear RGB and returns to the existing pipeline.

`ApprovedForestColors.cs` owns the C targets and native/grass color transforms. `ApprovedForestCanopy.shader` owns the authored PBR leaf/crown response, alpha clipping, LOD crossfade, instancing, depth and shadow passes. Both are CityForge-authored source; textures and meshes are excluded from public Git. These helpers are opt-in and do not mutate native materials during ordinary gameplay.

## Local checkpoint and replay

The immutable private checkpoint is:

```text
SourceAssets/ApprovedForest/RefinedC-2026-10-28/
```

It includes all 35 mesh JSONs (seven variants × LOD0–4), deterministic layouts, source/runtime materials and textures, grass derivatives, replay templates, authoring notes and visual/performance evidence. Its manifest checksums every packaged file and the approved public shader/color recipe. The replay tool pins that manifest hash. Do not force-add this directory: it contains licensed art and derivatives.

Verify without opening Unity:

```sh
python3 Tools/Trees/approved_forest_checkpoint.py verify
```

A clean public clone lacks this checkpoint and licensed native dependencies. The tool must fail clearly if they are missing; never substitute retired assets.

Use a fresh output directory so stale screenshots cannot qualify a new run. The generated player goes to the ignored `Builds/ApprovedForestReplay/` directory; `Library` is used only for replay state and the external build script.

For an actual replay, first open the saved **MainMenu in Edit mode** in the pinned Unity Editor 6000.1.12f1 with the local Pipeline server available. The saved `meadow-test-valley.cfregion` is loaded read-only; no new region or tree-placement save is written.

```sh
python3 Tools/Trees/approved_forest_checkpoint.py capture --output /absolute/path/outside/project
```

The tool verifies the checkpoint, restores only ignored local study resources, builds an ARM64 release player, and captures four cumulative stages from near/low/close/wide cameras. Build settings/shader inclusion are restored in `finally`; the player changes cloned/runtime objects only. Temporary replay files are removed after completion. It uses Launch Services once with environment variables, avoiding the earlier direct-executable-plus-`open -a` duplicate-player issue. Unity Hub is unnecessary. This replay currently targets macOS.

For manual inspection, `restore` stages the ignored resources without building; `clean` removes only unchanged files recorded as created by that restore. Never remove or overwrite pre-existing local work. Refresh Unity afterward. Capture refuses unsaved/non-MainMenu scenes; cleanup refuses changed replay files so they can be reviewed.

## Performance evidence and limits

The approved performance *setting* is 1600×1000 output with **80% scene render scale** (1280×800 internally, upscaled), full leaf PBR/visible crown geometry, configured 4096 shadow map, coarser authored shadow casters, depth priming disabled and 60 FPS target. UI renders at output resolution. Preserve the full-resolution artwork; this is an explicit image-detail tradeoff, not a free optimization or global quality change.

On the local M1 Max at Ultra/Gamma, the audited single-instance run with 600 authored trees plus 2,070 presented native mature trees measured 5,398 foreground frames over 90 seconds: **59.98 FPS**, mean 16.673 ms, p95 16.670 ms, p99 16.712 ms. The full-scale baseline was approximately 49 FPS. Earlier background/duplicate-menu-player controls are diagnostic only.

**That timed run preceded the final palette/refinement shader.** Final C has visual approval and replay qualification, not a new sustained performance certification. Before production integration, bake/streamline the color treatment, reduce the prototype's 6,000 extra shadow-proxy GameObjects, and repeat the foreground performance check. The existing model/LOD shapes should remain fixed.

Shadow-map resolution reductions and simplified lighting did little; depth priming was slower. No impostor atlas was tested. A populated city scene was unavailable, so buildings/traffic/simulation are not budget-qualified. Do not claim a full-city 60 FPS guarantee.

## Preservation and next work

The saved valley, original approved materials/textures, native foliage positions, water/camera, project quality/color space and build settings remain unchanged. Final editor state is clean MainMenu, Edit mode, Gamma/Ultra. The local study's 600 trees use the same layout across all palette/refinement comparisons; native canonical count is 5,361, presented mature count 2,070.

Next work is controlled integration and performance qualification of this locked baseline, not more artistic redesign unless requested. Private checkpoint evidence includes near/low/close/wide refined-C screenshots, cumulative comparison sheets, preservation checks and the audited pre-color performance frames.

## Validation of the locked source

The cleaned public shader/color recipe was replayed from the checkpoint in an ARM64 release player (zero build errors); all 16 captures completed. Refined-C camera/sun/volume/native-tint/quality metadata matched the accepted study at all four poses, and the captured appearance was visually reviewed. All 35 meshes, saved regions, native material/layer/texture files, original study art and project settings remained byte-identical. Temporary runtime/resources were removed afterward.

The local color/shader check passed 10 invariants (approved pale tint, palette normalization, grass luminance/alpha preservation and leaf material configuration), with no shader messages or compilation errors. The source-only filesystem guards can be run without Unity or private art:

```sh
python3 -m unittest discover -s Tools/Trees -p 'test_approved_forest_checkpoint.py' -v
```

These six checks cover corruption, collisions, preserving edited/pre-existing files/directories, stale capture rejection and ignored-path restrictions. They do not replace visual/performance qualification.

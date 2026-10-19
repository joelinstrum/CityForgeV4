# Broadleaf tree geometry optimization — 2026-10-17

> Subsequent update: these models are now integrated into native forests. See `ForestBroadleafIntegration.md` for current seasonal, material, placement and save behavior. The staged-only statements below describe the earlier geometry study.

Eight tree variants have 24 optimized FBX meshes staged locally in `SourceAssets/OptimizedBroadleaf/<variant>/<variant>_LOD0.fbx` through `_LOD2.fbx`. The V3 sources remain unchanged. These are full 3D mesh LODs; no whole-tree billboards were generated. Nothing has been added to gameplay or substituted into the approved meadow/poplar or mountain fir mixes.

## Verified triangle counts

| Variant | Source | Near | Medium | Distant | Near reduction |
|---|---:|---:|---:|---:|---:|
| SilverMapleA | 993,523 | 59,435 | 20,451 | 8,357 | 94.0% |
| SilverMapleB | 900,070 | 61,047 | 20,696 | 8,626 | 93.2% |
| CanyonLiveOakA | 1,057,971 | 58,805 | 22,704 | 9,927 | 94.4% |
| CanyonLiveOakB | 910,119 | 59,108 | 22,705 | 10,112 | 93.5% |
| SouthernLiveOak | 1,948,054 | 58,915 | 23,212 | 10,274 | 97.0% |
| AngelOak | 2,087,570 | 59,978 | 19,962 | 7,964 | 97.1% |
| OakPilotSummer | 106,326 | 59,703 | 20,007 | 8,146 | 43.8% |
| OakPilotVariantBSummer | 110,091 | 59,992 | 20,010 | 8,148 | 45.5% |

Source counts include the foliage meshes present in each authoring tree, including seasonal color alternatives. The two OakPilot entries are Summer exports; this pass does not build the other OakPilot seasons. Source totals are per separated variant, not the combined two-tree authoring file. The second maple and canyon oak base is explicitly assigned by its Bark2 name; it lacks the suffix used by the rest of the second variant.

## Method and quality limits

Coplanar simplification and collapse reduce dense branches and leaves. Disconnected small parts are thinned deterministically when their topology prevents further collapse. Foliage receives 85–92% of the nominal triangle budget; retained leaf elements are enlarged around their own centers, with bounded factors, to compensate for lost coverage. Moss uses smaller limits. Low-detail trunk geometry has a minimum budget to preserve the base. Canyon and Southern live oak branches use an additional component-area selection pass before triangulated collapse; this removes fine twigs first and avoids broad spike-shaped branch artifacts. Angel Oak's mixed source mesh is separated by bark/leaf material before reduction. Each second variant retains its own base; the exported vertices are grounded and uniformly scaled to the corresponding original full-tree height at every LOD. Exports are combined to one mesh object per LOD, with original material slots and UVs retained. This reduces object/transform overhead; multiple materials still require multiple draw passes.

These are lossy geometry candidates. Opaque Blender comparison renders were inspected for gross canopy and trunk changes; distant leaves are coarser and fine twigs/root details are reduced. This does **not** confirm textured appearance, opacity, seasonal behavior, actual Unity LOD transitions, or GPU performance. The FBX files preserve material slots, but Unity-compatible material/texture remapping and in-game visual review are still required before enabling these trees. In particular, the Angel Oak source has unlinked V-Ray material nodes. Do not replace the approved biome with these candidates automatically.

## Verification and rebuild

All 24 final FBXs were imported back into Blender. Checks passed for decreasing triangle counts, geometry budgets, finite vertices/UV coordinates, UV layer and material slot presence, scale, and ground contact. Each final LOD contains one mesh object. Counts measure exported triangles, not scene-wide runtime rendering cost. No FPS claim is made.

Rebuild with Blender 5.1.2 using `Tools/Trees/BroadleafOptimization/run.py`. The Python runner accepts `--blender`, `--project`, `--source` (V3 root), `--work`, and `--preview`. Defaults use the sibling V3 folder and a local Build workspace beneath `SourceAssets/OptimizedBroadleaf`. It runs inventory, initial optimization, Angel-specific separation, coverage correction, trunk correction, live-oak branch correction, base restoration/height alignment, mesh combination with orphan-vertex/unused-slot cleanup, and round-trip validation. Preview generation is optional. Run the entire pipeline rather than individual late stages against already-combined exports.

Only CityForge-authored Python and text documentation belong in the public repository. Models, manifests, previews, source artwork, and generated build data remain local and ignored. The current game assets, saves, meadow shaders, seasonal library, and ocean settings are unchanged by this optimization.

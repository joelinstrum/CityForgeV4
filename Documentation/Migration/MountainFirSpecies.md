# Three 3D fir species — 2026-10-17

The user authorized Balsam, Fraser and Cilician firs alongside Evergreen Pilot in Hills and Mountain Test. This trial uses real tree meshes with 3D trunks/branches and textured needle geometry, not whole-tree image billboards as source assets. Native Terrain may still select its normal distant representations.

## Sources and derivatives

- Balsam fir: existing V4 `Art/Trees/BalsamFirStudy/BalsamFirA.fbx` and `Balsam.mat`/BalsamAtlas, originally extracted from the fir-3 archive. New normalized 18 m derivative with three mesh detail levels.
- Fraser fir: existing V4 `Art/Trees/FraserFirA.fbx` and the existing fir bark/needle/cap materials. New normalized 20 m derivative with three detail levels; copies of the materials avoid changing any old study.
- Cilician fir: V3 `Authoring/Flora/IncomingVarious/cilician-fir/v01_source/Blender-Abies cilicica/Abies cilicica.blend` and its textures. New normalized 21 m derivative with lower mesh detail and URP cutout materials. The original 1.3-million-triangle source is not used directly in gameplay.

New art derivatives live under `Assets/CityForge/Art/Trees/MountainFirs`. Runtime prefabs live in `Resources/Trees/MountainEvergreen`. Original V3/V4 source models, original study materials, approved meadow/poplar assets, lighting, terrain and ocean settings remain unchanged. Imported art stays local and is excluded from the public source repository.

## Stable prototype and save contract

Indices 0–14 remain the approved seasonal library. Index 15 remains Evergreen Pilot, retaining the user's existing pilot trees. New indices 16/17/18 are Balsam/Fraser/Cilician fir. `MountainEvergreenMix` keeps existing evergreen instances intact. New fir substitutions use a separate deterministic probability band among existing mature poplars, without moving, adding or removing placements. Ground texture/road/terrain caches remain authoritative. The seasonal arrays append these same four evergreen prefabs in Summer/Fall/Winter; the small-plants toggle does not hide them.

Save validation permits the additional indices only for the named Hills and Mountain Test region. The binary format is unchanged. No player save is automatically rewritten; normal Save Region remains manual.

All new needle materials are matte and tinted to avoid the washed-out initial evergreen palette. Visual approval remains pending the user's test.

## Verified intake

| Species | Height | Near / middle / far mesh triangles | Northeast placements |
|---|---|---|---|
| Balsam fir | 18 m | 47,684 / 17,641 / 6,848 | 242 |
| Fraser fir | 20 m | 27,217 / 12,247 / 4,082 | 216 |
| Cilician fir | 21 m | 46,471 / 17,867 / 8,902 | 249 |

Cilician optimization welds coincident vertices, triangulates, removes small disconnected bark/twig fragments at increasing distances (retaining the larger trunk/branch structures), and simplifies meshes. Needle geometry and opacity textures remain; the full source is preserved outside the derivatives.

The existing 715 pilot trees remain, giving 1,422 evergreens among 15,243 total tree/understory placements. Editor checks passed for material mappings, normalized bounds, decreasing detail levels, presence of each species, unchanged saved positions/rotations/count, repeated-mix stability, all three seasons, the small-plants toggle, and a temporary-file save/load with native terrain cache restoration. Species-focused renders were inspected; user visual acceptance remains pending. The existing on-disk player save was not overwritten. No standalone performance benchmark was run for this addition.

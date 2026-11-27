---
name: cityforge-brick-buildings
description: Build, revise, or review brick architecture for City Forge in Blender. Use for any new brick building, brick façade, Federal/Georgian/Colonial masonry asset, brick material change, or architectural render where the approved City Forge brick appearance must be preserved.
---

# City Forge Brick Buildings

Use the approved Federal-Georgian Five-Bay v79 asset as the canonical brick visual standard. Never substitute a generic procedural brick, flat color, newly improvised shader, or plastic-looking material.

## Canonical source

Start from or append materials and façade construction from:

`Authoring/Architecture/Buildings/RenderMasters/CF_Building_FederalGeorgian_FiveBay_01/CF_Building_FederalGeorgian_FiveBay_01_render_package_v79.blend`

Use its approved review renders as the comparison target:

`Authoring/Architecture/FinalRenders/CF_Building_FederalGeorgian_FiveBay_01/v79_hero/directions/`

Read [references/brick-standard.md](references/brick-standard.md) before changing brick materials, UVs, masonry geometry, or texture paths.

Read [references/historic-refinement.md](references/historic-refinement.md) before finalizing any City Forge architectural asset or performing a realism, aging, weathering, or historical-detail pass.

Read [references/masonry-continuity.md](references/masonry-continuity.md) before removing an opening, infilling brickwork, changing quoins, or altering a masonry corner.

Read [references/runtime-render-contract.md](references/runtime-render-contract.md) before producing game-ready rotations, pivots, top-down artwork, seasonal renders, or a runtime `building.json`.

## Single-window placement

**RECESS SINGLE WINDOWS UNLESS TOLD OTHERWISE.** Place the sash and glazing behind the brick wall face with real openings and continuous masonry reveals; avoid a pasted-on assembly. Use about 4 inches (0.1016 m) for the current townhouse revision. Keep historic frame sections restrained and preserve stone lintels/sills as distinct masonry. See [historic refinement](references/historic-refinement.md) for review criteria.

## Required workflow

1. Duplicate the canonical source into a new asset directory. Never modify v79.
2. Preserve the original façade meshes, UVs, shader nodes, texture images, mortar response, brick depth/detail objects, and color variation.
3. Relink moved texture files to their existing project copies. Never respond to a missing path by replacing the material.
4. Make architectural changes in a new, clearly named collection.
5. Derive accessory placement from actual target-object bounds or anchors. Never place shutters, doors, signs, or trim from guessed coordinates.
6. Keep each shutter pair parented to its corresponding window and verify it is centered vertically and immediately adjacent to that opening.
7. Render close perspective checks from all four corners in addition to orthographic sprite views.
8. Inspect every review render visually at full resolution. Reject floating geometry, accessories without openings, façade collisions, stretched UVs, repeated toy-like patterns, missing textures, and mismatched material scale.
9. Compare the brick directly against a v79 hero render before delivery.
10. Describe incomplete work as a blockout or review checkpoint. Use “production-ready” only after the visual checks pass.
11. Preserve the approved design during refinement passes. Add authenticity through depth, shadow, restrained material variation, and small non-repeating imperfections rather than redesign or visible distress.
12. Treat removed openings and masonry corners as topology-and-mapping tasks. Extend or duplicate canonical geometry with its UVs and material indices intact; never conceal an opening with newly mapped cubes or a flat texture panel.
13. Model quoins as complete bonded corner units with continuous returns. Never hide a corner board until a close render proves the replacement closes the wall core without gaps, overlaps, or exposed backing.
14. Export the centered spatial contract, four rotation-safe oblique facings, a canonical top-down plan render, and winter roof-snow artwork according to the runtime render contract.

## Non-negotiable rejection criteria

Reject the asset if any of the following is true:

- Brick reads as flat color, smooth plastic, painted panels, or uniform blocks.
- Texture scale, mortar width, relief, roughness, or color variation materially differs from v79 without explicit art direction.
- A shutter is not paired with a real window.
- Geometry floats, intersects unrelated trim, or appears detached in a perspective view.
- Only one camera angle has been reviewed.
- Missing texture dependencies remain on visible render objects.
- Repeated wear, perfectly identical component edges, dramatic grime, or exaggerated damage makes the asset read as procedural, abandoned, or stylized.
- An infill reveals its former opening through different brick scale, bond, depth, color, UV phase, mortar alignment, or a rectangular boundary.
- Front and side quoin pieces fail to meet as one bonded corner unit, or expose a board, backing strip, wall core, black void, or daylight between returns.
- A game-ready package omits its centered rotation anchor, per-facing projected pivots, top-down plan render, or winter roof-snow exports.

When a requested design conflicts with the canonical material standard, preserve the standard and vary only historically appropriate color, aging, bond, or trim after showing a comparison render.

## Shared 3D building LOD standard

When preparing or reviewing 3D mesh tiers, read [the shared CityForge LOD standard](../cityforge-buildings/references/lod-standard.md). The preferred authoring goal is Near (~30k), Medium (~15k), and Far (~1k–3k) triangles, with budgets adapted to the asset. Preserve this skill's canonical brick appearance across tiers; bake small masonry detail for the far tier. This does not replace the brick/sprite runtime render contract or imply that native three-tier routing is already implemented.

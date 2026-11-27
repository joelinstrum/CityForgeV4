---
name: cityforge-buildings
description: Create, reconstruct, refine, tile-fit, or prepare LODs and private asset packages for CityForge 3D buildings in Blender, including buildings referenced by Tripo models and concept images. Use for building asset preparation; native game integration is a separate task.
---

# CityForge Buildings

Produce believable, detailed architecture with softened edges, restrained organic variation, convincing interiors and scalable LODs. Preserve the user's approved design through revisions. Work one building at a time unless the user requests batching.

## Shared repository and concurrent chats

- Before creating or revising a building, search the central local repository at `/Users/joelinstrum/Documents/CityForgeAssets` and read its `AGENTS.md` and `Repository/Workflow.md`. Reserve the stable asset ID with `Repository/library.py` and use the returned private workspace; do not overwrite another chat's work or auto-expire its claim.
- Reuse compatible cataloged components/materials read-only. Publish new reusable parts as separate versioned packages in `SharedArchitecture/` when useful within the requested task; reserve a dependency separately before changing it. Category folders alone are not evidence that components are available.
- Preserve approved asset revisions, validate the finished package, then register its actual saved location and truthful readiness. Release the reservation on completion or deliberate abandonment. The catalog does not install or validate assets automatically.
- This enables cooperative chats on the same Mac; it does not coordinate cloud copies or authorize shared live Blender/Unity mutations. Read [the repository reference](references/shared-repository.md) when using the catalog or publishing components.

## References and scope

- Use concept images for visual character and a supplied Tripo model for proportions, all elevations, roof layout and source detail. Inspect both. Do not assume a request for a fresh reconstruction authorizes simply duplicating the supplied mesh, or vice versa; use the established task intent.
- Inspect ZIP contents and texture connections before extraction/import. Preserve originals and resolve missing maps rather than substituting a generic material.
- Use private background Blender processes when another session may own live Blender or Unity. Read the current project's AGENTS.md and relevant importer/catalog conventions before project integration. The previously used project is `/Users/joelinstrum/dev/CityForge - V4`; verify that it is still the intended project.
- Keep native import, actual clock/interaction behavior, runtime performance and save/reload validation separate from Blender preparation. Do not install into the game when the request is to prepare/store assets.
- For brick buildings, also apply [cityforge-brick-buildings](../cityforge-brick-buildings/SKILL.md) and its approved material references. This workflow does not replace that brick standard with a generic shader.

## Reusable kits and building recipes

For modular kit planning, library gap audits, or buildings assembled from existing parts, read [references/modular-kits-and-recipes.md](references/modular-kits-and-recipes.md). Prefer compatible substitutes over generating near-duplicate assets; separate source collection, component preparation and building assembly in readiness reports. Keep architectural family distinct from the game's era grouping.

## Architecture and tile fit

- Retain fine surface character and subtle imperfections without exaggerated distress or repeated procedural wear. Avoid turning a granular reference into perfectly identical flat components.
- Give exposed building corners and roof borders restrained physical roundness. Selectively bevel important edges rather than adding subdivisions everywhere. Roof borders must wrap continuously through corners; inspect close-ups for open ends, black voids, overlapping returns and broken shading.
- Current lot pitch is **4 m**, verified in `LotDefinition.TileMeters`; check current code before integration. Rectangular lots are allowed in authoring; square examples do not limit every asset to square footprints.
- Choose each horizontal footprint dimension as the nearest positive multiple of the tile pitch, with halfway ties rounding upward. Example: 21 m → 20 m; 22 m → 24 m. If contraction harms the architecture, choose the next larger sensible lot and document why. Do not snap height.
- **Preferred adjustment: widen/lengthen corner masonry first.** Preserve window and door dimensions, floor heights, bay count, ornament and rounded details. Extend straight roof/slab spans and move whole facade assemblies, interiors, lights and hinges with their wall planes. Do not globally stretch the building to force a fit. Larger changes may need bay/layout changes rather than excessive corner growth.
- Measure the complete envelope, including roof projections, steps, fixtures and planters; check opening states too. Centre the reserved footprint at ground level, keeping the design's facade/entrance alignment coherent. Carry the same origin, orientation, units and footprint into every LOD.

## Windows, doors, lighting and materials

- **RECESS SINGLE WINDOWS UNLESS TOLD OTHERWISE.** Set single-window sash and glazing behind the exterior wall face, with genuine wall openings and finished wall reveals. About 4 inches (0.1016 m) is the current townhouse target; adapt depth to the architecture unless specified. Keep historic sash/frame sections slender and distinguish them from stone lintels, sills and casing. A surface-mounted window assembly with no reveal is not acceptable. Preserve approved buildings unless a retrofit is requested. Verify depth in oblique close-ups and retain the recessed reading across LODs.

- For detailed reconstructions, use genuine recesses, clear glazing, selective shallow interiors and restrained furnishings where visible. Light the rooms behind the glass; do not replace their appearance with flat yellow window rectangles. Keep stone, sash, mullions, opaque door panels and blinds distinct from illuminated glass.
- Keep warm ground-floor lighting and a stable scattered upper-floor lit pattern where appropriate to the approved design. Preserve the same pattern through LOD transitions and daylight/night states. Fixture glow and a physical local light pool are distinct effects.
- Author movable door leaves and selected opening sashes as separate parts with sensible hinges. **Future main entrance doors open outward toward the street**, per user direction. Plan recessed entrances, setbacks or the reserved lot envelope to accommodate the outward swing without colliding with walls, fixtures, steps or the pedestrian route; do not reverse doors inward just to pass a footprint check. Move pivots with tile-fit changes and verify open/closed poses. Preserve already-approved buildings as-is unless a retrofit is requested. Blender animation is not a runtime click controller.
- UV emission is appropriate for supplied static meshes and reduced tiers when it preserves actual pane/interior detail. Masks stay inset inside the panes, with straight boundaries and only a small anti-aliasing transition; no opaque glowing overlay cards or broad spill onto frames/stone.
- Inventory existing approved materials before inventing replacements. Procedural Blender materials can support a visual prototype, but their appearance does not automatically transfer through FBX. A production package requires portable textures/material setup; label unbaked prototypes accurately.
- Check outward face orientation with backface culling enabled before export. Blender's two-sided beauty render can conceal missing awnings, relief fans and frame surfaces in Unity. Correct reversed faces; use two-sided materials deliberately for thin cloth or other genuinely thin surfaces.
- Preserve readable lettering, awning stripes and other distinctive source maps at useful texel density. Keep their source UVs and separate material maps when a whole-building atlas would blur them. Carry authored roughness/smoothness into the runtime material setup, not a generic value for every finish. Verify albedo as sRGB and normal/metallic/smoothness data as linear by material role.
- Keep physical material scale consistent across buildings. Preserve original maps/UV detail when adapting source meshes. Bake small geometry into lower-tier materials without baking night glow permanently into daytime albedo.

## Modular signs and building variations

- Reserve named sign attachment points during reconstruction: entrance fascia, storefront band, projecting corner bracket or roof position as appropriate. Derive positions from actual facade bounds. Record building-local position/orientation, available dimensions, clearance and compatible sign types; move these points with tile-fit adjustments.
- Prepare signs as separate reusable assets so construction can select compatible signs by tenant, era and building category. Preserve the approved sign identity for landmarks or explicit user designs. Avoid permanently baking interchangeable tenant lettering into the building's architectural textures.
- Signs may include 3D lettering, illuminated marquees, neon sequences or moving/rotating elements. Author pivots, attachment geometry, animation clips and separate emissive regions; distinguish fixture glow from local light pools. Provide simpler distant representations preserving sign identity. Runtime attachment, animation controllers and clock hookup remain a separate integration task.
- Expose controlled appearance choices: named material slots or masks for trim, masonry, metal and shutter colors; separate optional meshes and compatible attachment points for shutters, awnings, doors and ornament. Prefer coordinated appearance sets over arbitrary combinations. Keep material/part identifiers stable through revisions and LODs.
- Document a construction-time selection contract: choose a compatible sign and appearance set once, then persist their stable IDs and any animation seed/phase needed for continuity. Do not rerandomize on reload or LOD change. Record defaults and compatible alternatives in preparation metadata without claiming an unimplemented runtime schema already exists.
- Check representative combinations for collision, missing mounting hardware, overhang beyond the reserved footprint, opening clearance and day/night appearance. Preserve attachment transforms and selected appearance across Near / Medium / Far tiers; simplify small parts rather than adding every near-detail component to the far tier.

## Shared materials and texture sourcing

- Start with existing approved textures and materials. Extra user-supplied textures are not a prerequisite for the first reconstruction: inspect the building/reference and available library, then identify specific missing finishes. Request focused references or maps only when needed, such as distinctive glazed tile, decorative metal, polished stone or a particular sign style.
- Prefer a small shared architectural material library with reusable masonry, stone, concrete, metals and trim sheets for repeated borders, frames and ornament. Group by compatible finish and scale, including era-specific palettes where useful; avoid one enormous mandatory atlas for every building. Keep unique signs and distinctive facade details separable.
- A Tripo atlas is generally building-specific. Do not assume it can serve as a shared trim sheet without extracting/re-authoring suitable surfaces, consistent texel density and portable material maps. Reuse must respect the approved brick standard and retain believable surface character.
- Shared textures/material assets can reduce duplicate texture memory and material switching, but texture sharing alone does not guarantee fewer draw calls. Avoid creating unique material instances for every variation; evaluate palette/mask-based variation or supported per-instance data against the current rendering pipeline and batching/instancing rules during integration. Do not claim performance gains without representative scene profiling.
- Keep texture scale and material IDs consistent across buildings and LODs. Bake lower-tier detail into portable maps while retaining separate day albedo and night emission. Inventory dependencies, texture resolution and material/submesh counts alongside triangle counts.
- Use the first rebuilt building to establish a reusable socket, variation and material convention. Author the requested asset and its preparation contract; do not expand that work into an unsolicited game-wide runtime system.

## Three LOD tiers

Read [references/lod-standard.md](references/lod-standard.md) when creating, reviewing or packaging LODs. The preferred authoring standard is now **Near / Medium / Far**, not merely the older two-tier near/distant package.

- LOD0 Near: approximately 30,000 triangles; street-level architectural detail.
- LOD1 Medium: approximately 15,000 triangles; ordinary district view.
- LOD2 Far: approximately 1,000–3,000 triangles; many buildings visible together.

These are starting budgets, not reasons to destroy an approved silhouette. Record actual counts and explain material exceptions. Do not label a ~15,000-triangle medium mesh as the completed city-scale far tier.

## Metadata and storage

- Preserve a stable asset ID through revisions. Keep a `Building.json` beside the building files, recording name/revision, era, category/subcategory, dimensions, tile footprint, material dependencies, per-tier counts/files, animations, lighting, sign sockets/compatible sign IDs, appearance-set and optional-part IDs, shared-material references, provenance, validation and integration status.
- Read current catalog IDs rather than deriving IDs from display labels. At the time of this workflow, Art Deco Era maps to `modern`, Commercial to `commercial`, Office to `office`, and mid-wealth $$ to `Medium`. Verify against current code/catalog before integration.
- Record user choices as authoritative. When asked to auto-fill, estimate jobs/density using explicit floor-area assumptions, mark estimates and retain their basis for balancing. Do not mistake lot area for usable office area. Leave economic costs unset until there is a meaningful game-currency basis; don't invent an availability end date from the start-era label.
- Default private library location: `/Users/joelinstrum/Documents/CityForgeAssets/Buildings/Era - <Era>/<Building Name>/`. Keep approved models and references rather than replacing the previous building with the next one. Respect any explicit destination.
- Include editable masters, FBXs with an explicit tier mapping, texture dependencies, metadata, validation, matched previews, and useful source references. Pack or relativize texture dependencies and verify the stored copies. Preserve earlier approved revisions before replacing files; do not silently rename legacy LODs or overwrite another asset.
- Save native game-ready metadata only after checking current importer/schema requirements. The preparation manifest is not automatically a valid Unity catalog entry.

## Unity-ready delivery

For buildings intended for the native game, complete portable materials and the prepared Unity package during asset preparation. Read [references/unity-ready-packages.md](references/unity-ready-packages.md) for the tested adapter, catalog JSON, incremental staging helper and conversion pitfalls. Seal reviewed outputs against their authoring inputs; an unbaked Blender prototype must not be handed off as ready to import. Reuse unchanged outputs instead of baking and rebuilding them during every installation. This does not authorize installation when the user requested preparation only.

## Review and completion

Compare reference and rebuild from all four elevations/corners, with storefront/entry and roof-corner close-ups. Review day/night and opening poses. For all LODs, compare matched camera, screen size and lighting; preserve silhouette, footprint, facade colors and lit-room selection. Reimport exports to check finite geometry, counts, bounds, orientation, materials/UVs and required animations. Use [the LOD standard](references/lod-standard.md) for transition and performance checks.

Call the asset prepared/offline-validated when that is the evidence. Claim native integration or production readiness only for the stages actually completed. Report remaining material bakes, missing tiers and runtime checks explicitly.

For the established office example and legacy-tier caveat, read [references/art-deco-office-001.md](references/art-deco-office-001.md) when revising that asset or reusing its authoring work.

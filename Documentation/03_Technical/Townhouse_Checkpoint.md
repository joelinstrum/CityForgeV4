# Townhouse checkpoint — November 25, 2026

## Continue here

The user approved Green Cornice Bay modular composition v005 as the quality benchmark. It assembles the shared entrance, four-pane sash windows, bay windows, level sandstone foundation, green cornice and antique brick v002. Preserve this appearance and the original four-floor design.

The user authorized retiring **R2 $$ across every era**, retaining this building as the starting replacement. Townhouse is a residential subcategory, separate from density, wealth and era. Retirement must hide older choices from new placement and automatic growth while preserving stable IDs and existing saves. Do not silently broaden an Art Deco model into earlier eras.

## Current state

- Shared building editor opens from the selected placed building in the Lot Editor and from the in-game lot panel. Draft/apply consumers preserve lot-template versus placed-instance scope.
- Prepared sign mounts distinguish projecting blade and front-facing fascia signs. The browser filters compatible signs and supports Original/No sign.
- Prepared material regions support trim/awning tints. Experimental wall tint and its saturation control remain in place; the user dislikes the overall wall-tint effect and has deferred it. Default saturation is 50%.
- The bottom view control is labelled Pan. It pans the inspection camera rather than moving authored lot objects.
- Source-only changes are in Git. Models, textures, UI artwork and generated catalogs remain private dependencies.

## Approved art versus installed game

Private library: `/Users/joelinstrum/Documents/CityForgeAssets`.
Approved master: `Buildings/Era - Art Deco/Green Cornice Bay/v005/Composition.blend`.
Asset ID: `cf-building-artdeco-green-cornice-bay-001`.
Native ID: `v3-building-r2-art-deco-townhome-01`.

**v005 is a reviewed Blender composition, not an installed native replacement.** The native ID still resolves to an earlier reconstruction. It has 76,054 triangles and 656 editable mesh objects; those are authoring counts, not an approved runtime renderer budget. Whole-house consolidation, reduced LODs, portable native material/lighting setup, clock/door hookup and adjacency checks remain. Do not retire the roster first and accidentally present the old reconstruction as the approved replacement.

## Next implementation

1. Read the library workflow and reserve the stable building ID before publishing a new revision. Reuse immutable components read-only. Use a private background Blender/Unity project; other chats may own the live editors.
2. Prepare the approved composition for native use: consolidate renderers, build reduced geometry, retain shared material maps and stable day/night room selections, preserve outward door hinges and inspect both tiers.
3. Measure threshold, landing, stair/door swept clearance, party walls and frontage fence sockets. The accompanying `Townhouse_Transition.json` contains measured starting anchors in **Blender coordinates**, not Unity coordinates. Missing socket/clearance data is explicit.
4. Install and verify the approved native replacement; then apply reversible R2 Medium retirement to both placement and growth, leaving saved-ID lookup/rendering intact.
5. Later add Townhouse Row versus Commercial Row. Optional wrought iron frontage is generated at build time from compatible sockets, interrupted for stair approaches, with row-end/corner handling. Persist choices/layout rather than randomizing on reload.

## Review and validation limits

Earlier component/composition checks and day/night reviews are local evidence. The isolated PR snapshot compiled successfully for both runtime and editor assemblies against the existing Unity 6000.1.12f1 QA dependencies. This does not constitute a new native playthrough or city-scale performance result. Wall tint and saturation were introduced experimentally at the user's request without QA. Re-check private prepared-package availability when continuing; do not run old import scripts that overwrite later approved revisions.

Local review: `/Users/joelinstrum/Documents/Codex/2026-11-24/w/outputs/Green-Cornice-Bay-Modular-Composition/Review.html`.
Local continuation handoff: `/Users/joelinstrum/Documents/Codex/2026-11-24/w/outputs/Townhouse-Roster-Transition/Handoff.txt`.

# Session preservation handoff — November 27, 2026

This PR now collects this chat's source changes, not only lighting:

- Building Look house-icon entry point, building appearance persistence, wall/brick finish choices and native garden/fence setup helpers. Earlier commit58c1409 existed on feature/era-content-profiles but was not on main; its remaining changes have been reconciled here while preserving main's pan-tool behavior.
- Sandstone townhouse2x5 preparation helper and its updated mini-garden frontage positions. The revised mesh/prefabs/catalog remain private generated content.
- In-game developer/player toggle and searchable Lots picker, including template/growable preview instances and normal placement/save behavior.
- Art Deco-only first-person cockpit overlay and quiet ModelT audio. Existing driving physics and camera are unchanged. Other eras use native POV. Importer and Inspector configuration are included; PNG/audio remain private.
- Road/sidewalk weathering, darker muted sediment, stochastic anti-repetition, micro/medium/macro layers, variable gutters, sidewalk bluestone face shuffling, native binding and numeric profiles. Texture import metadata is included; supplied PNGs remain private. Source-only missing-texture behavior is documented in Surface_Weathering.md.
- Active E2 afternoon lighting, immutable E/C numeric references, GPU comparison and native lifecycle evidence. See Lighting_E2_Handoff.md.

The public repo is source-only. Private artwork, ModelT MP3, cockpit PNG, brick textures, white-flower meshes, garden prefabs, sandstone model revisions, limestone material-study assets, generated catalogs, test saves, captures and local experiment runners remain outside Git. They are retained in the existing private project/output workflow. This PR does not recreate those assets in a clean source checkout. Do not rerun asset generation for unchanged prepared kits; preserve GUIDs and materials.

Unrelated concurrent work excluded: architectural rendering lab, building roster/window-frame/weathering experiments, new vehicle fleet and social walkers, transport/terrain edits not already on main, and broad catalog changes. Main's existing changes were neither reset nor staged. Road shapes/avenues/bridges already merged in PR36 remain intact.

Historical visual and GPU evidence is in the chat's private output folders. Fresh assembled-branch compilation and developer/cockpit/surface checks are kept separately in Session-Commit-Checks; do not confuse the prior291-assertion cockpit run or isolated weathering GPU microbenchmark with a fresh integrated full-city performance claim.

Next work: review E2 in ordinary towns and clock transitions; establish the six-material test scene; profile standalone-player GPU timing and reflection refresh spikes. Keep further geometry/material/window work separate from lighting evaluation.

# Additional Founder-era civic models

November 20, 2026: five supplied FBX archives from the local `Downloads/civics`
folder are registered through the existing private V3 building workflow.

| Model | Category | Catalog ID | Approximate width × depth × height |
| --- | --- | --- | --- |
| Academy | Education | v3-building-founders-academy | 17.21 × 12.97 × 16.13 m |
| Courthouse | Government | v3-building-founders-courthouse | 15.48 × 12.30 × 14.74 m |
| Infirmary | Healthcare | v3-building-founders-infirmary | 10.27 × 8.77 × 9.95 m |
| Schoolhouse | Education | v3-building-founders-schoolhouse | 7.31 × 9.41 × 11.74 m |
| College | Education | v3-building-founders-college | 34.40 × 17.05 × 19.15 m |

All use Buildings / Civics, Founder’s Era, unspecified wealth, and no growable
zone. They appear in the model browser and support the Building Properties editor.
These are models rather than synthesized playable lots. Authors save their own
Civics lots to expose them in the district Civic/Ploppable browser; category and
first-era metadata on those saved lots remain authoritative.

Sizes are uniform doorway-proportion estimates, not surveyed dimensions. Near
geometry, proportions, original color/normal artwork, trim, masonry and roof forms
are retained. Origins are centered at ground level. At zero yaw the supplied front
faces +Z; FrontageOffset identifies this as North-facing for doorway/growth metadata.
The Building Properties editor can adjust default placement scale subsequently.

Model.fbx is used at Zoom 0–2 and Distant.fbx at Zoom 3–5, with return switching
verified. Reduced meshes preserve illuminated window geometry, UV seams, roofs,
upper towers, footprint extremes and ground seating. Other short-edge collapses
use manifold-link and face-orientation checks, without separating facade patches.
Final triangle counts are in the verification CSV; reductions are approximately
13–22%, not an FPS claim.

Selected front windows use explicit sash-aligned pane grids baked through actual
source mesh triangles, including UV islands. The College also has selected side
windows. The source grids and projection sizes are saved in private WindowGrid
and ImportInfo JSON files. Masks are 4096px source / 2048px high-quality BC7 desktop
imports, linear, with shared evening/night behavior on both mesh tiers. Open bell
towers, louvers and unselected glazing remain unlit. No exterior lamp fixtures
were identified in the supplied models, and no fixture/pool geometry was invented.

Supplied roughness/metallic data is packed into a private linear metallic/smoothness
texture for URP Lit; color and normal images are preserved. ConfigureCivicSurface
only opts in explicitly categorized new civics with the packed map. Legacy civic
materials and the approved terrain/forest/ocean rendering are unchanged.
The new civic materials use a 0.35 smoothness multiplier to retain the supplied
roughness variation while avoiding overly shiny roof and window highlights.

NewCivicsQA in the isolated project checks five registrations and classification;
Building Properties compatibility; centered pivots and near/far bounds; reduced
geometry and bidirectional LOD switching; both-tier noon/evening/night masks and
PBR bindings; cardinal physical lot fit; and isolated lot-library roundtrip/civic
browser routing. Four-corner day, close facade, evening and near/far night captures
were visually inspected. R3 candidate counts remain unchanged. Evidence is in the
November20 ple/outputs/Civics-New directory. Authoring scripts and isolated fixture
lots are in ple/work/civics-new; no player region or lot-library saves were written.

All FBXs, textures, masks, materials, generated catalogs, import metadata and raw
archives remain private/ignored. The public source registrations require those
local dependencies for visual reproduction.

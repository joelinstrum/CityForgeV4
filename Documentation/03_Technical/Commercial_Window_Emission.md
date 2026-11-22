# Commercial pane-aligned night lighting · November 20, 2026

The C1/C2 commercial window pass covers all29 current commercial catalog entries.
Twenty-seven glazed buildings receive rebuilt 4096px pane masks, with143 selected
window/storefront grids. The open-front Blacksmith (False Front) and Livery retain their existing
forge/fixture/side-window PNGs byte-for-byte.
Plymouth Store receives its first selected-window night mask through the existing
FoundersShop/ConfigureBuildingWindowMask registration, without a Plymouth-specific runtime branch.

Original near/far FBX, diffuse and normal files are preserved. Masks are rasterized
through actual source mesh triangles and UV islands, following the approved R3
workflow. Pane positions are measured against320px orthographic daylight projections
(X horizontal, Z vertical, viewed from-Y, span1.08×max(width,height)). Numeric grids
are saved with each private asset as WindowGrid.json. Storefront detections were
curated to distinguish sash from bottles, barrels and shelving. Selected panes use
155/255, with .55/.65 source-pixel sash margins and two atlas-texel triangle padding.

Rasterize all relevant front-half triangles within the pane projection. Do not use
an albedo-brightness threshold for glass, skip UV-seam-spanning quads, or limit the
bake to the single front-visible facet: oblique views expose the resulting dark
facet patches. Preserve the authored fixture glow separately from pane emission.
C2 fixture values above the pane range are retained; C1 warm fixture regions outside
replaced windows are kept. Open workshop forge/lantern maps are unchanged.

Each glazed model also has a private NightWindowGlass.asset: thin pane rectangles
placed from the same measured sash grid and sampled source-frame depth. The27
meshes total1,338 quads. This fills folds/gaps in the scanned glass which an emission
texture alone cannot repair at oblique angles. Meshes use normalized source bounds. Runtime resolves the fitted near-model
bounds in object space and positions/scales the glazing beneath the placed-object
transform, so edited footprints plus saved scale, rotation and elevation remain
aligned. They are independent of the two existing body LODs.

LotAssetCatalogEntry.nightWindowGlass is optional; missing data retains the prior
mask-only behavior. ConfigureBuildingWindowMask registers ready meshes; catalog
setup merges the field into matching entries. ResidentialWindowLights creates one
shared-material glazing renderer, with no shadows, and enables it only for Evening/
Night. The original per-phase emission color drives it at155/255 strength through
Resources/Region/NightWindowGlass.shader, with normal fog. Noon disables the renderer.
Other buildings and the residential masks do not acquire glazing meshes.

Runtime mask import is linear, maximum2048, mipmapped, high-quality BC7 on desktop.
Existing warm noon/evening/night emission, exterior lamp pools, facade materials,
LOD meshes and transition thresholds retain their settings. No global brightness
or brick/material recolor is introduced. Original/private art remains ignored.

Targeted QA covers all29 registrations, medium-C2 growth candidates, reduced mesh
presence, existing LOD transitions in both directions, both-tier day/night emission,
C2 exterior lamp pools, night-only glazing/shader checks, footprint changes on
rotated/scaled/elevated lots and before/after/front/corner/distant renders. Original mesh,
color and normal SHA256 values are recorded separately. Evidence is in November20
ple/outputs/Commercial-Windows. Player saves/lot definitions are not written.
This is targeted lighting QA, not a full district performance/regression claim.

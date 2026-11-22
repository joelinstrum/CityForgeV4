# R3 pane-aligned window emission

November 20, 2026 local visual refinement for the sixteen supplied `R3MidWealth*`
buildings. These assets remain in the ignored/private Lots workflow.

The former painter selected dark color-texture pixels inside approximate window
rectangles and skipped screen-to-UV quads spanning UV seams. This produced noisy
glowing patches, incomplete panes and lost sash definition. Dimming emission alone
does not repair the pane boundaries.

Each selected window now has explicit sash centerlines measured against its supplied
daylight artwork. Pane masks are rasterized through actual source mesh triangles,
including UV islands. The source projection is 320 square pixels, horizontal X /
vertical Z, viewed from -Y, with the original bound center and a span of 1.08 times
the greater of source width/height. Glass is inset from sash centerlines by 0.5
horizontal and 0.6 vertical source pixels. Triangle padding is two atlas texels;
pane membership is evaluated in projected space so padding does not erase the sash.

Source masks are 4096px grayscale PNGs, pane value 155/255. Desktop import uses
linear sampling, maximum size 2048, high-quality BC7 and mipmaps. Keep existing
warm time-of-day emission values, diffuse/normal artwork, lantern lighting and
material bindings. Small separately authored lamp/door mask regions are retained;
accidental old glow elsewhere on the facade is discarded. Each private building
folder includes `WindowGrid.json` with its numeric authoring data.

Stone Dutch and Stone House had distant UV/geometry distortion exposed by the
sharper grids. Their replacement `Distant.fbx` files retain original front geometry
and UVs. Reduction only collapses 100 short, independent, seam-free rear edges per
model, with manifold-link and face-orientation checks. Stone Dutch is 23,592 near /
23,392 distant triangles; Stone House is 23,999 / 23,799. These two distant meshes
are heavier than the previous alternates. The other fourteen distant FBXs, all near
FBXs, all color/normal textures and the runtime lighting code are unchanged.

Avoid unrestricted planar dissolve or separately decimating a cut-out facade:
review attempts produced facade gaps and were rejected. Do not restore those
intermediate meshes. Authoring scripts, backups and final staged assets are in the
November20 `ple/work/window-grid` workspace; the final reducer is `lod_safe.py`.

Isolated Unity checks covered all sixteen registrations, dimensions/pivots, reduced
mesh availability, LOD 0/2/3/5/2 switching, and both-tier noon/evening/night emission
and lantern behavior. Final front/oblique and distant night captures were visually
inspected. Main-project original mesh/color/normal hashes and fourteen unchanged
distant FBX hashes were checked during synchronization. No player region or lot
library save was opened or written. Evidence is in `ple/outputs/R3-Window-Grid`.
This is targeted lighting/LOD QA, not an exhaustive district regression or FPS test.

November5: underwater basin run now45% of stored playerBankWidth (minimum2m), preserving dry-side grading and maximum depth. Existing saved lakes adopt it on replay without descriptor changes.

November5 reference refinement: calm-water shallow reflection contribution eases in
with depth; shallow cubemap highlights are gently biased away from violet while deep
water/river formulas remain unchanged. Player lake wet-edge gravel is more visible,
dry strip0.7–2.2m, with intermittent grass, smaller stones closer to the waterline
and a sparse independently seeded set of larger shoreline anchors. Geometry and
stored descriptors are unchanged. Seeded dressing/paint remain stable on reopening.

Current bank treatment (November4 rework)

Player lakes use an irregular0.9–3.4m mud/gravel edge, not a uniform tan beach or a
wide airbrushed fade. Shared damp dirt uses a muted tint/5m tiling; existing rock/sand
textures remain. Player-lake shore dressing has fewer rock groups and small isolated
reed stands. Geometry, lake water/reflections and natural-lake population are retained.

Generated riverbank painting must require an actual non-player river in the district.
GeneratedRiverDistrict alone is a rendering route, not sufficient painting eligibility.
Old caches can contain its broad soil0 halo; LakeMeadowRepair reconstructs meadow
weights locally before lake painting/canonical capture, while protecting native-water,
road/camp/other-lake/channel surfaces. It also updates an existing lake-base cache so
multiple lake repairs remain stable on save/reopen. Do not repair a halo by repeatedly
painting more opaque wet layers over it. Read HANDOFF.md for verification fixtures.
Earlier bank-width descriptions below document superseded experiments.

Visible dry lake banks now use40% of the previous feather width (60% narrower),
via LakeShoreRenderer.DryBankFeather. This does not alter stored basin bank widths,
water footprint or underwater slope; it only narrows the exposed-earth transition.

Shore transition: preserve existing meadow via a smooth earth blend instead of
painting the bright native fifth layer as moss. Player-lake dry feather extends to
outer-bank width+6m (noise85–100%); natural lakes use12m. Expand painting bounds only,
preserving seeded shore-detail placement bounds. No changes to water or basin shape.

Lake visual follow-up: bright white shore highlights came from opaque diffuse-alpha
smoothness in the base soil exposed by shoreline painting. Lake setup now makes
owned matte RGB-preserving copies of base soil and wet-bed diffuse textures, leaving
source materials/textures untouched. Visual diagnostic confirmed the white rim is
removed; do not rely on TerrainLayer.smoothness=0 alone in URP.

Approved appearance source: October12 Lakes-And-Ponds-Final-Report.txt and
Lake-Final-Editor-Zoom1.png in that session outputs. Shared Calm Lake Water shader,
Water/LakeWater material, LakeShoreRenderer and sharp cached cubemap are retained.
PlayerLakeGeometry.ConfigureApprovedProfile sets the new basin's size-scaled depth,
inner slope and separate playerOuterBankWidth. This uses the accepted large-lake
6m/35m/14m profile rather than the early shallow3m/8m trial. Existing descriptors with
outer width0 retain their stored legacy geometry unless explicitly reprofiled.

Lake completion now automatically dismisses the tool, clears the blue guides and
restores the default cursor. The lake remains; no Escape press is needed. This is
a lake-adapter Completed handler, not a global PointWire behavior.

# Player-created lakes and ponds

Water > Add Water Bodies > Lake / Pond uses PointWire guide markers. Closing a
valid loop commits water immediately; normal Save District/Region persists it.
Backspace edits the guide before closure; Escape cancels a sketch and does not undo
an already-created lake. Select Lake / Pond again for another body.

PlayerLakeEdits separates consumer semantics from PointWire. It validates a simple,
noncrossing guide, rounds it with three Chaikin passes, checks the rounded polygon,
then stores the final normalized regional outline in RegionWaterBody. Guides are
not the shoreline: corners are rounded inward without a spline overshoot. The approved basin profile scales with body size toward6m depth,35m underwater
slope and14m outer banks (smaller ponds scale down); bodies below5,000m² are typed Pond, others Lake.
The same approved LakeWaterRenderer/LakeShoreRenderer supplies calm water, depth,
reflections and wet bed/shore materials at an individual fixed water elevation.

First-pass bounds:3–64 guide points; at least100m² rounded area; at most30% of the
district bounding square;32 added bodies per region. Ground must leave9m room inside
the active district,12m from existing water and space from roads/camps; shoreline
height span cannot exceed12m. Merging water systems and user-selected water heights,
depths, shore widths and water removal are future work. Invalid completion retains
the guide and explains why, without adding water. Lakes do not raise original ground.

RegionWaterBody.playerCreated/playerDepth/playerBankWidth are additive saved fields.
The normalized polygon and surfaceLevelNormalized are authoritative. Basin replay
runs on district heights before SetHeights, independently of cached meadow painting.
It lowers a bounded patch, blends outward and leaves distant terrain unchanged.
Body-specific rendering is created once, including in coastal districts. Existing
natural water recipes retain their route; new fields default off in older saves.
The regional relief map recognizes the typed lake and uses its surface elevation.

The original pre-lake heightfield is retained privately for woodland percentile
eligibility. DistrictTreeCoverage ranks against original heights/slopes and natural
water, then filters lake/bank masks. Never re-rank based on excavated ground: doing
so changes forests elsewhere. Both canonical and explicit planted trees are excluded
from the stored outer-bank clearing, on live creation and stale-cache/fresh replay. Lake masks
have priority over older tree-addition records. Add-forest habitat queries see the
new shoreline. Existing forest objects outside the clearing are retained live.

Checks passed in disposable fixtures: generic PointWire state; crossed outline
rejection without mutation; actual wire closure creates32-point rounded lake;
center bed>2m below water; distant terrain unchanged; cleared trees; signed water
queries; disk/stale-cache and no-cache regeneration retain exact remaining forest
positions and lake identity; second independent Pond and two-body disk reopening.
Closed/live-string and filled lake renders inspected. Test source/report under
Library/ and the task work directory; no normal player saves written. Mixed
chestnut/locust/larch map qualified; tropical/coastal presentation not separately
visually qualified in this pass.

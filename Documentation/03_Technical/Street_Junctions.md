# Street junctions

The four street IDs (paved-street and the three Founders streets) use PointWire's
opt-in PlaceOnRelease gesture. Press/drag/release draws a street; individual clicks
still place waypoints. Releasing at a snapped target completes a T. Continuing
past the snap lock creates a crossing, shown by the same green square at the
centerline intersection. Road tools and other PointWire consumers retain their
existing input. The existing drag-from-an-existing-street gesture remains supported.

Street snap candidates follow saved-record order, prefer endpoints within 1.25m,
acquire within half the target width plus .8m, and retain the chosen coordinate
within 3.1m. After leaving the lock, a crossing endpoint bypasses tile-center
snapping so the grid cannot pull it back onto the target street. The indicator
uses the projected placement guide, including existing alignment/extension rules.

StreetJunctions builds explicit derived nodes and outward arms from exact saved
centerline intersections, endpoint connections and sharp polyline corners. It
merges points within 2cm; mere overlapping pavement is insufficient. Nodes rebuild
after placement, edit, repave, removal and district load, without a new save schema.
FreightRoadGraph already splits exact centerline intersections for connected routes.
Historical near misses are not moved automatically. Curved paths use their actual
sampled centerlines, without adding junction patches at every curve sample.

DistrictStreetJunctions renders one terrain-following mesh per node. Quadratic inner
corner fillets have a maximum 1.1m tangent length. Acute intersections receive longer
approaches; neighboring nodes constrain the available length. Four original street
albedos/tints blend by distance to the connected arms; per-surface UVs preserve
world paving alignment and the dirt street's lengthwise wagon texture. The overlay
fades into existing street surfaces over its final meter. It receives directional
lighting/shadows but casts no extra shadows. Rendering uses the original authored
Resources/UI/StreetJunction shader; artwork stays private. Flare meshes that fail
normal dry-ground, building, camp or district checks are omitted. This is a first
visual pass: existing sidewalk ribbons remain; no new pedestrian turn animation,
traffic controls, bridges or road junction policy is introduced.

StreetJunctionChecks.Run checks T/cross/angled/collinear/elbow topology, simple
rounded outlines, road exclusion, JSON reconstruction, removal and freight routing.
The isolated StreetJunctionQA wrapper additionally checks the real presentation,
shader/render, PointWire release/drag/click/cancel handling, snap hysteresis,
crossing overshoot, green overlay and teardown. Use the private CityForgeQA project;
never substitute player saves for a fixture.

## November18 visual corrections

Dirt junctions now use the existing seamless DirtRoadEarth albedo in world UVs,
fading back to the original wagon ruts at the approaches. This avoids both the
nearest-segment closed-loop seam and stretched/repeated grassy shoulders at joins. Native ground-cover exclusions include street widths plus
sidewalks and a plant margin. The streamer invalidates affected road tiles on load
or edits; no terrain regeneration or saved plant metadata change is required.

DistrictLotSurfaces defines bounded path lift (.075–.080m), transparent path queue
4034 and sidewalk queue4040. District sidewalks sit at .12m; junction patches use
queue4041 to keep street mouths above intersecting sidewalk ribbons.

## Sidewalk and lot-path overlap masks

StreetSurfaceMask indexes convex road-footprint pieces in a16m world grid. It
subtracts pavement footprints from sidewalk triangles, so joins work even for old
edge-touching streets that have no derived centerline node. Rounded node footprints
are also included. The same mask widened to sidewalk edges subtracts coverage from
lot paths; the remaining edges receive a .55m soft vertex-opacity fade. Frontage
soft paths are trimmed at other roads with a .3m fade. Geometry beneath paving is
absent, removing depth competition instead of depending on transparent sorting.

Clipped vertices interpolate height, normal, tangent, UV0, ribbon UV1 and preexisting
alpha. The source meshes stay unchanged and are reused for every remask; road
removal restores full paths. Masks update with RefreshDistrictSidewalks on road/lot
edits and load. This is derived presentation only, with no saved path/schema edits.
SidewalkOverlapQA checks rotated lots, slopes/UVs, repeated geometry, near-miss T
joins, coincident lot paths, restoration and a rendered review.

## Six-inch visual recess

District roads and streets use exactly .1524m virtual depth through LotPathBlend.
The original terrain/road meshes and actor heights are not lowered; parallax shifts
the apparent road bed and shows an earth-cut or paved curb wall. Sidewalk grade
stays unchanged. Street Junction shares the same shading/boundary across blends.

StreetSurfaceMask.BoundaryEdges subtracts covered edge intervals and retains the
exterior of the complete pavement union, including rounded joins. DistrictRoadRecess
caches a shared signed-distance RFloat field with nominal .5m spacing, capped at
1024x1024 (4MiB). Road geometry/width/curve/surface signatures control invalidation.
Two boundary samples per shaded road/junction pixel determine the virtual wall hit
and edge shadow; no extra road meshes or draw calls are created. RoadRecess.hlsl
is original source, and licensed lining textures remain private. Legacy lot path
recesses keep their own ribbon-based rendering.

RoadRecessQA covers exterior union edges, duplicate strips, T mouths, exact depth,
all principal surfaces, curve/junction rendering, ground-level sidewalks, original
geometry/save invariance, cached refresh and cleanup. It reruns SidewalkOverlapQA.
The small six-road fixture rebuilt its 106x112 field in about26ms; this measures
edit-time field construction, not full-town GPU/frame-rate overhead.

## November19 curved defaults and connection polish

New roads drawn by the transport tool and all preset street rings/spans now use
curved=true. Existing saved straight/curved choices remain authoritative, and the
context-menu Straighten angles action still works. Preset span clipping/merging
preserves curvature. When both a proposal and existing road are curved,
UnpavedRoadPieces compares their authored guides for shared-run coverage; comparing
a sharp proposal guide to the receiving rounded samples otherwise re-created
small corner fragments. All12 preset size/cardinal-rotation overlap checks pass
with zero new pieces for an existing matching ring.

Junctions now include ordinary road-to-street connections when at least one
participant is a street. Unrelated road-to-road junctions retain their old policy.
Same-direction arms use the longest incident continuation, not the shortest
endpoint stub. Width uses the widest overlapping arm with stable surface-ID ties.
This prevents a short overlapping end from eliminating one side of a T fillet.

At rounded receiving corners, an authored guide intersection still creates a
node if both rendered ribbons cover it and no nearby sampled intersection already
expresses that pair. All incident authored guide legs at an established node are
included, so a perpendicular leg does not hide the long parallel continuation.
This preserves deliberate saved connections without snapping nearby unconnected
roads. Zero-length intersection segments are rejected. Nearby nodes still bound
reach. Source checks cover both T fillets, overlap stubs, curved receiving guides,
road/street inclusion and unrelated-road exclusion.

Default approaches extend by5m beyond the half-width (previously3m), and overlay
opacity fades across the final3.5m (previously1m). Plain dirt and dirt streets share
the existing seamless earth at their meeting. Junctions now use UniversalFragmentPBR
with the road ribbons' lighting approach, replacing the brighter hand-lit patch.
Original road textures remain at the approaches; no imported art was changed.

Shared recess fields are rebound to junction materials as well as road materials
when rebuilt, avoiding a stale/missing boundary after presentation updates. Exposed
lining brightness is25% lower (.72 to.54) in both road/path and junction shaders;
pavement albedo and the six-inch visual depth are unchanged.

Private JunctionPolishQA renders bilateral T, curved receiver, road/street blend
and a read-only copy of the user's saved road topology. It checks simple bilateral
outlines, current recess bindings, shaders, curved defaults and station scaling.
SavedPatrolQA passes with the resized station, including real-interior blocking,
eight headings,2000 cached obstacle probes and full Patrol/Harass regressions.


## Connected corner cleanup · November19

Curved guides retain rounding only at isolated bends. Additional incident road legs
mark local derived junctionCorners; the shared RoundCorners sampler skips those
indices for pavement, sidewalks, shoulders and navigation. Saved guides/preferences
are unchanged. Refresh recomputes after edits and deletion, restoring isolated bends.
Ordinary road/road Ts now receive junction patches as well as street connections.

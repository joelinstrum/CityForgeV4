# Player stream appearance and creation feedback — November 5, 2026

Player streams now reuse GeneratedStreamAppearance’s approved Pass17 scanned patches,
ten individual stone meshes and native shrub prefabs. The optional player-route adapter
uses cumulative world-metre stations and immutable playerChannelLevels, preserving the
native generated-stream path. Player shrubs render in separate instanced batches with
runtime material copies, refreshed for the active season; they never enter canonical forest/saved tree arrays.

Channel beds fully replace meadow texture. Dry stream shoulders use the approved
StreamBankBlend.VariedCover with route-local coordinates; exposed excavated faces retain
earth coverage rather than stretched grass. Shared lake-bed palettes remain unchanged.
Canonical/native/mixed trees clear to the channel and bank plus a2m buffer. Streamed
ground-cover tiles are invalidated after creation and exclude the channel corridor on
rebuild. Existing channel records provide the same exclusion during cache restore and
fresh generation. All player surfaces/dressing are rebuilt after a channel edit to
avoid stale bank shrubs and duplicate extension dressing.

RiverRouteTool opts into deferred PointWire completion. Enter immediately restores the
normal cursor and hides guides, shows a modal “Creating stream…” or “Creating river…”
overlay, and blocks camera/map keyboard input. The overlay presents two frames before
validation and construction. Invalid routes return to the same editable sketch; success
clears it. Repeat Enter is ignored while busy. Synchronous lake completion is unchanged.
No channel version, saved route/grade, lake or native-water routing changes.

Verified actual Enter completion entry point, immediate cursor/busy state before any
mutation, double-Enter rejection, invalid sketch retention, valid cleanup, canonical and
mixed tree clearing,300 fully textured bed samples, single surfaces/approved dressing,
disk/stale-tree-cache reopening and fresh generation with exact surviving forest.
Disposable checks stay in Library; latest user checkpoint BeforePlayerStreamRefinement.

# Player streams and rivers

Water > Add Water Bodies > Stream or River starts RiverRouteTool, an open PointWire
consumer. Click downstream to place the route; Enter creates it; Backspace undoes
markers; Escape cancels. The hint is visible throughout drawing. Two points suffice;
additional points produce smooth bends. Blue parallel guides preview6m stream or
18m river width. Completion clears the guide and restores the normal cursor.

Shared district connections

A point within20m of an exterior active-union edge snaps just inside that edge.
Enter commits a single regional route, with a64m continuation into the adjacent
original district. Regional outer edges and internal edges of combined districts
are excluded. The curve stays inside the playable union until its terminal portal;
it cannot cut across an excluded corner. When the neighbor opens it renders/carves
its portion from the same saved record. Start within16m of the matching downstream
endpoint to snap and extend it; keep the same Stream/River type. Extension retains
the ID and every prior centerline/grade sample. It does not resmooth older reaches.

Geometry and persistence

RegionWaterBody.playerChannel is independent of playerCreated (the lake flag).
Typed River plus isStream identifies the kind. Centerline coordinates are normalized
region-wide; playerChannelLevels stores immutable world-metre surface grades.
Guide points are separate; RoundOpen preserves endpoints and rounds interior bends,
then resamples at approximately4m. PlayerChannelSampler uses the existing segment
hierarchy for nearest station and binary interpolation of stored grades. Both sides
of a boundary query the identical route and levels. Downstream grades never rise;
routes needing more than12m incision are rejected. This is authored channel shaping,
not a hydraulic simulation. River depth2m/banks6m; stream depth.65m/banks3m.

The channel is channelVersion0 intentionally: native generated-water evaluators,
heightfields, coverage ranking and native renderer routing must exclude playerChannel.
Do not turn it into a generated version2 channel: that would alter legacy terrain,
water levels and route queries. ApplyPlayerChannels replays a bounded cut after
original-ground coverage capture, before terrain SetHeights. Clearing masks remove
canonical, explicit planted and authored trees; added trees also avoid the banks.
Original-ground coverage ranking remains unchanged, so no forest is redistributed.

LakeWaterRenderer.InitializePlayerChannel uses the bounded moving-ripple river route,
with per-body footprint, terrain depth, sampled grade mesh and downstream field.
Depth and flow textures share the clipped body rectangle, not the full district.
It is a dedicated player-channel entry point; original ocean/lake/native river
settings remain separate. No still-water cubemap is generated for these channels.
Native terrain bounds and active-union masks clip water at the district edge.
Bank painting uses matte approved soil/pebble/sand layers and a broad dry feather;
canonical pre-edit maps are used for deterministic repainting. Existing source art
is unchanged. The regional relief map recognizes saved player channels as water.

Limits:2–64 guide points,4096 samples per route,65536 samples/64 routes per region.
A route must have at least10m endpoint separation. Camps, roads and existing water
are protected; natural-water/lake connections, confluences, reverse/upstream extension,
adjustable widths and flow simulation are future work. Water > Remove Water Bodies
deletes the entire player route, including any connected reach in a neighboring
district. Save District/Region persists routes and removals globally even if only
one detailed district cache is saved.

Verification

Disposable check drove actual menu callbacks and the shared CompletePath entry point.
District19 ->20 crossing had identical bed heights32.8145m at the seam and identical
water grade; each side rendered one surface. Neighbor-end snapping and extension
retained the original ID and all prior stations. River and Stream disk reopening,
a deliberately stale tree cache, and no-cache generation preserved exact surviving
forest positions with no underwater regrowth. Lake closed-loop PointWire behavior
also passed. Route/bank visuals and the drawing hint/ribbon are inspected separately.
No normal player saves are written by checks. Work scripts and reports are under
Library/ and the current task work directory. No automated physical key/mouse
injection is claimed; Enter calls the same verified CompletePath method.


## Canal creation · November19

Water Features > Canal > Draw Feature reuses RiverRouteTool with Canal=true. The
regional descriptor has player-canal-v1,8m width,1.6m depth and1.2m banks. Guides
remain straight; stored grades, portals and downstream extensions retain existing
channel persistence and protections. Extensions match profiles. Excavation uses a
flatter Pow16 cross-section; CanalBankRenderer builds a separate masonry-bank mesh
under the water object. Existing stream/river descriptors and paths are unchanged.


## Narrow stream widths · November19

New streams use width version1: Uniform6m, Gentle5–7m, Natural4–8m. Vary width
appears only for Stream. Smooth, body-ID-seeded variation is evaluated by cumulative
normalized station and shared by terrain, paint, water masks and stream dressing.
New stream banks1.5m/depth0.45m keep the corridor modest. Optional descriptor fields
persist version/mode; legacy zero retains existing widths. Extensions keep their
original mode/dimensions and stable old stations. River action is hidden for now;
internal channel mechanics remain available for future rivers.


## Bankfull stream presentation · November19

Player stream water now fills15cm above the saved excavation grade (5cm below the
original ground used to establish a fresh grade). PlayerChannelGeometry.Level is
the shared actual surface query for mesh, cached depth, wildlife and removal;
Sampler.Query retains the original excavation grade so terrain replay, widths,
station persistence and extensions do not change. Existing saved streams receive
the same fill on reopening without rewriting their descriptors. The footprint
still clips to the shared local width; this does not flood outside the corridor.
Player streams use absorption density1.4 to retain a readable water surface over
the shallow rock bed. The shader default remains0.42 for all other water bodies.


## Terrain-following stream rapids · November19

New player stream grades sample CoverageHeight (original detailed district ground)
rather than smoothed regional heights; off-district portal stubs still use regional
heights and persist their shared stations. Streams resample at1m to retain short
ledges. New unconnected sketches whose end is higher reverse before portal/grade
construction, so flow starts at the higher end. Extensions preserve their established
direction, ID, width mode and every prior station. Canal/river grading is unchanged.
Downstream grades remain nonincreasing: ordinary rises are cut, while routes climbing
over12m still explain that the stream needs a downhill path around the hill. A
waterfall cannot lift water over a ridge. Downhill drops have no trench restriction.

Player-stream materials enable slope-derived animated whitewater: quiet pools remain
clear, gradients form broken rapids, and steep grade faces form brighter cascading
sheets. It uses the saved water mesh and flow texture, with no per-frame physics,
particles or terrain mutation. All other water materials default cascades off. Old
stream geometry is preserved and receives slope shading wherever grades already drop.

Private StreamCascadeQA creates a route over ground differing20m from the regional
approximation, including sloping rapids and a4m ledge; verifies downhill reversal,
nonrising grades, stream-only shading, descriptor roundtrip/exact terrain replay and
extension preservation. Two rendered animation times are inspected; ordinary width,
bankfull, canal/marsh and prior integration checks also pass. No player saves written.


## Stream joins and falling curtains · November19 (supersedes restrictions above)

Player streams now snap within12m to any part of another player stream. Either
endpoint, tributaries into a middle reach and crossings are allowed; the previous
downstream-start-only and stream-overlap/self-overlap rejection rules no longer
apply. Existing downstream extensions keep their ID/stations. An uphill sketch
from an old downstream endpoint instead becomes a separate incoming branch,
automatically oriented downhill. The12m incision rejection remains only for
canals/rivers; streams can cut through uneven terrain. Roads, camps, regional bounds,
finite route limits and unsupported lakes/native-water intersections remain guarded.

Confluence grades blend into the receiver within its wet width plus5m. Receiver
records are immutable. Earlier stream surfaces own coincident wet pixels, avoiding
doubled transparency. Ownership is rebuilt from persisted array order, with only
overlapping streams included in the per-surface query list. Indexed nearest-station
queries keep snapping/blending bounded. Intersections are geometric and do not
introduce fragile saved ID references; removing a reach leaves other records intact.
Removal bounds now use full grade extrema and the actual15cm stream fill.

StreamWaterfallRenderer builds an overhanging lip, vertically subdivided falling
curtain and elliptical landing foam for drops>=0.8m on gradients>=0.6. Adjacent
steep stations group into falls with at most8m run and128 falls/body. Geometry and
animated procedural shader are separate from slope foam; there is no per-frame
terrain/physics simulation. The shader lives in Rendering/Water/Resources so builds
include it. All geometry is regenerated from saved grades on load and cleaned up
with the parent water object. Existing streams also receive falling curtains.

Private StreamConnectionsQA passes middle tributary, upstream-end join, reversed
uphill incoming branch from a downstream endpoint, receiver immutability, matched
join levels, network serialization, extension preservation, exact terrain replay,
actual falling geometry and shader support. Rendered join and waterfall inspected;
previous width/bankfull/canal/marsh checks pass. No player saves modified.


## Preserve surrounding terrain · November19

Drawn streams are excluded from TerrainGeography's44m freshwater/moist-grass
influence, which caused a broad bright green strip after creation. Native water,
lakes, marshes and canals retain their existing moisture rules. Near a player
stream, geography elevation/slope classification uses retained original coverage
ground, so excavation cannot change nearby grass into a different biome surface.
Player stream bed painting skips every dry-bank sample (distance>=local wet half
width); original alpha weights and authored paint remain there. Bed/rock/water
rendering is retained inside the channel. Derived controls/bed paint rebuild on
load, so existing streams receive the correction without rewriting player saves.

StreamGroundQA compares168 before/after geographic control samples and all dry-bank
alpha samples: unchanged. Native stream/canal/marsh moisture remains included.
Actual before/after terrain renders use the geography control maps; connection,
waterfall, replay and width regression checks run in the same disposable fixture.


## Clear stream visual refinement · November19

Player streams use their owned LakeWaterRenderer material with ripple tile14m,
strength2.4, roughness0.14, sun glint cap2 and fallback absorption0.75. The shared
shader keeps native river/lake/canal defaults. Player-only two-phase advected normal
fields combine14m and4.48m tiles. Bounded specular antialiasing and narrow sky/sun
responses replace broad fog-like highlights; daylight scaling keeps night water dark.
An opaque-color copy shared by the district camera supplies subtle refraction and
depth-graded spectral transmission. Cached bed depth remains authoritative. This
adds no wave physics or per-body reflection capture; one opaque copy is a rendering
cost and standalone GPU timing has not been qualified.

Stream-only runtime coarse pebbles clone the existing wet gravel layer at7m tile
size. Coarse coverage favors the margins, with finer gravel in the center. The layer
is derived, not saved; canonical pre-water alpha maps remain authoritative. Dry bank
samples are still untouched. Existing instanced stone assets form irregular edge
pebbles and occasional larger seats; existing meadow grass0/1 forms seeded clumps.
Native generated streams retain their previous dressing path. No new source art or
waterway schema was introduced; saved routes/grades/widths are unchanged.

Seventeen screenshot iterations plus baseline were compared with the supplied goal.
FinalStreamQA runs StreamGroundQA, StreamLookQA and SavedStreamLookQA:168 geography
controls and all tested dry-bank alpha values preserved, network joins/extensions,
waterfalls, exact terrain replay and persistence passed. Final renders use production
defaults at two animation times, two camera scales, two sun angles and night light.
A48-frame/24fps shader motion clip confirms movement; a grass-only image region is
identical between frames. A copied actual user region was opened through the normal
build path and rendered; original SHA256 is unchanged, no normal save was called.
Outputs are in the current chat outputs/Stream-Look. Main Unity was stopped after
the user saved/returned ready, then refreshed successfully.


## Channel load sampling (November 19, 2026)

PlayerChannelGeometry.RasterRanges builds conservative segment bands per raster
row. Bank painting, water footprint/depth masks and graded surface mesh generation
query only these bands. Padding retains visible shoreline samples and interpolation;
height/containment share the same nearest-station result. Native generated water
keeps its existing path. The saved two-stream QA district's player water meshes and
paint stage fell from 5475 ms to 1444 ms; total scene load is a separate measurement.


### District water load and wide dressing (November19)

Crest native-river clip masks now query conservative segment bands instead of every
dry district pixel. Ocean bounds and legacy native segments remain included; exact
clip values and resolution are retained. ApprovedRiverWater excludes dedicated
player-channel surfaces from native promotion. Channel height replay uses the same
conservative bands as paint. Near decoration is unchanged; zoom4/5 use wider batches
with subpixel stone omissions and no small dressing shadows. The shade/terrain/wet
footprint of player streams is unchanged. Stage logs include heightfield time.

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

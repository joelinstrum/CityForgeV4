# Assignable lumber freight

Camp composition is authored in the Lot Editor. Buildings and lot names do not
activate workers or spawn camp wagons. Harbor Council House lots default to the
Town Center receiving role; explicit receiver settings override that default.

- Select an Axeman and assign Character Script > Scripted > Lumberjack.
- Select a vehicle and open Script > Vehicle Behavior. Choose Resource and its cargo,
  a time period, and Town Center, then Save behavior. Suitable wagon profiles declare
  supportsResourceFreight. The previous Lumber Wagon assignment remains a legacy
  saved-data behavior.
- Set receiver lots through Deliveries > Lumber Mill or Town Center. For an already
  placed Harbor Council lot, select it and click Receives to cycle None, Lumber Mill,
  Town Center. This instance setting persists with the region. The lot selection
  panel shows stored and delivered timber.
- Save camp/receiver definitions, place them, and connect them with roads. Put the
  camp wagon on unobstructed dry ground, within 24m of its road access. It returns to
  that authored position. Leaving a clear exit avoids the need for off-road detours.

Lumberjacks credit one unit of storedTimber on successful felling. Assigned wagons
wait for a full load (default four units), handle loading/unloading (default four
seconds each), choose the shortest reachable explicitly marked Lumber Mill, and
fall back to an explicitly marked Town Center if no mill is reachable. Mill roles are explicit; the Harbor Council House defaults to Town Center. A stopped wagon shows a timber load
while carrying cargo. Existing horse animation, hitch articulation, and wheel motion
are shared with traffic. Lumber wagons were removed from automatic ambient spawning
(spawnEvery 0); other ambient traffic is unchanged.

FreightRoadGraph splits crossings and T/overlapping junctions, projects access onto
road segments, and finds a shortest connected road path. The receiver stop is on the
road within half the lot diagonal plus 16m of its center; wagons do not drive through
receiver buildings. Return routes include the final camp access segment. Dry-ground,
grade, building/fence and trunk checks reject blocked routes. Established routes are retained; only encountered blockers or changed/missing destinations
permit replanning. Cached round trips are reused for later deliveries. Only roads/lots presented in the open district,
including combined districts, participate. Background/off-district logistics and
traffic right-of-way are not implemented.

DistrictLotRecord stores timber stock, received timber and per-object vehicle job
state (phase, cargo, receiver ID, position). Loading and unloading transfer stock
atomically. A reconstructed actor resumes its saved cargo/position without taking a
second load or crediting an unloading twice. Claims, routes and handling timers are
transient; timers restart after loading a region. Failed routing keeps stock/cargo.
Counts and job records are validated. Older lots default to no vehicle script, no
receiver role and zero stock. Existing character behavior IDs remain stable.

Freight inventory is an initial lot-level ledger. Timber collection/carry animations,
processing into lumber, construction resource spending, and nationwide inventory
integration remain separate work. The runtime implements reusable opt-in behaviors;
assignments live in saved lot/object data.

Run CityForge.Editor.FreightChecks.Run in the isolated QA checkout. It includes
LumberjackChecks plus road topology, explicit mill preference/town fallback, missing
receiver behavior, invalid data, stock accounting, script/role serialization, saved
in-flight cargo, and deterministic ticks of the actual local wagon on a synthetic flat
district. No player map or lot library is saved by these checks. Live district visual
acceptance is still required for modal layout, cargo placement and real terrain turns.

## Earlier script library and shared turn presentation

The earlier Vehicle Scripts picker used a searchable, category-filtered library with a
separate selected-script description/settings pane. Config/VehicleScripts.json holds
names, categories, summaries, descriptions, compatibility requirements and settings
panel IDs. Registry entries expose implemented behaviors; no placeholder jobs are
shown. The saved behavior enum remains stable.

VehicleCornering adds look-ahead steering, braking toward tighter turns, gradual
acceleration, and bounded horse yaw. Ambient vehicles update each rendered frame at
close zoom instead of the previous 10Hz at zoom2. Freight movement consumes its full
distance budget across curve samples; route refreshes do not reset movement speed.
Existing trailer articulation and axle-relative wheel roll remain shared.

The lumber-wagon profile sets teamHorseHeight to 2.3m. Prepared teams normalize the
visible skinned vertices using bone/bind-pose transforms, center horse and wagon
laterally, seat their visible bases, and keep fitting outside animated FBX roots.
Normalized team prefabs preserve their authored metre scale/hitch rather than being
refitted by animated renderer bounds at runtime. Geometry-version labels let local
catalog preparation rebuild old private Team.prefabs without committing artwork.

VehicleMotionChecks.Run verifies runtime horse scale/seating, center alignment,
corner braking/acceleration, smooth heading rate, and the freight regression checks.
VehicleMotionChecks.Render writes isolated side/front previews under Library.

## Eagle sidewalk pickup

Eagle pickup treats explicit worn-sidewalk overlays, the outer2m of ordinary lot
land bounds, and road-side margins as open approach space. Segment samples must all
remain in those bands to bypass building renderer envelopes. Interior building
collision checks remain active. The final approach no longer clamps a sidewalk
passenger to the road centerline, and roof look-ahead stops at the pickup target.
VehicleMotionChecks covers a sidewalk pickup inside an oversized building envelope
and an interior target that must remain blocked. Live eagle pickup remains a visual
acceptance check.

## Configurable behavior — November18

The vehicle editor now configures What / When / Where instead of selecting a rigid
named job. Supplied private artwork from Downloads/Vehicle Script UI is loaded as
four compressed, non-readable, non-mipmapped textures, capped at1024px. Sprite
subrectangles reuse the supplied icon/button sheets; no concept screenshot is used
as the actual UI. What uses category radio buttons with Resource/Emergency/Event
dropdowns. Resource includes all existing site/required resources plus manufactured
Lumber and Bricks. Emergency and Event currently contain None and do not execute.
When offers Morning, Noon, Afternoon, Evening and Night. Where currently offers
Town Center only. Capacity, load/unload handling time, and Active remain authorable.

New configured resource behaviors use saved selected-resource stock, never regional
resource abundance as free cargo. Existing timber stock fields remain authoritative
for Timber; additional resources have validated per-lot stored/received amounts.
Resources with no producing gameplay currently wait for actual stock; this change
does not simulate new farms, mines or resource production. Cargo retains its resource
identity through save/reload. A once-per-selected-period departure flag prevents
repeat dispatch and persists with the job; observing another period rearms it.
Existing unconfigured LumberWagon saves retain their previous continuous operation
and mill-preferred routing until edited. The saved enum value1 is preserved; new
configurable ResourceDelivery is value2. Configured destinations use only explicitly
marked Town Center lots, including an assigned Harbor Council lot.

The shared actor is now LotResourceVehicle. Parked actors poll twice per second.
Routes are computed when no reusable route exists or after an encountered obstruction
or changed/missing destination. An unrelated road-revision change does not discard them. Reusable water-access
state avoids per-step allocations. This is a small state machine and scalar schedule
gate, with no planning/behavior-tree engine. Changed local sections are checked before entry; unchanged certified travel does not
repeat route-clearance probes. See Established_Actor_Routes.md.

VehicleBehaviorChecks.Run includes selected-resource ledger and schedule gates plus
an actual scripted Fish delivery at Noon, no departure in Morning, no repeat in the
same period, and rearming after a phase change. It also runs the existing freight,
vehicle geometry/cornering, and sidewalk collision regression checks.
VehicleBehaviorPreview.Render captures all three actual editor pages and the open
resource dropdown under Library/VehicleBehaviorPreview without saving player lots.

## Industrial ambient traffic and selected-lot demolition

Industrial lots no longer participate in RebuildAmbientTraffic. Previously every
building object counted as a traffic source: a four-tent camp produced four ambient
pedestrians and qualified for a covered wagon (spawnEvery4). Open ambient routes
ended with the normal disappear/restart behavior, which was unrelated to freight.
Placed/scripted camp actors and delivery vehicles remain authored on the lot.
Reentering a district or rebuilding ambient traffic clears previously spawned extras.

The selected-lot panel includes a red Detonate button. It clears any old demolition
stroke and sends only the selected lot through the existing demolition commit path;
roads and neighboring lots are excluded. Selection closes before the lot views are
rebuilt. Save/loading, remake and active lot-placement guards prevent invalid actions.

## Harbor Council Town Center default

LotDefinition.DeliveryRole resolves the Harbor Council House to TownCenter when no
receiver role has been configured. This applies to old saved/placed lots and new lots
without rewriting player maps. Routing, unloading and both role UIs use the resolved
role. Manual selections set deliveryRoleConfigured, including an explicit None, so
player overrides remain authoritative. Existing non-None roles are preserved. The
selected-lot panel shows Receives: Town Center for an unconfigured Harbor Council.

VehicleBehaviorChecks covers the default, saved-lot reload, ordinary-lot exclusion,
explicit override and the actual scheduled delivery runner targeting a default
Harbor Council receiver on the synthetic QA road network.

## Shared empty-to-full cargo progress

All resource-delivery wagons now load incrementally from actual lot stock, including
before their departure window. A partially loaded wagon waits; a full wagon departs
at its configured time when a destination/road route is available. Unloading removes
cargo incrementally and credits the receiver once per transferred unit. Loading and
Unloading append phase IDs3/4; existing saved Waiting/Outbound/Returning IDs remain
unchanged. Cargo resource, amount and fractional handling progress persist through
normal saves. A changed resource returns pending home cargo to its source stock
before switching, without silently changing its identity or losing units.

No extra script options are required. Load size and Load/unload seconds govern the
same behavior for every selected resource. VehicleCargo.json maps resource appearance
to logs, boards, blocks, rocks or crates. Shared cached meshes/materials provide up
to12 fill stages in one cargo renderer, updated only when the amount changes. No
cargo physics, per-frame mesh rebuilding, or per-resource behavior scripts are used.
Covered/enclosed vehicles preserve their cover/cabin; their counts and loading state
are visible in the selected-lot panel. Open carts display the progressive load.

Assigned vehicles also preview empty in the Lot Editor. The runtime reuses that cargo
view and restores its saved amount, avoiding duplicate cargo renderers. Private empty
body meshes remove the fixed load from the lumber/food wagon templates while leaving
source FBXs unchanged. Profile cargo bounds and protected driver bounds control this
one-time asset preparation. Prepare local catalog invokes Prepare delivery cargo;
generated EmptyBody assets remain private with the licensed source models.

CargoProgressChecks.Run checks all13 resource IDs: partial loading, full-only dispatch,
stock conservation, partial unloading, save/reload, no duplicate credit, empty/quarter/
full views and the existing scheduled delivery regressions. CargoProgressChecks.Render
writes isolated Timber/Stone progression captures for both open wagon models. Tests
never write normal player maps or lot-library files.

## Extraction placement capture

Industrial / Extraction lots require their selected resource within an 80m sphere.
Requirements & Bonus exposes Capture resource (Timber, Stone, Iron, Coal, Clay, Fish).
Older definitions use their first extraction resource requirement, or Timber by default.
The saved selection is independent of wagon cargo assignments.

Timber requires ten actual harvestable trees in the active district, outside the lot
footprint that placement clears. Grass, felled trees, and trees beyond the sphere
are excluded. Regional timber abundance is not a forest designation. Other resources
use resource surface layers or discovered deposits. The preview is translucent green
when eligible and red otherwise, with “You must be within reach of the resource.”
New placement and moves recheck before clearing vegetation or structures. Existing
lots are not demolished when their surroundings become depleted.

Queries reuse the timber spatial index and cache results until position, resource,
forest revision, discovery count, or season changes. The sphere and ground ring are
reused and hidden on cancel. Forest generation and rendering remain unchanged.
Run CityForge.Editor.ExtractionCaptureChecks.Run in the isolated QA checkout for
tree thresholds, exclusions, sphere height, saved resource selection, deposit
visibility, preview colors, cleanup, and the freight/cargo regression suite.

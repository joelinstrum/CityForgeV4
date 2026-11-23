# Harbor immigration and regional events

Definitions live in `Assets/CityForge/Resources/Config/RegionEvents.json`. The original
`immigrant-first-wave` ID is retained for save compatibility; its display name is
now **Harbor immigration**. `maxOccurrences:0` means ongoing arrivals without a
three-trip limit. Each confirmed eligible galleon arrival creates12 representatives
at10 people each and credits120 actual people to the town. Positive occurrence
limits remain supported for future finite events. Inclusive district-year eligibility,
source building, character pool, destination categories and notice text are also JSON.
The implemented circumstance is `galleon-docked`; future date-only/other triggers
need dispatch adapters. Definitions and code are shared source; art remains private.

`HarborVesselVisits` dispatches only after reaching a valid berth. Failed approaches,
barges and pirates do not count. The original boat navigation, schedules, gangways
and dock workers remain independent of immigration. The source must contain the
`harbor-council-house` asset. Moving preserves the placed lot ID and its progress;
deleting it cancels further arrivals from that council. A replacement gets a new
identity. Already-arrived people continue even if the council is removed.

The top-left courthouse **Game** icon opens mode and immigration controls. New and legacy
regions default to Running. Pause stops future population deliveries without consuming
an arrival, stopping ships, stopping landed people, or queuing missed arrivals.
Resume permits the next actual docking. `SavedRegionInfo.immigrationPaused` persists
per region and is retained during a region remake. Menu descriptions read the JSON
multiplier/count. Player is the default, including legacy saves without mode metadata. The menu switches
to Developer for unrestricted population/arrival growth and back to Player.
`SavedRegionInfo.developerMode` persists per region. Existing buildings are retained.

Progress is stored in `SavedRegionInfo.events`, keyed by event and source lot ID.
Occurrence counts, town population, total arrived population and settled population
use64-bit counters. Each pending representative records its represented population,
asset, original district, position, saved shore contact and assigned destination lot.
Old individual records without a population weight retain their original one-person
value; existing town population is not retroactively multiplied. Old councils at
three arrivals resume from their next docking under the updated unlimited definition.

People spawn on dry ground connected to the council's landward contact. A confirmed
galleon docking performs one destination discovery and assigns the entire waiting
cohort before walking starts. Whole residential parcels are exclusive reservations:
one visible representative (normally10 people) per available home or unbuilt parcel.
Anchored4×4 zoning counts as one home, not16 destinations. Auto paint is partitioned
into fitting authored layout footprints once during discovery. Existing residential
reservations and archived occupied homes exclude that parcel from new assignments.
Commercial buildings/areas accept overflow as temporary refuge; without commercial
space people wait at the harbor. Commercial refuge does not count as housing.

Saved destination IDs/cells/points and awaitingHousing preserve reservations through
reload. Walking and bounded route retries consume that assigned destination only;
they never search for alternative homes. Destination discovery is event-driven:
docking, one initial load, zoning/road/terrain changes, and building placement/movement/
deletion. Mutations coalesce while the zoning pause is held. No periodic vacant-site
cache refresh, occupancy polling, timed reassignment or eight-second growth retries
remain. New housing can therefore accommodate existing harbor/commercial waiters
without waiting for another ship. UnhousedPopulation includes people en route and
waiting for construction, as well as commercial/harbor waiters; available and reserved
residential parcel counts are shown in Game. Count updates read the event ledger.

In Player, a representative must physically reach its reserved vacant parcel before
construction begins. Binding transfers the reservation to the new lot ID. After the
construction reveal the resident proceeds immediately to the clear entry and fades
out over1.2seconds; the former additional10-second readiness hold is removed. Homes
remain empty at zero population. Developer bypasses growth demand. The starter shop
gate is one shop per120 actually housed people, excluding temporary commercial refuge.
This is an initial growth/household model, not a full housing-capacity or jobs economy.

Dijkstra retains the shortest connected street/road/sidewalk path; safe ground access
and bounded four-neighbor routing are fallbacks. Planning is budgeted and resumable,
with memoized clearance checks. Route completion/obstruction retries are movement
work, not destination discovery. Entered residential groups compact into per-lot
population and settled totals; commercial groups remain in the pending housing
ledger after visual arrival. Demolished occupied homes return their associated
population to the waiting ledger. Shared character art/materials remain unchanged.

Ongoing arrivals do not accumulate unlimited bodies or individual records. Each
source retains at most96 waiting representative slots (plus transient fading bodies);
further arrivals add population weight to waiting groups. At most96 event avatars
are presented in the active district at once. Completed individual records are
removed after their fade and replaced by aggregate totals. Population continues
to grow when visuals are grouped. The notice always reports actual new people.

Verification: `CityForge.Editor.RegionEventChecks.Run` covers JSON/year eligibility,
galleon filtering,120-person/12-representative accounting,104 consecutive arrivals,
pause/resume controls, old-save defaults, actual binary save/reopen, bounded pending
records without population loss, lot movement and deletion, exclusive residential reservations/commercial overflow, category/wealth filtering, shortest street routing and moved-in persistence.
Local runtime fixtures additionally exercise the actual Gameplay menu, paused-region
capture/reopen, representative walking/fading and240-person ledger compaction.
`PlayerGrowthChecks.Run` additionally checks Player defaults, physical arrival gates,
one-use parcel credit, Developer bypass, saved mode metadata, construction-hold
release, zoning metadata classification and domestic animal R1-yard obstruction.
Domesticated wildlife may occupy low-density residential yards; actual buildings,
fences, roads, water and other safety checks still block placement. Wild animals
retain the whole developed-lot exclusion.
Tests run in the isolated QA project with private character/building assets, never
in normal player saves. Existing pedestrian traffic checks also remain applicable.

Performance: idle councils perform no destination discovery. Catalog rebuilds are
explicitly invalidated by mutations/docking, and use a revision-aware spatial street
index instead of per-cell road-triangle tests. Full construction site/frontage
validation remains authoritative. Player growth excludes unrelated parcels early;
pending diagonal growth runs behind the scheduler. Road/ground planning retains its
8ms slices and bounded memoization; simulation freezes throughout zoning authoring.

Government & Policies: the Game icon opens the responsive dashboard defined in
GovernmentDashboard.cs. Overview and Immigration expose the existing pause/resume
and housing ledger; Game Mode exposes saved Player/Developer choices; Events reads
saved council aggregates. Unimplemented finance/policy sections are labelled unavailable
and never display example values from the supplied visual reference. State setters
refresh live labels; no UI-driven world/occupancy polling is added. Art stays private.

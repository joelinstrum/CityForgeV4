# Road shape stamps and45° snapping — November26,2026

Lowercase r/d/t/x/s/i stamps road shapes; Shift enlarges and Tab rotates90°.
Works from road selection and existing-road drags; continuation hints retain keys.
Freehand roads infer45° headings with a rotated4m grid. No save schema changes.
See Documentation/03_Technical/Road_Shapes_Handoff.md for controls, implementation,
validation commands, scope and safe continuation alongside the townhouse installation.
Isolated PR branch compiled and passed 2,478 native checks; actual previews inspected.
Source-only scoped checkpoint; other sessions' private art/catalogs are excluded.

# Session checkpoint and next bridge prototype — November23,2026

Read Documentation/03_Technical/Bridge_Assembly_Handoff.md for the consolidated
installed-feature summary, private asset paths, verification and agreed next task.
Bridges are transport structures, not lots: versioned JSON style definitions plus
saved bridge instances and an in-game drag/release designer. First prototype is a
plain-shape trestle with deck, railings, braced supports, inclined approaches and
confirm-only earthwork. No bridge implementation or artwork started yet. Prioritize
road/elevated route connectivity, real bed supports, water clearance and save/reload.
User asked to commit this checkpoint and continue in a new chat. Source-only commit;
private art/models/saves stay local. Do not interpret earlier Play discard approval
as permission to interrupt a future unsaved session.

# Building gallery era filter — November23,2026

Removed unused-tracking filter from shared galleries. Building gallery now seeds
its era/category from the current lot, resetting on lot/era/category context changes,
while retaining manual filter choices for unchanged context. Blank era defaults All.
Top shows LOT ERA as context; FILTER BY ERA dropdown sits below Favorites. First-era
metadata is never changed by browsing. Art Deco City Center remains visible in a
fresh modern civic lot. Seven native UI checks passed for default visibility, earlier
and all-era filters, same-lot filter retention, lot switching and removed checkbox.
Screenshot: be/outputs/Building-Era-Filter. Installed after explicit session-discard
approval. No saved lots changed.

# Civic lots define town centers — November23,2026

Supersedes generated Art Deco/timber starter-lot shortcuts. Founding and Civics
menus now select saved library lots only. CivicBuildingChoices.IsTownCenter uses
Civics category plus effective TownCenter delivery role, without a building asset
ID. FoundingLots also retains unconfigured legacy Founder category lots; explicit
None overrides it. Existing generated centers keep their saved explicit role.
No auto TownCenter role from the Art Deco model in LotDefinition; legacy Harbor
Council fallback remains for compatibility. Art Deco model remains in Buildings.
Lot Editor Lot role… opens the role/delivery modal; TownCenter selection enabled
only for Civics. Saved lot layout, first era and resource checks remain authoritative.
Town-center era filtering permits its first era and later; ordinary civic filtering
unchanged. Empty libraries prompt authoring and receive no generated fallback.
14 native Unity checks passed: model-only/explicit role/category rules, save/reload,
legacy/None compatibility, empty library, generic era rules and actual role UI.
Evidence: be/outputs/Civic-Lot-Rules. No user saved lots or placed centers changed.

# Art Deco pedestrians — November23,2026

ArtDecoCharacterPreparation imports8 private humanoid characters from the user's
art-deco-npcs ZIPs. Retained originals: SourceAssets/ArtDecoCharacters. Models and
controllers: Resources/Lots/ArtDecoCharacters. Black/tan men had no takes; use
Humanoid-retargeted brown-suit Idle/Walk clips. All8 avatars/loops/bone motion
verified in isolated native Unity; gallery screenshot inspected. Characters are
modern/1921–1945, player-placeable, group Art Deco Era.
Config/ArtDecoCharacters.json is source-only import metadata. PedestrianProfiles.json
owns era pools, zone counts and walking cadence: modern R2=2,R3=3,C2=4,C3=6 per
building, all wealth, .75 walking fraction/40s cycle. Spawn phases are staggered.
DistrictPlacedActors selects the effective district/lot appearance era; legacy eras
retain their pools when no profile exists. R1/C1 don't get this Art Deco set. Ambient
activity advances by active simulation cadence and leaves blocked/no-route actors
stationary; manual characters, freight, immigrant routes and native biomes unchanged.
Existing ambient instances survive unrelated refreshes. Modern commercial spawns
are2× residential at matching density. Primary compile4.63s, all8 registered.
Evidence: be/outputs/ArtDeco-Characters. No commit/push performed.

# Art Deco welcome and city trees — November23,2026

New-region welcome resolves CreationSettings.era by ID/name, then period name,
then Founder fallback. Modern era text remains in ContentCatalog.json; harbor art
replaces private ModernEraIllustration.png, using the same welcome layout.
Acknowledgments remain era:<id>. CityTreePreparation registers8 private seasonal
entries city-tree-01..04-{bare,planter}; source was a material-grouped12-tree grid
(four sizes × summer/fall/winter). Split meshes retain UVs and authored winter
branches, with reduced distant tiers in prefab LODGroups. Planter variants retain
concrete/mulch/iron; bare variants omit them. StreetTreePalette adds small/medium
bare and planter presets; HasPlanter suppresses the extra soil pit. No native biome
changes. All missing bark/planter textures recovered from OBJ texture archive in
the full collection. Source FBX/Blender/texture archives retained privately under
SourceAssets/CityTrees; Downloads files are not runtime dependencies.
216 isolated Unity checks passed including actual street planting, gallery seasons,
serialization, Zoom2/3 transitions and era welcome/acknowledgments. Primary compile
2.04s, all8 entries registered. Evidence in be/outputs/ArtDeco-Welcome-City-Trees.

# Lower Manhattan1609 import — November22,2026

Default private prebuilt preset is now Manhattan1609/Manhattan_1609_CityForge.json:
12×12km,2048² U16, source -40…80m/sea0, +41m game offset. Original supplied JSON and
PNG files remain unchanged; adapted schema maps fields and retains original metadata.
TerrainImportSettings.sourceMetadataJson additionally persists the complete JSON;
old saves remain compatible. The existing wizard uses a flat worker-built texture;
no separate3D preview. Actual districts retain native Terrain/Crest. Repair OFF.

Visual testing exposed far-zoom imported ocean coverage: reuse CoastalWaterClock's
ground focus, actual viewer for rides, and imported ScaleRange max(32,Side/32).
Do not change procedural oceans to fix imports. No source heights/masks or approved
water materials changed. Zero source shoreline conflicts. Native sample max error
0.26683m across625 test points; source-free native menu reopen/cache round trips and
v3/v13 temperate/tropical procedural regressions passed. Screenshots inspected.

New native region installed separately as "Lower Manhattan1609 — schematic"; existing
saves and former private Manhattan preset retained. Dataset/preset zip/native save and
verification are private outputs under the be chat's outputs/Manhattan-1609. No merge
or push performed. Temporary integration harnesses live under work/manhattan1609,
not in Assets. Read Documentation/03_Technical/Terrain_Import.md for limitations.
This is a schematic technical test, not geographically verified1609 reconstruction.

# Save District stays in place — November22,2026

Save District must preserve the current district, camera and world. HUD disk now
has an explicit SAVE DISTRICT label, an additional save-only click target. Save
and Back have no automatic UI navigation; saving clears selected HUD focus.
ShowOverview is blocked during saving and for .35seconds after completion to keep
the save release from triggering immediate Back navigation. Ordinary saves retain
the existing async writer. The save coroutine accepts a writer delegate so QA can
exercise native writes without normal saves. DistrictSaveStayChecks.Check covers
raycasts for disk/label/Back, capture/write/read with paths, camera/district identity,
focus cleanup, navigation guards and restored controls. Passed in isolated Play
mode on Unity6000.1.12f1; only disposable temporary save written. User's log showed
a Back pointer click, but the exact triggering user gesture was not reproduced.

# Lenient path creation / Enter commits — November22,2026

Pedestrian paths use ParkPathSiteProblem, not RoadSiteProblem. Existing building
clearance boxes, porches, props, fences and lot footprints must not veto a ground
path. Validate the sampled centerline for dry land and active-district containment;
solid models naturally cover paving beneath them. Creation and appearance editing
share this validator. Preserve ordinary road/building safety rules separately.
Player paths render at .09m above terrain and above authored lot-path render queue,
so lot overlays do not hide an accepted path. Enter/KeypadEnter finishes park paths
even with the pointer over HUD. In the Lot Editor Enter/Finish commits using the
last-used available material (default residential footpath); one-marker completion
retains its sketch. Existing edit/material options and undo remain.
LenientParkPathChecks.Run covers the old building veto, actual PointWire completion,
visible mesh/layer, serialization/undo, wet/outside-district rejection and actual
Lot Editor Finish handler. Isolated QA passed; rendered fixture inspected. No normal
saves changed.

# Approved warm night illumination — November22,2026

Preserve the user-approved23h warm building/street wash throughout night, including
midnight: NightSunIntensity .85, NightSunColor (1,.84,.66), ambient level1/white.
Previously SunriseProgress and the dawn ambient noon blend incorrectly treated
22–24h as completed sunrise/noon, then snapped cold at midnight. Night now explicitly
uses that approved illumination; dawn calculations exclude evening hours. Keep dark
sky, lamp/window schedules and daytime lighting. WarmNightLightingChecks.Run checks
22h/23h/midnight/2h/5h preset AND live district interpolation, dawn and noon. Passed
in isolated QA with Unity6000.1.12f1. No saved-map schema or private art changes.

# Sidewalk zoning — November22,2026

Manual zoning allows curb-straddling grid cells whose centers are outside paved
roads. Do not reject the whole selection because an outer cell clips pavement;
sidewalks and off-grid curbs must remain usable. Residential/commercial zoning
frontage accepts this partial-cell alignment. Road-centered cells remain blocked.
Actual lot/building road clearance is unchanged. SidewalkZoningChecks.Run covers
4m/8m road-edge cells, frontage and pavement protection; prepared-ground, event,
Player and interaction regressions passed in isolated QA. This changes designation,
not the building-clearance or growth-siting rules.

# Repeated zoning stress test — November22,2026

A24-block test reproduces progressive click latency:8x3 blocks (96 parcels),12 R2
medium-wealth/cobblestone then12 R1 low-wealth/dirt. Before the fix, clicks8/10/24
cost406/482/1316ms; earlier road objects were replaced on every later placement.

CutPlanes must tolerate one native normalized terrain-height storage step (1/32766).
Requested13m and sampled13.00128m previously caused false dirty/sync reports even
when the stored heightmap was unchanged; this triggered whole-road rebuilds. The
PreparedGroundChecks regression now requires Finish=false for unchanged flat grades,
in addition to exact before/after height samples and retained undo baselines.

CommitNewRoadPieces calls RefreshRoadShoulderDetails(deferSidewalks:true). Queue and
coalesce sidewalk repairs instead of synchronously rebuilding all sidewalks/recess
on each click. BuildRoadRecess is a finite row-sliced job (~4ms CPU budget), retaining
the old texture until its replacement is complete and rebinding road/junction materials.
Loading/real terrain-change paths retain the synchronous wrapper. A canceled paced
job must finish its matching recess field on restart even when masks were already
cached; do not gate that recovery solely on roadMasksChanged. Existing road objects
survive flat-ground placement. No periodic gameplay scans were introduced.
Final24-block run: click8/10/24 now21.5/25.4/68.2ms (last19.3× faster).
All surviving road object IDs retained; all1050625 height samples unchanged; zoning
pause retained. Final queued sidewalk job drained; recess covers every road and all
road materials bind the finished texture. Final-drain max rendered frame33ms. First
post-placement frame still had a411ms cold-start delay; do not claim every frame is
under70ms. This tests96 unbuilt parcels, not96 inhabited homes. Prepared-ground,
event, Player growth and selective deletion checks all passed. Outputs in this chat:
Zone-Stress-Test/{Results.txt,Before.csv,Timings.csv,24-Blocks.png,Deferred-Repair.txt}.

# Flat-ground zone placement work — November22,2026

User reports2–3s on clicking the ground to commit an8×3 block (not stamp selection).
Isolated small fixture: full legal R2 commit88.99ms, prepare5.11ms, creating roads38.98ms,
CommitBlockGroundPlane40.06ms, lamps0.93ms, growth queue0.30ms, overlay1.62ms.
The full reported stall is NOT reproduced by this fixture; do not claim it solved.

ApplyZoneGroundPlanes now returns whether actual terrain samples changed. When none
changed, CommitBlockGroundPlane retains existing road presentation, ground paint and
park paths instead of destroying/rebuilding the whole district. New road ribbons alone
are resampled against the committed datum. CommitNewRoadPieces already presented roads
and queued sidewalk repairs. Real height changes retain the existing refresh path.
Post-change legal commit80.12ms; the1050625-sample comparison still has zero changes.
This is a limited redundant-work fix, not proof of the cause of the full2–3s pause.

# Preserve prepared ground during zoning — November22,2026

DistrictLotGrading.CutPlanes blends new zone banks from CURRENT terrain samples,
not the original undo baseline. The old path could resurrect pre-terraform contours
in a new block's feather. Keep original[] for reversal only. Same nature-stroke
start/union behavior remains. CommitBlockGroundPlane detects an already flat current
platform and uses that actual height rather than a stale overlapping plane's datum.
Existing nonflat adjoining rules and the legacy water safety floor remain.
PreparedGroundChecks exercises rectangular/diagonal banks over land normalized to
sea+1 with a different retained native baseline: all16641 samples must stay exactly
unchanged, and Restore must still recover the native heights. Do not substitute a
check of plane.height for a comparison of actual surrounding terrain samples.
Full isolated runtime CommitZonedBlock passed with medium-wealth R2 8×3 parcels:
all1050625 terrain samples unchanged, zero height delta after actual placement.
PreparedGroundChecks plus event/Player/deletion regressions passed. Native Terrain
height quantization is retained; flat target uses the actual sampled platform level.
Primary Play session and saves untouched; restart Play then re-level affected ground
with Lots & Zones to remove previously authored ripples before fresh placement.

# Lots & Zones leveling preset — November22,2026

Terraforming defaults to Lots & Zones (appended enum value4, preserving legacy mode
values). Its fixed datum is SeaLevel +1m +Offset: zero adjustment is labelled Ideal
for lots and zones. The centered slider has an Ideal marker, live water-clearance
caption and Reset to ideal. Selecting legacy modes retains their relative behavior.
The fixed preset skips terrain statistics and remains consistent across strokes.
Existing prebuilt block grading extends the prepared plane; keep its adjoining-plane
rule and legacy sea+.2m safety floor. No saved terrain or old leveling targets are
raised automatically. Tropical water datums use the same relative clearance.
Isolated runtime QA passed exact target/offset, rectangular and diagonal prebuilt
block grading at the prepared height, centered slider/live label/reset and UI render.

# Government & Policies dashboard — November22,2026

The courthouse Game icon now opens a1600×900 responsive Government & Policies modal
using the user's supplied dashboard panorama and two icon/illustration atlases.
Native Unity UI builds all text, cards and controls; artwork is private/ignored in
Resources/UI/Gameplay/Government/{Dashboard,Icons,Illustrations}.png. Originals were
copied unchanged from Downloads; normalized UV crops avoid import-size dependence.
Preload art/font with HUD loading; no synchronous image imports on the modal click.

Overview has the six policy cards, live immigration toggle, Game Mode switch, actual
population/unhoused/available/reserved counts, and Policies/Events tabs. Sidebar adds
a dedicated Game Mode page with explicit Player/Developer choices and active styling.
Choices use existing setters/save flags; no new mode semantics or save schema.
Immigration has its existing running/paused control and real JSON arrival/housing
figures. Events displays saved per-council arrival and housed totals with scrolling.
Treasury/Taxes/Labor & Rights/Trade/Public Services are labelled Not yet available:
no invented budgets, tax rates, happiness, wealth or enabled labor policies from the
reference painting. Future mechanics require implementation before enabling settings.

GovernmentModalFit responds to viewport dimension changes, not polling. Close/Escape
restores camera state. HUD/stat labels refresh on actual state changes. Existing
simulation behavior behind Game remains; zoning's separate all-freeze lease persists.
Private art is not shared through Git; copy the ignored folder through the normal
private-asset workflow for other Unity instances. Source-only fallback stays usable.

Isolated UI QA passed overview/detail immigration toggles, explicit Player/Developer
choices/active states, actual SavedRegionStore.Capture settings, real saved event
history, all eight sidebar destinations, compact1000×650 fit, close/camera restoration
and reopening. Modal creation39.84ms with preloaded art. Existing event/Player checks
passed. Final screenshots and Verification.txt are in this chat's Government-Dashboard
outputs. Primary session and normal saves untouched. One Play restart loads this UI
and the preceding Harbor placement fixes together.

# Incremental Harbor Council placement — November22,2026

CommitLotPlacement no longer calls RebuildDistrictLots for new or moved lots. Add only
the new lot through PresentDistrictLotBatch(...,deferSidewalks:true); moving removes
and replaces only its own view while preserving record/event identity. Clear replaced
lot views selectively as well. Queue shared sidewalk repair; only newly created lot
surfaces need deferred street-mask application. Preserve all neighboring root IDs,
materials, grading and live actors. Register town-center state and harbor traffic
explicitly since these previously depended on the full rebuild's side effects.

HarborVesselVisits initializes its three vessel profiles using Resources.LoadAsync,
then starts the unchanged visit/docking loop. Do not restore synchronous ship prefab
loads inside placement. Disable stops the initialization/visit coroutine; enable
resumes missing profiles without duplicates. Zoning pause is respected. Actual
confirmed berth arrival remains the only immigration trigger.

ClearDevelopedSites rebuilds district ground paint only when touched paint changed.
ApplyLotGrading skips snow-ground refresh when no terrain cut changed; actual cuts
update the affected snow/grid bounds rather than rebuilding the district-wide snow
surface. Native materials and shoreline appearance are retained.

Isolated legal-waterfront runtime: Harbor Council placement35.84ms, move/recommit
24.44ms; all24 preexisting lot roots kept exact IDs and the council retained its own
record ID on move. All3 ship profiles loaded asynchronously in0.18s while editor
updates continued. Screenshot/verification outputs are under Harbor-Placement in
this chat. Main Play and normal player saves were untouched.

# Responsive demolition and cached grid — November22,2026

Selected-lot and rectangle demolition now remove affected views in place instead of
RebuildDistrictLots destroying/reinstantiating every survivor. Preserve root IDs,
materials, live neighboring actors and foundation data. Remove owned building/eagle/
tree/platform/worker registrations, population holds and residential ground indices;
queue housing updates. Restore standalone cuts locally while protecting surviving
lots, roads and shared zone/terraform grades. Shared block grades survive home deletion.
ResidentialGroundBatches.Remove compacts triangle indices once, not RemoveRange per
triangle. Area clears avoid unrelated road and lot rebuilds; explicit actor clears
still rebuild their actor presentation where needed.

Deletion queues one coalesced, paced sidewalk rebuild. Keep old sidewalks visible
until the replacement is ready. Reuse unchanged pavement masks and leave other lot
surface masks / park paths / road recess unchanged. Road edits and teardown cancel
queued work safely and dispose old assets. Navigation gets a completion notification.

District grid toggles reuse completed geometry. Initial preparation and dirty rebuilds
are finite event-driven coroutine jobs with4ms CPU slices, never a blocking full-
district rebuild inside a toggle or hover callback. Shared grid-vertex heights cache
terrain samples; actual lot/ground/district changes invalidate them, including feather
bounds and edge samples. Preview changes coalesce; hidden previews only dirty ready
geometry. First preparation finishes under the district loading overlay through
PrepareDistrictGridStages, so even the first toggle does no terrain sampling. Preserve parcel interior clipping, diagonal edges and combined-district gaps.

Isolated24-home runtime:1000 cached toggles11.67ms total, selected deletion18.87ms,
one-parcel area deletion15.86ms. All24 neighboring lot roots retained exact instance
IDs. Queued grid/sidewalk repairs completed; screenshots/Verification.txt in this
chat's Interaction-Performance outputs. Initial grid preparation is included in district loading; QA verified only0.1ms
additional wait before the first interaction. Dirty rebuilds remain time-sliced.
InteractionPerformanceChecks checks local grading restoration, protected neighbors
and shared grades, plus event/Player regressions. Main session/player saves untouched.

# Event-driven immigration / whole-parcel reservations — November22,2026

Docking discovers destinations once and reserves one whole residential parcel per
representative household (normally10 actual people). Authored4×4 parcels count once,
not once per painted cell. Auto paint is partitioned into fitting layout footprints.
Occupied and reserved homes are excluded globally across source councils. Saved
assignment IDs/cells/points are retained while route planning/walking; no rediscovery
or destination reshuffling in route retries. Existing safety and shortest-road rules
remain. Blocked pedestrians explicitly queue a retry to the same assigned lot.

Overflow uses commercial lots/zones as temporary refuge, otherwise waits at the harbor.
Commercial arrivals retain awaitingHousing and stay in the persistent pending ledger
after fading; they do not count as housed or create residential/shop demand. New housing
can receive existing harbor/commercial waiters when that actual edit updates the ledger.
Unhoused includes reserved/en-route/construction groups until physical home entry.
Game shows unhoused population and available/reserved residential parcel counts.
Occupied-home demolition returns its associated population to waiting, preserving totals.

Removed timed vacancy refreshes, periodic destination validation/reassignment and8-second
growth retries. MarkImmigrationSitesChanged coalesces real zoning/road/terrain/building
mutations (deferred by zoning pause); one initial load and docking also rebuild the
snapshot. Once all queued route work is done, nextEventTick is infinity until a new
event. Route retry timers schedule movement work only. Construction completion wakes
assigned residents immediately; the redundant10-second post-construction hold is gone.
The zoning all-freeze contract and manual pause restoration remain intact.

Runtime QA passed24 unique reservations/two boats,24 homes/240 people moved in, and a
third120-person boatload staying unhoused at the harbor when no space/refuge existed.
100 routing ticks after assignment and100 harbor-wait ticks performed zero destination
scans. Save/reopen retained Player mode and120 unhoused people. Rule checks cover
commercial overflow, pending commercial settlement and saved awaitingHousing. Test
neighborhood uses connected streets on a cleared pad; tree/water/slope safety remains
mandatory. Screenshots and Runtime-Verification.txt are in this chat's
Immigration-Assignments outputs. Primary session and normal saves untouched.

# Zoning pauses gameplay — November22,2026

Opening Zones acquires an event-driven pause lease. Time.timeScale0 plus explicit
clock/event/growth/pedestrian/worker/wildlife/NPC guards stops background gameplay.
Enabled world animators and playing particles are snapshotted once at entry and
paused; gameplay audio pauses. No per-frame scene discovery. Camera, UI, authoring
and rendering remain usable. Hiding the menu to paint/place does NOT release pause.
Repeated prebuilt stamps retain it after commit while the next stamp stays armed.
Cancelling the last stamp with no menu visible, closing Zones or leaving the section
releases it. ArmZone/ArmRectangularZonedBlock also acquire pause explicitly.
Restore exact prior time scale/audio state and only animation/particle states owned
by the lease. Previously disabled/paused effects remain so. OnDisable/OnDestroy
release the lease. Manual clockPaused is never overwritten. Do not persist the
transient editing pause in a save.

Simulation deadlines in immigration, growth, lot readiness and construction dust
use scaled Time.time, preventing timers expiring behind zoning and catch-up after
resume. UI preview throttles keep real time. Ship route search/sailing/docking yields
while paused. Leave camera-dependent render/LOD updates running for authoring.
Crest presentation and native terrain appearance are unchanged.

Isolated actual-menu QA passed frozen simulation time, clock, scaled coroutine,
animators, particles and audio; committing a stamp while armed; manual zone brush;
cancelling/closing and restoring prior0.75 speed; preserved manual clock pause and
previously disabled/paused effects; view destruction restoring global state.
Screenshots and Verification.txt are in this chat's Zoning-Pause outputs. Primary
session, normal saves and other Unity instance were untouched. Restart Play to load.

# Harbor / Player-growth performance correction — November22,2026

A council with no unsettled immigrants used to rebuild destination sites every2s,
including per-cell road-triangle frontage checks. Skip idle councils entirely.
Vacant goals now use cached spatial street segments, cheap street-side points and a
bounded256-result cache (revision invalidation/10s refresh); construction retains
full authoritative site/frontage validation. Avoid CaptureTownZones sorting/record
allocation in discovery. Pending diagonal home growth is behind the scheduler, not
every frame; Player candidates are filtered by actual arrivals before layout searches.

Road search supports resumable Search.Step() while preserving the synchronous
TryRoute API and shortest-path rules. Immigrant planning uses8ms slices, retained
road-search state and bounded8192-entry-per-live-person clearance memoization in2m
sections. Short-budget failures retry quickly; genuinely blocked paths back off.
Preserve target preference across interrupted attempts and invalidate cached work on
road/lot/presentation changes and QueueLotGrowth. Do not replace this with a hard
restart timeout: QA proved that stranded all passengers on some valid paths.

Final isolated acceptance passed existing event/Player rules, resumable graph checks,
zero-population/physical-arrival construction gates and all120 represented residents
moving in. Measured100 idle council ticks8.83ms total with zero site rebuilds;
16,384-cell initial discovery20.09ms;100 cache reads1.13ms total. These are subsystem
benchmarks, not measured FPS for the user's affected capital. The affected capital
state has not yet been identified for an exact reproduction. Primary Play and normal saves were untouched.
Machine showed roughly15GB swap in use; concurrent editor memory may compound stalls.
See this chat's Player-Growth/Runtime-Verification.txt and Region_Events.md.

# Player growth and residential domestic animals — November22,2026

Player is now the default for new/legacy regions without mode metadata. The private
courthouse Game icon opens saved per-region Player/Developer and immigration controls.
Developer bypasses population/arrival growth gates; Player keeps zero-population
private zoning empty. Existing buildings remain. Low-wealth settlers can target
vacant road-served residential zoning, physically arrive, unlock one home parcel,
wait for construction and enter/fade. Pending diagonal templates follow the same gate.
Commercial starter demand is one shop per120 moved-in people; this is not a full
housing/employment economy or demand-meter implementation. Save developerMode and
pendingPlayerLots through capture/open/remake; preserve event arrival flags/cells.

Fixed actual zoned-home category/wealth detection, fragile construction model lookup
and unreleased population holds. Street-side goals avoid trapping residents inside
future homes; blocked plots retry and can fall back to eligible completed homes.
Domesticated species may occupy R1 yards while actual buildings/fences/roads/water
remain blocked; wild animals retain whole-lot exclusion. Private icon/farm art stays
ignored. See Region_Events.md and PlayerGrowthChecks.Run.

Isolated runtime QA passed zero-population empty zoning, no construction on harbor
arrival alone, physical arrival -> construction -> all120 people moved in, actual
cow placement in a newly built R1 yard, UI mode switches and binary Player save.
Main Play session and normal player saves were untouched. Screenshots are in this
chat's Player-Growth outputs. Save and restart primary Play to load these changes.

# Ongoing representative immigration — November22,2026

Harbor immigration now continues indefinitely while enabled:12 visible immigrants
represent120 actual people per galleon. JSON uses populationPerRepresentative10 and
maxOccurrences0 (unlimited), retaining the legacy event ID. Gameplay control in the
top-left pauses/resumes immigration per region; the flag is saved and carried through
remakes. Pausing does not stop ships or people ashore and does not queue missed visits.
Counts/population use64-bit values. Existing population is never retroactively scaled.

To support indefinite arrivals, completed representatives compact into settled and
per-lot population totals; crowded waiting groups acquire additional population
weight rather than creating unlimited actors. Pending slots and live presentation
are bounded at96; total population remains exact. Existing moving-in, random balanced
lot assignment, shortest street routes and source deletion/movement rules remain.

Focused isolated checks passed104 consecutive arrivals (12,480 people), gameplay
pause/resume, saved pause metadata, legacy defaults, pending-ledger bounds and all
previous event/move-in regressions. Running Greenbank QA also verified the actual
Gameplay toggle, saved pause state, resume without catch-up, and two boatloads:
24 representatives moved in and compacted into240 people with no leftover avatar
records. The final Gameplay menu screenshots are in Ongoing-Immigration outputs.
Source/JSON remain separate from private art;
normal player saves and the primary Play session are untouched. See the current
Documentation/03_Technical/Region_Events.md and this chat's Ongoing-Immigration outputs.

# Immigrant move-in presentation — November22,2026

Immigrants now choose actual developed low-wealth residential/commercial lots,
balance reservations across them and randomize nearby alternatives. Empty zones
keep arrivals waiting until buildings exist. Destination lot IDs persist, and the
shortest usable connected street/road/sidewalk route is selected before ground
fallback. They reach0.8m outside the building facade, revalidate the target, then
fade out over1.2seconds using private material clones and leave the live pedestrian
list. The town population and settled event record remain; district reload and
save/reopen do not respawn moved-in bodies. Deleted/moved/ineligible destinations
are reconsidered; deleting the source still only cancels future waves.

Expanded RegionEventChecks covers balanced12-to-three-lot allocation, vacant-zone
waiting, residential/commercial and wealth filtering, shortest connected street
routing, deleted destination rejection and moved-in/assigned-lot save persistence.
Runtime QA checks all four avatars fade, source materials remain opaque and all12
bodies are removed while settlement/population records remain. Test captures and
harnesses are outside Git; main Play and normal saves remain untouched.

# Immigrant first wave — November22,2026

Added JSON regional-event definitions and saved per-lot event progress. Confirmed
galleon docking at a Harbor Council triggers12 low-wealth arrivals for its first
three visits. Cargo barges, pirates and unsuccessful approaches do not count.
Moving preserves progress; demolition cancels remaining waves. Replacements get
new event identities. Arrived passengers and town population persist across saves;
passengers wait for reachable low-wealth residential/commercial zoning and follow
safe one-way routes, then stop. A14-second named-town arrival notice informs the
player without a modal. Event configuration includes inclusive year eligibility.
No general calendar trigger is implemented yet; future triggers use dispatch adapters.

Isolated Unity compilation, low-wealth pedestrian regression checks and the focused
JSON/event/binary-save/navigation lifecycle checks passed. A running Greenbank
fixture also confirmed all twelve avatars complete their one-way low-wealth-zone
trips and stop. Spawn points remain connected to the saved landing contact; bounded
ground navigation can detour around a council without roads. Private art and player
saves are unchanged. Main Play session remains open; next Play restart loads source.
See Documentation/03_Technical/Region_Events.md for configuration, behavior and checks.

# Town Center recovery — November22,2026

Added a persistent “Add Town Center” action below the district toolbar whenever
an established town lacks its center. The capital reopens the original Founder’s
Building picker (including saved Harbor Council choices); secondary towns open
Civic Lots. “Choose later” leaves recovery available. Missing capital founders
are detected from actual placed lots as well as the legacy initial-founding flag,
so already-saved deletions recover without changing the save schema. Replacement
placement hides the button. Deletion does not force an automatic modal; initial
founding retains its existing one-time prompt. Existing resource/era/asset and
placement validation still apply. Recovery closes conflicting construction tools.

Isolated Unity QA compiled the modified source and passed11 state/UI checks:
defer/reopen, legacy completed-flag deletion, no forced replacement popup,
placed/remove visibility and secondary-town civic reopen/replacement. Tests used
a disposable snapshot and wrote no normal player save. Main Unity was observed
running the Augusta overview; its active Play session was left untouched. Source
changes load on the next Play session. Local test harness/captures remain outside Git.

# Terrain shoreline review checkpoint — November22,2026

Source branch: feature/terrain-shoreline-polish, based on main after merged PR28.
This follow-up contains the completed manual terrain refresh, shoreline/material
polish, new-region clearance and continuous-estuary work documented below. Unity
compilation and the isolated regression/render checks passed. Private assets,
normal player saves, builds and local QA captures remain outside Git. Historical
"no commit/push" notes below describe the earlier validation checkpoints.

# Continuous estuary seafloor — November22,2026

Final main verification: tested source matches QA; Unity compile exited0 and domain reload completed outside Play. Focused native/graded floor, bounded repaint, binary reopen and full Nature regression checks pass. Height arrays unchanged; no player save written.

User screenshot exposed a straight underwater wedge and stair-stepped green patch
where the native river meets the bay. Reproduced the same join from saved Greenbank.
The river-only coastal-distance sentinel(-4*Side) skipped native bed painting, while
bounded Nature refreshes painted sand. Clearer water exposed that material boundary
as an apparent depth jump. This was actual grass/base terrain paint on a submerged
bed, not the retired moist-grass overlay.

BuildCoastalBeachDistances now keeps a normal negative distance for ALL native wet
cells instead of suppressing river cells. Preserve river-edge seed exclusion so
native upstream banks do not get a broad coastal beach. Native shared-sea-level
river/ocean floors receive one continuous sand palette. Player channels remain
excluded from this native path.

Graded rivers blend coastal sand back into the existing native upstream bed over
12–96m landward of the analytic coastline. No hard river/ocean footprint boundary.
Initial paint and repaint use the same floor-weight rule. Under sculpted water,
blend toward sand by TerraformGroundBlend; do NOT use the whole rectangular pending
area/connected-water footprint to override graded-bed paint (that recreates seams).
Existing bathymetry and terrain targets remain untouched; no extra geometry,
textures, draw passes or renderer objects.

Before:71,251 wet samples,52,688 lacked coastal floor. After:zero missing floor.
Graded palette continuity tested over1,180 probes; fresh binary save/reopen retains
floor weights. Matched mouth/close renders reviewed. Height arrays are byte-identical
before/after. Full Nature regression passes: all sculpt modes, dry isolated pit, connected
ocean, raised/lowered shore, repeat update, combined scope and saved heights. No player save written.

Bounded repaint test preserves graded floor weight0.932 outside the sculpted
mask; the pending rectangle cannot overwrite it. Fresh reopen and all regression
checks pass. Evidence: November22 ple/outputs/Continuous-Estuary. One guarded
source synced; no player save written; no commit/push.

# Patchy shore materials, damp sand and clearer shallows — November22,2026

Main project verification: all seven source/shader files match tested QA; Unity compilation exited0 and domain reload completed outside Play. No shader errors reported in the latest reload. Full Nature regression, GPU wet/dry comparison, height identity and save/reopen checks pass.

User supplied a concept and explicitly distinguished its six swatches from usable
seamless assets. Priorities: irregular grass/sand coverage, darker wet sand driven
by waterline distance, readable sandy shallows, very subtle foam, and no added
shore geometry/objects. Do not import/crop the concept or recreate its swatches.

This supersedes the earlier constant-width/fully uniform MATERIAL-bank rule.
Geometry retains the tested monotonic Nature bank curves; only coverage is patchy.
CoastalBeachSurface combines broad width variation with remapped7/21m noise for
sand/grass patches. Source sand, dirt and detailed grass remain. Owned coastal sand tiling is5m
to retain readable grain from above; the source asset is not changed.
Existing base-layer weights still drive sparse-grass transitions. Lower dirt mass
keeps dry sand readable rather than making the entire bank a soil ribbon.

TerrainGeography reuses existing control1 BA (previously spare) for signed distance
encoded as clamp(.5+d/64) and sand coverage. RG remain normalized soil/rock weights;
BA must NEVER be included in terrain material-weight normalization. The existing
first control alpha remains reserved zero; retired moist grass stays retired.
The shared shader shades a narrow shore-distance damp zone with a muted darker
sand color, retaining the source grain and restrained smoothness. Base terrain
reuses its control read; add/basemap passes use one existing-map sample so sand
in later terrain passes receives the treatment. Sampler belongs to texture1 so
Metal does not optimize away the paired texture in add passes.

Owned coastal water material scales absorption AND scattering by the same factor
(.42 temperate/.70 tropical), keeping their deep-color ratio while revealing the
sandy bed in shallows. Minimum transparency alpha.015. Existing bathymetry/depth
input, waves, reflections, levels and special harbor inputs remain authoritative.
Foam intensity.18, global crest foam strength.06 and shoreline.035; existing ocean
outline input strength.22 (harbor swash.10). Calm river/lake renderer routing keeps
its existing foam-free profiles. No new shoreline meshes, objects, textures,
materials or draw passes; two spare control channels and one add-pass map read.

First Metal compile exposed an optimized sampler mismatch in the hidden add pass;
fixed by pairing its sampler with the actually sampled second control texture.
Checks must include hidden terrain add-pass shaders, not just parent material.

Final isolated checks: seven source/shader files; all loaded CityForge terrain and
hidden add shaders compile without errors. GPU wet-vs-dry comparison darkens21,826
pixels by>8RGB total.637 control texels carry near-shore data; no extra texture
allocation. Repeat update byte-identical. Fresh binary save/reopen controls match
after draining their normal deferred chunk refresh. Full Nature regression passes.
Terrain heights are byte-identical to the saved-bank baseline. Renders reviewed in
orthographic and lower views. Evidence: November22 ple/outputs/Shore-Material-Pass.
No player save written; no commit/push.

# Smooth sculpted bank curves — November22,2026

Main verification: both source files match tested QA; Unity compile exited0 and domain reload completed outside Play. Full Nature regression and saved-bank render checks pass; no player save written.

User rejected repeated W-shaped indentations in the shoreline. A replay of the
current saved Greenbank (district19,68 saved grading planes) reproduced the
short-wavelength bank fluctuations. Nature LevelerEdgeBlend now uses a single
monotonic smoothstep across the existing feather; its world-noise reach/detail
perturbations were making small repeated notches. Non-Nature/lot grading retains
its original behavior. Flat cores/targets, stroke metadata and water levels remain.

The edited material band is a constant6m transition, and native beach width varies
only at the broad100m scale. Remove the5m boundary jitter and11m width noise;
texture grain/approved dirt/sand/default-grass assets are retained. These changes
apply when saved edits replay on district opening; no resculpting or save rewrite.

Matched-camera before/after renders of the actual saved bank reviewed. All29
latest-stroke core centers remain unchanged within1mm. Full Nature regression passes (all modes, isolated pit, connected river/ocean,
raise/lower, save/reopen, repeat Update Terrain); final top-view replay capture reviewed. Work isolated in reserved terrain QA project.

# Shore blend and new-region bank clearance — November22,2026

Final: six main source files match tested QA; Unity compilation exited0 and reload completed outside Play. Full Nature regression, new-region binary persistence, both climates, native bank clearance and rendered shore checks pass. No player save written. Git diff --check passed.

User approved the river fix and requested softer grass/sand transitions, textured
banks, and1m land clearance for NEW REGIONS ONLY (explicit clarification).
Existing saved heights and explicit Nature targets are not raised.

TerraformShore bilinearly interpolates its signed grid instead of returning a
single floor-cell distance. Edited dry banks retain a narrow5–7m transition that
mixes existing dirt, sand and detailed default grass. Native beaches start their
fade at the shore rather than carrying a fully sandy inner plateau. Dirt coverage
uses the approved shared dirt layer, not retired moist grass. The owned coastal
sand normal scale is.65. No new art, source texture or water-material changes.

Default recipes are now version13. Coast composition starts dry land at sea+1m
(sea12 temperate /30 tropical). New native river records persist bankClearanceMeters1.
Fixed-level channel shoulders use that clearance; ground-following channels place
their water1m under the original ground. Rounded estuary dry banks retain the same
clearance. Missing/zero body metadata retains legacy.08m fixed/.20m graded rules.
Recipe versions1–12, stored heightfields, manual level offsets and player-created
waterways retain their original behavior. Tropical shelf and mountain forms remain.

Isolated Greenbank test found1239 positions blending dirt/sand/grass and checked
5cm continuity steps. New coast checks sampled194,934 dry points in each climate;
all at least1m above water. Fixed and graded channel checks pass, and an actual
MapMagic River&Bay with native water clearance survives binary save/load.

Full Nature regression passes: all modes, connected native water, isolated inland pit, raised/lowered ocean, combined scope, explicit button deferral, repeat-update idempotency, save/fresh reopen.

# Greenbank river shoreline refresh — November 22, 2026

Final verification: focused Greenbank river, full Nature regression, raised-river texture checks and visual captures passed. Main Unity compilation exited 0; domain reload completed outside Play. Main source matches QA; git diff --check passed. Player save unchanged.

The user's Lowest Point edit exposed a broad rectangular sand shelf at the native
tidal river after Update Terrain. Reproduced on a disposable copy of their region,
Greenbank district19 near(-408,-1344): edited bed about2m at sea12m, but the
shore field returned +1024 (no connected water), and the Crest mask hid the river.
This was a separate defect from the retired wet-grass material.

TerrainCoastEdits originally seeded its shared Crest connectivity only from saved
ocean polygons. Include native non-player river footprints in both original-cell
seeds and untouched-neighbor seeds. This preserves the actual river/bay network;
connectivity remains required, so unconnected low pits are not automatically flooded.
Water renderer/material settings, source geometry and terrain targets are unchanged.
Saved Nature edits replay through the corrected rule, so no resculpting/migration is
required. Player-channel/lake renderers remain separate and are not seed sources.

Greenbank Lowest Point fixture now has negative shore distance and physically wet
connected river across the cut. Before/after captures reviewed; save/fresh reopen
and repeat-update clipping pass. Full Nature regression and raised-river checks pass: detailed default grass, no beach or physical water on raised Greenbank ground; ocean cuts/raises, isolated low pit, all sculpt modes, district scope and save/reopen remain correct.

Final main-project verification: all seven runtime/shader files match the tested QA copies; Unity compilation exited 0 and domain reload completed. Git whitespace check passed.

# Smooth wet grass retired globally — November 22, 2026

User explicitly rejected smooth moist grass everywhere, including bright-green
river margins. This supersedes the earlier scoped overlay transfer below.
TerrainGeography no longer generates the moist slot, applies its forest
attenuation or indexes freshwater for a44m halo. Coverage normalizes across the
existing meadow/dry/woodland/soil/rock surfaces. Native lake/channel invalidation
remains, with obsolete RefreshWater calls removed.

The shared geography shader never samples the wet surface. It folds any legacy
geography-alpha wet weight into detailed meadow before normalization. Palette
slot3 remains reserved (and is skipped when binding) so local assets keep their
indices. This is NOT terrain layer3, which remains authored leaf litter.
Water geometry, levels, actual wetness, bank paint and canonical ecology remain.
The prior per-edit geographic donor transfer/warming is removed; base terrain
texture restoration retains its existing canonical mixture/fallback. AGENTS.md
records the user preference so this material cannot return as a fallback.

Isolated untouched Augusta:66,049 uploaded control samples and16,641 independent
positions all have zero wet weight and normalized active surfaces; four native
rivers and one ocean retained. GPU legacy100%-wet vs meadow controls differ by
~.49RGB byte levels on the ground sample, confirming shader redirection; shader
reports no errors. Native shore and edited close/wide captures inspected.
Save/fresh reopen/repeated-update material checks and full Nature regression
pass. Evidence: November22 ple/outputs/Wet-Grass-Retired. Seven guarded runtime/
shader files synced. Normal player save unchanged. No commit/push requested.

# Grass overlay band — November 22, 2026

This supersedes the earlier claim that the remaining green band was lighting.
Isolated layer/plant/geography/rebind renders identified the geographic material
overlay: old freshwater geometry gave the dry edited surface up to 86% smooth
moist-grass coverage, overriding otherwise correct default terrain textures.
Correct alphamap weights alone did not fix that overlay.

TerrainGeography now separates native classification from edited classification.
Edited grass extends the complete native material mixture from unmodified dry
grass reference sites (not moist grass or forest floor), retaining real exposed
rock on steep edits and the existing edge feather. Native terrain outside edits
uses its original classification. Original texture assets/tiling, ocean settings
and canonical water records remain unchanged. Base terrain layer ratios remain
world-aligned. A capped 257-square reference lookup builds during explicit
Update Terrain/startup replay; source eligibility reads native inputs directly
so it cannot depend on stale uploaded textures. Lookup invalidates after edits.

Augusta repro passes a semantic check rejecting wet/forest-dominated overlays
on fully edited dry ground, original base-layer ratios, fresh binary save/load,
and identical overlay samples before save, after reopening and after repeated
Update Terrain. Rendered at two scales and inspected. Repeat explicit update
measured ~949ms in this isolated Editor fixture; not a brush-stamp or FPS figure.
The lookup is warmed inside the explicit operation, not a later streaming frame.
All-mode Nature, connected ocean/inland pit, original seabed raise/lower, combined
district scope, repeat clip idempotency and save/reopen regressions pass.
Evidence: November22 ple/outputs/Grass-Overlay. Three guarded scripts synced;
normal player save unchanged. Main reload compiled successfully outside Play at
MainMenu; all three source hashes match QA and git diff --check passes.
No commit/push requested.

# Completed terrain texture continuity follow-up — November 22, 2026

The nearest-source transfer documented below still extruded a narrow reference
strip into a broad band. Final coastal repaint first recovers the actual grass
weights retained underneath the canonical beach/soil, at the SAME world position.
Normalize the original approved grass/coarse channels after removing old beach
soil; preserve their original two-dimensional spatial pattern. Nearby dry
meadow is only a fallback for samples with no grass mass, using a 2D reflected
swath instead of extruding the nearest row. No palette, texture, UV/tiling or
material edits. Geography avoids duplicate canopy queries on untouched ground.

Coast clip samples encode the terrain's sea crossing and retain a one-cell dry
overlap under the shoreline, matching native harbor practice so the terrain
intersection defines the edge. Physical wetness still uses actual sea level and
connected-ocean cells. Ground-cover refresh is bounded to the affected area.
The Level operation still preserves its flat authored core/target; lighting
can differ from unlevelled slopes. Native terrain-grid precision is unchanged.

Iterated Augusta renders reviewed at two scales, plus matched-height and
neutral-light diagnostic captures. Canonical grass ratios compare per sample
within .015; secondary/coarse layers persist after fresh reopening. Final full
Nature regression checks actual button deferral, all modes, ocean-connected and
isolated cuts, original seabed raise/lower, combined scope, low green land,
repeat-update idempotency and binary save/fresh reopen. Evidence: November22
ple/outputs/Terrain-Final. Player save unchanged. Three guarded sources synced;
main reload compiled successfully outside Play at MainMenu. All three source
hashes match tested QA and git diff --check passes. No commit/push requested.

# Default texture continuity on edited coasts — November 22, 2026

User rejected the single-green fill from the earlier low-plateau repair. The
coastal painter now extends the actual nearby canonical dry-ground layer
mixture, including both approved grass layers and the fifth coarse layer. A
lazy two-pass nearest-source field uses eligible dry meadow outside the old
beach; existing texture references, tiling, grading and world UVs remain.
TerrainGeography uses the same source ecology/canopy on edited ground, with the
existing edge feather. No new palette/art/shader or canonical geography changes.

Isolated Augusta low-sea+20cm shelf QA compares every base-layer weight against
its canonical source (within .015), checks secondary grass and coarse layers
after actual binary save/fresh reopening, and verifies coast updates stay
deferred until the real Update Terrain button. Inspected final render shows
default detail throughout the extended ground. Evidence: November22
ple/outputs/Default-Grass. Two guarded scripts synced; normal save unchanged.
Main reload compiled successfully at MainMenu; both hashes match tested QA
and git diff --check passed.
No commit/push requested.

# Low coastal plateau texture repair — November 22, 2026

User's Augusta screenshot exposed a gap in the previous Update Terrain QA: high
raised seabed passed, but low dry land retained the canonical beach's heavy tan
base soil even after its appended sand was removed. Native beach rules also
kept painting sand near the new shore. Edited dry terrain now restores the local
meadow/grass mix through the brush and banks, feathering over the outer 8m.
Sparse ground-blend cells build during explicit Update Terrain/startup replay,
not during drag. Untouched beaches and submerged sand retain their normal rules.
Water level input clears old raised levels over edited dry ground as well as wet
cells. Stranded old shoreline plants hide locally. No canonical geography, ocean
material/datum, native forest art or save schema changes.

Read-only Augusta copy reproduced the tan shelf; matched before/after images
reviewed. Full Nature regression passes deferred actual button dispatch, all
modes, low sea+20cm green ground, original seabed raise/lower, isolated pit,
combined-district scope and save/fresh reopen with matching green ground and
zero old water level offset. Evidence: November22 ple/outputs/Shore-Repair.
Five guarded source files synced; player save hash unchanged. Main reload
compiled successfully outside Play at MainMenu; five source hashes match QA
and git diff --check passes. No commit/push.

# Manual Update Terrain — November 22, 2026

Nature → Terrain now has Update Terrain beside Sculpt Terrain. Coast water and
sand wait for this explicit refresh across strokes/cancel; height edits and
destructive clearing remain immediate. Original seabed raised above sea now
clips ocean correctly instead of retaining its original zero mask. Beach/harbor
weights repaint from original ground using edited shore distances and heights.
Startup replay still refreshes automatically; existing saved Nature edits are
included by the button. Regional relief/resource geography remains unchanged.

Isolated Unity QA passed actual button dispatch, deferred clip through release
and cancel, raised ORIGINAL seabed water exclusion and sand removal, lowered
seabed water/sand restoration, all Nature modes, ocean-connected vs inland cuts,
combined-district scope, save/fresh reopen. Modal and final shoreline renders
inspected. Evidence: November22 ple/outputs/Terrain-Update. No player saves or
private assets modified. Main script reload passed compilation and stayed outside Play at its menu.
All seven source hashes match tested QA; git diff --check passes.
Changes local; no commit/push requested.

# Source checkpoint — November 22, 2026

Current delivery branch: `feature/terrain-and-city-editing`, based on the source
checkpoint merged in PR #27. This checkpoint includes the accumulated November
20–22 window lighting, building/lot/path tools, civic/character/tree integration,
soundscape, camera/daylight, zoning/road clearance, grading and Nature terraforming
work documented below. The repository remains public and source-only; licensed
art, audio, generated catalogs, local lots and player saves are excluded.

Latest validation: main Unity reload compiled cleanly outside Play; Nature coastal
wet/dry edits and fresh-save replay, all four level modes, raise/lower/smooth,
combined-district scope and stream clearing passed isolated QA. The destructive
City leveler regression with 129 saved planes passed at median 28.765ms per stamp
(Editor timings, not FPS). Evidence lives in the November20 ple chat outputs,
especially Nature-Terraforming and Leveler-Destructive. Detailed earlier feature
checks and local asset requirements remain in the chronological notes below.

Known limit: edited coastline updates district terrain, ocean clipping, levels and
navigation; the regional overview and canonical resource survey retain original
coarse geography. Whole road/path records touched by destructive leveling are
intentionally removed. Keep the approved tree-review choices in AGENTS.md.

# First city soundscape — November 21, 2026

Imported eight supplied MP3s privately under Resources/Audio/CitySounds, preserving source hashes. Central DistrictSoundscape starts with the region camera; LotRuntimeLoader adds passive character/transport emitter tags. Twelve pooled voices total: four shared ambience layers plus eight nearby moving actors. Walking variants split across pedestrians; other moving horses/riders/covered wagons/buggies use horse-and-wagon; open food/lumber cargo carts use wooden-wagon. Children-playing follows visible09:00–15:00 children. Built commercial lots trigger commercial-area; empty zones do not. Native/player streams and rivers trigger stream-longer by indexed centerline distance, excluding lakes/oceans. Effects are zoom0/ride only; nature plays across gameplay zooms/overview and gains exactly2× over canopy clusters. View-center ground distance/pan, gradual gain/pan changes, independent clip phases and existing master volume/mute preserved. No per-actor Update or AudioSource; canonical forest data is read/indexed without presentation changes.

QA passed real Unity playback/capture,501 registered emitters/12-source cap, mappings, movement/distance/zoom/ride/school/mute gates, built/empty commerce, stream types and forest2×.500-moving-emitter control benchmark median.177ms/p95.189ms per scan+fade, zero managed allocations across300 iterations; not a whole-city FPS/DSP claim. Three-second captured mix peak.258/RMS.034. Evidence November20 ple/outputs/City-Sounds; source/import details in City_Soundscape.md. Clips and seven scripts synced outside Play; main compiled without errors and confirms all eight clips, correct mono/stereo/sample-rate/load settings and twelve-voice cap. No QA harness/catalog or player saves copied; licensed/private audio remains ignored. No commit/push requested.

# Schedules, sunset, growth and pedestrians — November 21, 2026

Children at play now shows children09:00–15:00 (end exclusive); hidden models pause while the schedule driver stays active to restore them next morning. Central CharacterScriptHours supports all-day/overnight ranges; other scripts retain existing behavior, and Lot Editor children stay visible for authoring. Sunset now transitions blue→gold17:45→orange18:45→orange-red19:30→purple20:30→navy22:00; sunrise unchanged. Zoning building fit now shares placement's5% per-edge allowance for every zone/wealth, preserving strict waterfront/other-building guards and existing site/frontage/paving checks. Before/after saved-library QA recovered47 candidates (37 medium R1,1 low R1,1 low R2,8 low C1). Exact parcel/layout matching remains required. Founders medium residential/commercial ambient pools share the four high-wealth adult appearances plus dock worker; other eras share their high adult, with low/industrial population policy retained.

QA passed schedule boundaries/hide/restore/pause, every zone/wealth fit boundary, other-building and waterfront guards, all residential/commercial medium pools, and saved college-block geometry/frontage/pavement/occupied-lot regressions. Sky renders inspected. Read-only layout CSV and verification in November20 ple/outputs/Schedules-Sunset-Growth. Main source and shader synced outside Play and compiled without errors; main confirms09:00 on/15:00 off, five medium-wealth adults and shader support. No artwork/catalog/player-save writes, commits or pushes. See Children_At_Play.md, Zoning_Parcels.md, Camera_Ride_And_Dawn.md and Pedestrian_Wealth_Pools.md.

# Schoolyard children — November 21, 2026

Four child characters prepared and registered from Downloads/children as private V3Characters/Child* assets. Children at play is appended as behavior7; new children default to it, and it is selectable in Character Script → Scripted. Random available animations, no root-motion/route wandering, guaranteed cumulative >=50% idle. One-hour-per-child QA observed58.4–60.2% idle and passed animation/bone movement, reduced skinned LODs, scale, zoom2→3→2, default/explicit behavior and JSON persistence. Direct district children stay separate from pedestrian routes. Near/distant/play renders inspected. Original1938/1940/1904/1916-triangle models reduce to1006/1008/990/996; original bind poses and unit normalization fix FBX skinning. See Children_At_Play.md; evidence November20 ple/outputs/Children.

Files synced to main while the user's city was running in Play Mode, with a temporary one-shot reload deferral. The user then clicked the Play button to leave Play; the callback released the lock normally. Main scripts compiled without errors and all four entries were merged into the main local catalog in place. No reload lock remains; no QA catalog copy or player-save writes. No commits/pushes.

# Camera, dawn and city ride — November 21, 2026

Ride speed follow-up: accelerator capped at7mph (3.12928m/s), with mph in the overlay. Added Command+D/Control+D ground-level ride; arrows accelerate/brake/steer, Esc restores previous camera. Safe nearest-street start, transient save-safe state, terrain/water/occupied-lot/union guards, substepped movement and controls/speed overlay. Low12° tilt now lifts for terrain clearance instead of dollying back. Clock accelerates21:00–06:00 (45 real seconds), with exact boundary carry. Continuous violet dusk/navy sky and05:00–08:00 sun/ambient sunrise remove the06:00 lighting cut. Seventeen purchased source leaf materials permanently use forest-green RGB(.32,.40,.29); warm/dry/autumn leaves, bark and approved mixed forest palette unchanged. Isolated targeted QA and near/far tree/sky renders passed; main synced locally, no saves/commits/pushes. See Camera_Ride_And_Dawn.md and November20 ple/outputs/Camera-Daylight.

# Current checkpoint — November 20, 2026

Commercial college-block follow-up: ordinary Cobblestone Road and Brick Road now support commercial frontage (same paving/sidewalk eligibility), removing the misleading Road/Street restriction. Both vacant8x4 strips at(-152,596)/(-184,596) in the latest Guilford district35 save passed with original roads, actual saved water and lot footprints; pavement/occupied-lot guards still reject. All29 commercial models reviewed;27 glazed models have rebuilt143 sash grids plus optional night-only glazing meshes (1,338 quads total) to cover scanned glass folds. Two open-front workshop maps are retained. NightWindowGlass shader/optional catalog field and ResidentialWindowLights drive the glazing only at evening/night; normalized source bounds keep footprint edits and rotated/scaled/elevated lots aligned. Original116 near/far/color/normal files and player region SHA256 are unchanged. Fresh QA renders/phase/LOD/lantern/placement tests passed; main compiled and confirms27 meshes, all commercial masks, shader support and both masonry frontage IDs. Source/private assets synced locally with catalog merged in place. See Commercial_Window_Emission.md and Zoning_Parcels.md; evidence November20 ple/outputs/Commercial-Windows. No commit/push/merge requested.

Purchased animated trees: user approved syncing during Play (script reload allowed). All30 supplied tree shapes are prepared as private animated placeable trees and static forest variants, with summer/fall/bare-winter deciduous variants and reduced 3D LODs. New Maple1/2 replace silver-maple/silver-maple-b; Oak1/2 replace oak-pilot-a/oak-pilot-b. Legacy broadleaf forest indices are preserved and route to new static maple/oak models; approved mixed chestnut/locust/larch, poplars, ecology and saved transforms remain unchanged. Authored palette bypasses old maple tint/scale/base hacks. QA passed animation sampling, materials, reduced LODs, seasons, saved transforms and forest aliases; actual near/far render pixels align. Source and prepared private assets synced locally, catalog merged without copying QA catalog; main Unity confirms all30 entries and eight new static forest aliases, with existing catalog IDs retained. See Purchased_Animated_Trees.md and November20 ple/outputs/Animated-Trees. No player save writes or commit/push/merge requested.

Additional pavement: six supplied Downloads/textures albedos (macadam gravel, mossy fieldstone, herringbone brick, irregular flagstone, red brick, rustic wood) are private shared Paving materials and optional 4m Founders Streets at every wealth level, with commercial frontage and shared street/junction classification. Existing IDs/defaults/saves are preserved. Street picker expanded to four columns/three rows with fitted captions. Isolated imports, meshes/frontage, IDs and Founders/later-era menu renders passed; see Additional_Pavement_Textures.md and November20 ple/outputs/New-Pavement.

Additional civic imports: Academy/Schoolhouse/College → Education, Courthouse → Government, Infirmary → Healthcare, all Founder-era Civics with unspecified wealth and no growth pool. Five private Model/Distant FBX pairs preserve supplied color/normal artwork and metallic/roughness data, have centered ground pivots and doorway-estimated uniform sizes, and use original Zoom0–2 / reduced Zoom3–5. Selected panes use clean sash-aligned emission; no modeled exterior lamps were found. Isolated NewCivicsQA passed classification, Building Properties compatibility, bounds/pivots, LOD return switching, day/evening/night, cardinal fit, fixture save/load and civic-browser routing. Four-corner and near/far night renders were reviewed; source and assets were synced locally. No player saves or premade playable civic lots were created. See `Documentation/03_Technical/Civic_Building_Imports.md`; evidence: November20 `ple/outputs/Civics-New`. Art remains ignored/private; no commit/push/merge requested.

Building Properties follow-up: implemented a catalog-default editor using the Civic/Ploppable modal artwork and typography. Selected buildings expose Properties; the browser's edit mode opens the same form. Name/description, era, category/use, wealth, growable type and default scale persist atomically in separate local overrides; generated catalogs are session-cloned, not overwritten. Existing entrance/dimension and row tools remain available and share the styling. New manual/single/row placements use the default scale, with scaled fit/entrance/row markers; saved object scales and lot classifications are retained. Isolated persistence, growth/row and UI checks passed, including 1920/1366 visual captures. See `Documentation/03_Technical/Building_Properties_Editor.md`; evidence is in November20 `ple/outputs/Building-Properties`. Source was synced locally; main runtime assembly reloaded and the active properties form passed a smoke check. Main was returned to its main menu in Play Mode without saving a lot or override. No commit/push/merge requested for this follow-up.

Local R3 lighting follow-up: all sixteen supplied medium-wealth R3 buildings now have pane-aligned private window masks, replacing brightness-threshold patches and skipped-UV holes. Stone Dutch and Stone House also have conservative replacement distant meshes preserving their complete front geometry/UVs; the other fourteen distant meshes are unchanged. Isolated Unity checks passed registrations, dimensions/pivots, reduced-mesh presence, bidirectional LOD switching and noon/evening/night lighting; front/oblique night views and distant views were inspected. Masks and two FBXs were synced locally without editing player saves. This artwork remains ignored/private and is not contained in the existing commit or PR. See `Documentation/03_Technical/R3_Window_Emission.md`; evidence is in the November20 `ple/outputs/R3-Window-Grid` folder.

Branch: `feature/district-authoring-and-waterways`, based on the merged R2 import/Texturize checkpoint (PR #26). This checkpoint includes the November18–20 district authoring, civic/NPC, growable-row and water work. The repository remains source-only; local private artwork, generated catalogs, Unity caches and player saves are not part of the PR.

Latest behavior: Texturize owns world input, excludes building footprints and supports feathered Boundary/Free spray modes. Parks → Paths shares lot path materials, widths and presets and adds preferred pedestrian routes with persistent district path records. Rotated rectangular C2 parcels recover correct frontage dimensions on restore, undeveloped reservations can be restamped, C2 medium-wealth block streets default to cobblestone, and growth failures are visible. Day sky is softer blue with an orange/red sunset around19:00. Tiny player-stream stone meshes are replaced with pebble bed texture; fixed200m fixture near dressing801,714→409,752 triangles (-48.9%), not a whole-district FPS claim.

Other included work: deterministic/curved street connections and surface masks, six-inch visual road recess, area demolition, combined zoning-parcel grid cells, Change Lot, growable building rows and the redesigned layout dialog, civic placement browser, constable Patrol/crime escort, farmer field work, low-wealth rats, water features and width variation, stream connections/cascades/waterfalls, improved water glints, authored elevation carry-through/mill wheel, clocks/scripts/HUD/toolbars and content registration for private civic/R3/C2 imports. Detailed behavior and prior targeted validation remain below and in Documentation/03_Technical.

Latest validation: isolated DistrictRefinementsQA passed actual saved C2 4x4/8x4 candidates and materialization, all four cardinal parcel/frontage/physical street-fit checks, restore/re-stamping, spray containment/building masks/input ownership, preferred pedestrian paths and binary region save/load. Modal QA and stream before/after renders passed. Main Unity runtime assembly rebuilt after sync with no new compilation errors. `git diff --check` passes. Broader historical feature QA is recorded in the dated sections; no claim of a fresh exhaustive all-features run. Player saves were not written during latest QA.

Next session: read this summary, AGENTS.md and the relevant technical document before editing. Preserve approved meadow/forest/ocean presentation, older save behavior and private-asset separation. Work in the isolated QA copy while Joel edits lots. He asked not to be repeatedly prompted to save/return to menu; coordinate synchronization only when an active session would be interrupted. Main was at the menu outside Play at synchronization. No merge is authorized by the commit/PR/handoff request.

---

# C2 medium-wealth commercial imports · November 20, 2026

Imported all12 FBX archives in Downloads/C2-commercial as private V3Buildings/
C2MidWealth* assets: Ye Olde Inn, King's Arms, Apothecary (two variants), Bookshop,
Ceramics, Chevron Store, Coffee Shop, Merchants Shop, Dry Goods, Pottery Barn and
Sea Urchin. The coffee archive's supplied sign reads Sherborne Dry Goods; original
art remains intact. Registered Commercial2 / Medium / Founders via the existing
commercial catalog/growth pipeline; C1, wealth and residential pools are unchanged.
One sideways Apothecary was rotated in scratch geometry so storefront faces+Z like
the other imports. South-facing growth/row offset is180° for this C2 prefix.

Original Model.fbx at L0–2; reduced Distant.fbx at L3–5, same UVs/origin/footprint/
roof silhouette. Measured catalog dimensions and row wall/front-step profiles for
all12. Selected front window/shopfront masks preserve sash/merchandise detail;
projecting exterior lantern glass includes side faces and warm local pools. Existing
28% softer building emission and accepted broader lamp ranges apply to both tiers.
No authored source wall/brickwork replaced, no player lots/saves changed.

C2ImportQA isolated copy passed12 registrations, exact medium C2 pool/no low-wealth
fallback, fitted dimensions/ground pivots, reduced triangle counts, zoom switch both
ways, noon/evening/night emission and lantern pools; four corner and near/far renders.
C2RowsQA passed deterministic row packing at all four frontages and every profile
in a roomy footprint. Evidence: ple-2/outputs/C2-Commercial/{Day-Review.jpg,
Night-Review.jpg,Dimensions.csv}.
Some supplied buildings are deeper than three tiles including attachments; growth
and row selection retain normal fit checks instead of squeezing their proportions.

---

# District polish, Candlewick performance and rats · November 19, 2026

Texturize now blocks PrototypeRegionView.OnGUI world picking as well as Update
picking, and cancels active zone/road editing before opening. Zoom4 pan multiplier
1.3→1.625 (+25%). Residential window RGB emission is multiplied .72 at both night
and evening, including high wealth; lamp light pools retain their accepted strength.
Prebuilt medium-wealth R2/R3 stamps default to founders-cobblestone-street; existing
roads are reused. Fence substitutions enforce section/partial words on catalog ID
and label in addition to existing wealth/family/part/clearance checks. Invalid
diagonal rectangular stamps keep all red footprint geometry and feedback instead
of vanishing when preparation returns no lots. QA covers3×8 and8×3 at45/135/225/315°;
cardinal rectangle coverage was already verified.

Candlewick profiling: 259 bank-dressing batches,3926 instances/~3M triangles.
GeneratedStreamAppearance now retains exact near batches and supplies coarser
zoom4/5 batches, culling tiny stones and disabling tiny distant shadows. Dedicated
player-channel water cannot be promoted by ApprovedRiverWater into a Crest engine.
Candlewick also has a legitimate native river: its Crest clip-mask creation was
sampling the entire1024² district. Conservative river/ocean bands skip proven dry
pixels; GPU readback versus original computation matched every pixel (max error0).
Controlled open (overview plus district) fell14.429→10.151 seconds. Actual player
load depends on cold assets/editor state. Same-process play samples showed modest
wide-render gains, not a promised FPS. Grade-backed terrain now uses separable
sampling too;66563 comparison points had normalized error0. Player-channel terrain
replay uses conservative bands. Heightfield timing added to landscape stage log.
No ocean material or persistent shadow settings changed.

Urban rats: private UrbanRats/Rat.fbx + reduced Distant.fbx, body/normal/whisker/eye
materials; rat.blend meshes baked static, source Rat_Lod_0.rar supplies DDS maps.
Whisker mask is grayscale→alpha (original DDS alpha is opaque). Model fits0.5m full
length including tail; original texture detail retained at appropriate1024px/512px.
DistrictRats supplies2 per low-wealth zoning lot, capped24 across12 homes; bounded
short dry/obstacle-safe scurries and pauses, no rig/path graph/colliders/save schema.
Near/far meshes switch atzoom3, hidden and idle atzoom4–5, offscreen pauses. Homes
refresh every5s and removals/wealth changes/moves clear stale rats. Assets/catalog
remain private/ignored. No player saves written; SHA checks passed.

QA: HeightSamplingQA + StreamGroundQA + StreamConnectionsQA + DistrictPolishQA
passed in isolated copy, including full native mask preservation, fence words,
road defaults, texture GUI guard, all diagonal rectangle previews, window reduction,
rat population/movement/leash/far visibility, and original save preservation.
Evidence: ple-2/outputs/District-Polish.

---

# Additional R3 homes and broader lamps · November 19, 2026

Imported eight FBX archives from Downloads/R3-middle: Barn Roof, Blue Townhouse,
Dutch Roof, Mansard Roof, Stone Dutch, Stone House, 3 Window Brick Dormers and
3 Window Brick. All are medium-wealth Founders Residential3, 11.3m tall with
measured footprints and wall/step row geometry. Private model/color/normal/window
assets preserve supplied art. Distant meshes preserve UVs, footprint and roofs.
There are now sixteen R3 imports; existing PNG-only townhouse remains separate.
New modeled front lantern on 3 Window Brick has a ray-picked anchor; other new
fronts have no modeled lantern. QA registration, dimensions/pivots, reduced meshes,
original L0–2 / distant L3–5 switching, and evening/night windows passed for all16.

Streetlamp cones widen110→130°, inner55→75°, range increases25%; wall lantern
range2.8→3.8m. Warm colors, intensity, day/night timing and shadow-free lights stay.
Art and catalog remain private/ignored. Evidence: ple-2/outputs/R3-Middle.

User preference: do not repeatedly ask to save/return to main menu before reloads.
They explicitly said this after saving and replying ready for this import.

---

# Evening controls and stream load optimization · November 19, 2026

Player channel masks, terrain paint and graded meshes now use conservative per-row
segment bands, avoiding repeated nearest-segment queries over dry bounding-box
corners. Same copied two-stream district: player water meshes/paint 5475 ms before,
1444 ms after (74% reduction); this measures that stage, not total interactive load.
Existing water appearance, dry ground paint, joins and waterfall checks passed.

Zoom label sits above bottom-right messages. Clock has Pause/Resume, draggable
hour selection with AM/PM continuity, and exact hour/pause persistence. Stars fade
in after 20:00 and a procedural moon tracks the night. Prebuilt zones support
independent width/depth arrows (including 8×3), rotated rectangular parcels and
wealth/type-filtered counts. R3 window masks fill glazing UV shapes while retaining
sash lines. Construction dust has half material alpha, smaller billows and footprint;
scaffolding stays visible and dust waits until completion before rising. Move stays
cloud-free. Main Unity refreshed at main menu; no player save writes.

QA: EveningOddsQA interaction/sky/dust/8×3 rotation checks; StreamGroundQA,
StreamConnectionsQA, SavedStreamLookQA and R3ImportQA all passed in isolated copy.
Saved region SHA256 unchanged. Preview artifacts in ple-2/outputs/Evening-Updates.

---

# Growable Building Row prototype · November19, 2026

Growable layout now offers Single building / Building row. Rows save an empty
footprint and eligible pool. The road-facing edge supplies a gold front-step line;
entry marker moves it, row gap0–4m controls touching/spacing. Optional Preview Row
and arrow reshuffles never add permanent objects. Selected manually placed
buildings can become a footprint in an undoable edit. Single-only facing/path
controls are disabled in row mode. Building info / Row alignment edits canonical
South-facing wall/step markers; the eight R3 imports have measured catalog defaults.

LotRowBuildings performs bounded deterministic width packing, avoids repetition,
aligns measured walls/steps and validates bounds without stretching. Actual row
objects/positions/valid GUID IDs persist in normal lot/region metadata. Normal and
diagonal zoning, Change Lot, construction reveal and Change row are wired through
materialization. Change row preserves fixed authored props. Commercial1–3 growth
now respects stored wealth and the same frontage pipeline (matching assets/layouts
still required); low-wealth R3 restriction remains. Straight frontage/level ground
is the first-version target, with leftovers split at row ends.

See Documentation/03_Technical/Growable_Building_Rows.md. RowLotsQA in isolated clone
checks deterministic packing/site variety, joins/step line/no stretching, four
frontages, gaps/failure messages, commercial pool, exact row serialization, empty
saved templates, selected-building conversion, actual district construction reveal,
Change row and saved-region validation. Existing growth checks updated for the
already-established R3 low-wealth exclusion and C2/C3 support. Protected landscaping
also covered. Evidence: ple-2/outputs/Building-Rows. No player saves/lots edited.

# R3 medium-wealth Founders homes · November19, 2026

Imported eight FBX packages from Downloads/midwealth-r3 as private V3Buildings/
R3MidWealth* assets. The ninth archive, r3-midwealth-3story-brick-townhouse.zip,
contains twelve PNGs only; user said they will provide the model. Rechecked the
same directory after their follow-up; no additional model was present. Townhouse
is pending, not silently replaced with a flat image.

Catalog IDs v3-building-r3-mid-wealth-{2-story-brick,2-story-tan-wood,
2-story-yellow-house,3-story-brick,3-story-brick-2,3-story-yellow,
3-story-yellow-stucco,painted-white-brick}. Registered residential / Founders /
Residential3 / Medium, with supplied color and normal textures, actual dimensions
and ground-centered pivots. No procedural replacement of the incoming brickwork.
Each has an authored Windows.png, a reduced Distant.fbx preserving UV charts,
roof/footprint extremes, and a facade-ray-picked entrance-lantern light anchor.
Original models at L0–2; reduced at L3–5. Shared dusk/night emission and lamp pool
behavior applies to both tiers. No automatic lots/zones were added.

ResidentialWealthPolicy rejects Low Residential3 in Founders and Industrial,
including candidate growth and Founders layout eligibility. Zone modal disables
Low for R3 and switches selection to Medium; both direct and prebuilt zoning arm
paths normalize invalid early-era R3 Low to Medium. Other densities retain existing
wealth behavior. Old saved zoning is not rewritten.

R3ImportQA PASS in isolated QA: eight complete registrations, candidate growth,
early-era low-wealth exclusion, unchanged low R2 policy, dimension/ground pivots,
near/distant transitions0→2→3→5→2, noon/evening/night emission on both models and
lantern pools. Four-corner/day/night/LOD images visually reviewed. Evidence under
ple-2/outputs/R3-Residential, including Dimensions.csv and verification. Source
art, generated masks, FBX, materials and local catalogs remain private/ignored.

# Height carry-through, Feather, eagle fallbacks and horizon · November19, 2026

Read-only inspection found the saved library mill has item elevation−.5m, but the
existing district snapshot retained0m (and an older3×3 footprint vs library4×4).
LotElevationUpgrade performs a one-time height-only backfill for legacy placed
records from the same local template/object IDs. It preserves placed geometry,
nonzero per-object heights and district elevation. authoredElevationVersion1 is
persisted; new manual placements start at1. No disk region migration is performed:
normal user Save retains upgraded metadata. Preview now uses the same footprint
waterfront clamp and both authored height offsets as the placed model.

Texturize replaces Falloff with Feather0–8m; zero is a hard stop. GroundPaintData
uses PointWireGeometry.SignedDistance for a symmetric selection-edge coverage
ramp. Repeated passes respect its ceiling. Existing byte-weight persistence and
undo remain; no alphamap or biome changes.

Eagle drops prefer deep/open water, then actual river/stream centerline samples,
then a real canopy (approved forest, placed trees or lot trees). Waterless no longer
blocks the encounter. Tree landings skip water splash/submergence. Existing
passenger return and camera cleanup retained. Horizon blues extend down to the
horizon; near/far ridge tints are blue rather than the prior pale gray band.

LOCAL PRIVATE CREST PATCH (not public repo content): in embedded package
WaterReflections.Render, both reflection slices now ResetProjectionMatrix AFTER
CopyTargets and BEFORE ComputeHorizonPositionAndNormal. RenderCamera still uses
and stores the original oblique reflected matrices; this only makes the horizon
unprojection use a regular frustum instead of a degenerate oblique far plane.
Reapply this ordering after replacing/upgrading the private package. Reflection
settings, ocean materials and rendering resolution are unchanged.

FollowupPolishQA PASS against copied real saved region: authored height backfill,
explicit override preservation, actual district lowered mill and matching ghost,
feather hard/soft and repeated strokes, paint roundtrip, water and tree eagle
fallbacks, 280 orthographic/perspective camera pitch/height horizon cases with no
frustum errors. Source player save SHA unchanged. Screenshots in
ple-2/outputs/Followup-Polish; main project refreshed separately.

# Mill elevation, construction reveal and sky polish · November 19, 2026

District selected-lot panel now has separate Lot ±0.25m and Building ±0.25m
controls. Placed-lot elevation and authored per-object / whole-lot elevations
serialize as optional zero-default fields, clamped to ±20m. Editor selected-object
toolbar has Object ± and Lot ± controls. Whole-lot elevation moves its rendered
contents and ground overlays; it does not excavate terrain. Building control uses
the growth building, otherwise the first building on a playable lot. Save the
region after adjustments; editor-authored offsets save with the lot definition.

Lumber mill runtime wrapper and footprint are scaled 1.3, preserving authored
object scale. WaterMillWheel rotates both authored Mill_Wheel pivots (near and
far models) at 18 degrees/second about their thin mesh axis. No art rewrite.

Moving an existing lot no longer triggers construction dust. New placement hides
the final root behind stationary tan billows, without the old rising ground dust.
Reveal enables upward velocity and fades the veil after completion. Zoned growth
uses the same veil. District horizon has richer daytime blues and darkens the
entire sky/cloud composition at evening/night.

MillPolishQA PASS in isolated clone: offset serialization, runtime position,
30% scale, both loaded wheel tiers rotating, stationary tan veil and reveal-only
rise. Rendered cloud and day/night sky samples in ple-2/outputs/Mill-Polish.
Main project refreshed and assemblies reloaded successfully. No player saves edited.

# Clear, sparkling player streams · November19, 2026

User supplied a clear shallow blue-green stream reference and requested sustained
screenshot iteration. Seventeen variants plus baseline were rendered and compared.
Final production player streams have smaller advected ripple normals, narrow sky/sun
glints, subtle opaque-color refraction and depth-graded transmission, clearer stony
shallows and a bluer center. Daylight scaling keeps night water dark. Material
preset is isolated to playerChannel&&isStream; native river/lake/canal settings stay
authoritative. Requires one shared camera opaque-color copy, not per-body captures.
No wave physics; standalone GPU cost has not been benchmarked.

PlayerChannelEdits adds a derived stream-only coarse-pebble layer using existing
wet gravel at7m, strongest near margins, finer gravel centrally. Canonical saved
alphas exclude derived bed paint; dry bank paint and geography stay unchanged.
GeneratedStreamAppearance reuses existing instanced edge stones and native meadow
grass clumps for player streams. Saved grades, widths and routes are unchanged.

FinalStreamQA PASS: ground controls168, dry-bank alpha preservation (including new
layer zero weights outside bed), native moisture, width/grade/extension consistency,
confluences, waterfall geometry, descriptor roundtrip/exact terrain replay. Actual
copied user district opened normally and rendered with final production defaults;
original SHA256 unchanged/no normal saves. Close/wide, motion, alternate sun and
night rendered/inspected;48-frame motion clip exported. Main refreshed without new
C# or shader errors. User explicitly replied ready; stopped MainMenu verified.

See Player_Waterways.md and current chat outputs/Stream-Look for evidence, iterations,
Final-District.png, Before-After.png and Stream-Motion.mp4. No commit/push requested.

# Preserve grass around drawn streams · November19, 2026

TerrainGeography's44m moist-grass influence was recoloring ground around player
streams bright green. RefreshWater now excludes playerChannel&&isStream, preserving
native water/lake/marsh/canal moisture. Near drawn streams, geography height/slope
classification uses CoverageHeight (original retained terrain), so excavation does
not replace the surrounding geographic surface. PaintPlayerChannelBanks skips dry
stream-bank samples; wet bed rendering remains. Existing streams get corrected
derived controls and bed paint on reload; no save mutation/migration.

StreamGroundQA:168 before/after control samples and all dry-bank alpha samples
unchanged; native stream/canal/marsh moisture preserved. Before/after terrain
rendered with geography controls. Previous stream connection/waterfall/width and
persistence regressions retained. Main Unity stopped at MainMenu before edits.

# Stream connections and actual waterfalls · November19, 2026

Player stream snap/join now accepts either endpoint, interior tributaries and
crossings. Previous downstream-only/stream-overlap/self-overlap and12m stream
trench rejections are removed. Roads/camps/bounds/route limits and lakes/native
water remain guarded. Existing downstream extensions preserve ID/stations; drawing
uphill from that endpoint creates a reversed incoming branch instead. Existing
receiver records are not rewritten. Connection grades blend into the receiver;
ordered single-owner overlap masks prevent doubled translucent water. Indexed
nearest-point queries and overlap-only ownership lists bound creation cost.

StreamWaterfallRenderer adds separate lips, real vertical falling curtains and
landing foam at sharp drops, in addition to slope-based rapids. One combined mesh
per body, <=128 falls, no particle or per-frame terrain simulation. New procedural
shader included via Rendering/Water/Resources/StreamWaterfall.shader. Rebuilt from
existing saved grade arrays; no new descriptor fields. Removal ray bounds now
include all grade extrema and the actual stream fill level.

StreamConnectionsQA passes actual middle/upstream joins, uphill incoming branch
from an old downstream end, matched join levels, unchanged receivers, serialization,
extensions/exact replay, falling curtain geometry/shader and prior water tests.
Rendered join/fall inspected. Main Unity stopped at MainMenu before edits; no player
saves written. See Player_Waterways.md for authoritative scope/details.

# Stream rapids and cascades · November19, 2026

Player stream grading now uses original detailed CoverageHeight locally, avoiding
false trenches from regional smoothing. New streams sample1m, auto-reverse an
unconnected uphill sketch, and retain monotone downhill grades. Extensions preserve
all previous stations/modes/ID and direction. Regional portal stubs retain shared
stored grades. Canal/river behavior is unchanged. Routes climbing over12m still
need a path around the hill; steep downhill drops are supported.

Player streams enable slope-derived, animated whitewater in LakeWater.shader: calm
water, broken rapids on inclines and cascading sheets on steep drops. Uses existing
grade mesh/flow field; no physics/particles/per-frame terrain changes. Saved stream
grades are not regenerated. New shaping persists using existing descriptor arrays.

Private StreamCascadeQA: original detailed terrain vs flat regional approximation,
auto direction, gentle rapids and4m ledge grades, stream-only optics, exact saved
replay, extension identity/station preservation; rendered two animation times. Full
prior water/width/bankfull integration checks also pass. No player saves touched.
Main Unity stopped at MainMenu before source changes. See Player_Waterways.md.

# Stream water fill · November19, 2026

Player stream surfaces now sit15cm above their saved excavation grades, bringing
water toward the bank lip while leaving the approved bed and widths intact. Shared
PlayerChannelGeometry.Level reports the raised surface for rendering, removal,
wildlife and overview. Sampler.Query retains raw grades for excavation and extension.
No descriptors are rewritten; existing streams get this presentation on reopening.
Player-only stream absorption density1.4 makes shallow water visible; shader default
0.42 preserves other water. Bounded local footprint remains unchanged.

Private StreamFillQA passed actual creation, sampled wet bank edges, shared15cm
level, stream-only optics, disk roundtrip/exact terrain replay and existing width,
canal/marsh, farmer and modal checks. Rendered before/after inspected: formerly
brown-dominant stream now has continuous teal water across its bed. No player
saves written. Main Unity verified stopped at MainMenu before edits.

# Narrow streams and width variation · November19, 2026

New streams opt into playerStreamWidthVersion1 and a saved width mode: Uniform,
Gentle(default), Natural. Maximum halfWidthNormalized is4m/mapSide; Uniform uses
6m full width, Gentle5–7m and Natural4–8m (1–2 district tiles). Two smooth seeded
sine components vary width by cumulative normalized station. The seed derives
from stable body ID, so saving, cross-district rendering and extending never reroll
existing reaches. Width preferences/version are optional fields; legacy zero keeps
old constant widths, and extensions retain their original width/depth/bank settings.

Water modal has Stream-only Vary width buttons. Stream preview shows the maximum
for its choice (6/7/8m). The exposed River route action was removed per the user's
future-river scope; shared internal mechanics remain. Creation still uses PointWire.
New stream depth0.45m and banks1.5m plus a narrower dry paint feather avoid the broad
river-like corridor. Existing streams/canals/marshes/river descriptors are retained.

PlayerStreamWidth.AtStation is the shared normalized evaluator. Terrain excavation,
bank paint, RegionWaterBody.Contains / water depth-mask coverage, distance/wildlife
queries, and scanned stream rock/shrub placement use the same local width. Maximum
bounds remain conservative for clipping and collision/forest protections. Cached
phase is NonSerialized; validation bounds the saved version/mode. New state lives
on the existing water descriptor; no global terrain recipe or save-version change.

Private StreamWidthQA samples all modes at6001 stations each, asserting4–8m bounds,
continuity, distinct ranges, water-mask/point-width agreement and reload stability;
legacy widths stay constant. Actual Natural stream creation and full water-descriptor
disk roundtrip/fresh terrain replay pass alongside canal/marsh, farmer and zone-count
regressions. Modal and actual stream rendered/inspected. No player files written.
Live district was observed running before edits; user saved/closed it and explicitly
replied ready, then stopped MainMenu was verified before main C# changes.

--- Earlier work ---

# Water Features, field-working Farmer and filtered zone counts · November19, 2026

Zone modal footprint cards now distinguish block placement capacity (spaces) from
available saved buildable layouts. FoundersGrowthLayouts is loaded once on open;
counts filter current zone type/density, wealth and road-facing size, and refresh
on each selector change. Auto counts all matching sizes. No per-frame library IO.

LotScriptedBehavior.Farmer is appended as6, preserving Harass4/Patrol5. Character
Script > Scripted > Farmer validates the v3-farmer asset and persists the assignment.
DistrictFarmer walks a bounded grid of nearby field patches inside LandBounds,
rotated with the lot, and alternates Walk with7s Hoe work. It reuses PatrolClear
oriented obstacle/dry-ground checks, conforms to terrain and uses actor simulation
cadence. Blocked routes retry without teleporting; original authored positions stay
unchanged. LotCatalogSetup adds/loops the existing Farmer_Hoe FBX take on both
existing/new Farmer controllers. Private source art/controller remains ignored.

Nature > Water opens a centered Water Features modal on the HUD canvas, using
supplied private Icons.png as separate illustration/state layers and Concept.png
for text-free banner/water accents. User art is unchanged, sampled by UV rectangles.
Stream/Canal/Pond/Lake/Marsh cards select, Draw Feature arms existing PointWire tools.
River remains available via River route; Remove Water uses the existing picker.
Shape editing is explicitly unavailable, and unsupported brush options are not
presented as working controls. Popup.Visible includes the modal so repeated Water
category clicks dismiss it correctly. Close/Escape/mode change release the modal.

User explicitly requested real Canal and Marsh creation after clarification.
Canal uses the existing regional playerChannel descriptor with appearanceProfile
player-canal-v1, isStream=false,8m width,1.6m depth and1.2m banks. Guide reaches stay
straight, samples/immutable grades still use existing regional station machinery.
Pow16 bed cross-section creates a flatter engineered bed. A separate child mesh
supplies joined masonry retaining banks/caps with existing Bluestone material;
water mesh is preserved. Channel snapping/extension matches the canal profile so
it cannot silently extend an ordinary river. Existing streams/rivers unchanged.

Marsh uses playerCreated basin metadata with profile player-marsh-v1,0.55m depth,
8m bank and10m outer bank, requires <=2m bank height variation, and keeps lake's
existing closed PointWire validation, excavation, clearing and replay. Its water
has muted green shallow/deep tints and existing batched native reeds populate
shallow interior patches (bounded1400 total plant placements). Profile strings and
all dimensions persist in existing water records; no save-version/schema migration.
Both feature types use ordinary water removal and original-terrain restoration.

Private WaterFarmerQA verifies actual modal dispatch, wealth-filtered counts using
a disposable QA-only lot, Farmer movement/rotation/blocked-route and Walk/Hoe state,
script serialization, real canal and marsh creation, separate bank/water meshes,
reeds, disk water descriptor roundtrip, exact fresh terrain replay and removal
picking for both types. Rendered modal/zone/farmer/water close-ups inspected.
No player files were saved or modified. The isolated QA catalog needed an in-memory
R2 prefab binding repair due its older copied GUID; no production catalog change.

--- Earlier work ---

# Construction reveals, connected corners and shared HUD · November19, 2026

ConstructionDustVeil supplies a prefilled stationary dark particle volume plus
rising wisps for placed/moved lots and growable construction. Fixed billows have
zero velocity/noise and long lifetimes; completion explicitly starts a0.7s fade,
then the component releases its material/object. Finished lots remain hidden via
revealingLots throughout the existing schedule, including presentation rebuilds.
Intermediate timber/ground construction is also hidden behind the independent
veil. Existing upward placement dust remains. No persistent construction schema.

DistrictRoadRecord.junctionCorners is a NonSerialized, derived corner index set.
ConnectedRoadCorners finds additional incident legs at curved authored corners;
RoundCorners skips only those corners. Isolated and unrelated bends remain round.
RefreshRoadShoulderDetails recomputes corner policy and replaces affected pavement
meshes before rebuilding sidewalks, masks/recess and junctions. Sidewalks, shoulders,
guides and navigation share the same sampled centerline. Removing a branch restores
isolated curvature without changing saved points or curved preferences. Street snap
prefers an authored corner near the sampled curve. Junction patches now also cover
ordinary road-to-road Ts, per the user's expanded road connection rule.

Developer Zone modal height816 (normal game684), layout720 vs600, adds exclusive
Low/Medium/High wealth radio controls under UNITY_EDITOR || DEVELOPMENT_BUILD.
Selection changes selectedZoneWealth used by both automatic and preset zoning.
Nondevelopment builds omit the entire wealth row and use Low. Existing saved wealth
is retained. Radio discs are native UI graphics; no new raster artwork.

Both views use RegionBrass save/back artwork at53px. District positions shift left
to retain the clock. BrassHudHover adds a gold outline and6% scale on hover, resets
on exit/disable. SaveWithFeedback enables/animates the existing progress strip in
both views before capture and hides it in finally. No save triggered during QA.

Private RevealPolishQA covers isolated/local corner cleanup and branch removal,
three-arm Ts including ordinary roads, wealth selection/exclusivity/layout rendering,
shared brass art and hover/reset, district save pre-capture animation/cleanup, fixed
particle positions over simulation, rising wisps and explicit completion reveal.
BlockOverlapQA regression checks preset road reuse. No player files written.

--- Earlier work ---

# District placement, scripts and HUD polish · November19, 2026

Surround lot with fence now reserves the shared1m residential sidewalk width on
all four sides before the existing fence post/corner inset. Manual fence guides
retain their original placement behavior. Growable (Zoning) lot trees render at
75% of their authored runtime scale, after deterministic species variation; saved
objects and playable-lot trees retain their authored scales.

New low-wealth R2 zoning creates independent simple streetlamps at12m /3-tile
intervals along exposed road-facing boundaries. Cardinal frontages share a global
3-cell phase; rotated frontages use12m world-projected stations. Nearby fixtures
are deduplicated. Actor records persist zoningStreetlamp ownership, and lot
structure replacement skips those records. Demolition/player removal still works.
Existing actor placement limit512 remains; no player save/library is auto-written.

Civics → Government explicitly offers one built-in4x4 Town Center using the
existing Timber Council Hall asset. This exception does not restore unwanted
embedded Chapel/Constable lots. The center has a stable valid32-digit definition
ID, TownCenter freight delivery role, and registers a noncapital town in a district
that lacks one when placed. City/Civics opens in districts without towns once the
capital foundation exists; other construction asks for a local Town Center first.

Cmd+S command prompt accepts Help in either view. Help is a scrollable modal whose
buttons call the same dispatcher; region era-message scripts are region-only,
weather/lot/encounter commands are district-only, and season commands are shared.
Let it snow switches to Winter; Let it rain switches Winter to Summer. Tropical
snow is explicitly rejected. Place lot opens the existing saved-lot picker.
Existing Hooligan/Fell fir/Eagle command behaviors are preserved.

Persistent top placement strip is hidden. Customize lot now lives in the selected
lot panel; Tab rotates a placement. Placement feedback routes to the existing
message area. The52px clock is at top right, to the right of the back arrow;
back/save move left. Clicking the clock toggles pause and colors its hand orange
while paused. Legacy Pause clock toggle is gone; lower-right time/wealth/shadow/
lighting controls and legacy fish button are hidden/removed. Nature controls retain
weather and time-of-day access.

Private isolated DistrictSmallItemsQA validates scoped Help rendering, season
execution, pause/resume/clock positioning, hidden legacy UI, valid Town Center
serialization and district registration, R2 spacing/deduplication/ownership
serialization and protection during lot clearing, runtime tree scale without
authored mutation, and actual fence surround guide inset. No player data written.

--- Earlier work ---

# Matching district Nature and City toolbars · November19, 2026

User requested only the main district Nature menu match City. NatureToolbar now
uses five captioned category cards with the City's actual dark/gold rounded frame
artwork, identical146x82 hit targets and148 spacing, row bottom117, and the same
Nature/City switch bottom64 in both modes. Existing Nature icons remain, labeled
Terrain, Water, Forests, Animals and Sun & Sky. Georgia/serif captions, hover/press
palette and gold active states match the City styling. No inner popup redesign.

DistrictCategoryButtonArtwork samples the unchanged City atlas's empty side strips
and column to construct a caption-free frame; icons/text are separate UI elements.
It uses the existing TownButtonArtwork material/shader, with no new raster art or
licensed art checked into git. Selected state only dirties the graphic on change.
Runtime material/font ownership is disposed with the Nature component. Original
NatureToolPopup and seasonal/weather controls remain unchanged; the popup follows
the higher main row so it clears the new buttons. All category callbacks and tool
arming/dismissal hooks are retained.

Private NatureToolbarQA passes actual-render comparison, every category action,
gold selection/repeat-click dismissal, hover colors and pointer tint, row/button
geometry, both mode switches, hidden opposite row and existing popup routing.
Screenshots in current chat outputs/Nature-Toolbar. No player data written.

--- Earlier work ---

# Road curves, blend and civic scale polish · November19, 2026

User requested another5% closer Region Zoom5, curved angles by default, two-sided
T curves, softer road/street texture joins, a roughly20% larger Constable Station,
and25% darker exposed sunken edges. Zoom5 default factor is now.9025 relative to
the original (two successive.95 adjustments); farthestSize baseline and zoom0–4
remain unchanged. Existing saved camera poses still restore as authored.

New transport roads and preset rings/spans use curved=true; old saved choices and
Straighten angles remain intact. Curved proposal/existing-road reuse compares
authored guides, preventing duplicate corner fragments. BlockOverlapQA passes
all12 preset size/rotation cases, including zero new pieces over a matching ring.

StreetJunctions now blends ordinary road-to-street connections too, while unrelated
road-to-road policy remains. Same-direction arms use longest available reach and
widest source, rather than a short overlapping stub. Rounded-guide fallback keeps
an explicit authored intersection when both rendered ribbons cover it; incident
guide legs restore the receiving continuation. This fixes bilateral T fillets in
the user's actual saved topology. No endpoint positions or player records change.
Approaches extend5m beyond half-width and fade over3.5m; junction lighting now uses
the ribbons' URP PBR path instead of the brighter hand-lit decal. Shared recess
fields rebind to existing junction materials after rebuild. Lining multiplier.72
becomes.54 (25% darker), preserving pavement color and6-inch depth.

Station catalog footprint/dimensions scale by1.18: a full1.20 made the authored
135-degree building just exceed its existing5x5 placement allowance. User was told
18% is used to retain validity. Height10.62m, max span15.459m; original and reduced
meshes fit uniformly, and emission UVs remain. Catalog setup explicitly updates
this asset's existing generated dimensions; it previously left existing civic
footprints unchanged. Saved lot positions, scale and definitions are untouched.

JunctionPolishQA checks/renders bilateral T, curved receiver, road/street blend,
read-only saved layout, current recess binding, shader compilation, defaults and
station dimensions/LOD/emission/existing-lot fit. SavedPatrolQA and full PatrolQA /
Harass regressions pass after scaling. Actual player files were only read; no
region or lot saved. Screenshots in current chat outputs/Junction-Polish. See
Documentation/03_Technical/Street_Junctions.md for the current behavior.

--- Earlier work ---

# District and lot-editor maintenance · November 19, 2026

User authorized ten small changes, explicitly including natural forest and ground
paint in rectangle destruction. DistrictDemolition now previews a red world-axis
rectangle from mouse-down to pointer; release clears fully enclosed lots, origin-
contained placed actors/trees/props, center-contained zoning, forest trunks and
paint pixels. Crossing roads retain outside polylines. Escape/focus/UI cancellation
mutates nothing. Areas respect original active members. Road count/guide limits
preflight before mutation. ForestRemovalStroke.rectangleSize is optional additive
metadata, zero preserving circles; masks persist and filter caches/fresh ecology.
Later additions still override older masks. See District_Area_Demolition.md and
Forest_Removal.md. No player map/lot file was modified during implementation.

Preset roads already use UnpavedRoadPieces: existing coincident roads, regardless
of surface, are reused. Private BlockOverlapQA passed all12 size/cardinal-rotation
cases with a full existing ring producing zero new road pieces. Junction crossing
patches remain intentional, not duplicate parallel roads.

Selected zoned lots now have separate Change lot and Change building actions.
Change lot chooses a different saved Founders layout with matching frontage size,
zone, wealth, era and corner handedness; adjusts quarter turns for authored
frontage and retains diagonal heading, placed identity and stored resources. It
validates site before replacing, materializes a matching building/entry/path-style
variation, clears obsolete customization/vehicle jobs and rebuilds presentation.
No alternative gives visible feedback. Other saved lots remain untouched.

Trees fit if their origin/trunk lies within actual lot bounds; canopy no longer
blocks placement/save, repeats or runtime variants. The asset-picker randomize
checkbox no longer changes species in the editor: it marks newly authored trees
varyTreeOnPlacement. Existing deterministic same-group runtime variation handles
population, stays stable on reload and leaves authored definitions unchanged.
Save validation failures now open an explicit Unable to save lot modal with the
actual reason; IO/parse failures also display their reason. Validation label uses
the actual issue instead of a generic boundary warning.

Zoom5 size is95% of its prior default, both initial overview/area framing and
normal discrete zoom; farthestSize baseline is retained so zoom0–4 do not shrink.
Saved custom camera states still restore as authored. Nature/City modes sit at
bottom126 in Nature, above icons; City mode retains64 to avoid its toolbar.
Low-wealth R1 joins R2/R3 soft dirt frontage eligibility. Founders dirt streets
have dirt sidewalks by default and masked plank strips only at commercial lot
frontages; masonry street style remains authoritative. Preset zoning Escape
cancels without reopening the picker.

Private MaintenanceQA passed road clipping, actual area mutation and outside
paint preservation, forest mask compatibility, trunk fit, editor/runtime tree
variation, visible save failure, outline cancellation and Zoom5 sizing.
LotActionsQA passed real same-size/wealth lot replacement at45°, identity/resource
preservation, both action buttons, actual R1 dirt/no-plank rendering, commercial
plank rendering and hidden picker cancellation. Temporary QA library copies were
removed; main user library was read only. NeighborhoodFootpathChecks updated for
R1 inclusion. Isolated destruction render in current chat outputs/Maintenance.

--- Earlier work ---

# Combined parcel grid cells · November 19, 2026

User rejected floating size labels/extra parcel outlines and requested the grid
itself merge tiles, like combined districts. Replaced the prior parcel overlay:
G now shows one actual grid cell per complete reserved or grown zoning lot.
DistrictGrid clips fine lattice edges out of parcel interiors using spatially
bucketed ParcelGridCell rectangles; diagonal footprint borders join the same
mesh/material. Cardinal boundaries remain the original grid lines. There is no
extra colored border renderer or OnGUI label pass. Adjacent ordinary ground keeps
its fine grid; partially erased reservations are not visually combined.

Valid block previews temporarily merge the grid too; hover/cancel/focus visibility
restores the underlying grid. Zone color fills now join internal tile edges and
use one cardinal frontage arrow per parcel, including permanent zoning and block
preview. Diagonal color previews no longer have per-tile gaps. Saved dimensions,
exact-size growth enforcement and the underlying4m placement lattice are unchanged.

Private MergedGridQA covers12 size/rotation combinations, no interior lattice
segments, exterior fine grid, native diagonal borders, preview/cancel/G visibility,
partial erasure, unchanged zoning serialization and residential growth checks.
Combined district exterior borders and excluded L-shaped corners are covered.
Isolated actual-mesh Unity render saved in the current chat outputs. No player
region/lot files written. See Documentation/03_Technical/Zoning_Parcels.md.

--- Superseded parcel-label implementation and earlier work ---

# Exact zoning parcels and grid guides · November 19, 2026

Investigated the user's latest Guilford save via read-only metadata. Fixed parcels
already retained their requested sizes: no fixed-size/grown-layout mismatches.
Most smaller 2x2 lots grew on Auto zoning. Preset block placement skips existing
zoning rather than converting it; no existing lots or saves were rewritten.

HasGrowthZoning now also checks the final candidate world dimensions against the
requested frontage/depth, swapping axes for east/west. Existing exact-layout and
anchor checks remain. Missing matching layouts get an explicit size-required
status rather than the generic smallest-layout space hint.

DistrictParcelGrid adds terrain-conforming .14m parcel borders and cached world
label anchors. G toggles gold reserved parcels and cyan Auto-grown footprints;
labels show frontage × depth, with Auto marked. Native diagonal zoning lots retain
their rotated footprint. Block placement previews outline/label each available
parcel (green/red) and include selected size in the placement status. Borders are
rebuilt on grid/zoning changes or hover changes, not every frame; screen labels
are projected only on repaint, hidden when too small, overlap-filtered and capped
at256. Cancellation/UI hover hide the preview with the existing zone guide.

ResidentialGrowthChecks adds smaller-layout rejection. Private ParcelGridQA
covers fixed block sizes/rotations, exact preview border/label counts, rectangular
frontage rotations, save restoration and existing residential boundaries. See
Documentation/03_Technical/Zoning_Parcels.md. No save schema changes.

--- Previous handoff ---

# Diagonal station Patrol access fix · November 19, 2026

User's saved Constable Station already had configured Patrol=5, including the
placed definition in the region. Building yaw135°, local center(-1,1), constable
(7.02956,-7.02956). Work collision used the rotated model's enclosing AABB, falsely
classifying that clear front corner as inside the station. Every initial Patrol
connector therefore failed; this was not an assignment or missing-road issue.

LotRuntimeLoader.ObjectFootprintSize now exposes the existing unrotated footprint
size calculation. Work obstacle cache retains per-piece center, half-size and
inverse object yaw alongside its original AABB. Patrol's ActorRouteClear uses the
oriented narrow phase after inverse lot rotation; broadphase expands conservatively
for rotated clearance. FreightClear/LumberjackObstacle keep their existing default
AABB behavior and API, so unrelated vehicle/worker navigation is not broadened.
Water, terrain grade, trees and district bounds remain authoritative.

Private SavedPatrolQA reads a metadata-only copy of the user's saved region, checks
the actual placed assignment, reproduces the old false block, then runs the saved
station and five nearby road records on isolated flat terrain. With the fix the
constable walks away; real building interiors remain blocked across eight headings.
Existing2000-probe work-obstacle/cache checks and full PatrolQA/Harass regressions
pass. Original player lots/maps were only read, never rewritten. User need only
reopen the region after Unity refresh; existing lot/script assignment is retained.

--- Earlier Patrol implementation ---

# Constable Patrol and Harass response · November 18, 2026

User requested assignable Patrol for a constable authored on a Constable Station
lot. Appended LotScriptedBehavior.Patrol=5, preserving Harass=4 and older IDs.
Lot Editor Character Script → Scripted → Patrol requires v3-founders-constable
and a v3-building-constable-station in the same saved lot. Place the constable on
clear ground outside the entrance: that authored world point becomes home/booking
location. No officers, stations or lots spawn automatically. Assignment persists
through existing strict lot serialization; runtime crime/custody is transient.

DistrictLots attaches DistrictPatrol only to valid configured station constables.
DistrictHarass reports an incident when it actually scares a pedestrian. PatrolCrimes
keeps bounded actor registries and dispatches each incident to one nearest reachable
available constable, with saved registration order breaking ties. No per-frame
FindObjects scans or new crime types. Existing Hooligan harass test command reports
the same incident even in daytime, so it exercises response with an assigned officer.
A reserved temporary actor's deadline extends for the escort; booking releases the
test actor, camera-follow target and fright effects through normal test cleanup.

Patrol walks nearby street/road shoulders, runs using Run to intercept, stops the
harassment and victim fright at capture, then walks the detained unarmed hooligan
behind it along the same certified return route. Both wait safely if edits block
the route and retry; no teleport shortcuts. At home they retain separation, pause
for two seconds, then hide the hooligan through evening/night and resume patrol.
A normal hooligan restores its authored position/visibility next daytime; temporary
test actors are removed on arrival. Disable/deletion releases claims/custody. Motion
pauses beyond close zoom; active-operation budgets pause with it.

PatrolStreetGraph is cached by NavigationState. It splits walking lines at T/crossing
intersections and adds links across street ends/explicit junctions. Projected access
is limited12m, ordinary beat targets80m from home. Edges lazily reuse dry-ground,
structure/tree and district-boundary certificates. Per-frame movement interpolates
known paths; response replans at most once per second for a moved target, blocked
escorts retry every2s. Offscreen movement uses existing coarse actor cadence.

Private PatrolQA verifies strict save/script IDs, actual Scripted→Patrol Apply and
wrong-character/missing-station guards; street beat, Run, real Harass event, exclusive
claim, escort spacing, station booking, night hold/day restore, disabled officer,
blocked/newly edited routes, cache/steady allocation behavior and the real daytime
Hooligan harass command through delivery/cleanup. Existing HarassChecks and
HarassFollowChecks also run. Renders are in ple-2/outputs/Constable-Patrol. No player
region or saved lot changed. Main Unity was at MainMenu before editing.

--- Earlier saved-civics browser correction ---

# Civic browser uses only saved lots · November 18, 2026

User rejected the extra automatic model-only civic lots. CivicBuildingChoices.Collect
now accepts only saved LotDefinitions; removed catalog enumeration and BuildIn.
The browser lists only user-created saved civic lots, preserving their names,
objects, footprints, era and category. Two distinct saved lots using the same
building both remain selectable. Imported models remain available in the Lot Editor;
no assets, saved library files or placed regional lots were removed or changed.
Read failures show a retry message rather than offering imported fallback lots.
This supersedes the direct-model browser behavior described below.

--- Earlier popup implementation ---

# Civic Buildings placement popup · November 18, 2026

City → Civic Lots now opens the illustrated Civic Buildings modal, using the user's
private gold service-icon atlas and sunset panorama. TownCivicsPopup.cs uses the
existing ZoneModalArtwork skin and transportModal input/lifecycle slot. Eight
categories: Government, Education, Health, Safety, Culture, Parks & Recreation,
Maritime, Infrastructure. ContentCatalog adds Maritime/Infrastructure civic lot
subcategories for authoring. Chapel is Culture; Constable Station is Safety;
Small School is Education; existing Founders civic hall models are Government.

CivicBuildingChoices combines catalog civic building models with saved civic lots,
including legacy category/subcategory labels and saved Founder's Buildings under
Government. Agriculture remains in the Parks tool. Direct model choices become
normal playable LotDefinitions with valid generated GUIDs, a one-metre margin on
each side rounded up to 4m tiles, and the original asset/era reference. Definitions
pass LotDefinition.Parse, not just unchecked JsonUtility. Saved definitions aren't
modified. Existing lot placement/site validation/save presentation is reused.

Browser has a four-column scrolling card grid, selected detail/lot size, exact era
filter, future-era availability guard, true empty states, 45-degree rotation of the
selected preview/placement, hover/press states and Close/Cancel/Escape. Preview
textures are cached per opening at256px with transparent daylight and a front
view; other thumbnail clients retain96px defaults. Preview cache and camera lock
are released on dismissal, placement handoff and view destruction. Atlas crops
normalize to source dimensions regardless of Unity import downsampling. UI scales
to fit the canvas. No invented coverage/maintenance/power simulation is displayed.

CivicPopupQA (private QA checkout) passed real catalog placement-fit and strict lot
parse roundtrips, saved alias/immutability, category/era selection, empty Place
blocking, hover/reset, initial full card visibility and scrolling, rotation and
actual BeginLotPlacement handoff/cancel, camera/cache cleanup, plus screen bounds
at1920x1080,1280x720,1024x768. Reviewed renders in ple-2/outputs/Civic-Popup.
Main Unity was at MainMenu for refresh; no player region opened/saved. Artwork
remains ignored/private; code/config changes are local/uncommitted.

--- Earlier civics asset import ---

# Founders civics import · November 18, 2026

Imported the user's chapel, Constable Station and constable from Downloads/civics.
Catalog IDs: v3-building-chapel, v3-building-constable-station, v3-founders-constable.
Buildings appear under Buildings / Civics / Founders; character under Characters /
Founders. Chapel is10.107m wide x13.480m deep x14m tall; station is10.823m wide x
13.101m deep x9m tall. Station source entrance was on-X, rotated to match the
standard fitted frontage. Both have matching Distant.fbx meshes and use originals
at L0–2, reduced meshes at L3–5. Preserve source UV seams and roofs during reduction:
chapel25139→19808 triangles, station26881→23902; aggressive collapse visibly smeared
these atlases. Original sources/textures, prepared models/materials/catalog/prefabs
and emission masks remain ignored/private.

Warm pane masks preserve sash; station mask includes both lanterns' glass faces.
ResidentialWindowLights now admits any emissive Buildings entry, allowing civics
without pretending they are growable residences. Two aligned station wall-lantern
light pools share the existing day/evening/night controls. Constable retains source
skinning, Walk/Run/FoldArms takes, plus a quiet authored Idle; catalog preparation
builds the existing generic controller/prefab at1.72m. No patrol/policing simulation
or automatic officer traffic was added.

CivicsQA in the private QA checkout checks catalog filters, dimensions, reduced mesh
registration, L0/L2↔L3/L5 switching, both-tier emission and noon reset, lantern pools,
all four character states and moving baked skin, and lot JSON references. Preview
renders and verification live in ple-2/outputs/Civics. Batch pose review bakes the
sampled skin to avoid stale GPU skin matrices across multiple renders in one editor
frame. No player map opened or saved. Source changes remain local/uncommitted.

--- Earlier road-edge correction ---

# Exposed road edge correction and L0 panning · November 18, 2026

User clarified the darker brown request refers to exposed earth below sidewalks,
not the roadway. Removed the mistaken road-surface tint trial; authored pavement
colors restored. Shared-footprint earth wall shading now multiplies its lining by
(.783,.774,.801) on road ribbons and junctions. Masonry curbs and lot paths retain
their existing appearance. L0 pan multiplier is2.1125 (previous1.69), exactly25%
faster for keyboard/edge and drag panning; other zoom levels unchanged.

--- Superseded roadway tint trial ---

# Darker dirt tint trial · November 18, 2026

User approved trying a slightly darker, less yellow brown after accepting the
six-inch visual recess. Dirt Road, Wagon Road and Founders Dirt Street now multiply
their existing material tint by(.783,.774,.801), including the user-requested
additional10% darkening of the first(.87,.86,.89) trial. Junction blends inherit the same tint
from their source material. Texture/rut artwork and recess geometry/shading remain
as approved; this is a material-color trial awaiting visual feedback. No new render
work or licensed artwork. Before/after loop renders are in ple-2/outputs.

--- Approved six-inch recess baseline ---

# Six-inch visual road recess · November 18, 2026

User approved the visual recess proposal for roads and streets. District road
materials now use LotPathBlend at exactly .1524m virtual depth, with earth-cut
lining on natural roads and curb lining on paved surfaces. Sidewalks remain at
their prior grade. Junction blend materials use the same virtual depth/boundary.
Road/terrain/collision meshes, vehicle/pedestrian heights, saved records and water
levels are unchanged: this is a visual recess, not physical excavation.

StreetSurfaceMask.BoundaryEdges extracts only exposed union edges (including
rounded junctions and old edge-touching joins), so no internal curb spans a T or
crossing. DistrictRoadRecess caches a shared signed-distance RFloat texture at
nominal .5m samples, capped1024x1024/4MiB. Two shader lookups trace the virtual
bed/wall. Field rebuilds only when road geometry/surface signatures change and
is freed on district teardown; ordinary refreshes reuse it. The small six-road
fixture field was106x112/47,488bytes and rebuilt in about26ms in batch QA; this
is not a full-town GPU/FPS benchmark. Existing road meshes/draw calls are reused.
Explicit sidewalk/junction sorting orders preserve the overlap fixes after roads
switch to the blended recess shader. Legacy lot-path recess rendering is retained.

RoadRecessQA passes exactdepth, shared fields, exposed T/duplicate-strip boundaries,
dirt/wagon/gravel/masonry/curve/junction shader renders, unchanged sidewalks/geometry/
road JSON, field reuse and cleanup. SidewalkOverlapQA still passes. Before/after
renders are in ple-2/outputs. Main Unity was stopped at MainMenu before editing.
Source-only RoadRecess.hlsl is allowed in .gitignore; no licensed art added.
Changes remain local/uncommitted on feature/deterministic-street-junctions.

--- Earlier grass-palette update ---

# Grass Texturize palette · November 18, 2026

User clarified grass ground textures in the palette. Added four existing distinct
textures to Natural/All: Lush Grass, Patchy Grass, Tileable Grass and Worn Grass.
Maintained Grass is identical to Lush, so no duplicate swatch. Reuses private ground
textures via LotPathMaterial's optional textureResource path; no new/copied art.
Natural entries require available source textures. Existing entries/order remain;
current private palette totals26. Both Parks and Lot Editor use the same collection.
TexturePaintQA passed existing palette scrolling/count, paint/erase, shader/render,
rotation, undo/redo and save roundtrips. Unity was stopped at MainMenu before edits.
Changes remain local/uncommitted on feature/deterministic-street-junctions.

--- Earlier sidewalk correction ---

# Sidewalk overlap correction · November 18, 2026

Latest source supersedes draw-order-only handling described below. StreetSurfaceMask
indexes world-XZ carriageway/sidewalk footprints and clips presentation meshes:
street sidewalks are physically trimmed out of other street pavement, including
edge-to-edge T joins without an exact centerline junction. Actual rounded junction
footprints also mask sidewalks. Lot paths clip out of street+sidewalk coverage and
fade to zero at the cut over a nominal .55m band. Frontage dirt paths also trim at
other roads. UVs, ribbon attributes, original alpha and terrain heights interpolate
at clipped vertices. All district lot paths use the existing blend shader so both
hard and soft authored paths support the mask. Original meshes remain owned by the
lot surface; masks rebuild from them after road/lot edits and opening a district,
and removing a road restores the full path. Saved definitions are not changed.

SidewalkOverlapQA passes five lot rotations, exact hidden-area removal, soft alpha,
repeatable geometry, slope/UV interpolation, actual near-miss T trimming, two
coincident lot paths, unchanged lot JSON, and full restoration after road removal.
Rendered Sidewalk-Join-and-Path-Fade.png is under ple-2/outputs. Prior OddsQA and
street/block regressions also pass with the clipping integrated. Main Unity was
stopped at MainMenu before editing; no player saves or region generation touched.
Branch remains feature/deterministic-street-junctions, local/uncommitted.

--- Earlier district fixes ---

# District odds-and-ends fixes · November 18, 2026

Latest fixes on feature/deterministic-street-junctions (still local/uncommitted):
- Lamp emission now uses owned runtime materials with the emission keyword restored
  on both detail tiers. Catalog validation keeps a nonblack authored emission color;
  runtime day/night property blocks still switch glow off/on. Simple lamp's private
  LanternGlow mask now includes the reduced model's glass UV triangles, removing its
  unlit triangular holes. Art remains private/ignored; originals backed up in ple-2/work.
- Junction dirt uses the existing seamless DirtRoadEarth albedo in world UVs,
  fading back into the original wagon ruts on approaches. This removes both the
  closed-loop UV seam and stretched/repeated grassy shoulders at joins. RoadShoulderDetails now excludes native plants from street
  carriageways and sidewalks, with existing streamed tiles invalidated normally.
- Pedestrian visibility is reconciled without the phase-only cache. Returning actors
  refresh retained animation/bones, including a deferred wake when zoom-hidden.
  Time changes refresh visibility immediately. Night reductions and vehicle restart
  guards remain; actors are preserved rather than replaced each morning.
- Lot paths use a bounded .075–.080m lift, soft frontage paths share it; district
  sidewalks use .12m and an explicit later render queue. Junctions render after
  sidewalks, preserving clear junction mouths. Normal path layering stays below them.
- District Save/Back ColorTint now targets the button background, with explicit
  highlighted, pressed, disabled and reset states.

Isolated OddsQA passes four complete night/day cycles, actor identity/skin/animation
return, same-phase recovery, vehicle cooldown, street/sidewalk plant exclusion,
closed-loop UV continuity, actual path mesh height/queue and Save/Back pointer hover
and exit. Both lamp tiers rendered and inspected. Existing junction, Founders street,
block corner and twelve block overlap cases pass. Player saves were not modified;
main Unity was stopped at MainMenu before edits. New QA renders/logs are under ple-2.
No packaged-player or full-town benchmark claim. Await user gameplay feedback.

--- Earlier street-junction handoff ---

# Street junction first pass · November 18, 2026

Latest street behavior supersedes the earlier manual-road join description below.
PR26 is merged (verified November18). Current local branch is
feature/deterministic-street-junctions; changes are uncommitted and not pushed.
Unity was verified stopped at MainMenu before editing; no player region regenerated.

Streets now opt into PointWire release placement: release on a green snapped
centerline square to finish a T, drag beyond the snap lock to cross. Click waypoints
and Enter still work. Saved-order targets, endpoint preference and hysteresis make
selection stable. Exact centerline junctions reconstruct explicit nodes/arms on
load; no new save field or historical near-miss repositioning. One terrain-following
patch per node adds small quadratic inner-corner flares and blends original street
albedos. Normal roads retain their previous interaction/rendering. Existing sidewalk
ribbons remain. Invalid/space-constrained flare geometry is omitted. See
Documentation/03_Technical/Street_Junctions.md.

Isolated StreetJunctionQA passes topology, release/drag/click/cancel handling,
actual three-node presentation/shader render, stable snapping, overshoot handling,
green overlay, binary region roundtrip and teardown. Existing FoundersStreetChecks,
BlockCornerQA and all twelve BlockOverlapQA cases pass. This is a first visual pass,
awaiting user gameplay feedback; no packaged-player/performance qualification.
QA renders are in the November18 ple-2/outputs folder. Player saves and licensed
art were not modified. Source-only shader is explicitly allowed in .gitignore.

--- Previous session summary ---

# CityForge V4 session wrap · November 18, 2026

Read AGENTS.md, then this summary before using the historical entries below. This
summary is authoritative where earlier step-by-step notes describe superseded behavior.

## Repository and delivery

- Project: /Users/joelinstrum/dev/CityForge - V4
- Unity:6000.1.12f1. Branch:feature/r2-low-wealth-import.
- Base:main at98c8ec7 (merged PR25). Session changes are being delivered as one PR;
  consult GitHub for its current merge status rather than assuming it was merged.
- Public repository is source-only. All prepared/licensed models, masks, textures,
  catalogs, generated animation assets and saved maps remain in the private workflow.
- Original player maps and lot libraries were not modified for QA. Unity was stopped
  at MainMenu for the final source refresh. Do not automatically regenerate a region.

## Completed and current behavior

1. Ten R2 low-wealth Founders buildings imported locally, with reduced LODs, window
   emission and width/height/depth in meters. Six had removable detached artifacts.
   Low Wealth Res Trio2 is now25% larger:16.719m wide ×7.991m deep ×11.250m high.
2. R1/R2/R3 growth has no progression/population/service unlock requirements. Matching
   saved layout, wealth/density, frontage, dry ground and physical fit still apply.
3. Four Founders low-wealth pedestrians now serve R2/R3 too. Hooligan walking loops
   are repaired. Harass is an assignable nonviolent script: intermittent raised arms,
   evening-only bounded pursuit, faster walking, frightened pedestrians and cheap
   visibility/target checks. Cmd+S → Hooligan harass is a60-second camera-follow test
   that bypasses evening. Esc ends it; Esc no longer navigates back to the region.
4. Low-wealth R2/R3 frontage gets2m soft dirt walking paths. Founders streets have
   one-tile4m roadways, using Dirt, Cobblestone or Weathered Brick.
5. Zones uses the dark/gold modal. Fixed sizes create12-tile frontages in two outward
   back-to-back rows:2×2=12lots,3×3=8lots,4×4=6lots. Outside dirt streets are automatic.
   Overlapping stamps preserve old parcels/buildings/roads and add only complete free
   lots. Corner joins now retain the full road width. Auto and Erase remain available.
6. Tab cycles45-degree steps. Cardinal angles use normal persistent zoning; diagonal
   angles directly place matching saved growable layouts as native rotated lot records.
   The user confirmed diagonal appearance AND connections to manual roads work well.
7. A lot entrance marker is a placement guide. Authored door metadata is used when
   present; otherwise the front-edge midpoint is inferred. Missing doors no longer
   disqualify the entire growable building list. Physical fit remains enforced.
8. Texturize is available in Parks and Lot Editor → Textures → Texturize · Polygon
   brush. Click an outline, click its first marker to close, then spray/erase within
   the hard mask with a soft brush. New boundary keeps the paint; Done/Esc hides guides.
   Paint is stored in sparse compressed32m weight tiles at25cm resolution, up to8
   materials per tile. It is not a mesh per polygon/stroke. Persisted instances share
   weight textures. District paint is world-XZ; lot paint is local and follows45° lots.
   The user confirmed painting works nicely. The last fix replaces hidden six-item
   pages with an All-default scrolling palette, All/Natural/Paving filters, a scrollbar
   and total count. All22 currently available Lot Editor path textures are accessible.
   See Documentation/03_Technical/Texture_Paint.md for limits and render/data details.

## Explicit remaining work / next-session candidates

- Persistent DIAGONAL EMPTY ZONING / REGROWTH is not implemented. Diagonal stamps use
  matching built lot records and construction animation; demolition does not leave a
  diagonal growable parcel. Preserve this distinction; do not claim it is finished.
- Police dispatch / offender disappearance is a future design. Harass currently has
  no violence, damage, police arrival, arrest, crime economy or off-district simulation.
- User feedback on the new full scrolling Texturize palette is pending. There is no
  reported outstanding paint bug. Keep its polygon hard and brush soft as requested.
- Old already-saved clipped street segments are not retroactively repaired.
- R3 growth support does not invent production R3 models. Other C2/C3/industrial
  growth was not enabled by this residential work.

## Validation and how to resume

- Isolated project:/Users/joelinstrum/Documents/Codex/2026-11-15/we/work/gazebos/CityForgeQA
  ProductName:CityForge Holly Brook QA. Do not substitute the normal player data path.
- Scratch/logs:/Users/joelinstrum/Documents/Codex/2026-11-18/ple/work
  Visuals:/Users/joelinstrum/Documents/Codex/2026-11-18/ple/outputs
- Source is synced to QA with rsync -a Assets/CityForge/Scripts/ <QA>/Assets/CityForge/Scripts/
  Do not delete its disposable diagnostic wrappers. Several wrappers are intentionally
  QA-only, not public source. Resource shader:UI/TexturePaintGround must be copied too.
- Batch editor:/Applications/Unity/Hub/Editor/6000.1.12f1/Unity.app/Contents/MacOS/Unity
  Use -batchmode -projectPath <QA> -executeMethod <Class>.Run -quit -logFile <scratchlog>.
- Passed:residential growth, pedestrian loops/pools, Harass scheduling and follow,
  Founders streets/footpaths,108 block geometry combinations,12 overlap/corner cases,
  twelve diagonal size/angle cases and native rotation save roundtrip,192 entrance
  guide alignments, and instantiated Trio2 size/LOD/emission checks.
- Latest TexturePaintQA.Run passes concave masks, blend/erase, eligibility, layer cap,
  corrupt-data rejection, compression,4rotations, undo/redo, actual .cflot/.cfregion
  file roundtrips, both menus, lot tool arm/close, exact full palette count, scrolling,
  fixed mesh counts, shader/render review and paint persistence without the outline.
- These are targeted isolated checks plus the user's in-game feedback, not a full
  packaged-player build or live-town performance benchmark. No GitHub CI is configured.

--- Historical implementation notes follow; newer summary above takes precedence ---

# CityForge V4 handoff · November 18, 2026 · Full Texturize palette browsing

User could not find many Lot Editor path textures in Texturize. Both pickers already
used LotPathMaterial.All/IsAvailable; the first version hid most materials behind
Natural/Paving tabs and six-item pages. TexturePaintTool now opens on All, has All/
Natural/Paving filters, and shows every eligible material in a scrolling thumbnail
grid with an explicit scrollbar and total count. No page truncation. Current private
library has22 available materials. Paint data/material behavior is unchanged.

Isolated QA checks exact palette count against the Lot Editor source collection,
scrollable content, existing paint/save/rotate/menu checks and a rendered review.
Main Unity was stopped at the menu before source editing; no live map was changed.

--- Previous polygon-paint implementation ---

# CityForge V4 handoff · November 18, 2026 · Texturize polygon painting

Implemented the user's polygon-mask texture painting idea with existing path textures.
Parks → Texturize; Lot Editor → Textures → Texturize · Polygon brush. Click marker
points, click the first to close, then hold to spray. Natural/Paving swatches, brush
diameter/falloff, Paint/Erase, New boundary, Undo stroke and Done controls share a
single dark/gold panel. Done/Esc hides all editing guides while preserving paint.
PointWirePath/PointWireGeometry supply closed guide state and polygon validation.

GroundPaintData/Renderer store compressed sparse tile weight maps, eight materials
per32m tile at25cm resolution. One terrain mesh per tile, no polygon/stroke meshes.
Identical persisted weight textures are shared across repeated lot instances. District
paint is world-local in SavedRegionInfo.groundPaint; lot paint is local in LotDefinition.
groundPaint, rotates with lot edits and with native45-degree placement. Water/camp/
district eligibility applies to district brush samples. Existing terrain baselines,
private art/catalogs and player saves were preserved. Live Unity was already stopped
at the main menu before source edits, so no transient map checkpoint was needed.

QA: isolated Unity checks pass for hard concave masks, soft blending/erase, multiple
textures, palette limit, data validation, compression, four rotations, undo/redo,
actual region and lot-library file roundtrips, both menu entries, lot-tool arm/close,
shader rendering, fixed tile mesh counts and paint visible after the boundary closes.
Rendered review images are under the current chat's outputs/Texturize. Test-only lots
and region snapshots were deleted after checks. See Documentation/03_Technical/
Texture_Paint.md for architecture/limits. This is a first version for user testing.

--- Previous experimental diagonal neighborhoods ---

# CityForge V4 handoff · November 18, 2026 · Experimental 45-degree blocks

User requested trying45-degree pre-built neighborhood rotation. Tab now cycles eight
angles. Even steps retain the existing square-grid zoning implementation. Odd steps
use TownDiagonalBlocks: rotate parcel centers and the street loop45 degrees around
the quarter-turned plan center, preview actual rotated lot footprints, validate actual
oriented lots, clip occupied streets, join their corners, and place native DistrictLotRecord
objects with diagonalRotation=true. Existing save support already preserves these
records and their diagonal angles; no schema or old save conversion was needed.

EXPERIMENT LIMIT: diagonal stamps materialize matching authored growable layouts
and buildings directly, then play the normal construction animation. They do not leave
persistent diagonal empty zoning and will not automatically regrow after demolition.
They require an eligible ordinary (non-corner) growable layout of the selected size,
zone and wealth. Unsupported combinations show a matching-layout hint. The user
was told this limitation before completion. Grid-aligned stamps still behave as before.
No existing region or lot library was modified. Growth templates are cached while the
tool is armed, invalidated on its next activation. Both roads and lots validate before
mutation, with native road/lot collision, water, grade, camp and district checks.

Isolated Unity QA passes all twelve size/diagonal-angle combinations: exact lot counts,
45-degree street geometry, rotated preview coverage, partial overlap, and native lot
rotation JSON roundtrip. Existing corner and overlap regressions also pass. QA used
temporary uniquely identified layouts in its isolated library and removed them after.
Interactive appearance and diagonal pedestrian routing still need an in-game check.

--- Previous growable entrance guides ---

# CityForge V4 handoff · November 18, 2026 · Growable entrance guides

The user clarified that a growable layout entrance is a general guide and should not
require pre-authored per-building doors. Removed the Missing entry point rejection
from LotGrowableBuildings.PlacementProblem. Authored doors still align precisely;
missing/dimensions-only metadata now infers the midpoint of the model's front footprint
edge using FrontageOffset, then rotates/translates it onto the layout entrance guide.
No-guide layouts retain buildingCenter placement. Physical fit/collision checks remain.
The building picker no longer treats missing door metadata as a special exclusion;
the coordinate heading now says Entrance guide coordinates. Reopen the picker to
recalculate eligibility. No player lot or building-info records were changed.

Isolated Unity checks passed192 combinations across frontage, quarter-turn, reverse
facing, absent/dimensions-only/exact metadata, guide alignment, no-guide placement,
and rejection of actual out-of-lot fits. Temporary metadata used unique IDs in QA's
isolated persistent data and was cleaned up.

--- Previous Res Trio 2 enlargement ---

# CityForge V4 handoff · November 18, 2026 · Res Trio 2 enlargement

User requested Low Wealth Res Trio 2 enlarged uniformly by25%. Catalog span is now
16.719370m (previously13.375496m). Prepared placement dimensions are width16.719m,
height11.250m, depth7.991m. Updated the local ignored catalog and its ImportInfo.json;
near/distant meshes, UVs and window masks stay the same, with runtime fit scaling
handling both LODs. LotCatalogSetup now refreshes this existing entry when its footprint
changes, so future catalog preparation retains the enlargement. No lot definitions or
player saves were rewritten. Reselect/reopen a preview to rebuild an already instantiated
model at the new size.

Isolated Unity verification passed: actual instantiated model bounds match all three
new dimensions within1cm; catalog measurements, distant prefab and emission map
remain connected. Main Unity scripts refreshed and compilation completed.

--- Previous street corner completion ---

# CityForge V4 handoff · November 18, 2026 · Street corner completion

New neighborhood extensions now join touching proposed street runs into continuous
polylines before meshing, preserving mitered outside corners. The previous overlap
implementation split every side into a separate flat-ended ribbon, leaving half-width
squares of grass exposed at turns. Open endpoints meeting an existing perpendicular
street now reach its far edge by half that street's width, closing shared-road corners.
These changes affect newly prepared/committed stamps; existing saved road records are
not retroactively rewritten. Normal final road clearance still validates the completed
geometry. Building triangle clearance uses the same physical 1mm tolerance as other
road/lot checks to avoid false wall collisions from floating-point miter arithmetic.

Isolated Unity checks pass for all endpoint orientations, outer-corner mesh coverage,
closed-loop seams, preservation of intentional gaps, shared-road endpoint completion,
and all twelve overlap size/rotation fixtures (including building overhangs).
No player save was edited. Work remains local on feature/r2-low-wealth-import.

--- Previous overlap implementation ---

# CityForge V4 handoff · November 18, 2026 · Overlapping neighborhood extensions

The user confirmed block zoning works but requested overlapping extensions after a
street/building clearance rejection beside an existing neighborhood. PrepareZonedBlock
now keeps complete free parcels from a stamp, skipping candidate parcels that overlap
existing zoning, developed lots, or roads. Existing cells, anchors, facing, wealth and
buildings are untouched. Automatic streets are tested in one-tile spans and omit spans
crossing old zoning/lot footprints or actual building geometry. Clear spans merge before
normal road reuse; entirely clear blocks retain the original closed street ring.
The preview shows only new complete parcels and new street pieces, with the actual
new-lot count. Click repeats the same preparation and atomically validates the road
batch before adding cells. Fully occupied stamps give a no-room message. Dry-ground,
district, camp and capacity restrictions still apply to added infrastructure.

Isolated QA exercises all 2×2/3×3/4×4 rotations: clear rings, partial zoning overlap,
developed-lot overlap, zoning in the street margin, building overhang clearance,
existing street reuse, occupied-stamp rejection and no existing-state mutation.
No player map or library is saved. Changes remain local on the feature branch.

--- Previous complete-block implementation (overlap rejection superseded above) ---

# CityForge V4 handoff · November 18, 2026 · Back-to-back zoning blocks

Fixed Zones sizes now place complete blocks, following the user's clarification that
12 means tiles along each street frontage: 2×2 has six lots per row (12 total),
3×3 has four per row (8 total), and 4×4 has three per row (6 total). Two adjacent
rows face outward, with no street between their backs. Each block creates a closed
one-tile Founders dirt street ring outside the zoning. Existing road spans are reused
through the normal road clipping/commit path. Auto retains legacy drag zoning; Erase
arms the existing erase tool. Tab rotates the preview; Esc returns to the Zones panel.

ZonedBlockPlan builds exact parcel anchors and facing metadata for every rotation.
TownZonedBlocks validates zoning, terrain, lot conflicts, district boundaries and the
street proposal before committing the road batch and cells. Existing zoning in the
block or street margin is rejected. Preview rebuilds only on hovered-cell, rotation or
navigation-state changes. Save validation now accepts exact footprints and facing for
commercial/industrial cells too. Growth still requires a matching authored zoning
layout; this does not invent templates for unsupported sizes or unlock C2/C3/industry.
No player map or lot library was edited. Work remains local on feature/r2-low-wealth-import.

Verification: isolated Unity QA geometry checks cover 108 size/rotation/zone combinations,
parcel counts, complete coverage, outward facings, save validation and street ring geometry.
Modal controls are checked in the isolated QA project. Full interactive placement and
subsequent building growth should also be checked in-game.

--- Previous appearance-only implementation (superseded by utility wiring above) ---

# CityForge V4 handoff · November 18, 2026 · Zones modal visual redesign

The user explicitly requested an appearance-first redesign from Downloads/Dark Zones
Selection Panel.png, with utility wiring deferred. Zones now opens a large rounded
dark/gold panel above the existing toolbar, with serif headings, Residential/Commercial/
Industrial icons, Auto/2×2/3×3/4×4 footprint cards, Low/Medium/High density cards,
Erase and Select & Place footer buttons and a live visual selection summary.
Footprint counts say tiles rather than lots to match the game's one-lot footprint model.

Type/size/density callbacks only update local UI highlights and summary. Select & Place
and Erase are deliberately unconnected in this visual pass; the former compact zoning
picker is replaced. No new zoning utility, growth rule, erase behavior or saved-map
mutation was introduced. Hook the new controls to the existing zoning methods in a
later user-authorized utility pass. The close action still closes the town panel.

TownZoneModal.cs holds the layout. ZoneModalArtwork.cs provides cached native rounded
skins and gold code-drawn icons, without new licensed/raster art dependencies. Existing
bottom toolbar assets remain unchanged. A single owned serif font is reused per view.
The Zones panel has its own size/position and title/close button; other town sections
retain their existing layout.

Isolated Unity QA at the 1920×1080 game reference verifies all icon meshes render,
13 controls exist, selection highlights/footer update, and Place/Erase do not arm
zoning. Two selection-state renders were visually reviewed and are in this chat's
outputs/Zones-Modal. The main editor compiled; original region/lot libraries were not
saved. The user should review the appearance before the utility pass.

---

# CityForge V4 handoff · November 18, 2026 · Founders street surfaces

The Transport/Roads popup's Streets category now offers Dirt Street, Cobblestone
Street and Weathered Brick Street in the Founders era. Each carriageway is one tile
(4m) wide; sidewalks remain additional roadside space and honor the low-wealth
R2/R3 dirt-footpath treatment. Dirt Street retains its existing stable ID and
WagonRoadAlbedo texture/along-road mapping. New IDs are founders-cobblestone-street
and founders-brick-street, appended without changing older road indices or IDs.
They reuse cobblestone-road-9 and damaged-brick materials, with source-only fallbacks.

All three support commercial frontage, normal road tools, extensions, repaving,
sidewalk generation, saved-record validation and loading. The popup uses actual
material swatches, and later-era Paved Street remains available in its existing eras.
The commercial frontage hint now asks for a street rather than specifically dirt.

FoundersStreetChecks verifies width, IDs/indices, frontage, classification and save
roundtrip. Isolated QA uses the actual road builder/materials and menu callbacks,
verifies Founders and later-era choices, and renders the surfaces and menu. Outputs:
this chat's outputs/Founders-Streets. No player maps or lot libraries were saved.

---

# CityForge V4 handoff · November 18, 2026 · Wider neighborhood footpaths

At the user's request, low-wealth R2/R3 dirt walking strips are now 2m wide.
The edge feather remains approximately 0.36m, leaving about 1.3m of worn center.
Existing plank/bluestone widths remain unchanged. Frontage coverage admits the outer
2m lot band so the widened strip does not disappear at a flush road frontage.
Width/ribbon, blending, adjacency and street-replacement QA passed and previews were
regenerated in outputs/Neighborhood-Footpaths. No player saves were written.

---

# CityForge V4 handoff · November 18, 2026 · R2/R3 low-wealth walking paths

Roadside sidewalk space beside low-wealth R2/R3 lots now displays a softly blended dirt
walking path using the existing ResidentialFootpath/packed-earth palette. Other density,
wealth and opposite-side sidewalk defaults remain unchanged. Corner lots contribute
both street frontages; lot rotations and curved road cross sections remain supported.
The band uses the existing 1m shoulder next to the pedestrian route rather than painting
terrain or editing authored lot paths. Existing towns acquire it on district load.

`NeighborhoodFootpaths` derives frontage coverage from presented lots. Coverage feathers
at outer frontage ends and sums adjoining lots to avoid seams. `RefreshDistrictSidewalks`
filters nearby frontages, reuses district materials, prunes transparent-only triangles,
and inversely masks existing planks/bluestone where the dirt band replaces them.
`LotPathBlend` has opt-in vertex opacity and lightly varied feather width; default
properties preserve existing road/lot path appearance. Dirt remains partially transparent
to reveal the real ground. No per-frame path rebuilding, save schema, terrain repaint,
forest change or finish reroll is introduced.

`NeighborhoodFootpathChecks.Run` verifies density/wealth, all four frontages/eight lot
rotations, road side, feathered ends, adjoining lots and corner frontages. Isolated QA
uses the real sidewalk builder and R2 models, verifies 1m ribbon coordinates and shader
compilation, and checks partial plank-sidewalk replacement on the correct side.
Preview outputs: this chat's outputs/Neighborhood-Footpaths. No original map or lot
library was saved. Live player-town visual acceptance remains with the user.

---

# CityForge V4 handoff · November 18, 2026 · Smooth pursuit and intermittent gestures

The Hooligan follow jitter came from snapping the camera to movement occurring at
10Hz. Pursuit now advances continuously along previously cleared segments while
keeping target decisions infrequent. DistrictHarass runs before the camera's LateUpdate.
The Hooligan camera uses a 0.16-second SmoothDamp; Eagle follow remains direct and
unchanged. New follow targets snap once to frame the encounter, then follow smoothly.

Chase speed is 2.4m/s (ordinary ambient walkers approximately 1m/s; frightened walkers
use 1.6× their own speed). Walk playback is faster. Raised-arm gestures blend up/down
for about two seconds in each eight-second cycle, leaving normal walking arms between
bursts. Raised-arm pose calculations are skipped during the lowered interval. Evening
restrictions, temporary Cmd+S test bypass, Esc cancellation and scene collision
certificates remain intact. No additional geometry probes occur during interpolation.

HarassChecks verifies repeated raised/lowered periods. HarassFollowChecks verifies
smooth camera convergence, continuous movement faster than a frightened pedestrian,
existing scheduling/collision/cancellation rules and steady allocation checks.
The validation is isolated; the user's live town remains the visual acceptance target.

---

# CityForge V4 handoff · November 18, 2026 · Follow camera and Escape

The temporary `Hooligan harass` command now follows its Hooligan at close Zoom 0.
Escape cancels this test before other modal/tool handlers run, clears its test-only
pedestrian fright, closes any district-script dialog, and releases the camera where
it stopped. Timeout/replacement/district departure also release the camera target.
No camera restoration teleport or district navigation occurs on test cancellation.

Escape no longer implicitly calls ShowOverview or ReturnToMenu from the main view.
It still dismisses dialogs, selections and active tools. Use the explicit navigation
buttons to leave a district or return to the menu. Eagle follow remains available
when no Hooligan test is active.

Isolated HarassFollowChecks verifies a moving target updates camera focus, follow
forces close zoom, cancellation is idempotent and preserves SelectedDistrict, test
fright is cleared, and natural expiry releases the camera without navigation.

---

# CityForge V4 handoff · November 18, 2026 · Hooligan district test command

Cmd+S > `Hooligan harass` starts one temporary unarmed Hooligan encounter for 60
seconds near a visible walking pedestrian, bypassing the evening restriction for
that test only. The district clock is unchanged. The victim's hurry response also
honors the short test override. Normal authored Harass scripts remain evening-only.
The command requires an open district, a close view and a suitable clear approach to
a walking pedestrian near the camera. Feedback explains missing prerequisites.
Repeated runs replace the previous temporary Hooligan; expiry or leaving the district
removes it. The actor is not added to PlacedActors/PlacedLots and cannot enter a save.

HarassFollowChecks also verifies the Noon test follows/scares without changing the
clock, has a cleanup deadline and leaves saved population/lot collections untouched.

---

# CityForge V4 handoff · November 18, 2026 · Evening Harass pursuit

Harass now performs its raised-arm dance and bounded random pedestrian following
only during Evening. Other phases lower the arms and idle at the current position.
The Lot Editor preview also gates its dance on the Evening preset. Targets are active,
walking, non-vehicle pedestrians within 24m; encounters are transient and not saved.
A chase lasts at most 12 seconds before choosing again, stops 2m away, and uses short
0.5m collision-certified segments. New lot/road/forest/water changes invalidate the
current movement certificate; blocked approaches wait rather than find global detours.
Nearby victims turn away along their retained circuit and hurry for three seconds at
1.6× walking speed, without damage, combat, route rebuilding or police response.

Performance policy: target searches occur no more often than every four seconds,
inspect at most eight randomly sampled entries, and allocate no candidate lists.
Movement runs at 10Hz; short segment certificates are reused between checks. Scare
line-of-sight checks are limited to once per 1.5 seconds. Visual harassment pauses
outside the close camera view/Zoom 0–2; authored character animators use visibility
culling. There is no background/off-district crime simulation.

`HarassFollowChecks.Run` passes isolated evening-only pursuit, standoff, retained-route
flight direction, capped sampling/probes, offscreen pause, vehicle exclusion, new
blocker rejection and 10,000 steady active/inactive updates without managed allocation.
These checks are not a live-town FPS benchmark. The original saves/libraries were
not written. Police arrival and disappearing offenders remain future work.

---

# CityForge V4 handoff · November 18, 2026 · Harass character script

`LotScriptedBehavior.Harass` appends saved enum value 4; previous values remain intact.
Assign through Character Script > Scripted > Harass. The Hooligan uses a looping
in-place shuffle with raised arms, wrist/arm waves and torso/heading sway. The behavior
is stationary and nonviolent; it does not chase, attack or move through other lots.
No crime economy, police dispatch, arrest or disappearance system is implemented yet.

`LotHarass` shares the pose behavior between `LotCharacterPreview` and placed-lot
`DistrictHarass`. Preview refreshes use authored yaw rather than accumulating sway;
paused previews defer animation startup. Selection rejects unsupported arm skeletons.
`HooliganHarassSetup` prepares a private unarmed mesh for this behavior, preserving
source skinning, UVs, bones and animation data. The source character and other behaviors
retain their original art. The generated HarassUnarmed.asset remains under ignored
Resources/Lots/V3Characters/Hooligan. Prepare local catalog rebuilds it if missing.

`HarassChecks.Run` checks enum compatibility, save roundtrip, unarmed mesh assignment,
raised hands and varied motion over 20 seconds without moving the authored origin.
Isolated preview renders are in this chat's outputs/Harass. Police arriving and making
the character disappear is the user's intended later design, not a completed feature.

---

# CityForge V4 handoff · November 18, 2026 · Founders pedestrians and looping walks

The four low-wealth Founders pedestrians are present in the private catalog:
Cook, Stout Townsperson, Townsperson, Privateer. Their ambient pool had been limited
to R1/C1; R2/R3 now use it too. Wealth and era matching remain unchanged. Existing
established actors still keep their routes on unrelated lot edits; reopen the district
to rebuild an already populated neighborhood with the corrected pool.

Street Hooligan's Walk state referenced `Armature|preset:biped:walk` with loopTime=0,
while another walk take had looping enabled. `AddCharacterModelState` now repairs the
actual bound clip even when a Walk state already exists; it does not simply select
the first matching take. Generated prefab/controller/FBX import settings remain private.
`PedestrianTrafficChecks.Run` verifies all four characters spawn for low-wealth
Founders R1/R2/R3/C1, preserves traffic identities on unrelated refreshes, and checks
physical skeletal motion after multiple walk cycles for those four plus Hooligan.
Isolated preview captures and verification live in the current chat's outputs/Pedestrians.
No original region or lot library was saved by these checks.

---

# CityForge V4 handoff · November 18, 2026 · Residential density growth

R1, R2 and R3 now enter the same Founders-era town-growth pipeline with no density
progression, population, demand or service unlock. Matching saved zoning layouts and
building density/wealth, dry usable space, street frontage and existing building-fit
rules still apply. C1 behavior is preserved; other commercial/industrial growth is
unchanged. R2/R3 now share residential exact-size parcels, authored-depth selection,
wealth-boundary enforcement and automatic entry paths. Existing saved zoning is
checked when reopening the district; no repaint or save migration is required.

`ResidentialGrowthChecks.Run` verifies all residential densities, scheduling,
parcel/wealth/density boundaries, exact-parcel save restoration and candidate selection.
R3 candidate checks use a transient test tag because no production R3 models were
supplied. The isolated QA library check copies ShantyTown I and verifies all five
allowed R2 buildings plus the authored 3×3 parcel option. Original player lots and
regions are untouched. See the local R2-Residential/Growth-Verification.txt output.

---

# CityForge V4 handoff · November 18, 2026 · R2 low-wealth residential imports

Branch: `feature/r2-low-wealth-import`, based on merged PR #25 (`origin/main`).
Ten private building imports from `/Users/joelinstrum/Downloads/low-wealth-residential-II`
are registered as Founders-era Residential2 / Low wealth. Original archives are untouched.
Private prepared assets live in `Resources/Lots/V3Buildings/R2*` with `Model.fbx`,
`Distant.fbx`, `Color.jpg`, `Normal.jpg`, `Windows.png`, and `ImportInfo.json`.
Detached artifact components were removed from six buildings; attached lean-tos,
barrels, woodpiles and the collapsed façade remain. The misspelled source archive
`low-welath-apartment-collapse` has a correctly spelled catalog ID.

`LotAssetCatalogEntry.buildingDimensionsMeters` persists width X, height Y and depth Z
at placement scale. The building-info editor uses these prepared defaults, retaining
user overrides and its feet display. Sizes are architectural estimates because the
source meshes are normalized, not survey measurements. Per-model private ImportInfo
also records dimensions, triangle counts and removed artifact vertex counts.

R2 uses the existing window-light behavior at evening/night, with daytime emission off.
Selected front panes have UV masks retaining sash detail. Original color/normal maps
remain intact. Near/distant tiers retain the same UVs and physical coordinates; the
reductions retain 65% of source triangles after visual review, preserving
roof/chimney and façade detail. Existing Zoom 0–2 / 3–5 switching applies. R2 zoning growth/tool availability
is separate from importing these eligible catalog buildings; no zoning simulation
expansion was introduced.

Isolated QA and review outputs:
`/Users/joelinstrum/Documents/Codex/2026-11-18/ple/outputs/R2-Residential`.
No player region was loaded or saved. All model assets and the generated local catalog
remain private/ignored; source changes only belong in Git.

---

# CityForge V4 handoff · November 18, 2026 · Extraction, deliveries and route caching

Work on `feature/extraction-deliveries-and-route-caching`, based on `main` after merged PR #24.
Unity **6000.1.12f1**; normal entry scene `Assets/CityForge/Scenes/Gameplay/MainMenu.unity`.
Read `AGENTS.md` and the technical documents below. Repository remains public and source-only:
licensed art, generated catalogs/empty wagon bodies/UI artwork, builds and player maps stay private.

## Current gameplay and user decisions

- Workers and wagons are explicitly authored on a lot. Assign Axeman > Character Script > Scripted > Lumberjack.
  Industrial lots do not generate ambient street pedestrians or visiting wagons. Industrial wealth controls
  ground appearance; it does not restrict growth by neighborhood wealth.
- Vehicle Behavior uses **What / When / Where**. Resource supports all 13 cargo resources; Emergency/Event
  remain None. Time choices are Morning/Noon/Afternoon/Evening/Night; destination currently Town Center.
  Legacy Lumber Wagon saves retain mill-preferred routing. Configured deliveries target Town Center.
- Harbor Council House defaults to a Town Center receiver in existing and new lots unless explicitly overridden.
  The selected-lot panel exposes receiver role and **Detonate**. Detonate removes only the selected lot.
- Wagons load progressively from actual lot inventory, wait for a full load and their departure window,
  drive, unload progressively, then return. Partial amounts and handling progress survive saves.
  Shared cargo visuals cover all 13 resources; new selected cargo requires no new behavior code.
  This does not invent quarry/farm/mine production: non-timber extraction still needs a producer.
- Extraction lots need the selected resource within an **80m sphere**. Timber requires **10 harvestable trees**
  outside the clearing footprint. Placement/move preview is translucent green or red and blocks invalid placement.
  Existing lots are not demolished if their resource is later depleted. Generic non-extraction lots remain exempt.
- Every growable lot chooses fence/path variety **once at construction**. This is mandatory, with no editor toggle.
  Fence replacements preserve source wealth, era and all required shapes; one family replaces the whole set or
  the authored set remains. Full/partial spans are 2.4m/1.2m. No partial-to-full fallback or mixed-family substitution.
  Path pools preserve surface role, wealth, era and geometry; choices persist with the populated lot.
  Fancy textures are high wealth, plain textures medium, dirt/worn textures low. Compacted Gravel Pebble is shared
  across all three wealth categories. Rendering, moving and reopening never reroll finishes.
- Horse/wagon fitting is centered with a 2.3m horse, and turns use braking, look-ahead and smooth heading/articulation.
  Eagle sidewalk pickups exempt the outer lot band from oversized building bounds while retaining interior collision.

## Performance and established routes

Holly Brook in the saved Guilford region, district 19, is the user's primary test town (54 lots, 5,043 trees).
Matched short standalone tests on M1 Max at 3434x1818, small plants enabled, found repeated worker collision-scan
allocation bursts. Indexed asset IDs and cached lot collision footprints removed the principal hitch:
zoom-1 p99 wall frame **179.1 -> 15.8ms**, mean managed allocation **161,469 -> 3,523 bytes/frame**, and no sampled
post-fix pan frames above 25ms. These measurements precede the additional established-route cache below.

During panning, existing reduced models/forest instancing are used earlier. Wide views use 65% render scale,
40% LOD bias and at most two cascades; stationary quality restores after 0.2s. The approved 4096 shadow atlas and
stationary four-cascade policy remain. Wide forest batches coalesce to 256/512m cells, then restore at rest.
No sampled panning window repainted terrain, rebuilt lot presentation, rerolled finishes or completed streamed tiles.
Zooms 4-5 remain below steady 60fps and show long tails; do not claim every level is smooth/locked 60.

`EstablishedRoute` now retains certified paths. Unchanged established movement performs zero route-clearance probes
and no per-tick allocation. After a lot/road/forest/water change, only the approximately one-metre sections being
entered are checked and remembered. An unrelated edit does not trigger a new global route search.
Delivery wagons reuse certified out-and-back geometry across trips; missing/moved/ineligible receivers or encountered
blockers permit replanning. Workers retain sorted tree alternatives and cached rejections, skipping claims/removals.
Pedestrians keep their circuits/progress across layout refreshes; new/removed lots add/remove their own ambient actors.
Caches are transient and reconstructed once on load. Worker navigation remains straight-line: blocked approaches can
be abandoned; blocked returns wait. No general worker detour pathfinder or traffic right-of-way was introduced.

## Verification and next-session priorities

Disposable isolated QA checks passed; no normal player maps or lot libraries were saved:
- `CityForge.Editor.EstablishedRouteChecks.Run`: 10,000 unchanged moves without probes/per-tick allocations,
  local certificates, distant/second edits, blockers, reverse/closed paths, removed roads/sidewalks and pedestrian persistence.
  Includes lumberjack/freight checks, real wagon tree-blocker/removal recovery and repeated delivery route reuse.
- `CityForge.Editor.TownNavigationChecks.Run`: wealth/path/fence checks, 2,000 collision equivalence probes,
  indexed lookup allocation checks, panning/settling/disable restoration, unchanged forest instance counts and batching.
- `CityForge.Editor.ExtractionCaptureChecks.Run`: capture thresholds, 3D/range/footprint exclusions, resource selection,
  deposit discovery, preview cleanup, plus vehicle/cargo/freight/worker regressions.
- `CityForge.Editor.VehicleMotionChecks.Run`: fitting, braking, acceleration, horse heading and eagle sidewalk/interior rules.
- All four original region files remained byte-for-byte unchanged by SHA-256. `git diff --check` passed.

Known independent issue: original and modified development builds report a Metal shader error in
`CityForge/Lot Path Blend`, combined `_ROAD_WEAR` / `_WAGON_SURFACE` variant: duplicate `repeatLength` declaration
around line 200. Unity still reported successful builds and measured views rendered normally. Shader was not modified.
The editor-only texture UI checks also log `Destroy may not be called from edit mode` from runtime picker-row cleanup;
assertions pass and the batch process exits 0. This is a test-harness/UI edit-mode diagnostic, not a clean-console claim.

Next session: begin with live Holly Brook acceptance of delivery cycles, new blockers, street circuits, build-time
finish variety and editor controls. Profile remaining wide-zoom GPU/render load before changing more detail.
Do not reinterpret synthetic tests as live visual acceptance or the earlier performance numbers as post-route-cache FPS.
Preserve player saves and approved forest appearance. Background/off-district logistics, inventory spending/processing,
general worker detours, non-timber production and Emergency/Event behaviors remain future work, not completed features.

Technical references:
- `Documentation/03_Technical/Lumberjack_Workers.md`
- `Documentation/03_Technical/Lumber_Wagon_Scripts.md`
- `Documentation/03_Technical/Lot_Prop_Finishes.md`
- `Documentation/03_Technical/Town_Panning_Performance.md`
- `Documentation/03_Technical/Established_Actor_Routes.md`

The local session handoff in the Codex output folder records exact QA paths, logs, private inputs and the new PR URL.
Earlier handoffs below are historical; the November 18 decisions above supersede conflicting routing/finish behavior.

---

# CityForge V4 handoff · Harbors and Town zoning

The checkpoint branch is `feature/harbors-and-town-zoning`. Use Unity **6000.1.12f1** and open `Assets/CityForge/Scenes/Gameplay/MainMenu.unity`. Read `AGENTS.md` and this handoff before changing terrain or rendering. Richmond is the local test region and Halifax is its capital. Use a copy for tests that edit or save the region.

## Town construction and zoning

- Place the capital's **Founder’s Building** to unlock the **Town / Nature** switch in districts with a town. Nature remains available before founding. The region overview has its own controls.
- **Town → Civic Lots** and **Parks** filter the playable saved lot library and start standard district placement. **Town Info** shows the district’s towns, capital status and placed-lot count. Roads & Transit and Utilities are menu sections awaiting construction tools.
- **Town → Zones** has Residential, Commercial and Industrial densities 1–3. Drag an 8-meter grid rectangle, release to commit, paint over existing zones to replace them, or select **Erase zoning**. The overlay displays while the Zones panel is open. Zoning checks district membership, water and lot overlap. Region saves store sparse cells in `SavedRegionInfo.townZones` and old saves open with an empty zoning map.
- In the Lot Editor, **Zoning layout…** sets use, density and wealth ($–$$$). The blue outline reserves a variable building slot in meters. **Use selected building as slot** copies that building’s footprint and removes the selected model when applied. The saved **Zoning Lots** library is separate from regular playable lots and templates. Slot position and dimensions turn with the lot.
- Next work is to import the buildings the user will provide, tag them by zone use, density, wealth and footprint, and connect eligible building selection to zoning layouts. Preserve the authored decoration, paths, fences and ground layers while filling the slot. Growth, demand and road access are still open design work.

Start with `TownConstructionMenu.cs`, `TownZoning.cs`, `LotZoningLayout.cs`, `LotDefinition.cs`, `LotLibrary.cs`, and `LotEditorPanel.cs`.

## Waterfront lots and harbor traffic

- Author waterfront rows in the Lot Editor. Water starts on the south side by default. **Rotate Lot** turns the complete layout 90° and carries the water-facing edge with it. District placement tests eight orientations for waterfront lots and preserves the authored blue side. Select a placed lot to see its gold footprint and rotate by 45° or click **Move**, grab it and drag to a snapped destination. Existing quarter-turn saves retain their angles.
- Right-click a district on the **region map** for town rename, capital transfer and **Design Coastline**. Deep Harbor changes the district shoreline only. The approved treatment is damp earth and wet rock, a steep shoreline with a minimum offshore depth, slate-gray water, wind chop and animated waves with harbor foam disabled. Beach surf remains available.
- **Transportation → Harbor** and the Waterfront setting **Attracts ship vessels** enable visits. Older waterfront Harbor Council House lots are eligible by default. Routes enter from the district water edge and require a navigable coast or major river. Vessels cycle through the galleon, wooden cargo barge and pirate ship. The large hulls require more depth and clearance.
- Ships berth broadside. When the Harbor Council House porch overhangs navigable water, its measured socket is the preferred berth, with slight hull overlap. Inland placements can get a timber landing and a temporary gangway. Preserve the user's preference for visible direct contact where possible. Pirate visits are ambient. Economy and crime modifiers are future work.
- Harbor Council House defaults to 1.5×, matching two scale increments. Loading an older library copy below that scale enlarges it once, expands its lot if needed, and moves characters already on the deck with it. Already placed district lots are independent records.
- Select an NPC and open **Script → Scripted → Dock Worker**. The per-NPC settings control arrival delay, work time, cargo movement and visibility between visits. Defaults are arrival at berth, 22 seconds of cargo movement, and hidden between visits. Up to two workers use the measured deck lanes. **Characters** mode prioritizes NPC picks and fades buildings to 70% in the editor. The Harbor Council House’s porch has authored character placement heights.

Start with `DistrictLots.cs`, `FounderPlacement.cs`, `DistrictCoastline.cs`, `DeepHarborWater.cs`, `HarborVesselVisits.cs`, `HarborLanding.cs`, `HarborDockWorkers.cs`, and `LotCharacterDeck.cs`.

## Paths and private assets

- The Lot Editor includes street furniture, reusable templates, a path cursor showing center and width, PBR cobblestone and damaged brick, edge blending and a subtle recess treatment. The modern grass mesh trial was retired at the user's request.
- This repository is public and source-only. The private Lot Editor catalog and art are under the ignored `Assets/CityForge/Resources/Lots/` tree. Keep Crest, licensed meshes, textures, region saves, and generated outputs out of Git.
- Harbor prefabs live under `Lots/HarborTraffic`. The V3 barge comes from `Vehicles/WoodenBargeV01`. Local originals are `galleon.fbx` and `pirate-ship` (an FBX without an extension). The prepared galleon has 48,156 triangles near and 13,002 far. The pirate ship has 49,988 near and 13,496 far. Both use 2K textures. Lot Editor transport vehicles are deferred.
- Private path materials live under `Lots/PathMaterials/PBR`. Street furniture models live under `Lots/V3StreetFurniture`. **CityForge → Lots → Prepare local catalog** can rebuild references when the private assets are present.

## Verification

This session exercised district shoreline depth and tint, foam-free waves, Halifax ship routing and direct porch contact, dock worker timing and cleanup, eight-step lot rotation, drag and teardown, the Town unlock and civic placement, all nine zoning tools and save/reopen, and zoning layout classification, slot rotation and library persistence in Unity Play Mode.

---

# CityForge V4 handoff · November 8, 2026

Work in the existing CityForge V4 checkout at `/Users/joelinstrum/dev/CityForge - V4`. The checkpoint branch is `feature/regional-resources-and-capital-founding`. Read `AGENTS.md` and the checkout’s `HANDOFF.md` before changing landscape or rendering. This project uses Unity 6000.1.12f1.

## Gameplay and controls

- The region wizard has resource options and a River and Bay shape. The regional resource overlay shows survey icons and supports filtering, including settlement suitability. Hidden mineral deposits follow the discovery state. `Documentation/03_Technical/Regional_Resources.md` describes the resource model.
- On the region map, **Remake Map** is available for eligible regions without towns. It replaces the generated map after confirmation. **Manage Towns** lets players select, move, or delete a town, even when two markers overlap. Moving a town moves its marker while placed lots stay in place. Deleting a capital promotes the oldest remaining town. **L** toggles town labels in overview and district views.
- The first town is the capital. Founding it opens the capital district and offers saved **Civics → Founder’s Buildings** lots. The picker displays thumbnails, dimensions, requirements, and configured bonuses. It explains blocked choices. Placement snaps the footprint to the grid and checks dry, reasonably flat ground and overlap.
- Placed lots render the saved base texture, overlay paint, and paths. Preview trees receive the district’s ambient daylight fill. An older empty landscaping override now restores authored props. An intentionally cleared lot can be saved again to record the empty override explicitly.
- The district’s **Time of Day** dropdown offers Morning, Noon, Afternoon, Evening, and Night. The region saves the selected preset. Morning light comes from map east, afternoon and evening light from the west, and night has cool moonlight. The district’s direct daylight baseline is 25% lower than before this session.
- Crest coastal water retains shore surf and uses wider, stronger isometric ripple normals at wider zoom. Offshore foam also admits smaller crests. The user approved the broader ripples. Related code is in `CoastlineLandscape.cs` and `CoastalWaterPresentation.cs`.

## Local assets and validation

The public branch is source-only. Keep licensed models and textures, Crest package source, generated output, and player saves in the local checkout. The local Lot Editor catalog uses the founder buildings and landscaping assets under `Assets/CityForge/Resources/Lots`. The Agrarian Assembly Hall is a local saved lot in Unity’s persistent data directory. Its current configuration is a 2 × 2 lot, pasture requirement, and 10% population growth bonus. Richmond is a local save for checking the capital and placed hall. Use a copy for destructive checks.

Focused Unity checks during the session covered surveys and save compatibility, the founder picker and lot placement, placed ground layers, Richmond’s poplar restoration and town edits, district daylight presets, and coastal foam and ripple settings. The scratch Unity project and logs live under the previous chat’s `work/` directory and are outside the PR.

Bonus effects are stored and displayed. Applying those effects to the town simulation is future work. The last visual review also flagged the sharp, bright lawn border and vegetation poking through paving. These are separate from the district lighting and water changes.

---

# CityForge V4 handoff — November 7, 2026

## Lot Editor

Open **Lot Editor** from the main menu. The full-screen developer workspace has
a Back button and starts with an editable 3 × 4 lot. Width and length each
support 1–16 tiles. The right inspector edits name, cost, monthly cost, parent
category, subcategory, and first era. Costs are metadata in whole units.

**Save Lot** writes a local library entry by lot ID. **New** starts a new ID.
**Saved Lots** searches, loads, and deletes entries with confirmation for
deletion or discarding unsaved work. A legacy `Draft.cflot` appears in the same
library. District placement currently uses the most recently saved lot. It
places object prefabs on dry, sufficiently flat terrain and persists a lot
record with the region. The district customization workspace protects
developer-authored content and allows cosmetic object overrides.

- Trees, fences, and characters have searchable thumbnail pickers and recent
  items. Buildings open through era, then use category, then the asset list.
  Transportation has no imported building yet. Character and building ghosts
  show placement under the cursor. Buildings can rotate with R before placing.
  Placed assets support selection, dragging, rotation, and deletion.
- Fence PointWire routes accept open or closed sketches and fill with a chosen
  repeatable fence. Ground paths have 1, 3, 6, 9, and 12 m widths, optional
  rounded corners, material swatches, material changes, and deletion.
- Base textures fill the lot. Overlay brushes paint only the main grid tiles
  crossed by a drag. Terrain mode selects a tile for Delete or 90° rotation.
  Each layer supports 1-tile and 2 × 2 texture repeat. The preview uses olive
  ground, a solid gray surround, and a lighter secondary grid.
- The preview has Day/Night, hard shadows, +/− zoom, and Suspend animation.
  The three Indigenous character prefab roots were scaled up 12% in the local
  private assets, which also affects the camp instances using those prefabs.
- The character Script icon saves per-character settings and previews
  randomized Idle and Walk. Characters can wander, follow a reachable paved
  path, or favor an approach to another NPC. Interaction scans run at most once
  per 10 seconds per character, with up to four candidates per scan. Walk
  animation starts only on a moving frame. Suspend animation pauses simulation.

Start implementation work in `LotEditorPanel.cs`, `LotEditorViewport.cs`,
`LotCharacterPreview.cs`, and `Scripts/Lots/`. The main-menu entry is
`MenuAction.LotEditor`. District integration is in `DistrictLots.cs`.
`LotCatalogSetup` creates the ignored local asset catalog from available models.
The source PR includes the built-in content JSON and mod guide. Mod fragments
load from `Mods/ContentCatalog` beneath Unity's persistent data directory and
use stable IDs. See `Documentation/03_Technical/Content_Catalog_Mods.md`.

Pending work includes district rendering for lot ground textures and paths,
running authored NPC behaviors in districts, Run and building-entry behaviors,
Scripted sequences, and evaluating mod category conditions. The lot's first
era and costs are currently stored fields. District placement has no library
picker or era/economy enforcement. No next feature has been selected.

Validation used Unity 6000.1.12f1 in a disposable project copy. The checked-in
`LotEditorChecks.Run` covers the editor flow. Additional batch checks exercised
the V3 imports, material and terrain tools, animation and NPC behaviors,
multi-lot storage, legacy saves, and valid and rejected JSON mod fragments.
The latest batch log is `work/fence-unity/content-catalog-mod-check.log` in the
November 6 Codex chat workspace. Full district gameplay verification remains
pending.


## CityForge V3 model source for future Lot Editor imports

The local source project is `/Users/joelinstrum/dev/CityForge - V3`. Start in
`Assets/CityForgeV3/Resources/CityForgeV3`, especially `Props`, `Buildings3D`,
`BuildingProps`, `Flora`, `Garden`, `Bridges`, `Boats`, `Railroad`, and
`Vehicles`. V3's `Runtime/World/LotWorldController.Props.cs` (under
`Assets/CityForgeV3`) records the resource paths, textures, and model scaling
used in-game. Import individual models with their texture maps as needed.

V4's first V3 imports come from `Props/PicketFenceV01`,
`Props/WroughtIronFenceV01`, and `Props/WroughtIronVariationsV01`. The local
copies live under the Git-ignored `Assets/CityForge/Resources/Lots/V3Fences`
folder. V4's `LotCatalogSetup` registers them with stable IDs and URP materials.
The two straight panels fill paths. The corner, decorative garden fence, and
gate are standalone pieces.

The main menu opens the Lot Editor through `MenuAction.LotEditor` in
`Assets/CityForge/Scripts/UI/MainMenuController.cs`. Path swatches and surfaces use
local files under the Git-ignored `Assets/CityForge/Resources/Lots/PathMaterials`
folder. The V3 source paths below are relative to its `Resources/CityForgeV3` folder.

| V4 resource | V3 source |
| --- | --- |
| `cobblestone.png` | `LotTextures/CobblestoneSuppliedV01/cobblestone-texture.png` |
| `brick.png` | `LotTextures/BrickPavingV01/brick-texture-1.png` |
| `concrete.png` | `LotTextures/UrbanOverlaysV01/concrete-sidewalk.png` |
| `gravel.png` | `Water/River/BanksV3/open-gravel.png` |
| `packed-earth.png` | `Water/River/02-river-bed-dirt.png` |

The Characters picker uses existing Indigenous camp NPC definitions and private
V3 copies under `Assets/CityForge/Resources/Lots/V3Characters`. The V3 models
come from `Props/Characters/{FarmerV01,HistoricPolicemanV01,VictorianGentlemanV01,HooliganV01}`
and `Characters/DockWorkerV01`. `LotCatalogSetup` prepares the local prefabs
and Idle/Walk controllers. Era groups use the CityForge timeline.

The first V3 building batch uses source meshes and color/normal maps from
`Buildings3D/{WoodenCottageV01,PlymouthStoreProduction,SmallSchoolV01,LumberMillV01,QueenAnneColorHouseV02}`.
The local copies are in `Assets/CityForge/Resources/Lots/V3Buildings`.
`LotCatalogSetup` registers their era and use categories. Check `Buildings3D`,
`Buildings`, and `Industry` for future imports.

## Previous session — region controls and district horizon

PR #12 added player water removal, town founding on the region map, separate
region and town name lists, region suffix selection, and the 25/50/75% coverage
dropdown in Environment. Terrain retains per-district coverage controls. In a
district, the disk and arrow handle save and return, the lower-right panel shows save
status, and I toggles the upper-left district details. Hold C and move the pointer
or scroll to tilt the camera. The slider appears while C is held.

The district horizon is a camera backdrop. It samples the existing region elevation
data for distant silhouettes and adds a blue sky and drifting clouds at low angles.
The default isometric view keeps its floating slab. The horizon adds no neighboring
district instances. Commit `f533b05` is the checkpoint before the horizon work.
Gameplay and editor C# compilation and whitespace checks were run during the
session. The final sky colors and backdrop frame time have not been independently
verified in Unity Play Mode.

The working scene notes and recovery checkpoints below are historical. Use the
current Unity version in `ProjectSettings/ProjectVersion.txt` and the existing local
checkout for private runtime assets. Keep player saves and licensed assets out of
Git.

# District nature editing handoff — November 5, 2026

The working district is Tree Coverage Preview, District 4–3 (index 19), in Unity Play
mode. The latest working snapshot is `Library/BeforeCloseZoomOptimization.cfregion`.
It retains four water records, including both player streams, and 5,936 canonical tree
instances. The district was reopened with its original camera and 375 animals. Use this
checkpoint for local recovery. Earlier checkpoints contain fewer player streams.

District creation supports per-district woodland coverage. The Nature toolbar provides
forest painting and removal, lake outlines, and stream and river routes. Waterway routes
can continue into neighboring districts. Player streams reuse approved stone and shrub
dressing and clear their channel of grass and trees. Land wildlife respects water
barriers, and deer visit reachable freshwater banks to drink. Coastal sharks are ambient
patrols. See the feature notes under `Documentation/03_Technical` for behavior and limits.

The latest performance pass caches water bounds, skips repeat dry-footprint checks,
and groups stream dressing into 48 m render batches. In the live Unity Editor district,
zoom 0 stationary median frame time fell from 74.04 to 11.27 ms and p95 from 85.19 to
11.95 ms. Scripted pan p95 was 10.77, 12.73, and 13.58 ms at zooms 0–2. These are local
Editor measurements. Wildlife checks covered 559 wet samples, 55,500 land-animal
positions, a controlled stream crossing, drinking and return, and zoom transitions.
Raw timings are in `Library/CloseZoomBefore.txt` and `Library/CloseZoomFinal.txt`.
The meadow sun uses lower shadow bias to bring hoof shadows closer to the ground.

The public repo carries source and integration documentation. Restore the private
runtime assets for a full visual check, including the approved mixed forest, stream
rock meshes, UI atlas at `Resources/UI/NatureToolbarIcons.png`, shark assets, and wildlife.
The local deer controller includes the drinking clip prepared by WildlifeImporter.
The private assets and checkpoint are available in the existing local checkout.
Use the current Unity project version from `ProjectSettings/ProjectVersion.txt`.

# Wildlife water barriers / deer drinking — November 5, 2026

Latest ORIGINAL checkpoint: Library/BeforeWildlifeWaterBehavior.cfregion. User had added
another stream before this request:4 original water descriptors. Do not restore older
3-body checkpoints. Original map/camera/season/animal setting restored after tests;
no normal save/commit/push. Wildlife activities/positions are transient and restart.

DistrictWildlife is now partial; WildlifeWaterBehavior.cs owns WildlifeWaterAccess,
physical dry-path sampling and animal activities. All land mammals/ground birds stay
out of water using actual footprints/levels (including immutable player stream grades),
a0.5m clearance ring and <=0.4m path steps. Proposed paths and per-frame steps are checked.
Fish/flying birds/shallow-bank crabs retain distinct habitat rules. Existing animals
covered by newly created water recover to a safe dry bank; bounded failure hides/retries.
WoodlandRevision refreshes trunk indices after water/forest edits.

Deer with the local Drink state occasionally seek a reachable freshwater bank within64m,
walk safely toward it, stop/face water, lower heads for6–10sec, then return to their prior
roaming position. Only ecology restrictions relax for a water visit; water/tree/road/
slope safety stays. Ocean banks are not drinking targets. Cooldown65–135sec; unsuccessful
search retry20–45sec; initial12–45sec. One bank search per tick. No pathfinding/swimming,
health/economy/thirst UI or persistent activity schema. Far zoom still pauses simulation;
drinking animation resumes correctly when showing wildlife again.

Private deer controller/clip prepared with WildlifeImporter.PrepareDeerDrinking using
existing Animal Pack Deluxe Deer_Eat.fbx as the authored head-down motion. Adds looping
Resources/Wildlife/deer-drink.anim and Drink/Drinking to local controller, keeping original
FBX untouched and ignored assets out of public source. Normal wildlife builder includes
this upgrade. Existing WildlifeChecks permits the deer-only state and checks safe movement
in the relevant activity. Other species keep their original animations.

Actual-map tests passed559 wet lake/channel samples, controlled stream crossing (dry
endpoints blocked with water, same path accepted without that water), saltwater rejection,
recovery after water occupies an animal,55500 land positions over30 simulated population
seconds, natural deer approach/drink/return, stationary dry feet,1152 actual moving Walk
animation samples, head lowering1.489m ->0.471m relative to feet, and zoom hide/show.
Source compile and git diff checks pass. Work scripts under current chat work/, report
Library/WildlifeWaterChecks.txt; outputs/Deer-Drinking.png and Wildlife-Water-Checks.txt.
Read Documentation/03_Technical/Wildlife_Water_Behavior.md before extending.

# Player stream appearance / creation feedback — November 5, 2026

Latest ORIGINAL user checkpoint: Library/BeforePlayerStreamRefinement.cfregion. User had
added one player stream;3 water descriptors total. Original working map/camera/season
restored after all checks; no normal player save, commit or push. Test streams exist
only in Library/PlayerStreamRefinementRoundtrip.cfregion. Keep the latest original
checkpoint; older lake checkpoints would lose the user's new stream.

Player streams now use GeneratedStreamAppearance optional player-route adapter:
Pass17 scanned patches,10 individual stones, native seasonal shrub prefabs, actual
cumulative route stations and immutable player grades. Player shrubs use independent
instanced mesh/material batches (runtime material copies), never canonical tree caches.
Fall/Winter/Summer refresh rebuilds this dressing without changing route or stone seats.
Actual existing stream has103 patches/2286 stones/95 shrubs in summer.

PlayerChannelEdits paints full bed coverage (no4% grass residual), approved sparse
StreamBankBlend coverage on dry shoulders and earth on exposed excavated bank faces.
Existing route geometry, levels, width/depth, native generated routing and lakes stay
unchanged. Root clearing includes banks plus2m buffer. Existing canonical/mixed removal
is retained, streamed small-plant tiles now invalidate after creation and exclude the
corridor on rebuild. After editing, player water surfaces/dressing rebuild together so
older bank shrubs aren't lost during forest refresh and extensions don't duplicate.

RiverRouteTool opts into PointWireTool.DeferCompletion. Enter restores normal cursor
immediately, hides sketch, presents Creating stream/river overlay for two frames before
work, blocks map/camera keyboard input, rejects repeat Enter. Validation failure keeps
editable sketch; success clears it; cancellation/teardown releases pending work and
input. Reuses RegionLoadingOverlay.ShowOperation. Closed/lake completion stays synchronous.

Checks passed immediate feedback before mutation, duplicate Enter, invalid route,
success and cancellation cleanup, canonical/mixed root clearing,300 bed samples without
meadow paint, one surface/approved dressing per stream, exact forest/dressing after disk
and injected stale-tree-cache reopening, no-cache generation, seasonal dressing refresh,
original-water-descriptor preservation and source compile/diff checks.6010 original
canonical instances remain after corridor filtering; exact positions outside it retained.
Current chat outputs/Player-Stream-Refinement.png, Stream-Creation-Feedback.png and
Player-Stream-Refinement-Checks.txt contain preview and evidence. Read Player_Waterways.md.

# Steeper player lake basins — November 5, 2026

PlayerLakeGeometry.Apply uses underwaterRun=max(2, saved bank width *0.45). Same maximum depth, outline, waterline, outer dry grading and approved grass/dirt paint. Applies on replay to existing and new player lakes; stored descriptors unchanged. Typical35m run now15.75m. Synthetic terrain probe confirms >3m depth8m offshore and full6m by16m, waterline/distant dry terrain intact. Actual map roundtrip passes unchanged descriptor, forest and basin after reopening. Latest original checkpoint Library/BeforeSteeperLakes.cfregion; map restored from it, temporary roundtrip in Library only; original camera restored. No normal saves or commit. Preview outputs/Steeper-Lake.png.

# Softer lake bank revision — November 5, 2026

User rejected previous water tint and angular dry-bank edge. Removed shallow RGB channel clamp, brightened calm lake scattering to deep (.035,.14,.19), shallow (.10,.22,.23). Shallow reflection now20% strength ramping to full over0.5–3m. Player dry feather3.5–5.5m at lower24–56% weight, using7/19m noise instead of sub-texel strong edging. Original map checkpoint Library/BeforeSoftLakeBank.cfregion restored; camera restored after preview. Compile/shader and full terrain weight normalization checks passed;6116 trees unchanged. No normal save/commit. Supersedes color/feather settings below.

# Restrained lake-edge reference refinement — November 5, 2026

User reference requests less violet at the shallow edge and a thin earthy/pebbled
shore with occasional visible stones and grass tufts. LakeWater.shader now ramps
the constant calm-water reflection strength over0.15–1.5m depth, and gently limits
red/blue-heavy captured highlights below1.75m. Deep calm water and the separate river
branch retain their prior color/reflection formulas. No lighting/material asset edits.

Player-lake dry paint narrows0.9–3.4m to0.7–2.2m; gravel fraction slightly increased.
Random stones now stay within4m submerged /1.3m dry distance. A separate seeded
accent sequence places occasional1.6–2.4m shoreline stones, with the total2048-rock
limit retained. Small grass clumps slightly larger; intermittent placement retained.
No new art or changes to contour, basin, water level, forest or PointWire behavior.

Shader compilation/render checked on actual user's lake. Adapted work check
LakeEdgeRefinementChecks.cs passed halo/native-bank guard, paint normalization,
unchanged geography/forest and deterministic painting/dressing after disk reopening.
Current dressing117 rocks /41 plant clumps. Full preview inspected. Latest original
user checkpoint: Library/BeforeLakeEdgeRefinement.cfregion. Original working map
and camera restored after checks.
No normal saves written; all current and prior work remains uncommitted.

Final lake-bank restoration: original working map and camera reopened from
Library/BeforeLakeBankRework.cfregion;6,116 trees and2 original water descriptors,
new local bank repair applied,196 rocks/42 plant clumps. Play running, no tools armed.
Actual lake full/close previews and reopen report copied to current task outputs.
No normal player saves written; user can review before accepting this appearance.

# Lake bank quality rework — November 4, 2026

User rejected the broad blurred tan/neon-green banks. Root cause: GeneratedRiverDistrict
was enabling PaintGeneratedRiverBanks for any generated watery district, including
lake-only districts. It injected a broad soil0 band (e.g.76% at8m outside the real
shore) underneath lake painting and into canonical saved meadow caches. Width/tint
changes to LakeShoreRenderer alone could not remove it.

Gate ONLY the riverbank PAINT call with HasNaturalRiverBanks (non-player River bounds
overlap); keep GeneratedRiverDistrict water-render routing unchanged, or the general
typed-water fallback can introduce an unwanted ocean surface. Recognition uses bounds,
not RegionWaterBody.Intersects, because centerline-only rivers have no contour edges.
Native river qualification was tested explicitly; player channels remain separate.

New LakeMeadowRepair partial repairs ONLY the24m area around player lakes before
wet-bed layers are captured/painted. Reconstructs normal meadow dry/grass/litter from
existing ecology/canopy, retains extra terrain layers, protects native water, camps,
roads, other lakes and player channels, and updates canonical lake-base maps. It
removes old cached river-soil contamination without rebuilding trees or geometry.

Player lake dressing now has an irregular0.9–3.4m dry edge with small spatial edge
breakup, darker damp shared dirt at5m texture scale, existing pebble/sand textures,
sparser rock clusters and intermittent small reed/grass stands. No new textures or
source-art changes. Water shader/reflections/outline/level/depth/forest unchanged.
Natural lake dressing population retains its previous density; runtime wet-soil
palette is darker. Actual user's lake is used for review, not the oval test fixture.

Work/LakeBankReworkChecks.cs passed: native river guard; old halo gone8–22m out;
nonnegative/normalized paint; unchanged water descriptors, bed and6,116 trees; saved
painting and seeded dressing fingerprint survive disk reopening without accumulation.
Current lake:196 rocks,42 plant clumps. Full and close renders visually inspected.
Latest ORIGINAL user checkpoint: Library/BeforeLakeBankRework.cfregion. Disposable
roundtrip fixture: Library/LakeBankReworkRoundtrip.cfregion. Restore original user
checkpoint after checks, letting local repair run. No normal player saves written;
all changes remain uncommitted.

# Narrower visible lake banks — November 4, 2026

LakeShoreRenderer dry earth/grass feather reduced to40% of its prior width (60%
reduction): typical player lake20m ->8m; natural lake12m ->4.8m, with the same subtle
noise/falloff. Paint rectangle follows that width. Underwater wet-bed coverage,
water outline/level, basin, forest and seeded shore-detail layout unchanged.
Latest user checkpoint: Library/BeforeNarrowLakeBanks.cfregion; restored after checks.
Runtime width8m verified (prior20m), preview inspected. No normal saves written.

Final player-channel restoration: original working map/camera reopened from
Library/BeforeLakeGrassTransition.cfregion, district19,6,116 trees,2 existing bodies;
no test waterways retained. Water / Add body-type choices open, tools unarmed.
PlayerChannelChecks passed again after boundary guards; rendered ribbon/hint and
created stream/river inspected. Normal save files untouched; skill validated.

# Open PointWire streams/rivers and district continuation — November 4, 2026

Water / Add / Stream and River are now live via RiverRouteTool. PointWire OpenPath
uses Enter (or keypad Enter) to finish after2+ markers, with Finished separate from
Closed; lakes still close to their starting marker. RibbonWidth previews6m streams
and18m rivers; hint explains click/Enter/Backspace/Escape and district/end snapping.
Success clears the guides and restores default cursor. Completion errors retain them.

Routes live in RegionWaterBody.playerChannel (not the lake's playerCreated flag).
Normalized centerline and world-height station grades persist globally. Exterior
union edges snap within20m and add64m into a neighbor. Open that district to see it;
start within16m of its downstream endpoint to extend the same body ID/type. Old
stations/grades are immutable. Regional outer borders and internal combined edges
do not create portals; curves cannot cut across excluded union corners.

PlayerChannelSampler shares exact route/grade queries across tiles. Bounded basin
replay and forest/bank exclusion apply on every district/cache/fresh load; baseline
coverage ranking remains unedited. Dedicated bounded moving-water surface uses
matching per-body depth/flow UV domains and grade mesh. Native generated river and
ocean routing explicitly excludes player channels; legacy lake appearance retained.
Relief atlas/mesh include the new water. Read Player_Waterways.md and PointWire.md.
Skill cityforge-pointwire updated for this usage. New sources are private-art-free.

Checks: open/closed lifecycle, actual menu completion cleanup, river19->20 seam
bed32.8145m on each side and identical water grade, one surface, downstream snapping,
identity/station-preserving extension, separate Stream, disk/stale-cache/fresh load
with exact surviving forest positions and no regrowth. No physical input injection
claimed. Work/PlayerChannelCheck.cs and PlayerChannelReopenCheck.cs reproduce them;
Library/PlayerChannelChecks.txt holds results. Saved fixtures are under Library/.

Unity was closed when this turn began (no live map to checkpoint); started the
existing MainMenu scene and tested disposable copies of the latest user checkpoint
Library/BeforeLakeGrassTransition.cfregion. No normal player saves written. Restore
that checkpoint/camera after tests (final restoration recorded below). New work and earlier nature changes uncommitted.
Limitations: downstream-only same-type extension, no confluences/natural-water joins,
removal/width controls or hydraulics. Routes must avoid camps/roads and deep uphill
trenches; shared station/metadata limits are enforced before committing.

# Subtle lake grass transition — November 4, 2026

LakeShoreRenderer no longer adds the ungraded fifth native terrain layer as moss.
Earth/pebble/sand weights retain original meadow through a wider smooth feather,
scaled to outer bank width+6m for player lakes,12m for natural lakes, with restrained
noise rather than a high-contrast threshold. Painting bounds alone expand; keep the
original detail-placement bounds/RNG so rocks/plants/leaves retain their layout.
No height, shoreline geometry, water settings, forest or source assets changed.
Visual preview verified a softer transition without the vivid green ring; sampled
terrain weights remain nonnegative/normalized. Current user checkpoint is
Library/BeforeLakeGrassTransition.cfregion; user map/camera restored after checks
(6,116 trees,2 bodies), Play running. No normal saves written.

November4 final visual: matte base-soil alpha resolved the bright white banks.
Approved-profile reference and final comparison inspected; normal water settings
retained. Source assets untouched. New profile save/stale-cache/fresh replay passed
with identical remaining tree positions. User map restored from
Library/ApprovedLakeLookCurrent.cfregion; its one player lake has approved profile,
unchanged polygon/level. No normal player saves written; use Save District to persist.

Lake visual follow-up: bright white shore highlights came from opaque diffuse-alpha
smoothness in the base soil exposed by shoreline painting. Lake setup now makes
owned matte RGB-preserving copies of base soil and wet-bed diffuse textures, leaving
source materials/textures untouched. Visual diagnostic confirmed the white rim is
removed; do not rely on TerrainLayer.smoothness=0 alone in URP.

# Approved lake appearance alignment — November 4, 2026

Located October12 Lakes-And-Ponds-Final-Report and Lake-Final-Editor-Zoom1 reference
in that session outputs. Player lakes already use the accepted LakeWaterRenderer,
Water/LakeWater material, LakeShoreRenderer and sharp local cubemap settings. The
first-pass3m/8m basin differs from the accepted6m/35m inward shelf /14m outside blend.
New lake creation now scales toward that accepted6/35/14m basin (small ponds scale
down), stamps calm-lake appearanceProfile, and separates playerOuterBankWidth from
inner slope width. Existing saved lake geometry is retained (outer width0 preserves
legacy behavior). Tree clearing follows the actual outside bank width; coverage
ranking remains against original ground. Shared shader/material settings unchanged.
Pre-change checkpoint: Library/BeforeApprovedLakeLook.cfregion. The current user's
one drawn lake was explicitly reprofiled without changing outline/level; no normal
save written. Current checkpoint: Library/ApprovedLakeLookCurrent.cfregion.
New-profile creation, stale-cache and fresh generation checks pass with exact
remaining forest positions (6,091 trees in disposable fixture). Runtime confirms
CityForge/Calm Lake Water, reflection0.20, normal detail0.008 and LakeShoreRenderer.
Forest planting also rejects the whole stored bank clearing, preventing trees that
would disappear on reopening. Original shader and material assets unchanged.

# Lake completion cleanup — November 3, 2026

Successful lake closure now dismisses the lake tool through Wire.Completed in the
LakeOutlineTool adapter. It clears markers, border/string and hint, restores the
normal cursor and collider state, and retains the created lake. Rejected completion
stays armed/editable. Generic PointWire completion behavior is unchanged.
Latest user checkpoint: Library/BeforeLakeAutoFinish.cfregion; preserve its lakes.

# Curved player lakes + reusable PointWire — November 3, 2026

Lake / Pond now commits a rounded lake when the guide loop closes: three Chaikin
passes,3m default basin,8m blended banks, approved calm water and shore materials.
A four-marker guide produces32 shoreline points. Smaller bodies (<5,000m²) are Pond.
New RegionWaterBody.playerCreated/playerDepth/playerBankWidth plus normalized
outline/surface level persist through normal saves. Basins replay before district
terrain application; caches do not own them. Existing water recipes unchanged.

The interaction is now generic PointWirePath / abstract PointWireSurface /
PointWireTool / PointWireGraphic, with PointWireGeometry for consumer-independent
polygon operations. LakeOutlineTool only adapts toolbar/surface/completion. Call it
PointWire; installed and validated skill: ~/.codex/skills/cityforge-pointwire/SKILL.md.
Read Documentation/03_Technical/PointWire.md and Player_Lakes.md. Completed lake undo
is disabled: Backspace edits an uncommitted sketch, Escape clears sketch only.

Validation: simple/noncrossing guide and curve,3–64 lake markers,100m² minimum,
30% district maximum,32 lakes/region,9m district margin,12m existing-water margin,
road/camp clearance,12m maximum shoreline height span. Invalid closure stays editable.
No water merging/removal, configurable depth/level or domain undo yet.

Preserve original-ground woodland coverage ranking: retain pre-lake terrain for
coverage eligibility; filter the8m lake/bank clearing after ranking. A test exposed
extra forest loss if lake water/slopes entered percentile eligibility; corrected.
Disk/stale-cache and fresh generation now both retain exact remaining positions
(6,096 trees in disposable fixture). Native field data itself stays unchanged; user
basins replay from descriptors. Live forest roots outside the clearing retained.
Typed lakes also appear on regional relief at their own water elevation.

Checks pass: pure path lifecycle, invalid guide without mutation, actual menu/wire
completion,32-point lake,>2m water depth, distant terrain unchanged, tree exclusion,
water distance, disk/stale-cache/fresh regeneration, separate small Pond and two-body
reopening. Filled lake and live-string renders inspected. Latest actual user state
checkpoint Library/BeforeCurvedLakes.cfregion (6,116 trees); test lakes are disposable.
Original user checkpoint and camera restored:6,116 trees,967 explicit additions,
59 removal records; Water / Lake-Pond menu ready, unarmed. No normal saves written;
changes uncommitted.

# Lake / pond outline utility — November 3, 2026

Add Water Bodies > Lake / Pond now arms LakeOutlineTool. Blue arrow, click terrain
to place markers; live string follows pointer; click first marker within24 screen
pixels after3 markers to close. Larger starting ring. Backspace removes the last
marker or reopens a completed loop; Escape/tool change/overview cancels and clears.
Toolbar selection alone is unarmed. Cursor/collider states are restored on cancel.
UI/focus/save/loading guards prevent accidental placement. Boundary max256 markers;
terrain drape samples cap32 per edge, keeping the UI mesh below vertex limits.

Transient sketch only: no terrain, water, forest or normal save changes. Closed
sketch remains visible until cancellation, not persisted. Expose Points/Closed for
future lake creation; no basin, water fill, polygon validation or editing backend
is included. Streams/rivers/removal remain menu-only. Boundary uses a code-native
LakeOutlineGraphic overlay (CanvasRenderer required) so strings remain visible over
crowns; marker rings reuse the private-free forest circle shader.

Checks passed: menu arms only Add Lake/Pond, marker placement, duplicates and early
close rejected, close/reopen/undo, cancel/collider cleanup, unchanged forest. Closed
and live-string screenshots visually verified. Original checkpoint is
Library/BeforeLakeOutline.cfregion; camera/test sketch restored/cleared afterward.
No normal saves written; uncommitted. See Documentation/03_Technical/Lake_Outline.md.

# Water editing menu foundation — November 3, 2026

Water now offers Add Water Bodies / Remove Water Bodies, plus/X badges. Clicking
an option or Choose a water body opens Lake / Stream / River choices with Back.
WaterTypesOpen/WaterBody expose transient selection; WaterBodySelected(add,kind)
is a dedicated future backend hook. No water/terrain mutation is implemented yet;
selection feedback makes that clear. Oceans are excluded. Forest callbacks unchanged.

# Woodland option labels and order — November 3, 2026

Forests now lists Add Woodlands first, Remove Woodlands second, Add trees third.
Plus/X badges, help text and brush callbacks follow the new order (add0/remove1).
Forest editor checks updated to match.

# Woodland painting — November 3, 2026

Add Woodlands is live: green tree-plus cursor, shared200/400m circumference brush,
Tab toggle and click/hold/drag. Individual Add trees remains future work. Planting
uses the approved mixed palette, deterministic9.48m district lattice and4.2m minimum
trunk separation; water, steep slopes, roads and camp clearings stay open. Existing
forest objects are preserved; SpatialForestRenderer.Append captures new trees only.

SavedRegionInfo.forestAdditions stores explicit original-district placements, scales,
rotation and selected variant. Merge these after base removal filtering: additions
override older masks; later removal deletes addition records. Merge is idempotent
for cached and fresh generation. Limits:65,536 additions,200,000 canonical trees,
16MiB metadata. Never re-rank the base woodland footprint after editing. Native
legacy/tropical families retain native rendering; that fallback was not separately
visually qualified. See Documentation/03_Technical/Forest_Planting.md.

ForestAdditionChecks passed:36 trees with200m circle,105 more with400m,76 by drag;
no overlap stacking/stationary edits; existing objects retained; typed Larch,
remove/replant ordering, seasons, disk/stale-cache reopen and fresh regeneration
preserve exact populations/signatures. Before/after clump render inspected. Latest
user checkpoint BeforeWoodlandVisual.cfregion preserves5,149 trees and prior user
clearings. No normal player saves written. Changes remain uncommitted.

# Larger continuous forest-removal brush — November 3, 2026

User requested double both brush circles and hold/drag clearing. Default is now200m
circumference (radius31.831m), Tab400m (radius63.662m); old saved masks retain their
original physical radii. Popup help/cursor readout updated. Brush arms only through
Remove, starts a drag only on valid landscape left-down, continues while held and
stops on release. UI-started drags cannot erase on entering terrain; crossing UI or
invalid terrain resets the interpolation anchor, so painting does not join across
blocked intervals. Losing app focus/cancel ends the drag. Tab while held resets the
anchor and applies the new radius at the current pointer.

ForestRemovalBrush.BeginDrag/ContinueDrag/EndDrag share the runtime input path. Motion
is sampled at radius×.25 intervals, interpolating skipped positions during fast moves;
release stamps the endpoint. Holding stationary is a no-op. All samples for one frame
are committed together via batched RemoveForest/RemoveWithin overloads: one canonical
filter, canopy/native refresh, signature/batch invalidation and WoodlandRevision update.
Metadata circle records remain compatible with prior saves. Empty canonical/mixed
changes skip terrain refresh, and circle limits are applied before deletion so every
cleared sample can persist. Save/reopen, stale-cache and regeneration behavior retained.

Disposable checks passed:200/400 toggles,36 then84 trees removed, fast drag7 additional,
no corridor gaps, no stationary edit accumulation, one data revision for a multi-sample
jump, release stopping edits, outside-tree preservation, seasonal/distant rendering,
full disk/stale-cache reopen and no-cache regeneration. Extra CUA native mouse check
was inconclusive: automated clicks/drags generated no Unity Input mouse-button frames
(Watch log empty). Do not claim a physical/native mouse-input test passed. Cursor and
larger ring rendered correctly; code/input-state path and brush APIs tested live.

Private original checkpoint Library/BeforeForestDrag.cfregion captured the user's
existing manual clearings:5782 trees,7 removal masks (prior5832 baseline minus50).
Do NOT restore the earlier unedited checkpoint and discard those edits. Disposable
input-test copy is discarded; latest original restored via staged initialization and
original camera. Current Forests popup open, Remove unarmed. No normal player save
written; work uncommitted. Report Library/ForestRemovalChecks.txt; prior100/200m
section below is historical and superseded for new brush sizes/gestures.

# Forest removal brush and doubled popups — November 3, 2026

User requested popup windows twice their prior size, Forest choices Remove / Add
Woodlands / Add trees, and a functional Remove brush with tree+X cursor, red ground
circle, click removal, Tab toggling100m/200m CIRCUMFERENCE. Implemented. Popup local
scale2 doubles its prior580×350 dimensions. Forests opens without arming; explicitly
clicking Remove publishes OptionInvoked, arms ForestRemovalBrush and hides the popup.
Clicking Trees again reopens configuration/cancels painting. Other category selection,
Escape, overview/loading/disable restore the normal cursor. Add tools remain settings
only. This supersedes the earlier all-settings-only popup limitation for Remove.

Brush radius=C/(2π):15.9155m or31.831m. One left click removes woody canonical trees
whose bases lie inside the circle; no drag painting. Existing approved mixed forest
is individual trees, so no whole-cluster deletion is needed. Native shrub slots11–14,
streamed grass, terrain and existing ground paint stay intact. UI raycasts block
painting; activating Remove cannot fall through into the landscape. Cursor overlays
a gold tree group with redX/size label, hides system cursor only on valid focused
terrain, and restores it over UI/on exit. Red ring conforms to terrain, uses a
main-camera-only overlay shader so it stays visible through crowns and avoids Crest
reflection captures. Temporarily enables the district TerrainCollider while armed
for older maps, then restores its prior state.

MeadowDistrictLandscape.RemoveForest updates canonical full/mature arrays, canopy,
native seasonal presentation and MixedDistrictForest placements/signature. The
SpatialForestRenderer removes captured tree entries and invalidates spatial batches
instead of recapturing/replacing the whole forest. Legacy approved/palm draw paths
also honor cleared areas. Clearing records are additive SavedRegionInfo.forestRemovals
metadata (world center/radius and original member district IDs), validated on read/
write. Old saves default empty. Save District/Region persists edits across districts;
stale caches and fresh generation filter the same masks BEFORE presentation. Rank
coverage first, then filter selected trees, so clearing cannot refill from alternate
candidates. Terrain and ground paint are not regenerated. No undo/add-placement is
implemented yet. See Documentation/03_Technical/Forest_Removal.md.

Resources/UI/ForestRemovalCircle.shader + meta are original source explicitly allowed
through .gitignore; the existing generated icon bitmap remains private/ignored.
New files: UI/ForestRemovalBrush.cs, World/ForestRemovalEdits.cs, Editor/ForestRemovalChecks.cs.

Verification on a disposable clone:100m removed9 trees;200m removed27 additional;
unchanged outside trees; immediate distant submissions and native/mixed counts;
season changes; metadata round trip; full reopen with deliberately stale cache; and
regeneration with no cache all passed. Distant check explicitly uses camera size300
(zoom4 on a1024m cell is below the instancing cutoff). Live native keyboard Tab
100→200→100, red circle, redX cursor and enlarged popup visually verified. Report
Library/ForestRemovalChecks.txt. Private test files under Library/ForestRemoval*.cfregion.
Original Library/BeforeForestRemoval.cfregion restored: current user Tree Coverage
Preview seed642890401 district19 retains5832 trees and ZERO removal edits. Forests
popup is open, Remove unarmed. No normal saves written; source/shader compilation
and whitespace pass. All work remains uncommitted. Legacy/palm removal has code
coverage paths but was not separately visually qualified in this temperate test.

# Nature category popups and option settings — November 3, 2026

User requested a popup per toolbar category: small heading, option icons on the left,
brief explanation on the right, then more settings after choosing an icon. Added
NatureToolPopup, one exclusive reusable580×350 window above the floating toolbar.
Terraforming has ^ / v / = for Raise terrain (Terraform hills & mountains), Lower
terrain (Add valleys), Level terrain (Level out an area), with independent brush-size
and strength/blend sliders. Current district is left with Terraforming/Raise open.

Water: lake/pond/river with area/width and depth. Foliage: individual tree/woodland
brush/ground foliage with type choice and brush size. Animals: individual/herd with
species cycling from actual private WildlifeLibrary entries and herd size. Sun:
Seasons/Weather tabs with existing live season/precipitation actions and availability
rules; original icon row was reparented into this popup. Close X, toolbar toggle and
Escape dismiss the current popup. Only one category panel is visible at a time.
Main toolbar remains icon-only with no background bar; text appears inside popups.

These are transient tool configuration panels, NOT terrain/placement implementations.
NatureToolPopup stores independent per-category/option values and choice indices,
exposes Value/ChoiceIndex and SettingChanged/OptionChanged for future editing
backends. No paint, placement, water-body creation, undo or persistent editing schema
is claimed. No Apply button implies mutations. Existing Sun actions remain live.
No additional generated bitmap was needed; gold atlas reused, terrain glyphs use UI text.

Updated NatureToolbarChecks passed all five popups/options/descriptions, main toolbar
no-label/no-bar constraints, brush slider values and per-option persistence, close,
Sun actions/availability, hidden legacy HUD, and unchanged tree count/signature.
Additional live pointer-input check moved brush slider to76.25 then restored25.
All five window screenshots visually inspected (Terraforming, Animals and Sun in
particular); corrected slider fill tracks to stay thin. Reports Library/NatureToolbarChecks.txt,
images Library/NaturePopup-{Terrain,Water,Trees,Animals,Sun}.png. No normal saves
written. Original user map checkpoint Library/BeforeNaturePopups.cfregion was restored
with full staged initialization. Current map remains Tree Coverage Preview seed642890401,
district19,5832 trees. Source compiles; whitespace passes; changes uncommitted.

# Icon-only nature toolbar — November 3, 2026

User requested a subtle bottom nature toolbar with no text or background bar:
mountain (height/depth), waves (water), trees (foliage), deer (animals), sun
(weather/seasons). NatureToolbar builds five floating gold RawImage buttons at the
bottom center of the district HUD. Hover brightens; selection has a small gold
underline. It hides the bottom instruction text in district view. Overview retains
its existing controls and hides/dismisses the toolbar. Escape dismisses the current
category/flyout first, then a subsequent Escape uses normal region navigation.

Sun has a second icon-only row: summer leafy branch, autumn leaf, winter bare twig;
clear, rain and snow. Buttons use existing SetSeason/DistrictWeather APIs, with
Tropical seasonal and temperature-dependent precipitation availability. The old
right-side season/weather buttons remain built for legacy callback tooling but are
hidden. Terrain/Water/Trees/Animals currently select a NatureTool enum and publish
ToolSelected; actual terrain brushing, water creation, foliage placement and animal
placement/herds are future work. Do not claim those editing backends exist.

Generated transparent1448×1086 atlas, four columns/three rows, saved privately at
Assets/CityForge/Resources/UI/NatureToolbarIcons.png (Resources/UI art is ignored by
source-only Git policy). PNG/importer/meta must be restored with local UI artwork.
Uses clamp/bilinear, no mipmaps/NPOT scaling, uncompressed alpha. Original generated
image remains under ~/.codex/generated_images. Current chat outputs contain atlas,
final generation prompt/tool details and in-game closed/Sun screenshots. Built-in
image_gen used with existing T_Region_Nav_Icons.png as gold style reference; no
licensed reference art is bundled into source control. New C# files are source.

NatureToolbarChecks passed five categories, no Text/background, Sun callbacks,
season/weather availability, dismissal, hidden legacy controls, and unchanged tree
count/signature. Live additional checks passed Tropical availability, overview hide
and cached district reentry. Report Library/NatureToolbarChecks.txt. Original user
region was checkpointed to Library/BeforeNatureToolbar.cfregion before source
compilation, then reopened through full staged initialization (no bank-stage skip
needed for this inland map). Current Tree Coverage Preview district19 restored;
no normal player save written. Work uncommitted alongside earlier session changes.

# Larch foliage lightened — November 3, 2026

User requested slightly lighter evergreens. Updated only the two private prepared
Evergreen/Larch_branches_LOD0.mat and LOD1.mat foliage colors from(.27,.30,.25,1)
to(.32,.36,.30,1), about20% brighter tint. Both _BaseColor and _Color match. Branch
textures, normal strength, cutout, bark, broadleaf materials, lighting/shadows and
population are retained. These shared materials serve approved runtime tall/medium
larches as well as the private source prefabs; rebuilds reuse the references.
They are ignored artwork, not public Git content. Original supplied assets remain.

Live same-camera before/after inspected in the user's newly created Tree Coverage
Preview region (seed642890401, district19), retaining5832 trees and placement
signature. Their original camera was restored after comparison. No source reload,
region regeneration or map-save operation performed. The user independently created
and saved that region while the prior creation screen was open; do not replace it
with the disposable verification map. Initial attempt to open that fixture was
rejected because a region was already open; editor was unpaused and comparison ran
on the existing district instead. Material originals backed up in current chat
work/larch-brightness. Comparison images Library/LarchFoliageBefore/After.png.

# Per-district Tree Coverage creation controls — November 3, 2026

User requested 25%, 50%, 75% Tree Coverage per district during region creation, with
woodland footprints resembling the approved forest tests. This later request adds
explicit coverage choices; it does not migrate the existing Forest Gump region.

Create Region / Terrain now has an 8×8 coverage grid (north at top), a Set All
Districts selector (with Apply to reset custom cells) and clickable per-cell 25→50→75 cycling. Default50%. The old
global Vegetation selector is replaced with guidance to these controls. New wizard
and default-menu recipes use treeCoverageVersion1, default percent50 and optional
per-original-district array; missing/empty arrays use the default. Versions remain
geography12 / forestAppearance1 / waterway5. Missing/zero coverage versions retain
legacy ecology/planting; stored maps are not rewritten.

DistrictTreeCoverage uses seeded broad/fine noise ranking (.82/.18) and jittered
~9.48m planting cells, adapted from the approved70% fixture. It selects the requested
fraction of eligible planting cells, not a uniform tree-count thinning or measured
canopy area. Water9m, camp clearings8m, slope>25 degrees, below-sea ground and active
footprints stay excluded. Original cell IDs retain their percentage in combined
unions. Canonical native seasonal planting uses the same plan; approved summer mixed
trees bypass the older10m thinning/woodland gate for these new recipes only.
RegionBiomeMap uses matching noise and sampled per-cell thresholds for ground/relief
woodland fields. Exact detailed planting ranks eligible cells; preview thresholds
are approximate. Species, art, lighting, shorelines, water and legacy seasons remain.
See Documentation/03_Technical/Approved_Mixed_Forest.md.

Validation: planner checks8 seeds×3 percentages, exact eligible targets, exclusions,
determinism, nested footprints, independent district settings, recipe JSON/default
round trip, invalid values and legacy opt-out. Live normally created512m districts:
25%=729,50%=1458,75%=2187 trees; canonical/plan/mixed counts match, grounding and
exclusions pass. L-shaped union keeps4374 trees and original member choices. Cache,
private disk round trip and seasonal stability passed. Fresh wizard controls and
final UI layout visually inspected. Full disk reopen of the L-shaped union passed:
25/50/75 choices,4374 trees, canonical positions/species and recomputed plan retained.
Editor left in Create Region / Terrain at the coverage grid, with all cells50%. Reports: Library/DistrictTreeCoverageChecks.txt and
Library/DistrictTreeCoverageLiveResults.txt. Disposable verification map only under
Library/TreeCoverageRoundtrip.cfregion; private user recovery remains
Library/BeforeTreeCoverage.cfregion. No normal saves written; work uncommitted.

# Automatic meadow paths removed — November 3, 2026

User wants natural meadows without automatically generated paths, ahead of settlement
work. BuildRoad now initializes an empty route for all meadow districts, including
coastal/mountain fixtures. BuildRoadMesh skips empty routes. New ground painting and
vegetation placement therefore receive no pilot-path masks or exclusions.
Cached districts retain saved trees and terrain; RestoreSnapshot removes the known
pilot-road soil contribution along the stored centerline from a cloned control map,
redistributing it proportionally to the three native vegetation layers. Other layers
are retained. The runtime road is then cleared, so new captures/reopens do not repeat
the subtraction. Previously excluded trees are not regenerated in old districts.
This is for the old decorative pilot path only; settlement road persistence must use
separate explicit data when implemented. Normal player saves are not written.
SavedRegionStore now accepts zero road points (previous minimum two rejected empty
routes). Private recovery checkpoint: Library/BeforeMeadowPathRemoval.cfregion.

Live source compilation and cached-district reopen passed: zero road points/no ribbon,
7,788 ground samples lost pilot-path soil, 14,277 canonical trees and 2,647 mixed
trees/signature unchanged. Private save/load and actual district reopen passed with
zero ground-weight delta from the pathless cache. Existing 8-bit terrain weight
quantization retained. Verification: Library/MeadowPathRemovalChecks.txt. Bank
repainting was skipped by the recovery driver because this cached district was
already painted; normal beach/forest/water setup ran. Changes remain uncommitted.

User clarified the current forest spacing is acceptable; do NOT implement the dense
70% fixture as normal planting. Verified Tree01_.fbx download metadata contains model
5197447, matching the user's CGTrader settree URL. Summer chestnut/locust derive from
that pack; larches are separate. Autumn/winter still use legacy seasonal trees.

# Summer forest reactivation — November 3, 2026

User reported all trees disappearing after Fall/Winter -> Summer and asked whether
the previous chat's approved clusters were present. The open region uses forest
appearance1 with2,647 approved mixed trees, signature d480c2f6d077b2e1, unchanged from
prior checks. The eight-entry chestnut/locust/larch library is authoritative; normal
maps use seeded native woodland positions, not the private70% stress fixture layout.

Repeated inspection showed the stored forest intact and rendering in sampled close
and distant views; persistent disappearance was not reproduced. SpatialForestRenderer
now explicitly restores its active rendering backend on re-enable, toggling native
LOD registration through the other backend, and invalidates cached selections.
Disable also invalidates selection and clears submission counters. This hardens the
season/tree-toggle reactivation path without changing geometry, placement or palette.

The previous integration test checked holder.activeSelf for Summer, which does not
prove rendering. MixedForestSeasonChecks now invokes the actual HUD season buttons,
tests close/distant/close paths, checks submission/LOD state, and compares rendered
pixels against a temporarily hidden mixed forest. It also checks Trees Off across
season changes and preserves canonical counts/signatures. Run CityForge / Forest /
Check summer visibility after seasons. Private recovery: Library/BeforeSummerTreeFix.cfregion.
Live verification passed: actual HUD Fall/Winter/Summer at zooms 2/4/2, individual
and spatial rendering, visible pixel differences 20,548 / 6,566 / 20,485 of 33,800
samples, and Trees Off/On across seasons. Tree count/signature and canonical 14,277
trees stayed unchanged. Report: Library/MixedForestSeasonChecks.txt.
No normal saved region is overwritten. Rendering changes remain uncommitted alongside
the patrol-only shark work; swimmer/encounter code remains deferred and removed.

# Supplied great white patrols only — November 3, 2026

User clarified: shark patrols now, swimmers later. The preliminary ambient swimmers
and encounter implementation have been removed from source and the live scene.
CoastalSharkPopulation now contains only one transient shark patrol per navigable
coastal district with Animals enabled. CoastalOceanHabitat enforces actual ocean
polygons,2.2m depth,2.8m clearance and active combined-district footprints. Targets
and each movement step are checked; no swimmer targeting/scare/flee logic remains.

The supplied2,178-vertex mesh and rig are prepared privately under ignored
Resources/CoastalWildlife/GreatWhite/. The full1–117 frame action at24fps is4.833s;
normalization produces an approximately5m shark. Builder: CityForge / Wildlife /
Prepare supplied great white (from the main menu). Original downloads are intact.
Do not publish supplied or derived artwork or substitute the Animal Pack shark.
See Documentation/03_Technical/Coastal_Sharks.md for setup and checks.

Private checkpoint: Library/BeforeSharkPatrolOnly.cfregion. Older BeforeCoastalSharks
preserves the original pre-shark view. Find coastal shark frames the live patrol.
CoastalSharkChecks.Begin() passed patrol movement (9.79m across30 samples), habitat
constraints, Animals toggling and no swimmer spawning. Compilation/whitespace pass. Latest report: Library/CoastalSharkChecks.txt and
this chat's outputs/Coastal-Shark-Checks.txt. Earlier encounter checks/captures are
historical experiments superseded by the user's patrol-only instruction.
Normal player saves remain untouched. Shark source changes are uncommitted; the
preceding approved coastal/beach changes are committed as01ee3dd. Private assets
remain excluded from Git. No player-performance or all-season visual guarantee.

# Approved coastal streams and beaches — November 3, 2026

The user approved the restored stream water, reduced shoreline shimmer, sandy ocean
floor, and softer grass/sand transition with a restrained existing dirt blend.
These changes are integrated into normal new/cached coastal district loading.

## Production behavior

- CoastalWaterClock centers ground-following Crest detail on the camera rig's ground
  focus. Following the distant isometric camera's own XZ had put visible streams in
  coarse cascades, leaving apparently dry beds.
- Coastal clipping uses an RHalf signed-distance mask, up to2048 samples for
  ground-following districts. The previous512-square binary mask used4m texels in
  the2,048m combined district and clipped61 of192 sampled stream centers.
- GroundFollowingWaterLevels preserves ocean surf while an owned foam renderer input
  suppresses ocean foam on elevated inland channels. InlandFoamExclusion.shader is
  original integration source; purchased water/terrain art and Crest source stay
  private. Do not use FoamLodInput Minimum blend: Crest validation rejects it.
- Coastal ground-following water uses regular-frustum reflections, as raised rivers
  already do. CoastalWaterPresentation caps existing TAA jitterScale at .125 and
  restores its previous value on disable. Other initial AA modes remain untouched.
  Switching to SMAA/FXAA darkened this renderer's output and was rejected.
- CoastalBeachSurface uses the existing private RichSand layer. The ocean floor is
  full sand at all depths; the earlier4–12m offshore fade exposed grass and was
  rejected. Dry beach width varies26–40m with multiple noise scales and a small
  irregular edge offset. Blending starts at28% of local width. The dry upper beach
  mixes approved matte courtyard/settlement dirt from existing soil layer0.
- Camp clearings, roads and combined-district exclusions are protected. Runtime
  meadow grass avoids the beach core. The dedicated coastline-test treatment is
  retained. Water palette, wave weights, sea level, heights and tree placement remain.
- Beach/camp/lake presentation rebuilds over canonical ground. CaptureSnapshot uses
  coastalBaseAlphamaps so the extra sand layer and dirt mixing do not shift saved
  soil indices or require cache migration. Normal player saves were not overwritten.

## Verification and practical limits

Unity compilation and whitespace checks pass. The seed109894062 combined district
retains2,647 approved mixed trees and placement signature d480c2f6d077b2e1.
All192 sampled stream centers are unclipped and below intended water level; minimum
depth .4399m. Foam exclusion is active, ocean FFT and reflections remain enabled.
TAA jitter transitions .125 -> .5 on presentation disable -> .125 on re-enable.
All6,351 sampled submerged ocean cells have sand weight1 and grass weight0. The
approved dry blend affects51,824 samples. Saved ground remains six layers and passes
a disk round trip; sampled canonical base paint differences are zero.

A full cached reload completed terrain/bank/water initialization. Later focused
reload checks reused already-painted banks and skipped redundant bank repainting.
Reports and approved captures are in the November3 chat workspace outputs directory.
Coastal cached loading remains slow: bank repainting took several minutes and the
higher-resolution water setup measured about40s locally. This is not a player/FPS
or universal loading guarantee. Full coastal visual QA across views/seasons and
elimination of every shimmer remain unproven. User accepted the observed improvement.

## Private recovery and delivery

Library/BeforeApprovedBeachTransition.cfregion preserves the latest pre-integration
region/camera. Other private checkpoints include BeforeSandySeabed.cfregion,
BeforeCoastalFlickerFix.cfregion and the validated StreamFix-VerifiedRecovery.cfregion.
Do not use BeforeStreamMaskFix.cfregion as a normal save: an early domain-reload
checkpoint has an incomplete road cache. Runtime-only region data was reconstructed
from the same seed/recipe during recovery; the approved tree signature was retained.

Historical preview scripts in this chat's work directory are one-shot experiments;
do not reapply them over initialized production beaches. The approved transition is
now source behavior. Source-only changes are recorded on the feature branch; no PR
merge was requested. Purchased artwork, maps, private packages and snapshots remain
excluded from public Git.

# Approved mixed woodland integrated — November 3, 2026

User approved the chestnut/locust/larch appearance and requested normal new-region
integration plus a source commit. The forest is now part of the game, not just an
in-memory preview. See `Documentation/03_Technical/Approved_Mixed_Forest.md`.

- Wizard and default main-menu creation set `forestAppearanceVersion=1` for new
  recipes. New temperate summer districts use the approved five chestnuts, locust,
  and two larches, with their dark palette and fuller distance meshes.
- Missing/zero forest versions preserve existing forests. Geography stays12;
  regional relief, tropical palms, mountain fir slots and native fall/winter
  presentation are retained. No existing player maps were migrated or overwritten.
- `MixedDistrictForest` selects existing canonical mature woodland points with a
  deterministic10m cell limit, respecting native ecology, water/road/camp clearances,
  slopes and active combined-district boundaries. It does not force the70% stress
  layout onto normal maps. Canonical planar positions/species/rotations/scales remain
  stored; displayed trees are grounded against current terrain.
- `SpatialForestRenderer` is the production name of the former trial renderer.
  It uses original LODGroups in close views and128m spatial instancing in views
  of size200 or larger. Top-down panning reuses LOD matrices. Distant shadow meshes
  are submitted separately from unchanged visible crowns. These are static captures;
  editing/moving trees requires rebuilding them. Water-reflection visual QA remains
  separate; submissions retain per-camera culling/shadow behavior.
- `PerspectiveMeadowBatches` combines selected ground-cover matrices for both
  camera projections. Matrix density/positions, navigation throttling and off-camera
  reflection submissions remain unchanged. Showcase opt-outs remain direct.
- Approved district zoom steps are[8,16,40,80,160,320] for the common640 reference;
  overview zooms retain their prior behavior. L4 pan multiplier is1.3 (+30%).
- The accepted grass is slightly less saturated and the conspicuous canopy litter
  stamps are removed from grass blending. Source shaders/settings are committed;
  material/texture artwork remains private.

## Private assets and recovery

Runtime library: `Assets/CityForge/Resources/Trees/ApprovedMixedForest/V1/Library.asset`.
Its compact eight prefabs reference private art under
`Assets/CityForge/Art/Trees/ForestTrees1Trial/`. Both stay ignored in public Git.
Builder: **CityForge / Forest / Build approved mixed forest library**. A private
35MB `Approved-Mixed-Forest-PrivateAssets.unitypackage` backup was exported to this
chat's outputs, including the library dependencies, excluding the huge test layouts
and Crest. Never add that package/art to public Git. New-region loading preflights
the library and reports missing private content rather than restoring retired trees.

Original approved stress fixture remains private: `HugeForest70PercentTrial.prefab`,
~202MB,8,165 trees across70% of planting cells in a1024m district. Loading/saving it
may require Unity Pipeline `--timeout180`. Do not overwrite it with disabled source
renderers from an active instancing experiment. New production regions need only
V1's compact library, not that fixture or the480-tree authoring layout.

The last user preview was checkpointed to ignored
`Library/BeforeMixedForestIntegration.cfregion`; earlier private assets are intact.
The current editor shows a fresh normally created verification region (seed70331),
not the stress-test overlay. No normal Regions-directory files were modified.

## Verification and measured limits

`MixedForestIntegrationChecks.Begin()` passed actual main-menu generation, grounded
placement, water/road/camp exclusion, tree/understory toggles, native fall/winter and
summer restoration, neighbor/cache reopening, disk round trip, legacy recipe behavior,
and an L-shaped union with its excluded corner empty. New overview8.82s; first512m
district5.01s with2,260 trees; placement signature788453d361dc1471. Native Y values
may re-seat against quantized terrain on cache load; planar ecology and mixed-tree
signature were preserved exactly. Reports/snapshots are under ignored `Library/`.

Previous8165-tree tests demonstrated the limits of batching: distant instancing
reduced median CPU frame time but close grouping regressed, hence the hybrid cutoff.
Simpler distant shadows reduced repeated L5 panning p95 from92.95ms to42.63ms while
median remained~39ms. These were editor observations, not player/FPS guarantees;
absolute frame times across sessions/resolutions are not comparable. User accepted
the appearance and the more consistent L5 result. Preserve those visual decisions.

Region creation/district loading optimizations from this session are also retained:
exact indexed route distances/current coordinates, bounded bank work, worker control
map sampling and skipping snow meshes outside summit footprints. See
`Documentation/03_Technical/Region_Loading.md` and `LoadingPerformanceChecks`.

Detailed experimental logs are retained locally in the chat workspace's
`work/mixed-forest-integration/Handoff-Experimental-Archive.txt`; old trial scripts
may reference the former `SpatialForestTrialRenderer` name and must not be rerun
blindly against the production integration.

# Session checkpoint — November 1, 2026

Read this section before the chronological notes below. Current approved result: Indigenous Camps option (off by default; seeded2–3 when enabled), restored camera-fitted shadows, muted teal water, asynchronous Save District feedback, balanced tree colors and configurable richer meadow grass. New region wizard uses geography version12 plus independent waterwayVersion5. Streams are seeded6–20feet wide per route (no along-route width modulation), with irregular bends, separated corridors, mountain avoidance, near-ground water, Pass17 rocks/pebbles/shrubs, settlement dirt banks and mildly sedimented water. Older saved routes retain geometry/width; appearance integration applies to new-style streams on load.

Grass controls: Resources/Region/MeadowGrassSettings.asset; saturation1.12, brightness.94, tint(.95,.99,1), variation1. Disable applyRefinement to reverse that pass. Preserve native MeadowRuntimeBaseline/SeasonalTreeLibrary and the shared settlement dirt; no retired forest fallback. White streak was dirt diffuse alpha interpreted as smoothness: district-owned soil copy alpha8/255 fixes it. Raised channels use Crest's supported non-oblique reflection path. Presentation now guards missing View.Data during assembly reload and tolerates camera teardown.

Validation completed: runtime and Editor source compilation (existing warnings); whitespace;6480 version5 recipe cases for6–20ft bounds, count, separation, mountain clearance and legacy behavior. Earlier distance-query comparisons and shallow-bed profile checks passed. Live Unity meander/Pass17/cache-reopen regression passed with no duplicate shrubs and retained saved paint/trees; most recent visual run (version4 before widening) took37.88s overview and12.30s district, with57 scanned patches/1299 individual stones/53 shrubs in the selected district. Version5 width expansion has geometry checks, but no additional live visual run. Native Unity assembly reload after null-reference fix completed without recurrence.

Remaining practical limits: ground-following water is a visual elevation field, not hydraulic downstream flow. No new coastal/combined-district visual regression or frame-rate benchmark this session. Mixed major-river/stream districts keep the shared major-river water palette. Stream width is constant per saved route. Source-only checkout needs existing private local artwork and Crest dependencies to render. No saved regions were overwritten. Test previews are disposable unsaved regions.

Explicit integration runner: write an absolute output directory ending in pass17-generated-streams to Library/SeparatedWaterwayLoading.request while at the main menu in Play mode. It creates a disposable MountainBorder/Many streams region, opens a district, and tests cache reopen. It skips unrelated open regions. Do not run it on a user's active region. See Editor/SeparatedWaterwayChecks.cs for geometry checks.


## Presentation reload null-reference fix — November1,2026

User reported MeadowDistrictLandscape.ApplyPresentation line617 from OnEnable. Editor log tied repeated failures to assembly reload, when transient View.Data is unavailable but Baseline survives. ApplyPresentation(true) now waits for Baseline, View, Data, controller and Camera before snapshotting/changing any global settings. Disable/teardown still restores globals and releases camera backup even if viewer was destroyed; camera reset tolerates missing region data. Runtime compilation/whitespace passed; native Assets Refresh imported the new assembly and completed domain reload without another NullReferenceException. Stream appearance/geography unchanged.

## Wider stream variety — November1,2026

New wizard recipes now use waterwayVersion5: seeded stream full width uniformly6–20feet (1.8288–6.096m), constant per route. Same one RNG draw preserves route seeds; versions0–4 and saved routes keep their widths. Major rivers, bends, shallow banks, settlement dirt, rocks, sediment water and separation/mountain rules retained. Runtime compilation and6480 version5 recipes pass width bounds, counts, disjoint bank envelopes, wet/dry profiles and legacy defaults. Editor check now covers both earlier separated recipes and version5. This supersedes the earlier12ft ceiling for newly generated streams. No saved maps rewritten.

## Stream meanders and sediment — November1,2026

New wizard recipes use waterwayVersion4 (geography remains12). Streams add shorter irregular220–340m bends (up to28m amplitude, capped to20% lane spacing) to the existing broad drift; major rivers unchanged. Variable route sampling at24 segments/cycle; distance windows and GeneratedStreamAppearance now derive segment counts from stored routes. Disjoint lane envelopes preserve separation and mountain front70% clearance. Versions0–3/saved routes retain existing geometry. Bank soil already uses NativeCamps/Courtyard-Dirt, the settlement texture, with damp tint and owned matte-alpha copy. Generated stream-only district water uses absorption(1.65,1.35,1.15) and muted olive sediment scattering(.105,.125,.078), reducing bed visibility; mixed major-river districts retain their water palette. No source art changes or saved-map writes. Runtime compile and6480 version4 recipe checks passed; live generation and cache reopen passed:49 scanned patches,1164 stones/pebbles,47 shrubs, no duplicate shrubs on reopen. Overview37.66s and district12.27s on this run; more detailed curves cost additional generation time. Preview framing now selects an interior bend, away from district slab edges.

Reflection-camera frustum correction (2026-11-01): user reported Crest WaterReflections.ComputeHorizonPositionAndNormal / ViewportToWorldPoint out-of-frustum errors. Ground-following inland channels now set the supported Reflections.UseObliqueMatrix=false before enabling the renderer; a single global sea-level oblique clipping plane does not describe these elevated channels. Reflection rendering remains enabled, source Crest package untouched, old flat channels/ocean settings unchanged. Runtime compile and whitespace passed. Live Unity regression passed: fresh overview17.14s, district8.63s, cache reopen retains43 scanned patches/1151 stones with no duplicate shrubs. No new out-of-frustum or exception messages after import/run; reopened screenshot inspected, matte bed and water preserved. No region save written.

Stream white-streak correction (2026-11-01): A/B captures isolated the continuous white line to terrain soil specular, not water glints/reflections. URP Terrain Lit takes diffuse alpha as smoothness even with layer smoothness zero. Stream districts now use one owned runtime copy of shared dirt RGB with alpha8/255 on base soil and dedicated damp bank layers. Source art, water settings, rock placement and grass are unchanged; copy is released with district assets. Six comparison captures live in chat outputs/white-stream-streak.
## Approved Pass17 treatment on generated streams — November1,2026

User requested carrying the approved Stream Rock Study Pass17 appearance into normal generated streams. New GeneratedStreamAppearance applies to isStream/channelVersion>=2 bodies, using their saved routes/local water levels. No new geography version, no saved-map regeneration. Original fixed-coordinate study gates and study geometry remain unchanged. Generated stream geometry retains20cm freeboard,12ft cap, separated corridors and mountain avoidance.

Reuse existing private StreamRocks/Rocks scanned mesh/material and all10 IndividualStreamRocks/Stones variants; same three diameter bands, patch gaps and native willow/bush choices as study. Scale scanned patches to fit narrower streams; seat all stones on actual district terrain, avoid scanned patch footprints, camps, oceans and excluded district footprint. Stable route-ID seed salt, rendering batches<=1023, height-aware bounds. Shared StreamBankBlend.VariedCover now accepts route-local coordinates via overload; old signature forwards identical coordinates. Reuse Pass17 matte/shared damp earth and independent-side irregular blending instead of continuous pale rim. Dedicated runtime soil layer is appended; caches missing only that slot are padded while retaining their saved roads/trees. Stream-only inland districts use Pass17 refined water clarity/colors through existing ConfigureWater; mixed major-river districts keep current approved river palette.

No licensed artwork changes/additions, fish stocking, global lighting, grass palette or tree material edits. Runtime decoration is rebuilt from routes on load; cached shrubs are position-deduplicated. No region files written. Resource preparation remains the existing private-art workflow; missing approved assets report an error instead of substituting unrelated rocks.

Offline runtime/Editor compilation and whitespace passed. First real Unity generated MountainBorder/Many streams/Small4096m/grid8/seed8472 test:43 scanned patches,1151 individual rocks/pebbles,46 native shrubs; overview16.74s, district7.72s. Zoom1 close screenshot inspected: stones visible, bank earth blends into meadow, surface41m follows shallow ground. New explicit integration mode outputs/pass17-generated-streams also simulates an older cache without the soil slot and checks saved terrain reuse, deterministic rock counts, no duplicate shrubs and normalized weights. The reopen test initially exposed a missing baseline clone before appending generated-stream soil, which grew the shared in-memory layer list. Fixed by cloning before any append. Original private asset file remains five layers; cache reuse then passed with identical43/1151 rock counts, zero newly added shrubs and623 cached trees preserved. Terrain readback normalization uses the8-bit per-channel rounding bound, rather than an unrealistically exact float comparison. Final clean-source Unity run PASS: overview17.55s; district7.86s;43 scanned patches/1151 individual stones/46 shrubs. Simulated older five-layer cache expanded to the proper six layers, UsedSavedTerrain=true, identical rock counts, zero duplicate shrubs and623 cached trees retained. Stored weight sum max error .003921747 is within six-channel8-bit rounding bound .01177471. Explicit test unloads/reloads the source baseline before generation to clean up earlier transient shared-memory mutations; private source asset file remains five layers. Final reopened close screenshot inspected; left unsaved test district31 at Zoom1/size26 with restored appearance. outputs/pass17-generated-streams contains frames and validation. No saved-region writes, commit or push. Coastal/combined-district visuals and a frame-rate benchmark were not run.

## Ground-level waterways — November 1, 2026

User rejected the gully appearance and requested water nearly flush with adjacent ground. Cause: prior channelVersion1 graded surrounding35m+ terrain down to the global12m/30m surface over80m banks. New wizard waterwayVersion3 creates channelVersion2. Ground outside the wetted ribbon remains unchanged; surface follows original shared upland height minus .20m. Stream bed .45m below water, major-river bed2m below water, fourth-power edge profile returns to original ground. No broad valley excavation. Existing saved metadata0/1 retains prior behavior; no automatic migration. Mountain-parallel routing,12ft stream ceiling and disjoint corridors retained.

GroundFollowingWaterLevels owns a513-square RFloat Crest LevelLod texture of local ground-relative offsets and reports the absolute height range for culling. Enable AnimatedWavesLod to pack local level data without adding wave shapes to the approved inland renderer. Crest clipping and wet checks use the corresponding local surface; fallback bounded mesh/depth and overview mesh also follow local levels. Coastal renderer conditionally includes raised new channel footprints and local level offsets; ocean offsets clamp to existing sea datum so seabed/shelf and ocean level remain unchanged. Materials/colors/light unchanged. This is a ground-following visual channel, not a hydraulic/downhill-flow simulation.

Offline production-code profile tests pass:20cm bank freeboard, bed below water, exact unmodified heights outside ribbon;6480 prior recipe/clearance/count cases and20000 distance comparisons remain passing. Runtime/Editor source compilation succeeds with existing warnings. Live Unity Small/grid8/seed8472/MountainBorder/Many streams overview generated16.39s and detailed district31 ready7.96s. Level input enabled; original ground41.18271m versus water40.98271m. Overview screenshot inspected: broad gullies removed. Then narrowed new-profile bank paint from16m to1.25m and reduced intensity .86 to .55; final close-view regression passed after explicit Assets/Refresh (keyboard refresh while Game view focused had left stale assemblies). Final runtime/Editor assembly timestamps17:06:23/24; regenerated overview17.14s, district7.89s. Captured zoom1/size26 close-up and inspected visible ground-following water with narrow soil edges; left unsaved test district31 framed at stream. Evidence outputs/flush-waterways. Existing saved regions were not written. Coastal mixed-water source path compiles but was not visually exercised; live test was MountainBorder. No commit/push requested.

## Mountain-clear waterways — November 1, 2026

Live Unity same-seed Small/grid8/MountainBorder/Many streams generation completed16.43s. Screenshot outputs/mountain-waterways/Stream-Overview.png visually confirms intact range and five lowland routes parallel to it; left new unsaved test overview open. The screenshot/test ran after runtime routing compilation but before the final expanded integration-report assertions recompiled; mountain clearance and unchanged-height assertions were executed separately in the offline production-code tests. No saved region writes.

User requested rivers/streams avoid cutting through mountain ranges. New wizard recipes use waterwayVersion2, geography remains12; flag0 and1 keep their previous route generation when explicitly regenerated, existing stored bodies unchanged. For current MountainBorder (geography>=9, rear quarter v>=.75), CreateSeparated places seeded meandering routes along x, parallel to range, in separated corridors within front70%. Complete water plus graded bank envelopes stay below .70, leaving a dry buffer to mountain foothills. River counts/widths, stream10–11.5ft nominal cap and nonintersection retained. Other shapes keep previous route direction. This is the Mountain Border rule, not a general hydrological pathfinder for arbitrary terrain.

Persisted channelAlongX switches the optimized uniform-axis segment search to x; missing flag stays false for old channels. ContinuousHeight and SignedDistance now prune on both axes. Terrain, masks, current vectors and relief use the same saved curves. No waterway regeneration when opening saves. Previous overview performance cache remains.

Offline runtime/Editor compile and whitespace passed. Actual production-code Mono checks6480 recipe combinations across seeds/sizes/grids/counts verify both axes, complete bank envelope separation and mountain clearance;20000 random distance queries match full segment search; mountain-zone analytic heights remain unchanged. Existing cache/profile/legacy checks pass.

## Narrow-stream region loading fix — November 1, 2026

User reported freeze generating zero major rivers, Many streams, Mountain Border. Found heavy new overview sampling: each shaded pixel called analytic channel heights up to19 times, and each query recomputed every129-point route Bounds. Added nonserialized Bounds cache for immutable channelVersion1 geometry (invalidates on array replacement/width change); legacy contours retain dynamic behavior. RegionalReliefHeightCache samples analytic heights once at1025 atlas resolution, yielding on8ms budget; relief height/normal/shadow taps reuse that4MiB overview-only cache. District terrain still uses exact analytic channels. Stream widths, routes, separation and saved data unchanged.

Measured bundled-Mono actual production code: old relief-style128-row height workload26.583s; Bounds cache alone .637s with identical checksum; full1025-row cached workload .720s plus .272s prepass. These are focused height/query benchmarks, not whole loading time. Extended SeparatedWaterwayChecks verifies atlas-pixel height equivalence, yielding and cache invalidation plus original3240-recipe/10000-point route checks. Offline runtime/Editor assemblies compile with existing warnings; whitespace passed.

Added explicitly requested-only Editor integration hook Library/SeparatedWaterwayLoading.request (file contents output folder). User had reopened Unity; refreshed/compiled and ran real MainMenuController.CreatePrototypeRegion through existing worker/preloader/OpenStages path, Small4096m, grid8, seed8472, no rivers, Many streams, MountainBorder. PASS: overview ready in16.89s, five streams each below12ft, screenshot visually inspected, no region save written. Evidence in current chat outputs/stream-loading-fix. Left unsaved Stream loading verification overview open in Play mode. Full original seed/size unknown, but reported option combination reproduced successfully. No commit/push requested.

## Separate narrow waterways — November 1, 2026

New Region Creator recipes opt into waterwayVersion1 (geography remains version12). Missing/zero retains the old RegionalRiverNetwork and coarse broad carving. CreateSeparated makes exactly the requested major rivers plus2/5 streams in shuffled disjoint corridors. Streams have physical full widths10–11.5ft across map sizes/grids, below12ft; major half widths remain .014 regional units. Seeded mild meanders and their80m-or-less graded bank envelopes cannot intersect. No tributary joins, including stream/stream and river/river. Routes currently traverse the region; no new hydrological source/mouth simulation.

Persisted per-body channelVersion/isStream/bankWidthNormalized describe continuous channels. New regional Heights retain the uncarved shared upland field; PrototypeRegionData.SmoothHeight applies the saved channel profile at query resolution, preserving old heights when metadata is absent. Terrain sampling increases to approximately1m (4097 cap, up to2m for largest combined footprints). Coast ocean interiors retain their depth profile. Relief colors query actual channel distance. Native shoreline/placement distances use exact routes inland; fallback water mask and Crest wet checks reject points outside the ribbons. New Crest clip texture uses RHalf signed distance rather than a binary R8 mask, reducing subpixel widening/disappearance. Colors/materials/lighting unchanged. Existing saved regions were not regenerated or written.

Offline actual Unity runtime and Editor assembly compilation passed using cached Unity response files and Roslyn, with existing warnings. Executed actual planner/profile code through bundled Mono:3240 seed/grid/size/count combinations pass stream widths, counts, bank separation, legacy defaults and wet/dry profile checks; optimized distances compared against full segment projection at10000 random points. Repeatable menu CityForge/Checks/Separated waterways. Unity remains closed per user cooling break: rendering, Crest clip behavior at viewing distance, terrain seams, save/load and large combined-district performance still require in-Editor verification. No commit/push requested.

## Configurable grass-only richness pass — November 1, 2026

User supplied detailed grass refinement brief: richer/cooler medium meadow green, modest saturation increase, slight darkening, preserve broad variation/aerial softness and isolate all non-grass content. Diagnose first: baked EarthyGrass1/2 already desaturated, previous warm/bright remaps contributing; actual Natural Meadow Transitions shader retains mip bias2.5. Scene ACES/exposure+.05/saturation-2/contrast12 and accepted lighting unchanged.

Added MeadowGrassSettings ScriptableObject + source-only Resources/Region/MeadowGrassSettings.asset: applyRefinement true, saturation1.12, perceived sRGB brightness.94, tint(.95,.99,1) luminance normalized, variationStrength1. Added MeadowGrassAppearance on normal forest initialization after runtime grass layers; it grades source grass into owned2048² RGBA32 mipmapped copies on entry/settings changes, incorporates preceding layer remaps and uses unit remaps while enabled. Existing textures/alpha/control weights/tiling/filter and geographic data preserved. New Resources/Region/MeadowGrassGrade.shader is a one-time blit helper only; terrain drawing shader remains unchanged and adds no per-frame samples/arithmetic. Full runtime copies ~43MiB plus apply-time GPU readback/mipmap cost. No standalone perf benchmark.

Inspect MeadowGrassSettings asset to iterate live. Apply Refinement OFF restores EXACT preceding texture refs and remaps(1.25,1.12,1.20)/(1.42,1.18,1.18); ON restores experiment. Settings are source-only config; narrow .gitignore exceptions added. Original artwork remains private/unchanged. Only one new init call in ApprovedDistrictForest; earlier changes in this branch are unrelated and preserved.

Validation: compile/shader compile passed. Initial helper texture-binding error corrected by explicit Properties; identity blit RGB/alpha max error0. Regenerated mip alpha vs compressed originals max sampled3/255 (shader itself passes alpha exactly). Live controls/rollback/source isolation/mips/cleanup passed; non-grass layers, sun, postprocess unchanged, authored foliage brightness1.5 unchanged; canonical geography identical and73205 sampled terrain weights equal. All54 existing save files unchanged during verification. Before/After same camera plus Checks and detailed Grass-Refinement-Report.txt under this chat outputs/grass-refinement. Scratch work/grass-refinement/check.cs; ignored Library/GrassRefinementSession.cfregion preserves session. Left updated view open, candidate awaiting feedback. No commit/push/PR requested.

## Grass contrast against trees — November 1, 2026

User approved warmer muted meadow poplars, then requested lighter/warmer/less saturated grass with subtle fresh/dry variation to separate tree crowns. ApprovedDistrictForest now sets its two existing owned runtime grass layer copies to RGB remap scales(1.25,1.12,1.20) and(1.42,1.18,1.18), alpha1. Existing grass1/grass2 textures and spatial layer weights provide the fresh-versus-warmer/straw variation; no new paint patches, source images/layers, shader, alpha/smoothness, normals, tiling or saved data changes. Greater red/blue relative to green mutes saturation; stronger red in second layer warms it. Trees, previous olive poplar palette, authored brightness1.5, lighting and shadows unchanged. Original checkpoint/private artwork remains untouched.

Live trial/after inspected at saved district camera. Unity compilation and whitespace passed. Fresh normal saved district load retains tints on owned layer copies, canonical geography byte-value equivalent,73205 canonical ground-weight samples equal, authored brightness1.5 retained. outputs/grass-contrast contains Before/Trial/After and Checks.txt. Original view preserved via ignored Library/GrassContrastSession.cfregion; current district left open with warmer/lighter grass. Existing user region not overwritten. Candidate awaits feedback; no commit/push/PR requested.

## Meadow palette harmony — November 1, 2026

User found prior vivid meadow greens clashed with olive grass, liked authored maples, and approved muting/warmer olive direction. Supersedes the deeper-green formula below: MeadowTreeGreen now lerps original hue toward .235 by .65, saturation min(.65,original*1.05+.015), value*.84; alpha retained. Prepared NativeSummer poplar copies refreshed via existing guarded Edit menu. Maples/authored crowns brightness1.5, grass, lighting, bark, shrubs and fall/winter remain unchanged.

Unity compile/whitespace and fresh normal saved district load passed; all poplar healthy/dry renderer slots match revised recipe, authored brightness1.5 preserved. Same camera restored via ignored Library/MeadowOliveSession.cfregion; visual inspected and left open. New chat outputs/meadow-olive/After.png and Checks.txt. Existing saved region not overwritten. No commit/push/PR requested. Candidate awaits user feedback.

## Deeper meadow poplar greens — November 1, 2026

User approved lightened authored crowns, then requested deeper green rather than washed-out meadow plugin trees. Added ApprovedForestColors.MeadowTreeGreen: original hue lerp toward .30 by .35, saturation min(.8,original*1.45+.05), value*.86, alpha preserved. Applies to prepared NativeSummer poplar prototypes0–10 (juvenile and mature), including their Foliage/Cross LOD materials. Shrubs11–14 retain WarmPale; original source materials, fall/winter palettes, bark, grass, global lighting and authored forest brightness1.5 are unchanged.

BuildNativeSummer uses the new recipe for these poplars. Repeatable Edit-mode menu CityForge/Forest/Refresh meadow tree greens recolors only prepared approved NativeSummer copies, validates source/target structure/path and derives every tint from original SeasonalTreeLibrary summer materials to prevent accumulation. Local prepared12 leaf materials updated and remain private/ignored. No licensed files added to Git.

Unity compilation/whitespace passed. Normal fresh saved district reload verified all12 prepared materials match new healthy/dry colors and authored brightness remains1.5. Same-camera Before/After in new chat outputs/meadow-green, with Checks.txt. Original view preserved in ignored Library/MeadowGreenSession.cfregion; left same district/camera open for review. Existing user region was not overwritten. No commit/push/PR requested.

## Dark crown lightening — November 1, 2026

User requested lightening the dark trees in the close screenshot. IMPORTANT correction to chat's initial diagnosis: the visible mature crowns are drawn by ApprovedDistrictForest, which replaces native/prototype trees in Summer. Changing the six SilverMaple GreenLeaf materials did not affect those crowns. Those temporary source-material tint changes were fully reverted to(.20,.32,.25); ForestTreePalette/source assets remain unchanged. Do not repeat prototype-only diagnosis for approved rendered crowns.

ApprovedDistrictForest.FoliageBrightness default now1.5 versus1.0, using existing leaf-only _InstanceTint multiplication across all LODs. No shader/mesh/palette-target/wood/tropical-palm/native-poplar/global-light/water/cast-shadow changes. Instanced renderer also uploads its own original baseline SH probes; global ambient-probe changes do not automatically update these crowns. Keep that distinction in future lighting diagnosis. User asked for leaf lightening, so only the existing leaf brightness control changed.

Live before/after verified visibly lighter green with leaf detail, retaining darker shaded crowns. Evidence outputs/lighting-refinement/Maples-Before.png and Forest-Lightened.png (Maples-Trial was ineffective prototype test). Source compiles cleanly. Original camera preserved via ignored Library/MapleLighteningSession.cfregion; normal district reload used for persistence verification. No user region writes, no commit/push/PR requested. Candidate awaits feedback.

## Daylight fill refinement — November 1, 2026

User accepted the recommendation to modestly lift ambient fill and neutralize the yellow sun. MeadowDistrictLandscape now initializes directional sun color(1,.98,.93), retaining original intensity1.25/direction45,220 and seasonal Hard shadows/strength. ApplyPresentation raises ambient intensity .7→.84 and scales the original custom ambient SH coefficients R/G1.20, B1.28, a gentle cooler fill. Custom SH needs explicit scaling; ambientIntensity alone is insufficient. Coefficients derive from private Baseline each entry, never multiply live state. Source baseline art/materials/volume/reflection settings remain untouched. Existing restoration captures/restores overview lighting.

Live district/camp before-and-after views inspected. Unity compile passed; fresh normal saved district load confirmed persistent settings, original sun strength/direction, all seasonal Hard shadows, overview probe/intensity restoration, repeated ApplyPresentation no accumulation, unchanged teal river material and4096/four-cascade pipeline. Camera motion quality was allowed to settle before final district/camp captures. All54 pre-existing region files hash-identical during validation. New chat outputs/lighting-refinement contains District-Before/After and Camp-Before/After plus checks; scratch work/lighting-refinement. Left camp framed at zoom1/size24 for review, original district camera retained in ignored Library/LightingRefinementSession.cfregion. Candidate awaits visual feedback; no commit/push/PR requested.

## Save button feedback — November 1, 2026

Save District/Save Region now immediately disables the save button, shows SAVING… and a thin gold moving bar inside its lower edge. This is an indeterminate activity indicator. Coroutine yields before main-thread Unity snapshot capture, then SavedRegionStore.SaveAsync resolves the path on the main thread and moves validation/compression/atomic disk write to Task.Run so animation/rendering continues during writing. Existing synchronous Save remains for tools. Success shows DISTRICT SAVED/REGION SAVED; failure shows SAVE FAILED; finally hides bar and restores buttons for retry. Duplicate clicks and region/district exit are guarded while saving; the Return button is temporarily disabled. No schema changes.

Unity recompile and whitespace passed. Live temporary-fixture checks passed immediate feedback, duplicate suppression (no duplicate backup), navigation guard, moving bar across18 frames, async district/cache file reload, invalid-ID failure recovery, successful retry/backup, and worker validation fault propagation. Expected deliberate invalid-ID error briefly triggered Unity Error Pause; resumed after inspection. Original ID restored and only unique fixture/backup/temp deleted. All53 pre-existing save files byte-identical. Evidence outputs/save-feedback in this chat; scratch check code work/save-feedback/check.cs. Current district/camera preserved through ignored Library/SaveFeedbackSession.cfregion. No commit/push/PR requested.

## Muted teal river color candidate — November 1, 2026

User requested ordinary rivers match uploaded Screenshot 2026-11-01 at 9.44.40 AM: muted teal rather than saturated blue. ApprovedRiverWater.Configure now uses absorption(.55,.38,.40) and scattering(.14,.31,.32), replacing(1.8,1,.32)/(.024,.24,.44). Only these two material color properties changed; existing ripples, glints, flow, reflection strength, lighting, geometry and shore remain. Historical StreamRockTestRegion overrides retain their accepted colors; ocean/lake profiles unchanged. Candidate awaiting user visual feedback.

Live same-camera before/after inspected in iriquois nation district27. Reference open-water sample median RGB47/71/79; original0/29/84; candidate34/62/70 (approximate different-crop/screenshot comparison, not exact colorimetry). Evidence new chat outputs/river-color/Matched-Teal.png and Before.png. Unity recompile passed. Session copied only to ignored Library/RiverColorSession.cfregion for reload; original region not overwritten. Source-only, no commit/push/PR requested.

## Normal district shadow precision — November 1, 2026

User reported nearly absent shadows in ordinary saved district (iriquois nation, district27, seed243115447). Atlas4096/four cascades/Hard sun remained correct. Normal framing used the1200m height storage range, unlike close studies; this forced near10/far2306.97 and lost shadow precision. CityForgeCameraController.ApplyPose now uses the active detailed terrain bounds maximum plus world Y for camera setback/depth fitting, retaining80m canopy/building clearance. Overview fallback, original framing metadata, navigation throttling, pipeline defaults and sun appearance remain unchanged. Shared camera path also covers combined districts. TerrainData.bounds retains real mountain maxima.

Verified live before/after shadows (trees and teepee), Unity compilation, normal saved overview/district load after fresh Play, all24 combinations of six zooms/four headings for valid clip planes and pipeline distance, restored current camera near88.42/far378.61 versus old2306.97. Evidence in new chat outputs/shadow-fix. Current session was temporarily copied to ignored Library/ShadowFixSession.cfregion to preserve the view during recompile; existing user region file was not overwritten. Left original district/camera open with visible shadows. No separate mountainous/combined visual qualification this pass. No commit/push/PR requested.

## Indigenous Camps creation option — November 1, 2026

New wizard Environment page offers INDIGENOUS CAMPS: Off / On — 2–3 camps, default Off. ReadCreationSettings writes nativeCampVersion 0/1 from this control; the no-settings menu fallback also stays off. Enabled generation independently seeds a target of two or three camps for every region size/grid, with unsuitable terrain allowed to produce fewer. Existing saved records retain their former allocation; load validation remains compatible with older 13-camp saves. Camp appearance/geography unchanged.

Local branch feature/indigenous-camps-option starts at merged origin/main 977c187. Unity recompile passed; NativeCampChecks.Unit passed including both target counts across seeds/grids, determinism, water preference, Off/legacy absence, temporary save roundtrip, all-water exclusion and unchanged geography. Live wizard default/On/Off recipe checks passed. No existing regions rewritten. No commit/push/PR requested.

## Accepted settlements and persistent shadows — November 1, 2026

User approved the settlement look and requested permanent sharper shadows, commit, push and PR. Branch feature/native-settlements-sharp-shadows starts at origin/main after merged PR7. CityForge_URP now persists four cascades with splits(.50,.68,.85), existing4096 atlas; ApplySeasonShadows keeps Hard in Summer/Fall/Winter with existing seasonal strengths. Camera depth fitting and navigation quality stay intact. This supersedes the earlier temporary-shadow notes below; no preview clone is required. Four cascades add render work; standalone hardware performance is not qualified.

Final Unity recompile passed. NativeCampChecks.Unit and normal new-region/save/load/district run passed:13/64 camps (13 near water),9 teepees, upright totem,12 residents and safe paths. Unique regression fixture was removed without replacing user data. Existing saved-camp refinement passed source pipeline identity/defaults and seasonal shadows, dirt/cache/grass/fire/roles/navigation. NpcLocomotionChecks now explicitly starts safe routes for each appearance rather than relying on random idle durations; final1590 moving/2546 stopped samples,3 definitions,0 state/clip mismatches across Zoom0/1/2, hide/show and forced idle reset. All52 existing region files byte-identical. Source-only staged audit:60 files, no private art/generated assets/saves. The authored Flame shader and preparation instructions are included. outputs/pr-validation in this chat contains final evidence. Left the accepted saved camp open in Play at Zoom1 on persistent shadows.

See Documentation/03_Technical/Native_Settlements.md for the final accepted behavior, private asset preparation and tests. Earlier dated notes below document implementation history.

## Stream fish option — October31, 2026

Fish now opt-in and default OFF through PlayerPrefs CityForge.StreamFish.Enabled (fallback0). StreamFishOption replaces eager stocking on stocked stream-study districts; OFF never calls fish Resources.Load. StreamFishControls adds right HUD FISH: OFF / FISH: ON (ZOOM 0) button at y-678, visible only where option exists. ON lazily creates population; existing closest-only culling retained. OFF synchronously unsubscribes zoom event, stops movement, deactivates and destroys fish roots and population component. Assets remain private for future use. Preference persists separately from region saves. Current Pass17 landscape/camera unchanged; no new fixture needed.

Compilation, normal menu/district landscape checks, default OFF, UI ON (46 valid fish), Zoom1 hidden/disabled, UI OFF teardown and closest zoom no-respawn checks passed. Existing49 region files unchanged. Left Pass17 Zoom0 with FISH: OFF. Outputs/fish-option in current chat includes frame and checks. No performance benchmark, commit, PR or publish.

## Stream study Pass17 — closest-only fish and closer camera, October31, 2026

User requested fish only at closest zoom and25% stronger zoom for the two closest settings. User explicitly clarified UI levels0 and1 (not displayed level2). CityForgeCameraController.SetZoomLevel now divides the old orthographic size by1.25 only for0/1; levels2–5 keep old sizes. Global zoom navigation changes; explicit saved camera-state restoration remains authoritative and files are unchanged.

Current **Stream Rock Study — Pass 17** (`stream-rock-study-pass17-2026-10-31`) retains Pass16 landscape/46 larger fish. StreamFishStudy listens to ZoomChanged: only0 activates fish roots and enables the movement component;1–5 deactivate every fish and disable movement updates. At0, offscreen fish movement skips Renderer.isVisible=false; Animator.CullCompletely stops offscreen rig evaluation. Event unsubscribes on district destruction. This visibility/performance policy also applies when opening earlier bluegill-study IDs; their size/depth variants remain intact. No global wildlife-library changes.

Compilation/normal menu+district loading/landscape checks passed. Six-level assertions passed for standard study base24: sizes9.6/19.2/48/96/192/384, fish active46 only at0,0 active and component disabled at1–5. Fish positions stayed frozen while hidden; returning0 resumed46 active fish,8 on screen,7 moved,0 invalid habitats. Closest frame inspected in current chat outputs/stream-fish-pass-17. All48 pre-existing save/backup/support files unchanged. Left Pass17 at closest0/heading45/base24/summer-clear. No FPS/GPU benchmark, commit/PR/build/publish; candidate awaits feedback.

## Stream study Pass16 — fish visibility, October31, 2026

User requested50% larger bluegills swimming a little nearer the surface. Current candidate **Stream Rock Study — Pass 16** (`stream-rock-study-pass16-2026-10-31`) uses1.5× fish length (.36–.48m), with size-dependent center depth max(.18,length*.42+.035)m vs Pass15's.24m. Larger fish remain submerged; conservative cluster path clearance increases.26→.39m. Existing stone clearance and habitat/path checks remain. Crowded stocking slots are skipped;46 fish fit versus48 previously. Old Pass15 retains its size, depth and paths. Water clarity/materials, banks, bed and rocks unchanged.

Compilation, normal menu/district load and landscape checks passed.46 fish moved,0 invalid habitat/rock positions, measured sizes.362–.480m, maximum renderer top11.951m below12m surface, minimum center depth.187m. Actual normal and close fish frames inspected in current chat outputs/stream-fish-pass-16. All47 pre-existing save/backup/support files unchanged. Left normal study camera at heading45/base size24/zoom1, centerz170 for visibility comparison; close image uses base size6. No commit/PR/build/performance qualification; candidate awaits user feedback.

## Stream study Pass15 — bluegill stocking, October31, 2026

User requested trying the supplied fish from `/Users/joelinstrum/Downloads/fish`. Current candidate **Stream Rock Study — Pass 15** (`stream-rock-study-pass15-2026-10-31`) retains Pass14 landscape/water/rocks and adds48 bluegills in six groups in deeper open reaches. Exact-ID trial only; existing DistrictWildlife/TemperateWildlife and ordinary rivers are unchanged. Fish are24–32cm long, swim at.13–.25m/s, remain submerged and use checked open-water segments. Conservative cluster rectangles and individual-stone shelf masks keep paths away from rocks; terrain depth must exceed.44m. No per-fish collider or gameplay population persistence; deterministic stocking derives on fixture load and is destroyed with the district.

Private original BLUEGILL.fbx and textures archive preserved in ignored SourceAssets/Bluegill. Original supplied model has34 bones and no animation actions. Tools/Fish/prepare_bluegill.py normalizes/applies rig transforms and derives a40-frame/30fps looping spine/tail cycle; StreamFishAssetBuilder packs supplied color+fin alpha, imports normal, creates URP cutout material and loop controller. Prepared assets remain private Resources/StreamFish. Runtime prefab is BluegillFish (distinct from Bluegill.fbx resource name); wrapper corrects forward/up axes, Animator remains on model child. Original source/reflect/subsurface textures preserved; runtime uses color/alpha/normal with restrained fixed gloss. Do not publish artwork or generated FBX/textures/prefab/controller.

Compilation, ordinary fixture menu/district loading and landscape checks passed: same49 clusters/564 stones/98 individual bounds above water; depth.32–.83m/width5.8–8.1m. Fish validation:48 spawned,48 moved,0 invalid habitat/rock positions;48 looping animators, spine bend3.88 degrees across sampled cycle; maximum renderer top11.881m below fixed12m surface. Actual asset preview and in-water close frame inspected. Current chat outputs/stream-fish includes checks/captures. All46 pre-existing save/backup/support files unchanged. Pass15 left open in a close bluegill view (heading45, base size6, zoom1, near z165), summer/clear. This is a visual/motion trial, not a performance or whole-river ecology qualification. No commit/PR/build/publish.

## Stream study Pass14 — October31, 2026

User authorized another10% increase over Pass13 bank height. Current candidate **Stream Rock Study — Pass 14** (`stream-rock-study-pass14-2026-10-31`) uses24.2% of pre-lowering core bank relief (110% of Pass13's22%). Retains matte alpha8/255 soil, existing footprint/side variation, deeper bed, approved water settings and rock seating. Earlier studies preserve their treatments.

Compilation, normal menu/district loading, deterministic save round trip and checks passed.148 profile samples meet110% of Pass13 within.000002m. Same depth.32–.83m, width5.8–8.1m,49 clusters/564 individual stones/98 bounds above water. Actual close frame inspected; zoom1/2 captures in current chat outputs/stream-pass-14. All45 pre-existing save/backup/support files unchanged. Pass14 open at45 degrees/zoom1, summer/clear. Candidate awaits review; no commit/PR/build/performance qualification.

## Stream study Pass13 — October31, 2026

User liked Pass12, requested10% higher banks and removal of bright white bank patches. Current candidate **Stream Rock Study — Pass 13** (`stream-rock-study-pass13-2026-10-31`) raises core relief from20% to22% of the pre-lowering bank profile (110% of Pass12, not10 percentage points). Outer meadow feather remains. Live diagnostic confirmed white streaks were specular soil glare: procedural RGBA earth alpha255 was consumed as terrain smoothness despite the layer slider. Pass13-only earth alpha8/255 makes it matte while retaining identical RGB/grain and coverage. Old study IDs retain their old textures/profile. Water material/glints, rock seating and deeper bed unchanged.

Compilation, normal menu/district loading, deterministic save round trip, rock/depth checks passed.148 profile samples meet110% of Pass12 within.000002m. Depth.32–.83m, width5.8–8.1m, same49 clusters/564 stones/98 bounds above water. Final zoom1/2 captures inspected: bank glare removed, water glints retained. All44 pre-existing save/backup/support files unchanged. Pass13 open at45 degrees/zoom1, summer/clear. Candidate awaits user review; no commit/PR/build/performance qualification. Outputs in current chat outputs/stream-pass-13.

## Stream study Pass12 — October31, 2026

User explicitly requested80% lower stream banks. Current candidate **Stream Rock Study — Pass 12** (`stream-rock-study-pass12-2026-10-31`) scales dry bank relief about the fixed12m water plane to20% of Pass11, fully within3m of the old wet edge, feathering into neighboring meadow over3–10m to avoid a new outer ridge. Narrow paint coverage, earlier slope/side variation, approved rock seating and deeper bed are retained. Wet vertices stay unchanged; lowering adjacent dry vertices changes interpolated wet-edge position slightly. Earlier study IDs preserve appearance; ordinary generation unchanged.

Compilation, normal menu/district loading and checks passed.148 profile samples meet20% target within.000001m; actual near-bank samples~.049–.264m above water. Same4,898 bed vertices lowered by up to.30m; depth samples.32–.83m. Wet width5.8–8.1m vs prior5.5–7.8m due terrain interpolation at the lowered edge. Same49 clusters/564 individuals/98 bounds above water. Actual zoom1/2 captures inspected in current chat outputs/stream-pass-12. All43 pre-existing save/backup/support files unchanged. Left Pass12 open at45 degrees/zoom1, summer/clear. Candidate awaits user visual review; no commit/PR/build/performance qualification.

## Stream study Pass11 — October31, 2026

User supplied a close-up of the approved bank section and requested deeper water without touching banks. Current candidate **Stream Rock Study — Pass 11** (`stream-rock-study-pass11-2026-10-31`) lowers open center-bed pockets by up to.30m, tapering to zero toward shallow margins and beneath existing rocks. StreamBedDepthStudy runs after the unchanged Pass10 terrain/rock placement; conservative scan-footprint and individual-stone shelves preserve seating for all approved rocks. No water-level/material, bank shape/paint or rock-transform changes. Crest depth probes repopulate after SetHeights so absorption/depth reflects the deeper terrain. Canonical stored fields remain equal to Pass10; the depth pass derives displayed terrain/collider on each fixture load, with no accumulation. Previous studies preserve appearance. Candidate awaits visual feedback.

Compilation, deterministic save round trip, normal menu/district loading and depth checks passed.4,898 vertices lowered, maximum.30m; actual dry-height readback difference0m. Displayed center depth.32–.83m vs Pass10.32–.54m; width5.5–7.8m and exposed-slope metric1.026 unchanged. Same49 clusters/564 stones/98 individual bounds above water. Actual zoom1/2 frames inspected in current chat outputs/stream-pass-11. All42 pre-existing save/backup/support files unchanged. Pass11 open at45 degrees/zoom1, summer/clear. No commit/PR/build/performance qualification.

## Stream study Pass10 — October31, 2026

User accepts occasional steep/shaded banks and requested alternating steep sections and gentler pullbacks. Current candidate **Stream Rock Study — Pass 10** (`stream-rock-study-pass10-2026-10-31`) smoothly varies dry-bank compression1.25–3 along independent17m-scale reaches on each side. Retains Pass9's protected immediate.75m edge, narrow dirt/soil paint, earlier independent low/high margins, unchanged water and approved rock seating. Previous study IDs preserve appearance; no ordinary recipe changes.

Sampled dry shoulder footprint.631× Pass8, wet-height sample difference0, tight-bank control reaches full0–1 range. Candidate awaits user visual review.

Compilation, deterministic field/save round trip, normal menu/district loading and approved rocks passed:49 clusters/564 individuals/98 bounds above water; depth.32–.54m and width5.5–7.8m unchanged. Actual exposed-slope metric1.026 vs Pass8 1.582 (~.649×); narrower overall with selected gentler pullbacks. Final actual zoom1/2 captures inspected in current chat outputs/stream-pass-10. All41 pre-existing save/backup/support files unchanged. Left Pass10 open at45 degrees/zoom1, summer/clear. No commit/PR/build/performance qualification.

## Stream study Pass9 — October31, 2026

User liked Pass8's direction but requested substantially narrower bank footprints. Current candidate **Stream Rock Study — Pass 9** (`stream-rock-study-pass9-2026-10-31`) keeps the immediate.75m dry edge fixed and compresses the outer dry cross-section by3, while retaining Pass8's independent high/low reaches. Dirt and underlying soil paint also use doubled positive shore distance so their widths narrow together. Submerged paint and wet vertices remain unchanged; approved rock seating stays on the same pre-refinement quantized heightmap. Canonical saved fields match Pass8; earlier study IDs preserve their treatments.

Sampled dry-shoulder footprint (12.1–13.4m) measures.429× Pass8; maximum wet-height sample difference0. This is a scoped study presentation, not ordinary map generation. Candidate awaits user review.

Compilation, normal saved-region/district loading and checks passed. Actual displayed exposed-slope metric.724 vs Pass8 1.582 (~.458×, about54% less footprint); width5.5–7.8m/depth.32–.54m retained. Same49 clusters/564 individuals/98 individual bounds above water. Actual zoom1/2 frames inspected in current chat outputs/stream-pass-9. All40 pre-existing save/backup/support files unchanged. Left Pass9 open at45 degrees/zoom1, summer/clear. No commit/PR/build/performance qualification.

## Stream study Pass8 — October31, 2026

User approved the water/rocks and rejected the continuous marker-like bank strip, requesting dirt that blends into grass plus higher/lower bank reaches. Current candidate **Stream Rock Study — Pass 8** (`stream-rock-study-pass8-2026-10-31`) varies both banks independently with broad seeded low-margin reaches. Low shoulders approach water level, alternating with existing higher shoulders. Wet/lower geometry at or below12.05m stays exact; terrain collider follows displayed terrain. Canonical saved height/water fields remain equal to Pass7. Preserve prior study IDs and appearance.

Bank paint now breaks up both the dirt overlay and the continuous pale-soil underlay, with grass reaching the water in some reaches and irregular, softly feathered dirt pockets elsewhere. Noise operates at multiple scales; lower banks favor grass. Submerged bed keeps the previous soil treatment so the bank blend does not recolor shallow water. Previous procedural earth grain/source soil/native grass/global shaders remain unchanged. Approved rock seating remains on the private pre-refinement quantized heightmap. Bank appearance is a candidate awaiting user review, not an approved global generator change.

Compilation, deterministic save round trip, normal menu/district loading and rock checks passed:49 clusters/564 individual stones/98 bounds above water. Depth.32–.54m; width5.5–7.8m. Displayed near-bank samples at nominal wet half-width+1m range12.223–12.943m (water12m), on independently varied sides. Dirt weights at those samples0–.396;239/370 samples below.05 and25 above.25. Layer-weight sum error≤1/255. Earlier saved height/water fields equal, protected lower height samples unchanged. Final zoom1/2 captures inspected in current chat outputs/stream-pass-8. All39 pre-existing save/backup/support files unchanged. Pass8 open at45 degrees/zoom1, summer/clear. No commit/PR/build/performance qualification.

## Stream study Pass7 — October31, 2026

User liked the dirt direction but requested removing scorched contrast and repeated rectangles. Current candidate **Stream Rock Study — Pass 7** (`stream-rock-study-pass7-2026-10-31`) replaces the short-repeat photographic bank diffuse with owned, seamless, low-contrast procedural earth grain (19.7m tile; muted brown .40/.32/.23; no soil normal map). Native grass, source RichSoil and global terrain shader remain unchanged. A study-only two-dimensional upper-shoulder filter reduces angular shading; old displayed heights at/below12.25m are protected, with a feather above that level. Sampled protected height difference0 across2708 points; maximum dry adjustment~.41m. Rock seating uses a private owned copy of the pre-refinement quantized heightmap to retain approved placements; shrubs follow the displayed terrain. This is presentation geometry; canonical saved fields remain unchanged. Previous study IDs preserve their earlier looks. Candidate awaits visual feedback.

Compilation, normal menu/district loading, deterministic field/save round trip and approved rock checks passed:49 clusters/564 individuals/98 individual bounds above water, depth.32–.54m and width5.5–7.7m retained. Actual zoom1/2 captures inspected in current chat outputs/stream-pass-7; previous saves/backups unchanged. Canonical fields equal Pass6. Left Pass7 open at heading45/zoom1, summer/clear. No commit/PR/build/performance qualification.

## Stream study Pass6 — October31, 2026

User requested trying damp brown dirt beside the water, fading irregularly into grass, while retaining bank shape, approved rocks and water. Current candidate **Stream Rock Study — Pass 6** (`stream-rock-study-pass6-2026-10-31`) is open in northeast district3, summer/clear, heading45/zoom1. Dirt-bank appearance awaits user review; Pass4's mixed rocks remain approved.

Pass6 adds an owned clone of existing RiverBeds/RichSoil as a dedicated bank layer, muted brown(.38,.29,.20), low gloss, near-water dominance and noisy feather into native grass. Source soil and surrounding terrain materials remain unchanged. Soft-bank geometry/waterline and canonical height/water-distance fields match Pass5. Same49 clusters/564 individual stones/98 individual bounds above water, width5.5–7.7m/depth0.32–0.54m. Paint base weights are recovered before cache repaint to avoid accumulating dirt on reload.

Compilation and normal load/field round trip/owned soil layer/approved counts passed. Dirt peak~.98; sampled layer-weight sum error≤1/255 from terrain map quantization. Source soil tint still(.44,.35,.26); saved fields exactly equal Pass5. Actual zoom1/2 frames inspected in outputs/stream-pass-6. All17 pre-existing save/backup files unchanged. No commit/PR/build/performance qualification. Read Stream_Rock_Study.md.

## Stream study Pass5 — October31, 2026

User explicitly APPROVED Pass4's individual rocks plus full clusters, then requested less green/triangular banks and a better grass-texture blend. Preserve the49 clusters/564 individual stones. Current candidate **Stream Rock Study — Pass 5** (`stream-rock-study-pass5-2026-10-31`) is open in northeast district3, clear/summer, heading45/zoom1. Bank treatment awaits visual feedback.

Pass5-only StreamBankBlend rounds convex dry upper shoulders (up to~0.31m lower), anchored outside the first metre of the wet boundary. Continuous noisy soil→grass feather replaces the old abrupt wet/dry multiplier. Shore-distance field1025 vs257; terrain pixel error.25 vs1. Saved canonical height/water-distance fields and water material match Pass4 exactly. Native wet terrain was compared with a temporary original TerrainData at matching quantization: maximum wet-height difference0. Width5.5–7.7m/depth0.32–0.54m retained. The upper dry grassy shoulder becomes gentler; its physical slope width is not the old narrower exposed-soil-band metric.

Compilation/normal-menu load/field round trip and approved rock counts passed. Actual zoom1/2 captures inspected in current chat outputs/stream-pass-5. All16 pre-existing save/backup files unchanged. No global terrain shader/material or other saved-map modifications, no commit/PR/build/performance qualification. Read Stream_Rock_Study.md for scope and current evidence.

## Stream study Pass4 — October31, 2026

User rejected Pass3's compressed edge rocks and explicitly requested fuller clusters with new individual stones through cluster-free bed. Current candidate **Stream Rock Study — Pass 4** (`stream-rock-study-pass4-2026-10-31`) is open in northeast district3, clear/summer, heading45/zoom1. Restores49 full scan patches from Pass2 while retaining Pass3 water/banks.564 individual stones use ten newly supplied variants (98 bounds above water), outside expanded cluster footprints. Source downloads `/Users/joelinstrum/Downloads/river-rocks`; private originals SourceAssets/IndividualRiverRocks and private prepared Resources/IndividualStreamRocks. Tools/Rocks/prepare_individual_rocks.py and IndividualStreamRockBuilder prepare meshes/color/normal/roughness; exclude the source scene/backdrop/HDRI. Read Stream_Rock_Study.md.

Validation passed compile, normal menu/district opening, ten variants actually drawn, zero individual-stone overlaps with expanded cluster rectangles and unchanged saved height/water-distance fields versus Pass3. Depth0.32–0.54m/width5.5–7.7m retained. Actual zoom1/2 captures inspected in current chat outputs/stream-pass-4; all15 pre-existing save/backup files unchanged. Previous studies preserved. This candidate awaits user visual feedback; Pass3's rejected edge-rock appearance is not an approved baseline. No commits/PR/publish/build/performance qualification requested or performed.

## Stream study Pass3 — October31, 2026

User approved recommended follow-up: retain width, restore restrained teal/reflection contrast, soften pale banks and bury smaller clusters at alternating edges. Current local candidate **Stream Rock Study — Pass 3** (`stream-rock-study-pass3-2026-10-31`) is open in northeast district3, summer/clear, heading45/zoom1. Earlier study IDs retain their treatments and files. Read Stream_Rock_Study.md. Exact-ID changes only: lower-scattering muted teal/stronger planar reflection, varied lower soil blend on banks, narrower lengthwise scanned patches with buried bases and at least1m conservative centre clearance each side. These remain scan patches, not separate closed stones; current shading does not physically flow around rocks.

Compilation and functional checks passed;49 patch placements, width5.5–7.7m/depth0.32–0.54m unchanged. Saved height and water-distance arrays verified byte-for-byte equal to Pass2. Actual zoom1/2 frames inspected in current chat outputs/stream-pass-3. Existing save/backup hashes preserved; no commit, PR, release build or performance qualification. Await user feedback before considering this an accepted appearance.

## Stream study Pass2 — October31, 2026

User requested broken-up rock coverage, roughly25% wider streams,25% narrower banks and less saturated shallow water. Current candidate is **Stream Rock Study — Pass 2** (`stream-rock-study-pass2-2026-10-31`), northeast district3, summer/clear, heading45/zoom1. Original study and other saves are preserved. Exact-ID scoped changes:49 scanned patches vs80, longer open reaches, smaller patch footprints, calibrated wider wet profile/narrower bank slope,25% narrower soil paint and neutral gray-green/clearer water. Normal river/ocean materials and generation unchanged. Current displayed samples:5.5–7.7m wet width,0.32–0.54m depth; mean width1.221× original, exposed-slope0.800× original. Coarse sampling prevents exact uniform percentages; do not claim measured25% on every cross-section.

Compilation, deterministic field, save round trip, normal menu/district loading, material values and width/slope comparisons passed. Actual zoom1/2 captures inspected in this chat outputs/stream-pass-2. All13 pre-existing save/backup files byte-identical. First calibration candidate measured only18% widening and was replaced after archiving it in chat work. No commit/push/PR, release build or performance qualification. Candidate awaits user visual feedback. Read Stream_Rock_Study.md for scope and limits.

## Stream rock study — October31, 2026

User requested the downloaded river rocks and a stream test region. Local branch `feature/stream-rock-preview` starts at merged main `1fe2eba`; no commit/push/PR requested. Read `Documentation/03_Technical/Stream_Rock_Study.md` for source/preparation, explicit fixture, scope and limits. Purchased source is preserved under ignored SourceAssets/RiverRocks; private Unity derivatives are Resources/StreamRocks. Original download is one connected 8m scan (14,645 triangles), not separate rock models. Example fluid cache is not imported.

New local **Stream Rock Study** (`stream-rock-study-2026-10-31`) uses an explicit1024m/2×2 field, open-meadow vegetation, shallow winding water, instanced scanned patches and approved native bank shrubs. Open Region retains normal relief overview; northeast district3 contains the inspection reach. Exact-ID presentation hooks leave ordinary generation/old saves unchanged. Approved Crest river/current/glints and native biome assets remain authoritative. Study is for summer visual review; physical obstacle flow, individual closed stones and performance qualification are not implemented. See current chat outputs/stream-rock-study for final checks/captures. Existing user-save hashes are checked separately. This candidate awaits user visual feedback; do not treat it as approved global stream generation.

## Tropical shelf depth and thicker map edges — October31

Verified version12 physical24m shelf depth, unchanged relative land relief, consistent water datum, unchanged inland fields, authoritative temporary save round trip, normal saved-region open, real Crest scattering input and absence of internal walls in a four-cell district. Final region and district captures at45/135/225/315 reviewed in chat outputs/tropical-depth-final. Boundary input padding removes the cyan fallback rim. Compilation and whitespace checks passed; original five user-save hashes unchanged. Unsaved four-cell Tropical shelf preview left open at45 degrees. The fixture uses explicit flat geography; the unchanged MapMagic worker creation path was not requalified in this pass.

New default recipe version12 gives Tropical ocean layouts a gradually deepening continental shelf: sea level30m, seabed reaching6m (24m depth) over12% of region width. Land and all explicit water surfaces shift upward18m together, preserving relative relief, river depths and shoreline location. Non-ocean and Temperate generation remains unchanged; versions1–11 and stored heightfields remain authoritative. Existing Tropical maps receive the offshore color presentation without regenerating their geography.

Tropical Crest scattering uses an owned256-square texture sampled in shared regional coordinates, transitioning from aqua nearshore to richer blue offshore. CoastalWaterPresentation applies its existing projection-dependent color multipliers to the input and retains adaptive sun-glare smoothness. Waves, reflection settings, shoreline foam and clipping are retained. Regional relief uses the same shelf blend for ocean colors. The depth probe captures the entire new seabed range.

Native districts now have an earth-colored exterior slab. Edge generation follows original member-cell adjacency, suppresses internal walls, preserves concave L-shaped boundaries and uses one common bottom. Owned meshes/materials are released with the landscape. Regional overview slab depth increases from2.5% to4.5% of map width. These edges are presentation only and do not alter saved terrain or gameplay colliders.

## Ocean visibility at consistent headings — October31

Final Unity checks passed actual ocean color sampling at45/135/225/315 degrees (all aqua, not white), original.62 gloss at the other three diagonals, adaptive.20 at the glancing-sun45-degree heading, perspective.8 restoration, single/combined orientation checks and actual coastal clipping masks. Captures reviewed in chat outputs/water-heading. Compiled and whitespace checks passed. Corrected coastal district left open. Earlier diagnostic material/light changes were play-only and discarded before final verification.

The45-degree heading fix exposed broad direct sun glare in Crest’s orthographic ocean surface: the whole tile could turn nearly white. CoastalWaterPresentation now smoothly reduces orthographic smoothness from the approved.62 to.20 only as the viewing direction aligns with the reflected directional sun (dot.85→.98). It updates as camera/sun directions change. Other headings retain the original gloss; perspective restores its captured profile. Climate absorption/scattering, reflections, foam, wave settings, lighting and heading preservation remain unchanged. No Crest/package shader or licensed asset edits were made.

## Consistent region/district heading — October31

Unity verification passed single/combined coastal entry at45 degrees, matching east/north camera-space directions, return after deliberate90-degree rotation, and existing coastal footprint checks. Actual captures reviewed in chat outputs/district-orientation. Compiled without errors; whitespace checks passed.

District entry now retains the current overview camera heading after all normal, coastal, mountain and combined framing paths. Return to Region retains the current district heading, including deliberate Q/E rotation. The former45-degree overview vs315-degree meadow/combined default caused east-facing coastlines to appear to rotate90 degrees on entry. Geographic heightfields, water footprints and cardinal axes are unchanged. Camera framing still chooses area-appropriate focus/zoom/depth; transition heading is applied before the preloader completes. Explicit saved camera restoration remains authoritative.

## Combined border colors — October31

Applied groups now use the same normal white border as other districts; only the pending selection is gold during Combine Districts mode. Shared IDs still hide internal edges. Live Unity applied/selection captures reviewed in chat outputs/combine-border-colors; original groups preserved, combine mode left off. Whitespace checks passed.

## Combined districts — October31

Final Unity verification passed selection/Apply, safe deselection, no diagonal/wrap/fifth cell, saved group/cache normal reopen, L terrain holes and native-tree confinement, canonical height agreement, expanded four-cell square, four-cell strip and its excluded padding, no-op cache retention and overlapping metadata rejection. Coastal L checked actual Crest clipping via GPU readback: selected water retained, omitted corner excluded; final315-degree capture reviewed. Evidence: chat outputs/combined-districts and combined-districts-coast. Compiled without errors; whitespace checks passed; all5 existing user-save hashes unchanged. Unsaved combined coastal overview left open.

Region HUD has COMBINE DISTRICTS (gold active state), gold union selection outline and APPLY enabled for2–4 original edge-connected cells. Diagonals, wraparound, disconnected additions/removals and more than4 cells are rejected. Existing groups select atomically and can expand only within the same total4-cell limit. Applied groups use the normal white outer boundaries and hide internal grid lines. Gold is reserved for the pending selection while Combine Districts is active. Turning the mode off discards the pending selection. Apply changes the session; Save Region/Save District persists it.

`CombinedDistrictGroup.cells` is additive saved metadata; original grid IDs, shared geography and recipe are unchanged. Groups validate on both read and write for size/connectivity/uniqueness/overlap. Clicking any member opens the same minimum-ID anchor and full union. Native district terrain samples the canonical shared field across a square enclosing the group, using terrain/collision holes to exclude unselected cells (including L-shapes and strip padding). Tree placement, roads, snowfall/caps and water masks honor the same footprint. Combined terrain control maps are bounded to2048 samples. The merged camera frames the full area at the approved315-degree native view, with depth accommodating absolute terrain height.

Applying a changed group invalidates member terrain caches to match the new geometry; reapplying the same group retains its cache. Existing one-last-district save-cache semantics remain, now keyed by the group anchor. Shader grouping IDs remove internal borders without changing region geometry/picking; HUD shows the resulting logical district count. No saved region is automatically rewritten.

## Seeded Mountain Border variation — October31

Verified normal worker-backed creation for all3 patterns, repeat-seed field equivalence, rear-quarter confinement,1/2/3 cap policies, main district entry, unchanged Land output and temporary save round trips. Single/twin/full overview captures reviewed in chat outputs/mountain-variations. Compiled without errors; whitespace checks passed. Unsaved twin-summit preview left open.

New default recipe version11 adds three seed-selected silhouettes within the approved rear-quarter natural range: a higher wall with one dominant summit, a two-summit ridge, or the approved fuller segmented ridge. Seed controls summit positions/widths and existing noise detail. Cross-range profile, foothill blending and restrained upper-slope uplift remain. Snow caps follow the silhouette (up to1/2/3 separated summits), shared by region and district views. Other shapes and versions1–10 remain unchanged; stored geography is authoritative.

Create Region now assigns a fresh visible seed each time the creator is opened, so consecutive new regions vary without manual seed edits. Navigating wizard pages retains it. Entering the same seed reproduces the same geography; changing an existing save or regenerating on load is not involved.

## Raised summits with permanent snow caps — October31

Verified exact version9 equivalence below the156m lift threshold, restrained summit uplift,3 separated snow caps, snow-free low slopes, deterministic fields, rear-quarter confinement, unchanged Land output, temporary save/load and normal worker creation / detailed district. Peak relief rises339.5→394.5m for the reference seed. Region/district snow captures inspected in chat outputs/snow-capped-border. District overlay receives shadows and survives Clear. Compiled with no errors; whitespace checks passed; preview left on overview.

Default recipe version10 preserves the approved compact rear-quarter range. After MapMagic erosion, only Mountain Border samples above156m of relief receive a30% lift of the amount above that threshold. All lower height samples stay exactly identical to version9; crest placement, reach and foothill transitions remain unchanged. Other shapes and versions1–9 retain their prior geography.

`MountainSummitSnow` chooses up to3 separated high local summits from the stored canonical field, then applies height-limited, irregular snow caps. Overview atlas and detailed terrain overlay share that selection/mask. Caps leave water and lower slopes clear, receive shadows in districts, and remain when temporary weather is cleared. They change presentation only; no canonical map/tree data are painted or rewritten. Runtime meshes/materials/masks are owned and destroyed with their district. Existing saved maps remain untouched.

## Compact mountain reach — October31

Unity checks passed rear-quarter confinement (interior relief7m), deterministic fields, legacy Land equivalence, Flat override, temporary save/load and normal worker creation/district entry. Actual overview visually reviewed in chat outputs/compact-mountain-border. Compiled with no errors; whitespace check passed. Corrected unsaved overview left open.

Default recipes are version9. The approved natural north/back Mountain Border is compressed only across its depth: the previous40%-wide taper envelope is mapped into the rear25% of the region, retaining its along-range shape and amplitude. The creator preview uses the same rear quarter. Versions1–8 and stored maps retain their original geography; other terrain shapes are unchanged.

## Natural Mountain Border correction — October31

Verified version8 deterministic fields, back-edge confinement, unchanged Land output, Flat override, temporary save/load and normal worker-backed creation / detailed district. Actual overview reviewed against the user reference; evidence in chat outputs/natural-mountain-border. Test recipe matches Test Region i seed730421/topography3. Existing user saves remain unchanged; unsaved corrected preview left open on overview. Compilation and whitespace checks passed.

The user rejected the exaggerated version7 ranges. Default recipes now use version8: Mountain Border reuses the approved version5 river-flanking massif formula (same ridge widths, taper, peak variation, mild ribs and normal amplitude), translating the two sections from one side of the river to the north/back edge. It does not use the rejected tall triple-ridge formula. The rest of the map remains gentle; the Terrain preview marks the same north edge. Flat Environment still provides a moderate mountain border. Other terrain shapes are unchanged. Version1–7 and all stored fields remain readable and authoritative.

New version8 districts retain the normal approved meadow presentation used by Test Region 1; version7-only rock/scree and framing changes are isolated to version7. No source save is regenerated or changed.

## Rugged Mountain Border — October31

Verification passed deterministic new fields, one-sided confinement, Flat override, unchanged Land output, version7 temporary save/load, normal worker creation and approved mountain district materials. Seed730421 Small peak rises from321.8m to1029.9m above the regional base; interior relief remains6.4m. Overview and district captures inspected in chat outputs/mountain-border. Source compiled and whitespace checks passed. An unsaved new Mountain Border preview is left open; existing user saves are unchanged.

New recipes default to version7. Only Mountain Border geography changes: three wandering ridge divides with high summits, deep intervening valleys, restrained rocky ribs and lower foothills, all confined to the seeded edge and fading out by32% of the region width. Other shapes retain their version6 geography; versions1–6 and stored maps remain authoritative. MapMagic erosion, worker-backed creation and existing physical1200m terrain range are retained.

New Mountain Border districts use the approved mountain world-projected rock/scree surface layers (runtime baseline version107), with meadow lowlands retained. Entry frames the actual district height range rather than the meadow comparison point, so tall peaks fit. No licensed assets or saved terrain are rewritten.

## Gradual clearing and snow shadows — October31

Clear now tapers emission and existing-particle opacity over4 seconds, eases fog away over4 seconds, and melts full snow cover over10 seconds using shrinking spatial patches. Partial cover melts at the same rate. Selecting Snow during a melt resumes from remaining cover; incompatible season changes use the same clearing transition. District exit still cleans up immediately. `PrecipitationStrength` and `IsClearing` expose transition state for checks. Snow receives directional tree/object shadows with correct URP screen-space shadow coordinates, soft-shadow variants and stronger diffuse shadow contrast. No canonical terrain/tree/save data changes.

Unity checks passed partial melt/taper, fog/particle completion before remaining cover, full melt completion, existing seasonal/water/canonical/navigation checks. Shadow-on/off close captures and midway melt inspected in chat outputs/weather-gradual. Original Tropical district restored in clear weather.

## Temporary district precipitation — October31

Unity verification passed normal saved-region/district loading, invalid weather rejection, seasonal control gating, falling particles, partial coverage at2 seconds, full fog at4 seconds / coverage at10 seconds, water exclusion, canonical tree/height stability, Clear reset, overview cleanup and Tropical winter rain-only. Wide/close captures inspected in chat outputs/weather. Compilation and whitespace checks passed; all four original user-save hashes unchanged. Weather is left clear on the original Tropical district.

`DistrictWeather` owns district-only TEMP WEATHER controls (Rain, Snow, Clear), camera-following procedural particles and a terrain-conforming snow overlay. Snow requires `VisualSeason.Winter`; Rain requires a nonwinter visual season, so Tropical stays rain-only year-round. Season changes stop incompatible weather. Snow fog fades in over4 seconds; dry-land coverage reaches full at10 seconds. Both canonical water distance and actual submerged-shore distance mask the overlay. Clear and district exit restore the previous fog and remove temporary cover; weather never mutates or persists terrain/tree/save data. Resources/Weather contains authored runtime shaders for player retention. Approved base terrain, trees and water remain authoritative.

## District actions and climate seasons — October31

Verified normal saved-region loads for Tropical Winter and Temperate Summer, calendar transitions, disabled/enabled controls, prototype/canonical/palm/shadow stability, saved calendar round trip, top-button district→overview→menu navigation, cache retention and original region restoration. Evidence: chat outputs/district-hud-climate. Source compiled; no user saves rewritten.

District HUD now says SAVE DISTRICT and RETURN TO REGION. Save uses the existing region snapshot including the active district cache, with DISTRICT SAVED feedback. Return opens the same region overview; overview retains SAVE REGION / RETURN TO MENU. The duplicate REGION OVERVIEW district button is hidden. This does not introduce independent per-district save files or change the one-last-district disk-cache contract.

`PrototypeRegionView.Season` remains the persisted calendar state. `VisualSeason` is always Summer for Tropical climate and otherwise equals Season. Tropical Fall/Winter buttons are disabled and dimmed. Native prototypes, authored-canopy visibility/replacement masks, legacy authored palms and seasonal shadow treatment use the visual season, so a winter calendar event or winter save cannot defoliate Tropical forests. Direct landscape seasonal calls also honor climate. Temperate Fall/Winter behavior is retained. No saved canonical trees/maps are rewritten.

## Peninsula north-to-south orientation — October31
Verified normal Create Map -> north-attached Peninsula -> coastal district61; Crest/palms/dry-road checks and persisted direction passed. Actual overview/preview screenshots reviewed in chat outputs/peninsula-north. Source compiled and whitespace checks passed; existing save hashes unchanged.

New wizard Peninsula maps attach to the north edge and extend south, in both the preview and actual geography. `peninsulaNorthToSouth=true` is stored in new wizard recipes; older recipes without the flag retain their original seeded orientation and stored fields. Coast/Mountain Border orientation remains seed-selected. Terrain choices feed normal worker-backed creation and detailed districts, not just previews. Earlier peninsula seed-orientation statements below are historical.

## Peninsula and Archipelago correction — October31
Peninsula follow-up validation passed: one land component and exactly one land-connected boundary, sampled shoreline/height agreement, deterministic generation and save round trips for all eight serialized shapes, six current UI labels/mappings, normal Peninsula and renamed Archipelago creation, and a real Peninsula coastal district with Crest/palms/dry-road assertions. Unity captures reviewed; compilation and whitespace checks passed. Private evidence: chat outputs/peninsula. Creator left on Peninsula with Environment defaults restored.


Current creator order: Peninsula, Land, Coast, Mountain Border, Pangea, Archipelago. Peninsula replaces the temporary four-large-island choice, with one connected landmass touching exactly one map edge and water on the other three; the seed chooses its orientation. Archipelago is now the renamed nine-small-island option. Pangea remains the renamed single-island option. Serialized IDs are preserved: Peninsula appends7, Archipelago UI maps to SmallIslands=5, and the previous four-island Archipelago=6 remains readable but hidden. No existing terrain/save is reinterpreted. Earlier choice lists below are historical.

## Terrain naming correction — October31

Creator order is now Archipelago, Land, Coast, Mountain Border, Pangea, Small Islands. Current Pangea replaces One Island and uses that existing single-island plan; former broad-continent Pangea is hidden but its serialized ID/geometry remains compatible. New Archipelago is appended enum6 and produces four larger islands; Small Islands still produces nine smaller ones. Do not renumber enum values or reinterpret existing saves. See Region_Terrain_Shapes.md. Previous six-choice lists below are historical.

## October 31 — Region creator Terrain step

Final six-shape recipe/persistence/UI/worker-creation checks passed, with actual coast/island district water/palm/dry-road assertions and reviewed Unity captures. ARM64 release build passed with zero errors; existing three save hashes unchanged. Private evidence: this chat outputs/terrain-step3. Creator left open on step3 (Land/default Environment).

New wizard flow: Basic Settings → Environment → Terrain → Create Map. Terrain offers Pangea, Land (default inland behavior), Coast, Mountain Border (one seeded edge), One Island and Small Islands (nine islands), with seeded outline preview. New recipes are version6; all stored maps/version1–5 behavior remain authoritative. Ocean descriptors support serialized land holes; beaches/surf/palm bands use those shorelines and ocean bounds no longer misclassify fully inland island districts. New ocean-layout pilot roads stop at dry runs instead of crossing water. Metadata cap is bounded1 MiB for archipelago contours. Read Documentation/03_Technical/Region_Terrain_Shapes.md for generation, persistence, checks and limits. Prior statements that the wizard cannot generate coasts are superseded. No save migration or public licensed/generated art; no commit/push authorization.

## October 31 — coherent gameplay canopy

Verified normal cached/uncached Tropical districts, Temperate no-palms, legacy coast/meadow, seasons/save capture, district reentry and corrected fresh-instance disk reload. All three save hashes unchanged. M1 Max standalone at 3434×1728/Ultra/100%: before→after FPS near 57.85→58.60, low 31.67→30.27, distant 40.97→35.81 (15 foreground seconds each, not sustained qualification). Distant performance is a remaining limitation. Crown-bound neighbor coverage after palms 99.69%, ≥3 neighbors 94.59%; 962 broadleaf/1,647 total palms in user district44. Original low view is obstructed; supplementary matched low pair raises focus20 m. Temporary harness removed.

Coherent touching/overlapping refined-C patches are required ordinary forest behavior. Generated districts now grow deterministic crown-aware groups at canonical mature placements within the existing 25% replacement budget, with geography/trunk-spacing checks; random per-tree replacement is superseded. Tropical approved palms prefer fringes/openings at unchanged 30% inland/50% ocean-shore quotas. Native palms and wind remain. The already clustered recipe-less supplemental layout and frozen replay remain compatible. `FoliageBrightness=1.0` restores locked C tint (supersedes the later 1.25 boost); no approved mesh/shader/palette/global lighting edits. See Approved_District_Integration.md for algorithm/limits and private chat canopy-clusters evidence for current validation. No canonical save/terrain rewrite or public licensed/generated art. Performance at current Ultra/100% must be read from the new measurements, not historical 80% trials.

# CityForge V4 — main development handoff

Updated 2026-10-31. Read this before changing terrain, foliage, map creation, or rendering.

## Generated districts — October 31, 2026

The user created Tropical `Test Region i` and reported missing olive crowns/palms. Its saved recipe/climate were correct. Palms were wrongly coastal-only for their intended Tropical-region behavior; the authored canopy used a supplemental central test grove rather than replacing trees throughout generated forests. All detailed Tropical districts now get the30% base palm mix (50% within75 m of actual ocean shoreline). Generated recipe-based districts replace a deterministic25% of mature native placements with approved summer crowns; palms can replace a share of either population. Canonical arrays/caches and saved terrain are preserved, and palms stay fixed across seasons. Recipe-less reference tests retain their approved layouts. See the October31 section in Approved_District_Integration.md and outputs/district-tree-integration in this chat for evidence. The user's existing save is preserved; no recreation is needed. No additional publish/commit/PR authorization was given.

## Tropical coast follow-up — October 30, 2026

The user approved the light aqua Tropical coastal water. Climate now stores Temperate/Tropical independently of moisture, defaulting old maps to Temperate. Coastal material absorption/scattering change only for Tropical; the accepted swash and other environment settings remain intact. The wizard still generates inland maps. See the latest section in `Documentation/03_Technical/Coastal_Shoreline_Trial.md`.

The user then supplied an eight-model coconut pack and approved 30% palm replacements away from shore, rising to 50% within 75 m of semantic ocean shoreline. These coconut variants supersede the initial 20% V3 date-palm trial. All eight have four LODs and share texture atlases; the supplied FBX contains no animation tracks. Canonical tree arrays, save data and the refined-C checkpoint remain untouched. Source/preparation details and verification are in `Documentation/03_Technical/Tropical_Coastal_Palms.md`. The user approved the coconut visuals, then requested wind at the two closest zoom levels. Procedural GPU wind now targets zoom0/1, fading to still palms at zoom2–5; the supplied FBX has no vendor animation clips. See the close-zoom wind section in the palm document. No sustained performance result is claimed. Local work is on `feature/tropical-coastal-water`, based on fetched `origin/main`; no new commit/push/PR has been requested.

## District entry integration — October 30, 2026

The user reported missing approved landscapes/trees/water when clicking districts. The single live save is the original recipe-less 2×2 Meadow Test Valley. Its southwest district reproduced `NaturalLandscape=false`: the historical gate only admitted district3 (plus special lake/mountain/recipe cases). The hidden legacy tree preference was a separate visibility gate.

Local source now selects the native meadow for every district, enables native trees on entry, adds the refined-C summer canopy via private prebuilt assets/instanced rendering, automatically applies accepted Crest river glints in river-only districts, and routes saved ocean districts through coastal Crest surf. See `Documentation/03_Technical/Approved_District_Integration.md` for asset preparation, limitations and validation. This supersedes the earlier statements below that approved forest/river presentation is exclusively an editor study, within the scope described there. Original checkpoint and public approved shader/color recipe remain immutable. No push/merge is authorized by this integration task.

Validation passed for all four original saved districts, an unsaved v5 generated district, the frozen 16-capture forest replay, all 70 mesh parts and the normal ARM64 player menu/coast/river/cache path. Player visual debugging found stripped terrain `_NORMALMAP` / `_MASKMAP` variants; the private asset builder now retains four material combinations without changing the accepted art or sunlight. Keep that preparation step in future builds. Authored fall/winter variants, discrete-LOD crossfades, independent estuary materials and sustained city performance remain separate gaps. Player save remains byte-identical. See the integration document for exact scope and evidence.

## User-requested palette follow-up — October 30

The user found integrated authored trees nearly black and shallow legacy rivers too similar to grass. Runtime leaf tint now has a 1.25 brightness multiplier; native pale foliage, geometry and the frozen refined-C checkpoint remain unchanged. The runtime river profile uses absorption (1.8,1,.32) and scattering (.024,.24,.44) for clearer blue/teal separation; current/glints, sunlight, grass and coast remain unchanged. See the integration document for the exact scope. These runtime adjustments supersede its earlier unadjusted appearance and remain local/uncommitted.

## Panning follow-up — October 30

User clarified zoom3/4 jaggedness as uneven motion. Native VSync now selects the refresh divisor closest to 60 Hz (2 at 120 Hz; 1 at 60 Hz) and restores prior settings on meadow exit. Uncapped toggle and existing motion quality remain. Approved authored batches combine palette colors (35 mesh/LOD groups instead of105); shader/probe uploads use actual counts and lists are preallocated. Appearance, geometry, palette and frozen replay are unchanged. See the integration document and pan-review player evidence.

## Approved authored forest canopy — October 28, 2026

The user approved the authored Tree06 broadleaf forest, its crown/trunk geometry and **refined C** olive palette, and requested a locked checkpoint/PR/handoff. This is distinct from the retired downloaded maples below. See `Documentation/03_Technical/Approved_Forest_Canopy.md` before further foliage/rendering changes.

- Preserve seven variants, accepted scale/crown spread/leaf size, touching canopies, refined C dark-tree hue, slightly warmer/dimmer pale native trees, subtle intra-crown color variation and earthy desaturated grass. The authored recipe is `ApprovedForestColors.cs` and `ApprovedForestCanopy.shader`; normal gameplay is not automatically switched to this local study.
- Private immutable checkpoint: `SourceAssets/ApprovedForest/RefinedC-2026-10-28/`. Verify/replay using `Tools/Trees/approved_forest_checkpoint.py`. Meshes/licensed textures/derivatives/builds/screenshots stay outside public Git. Missing local art is an explicit error, never permission to restore retired forest assets.
- Approved performance setting: 80% scene render scale, full visible crown/PBR shading and configured 4K shadow map, coarse authored shadow geometry, depth priming off. The audited **pre-final-color** M1 Max run held ~60 FPS over 90 foreground seconds with 600 authored + 2,070 native mature trees. Final refined C is visually approved but must be remeasured after color baking/streamlining; no full-city guarantee or impostor qualification.
- Source-only CLI pilot pins Unity Pipeline0.9.1-exp.1 and documents CLI beta14 / Editor6000.1.12f1. Direct Editor/CLI builds work without Hub. Preserve the user's Keychain and credentials; do not revive the beta8 permission-prompt workaround attempts.
- This checkpoint is the next session's art baseline. Preserve native forest/saves/water/camera, Gamma/Ultra and clean MainMenu. Remaining work: lean production integration, color baking and city/performance validation, not further visual redesign without user direction.

## Downloaded maple trial retired — October24, 2026

The user rejected the downloaded maple assets and requested their removal from the game. All import/close-view/population/opaque-cluster/leafy-branch preview scripts, controls, custom foliage shaders and rebuild tools were uncommitted experiments and have been removed from the active project. The unsaved trial was stopped; no trial placements or LOD/shadow overrides remain active. Native meadow/poplar, the pre-existing Silver Maples (mask3), seasons, camera, water settings and player saves are unchanged.

Local imported/generated trial artwork was moved outside Assets to ignored `SourceAssets/MapleTrial/RetiredRuntimeAssets-2026-10-24`; original supplied sources/textures remain in SourceAssets/MapleTrial. Authored trial code and detailed historical reports are archived outside the project in this chat workspace under `work/maple-trial-retired-2026-10-24`. Do not revive any of these candidates as a forest fallback. See `Documentation/Migration/Downloaded_Maple_Trial_Retired.md`. Public Git remains source-only; no licensed artwork, source archives, player saves or builds are committed.

## Session close — approved water and camera, October22

The user approved the final river appearance and requested a PR and next-session handoff. This section supersedes older statements that the latest selective/evolving treatment is awaiting approval. Branch: `feature/cityforge-next`; base: `main`. The PR also includes checkpoint9e538d8 (district river/terrain fixes and optional wildlife).

- Approved camera: upper tilt35.26439 degrees (pole35.3), lower12; river inspection heading45. Region/meadow diagonal headings remain45+90n. Pole and I toggle work; old steeper saved camera states clamp on load without rewriting files.
- Approved river preview: native flat Crest grid, authoritative stationary district clipping, blue/teal depth colors, downstream current0.55m/s, selective sun glints, evolving lighting-normal slopes0.018 at speed2. Above31 degrees, smooth restraint reduces highlight coverage and gain; final top-angle gain0.20. Preserve31-and-lower response and all these settings.
- This is still an **editor preview integration**, not automatic production district water. Normal district loading retains its bounded fallback. Open the intended river district, then use the editor preview menu/request. Do not claim the accepted shader is already the gameplay default.
- Rebuild local shader: write `build` to `Library/RiverGlintShader.request`; check `Library/RiverGlintShader.txt`. It generates Git-ignored `Assets/CityForge/LocalWaterStudy/RiverGlint.shader` from the installed licensed SDK, using authored `RiverGlintResponse.hlsl`. Do not publish generated shader/SDK/artwork/saved maps.
- Apply approved preview: use `CityForge/Water/Preview selective river glints` (profile speed2) or the chat work request `river-evolving-glints-trial.json` with sunlightView=true, yaw45, pitch35.26439, projection0. Latest live district is unsaved, upper isometric/zoom2, approved glints active. The bounded-preview menu restores the native baseline; removing the Crest preview restores bounded fallback.
- Local dependencies: licensed native meadow/Crest/animal/bird assets are intentionally absent from public Git. The existing installed Crest `Surface/Normal.hlsl` also has the earlier CityForge opt-in advection hook used by RiverFlowAnimation; it was not changed or published in this wrap-up. A clean SDK install will need equivalent integration for the downstream normal advection. Do not assume a fresh public clone reproduces the local view unaided.
- Remaining engineering: shader evaluates PBR twice, needs performance qualification/optimization, and thresholds are exposure-dependent artistic controls. Evolving normals affect URP lighting, not a fluid simulation or Crest mesh displacement/refraction. Shader builder uses an internal installed-version ShaderGraph API. Automatic river deployment/portable licensed integration are future work, not silently authorized changes.
- Verification: latest camera suite passed61 positions,35.3 cap, legacy85 restore, centering, I toggle, projection matrices and JSON round trip. District GPU clip coverage and100/-80m pans passed at zoom0/2/5 after camera change; final glint restraint subsequently captured24 frames each at31/35.3 and zoom1/2 with shader/clip checks. Player saves unchanged. Earlier wildlife/geography checks are historical, not freshly rerun at session close.

## Earlier accepted state and project context, October19

This summary supersedes historical four-district and earlier palette descriptions below.

- New wizard regions have 64 districts (8×8); legacy saves/test regions retain their original grid. Main-menu opens the relief overview; clicking a cell loads only its detailed native meadow biome behind the preloader, at zoom5.
- Wizard seed, topography, rivers/streams, moisture, vegetation and size influence shared geography/ecology. Era/period are metadata only. Wizard oceans are not implemented; coastline tests remain separate.
- Current new geography recipe is v5: MapMagic Noise → CityForge range constraints → MapMagic erosion, then connected river carving and biome composition. Generation/composition run on workers. Stored heightfields remain authoritative.
- Accepted regional appearance: midpoint olive greens (grass 0.43/0.47/0.285; woodland 0.265/0.325/0.18), warm exposed rock, deep-blue rivers with NO river border. New thin tan beaches apply only to semantic ocean outlines. The map now has dark earth slab sides; framing includes their bounds. Overview mesh513², atlas1025², presentation-only height exaggeration1.25×.
- Preserve the native MeadowRuntimeBaseline / MeadowDistrictLandscape / SeasonalTreeLibrary. Retired prototype trees stay absent. Both Silver Maples are enabled locally (mask3); six optional oaks off. Firs remain restricted to Hills and Mountain Test. Public clone excludes licensed assets.
- Preserve VSync, temporary movement quality reduction, close pan factor2.5× at zoom1–3, and district entry zoom5. Preserve approved ocean surf and clock0.45 (~17seconds).
- Save/load preserves creation recipe, grid and one last detailed district cache. Independent edited-district persistence is still future work. Delete on Open Region archives recoverable saves locally.
- Latest RegionalReliefChecks passed shader/slab checks, all64 picks, shared edge, native seasons, district isolation, cache and temporary save round trip; coastal slab preview inspected. Earlier MapMagic/district-loading checks passed before the last presentation refinements. Do not describe them as freshly rerun on every palette change.
- District river repair (October 19): v5 detailed rivers now use a stationary bounded calm surface reaching the saved terrain shoreline, with native soil banks. October20 adds optical lengthwise downstream currents in the bounded shader, following centerline bends; no displaced waves or shore-lapping foam. Runtime water mapping/capture uniforms now have hidden shader properties so Unity reload retains them. The first current trial was too subtle; river-only scene-depth absorption makes underwater animals more readable. User rejected the painted-current appearance even at27.5% strength and supplied a fine-ripple river reference. The analytic-wave trial was rejected as television static. Latest provisional river treatment uses the existing local Crest normal texture with mipmaps, two-phase coherent downstream advection, broader low-frequency detail and restrained reflection glints. Four variants were inspected at gameplay zoom1/2; the broader variant was selected. User reported it is a lot better and requested faster movement; river advection is now doubled from0.275m/s to0.55m/s, with appearance unchanged. Future work should address river reflections/lighting and riverbed/shore presentation, rather than claim more small shader tweaks will achieve the image. See the latest visual-review notes in Regional_Relief_Map.md. Ocean, lake and pond styles are unchanged. Orthographic cameras retreat to avoid terrain clipping and compensate forest culling distances. No regional geography/palette or ocean settings changed. Focused checks and visual review are described in Regional_Relief_Map.md; user reported the river repair is much better.
- Optional native Crest river preview (October20, after checkpoint9e538d8): user approved blue/teal colors and sharp directional sunlight glints. `CrestDistrictRiverPreview` now uses the native flat Crest grid with stationary world-space clipping, rather than the unsuccessful custom MeshRenderer route. A padded inverted clip quad plus a1024-square wet/dry mask from the displayed saved terrain prevents exterior overflow and exposed water under raised tile edges. All wave displacement remains disabled. `CrestRiverStudy` captures zoom0/2/5 when boundaryCheck=true and checks stationary clip coordinates, geographic domain bounds and shader compilation. The first clipped preview still cut back one river end: the inherited maximum scale32 and four cascades covered only1024m around the moving focus. October20 follow-up explicitly sets scale range8–256 and eight cascades; the new wide capture fills that previously exposed riverbed through to the tile edge, while the exterior remains clipped. Close colors/glints were visually checked. Final preview checks passed at zoom0/2/5, before and after100m/-80m focus pans: the outer GPU clip cascade covers the complete district with a two-texel margin on every captured frame, while the clip transform stays fixed. Captures were visually inspected close and wide. This is a single-district preview, not yet general production integration or a performance acceptance. Earlier assertions that a cloned fixed mesh was the visible Crest surface were insufficient: the generated grid was still visible, and must not be repeated as a rendering result. Native grid vertices can move internally; the visible wet footprint is geographically clipped. These changes are still optional/uncommitted; production creation uses the checkpoint bounded shader. Profile: `Assets/CityForge/Scripts/Editor/RiverSunGlintPreview.json`; menus under CityForge/Water. Remove preview before running the older fallback-only river motion checks. The pebble overlay remains off. No player saves changed.
- Camera tilt inspection control (October22): district HUD now has a lower-left vertical CAMERA TILT pole/dot with live degree readout. Drag bottom12-degree low-angle to the existing isometric pitch; I still selects endpoints and dot stays synchronized. Focus/heading/zoom remain fixed. Intermediate cameras use standard perspective matrices with narrowing FOV at matched focus-plane scale; the top is native orthographic. A custom matrix interpolation was rejected because it broke URP water-depth reconstruction. Normal water profile remains untouched. Intermediate tilt is optional camera metadata (hasViewTilt/viewTilt), retaining legacy bool-only restore. CameraTiltChecks passed61 slider values, focus centering, finite projections, endpoints/I, JSON round trip and unchanged water material; UI and middle/top rendered captures inspected. Current restored river preview left low-angle. No player saves written.
- Low-angle baseline restored (October22, supersedes fixed-world experiment): user rejected fixed-world water as embossed wallpaper and preferred the earlier low-angle view. Removed CrestRiverWorldNormalChunk and per-ring scale/blend overrides entirely. Profile restores native Crest normal texture, scale4/strength0.08/smoothness0.97/reflection0.25, with no camera-dependent water adjustment. normalBlend0 is the default. An authored optional RiverNormalBlend shader layers rotated/offset normals into an in-memory mipmapped texture; full-size layering kept broad glossy bands, while scale2/strength0.065 softened them but did not solve the isometric appearance. Both low/isometric zoom1/2 captures inspected; zero-normal control confirmed the pattern comes from normal shading. Layered trial remains disabled; user-approved baseline active. Existing clip checks passed, saves unchanged. Isometric appearance is unresolved: do not claim this restoration or trial fixes it.
- Isometric Crest river follow-up (October20, superseded by October22 fixed-world experiment): user rejected broad glossy/plastic ridges from above while approving first-person water. Preview profile now enables adaptive ripple scale: native normal texture and original scale4 remain in the low12-degree view; elevation15–35 degrees blends toward scale0.8 overhead, with square-root cascade-scale compensation to restrain zoom inflation without completely filtering distant texture. Normal strength0.08, smoothness0.97, absorption/scattering, reflections and current speed remain unchanged. Softer trials lost glints; an optional generated normal trial introduced patterned shimmer and is disabled (natural=false). Original normal texture restored for both views. Isometric close/wide and100m/-80m pan captures passed existing clip-coverage/shader checks; lower camera was inspected over visible water. Some regularity and spatial cascade changes remain visible: this is a provisional improvement awaiting user acceptance, not a matched photographic reference. CrestRiverStudy can explicitly select projection and label captures; waterView searches for a terrain-clear lower inspection position. No player saves changed.
- District slope blending (October 19): v5 detail now uses shared monotone cubic heights, terrain pixel error1, matching native collision, vertically re-seated cached trees and finely tessellated road ribbons. Saved heights, approved materials and regional relief remain intact. User approved the smoother result.
- Summer maples (October 20): both Silver Maple summer leaf tints changed from RGB(.25,.45,.28) to (.20,.32,.25) for darker, more muted forest green. Only six GreenLeaf materials and the import palette changed; autumn/winter, bark and other species remain intact. User approved the darker result.
- Temperate wildlife (October 20): new-region Animals option defaults to Temperate wildlife; None disables it. Existing saves default off and can opt in through the district ANIMALS button; Save Region persists the choice. Ten locally imported Animal Pack Deluxe species (including user-requested bank/shallow-water crabs) plus seven Living Birds species (ground and flight variants) use habitat-limited placement and safe idle/walk/swim loops. Farm, tropical and marine animals are excluded. WildlifeChecks passed new/legacy regions, wizard controls, habitat/movement, animation, deterministic placement and temporary save round trips. Player saves were unchanged. Missing legacy creation recipes now remain null instead of acquiring current grid/scale defaults. See Documentation/03_Technical/District_Wildlife.md.
- Remaining map work: wizard coastline integration, more natural confluences and drainage networks, settlement data/markers, and independent per-district edited storage. Wildlife now renders only at close zoom0–2, with population count and FIND ANIMAL inspection control; script reload retains the population list. Land wildlife density is now about3× the initial trial, plus a larger bird population in openings and above woodland. Installed packs contain no squirrels. Further wildlife visual feedback is pending.

Work directly in the existing project and preserve local assets/work. This public repository is source-only. Never publish commercial art/packages or player saves. Read AGENTS.md before editing. No automatic save regeneration.

## Current geography generator — MapMagic2 trial (October 19)

New recipes default to **version 5**, using MapMagic2 Noise → CityForge range/valley constraints → MapMagic2 Erosion for the shared regional elevations. The earlier version4 Noise → Levels → Erosion trial is preserved for legacy recipes. See `Documentation/03_Technical/MapMagic_Regional_Geography.md`. This supersedes the earlier custom-noise generator as the default for new maps, while version1–3 maps and all stored heightfields remain intact. MapMagic does not own terrain rendering, trees or materials: retain RegionReliefMap and the approved native district biome. Generation runs on a worker behind the creation preloader; districts still use their own preloader and start at zoom5. Existing river routing is retained and is not MapMagic-generated drainage. The new terrain is a trial awaiting visual feedback. MapMagic/provider and full expanded-biome integration checks passed. Do not rerun the historical MapMagicMeadowStudy authoring tool as a gameplay baseline.

## Current regional architecture — October 19, 2026

**New regions now use 64 districts (8×8), with a separate relief-map overview.** This supersedes the earlier four-district/full-meadow-overview descriptions below. Main-menu loading starts at the regional map; click a cell for its approved detailed biome. Only that district is rendered. Older saves retain their original 2×2 layout and dimensions. Medium new regions are 8,192m across, with 1,024m districts. Keep the approved native biome inside districts; do not put individual trees back into the regional map.

See `Documentation/03_Technical/Regional_Relief_Map.md` for grid/save contracts and limits. City/town markers await actual settlement data. Current persistence retains one last district cache; independent edited district storage is a future requirement before multi-district construction. RegionBiomeChecks and RegionalReliefChecks passed; no player saves were rewritten. Both Silver Maples are enabled locally, oaks remain off, VSync and movement quality remain in place. Ocean presentation is unchanged.

## Approved biome and appearance

The current biome is the approved **temperate native meadow / poplar woodland**, rendered by `MeadowDistrictLandscape`, with the user-authorized maple/oak forest mix described below. It is not the early prototype forest.

Authoritative local assets:

- `Assets/CityForge/Resources/Region/MeadowRuntimeBaseline.asset`: approved terrain material, terrain layers, vegetation prototypes, lighting, camera and water templates.
- `Assets/CityForge/Art/Environment/MeadowNativeURP/Integration/Polish/Materials/Natural Transitions Terrain.mat`: approved meadow terrain material. Preserve its grass colors and texture treatment.
- `Assets/CityForge/Art/Environment/MeadowNativeURP/Integration/Polish/Layers/`: approved meadow soil, quiet grass and leaf-litter terrain layers.
- `Assets/CityForge/Art/Environment/MeadowNativeURP/Showcase/Flora/`: approved scanned poplars, young poplars, grey willow and maple bushes, plus meadow flora.
- `Assets/CityForge/Resources/Seasons/SeasonalTrees.asset`: matching Summer, Fall and Winter prototype arrays. Use `SeasonalTreeLibrary` and `SetSeason`; retain tree positions and prototype indices.

Generated version-2 biome regions use these assets in BOTH the full-region overview and each district. Overview and district views must not silently switch to prototype trees or ground. Existing test maps retain their established terrain/water behavior. If an unsupported older view lacks approved foliage, leave its retired trees absent rather than restoring the old forest.

## Retired tree: do not restore

The old runtime forest selected `Trees/CityForgeBroadleaf` through `ForestPresentation.json`. Its prefabs have been moved outside Unity to `SourceAssets/RetiredPrototypeTrees/`; that archive is local and excluded from the public source repository. `PrototypeRegionView.BuildTrees` and `AddTrees` are intentionally inert, and `DistantTreeRenderer` is an inert compatibility shell. Old saves do not authorize restoring these trees. Historical tree importers/evaluation scripts are not the current visual baseline and must not be used to select gameplay foliage.

Do not “fix” missing approved assets by reconnecting `ForestPresentationSettings`, prototype broadleaf/elm packs, or distant prototype silhouettes. Report missing assets and restore the approved local baseline instead. The public repository is source-only; a fresh clone lacks the licensed assets.

## Current map generation

One seeded continuous 513 × 513 heightfield is divided into Southwest, Southeast, Northwest and Northeast quadrants. Wizard seed, topography, rivers and streams affect geography; temperate moisture and vegetation choices affect the continuous ecology field. Medium creates four 1,024 × 1,024 m districts. See `Documentation/03_Technical/Region_Generation.md` for algorithm, persistence and scope.

Checks cover option influence, four shared district edges, determinism, recipe save/load, approved overview materials/seasonal trees, transitions and camera state. River visibility in generated regions still needs visual acceptance; an active water component is not sufficient evidence.

## Preserve completed work

- Lakes/ponds, rivers, grass hills, textured mountains, larger districts and seasonal trees are approved foundations.
- `I` toggles isometric / lower camera views.
- Grass/quilt experiments were rolled back. Do not reapply experimental shader patches from old outputs.
- Oceans are complete for now: preserve the Coastline Test footprint (~70% land / 30% ocean), brighter deep blue in isometric view, beachward waves and advancing/spreading/receding/fading shoreline foam. Keep ocean clock speed 0.45 and surf cycle about 17 seconds.
- Work directly in the existing project; preserve unrelated work. Do not publish third-party assets or Crest source. Do not push without user authorization.

## Verification workflow

In the Unity editor, write `integration` to `Library/RegionBiomes.request` to run `RegionBiomeChecks`. Inspect the rendered overview and a district as well as the report. Check Summer/Fall/Winter and return-to-overview transitions. Legacy tree regressions must assert no `RandomTrees`, no attached distant-tree renderer, and no retired broadleaf Resources prefabs.

## Saved region deletion

The main Open Region list has a Delete action on each row. First click shows the named-region confirmation; Cancel retains the save, Confirm removes it and refreshes the list. `SavedRegionStore.Delete` moves the selected `.cfregion` and its `.bak` into a unique folder under `Regions/Deleted` inside `Application.persistentDataPath`. These files remain locally recoverable; they are not listed as active saves. A removal marker prevents the bundled meadow test region from reinstalling itself after deletion. Other regions are untouched. Deletion rejects paths outside the active Regions directory.

`Library/RegionDelete.request` runs the editor regression check using only a disposable generated save; it checks confirmation/cancel, removal, archive readability, backup preservation, list refresh and path rejection.

## Evergreen mix in Hills and Mountain Test

The user authorized an experimental evergreen mix for the **Hills and Mountain Test**. `MountainEvergreenMix` appends four stable slots after the 15 approved seasonal prototypes: index 15 Evergreen Pilot, 16 Balsam fir, 17 Fraser fir, and 18 Cilician fir. Runtime prefabs are under `Trees/MountainEvergreen/`. The pilot’s species is unspecified; the three named firs are distinct 3D sources. The seasonal poplar/understory library and shared meadow baseline asset remain unchanged.

Only this named test region uses the mix. It deterministically substitutes a small, clustered share of mature poplars, preserving total placement count, positions, rotations, terrain, road and ground texture cache. Existing evergreen indices 15–18 are retained unchanged, making the substitution idempotent. The original pilot selection remains stable; the three new firs use a separate selection band among remaining mature poplars. The evergreen stays green in Summer/Fall/Winter and remains visible with small plants disabled. Save validation permits the fir slots 15–18 only for this test region; the later broadleaf slots 19–26 are valid across native regions. Existing on-disk player saves are not rewritten automatically; normal Save Region remains manual.

The preview showed sparse foliage; the review material now uses a lower alpha cutoff (0.15) to retain more needles. Treat this as an experimental visual addition for the user's testing, not final foliage approval. Do not replace the approved biome based on this trial. `Library/MountainEvergreen.request` runs persistence/season/cache/placement checks and opens the saved test district. See `Documentation/Migration/V3Evergreens.md` for the V3 source inventory. V3 originals remain read-only and imported art stays out of the public repository.

Evergreen color revision (2026-10-17): needle materials now use a deeper green multiplier `(0.38, 0.56, 0.44)` and smoothness `0.04`, with specular/environment highlights disabled. This addresses the pale yellow-green source needles without changing meadow/poplar materials, bark, lighting, alpha coverage or placement. `V3EvergreenImport.NeedlePalette` preserves the settings on reimport. User visual acceptance is still pending.

Three-fir addition: see `Documentation/Migration/MountainFirSpecies.md` for source lineage, three mesh detail levels, scale, material and save contracts. All three additions use genuine 3D source meshes and keep green needles through the seasonal controls. The existing meadow/poplar library remains the biome foundation; no retired prototype broadleaf trees are reintroduced. Fir appearance remains subject to user review.

## Open Region loading progress

Main-menu Open actions now show an independent `RegionLoadingOverlay` with a named region, stage text, activity dots and a progress bar. `SavedRegionStore.Load` runs on a background Task; scene construction stays on Unity's main thread and yields through `PrototypeRegionView.OpenStages` / `MeadowDistrictLandscape.BuildStages`. Terrain height sampling and painting yield in row batches. HUD setup must not call the synchronous landscape builder during initial loading: defer it until the saved district/overview is selected. This removes a redundant full-region build before opening a saved district.

Existing synchronous editor/test entry points drain the same staged implementation; old asset-study tools use `OpenSavedRegionImmediatelyForTools`. Preserve these compatibility paths. Duplicate Open is blocked; failure cleans up the partial region and returns to the menu; loading never writes the player's save. Stage weights are estimates, not an ETA. Some native asset/GPU operations can still pause a frame. See `Documentation/03_Technical/Region_Loading.md`; verify with `Library/RegionLoading.request`.

Loading verification after the redundant-overview fix: cached / rebuilt Hills and Mountain loads observed maximum editor-update gaps of 531 / 640 ms (previously roughly 4.5 seconds). Missing-file recovery, duplicate clicks, progress completion, four-district biome integration and all mountain fir persistence/season checks passed. These are editor observations; cold native/GPU asset operations may still pause.

## Integrated maple and oak forest mix (2026-10-17)

The user authorized the optimized trees in game. `ForestTreeMix` now substitutes 24% of eligible remaining mature poplar placements in **all native meadow region/district forests**, including generated overviews and existing test maps. The approved meadow/poplar terrain and understory remain the biome foundation. This does not reintroduce retired prototype trees. New resource prefabs are `Trees/ForestBroadleaf/<species>/<season>`.

Keep append-only slots: 0–14 approved seasonal library, 15–18 the four existing mountain firs (placement still Hills and Mountain Test only), 19–26 Silver Maple A/B, Canyon Live Oak A/B, Southern Live Oak, Angel Oak, OakPilot A/B. All native regions reserve 15–18 so broadleaf IDs are the same everywhere. Do not reorder or resize the original `SeasonalTreeLibrary`. The fir mix runs first, broadleaf second; both retain existing species slots, total placements, positions and rotations. Save validation accepts broadleaf slots in ordinary regions and keeps fir slots restricted to the mountain test. No automatic player-save rewrite occurs.

Seasons: maples use derived green Summer, source golden/orange Fall, and bare Winter geometry; the source's GreenLeaf albedos needed a green conversion. OakPilots use separate genuine seasonal FBX exports. Live oaks stay evergreen, including Angel Oak moss. All have three 3D mesh LODs. Matte URP cutout/normal materials and `ForestTreePalette` affect only these new trees; the terrain, poplars, fir palette and ocean settings remain intact. Nominal species heights are 13–18m. See `Documentation/Migration/ForestBroadleafIntegration.md` for exact placement, season, source, material and rebuild contracts, and `BroadleafOptimization.md` for the original geometry study. The earlier staged-only status is superseded by this integration.

Unity checks passed for all eight species (1,081 new broadleaf placements / 15,243 total in Hills and Mountain Test), seasons, three textured mesh LODs, mature-tree visibility, deterministic repeat, temporary save/load and terrain-cache restore. The original player save stayed untouched. Four-district generation integration passed. Visual acceptance of individual new trees remains with the user. Use `Library/ForestTrees.request` to rerun the forest checks, `Library/MountainEvergreen.request` for the fir regression, and `Library/BroadleafImport.request` to rebuild imported prefabs while stopped. Source-only Python intake tools are in `Tools/Trees/BroadleafIntegration/`; all licensed/generated art stays local and ignored.

## Forest performance isolation (2026-10-17; current behavior)

The user approved the new trees' appearance, then reported a performance regression. Keep their geometry, materials and seasonal palettes intact. The eight latest maple/oak additions now default OFF via `ForestTreeMix.EnabledSpeciesMask` (PlayerPrefs `CityForge.Forest.SpeciesMask`, default 0). In a district, **NEW TREES: OFF** opens eight independent switches plus NONE/ALL. Selection persists globally and can be cumulative. Native poplars and the existing mountain firs stay enabled.

Canonical generation still assigns append-only species IDs 19–26. Disabled additions display native seasonal poplars at the same positions instead; canonical IDs, sizes and rotations remain in memory and saves, allowing later re-enabling without regenerating or rewriting player saves. Fallback poplar sizes use the native baseline; this is not an exact reconstruction of each original poplar subtype. Only enabled broadleaf prefabs load through the normal runtime palette. Never remove the new assets or change the approved meadow ground to address this regression.

Fixed-camera editor comparison at 3434×1728: wide native poplar/fir presentation 16.11 ms/frame versus all eight additions 30.91 ms; draw calls 14,928 versus 34,855. Individual species were 16.50–18.83 ms. Close comparison was 17.44 versus 33.56 ms. Combined rendering cost is the finding; this does not establish a specific CPU/GPU bottleneck. Many oaks use 12–13 materials. Start reintroduction with Maple A/B or Angel Oak, then measure cumulative combinations. See `Documentation/03_Technical/Forest_Performance.md`. `Library/ForestPerformance.request`, `Library/ForestSwitches.request`, and `Library/ForestTrees.request` run the comparative, UI/persistence, and seasonal/asset checks respectively. The latter temporarily enables all species and restores the previous preference afterward.

### Current forest trial: Silver Maple A only

The user authorized reintroducing **Silver Maple A** first. The local persisted species mask is now `1`; the other seven additions stay off. The default for a fresh preference remains off. Poplars/firs and approved materials are unchanged. Use NEW TREES to adjust the experiment; do not automatically enable the other species.

### October 19: Maple isolation and close panning

The user noticed panning “shaking” after enabling Silver Maple A. The local persisted species preference is back to mask 0 for comparison; this does not prove that model caused the shaking. Keep all eight optional additions off pending comparison. Native poplars/firs remain. Region camera keyboard and middle-mouse pan speeds are now exactly double their prior values at ZoomLevel 1, 2 and 3. Levels 0, 4 and 5 and non-region cameras retain prior speeds; Shift still applies its existing multiplier. Unity compilation passed. No terrain/art or saved maps were changed.

### October 19: navigation quality trial

The user clarified the issue as uneven camera motion, not visual shaking, and authorized reducing detail while moving. Terrain SetHeights/SetAlphamaps occur during build/cache restore, not panning. `CameraMotionQuality`, attached by the camera controller, detects translation, rotation and zoom after camera updates. During motion it temporarily uses 75% of the previous URP render scale (about 56% of pixel count) and 50% of the previous LOD bias. After 0.2 seconds without movement, or on disable/destroy, it restores the exact captured settings. Screen-space UI stays native resolution. It does not alter terrain data, trees, materials, ocean clock, surf settings or saved maps. Silver Maple remains off. This is a trial requiring user smoothness/visual review; no measured frame-time improvement is claimed yet. Render-target changes and LOD transitions may themselves need profiling if the trial does not help.

Unity compilation and `Library/CameraMotionQuality.request` lifecycle checks passed for translation/zoom activation and exact idle/disable restoration. The check is not a performance benchmark. Source: `Assets/CityForge/Scripts/Camera/CameraMotionQuality.cs`. The policy applies to cameras with `CityForgeCameraController`; it assumes the game's single active navigation camera because URP render scale and LOD bias are shared quality settings.

### October 19: panning frame pacing investigation

Scripted pan comparison found uneven intervals under the software 60 FPS cap even when average was ~60 FPS. Native streaming suspension had little effect in this run; motion quality reduced load but did not remove cadence variation. Unity documentation warns desktop targetFrameRate timing can microstutter. Normal pacing now uses vSyncCount=1 and targetFrameRate=-1; HUD says FRAME PACE: VSYNC. It follows monitor refresh. Existing FramePacing60 preference/method names remain compatibility aliases; do not revert synchronized mode to a software cap. Uncapped still available. No change to ocean/terrain/species appearance.

See Documentation/03_Technical/Camera_Panning.md and CameraPanChecks. Editor timing cannot prove display smoothness or identify every GPU/CPU stall; user comparison and standalone capture remain needed if symptoms persist. Optional maples/oaks remain off.

### Current trial: both Silver Maples with VSync (October 19)

Following the user's report that VSync improved panning, both Silver Maple A and B are enabled in the local persisted mask `3`. All six oak additions remain off. Native poplars/firs and seasonal palettes remain unchanged. Close panning at ZoomLevel 1–3 is now another 25% faster: the existing 2x factor becomes 2.5x, including keyboard and middle-mouse dragging. Levels 0, 4, 5 retain prior speeds. VSync and temporary movement-quality mode remain enabled. User will evaluate this two-maple combination; no performance acceptance is assumed.

### District preloader restored — October 19

Gameplay map clicks and district buttons now call `RequestDistrict`, which immediately shows the existing `RegionLoadingOverlay` with the region/district name. It presents two frames before advancing `EnterDistrictStages` and the nested `UpdateLandscapeModeStages`/native biome builder. Camera and region input are held during loading; duplicate requests are ignored. Progress reports named stages and completes before restoring input. Failure cleans partial detailed terrain and returns to the relief map. Native asset/GPU calls can still pause a frame; progress weights are estimates.

The synchronous `EnterDistrict` wrapper remains for editor/asset checks and drains the same staged routine. Do not wire gameplay clicks back to that immediate wrapper. `RegionLoadingOverlay` shares its nested-iterator/error runner between menu region loads and district loads. `Library/DistrictLoading.request` passed: immediate overlay/input lock, duplicate rejection, 504 rendered frames through 14 stages in the observed run, monotonic progress, successful district load, input restoration, overlay removal, return to relief and disposable failure cleanup. Player saves untouched.

### Regional framing and district entry zoom — October 19

Regional FrameRegion now fits the actual relief renderer bounds around their center, projecting all eight bounding corners into the isometric camera with a 25% margin for HUD clearance. Initial staged region loading refits after the relief mesh exists. This avoids relying on the old prototype framing assumption for the map's lower point. Detailed district entry now finishes at zoom level 5 (including the pre-existing coastline path); closer zoom speeds and approved meadow/ocean settings are unchanged. DistrictLoadingChecks verifies every relief vertex lies inside viewport and near/far clip planes, plus zoom5 after staged district loading.

### Reference-driven ridge/valley revision — verified trial (October 19)

The user supplied a shaded relief reference with mountain ranges, a broad winding river and tributaries. New recipes now default to version5, preserving version1–4 saved geography. RegionalLandformNode supplies seeded range/valley constraints inside the MapMagic graph before erosion. RegionalRiverNetwork creates one trunk and the requested connected tributaries, with shared water-distance/height carving. This is constrained procedural routing, not a physical drainage simulation.

A major scale issue was found: the old prototype ratio reduced an 8km map's vertical range to ~86m. Version5 stores a physical terrainHeightRange=1200 and sea level12; older saved dimensions remain unchanged. The overview now uses a 257×257 mesh and a 513-square terrain-derived shaded atlas, olive cover patches, warm stone, shallow/deep water and shore colors. Atlas creation yields in row batches. The current intended display exaggeration is 1.25x; district geometry uses physical saved heights. Both seasonal maples, approved native meadow materials and all ocean assets/clock stay unchanged.

The first version5 MapMagic run passed determinism, seed variation, erosion, stored height round trip, connected tributary and worker-creation checks. Its inspected render overemphasized tall wall-like ranges. The latest code lowers/broadens ranges and varies crest heights; it also fixes bank distance sampling over neighboring curve segments and moves water/biome composition to the worker. The Mac was subsequently unlocked. The final revision compiled and its screenshot was inspected: ridges are lower/broader, foothills greener, river banks smoother. The output is still visibly procedural, especially river profiles, and is a trial awaiting user visual acceptance. Do not claim an exact match to the reference. MapMagic generation checks, expanded-grid/season/save checks, and district preloader/framing/zoom5 checks passed. Player saves were untouched. The updated preview is Regional-Ridge-Valley-Preview.png in this chat outputs.

### Regional palette feedback — October 19

The user approved the ridge/valley direction and requested deeper, richer greens and removal of the light blue river border. RegionReliefMap now uses a uniform deep blue for overview water, with no cyan shallow-water rim; land cover blends richer grass green into deep woodland green. Terrain geometry, river routing, warm mountain stone, district biome materials and approved oceans are unchanged. These are overview palette changes only.

### Second relief reference refinement — October 19

The user supplied a second reference for finer ridges/gullies and natural river/land transitions. Current trial uses a 513-square overview mesh, 1025-square atlas, continuous green cover, restrained curvature shading, small ridged range modulation before erosion and broader valley transitions. River distance checks all curve segments; version5 cartographic water follows channel distance rather than painting the entire low valley blue. Keep deeper greens and no cyan rim. Existing stored heights are preserved. See Regional_Ridge_Valley_Trial.md; visual fidelity remains a trial, not an exact illustration match.

Final second-reference preview inspected; RegionalReliefChecks passed with the channel mask. Fine drainage networks from the illustration are not yet implemented, and some confluences remain geometric. The current preview is open as an unsaved test region.

### Regional palette correction — October 19

The user requested removal of all river borders, including the tan shoreline band, and muted army/olive greens rather than blue-green woodland. RegionReliefMap now has no painted bank edging and uses olive grass (0.43,0.48,0.25) through woodland (0.25,0.33,0.14). Keep this overview palette separate from approved district biome and ocean materials.

### Regional green saturation follow-up — October 19

The user preferred the border-free olive map but found greens still too vibrant. Overview grass/woodland endpoints are now (0.43,0.46,0.32) and (0.28,0.32,0.22), reducing saturation while retaining similar brightness. Preserve border-free rivers and the approved district/ocean appearance.

### Regional palette midpoint — October 19

After rebuilding, the user found the latest desaturation too muted; earlier feedback had used an older render. Current overview palette is the midpoint between the two latest olive revisions: grass (0.43,0.47,0.285), woodland (0.265,0.325,0.18). Keep rivers border-free. Review after recompilation and reopening the same region; atlas colors are built on load. District biome and ocean appearance are unchanged.

### Ocean beach and regional slab — October 19

The latest midpoint greens were accepted. The user requested a thin tan ocean beach and visible slab thickness. RegionReliefMap now paints a narrow 0.0025-normalized tan band only beside semantic Ocean outline segments, excluding map-boundary segments. Rivers remain unbordered. Regional slab sides follow the entire surface perimeter down to a flat base 2.5% of map width below the lowest displayed surface; brown vertex colors darken toward the bottom. Slab sides have no picking collider and hide with the overview. Regional framing includes side bounds. These changes affect overview presentation only; district heights, native biome, coastal surf resources and 0.45 clock remain intact.

Slab/beach verification: Unity compiled both shaders; RegionalReliefChecks passed slab bounds, all64 picks, detailed shared edge/seasons/cache and temporary save/load. Coastline Test overview screenshot inspected; dark slab sides fit in frame. No player saves written.


### Controlled river projection study — October 22

User reports subtle shimmer at35 degrees, plastic highlights by37 and a jump39–40. RiverProjectionChecks now polls Library/RiverProjection.request in a live native Crest district. It freezes river advection, fixes focus/yaw/distance/framing and captures35/37/39/40 perspective, then40 orthographic at identical eye pose and clip planes. The pair is repeated with planar reflections disabled. All temporary material/camera/flow state is restored in finally; no player saves are written.

Observed: broad highlights develop by40 even in perspective; orthographic redistributes highlights across the river at the same eye pose. Disabling planar reflections does not remove either effect, so do not blame the local orthographic planar-reflection workaround without further evidence. The exact shader cause of the remaining repeating ripple/plastic appearance is unresolved.

Native URP water currently uses metallic workflow (_WorkflowMode1, no _SPECULAR_SETUP). Crest Fragment.hlsl only writes its _Crest_Specular output under _SPECULAR_SETUP, so earlier specular-strength tuning did not control that output. Material overrides are supported: diagnostic additionally tests specular workflow0/_SPECULAR_SETUP with strengths0.35 and0.10, including the low-angle12 degree view. Strength0.10 reduces broad glare at40 ortho, but repeating ridges remain; this is a diagnostic trial, not an accepted fix. Baseline material/profile and camera endpoints remain unchanged. Do not enable the workflow globally or alter oceans based only on this study.

Study completed and captures inspected. Chat outputs include River-Controlled-Comparison.png and River-Projection-Checks.txt. Player save hashes verified unchanged. Next targeted work: separate the direct-light specular response from normal-map pattern/filtering, keeping projection fixed; preserve approved low-angle appearance and require visual review before changing default water settings.


### River surface isolation — October 22

RiverSurfaceChecks polls Library/RiverSurface.request in a live native river preview. Thirty fixed-pose captures compare baseline, specular workflow with zero specular output, flattened/halved normal strength, smoothness0.90/0.85, normal-texture mip bias+2, and three authored procedural ripple trials. Each gets12 degree perspective (native low-angle clip range),35 perspective and40 orthographic. River advection is frozen; all changed material values/keyword/texture, camera pose and flow state restore in finally. Filtering uses Graphics.CopyTexture into a temporary compressed texture because Instantiate of the non-readable native normal asset logs a Unity error. No imported texture settings are edited.

Observed: zero specular removes most bright plastic-looking ridges while preserving blue/teal water and subtle normal shading; flat normals remove surface detail; half-strength normals largely lose the desired white glints. Smoothness0.85 broadens/increases glare, and mip bias+2 gives little visible improvement. This points strongly to the specular response interacting with normal slopes. Zero specular affects the URP specular response, including direct/environment contributions; it does not isolate sunlight alone. Do not claim a specific direct-light bug yet.

An authored RiverRippleNormals shader bakes periodic height derivatives into temporary in-memory normals. Fine variants replace broad ridges with excessive glittering dots and expose visible spatial bands; rejected and left disabled. The bands may involve Crest cascade-dependent normal scaling, but their cause has not been isolated. Do not promote these procedural normals or half-strength/smoothness variants as fixes. Baseline profile, native ocean and camera endpoints unchanged; player save hashes verified unchanged. No new accepted water appearance.

Chat outputs: River-Surface-Isolation-Comparison.png, River-Surface-Isolation-Checks.txt and full baseline/test images. Next useful isolation is direct sunlight versus other specular contributions, or a deliberate shader specular response with stable fine detail. Avoid more blind normal-pattern substitutions: the finer procedural test was visually worse.


### Direct sunlight isolated; selective-glint shader trial — October22

RiverLightChecks now polls Library/RiverLight.request and writes Library/RiverLightStudy.18 fixed-pose images compare baseline, _Crest_Occlusion0 (suppresses URP indirect surface lighting), sunlight intensity0, both, and sun elevation +/-10 degrees. Native sun is intensity1.25, rotation(45,220,0). Broad bright ridges persist with indirect lighting suppressed and disappear with sun suppressed. Lowering sun elevation to35 changes them substantially; raising to55 largely removes them. All sun/material/camera/flow changes restore. This is strong evidence that direct sunlight drives the broad bright ridges, not proof that Crest's lighting is defective.

Targeted scalar specular trials0.1/0.025/0.01 and normal strength0.06 failed: reducing strength also removes desired sharp glints. Stored in Library/RiverSpecularStudy. Do not keep tuning scalar strength alone.

Authored RiverGlintResponse.hlsl is a nonlinear artistic experiment: evaluate native UniversalFragmentPBR with/without specular, retain the positive specular difference according to a brightness smoothstep, then recombine it with the base. It evaluates lighting twice and has NOT been performance-qualified; thresholds are exposure-dependent. It is not a physically based fix. It does not create painted white marks or change ripple textures.

RiverGlintShaderStudy polls Library/RiverGlintShader.request, generates the installed native Water shadergraph using its installed ShaderGraph importer, substitutes only the URP forward-pass include with the authored wrapper, and imports Assets/CityForge/LocalWaterStudy/RiverGlint.shader. The generated derivative contains licensed-generated source and is explicitly Git-ignored together with its folder meta. Never publish it. No Crest/URP package sources were edited. Builder uses a guarded internal ShaderGraph API specific to this installed version; fail rather than silently patch a different structure. Build report Library/RiverGlintShader.txt.

Material variant issue: constructing Material(template.shader) then CopyPropertiesFromMaterial lost inherited properties; that preview change was reverted. Keep original native material construction in CrestDistrictRiverPreview. Shader swaps in editor studies detach ONLY its disposable clone via material.parent=null, preserving inherited properties. Never detach or mutate the imported asset itself.

RiverSurfaceChecks has evolved from the original surface isolation matrix into12 selective-glint comparisons (Library/RiverSurface.request -> Library/RiverGlintStudy). It restores original shader/material/camera/flow afterward. Soft gate0.05–0.2, medium0.15–0.5, strong0.3–0.9, gain1; all use specular workflow0/_SPECULAR_SETUP and specular0.35 with the original normal map/strength0.08/scale4/smoothness0.97. Medium reduces broad lit ridges while retaining isolated bright white glints; strong becomes sparse. Still images at12/35 perspective and40 orthographic inspected. This remains a candidate, not user-approved; glints can still look detached and clump at zoom2.

CrestRiverStudy now supports glintResponse, glintStart/End/Gain and specularWorkflow in request JSON. Menu CityForge/Water/Preview selective river glints applies medium; requires the local shader already built. Preview bounded Crest river returns to baseline profile/shader. Baseline RiverSunGlintPreview.json remains unchanged. The live unsaved district currently has medium candidate active at native isometric/zoom2; I and the tilt pole remain available. Work request saved at chat work/river-selective-glints-trial.json.

Motion validation:24 frames each at zoom1 and2 with normal current; frame samples inspected. Fixed clip transform/GPU district coverage and shader checks passed for captured zooms. Panning was NOT repeated in this run; generic CrestRiverStudy check text mentions older pan checks, so do not present it as a fresh pan test. Player save hashes unchanged. Ocean, native tree assets, geography, saved camera endpoints and VSync unchanged. No commit/push requested this turn.

Chat outputs: River-Sunlight-Isolation.png, River-Selective-Glint-Comparison.png, River-Selective-Glint-Live.png, River-Selective-Glint-Motion.mp4 (captured timestamp cadence), River-Selective-Glint-Motion-Frames.png and diagnostic reports. Next: user visual assessment through the tilt pole; if promising, refine highlight continuity and optimize the double lighting evaluation before promoting it into gameplay.


### Glint lifetime / evolving lighting normals — October22

User says selective glints look better, but travel unchanged like floating objects; wants them to change as waves evolve. RiverGlintResponse.hlsl now adds three smoothly traveling/interfering world-space slopes to inputData.normalWS before evaluating PBR lighting. Amplitude0.018, speed1; wavelengths~1.7/3.1/4.7m, angular rates3.2/2.3/1.65rad/sec. No random brightness, blink mask or painted marks. This changes the normals used by URP lighting (and therefore reflection alignment); native Crest emission/refraction and water mesh displacement are not changed. It is a visual shading model, not a new fluid simulation.

Builder adds _CityForgeRippleEvolution and _CityForgeRippleEvolutionSpeed properties to the local generated shader; rebuilt successfully. CrestRiverStudy Tuning now exposes rippleEvolution(default0.018), rippleEvolutionSpeed(default1), captureFrames(default24,max120), captureInterval(default0.125). Prior selective trial can be reproduced by explicitly passing rippleEvolution0. Baseline RiverSunGlintPreview.json unchanged; native oceans untouched.

Checked control0 vs trial0.018 with64 frames each at zoom1 and2, fixed camera/same material/colors/current. Each capture set spans~13.4sec because render requests take time; videos use actual PNG timestamps, not requested cadence. Water-only highlight patches retain best-translation correlation~0.85/0.90 in the control versus~0.34/0.39 with evolving slopes over8 frames(~1.7sec). This supports actual shape/lifetime change beyond drift; it is not an aesthetic score, and control/trial have different starting phases. Four-frame samples inspected: clusters change shape and disappear/reappear. Low-angle24-frame capture inspected; bright smooth reflected-water look retained. Clip transform/cascade coverage/shader checks passed at captured zooms; pans not repeated. Player save hashes unchanged.

Live unsaved district left in native isometric/zoom2 with medium selective glints plus evolving slopes active. Work request chat work/river-evolving-glints-trial.json. Chat outputs River-Evolving-Glints.mp4, River-Glints-Drift-Control.mp4, River-Evolving-Glint-Frames.png, River-Evolving-Glint-Checks.txt. Further user visual assessment needed; keep intensity/speed tunable. Existing double-PBR performance caveat remains.

Diagnostic caveat: RiverSurfaceChecks freezes downstream advection, but _Time-driven evolution continues on the custom shader unless _CityForgeRippleEvolution is explicitly set0. Future frozen still comparisons must disable it or freeze the phase. The previous selective-gate still study predates evolution and was truly frozen.


### Faster glint evolution — October22

User approves evolving-glint direction and asks for faster animation. Applied rippleEvolutionSpeed2 (was1) to the live selective preview and chat work/river-evolving-glints-trial.json. RiverSunGlintPreview.json now records rippleEvolutionSpeed2 so the selective-preview menu keeps it; native baseline mode ignores that setting. Current advection speed, normal amplitude0.018, glint thresholds/gain, water colors and ocean remain unchanged. Request used projection-1 and no camera refocus to preserve the current user view.64-frame captures at zoom1/2 completed and frame samples inspected. Output River-Faster-Glints.mp4 uses actual capture timestamps. Live preview remains at the faster setting. No commit/push.


### Approved35.3 degree camera endpoint — October22

User accepted recommendation to cap the view at true isometric35.26439 degrees (UI35.3), with diagonal45 degree heading. CityForgeCameraController.IsometricPitch is now the shared constant for defaults/reset/region and meadow framing. Serialized camera pitch is clamped in ApplyPose and older restored camera states clamp to15..35.26439; mouse orbit also respects that maximum. Low endpoint12 degrees and perspective/orthographic transition remain. Default region yaw45 and native meadow yaw315 are both45+90n diagonal headings; existing chosen quadrants/rotation remain. River profile/editor default and chat work trial headings now45 rather than49, pitch35.26439 rather than40. No saved maps rewritten.

CameraTiltChecks passed61 slider positions, centering/finite matrices, invariant focus/heading/zoom, legacy85 degree restore cap, pole upper35.26439, I endpoints and intermediate camera JSON round trip. Water material values unchanged through sweep. Native river preview passed GPU full-district clip coverage at zoom0/2/5 and100/-80m pans; full district far framing inspected and fits. UI screenshot shows35.3 on the pole. Player save hashes unchanged. Live unsaved river district left at upper isometric/zoom2/yaw45 with selective evolving glints at speed2; water shader/color/current untouched this turn.

Chat outputs Camera-Isometric-35.png, Camera-Isometric-35-Checks.txt and District-Framing-35.png. Earlier35/40 camera studies are historical; controller now caps restores above35.26439, so any future diagnostic needing steeper views must explicitly isolate a diagnostic camera rather than silently use the gameplay rig. No commit/push requested.


### Glint restraint above31 degrees — October22

User prefers glint amount at31 degrees but finds it excessive at higher tilts. RiverGlintResponse now computes camera pitch from abs(UNITY_MATRIX_V[2].y), then smoothsteps highTilt from31 to35.26439. At/below31 the prior response is unchanged. Toward the cap, gate start adds0.20, gate end adds0.35 (reducing coverage), and specular-only gain eases from1 to0.20. First endpoint gain0.55 still looked too strong; tightened to0.20 and re-inspected. This is explicit camera-angle artistic compensation requested by user, not a physical water model. Camera cap35.3, colors, current, evolving-normal amplitude and speed2 unchanged. Projection switch uses the same orientation-derived gain, so no attenuation reset at orthographic endpoint. Only the local generated study shader uses the authored wrapper; ocean/native SDK untouched.

CrestRiverStudy now supports optional viewPitch(default-1) for editor captures; nonnegative12+ maps into the gameplay tilt pole. Captured31 reference and35.26439 restrained variants at zoom1/2,24 frames each. Frame samples inspected: refined top view has reduced glare/clumping and retains moving glints. Clip/shader checks passed at captured zooms; pan sweep not repeated. Player save hashes unchanged. Live unsaved district left at35.3/yaw45/zoom2 with refined restriction and evolving glints speed2. Output River-Glints-Limited-Above31.png. No commit/push.

### Controlled panning comparison saves (2026-10-30)
Four local Open Region entries `pan-test-meadow`, `pan-test-trees-30`, `pan-test-trees-50`, and `pan-test-trees-80` clone the original valley's complete binary terrain/paint/native-tree payload. Each opens northeast district 3 automatically. All are waterless, summer, animals off, and initially small plants off. `PanningTestDistricts` applies deterministic nested presentation subsets to mature native trees and approved authored trees, after full canonical placement is built. Percentages mean population retained, not ground coverage. Original save/checkpoint are unchanged; files stay outside public Git. Use Return to Menu > Open Region to compare at zoom 2 and 3 with the same camera heading; keep seasons/species/frame pacing unchanged between cases. Test fixtures do not establish the cause of the user's stutter; Editor frame timings are not performance evidence.

Coastal panning follow-up: new local save `pan-test-coast-50` / `Pan Test - Coast - Trees 50%` uses the existing approved `CoastlineTestRegion` geography and native coastal Crest setup, with the same 50% deterministic tree presentation mask. Summer, animals and small plants off. Automatically enters northeast district. This coastal fixture uses the established 1,024m coastal district (the dry valley tests were 512m), so it is a practical mixed-load visual test rather than a single-variable water benchmark. Original saves and coastal shader/material settings are unchanged.

Coastal life load comparison (same day): `pan-test-coast-50` now defaults to animals ON in its local save and additional small plants ON via its exact diagnostic ID. Other dry test IDs remain unchanged. Diagnostic native filtering now selects the same 50% mature population first and appends all small plants when enabled; toggling understory no longer reshuffles the mature population. Authored 300-tree subset and coastal water unchanged.
Standalone measured 3434×1818, VSync2 at120Hz, repeated triangular keyboard-speed transform pans at zoom2/3, four cases twice in reversed order, 8 measured seconds after2 warm-up per case/zoom. Baseline >25ms frames0.8%/0.3%; plants2.6%/3.9%; animals1.7%/0.5%; combined15.2%/7.6%. Combined p95~33.3ms vs baseline~17ms. Plants yielded frequent generation0 collections; method-level allocation/CPU/GPU profiling still needed to establish exact cause. Animals283, disabled rendering/animation/movement at zoom3. Native mature3563, additional small plants11913, authored300. Temporary env-gated runtime helper removed; final helper-free build succeeded. Full CSV/raw frames/method report in current chat outputs/coast-load-comparison. Original valley/checkpoint preserved; no commit/push.

### Measured navigation optimization — 2026-10-30
Retained only optional streamed-vegetation suspension. CameraMotionQuality now runs early in LateUpdate (-100), exposing NavigationWorkSuspended during its existing ReducedQuality interval. MeadowRuntimeVegetation returns before tile queue reconciliation, generation, eviction and allocations; streamed MeadowGroundCoverRenderer tiles return before cache/draw submission. Cached tiles and partial RNG/generator survive the pause. Existing 0.2s settle interval, renderScale×.75, lodBias×.5 and incremental2ms streaming resume remain unchanged. Native terrain small plants remain rendered: no SetTreeInstances/rebuild on camera motion. Trees, ocean and animals remain active. Animal animation/movement pausing was measured separately and reverted because no convincing incremental gain was established.
Fresh same-build standalone tests: all coastal life enabled, previous vs vegetation-pause vs experimental vegetation+animal-pause, two reversed-order passes at zoom2/3. Vegetation pause reduced >25ms frames from5.40% to0.31% at zoom2, 1.48% to0% at zoom3; zoom2 p95 from29.20ms to17.27ms. Gen0 collections46/41→0/0; streamed tiles during pans534/566→0/0 across both runs. API direct allocation bytes returned0 even for controls and is treated unavailable. Measurements short and VSync-capped; not universal guarantees. Current chat outputs/navigation-optimization has method, rawframes, per-run summary. Temporary runtime helper removed and final build passed with zeroerrors. Existing user maps/checkpoint unchanged; no commit/push.

### Accepted baseline — 2026-10-30
User approved locking in the integrated district forest/water presentation and measured streamed-vegetation navigation suspension. Preserve this behavior as the baseline: optional streamed ground cover pauses during navigation and resumes after0.2s settle; existing tree LOD/resolution reduction stays; animal and water animation remain active. Further changes should be supported by profiling the original river district. Final standalone build succeeded (zero errors, nine warnings); functional navigation/restore checks and art-checkpoint verification passed. Saved test regions, commercial assets and generated runtime resources remain local outside source control. No merge or publication authorized by this acceptance.

### District HUD cleanup — 2026-10-30
User requested removing temporary menus while retaining seasons. District HUD now keeps Summer/Fall/Winter, save/menu/overview, camera tilt and a compact zoom readout. Removed temporary landscape, frame pacing, species, wildlife/find-animal, tree/small-plant, numbered zoom and relief diagnostic controls from normal HUD construction. Diagnostic engine API remains available for existing editor tooling, with its UI and keyboard shortcuts explicitly disabled in normal play. Vegetation, wildlife, water, navigation optimization, and saved presentation preferences are unchanged. Main-menu region creation/opening controls are unaffected.

### Coastal shoreline trial — 2026-10-30
User authorized deleting temporary test saves and keeping one new coastal test. Five pan-test saves archived via SavedRegionStore.Delete; active saves now original Meadow Test Valley plus new Coastal Shoreline Test (`coastal-shoreline-test`, coastVersion3). Original valley hash remainsfa1c138a899b5149714c477753bf4cb9d7c7b04e2f75047e3a1e2aabed293a8d. Fresh test retains canonical terrain/tree cache locally for faster reopening; metadata creation explicitlynull for legacy2×2 scaling. New ID uses deterministic50% mature/authored density, no animals/optional small plants by default.
`CoastalShorelineSwash` is an opt-in GPU displacement/foam candidate for Coastline&&coastVersion>=3 only. Packet amplitude0.44m,8sec, coastline-varying phase; low-beach clip reserve permits physical wave/terrain intersection. Old continuous shoreline foam disabled only for trial. Native water materials/colors, trees and navigation optimizations preserved. Foam/wet sand require more visual refinement; no wetting material yet, no standalone timing qualification yet. See Coastal_Shoreline_Trial.md. New shaders are original authored source allowed in Resources/CoastalSwash; commercial art/packages remain ignored. Input shader name prefixes must match Crest's validators or the input is disabled and its renderer can become visible. Wrong-category first capture discarded; corrected fresh-input registration/hidden-renderer check passed. No commit/push requested.

Coastal swash visual acceptance: user says the new coast is “way better” and authorizes commit, push and a new PR. Preserve the current first swash candidate, cleaned HUD and measured vegetation navigation pause. PR#4 has already merged on GitHub; publish subsequent environment integration/navigation/coast work on a new branch/PR. Leave the PR unmerged. New coastal swash has successful player compilation and fresh runtime input checks; wet-sand response and standalone swash cost comparison remain future work.

### NPC ambient-life foundation — November 1, 2026

User authorized implementing the NPC plan with supplied animated characters in `/Users/joelinstrum/Downloads/indian stuff`. Initial candidate is ten transient residents, one supplied fringe-dress appearance, idle/walk/wander and paired conversations using fold_arms as a placeholder. All three supplied character archives have real rigs and idle/walk clips; no invented rig animation or imported cultural behavior. Artwork stays private in ignored SourceAssets/NpcStudy and Resources/NpcStudy.

New NpcDefinition/NpcBehaviorProfile assets separate appearance/capabilities and reusable behavior configuration from per-person NpcRecord/NpcBrain state. One NpcPopulation scheduler (30Hz at Zoom0/1, 10Hz at2), bounded 33x33m NpcLocalNavigation grid, personal-space checks, two-party and meeting-spot reservations, shared talk duration, timeout and interruption cleanup. Close visuals use Animator.CullCompletely offscreen. Zoom3–5 hides all roots and pauses transient records; return close resumes. This does not implement abstract far resident simulation, persistent citizens, jobs/economy, schedules, bridges/interiors or district-wide navigation. Dynamic congestion waits and times out; no crowd steering yet.

Use CityForge > NPC > Start study in stream district in Play after opening accepted Stream Rock Study Pass17 northeast district. Exact fixture/district gate; no automatic spawning on normal map loads and no save schema changes. Builder menu prepares local definition/controller/material/prefab and two assignable behavior profile assets; refuses replacing existing prepared definition. Tools/Npc/prepare_npc_study.py copies/validates private source files without overwriting different data. See Documentation/03_Technical/Npc_Ambient_Study.md.

Final compile 0 errors/warnings. NpcStudyChecks normal saved-region menu/district load passed: ten residents,950 open grid cells,10 moved >1m,28.9-degree animated leg rotation,2 completed conversations; no invalid positions or personal-space overlaps, zero ground-height error. Timeout/paired interruption, unreachable path, Zoom0–2 captures, far visual removal/study pause, close resume, full teardown/restart passed.49 existing region files byte-identical, no new saves. All artwork/generated assets ignored; source/documentation/meta/Python only. No standalone population benchmark or full-city scalability claim. Current chat outputs/npc-study has checks, audit and captures. Leaves the candidate running at Zoom0, diagnostics hidden, fish OFF. No commit/push/PR modification/merge requested. Await visual feedback before expanding appearance or activities.

### NPC stationary idle correction — November 1, 2026

User observed NPCs walking in place while stationary. NpcWalkAction now enters Idle, selects Walk only on actual horizontal displacement, selects Idle when blocked/zero-motion, and returns Idle at arrival/exit. Consumes reached waypoints without a stationary walking tick between segments. A blocked NPC retains its route and resumes when clear. Conversation animations unchanged.

Compile zero errors/warnings. Extended NpcStudyChecks passed normal load/population checks and forced blocker regression against actual Animator states: blocked=Idle, clear/moving=Walk, stopped/arrived=Idle after blends. Existing ten-person movement, paired conversations, no invalid positions/overlaps, grounding, interruption, zoom and teardown/restart checks passed.49 existing region files unchanged. Left fresh population running at Zoom0, fish OFF. Current chat outputs/npc-idle-fix holds reports and captures. No commit/push.

### Sharper shadow live preview — November 1, 2026

User requested trying sharper shadows after discussing performance trade-offs. Current live Play session uses a TEMPORARY cloned URP asset named `CityForge sharp shadows LIVE PREVIEW`,4096 atlas,4 cascades with splits(.50,.68,.85), and runtime directional sun Hard. Approved pipeline/quality source assets unchanged; no permanent default change. Sun intensity/strength/bias unchanged (.05 depth/.4 normal bias). Original current baseline was4096/1 cascade/Soft. Camera already fits near/far to terrain; current374m shadow distance reflects300m camera setback, so it was not simply an unfitted global500m range. Merely disabling softness produced a smaller improvement than the four-cascade layout.

Compared1-soft,1-hard,2-hard(split.72),4-hard and4-soft with fixed camera/frozen NPC poses, full renderScale1,24 warmup frames and capture metadata. Four-hard yielded clearest NPC silhouettes. Live cloned preview exercised Zoom0/1/2 and offset camera poses, then restored original Zoom0 pose. NPC movement and animation resumed; navigation quality component restored/active. All49 saved region files byte-identical. No standalone timing benchmark; four cascades add shadow rendering work and are NOT performance-qualified for forests/full cities. Do not claim free improvement. Earlier comparison attempts were superseded by stable full-scale captures. Current chat outputs/sharper-shadows has images, comparison, metadata and Preview-Checks.txt.

Preview only: switching seasons can restore seasonal light softness; stopping Play ends the temporary trial. To restore baseline in this session set QualitySettings.renderPipeline to AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/CityForge/Rendering/CityForge_URP.asset"), return sun.shadows to Soft for current summer, and destroy the old temporary clone after switching away. Await user visual feedback before implementing a permanent policy; preserve forest/water/camera behavior and qualify performance before adopting broadly. No commit/push.

### New-region native settlements — November 1, 2026

User requested Native American camps in about20% of new-region districts,5–9 teepees in a horseshoe near water, supplied central totem and supplied chief/warrior/female NPCs constrained to the camp vicinity. Implemented automatic new-wizard content via independent nativeCampVersion1 (geography recipe remains12; missing/zero legacy field produces no camps). NativeCampPlanner runs in existing worker composition, chooses a seeded20% target of original cells, prefers closest suitable near-water candidates, falls back to dry land if necessary, skips unusable/all-water cells. Stores stable district/center/heading/seed/count/water metadata in SavedRegionInfo.nativeCamps. Save/load/Capture retain it; opening older maps does not backfill camps.

NativeCampDistrict renders records belonging to ActiveDistrictMembers (compatible with combined/L-shaped district boundaries). One camp per original selected cell.5–9 inward-facing teepees over260 degrees,100-degree opening toward water; central5m totem. Ring radius14–20.4m. Uses existing terrain without changing canonical geography. Native tree/streamed understory clearings exclude camp footprints; native tree buffer8m outside clearing. Records are fully within original cell margins. Actual-record Camp buttons on the relief overview open the relevant district; hidden in detailed view/transparent to Combine selection.

Shared NPC foundation now supports camp-specific centers/definitions/seeds. One chief, two warriors and teepee-count female residents (8–12 total).65×65 one-metre navigation grid, conservative tent/totem/tree obstacles, single connected component and circular leash(radius+7m). Existing idle-when-blocked, personal space, paired conversation, timeout, cleanup and close-only Zoom0–2 presentation retained. Individual activities/positions remain transient and restart deterministically on district load; this is ambient settlement content, not economy/combat/diplomacy or permanent citizen persistence.

Private Resources/NativeCamps prepared from teepees-2 and source character archives. Extensionless Downloads/indian stuff/totem is FBX; source1,417,346 vertices preserved, prepared decimation15,002 vertices/~30k triangles (before Unity import splits). Supplied vertex colors are effectively white; baked private texture uses matte wood tint. Totem wrapper MUST preserve original imported270-degreeX rotation on the child; assigning camp yaw directly to the raw single-mesh FBX lays it down. Final wrapper/asset builder corrected and validated after reopen. Four supplied2K teepee colors shared, supplied normal map, shared private resident definition; new chief/warrior Generic controllers use actual idle/walk clips, fold_arms where present and idle fallback for warrior. All art/generated assets remain ignored. Tools/Camps has guarded private input-copy, Blender model reduction/normalization and vertex-color bake scripts; NativeCampAssetBuilder creates Unity assets without replacing a prepared asset set.

Final compile0 errors/warnings. NativeCampChecks passed normal new-region worker generation and normal saved overview/district load; deterministic13/64 camp metadata (all13 near water for seed921117),5–9 horseshoe spacing, legacy missing-field behavior, metadata roundtrip, all-water skip, and bit-identical canonical geography/water with camp flag on/off. Actual overview markers match records. District28 (UI5–4):9 teepees, one upright totem,12 residents, three supplied definitions, native tree clearing;30-second movement safe within25.4m against27.4m leash. All49 pre-existing region files byte-identical. One active unique test save native-camp-study-2026-11-01 / Native Settlements — Trial; intermediate candidates archived recoverably rather than replacing user saves. Current chat outputs/native-camps holds final images, overview, checks, preservation report and implementation notes. No standalone camp performance qualification.

Left saved trial district28 open at Zoom1, camp/water visible, NPCs active, sharper shadows reapplied as the previously accepted TEMPORARY cloned4096/four-cascade/Hard preview. Rendering source assets/quality defaults remain unchanged; permanent shadow policy/performance qualification remains separate. No commit/push/PR change/merge requested. See Documentation/03_Technical/Native_Settlements.md for reproduction and scope.

## Camp courtyard refinement — 2026-11-01

User requested closer teepees and grass mixed with dirt. NativeCampRecord.Radius now75% of original (10.5–15.3m for5–9 teepees); saved centre/heading/count untouched. NativeCampGround adds owned runtime matte dirt layer, noise-broken courtyard coverage and fading entrance paths, capped within ring. Original grass retained; terrain heights/geography unchanged. CaptureSnapshot prefers canonical lake map, otherwise pre-camp map, excluding derived wear from cache; painting guarded once. No added source art. NativeCampRefinementChecks.Unit plus saved-trial normal overview/district load passed: nine tents15.3m ring,12 residents safely moving30sec within22.3m leash, normalized terrain weights, localized wear, canonical cache exclusion. Compile zero errors/warnings; all52 existing files under Regions byte-identical. Outputs/native-camp-refinement has Tighter-Camp.png and checks. Left Play mode at trial district28 Zoom1/base32 with accepted temporary cloned4-cascade Hard shadows. No commit/push.

## Campfire — 2026-11-01

User authorized campfire and reuse/resizing existing stones. Added NativeCampfire to every rendered camp before residents/nav initialize. Reuses first3 non-moss RichRockAssets meshes and shared materials,12 individual stones0.43–0.56m wide, terrain-seated; originals unchanged. Hearth sits4.3m toward opening from centre/totem; five procedural charred logs, low-budget flame/ember/light-smoke systems, source Resources/Campfire/Flame.shader and existing precipitation smoke shader. Owned runtime materials cleaned on unload. Point light range5/no shadows, mild flicker; effects/light paused/cleared Zoom3+, resume0–2. Static hearth stays visible. Explicit3.5m-wide obstacle before navigation prevents crossing/spawning in fire. No audio, sitting clips or new gathering behavior yet. Normal saved trial load/refinement checks PASS:12 residents safely move30sec outside hearth;12stones and all3 emissions; far stop/close resume; existing tighter ring/ground/cache checks. No runtime console errors; all52 existing saved region files byte-identical. Left Play at camp district28 Zoom1 with temporary sharp shadows. Current outputs/campfire has Campfire.png, Campfire-Close.png and checks. No commit/push.

### Visible camp smoke refinement

User requested smoke because initial wisps were unreadable. NativeCampfire smoke now7/sec,7sec lifetime,1.05m base widening3.2x, warm-grey alpha.42, rise.85m/s, gentle side drift and low-cost noise.96 cap and Zoom3+ shutdown retained. Normal saved trial reopened; refinement checks PASS including emission/zoom lifecycle/resident avoidance. Visually confirmed clear small plume at Zoom1. Compile passed;52 existing save files unchanged. Left trial open at Zoom1; outputs/campfire-smoke/Smoke.png. No commit/push.

### Balanced camp population

User accepted1 chief/5 warriors/6 female residents for the nine-teepee camp. InitializeCamp keeps teepees+3 total, reserves one chief, assigns floor((total-1)/2) warriors and remaining female residents; smaller camps scale proportionally. Chief behavior remains unchanged (request accepted mix only). Updated creation/refinement checks; normal saved trial reload PASS and direct runtime count Chief=1 Warriors=5 Female=6,30-second safe movement and hearth avoidance passed. Compile passed;52 existing save files unchanged. Left camp open in Play with smoke/sharp-shadow preview. Outputs/camp-population checks/image. No commit/push.

### Warrior appearance audit

User thought many chiefs/no warriors and supplied bare-chested red-handprint warrior screenshot. Verified native warrior ZIP exists; runtime Warrior/Model.fbx SHA256 matches archive a2f77451f5f2ec78074f373a43c26f0416943124a0bb43509327236f055757c1. Diffuse atlas matches reference. Rendered actual three gameplay prefabs side by side (screen LEFT female, CENTER warrior, RIGHT feather-headdress chief; camera reverses worldX). Current scene has only one NpcPopulation and meshes verified directly:1Chief/5Warrior/6Resident, no accidental prefab reuse. No code/art changes required. Temporary audit objects/camera/RT cleaned up. outputs/warrior-audit/Female-Warrior-Chief.png shows identities clearly.

### Three closer zoom steps

User requested closest three zoom levels50% closer. SetZoomLevel now multiplies visible span by.5 for levels0–2, retaining earlier1.25 boost for0–1;3–5 stay unchanged. This interprets closer as half visible width/height (2x apparent scale). Actual live six-level check on study farthest512: old12.8/25.6/64/128/256/512 ->6.4/12.8/32/128/256/512. Compile and normal saved-trial refinement checks passed;52 saved files unchanged. Explicit stored camera restore retains saved size until next zoom action. Left camp at new Zoom1 with sharp preview and smoke. outputs/closer-zoom holds Zoom-Checks and visual. No commit/push.

### Supplied courtyard dirt texture

User supplied Downloads/terrain/dirt-tile.png and accepted trying it. Copied unmodified into ignored private Resources/NativeCamps/Courtyard-Dirt.png. NativeCampGround optionally loads this with2.5m tile, neutral remap and smoothness0; retains procedural fallback when absent. Import sRGB/Repeat/Trilinear/aniso8/mips/max2048/NPOT None/CompressedHQ/alpha None. Original procedural tint would darken photographic texture so bypass it for supplied texture. Grass masks, canonical saves and terrain geometry unchanged. Compile and saved-trial checks PASS; direct live layer verified supplied path/tile2.5/smoothness0. Visually checked normal and closest views: fine grit/pebbles visible, no obvious hard seams in inspected courtyard.52 saved files unchanged; private PNG verified ignored. outputs/courtyard-dirt/Dirt-Close.png and Tighter-Camp.png. Left Play at new closer Zoom1 camp, sharp shadows/smoke. No commit/push.

### Shared dirt, bare courtyard and meadow tufts

User requested supplied dirt throughout applicable terrain (e.g. riverbanks), less green ground in camp center, actual meadow grass. Added SharedDirtSurface.Apply on runtime copies of base soil0, dedicated river/stream soil and wet lake soil; one optional private albedo2.5m, matte, neutral remap; damp variants65% tint. No edits to canonical masks/heights/water or private source TerrainLayers. Careful: rich river soil replacement happens in initial RiverBedVersion block before mountain/coast layer overrides, avoiding replacing a cliff layer by index. CampGroundWear inner zone94–99.5% dirt with irregular fade; stronger entrance paths. NativeCampGrass uses existing baseline plants0/1, seeded up to64 short edge tufts per camp, avoids expanded structure/fire obstacles and bare core. Shared mesh/material instancing, no grass shadows/colliders, Zoom0–2 only. Existing streamed camp exclusion retained so grass is deliberately sparse. Compile and normal saved-trial refinement checks PASS: core coverage,64tufts, shared bank/camp diffuse, normalized weights/cache exclusion,30sec safe NPC movement, fire zoom lifecycle. No runtime errors;52 existing files byte-identical. outputs/shared-dirt has full camp, Dirt-Close.png and Riverbank.png/checks. Left camp open Zoom1/sharp preview. No commit/push.

### NPC movement/idle sliding fix

User reported translating actors still in idle. Reproduced34 non-walk Animator states among770 actual moving samples. NpcBrain now derives IsMoving from actual horizontal displacement, resolves final animation after action changes, and population resolves again after meeting callbacks. Walk is immediate/evaluated before visual translation (no idle-to-walk blend lag); stationary Idle/Talk resolved explicitly. Animate checks real current/next state rather than only cached request. Animator.keepAnimatorStateOnDisable true and CullUpdateTransforms preserve state across hiding/offscreen. NpcLocomotionChecks observes actual root displacement and real Walk state plus actual walk clip, all3 definitions at Zoom0/1/2, far hide/show and forced Animator idle reset. PASS1299 moving/2805 stopped samples,0 mismatches,3 moving definitions. Normal camp movement/hearth/ground checks also PASS; compile passed;52 existing saved files unchanged. Left Play camp Zoom1; outputs/npc-locomotion contains before/after evidence. No commit/push.

## Growable Lot Layout modal — 2026-11-20

Redesigned the lot editor growable layout modal to group Building Type, Building Footprint, Orientation, Paths & Access, and Optional Offsets. Added gold selected states, numeric step controls, road-edge dropdown, and viewport fitting. Existing apply/validation, eligible-building selection, entry-marker picking and selected-building footprint conversion remain intact. UI now lives in LotGrowableLayoutModal.cs (LotEditorPanel partial); LotEditorIcon adds shop/industry/row/chevron glyphs. No growth schema changes. Tested in isolated CityForgeQA: Apply/Close, validation, spinners, row/single enablement, selected-building conversion and dropdown; captured 1920x1080 and1366x768. GROWABLE_MODAL_PASS. Synced the four tested source/meta files to main; player saves untouched.

## District painting, paths, zoning and sky — 2026-11-20

Texturize retains input ownership across presentation refreshes, refuses lot selection at both picker and selection entry points, paints empty lot ground and other district terrain while excluding physical rotated building footprints. Added Boundary / Free spray modes using the existing feather, brush, erase and undo controls; null GroundPaintData polygon explicitly means unbounded spray, retaining per-pixel containment. Palette starts at top.

Parks → Paths adds shared lot path materials/widths, curved angles, freehand PointWire open/closed routes and four presets. New additive SavedRegionInfo.parkPaths array captures/restores/validates district paths; old saves default empty. Paths clip/fade beneath street sidewalks, avoid buildings/water and participate in area demolition. District park paths and authored lot pedestrian paths register as preferred walking surfaces; vehicles unchanged. See Documentation/03_Technical/District_Park_Paths.md.

C2 medium-wealth automatic block streets default to cobblestone. Rectangular stamp parcel sizes now persist relative to frontage; RestoreTownZones repairs complete anchored parcels from actual cell extents, recovering older rotated stamps. Undeveloped zoning may be restamped; intersecting old reservations are replaced without removing built lots. Placement uses the same curved paving-corner tolerance as growth; sidewalks are not obstacles. Commercial growth enforces wealth too. Growth failure/status messages now appear outside the zone-drag mode.

Day horizon lightened toward sky blue; directional orange/red sunset peaks at19:00, fading toward stars/night. Player stream tiny stones (<0.4m) and fine bank-stone meshes replaced with existing photographic compacted-gravel-pebble bed texture at2.5m repeat. Medium/big rocks, scanned patches, native bank plants and water shader retained; generated natural streams unchanged. Fixed200m fixture:801,714 ->409,752 near dressing triangles (-48.9%),952 ->163 individual stone instances. This measures dressing geometry, not district FPS or total frame triangles.

Isolated QA passed: actual saved C2 4x4/8x4 row candidates/materialization; all cardinal rotations, parcel restore, full frontage/physical fit against curved included streets, restamping undeveloped zoning; spray masking and lot input exclusion; path creation, preferred walking registration, metadata plus binary region roundtrip; UI and day/19:00 shader renders. Compared production stream captures before/after. No player save writes. Synced21 files from QA; main was at menu outside Play. Artifacts: current chat outputs/District-Refinements.


## Wealth grass, fox wildlife and replacement oak — 2026-11-21

Untextured civic/ploppable lots now fall back to the same era/wealth grass libraries used by homes. Explicit ground textures and Disabled automatic ground remain authoritative. Confirmed the saved high-wealth New England church selects high-wealth-texture; no lot JSON or player saves rewritten.

Private ImportedFox uses the supplied rig/walk with a stationary Idle and Moving-controlled Walk, shared-rig near/far skins, approximately0.48m height. Registered fox in TemperateWildlife and preserved it when rebuilding wildlife; automatically attempts at least two foxes in suitable woodland using existing habitat/water/road/trunk checks. Animal picker reads the library directly. Seeded disposable-map test spawned9 foxes, exercised24 simulated seconds of safe roaming, and verified distant animation suspension.

Private Lots/Trees/NMAOak004 replaces oak-pilot-a/b aliases, including seasonal lot entries and optional native forest species slots6/7. Approved default mixed forests remain unchanged; old live-oak slots2–5 remain disabled. Prepared original canopy clusters as alpha cards with simplified branches and preserved exposed trunk:98,171 /34,680 /10,296 triangles. Summer/fall/winter variants; native LOD selects the reduced tier for district zoom3–5. Actual district zoom2→3→2 binding passed. Original multi-million-triangle model stays outside Assets. Licensed art and generated catalog remain ignored/private. FoxOakPreparation editor menu regenerates materials/prefabs/catalog from prepared private meshes. Main compilation passed and catalog explicitly refreshed after imports. Current chat outputs/Fox-and-Oak contains previews and verification reports. No commit/push.


## Pedestrian modal, placement decision, audio and sky — 2026-11-21

In-game Parks→Paths uses the supplied pedestrian concept: private town banner; serif text, dark/gold sections and selectable surface thumbnails/search; widths1/2/3/6/9/12m, feathering, facing, shapes, corner style and sink0.5ft/1ft. Shared PedestrianPathModal/Options also edits selected Lot Editor paths. Six private PathBorders textures support Curb/Brick/Fieldstone/Timber/Limestone/Earth and Both/Left/Right lining. Enum values append, with Both=0 default; only sunk paths display borders. Left/right follow drawing direction. Existing LotPathBlend virtual depth is shared rather than altering authoritative terrain. Road materials reset custom border uniforms to preserve old road curbs. Existing domain creation/edit undo, selection, road clipping, routing and additive JSON/binary metadata remain authoritative. Lot Editor closes the modal after Apply/Undo and handles Ctrl/Cmd Z outside search fields.

Plop terrain toolbar removed. A valid new land-lot click freezes a cloned pending placement and opens Follow ground slope / Level lot / Cancel before any structure/vegetation clearing or construction. The mouse/Tab placement handlers pause while pending; confirmation revalidates the same position then commits the chosen clone. Existing lots and source lot files remain untouched until a real confirmed placement. Existing moves retain their prior mode; waterfront lots keep dedicated waterfront behavior.

Detailed audio now plays at L0/full and L1/half; POV full. Nature/night beds remain district-only all-zoom exceptions, also halved at L1. Added night-time-summer MP3 bed for summer/fall, crossfading18:30–21 and05–07, winter excluded; supplied commericial-louder MP3 replaces commercial-area under its original GUID/path. Thirteen pooled voices preserve the eight-actor budget. All voices loop.

Sky yellow/gold onset changed to soft orange; ordered red/purple remains. Stars reach22% at19:00 and full at21:00. Clouds drift using unscaled float time and retain warm color/visibility through sunset instead of being overwritten by the dusk gradient.

Isolated actual-Candlewick copy passed frozen pending position, cancel with no construction, both terrain choices, path create/edit, binary save/load, appearance/create undo, Lot Editor apply/close/undo, looping voice budget, actual L0/L1 gain, summer night handoff and winter exclusion. UI renders checked1920×1080 and1366×768; shader renders verify all six textures, physical left/right placement, orange dusk/early stars and cloud drift. Licensed art/audio stay ignored/private. Main sync performed after capturing live Guilford overview into ignored Library/PathExperience-BeforeReload.cfregion; normal RegionLoadingOverlay stalled after the reload, so synchronous RegionBuildSteps editor recovery reopened the checkpoint. Confirmed region ID/name/district/camera, lots, roads, paths, towns and forest edits match the live pre-reload checkpoint; only43 new borderSides=0 serialization defaults differ. Restored Play Mode and left Unity paused. All88 recorded normal player region/lot files unchanged. Player region/lot saves were not written. No commit/push. Current chat outputs/Pedestrian-Paths-and-Sunset contains previews and reports.


## Parks concept, ambient zoom levels and strict zoning feedback — 2026-11-21

Parks overview now follows the supplied Ornate Parks UI Overlook concept: private banner/tree-card artwork, Agriculture/Parks/Paths/Texture tabs, planting/scatter/removal actions and saved park/agriculture lot access. Existing tree picker/brushes, pedestrian-path modal, texture paint and built-in corn/wheat gardens remain the consumers. Responsive fit verified1920×1080 and1366×768. Private UI/OrnateParksConcept image remains ignored.

Day/night nature beds now use full volume at L0/L1/L2, half at L3/L4 and zero at L5; riding full. Regional overview remains silent; actor/commercial/stream/children policies unchanged. Actual day/night AudioSource gain checked at every level in a disposable copy of current Candlewick.

Latest saved screenshot parcel is unanchored R2 $$,4×4 cells, world bounds(-132,568)→(-116,584), district35. Physical lot/pavement checks pass. No matching R2 $$ models/layouts exist. User explicitly chose strict R2; no lower-density/wealth fallback allowed. District growth previously retried cells already occupied by developed lots, reporting unrelated overlaps. OccupiedGrowthCells now matches overlay occupancy and skips those cells before candidate generation. Fully developed zoning emits no false status or duplicate lots. Local parcel and full district now report missing R2 content. Final compact toast: “R2 $$: buildings missing.” Layout-only gaps also get a specific short message. Empty cells remain available; no collision rules relaxed or player data cleared.

Inspected am_058_030.rar via verified nested OBJ extraction:887,371 vertices,328,274 polygons,639,104 triangles. User cap20,000: no import/replacement or reduction; existing oak assets unchanged.

UI/actions, physical parcel clearance, strict content message, occupied-cell skip, full district scan and actual day/night L0–L5 volume checks passed isolated QA. Final message width fits the existing toast. Main sync outside Play; no player saves written, no commit/push. Current chat outputs/Parks-and-Zoning contains interface previews, diagnosis and count reports.

## Plane tree pack and rejected tree retirement — 2026-11-21

User approved reducing supplied Downloads/Tree Pack models below20,000 triangles. Six private plane/platanus variants registered as plane-pack-01 through06, labels Plane Tree01–06, available through the existing Parks tree picker and Lot Editor. Prepared metre-scale/recentered meshes, saved green materials with authored bark/alpha foliage, fall material and leafless winter prefabs. Near counts10,543 /10,351 /10,534 /12,008 /14,399 /18,550; distant7,611 /7,419 /7,460 /7,699 /11,468 /15,233. Source47k–84k models remain outside Assets. Branch reduction preserves source foliage for1–5; variant6 foliage also reduced to meet the cap. PlaneTreePreparation regenerates seasonal private prefabs/materials/catalog from prepared private meshes. All art and generated catalog remain ignored.

User rejected oaks and gold tree: oak-pilot-a/b plus golden Purchased Maple1 (silver-maple) are retired centrally. Catalog regenerators no longer offer them; existing saved-lot references keep their IDs but render no model. Optional native forest slots0/6/7 disabled, indices reserved; only retained green MapleB remains available in species experiments. Approved chestnut/locust/larch mixed forest and native baseline presentation preserved. No player region/lot data rewritten.

Isolated Unity asset QA passed all six <20k counts, texture bindings, grounding/scale, all-season registration, winter leaf removal and district zoom2→3→2 transitions. Near and distant Unity renders inspected. Main sync outside Play Mode, guarded source baseline comparisons. Current chat outputs/Tree-Pack contains six-variant contact sheet, per-tier previews, counts and checks. No commit/push.

## Townhouse density correction — 2026-11-21

User confirmed Founders/Colonial townhouses belong to R2, reserving R3 for later large apartments. Reclassified all16 v3-building-r3-mid-wealth-* models from Residential3 to Residential2, keeping Medium wealth, Founders era, art, geometry, materials and legacy IDs. Catalog source helper renamed R2Townhouse; existing catalog update path persists changed zone. Legacy R3 strings remain only stable IDs/private art paths, not density tags.

Also reclassified the three saved, restricted townhouse row layouts to R2 so growth can use the models: R2 Mid Wealth Townhomes2 (57773ef...), R2 Mid Wealth Row Houses (656a573...), R2 Half Townhouse Lot (a996d4...). Only template zone/name changed; original files backed up in current chat work/townhouse-r2/before. Existing placed district records and region saves not rewritten; existing R3-painted cells are not automatically rezoned. Other R2 low-wealth content retained. No lower-density fallback added. Original church-side4×4 parcel still requires a matching-size authored R2$$ layout; reclassification alone does not resize existing row layouts.

Isolated Unity QA passed all16 density/wealth/era classifications; all three templates select candidates and materialize fitted rows, and the same models are excluded from R3 growth. Main sync guarded against source/template changes, outside Play. Current chat outputs/Townhouse-R2 contains checks. No commit/push.


## Tree inventory and test-phase preference — 2026-11-21

User explicitly says existing lots/regions/districts are disposable during this test phase; prioritize clean improvements and do not treat preserving test data as a blocker, while keeping the game working. No deletion requested in the inventory turn; await specific keep/remove selections.

Current chat outputs/Tree-Inventory contains105 prepared tree/version entries,210 Unity side/elevated screenshots,14 labeled contact sheets, searchable Tree-review.html with browser-local Keep/Remove/Undecided choices and text export, CSV and authoritative inventory.json mapping T01–T105 to asset IDs/source paths. Counts include active planting assets, approved canopy8, poplars8, native understory, older prepared assets, retired imports and tropical palms. Seasonal variations not separately counted; identical near mesh/material wrappers consolidated as aliases. Some original/high-detail and reduced versions remain separate entries. Four registered London Plane variants A–D exist (T09–T12), correcting earlier incomplete count of two.

Review favorites: T01–T08 canopy (five chestnuts, two larches, locust); T09–T12 London Planes; T13–T20 Meadow Poplars. T30–T35 new plane pack. T94–T97 retired purchased/NMA assets; older oak sources also appear among prepared legacy entries. Never infer deletion solely from gallery choices until user supplies their selection. Keep review IDs stable by using existing inventory.json, not regenerating sorting after removals.

All captures rendered in isolated QA with consistent brighter neutral lighting; individual images frame each tree separately (height labels show actual metres). Registered current trees go through LotRuntimeLoader for placement scale/material tint; retired/legacy captured directly for comparison. Counts show authored near/far geometry rather than measured performance. Search, overhead swap, decision persistence/filtering and105-row export tested in browser;210 nonblank captures checked and all14 sheets visually reviewed. No gameplay source/art or player saves changed; main Unity session left alone. QA scripts remain isolated.

## Exported tree selections applied — 2026-11-21

User exported Downloads/CityForge-tree-choices.txt and instructed keeping every entry not marked Remove. Validated all105 rows against the stable inventory:56 Remove,29 Keep,20 Undecided; all49 non-Remove entries retained. Applied explicit user selection supersedes older approved forest species membership. Export snapshot and exact retained/removed CSV lists are in current chat outputs/Tree-Selection.

Central retirement policy excludes23 catalogue IDs including both oak aliases, both purchased maples, all six plane-pack trees, Beech2, Birch1–3, Fir1, both Junipers, Pine1–5 and Sycamore. These do not appear in planting/editor menus or render from existing lot references. Unmarked/Undecided entries remain available. Older selected source versions are no longer used by native presentation; source art/shared dependencies remain private for the kept reduced variants and authoring tools rather than breaking references through physical deletion.

Mixed woodland now picks only Chestnut01/02/04/05 plus LarchTall/Medium. Chestnut03 and Locust slots get zero weights and kept-prefab aliases; new and previously planted removed variants resolve to kept trees. Planting menu is Mixed/Chestnut/Larch; chestnut selection excludes03. Retained proportions normalized from previous weights. All optional broadleaf experiments retired; empty species-controls panel hidden. Native load/presentation filters juvenile poplars0–2, maple bushes/willows11–14 and removed fir pilot15. Mature poplars remain. Native mountain Fraser/Cilician use the kept reduced versions; undecided original Balsam stays. Seasonal prototype slots remain valid. Grass, reeds, fallen leaves and non-inventoried ground-cover plants unchanged.

Isolated asset QA checks all catalogue choices, removed lot visuals, retained references,100k mixed picks, prototype filtering and mountain bindings. Actual Candlewick district copy verifies six-model canopy, no removed canonical understory, valid summer/fall/winter/summer tree references and zoom2/3/5/2. Main sync outside Play;13 guarded source/doc/private-library files; catalogue refreshed after reload. No normal player region saves written, no commit/push. Technical docs and AGENTS updated to honor explicit review. No billboard/tilt changes implemented in this task.

## Spray feedback, exclusive painting and zone-safe park trees — 2026-11-21

Shared TexturePaintTool now draws its exact brush radius through a non-interactive HUD-space BrushFeedback graphic: fixed-width gold circle with dark outer contrast plus spray-bottle/droplet cursor. This replaces the easily lost thin world-space brush ring; boundary guides retain existing PointWire behavior. Shows over paintable ground, hides over UI/outside/focus loss, restores original OS cursor on exit. Shared in district and Lot Editor painting. Parks tree spraying also uses the same feedback.

Active painting blocks district selection and toolbar/mode switching; Lot Editor selectables stay disabled until paint finishes. Paint owns left drags even when the Hand tool is selected. Close/Escape release-frame guards prevent Escape leaking into other mode/deselection handlers in the same frame. Paint/erase, texture controls, stroke undo, boundary/free-spray behavior retained.

Parks Add Trees caches naturally <=20m models and prefers these when the trunk position is on any painted zone or inside a growable lot. Explicit taller choices/fallbacks capped from actual visible mesh bounds to20m. Same cap reapplies in PresentPlacedTree across seasons and reloads. No duplicate mesh/material assets or save fields. Existing tree placement scale/tint preserved; TreePlacementHeight measures upright prefab-world bounds (root rotations matter) and matches existing fit/scaling. Water/road/spacing/population rules unchanged.

QA: cached heights match actual retained tree instances; mixed pool <=20m; intentionally oversized retained model capped in summer/fall/winter; district toolbar/selection ownership, feedback raycast exclusion and close/release; Lot Editor controls, Hand-drag priority, cursor circle/spray visual and restoration. Current chat outputs/Spray-and-Zone-Trees contains final Lot Editor preview and verification reports. Initial world-preview capture was outside camera framing and excluded from deliverables; no billboard or camera-tilt feature added.

Main sync12 guarded source/meta files. Unity was live in Candlewick district35; captured current region to ignored Library/SprayZoneTrees/BeforeReload.cfregion, returned to menu for reload. Unity left Play during compilation/reload; restarted Play and synchronously reopened region+district, then restored exact camera and clock (EnterDistrict resets hour to noon unless explicitly restored). Confirmed all persistent metadata and224 actor/tree placements match checkpoint; only unused empty streetscape wrapper IDs and timestamp differ. Main left in original running Play state. No normal region/lot saves written, no commit/push.

## Rounded-corner R2 growth clearance — 2026-11-21

Reproduced latest screenshot in live Guilford region 794e5f433a03419f8b1278dc6f64d726, new district27 (not old Candlewick35). All four unbuilt R2 $$ 8x3 row-end parcels have valid zoning and saved templates; rounded carriageway clips their empty rectangular lot corners while all generated townhouse footprints remain clear. Anchors (858,803),(883,803),(858,827),(883,827). No layout/wealth fallback or C2 size changes.

Growth now permits bounded empty ground corner clipping for ground-following, unraised growable lots only. Corner cut limited to one4m grid cell and quarter of short side; convex road intersection still rejects pavement through the body. Level foundations and elevated lots retain full rectangular clearance. Required primary/side frontage uses the same bounded rule. Materialized building footprints receive a separate strict pavement intersection check before growth commit. Sidewalk policy unchanged. Four guarded production files synced: DistrictRoadBuildingClearance.cs, DistrictLots.cs, TownResidentialFrontage.cs, TownLotGrowth.cs.

Isolated QA loaded actual live snapshot and current saved layout/building metadata: growth scheduler populated exactly all four previously blocked parcels, 29→33 lots, construction completed. Negative controls reject road through lot interior/buildings, raised lots and level foundations. Final render inspected with clear carriageways; current chat outputs/R2-Rounded-Corners contains Four-parcels-grown.png and Verification.txt. Scratch/baselines/sync manifest in work/r2-growth. No normal player saves or templates rewritten.

Main source compiled successfully. Checkpointed original running overview in ignored Library/R2RoundedCorners/BeforeReload.cfregion before exiting Play and syncing; restarted Play, synchronously restored overview, camera and exact clock. Before/after metadata, lots, zones and actors match apart from timestamp/unused empty streetscape wrapper IDs. Original paused flag restored. Newly fixed parcels will grow when district27 is opened. No commit/push.

## Pre-built zoning cuts to lowest ground — 2026-11-21

User now requests newly added pre-built zones flatten down to their lowest footprint point, superseding slope-following default for NEW growable/pre-built zoning lots. Each new grown lot (all zone/wealth layouts) records cutToLowest and cutGroundHeight, sampled across its rotated footprint at2m intervals plus enclosed native heightmap vertices. Direct saved zoning-lot placement uses the same policy and skips the civic terrain-choice modal. Other ploppable lots retain the modal. Old placed lots keep their previous behavior; no retroactive flattening or template rewrites.

DistrictLotGrading applies sparse reversible cuts to the runtime district TerrainData, including collider, with one sample transition band for a flat lot edge. Canonical regional geography is untouched. Saved world grade replays after reopen/season rebuild. Original changed samples are retained sparsely and restored before re-presenting remaining lots, so demolition/moving does not accumulate terrain scars; moving a graded zoning lot resamples its new site. NaturalLotHeight reads the original samples while LotHeight exposes registered flat platforms. Actual buildings/props, grass/ground paint and paths all share the saved plane, preserving authored elevation offsets. Retaining walls support cut faces with fieldstone, no extra earth cap draw for cut lots. Carriageway samples protected, building/road collision checks retained. Cached weather snow refreshes to edited terrain. Existing dry-land and12m grade guards retained.

QA in isolated project: synthetic sloping terrain becomes uniformly low, road samples unchanged, source height lookup retained, restoring cuts reproduces original heights exactly. Actual four R2 $$8x3 parcels in district27 grow and complete; measured interior terrain range0.0000m in each. Rebuild and binary save/load preserve all four saved levels; other29 original lots remain unflagged. Road/building negative controls pass. Final screenshot visually inspected, reports in current chat outputs/Zone-Grading. Main synced five guarded source files plus new component/meta while observed outside Play at menu. No normal user saves, art, commits or pushes.
Main reload verified: isCompiling=false, scriptCompilationFailed=false, grading component and saved flag present. Left outside Play as observed before synchronization.

## Shared block planes, preview cost and population grace — 2026-11-21

User requested extending pre-built zoning grading through surrounding streets and one full extra tile, reducing calculations while hovering, and citizens only10 real seconds after buildings finish. New automatic cardinal/45° block commits persist ZoneGroundPlane records (center,size,rotation,height,district) in SavedRegionInfo; capture/open/write/read validation included. Footprint includes block plus road center offset, actual planned street half-width,2m sidewalk allowance and extra4m tile. One lowest grade across the expanded block; overlapping/adjoining drops inherit the existing plane height, including new growable lots. Runtime terrain planes replay before road building; road meshes, sidewalks, paint and park paths rebuild once at commit. Shared height lookup projects roads and future lots to the plane. Outer12m smooth transition beyond the flat extra tile removes abrupt road steps; actual flat block area remains constant. Existing per-lot records remain compatible. Graded site slope validation uses current ground rather than the buried original hillside. Canonical regional geography remains unchanged.

Preview: automatic block heavy validation/road mesh creation limited8Hz while moving, no longer invalidated by camera navigation; clicks/rotation force immediate fresh validation. Individual lot preview work limited12Hz, press/release/rotation bypass throttle, and level outline uses cheap center sample rather than resampling whole footprint. Actual grading/minimum runs on commit, never hover. DistrictLotGrading caches terrain/spacing/origin and batches minimum height reads, avoiding repeated per-vertex native calls. No measured FPS claim.

Population: growth and diagonal construction mark lots held before presentation; ambient citizens/vehicles and authored lot characters remain absent. Construction finally releases a10-second unscaled-time grace period; expiry wakes characters and queues ambient route/population updates. Direct newly plopped lots also wait10 seconds. Existing unrelated traffic is retained. Runtime cooldown is transient (construction itself remains transient on reload as before).

Isolated actual district27 QA passed four R2 $$8x3 growth/construction, no associated citizens during construction or grace, eligibility after10 seconds, flat terrain/road/extra tile, future-lot inheritance, adjoining identical plane, and binary save/load of both shared planes and per-lot levels. Original pavement/building intrusion negatives and sparse terrain restore tests passed. Inspected final rendered road transition after correcting initially abrupt join. Current chat work/zone-extensions includes baseline, scripts, logs and guarded manifest; outputs/Zone-Extensions includes verification and final render. Ten guarded main source files synced while main confirmed outside Play at menu; normal player saves/art untouched, no commits/pushes.
Final main assembly verified after reload: compiling=false, scriptCompilationFailed=false, six-parameter Cut including blended-height callback loaded; left outside Play at original menu state. Final sync hashes and whitespace checks pass.

## Flat lot retaining-wall correction — 2026-11-21

User screenshot showed tall sloping fieldstone faces surrounding already flattened row-house lots. Root cause: PresentDistrictLots passed NaturalLotHeight (intentionally preserved original terrain) to DistrictLotFoundation.Build even for cutToLowest lots. Changed only DistrictLots.cs: cut lots now pass CurrentLotTerrainHeight, sampling the actual edited display terrain; ordinary uncut raised platforms still use original natural ground. Buildings, paths and grass already used the saved plane; the unwanted geometry was the retaining-wall builder recreating the buried hillside. No new sampling on preview, save fields, material/art changes or canonical-terrain changes.

Isolated QA uses shared-plane town fixture at12.5m over the original sloped district27: four new R2 $$8x3 parcels build, all measured ground ranges0.0000m; no retaining vertices rise into buried original terrain, and rebuilding stays flat. Existing growth, road/body collision, population grace, adjoining-plane and binary-save checks pass. A first assertion applied to independently cut lots was too broad: those may legitimately retain against protected, ungraded roads at their corners; revised fixture matches the user's fully shared-plane block. Final render inspected. Work/flat-build contains baseline/test/log/sync hash; outputs/Flat-Build contains final render and verification. One guarded source file synced; main confirmed outside Play at menu and compiled cleanly with CurrentLotTerrainHeight present. No normal user saves, art or commits/pushes.
White-bank question: reproduced a similar white highlight on the steep edge of the shared-plane fixture. Terrain-only capture retains it; disabling direct lights and reflections removes the white glints. This supports terrain lighting/glare, rather than rock/snow painted by grading (grading only changes heights). Diagnostic images Bank-terrain-only.png and Bank-without-direct-light.png in outputs/Flat-Build. No terrain shader/material change made. The user's exact unsaved screenshot view was unavailable because main was already back at menu. Final lighting QA also passed all build/grade/population/save checks.

## Leveler tool and frozen traffic — 2026-11-21

User steered the active frozen-traffic investigation to adding a Leveler beside Detonate while retaining/improving automatic zoning leveling. Added DistrictLeveler.cs with a Level button at x1268, toolbar width1336. Default8x8 tiles (32m), held Shift16x16 (64m); rounded rectangle with4m corner radius, brown tile selection and gold outline, red invalid state. Click commits once; Escape exits; UI/focus/loading guards and release-frame gating; owns world input while armed. Preview recalculates at most8Hz on moved cells/size changes; full minimum/water/terrain work only at click. A small original Resources/Region/LevelerPreview.shader renders selection consistently across uneven ground; exact .gitignore exceptions include only this source shader/meta. No new art.

Commit samples CURRENT terrain, includes enclosed native vertices, saves lowest height with a20cm sea-level floor. Complete contained lots adopt the new saved height; partial lots and waterfront lots rejected to avoid distorted half-lot foundations. Existing road/sidewalk/paint/path/prop heights rebuild using the plane. Native/placed vegetation in the rounded footprint clears, as with zoning; authored lot landscaping and other props remain. Leveler flag + cornerRadius extend existing ZoneGroundPlane metadata, defaulting to old rectangular behavior. IsForestRemoved replays leveler masks on caches/fresh generation, preserving later explicit forest additions. Newest planes take precedence; subsequent block drops prefer newest adjoining plane. Automatic block and standalone new-zone minimums also use20cm floor. No normal user save is written automatically; Save persists normally.

Frozen traffic reproduced from the user's new saved region11a282687cc14771b97743526e2ca3a8, district11, copied read-only from main after user exited Play. Grade was12.012m vs sea12m: generic10cm sea-datum heuristic rejected valid dry graded streets on route revalidation. LotFreightJobs now uses actual WildlifeWaterAccess.Dry on graded planes while retaining original ungraded datum margin, actual water footprints, grade-step, tree/building/fence checks. Walkers separately had movement gated by a rendered Animator state; culled animators could fail that gate forever. Initial actors now request a visual wake, state transitions evaluate once with AlwaysAnimate then restore culling, and locomotion no longer waits on animation evaluation. Ten-second post-construction grace preserved; parked profiles remain parked.

QA: copied actual town showed zero moving walkers before fix; vehicles stopped after lot/road rebuild. After fix44 walkers/11 moving vehicles advanced in original fixture; final leveler fixture measured43/11 both before and after grading/rebuild (start/end displacement measure can miss a short out-and-back trip). Actual water and building intrusion negatives remain blocked. Leveler8/16 geometry, rounded exclusions, brown previews, toolbar adjacency, no hover mutation, actual flat terrain, forest mask, save/load and cancellation passed. Real existing lowland complete lots/street raised from~1cm to20cm; movements still pass. Initial lowland test center clipped a rounded corner/partial lot and correctly rejected; fixture moved to fully contain lots. Inspected final UI captures of both sizes in current chat outputs/Leveler; work/leveler and work/traffic-motion contain baselines, actual snapshot, reports and logs. Latest adjoining-plane preference was reviewed as a one-line selection change after scene QA; final main assembly compilation verified.

Main confirmed outside Play at menu before synchronization; six guarded existing source files plus new tool/shader/meta and narrow .gitignore exceptions synced. Main compile succeeds, CommitLeveler and shader found, shader reports no errors. No private artwork, normal region/lot saves, commits or pushes. Main left outside Play as found.

## Leveler45-degree Tab rotation — 2026-11-21

Tab now advances leveler selection by45° (eight states, wraps360→0), forcing immediate preview/validity refresh without waiting for hover throttle. Shift retains angle when switching8x8/16x16; a newly armed session starts0°. Grid vertices and rounded outline now use ZoneGroundPlane.World, matching existing rotated minimum sampling, grade/forest containment and persisted plane rotation. Updated hints. Rotated lot-overlap checks use the oriented footprint rather than rejecting everything inside its larger axis-aligned bounding box.

Single guarded DistrictLeveler.cs change staged/tested in QA. Eight-turn wrap, both sizes at each angle, grid/outline containment and first-vertex transform, actual45° flat terrain, binary save/load angle,20cm lowland floor, water/building guards, tool cleanup and traffic after grading/rebuild pass. Final45° UI render inspected in current chat outputs/Leveler-Rotation; logs/baseline in work/leveler-rotation. Main confirmed outside Play at menu before sync. No normal player saves, art, commits or pushes.

## Leveler click latency removed — 2026-11-21

User reported multi-second wait cursor on leveling. Profiled six identical8x8/16x16 operations against latest saved district11 snapshot with10 prior planes and one lot/no roads: synchronous handler198–362ms, median264.1ms. Dominant191–318ms stage was RebuildDistrictLots restoring every previous terrain cut and replaying all planes; the handler also rebuilt all roads/paint/paths/actors and invalidated all streamed ground cover. These allocations/recreations could add deferred rendering/GC cost beyond measured handler time.

Manual Leveler now appends/applies only the new plane and updates affected terrain-conforming meshes/transforms in place. DistrictLevelerUpdates snapshots old heights before mutation, scopes work to stamp plus existing12m transition, and preserves scene object/material/texture/route/animation identities. Imported model meshes are never deformed. Selected lot models move by grade delta; procedural surfaces and source/clipped path meshes reproject; residential ground batches track per-lot vertex ranges and keep their CPU backing lists current for future additions. Roads, sidewalks, district paint/park paths/zoning meshes retain their existing assets. Existing actors/props get height changes without respawn. Only affected lot foundations refresh, with platform entries replaced per lot. Existing lot SiteHeight callbacks now read current saved cut height dynamically. Metadata/save behavior unchanged.

Terrain grading skips height synchronization for unchanged samples; exact repeat stamps do not append duplicate planes. No full terrain restore/replay on manual clicks. Ground cover invalidates only intersecting tiles, with existing bounded streaming; native forest clearing avoids allocating a full kept-array when nothing is removed. Leveler validation uses one actual water-footprint check per sampled ground point instead of nine redundant5cm footprint samples; walking/vehicle clearance guards remain unchanged. Snow keeps cached vertices/normals and its mesh/material, updating only the affected heightfield rectangle rather than reconstructing the district snow slab.

Measured same six operations17.5–36.4ms, median29.19ms (~9.05× faster); populated-town stamp24.97ms, snow stamp24.98ms. These are isolated Editor synchronous click timings, not a whole-game FPS claim. Tests preserve all lot roots, road mesh IDs and actor IDs with no LotPresentationBuilds increment; selected buildings and batched grass adopt new grade; collider updates immediately. Snow mesh ID retained. Rotation/both sizes,20cm floor, water/building negatives, forest masks, save/load, cleanup and continued traffic pass. Final render inspected. Current chat outputs/Leveler-Performance includes Benchmark.txt, verification and captures; work/leveler-performance includes latest user's read-only snapshot, baseline/per-stage timings, guarded files, optimized runs and validation.

Nine guarded source files plus DistrictLevelerUpdates.cs/meta synced while main confirmed outside Play at menu. No private art, normal player region/lot saves, commits or pushes. No new modal or deferred fake-completion flow; terrain operation remains synchronous and bounded.
Final main reload verified: isCompiling=false, scriptCompilationFailed=false, optimized CaptureLevelerUpdates present. All synced hashes match QA source; whitespace checks pass. Main left outside Play as found.


## Leveler drag and natural banks — 2026-11-21

New manual edits have deterministic world-anchored rough outer feathering: variable outward fringe and bank reach inside the existing12m update margin. The selected core stays perfectly flat/buildable. Persisted roughEdges defaults false for older planes, preserving their prior banks. Automatic zone grading retains its existing behavior.

Hold left mouse and drag to level continuously at the first successful stamp height. Rounded swept brush geometry fills every intervening position; fast pointer motion catches up in bounded32m segments, at most one terrain update per frame. Stationary holding does not reapply. Straight same-direction segments merge metadata after bounded physical application, avoiding hundreds of records for straight strokes. sweep persists through existing region metadata. Shift/Tab reset the stroke anchor while retaining its height; Escape, invalid ground, UI or focus loss cancel pending work. Partial lots remain protected unless already at the exact stroke height. Dry-ground/waterfront/district guards and20cm sea floor retained.

Isolated town QA passed all prior leveler/traffic/collider/object-identity/snow/save checks, plus eight-angle swept geometry, continuous48m drag with no gaps, locked height, stationary deduplication, compact metadata, persisted sweep/roughness and spatial bank variation. Populated stamp25.97ms, snow25.95ms, final drag segment36.80ms synchronous Editor measurements. No whole-game FPS claim. Existing roots/road meshes/actors preserved. Preview render inspected. Current chat outputs/Leveler-Drag/Verification.txt and work/leveler-drag contain evidence/baselines/logs/sync manifest.

Two guarded existing source files plus DistrictLevelerStroke.cs/meta synced while main confirmed outside Play. No normal player saves, private art, commits or pushes. Main kept outside Play as found.
Final main reload verified: compiling=false, scriptCompilationFailed=false, ProcessLevelerStroke present. All synced hashes match tested source; scoped whitespace checks pass.


## Gentler manual leveler hillsides — 2026-11-21

User requested hill edges instead of steep cliffs. Rough manual leveler planes now feather through a32m outer margin (roughly28–32m total transition, previously7–12m). Reduced high-frequency height perturbation from.16 to.025; retained world-anchored irregular banks and perfectly flat core. Automatic/nonrough planes retain12m. GroundPlaneBlend centralizes per-plane margin for height lookup, native terrain cut/replay and bounded object/snow updates, so the wider hillside registers everywhere. No additional hover work or per-frame district rebuilds. Existing rough manual edits adopt broader banks when replayed; saved schema unchanged.

Isolated town QA passed drag/height/geometry/save/collider/object identity/snow/water/building/traffic checks. Inspected final render shows broader smooth bank. Populated stamp46.84ms, snow46.63ms, final drag segment75.29ms synchronous Editor measurements; larger affected area costs more than former narrow banks (prior26/37ms), no whole-game FPS claim. Two guarded source files synced while main confirmed outside Play. No normal saves, art, commits or pushes. Evidence in current chat outputs/Leveler-Hills and work/leveler-hills.
Main reload verified compiling=false, scriptCompilationFailed=false, manual GroundPlaneBlend=32m. Main left outside Play as found; scoped whitespace checks pass.


## Destructive leveler and accumulated-edit performance — 2026-11-21

User explicitly superseded preservation: flattening clears what it touches, including floating trees, with no lot/road/water/slope preservation validation. Manual tool keeps its flat core, rough32m hillside bank, locked stroke height, rotation/size and20cm sea-level floor. LevelerSiteProblem no longer runs placement checks; native minimum uses one batched read, without per2m water/height checks. CaptureLevelerUpdates is no longer called by manual leveling.

DistrictLevelerDestruction deletes intersecting lots and whole touched road/path records rather than splitting/repositioning them. Deletes corresponding roots, batched ground triangles, platform/building/eagle/tree caches, placed actors/props, affected traffic, zoning and paint. Removes forest additions and native/mixed trees throughout the full core plus32m bank. IsForestRemoved now includes banks for old rough manual planes too, fixing saved floating trees on replay. Outside historical edits and the automatic zone-leveling workflow remain functional. Only touched road/path/paint/population systems refresh once at stroke finish/cancel; no full district lot rebuild or terrain restore on a stamp. Whole road records are intentionally removed when touched, consistent with destructive behavior.

Actual latest saved region11a282687cc14771b97743526e2ca3a8 is now district20, with129 planes. Reproduced20 large16x16 edits at410.89–1092.52ms, median682.11ms. Initial removal of preservation plus bounded historical filtering still took140–175ms; profiling showed terrain141–168ms vs trees~2ms and destruction~0.02ms. Replaced per-vertex scans of edit history with sparse native-sample accumulation: newest flat core wins, otherwise nearest-bank distance/height persists. One new plane evaluates once per affected sample, independent of history length. Batched current heights and original source samples supply bank heights without per-vertex native GetHeight calls. Cache clears with terrain Restore and replays deterministically from existing metadata; no new save schema. Cached target height repairs intervening non-brush cuts when a brush revisits them. Cached plane AABBs and quick containment rejection also reduce forest/height-query work.

Final same20 operations23.59–30.40ms, median25.12ms (~27.1x median improvement), synchronous isolated Editor timings, not FPS. QA passes full-bank native tree removal and persistent mask, flat ground, collider, native bank/height lookup agreement, intervening-cut cache correction, partially touched town lots/roads deleted outright, and binary save/load with deleted content and planes. Main player save only copied read-only for QA; test deletions do not mutate it. Harness initially attempted opening a second region before destroying the first view, and used field reflection for the internal PlacedLots property; both harness errors corrected before final successful run. Work/leveler-destructive contains baselines, profiling, logs and guarded sync manifest. Outputs/Leveler-Destructive includes benchmark and verification.

Five guarded existing source files plus DistrictLevelerDestruction.cs/meta synced while main confirmed outside Play. No private art, normal player saves, commits or pushes.
Final main reload verified: isCompiling=false, scriptCompilationFailed=false, DestroyLevelerContents present. All synced hashes match tested source and scoped whitespace checks pass. Main left outside Play as found.


## Harbor founding in combined capital districts — 2026-11-21

User reported Harbor House placement trouble after switching coast to bay; quoted warning about staying inside the capital. Latest read-only saved Burlington regionf94af9faa5744b4886092c4456f0af9d has Clayburn capital in original cell27 and a combined district27+28, both Deep Harbor. FounderPlacement.UpdateLotPointer still built the founder validation Rect from the capital's one original1024m cell, despite InCapitalDistrict correctly accepting the combined anchor. This rejected otherwise valid bay sites in28 before waterfront validation. Water levels were not the cause of that warning.

Founder placement now passes ActiveDistrictBounds; normal LotSiteProblem/WaterfrontSiteProblem retain sampled InActiveDistrict union checks, dry/wet/grade, overlap and road checks. All placements/moves record the actual original cell containing the snapped center, including founders inside combinations, instead of stamping every founder with the old capital cell ID. Town/capital identity remains unchanged. No water-level, shoreline, material or template changes.

Isolated QA loaded user's exact saved region and6x6 Harbor Council House template with2 waterfront rows. Found valid bay site(152,-1000) in28; old original27 Rect reproduced Stay inside the capital’s district, combined bounds passed existing full waterfront checks at unchanged sea12m. Actually committed Harbor House, founding completed, rendered building and visiting ship, saved/loaded actual original cell28, and rejected unselected29. Render visually inspected; test view includes district slab edge but Harbor House visibly placed at shore. Main player save untouched. Evidence in current chat outputs/Harbor-Placement and work/harbor-placement. Single guarded FounderPlacement.cs synced while main observed outside Play at menu. No private art, normal saves, commits or pushes.
Final main reload verified: compiling=false, scriptCompilationFailed=false; guarded hash matches tested source and scoped whitespace check passes. Main left outside Play as found.


## Nature terraforming and editable ocean shorelines — 2026-11-22

Nature → Terrain now opens a themed Terraforming modal with working Raise, Lower, Level and Smooth operations, brush size and elevation/strength controls, plus links to existing Trees/Animals tools. Level modes: Lowest Point, Brush Average, Clicked Height, Sea Level; a signed offset allows modest cuts below sea. Sculpt Terrain hides the modal and arms the existing rounded rectangular leveler. Default32m brush, Shift doubles size, Tab rotates45°, click/drag, Esc or another Nature tool cancels immediately. It works before town founding and blocks world selection while armed. Nature temporarily enables/restores the district terrain collider. City leveling retains its existing lowest-point/20cm-above-sea behavior and permissions.

Raise/Lower set each drag to its starting brush average ± elevation step. Level holds its starting target. Smooth blends toward the local brush average with adjustable fractional strength and a fresh local target per processed segment. All modes reuse bounded swept edits, soft rough32m banks and destructive content clearing. Native animals and generated stream stones/shrubs clear too, across all three stream LOD tiers and patch obstacle seats; old harbor shore stones hide under earthwork masks. Stream instances outside the brush remain. Wildlife/stream component lookup is cached; no repeated forest-hierarchy scan. Only editable original district members contribute to averages or clearing; persistent leveler forest masks now respect their owning combined district, avoiding accidental neighbor clearing.

ZoneGroundPlane adds optional nature, stroke and strength fields; missing fields preserve all legacy grades. Sparse native-sample accumulation stores the pre-stroke height, nearest distance and resulting height, so same-stroke overlaps/straight merges reproduce exactly, new strokes can reshape old cores/banks, and smooth replay is deterministic. Nature height queries use cached native sample tags and actual current terrain, not historical per-point plane scans or a smooth stamp's target mean. Non-flat smooth stamps are excluded from automatic zone-plane height inheritance. Validation bounds strength/IDs, and commits stop at the existing10000-record save limit.

Coastal districts retain the approved ocean datum/material/renderer. TerrainCoastEdits queues affected areas and updates only after the drag (or coalesced startup replay). A sparse edited-cell flood fill connects below-sea cells to the existing ocean; isolated inland pits stay dry. The readable owned Crest clip texture updates in bounded64px tiles. Ground-following level samples get a small sea-level halo so canonical hillside heights cannot lift new bay water. Wet queries, shore-distance/navigation queries, ocean clipping, ground-following levels and depth probes share the edited shoreline. Raising a bay back above sea retracts water. Native camp/river/lake metadata and original ocean polygons are not rewritten. Scope is district terrain and coastal water presentation/physics; the coarse regional relief overview and canonical resource survey still use their original geography.

Found/fixed an older replay bug: RebuildDistrictLots restored all cuts then returned when there were zero lots, without reapplying shared planes. Empty nature-only districts now replay their planes too. This is required for saving terraformed areas before founding a town.

QA on a read-only copy of Burlington/Clayburn combined27+28: modal arms before founding; immediate tool-change cancellation; sea-offset cut at(128,-320) joins ocean at unchanged12m datum, clip texture/negative shore distance/level-input halo agree, full-bank trees clear; isolated inland pit remains dry; Raise/Lower steps, all four level targets, locked drag height, Smooth fractional blend, raising shore dry, out-of-group clearing rejection, binary save and fresh region reopen with matching native heights/coast wetness all pass. UI, carved-bay and raised-shore renders visually inspected. New stream-tier check verifies affected instances and obstacle seats disappear from near/wide/far tiers while outside instances remain. Existing destructive City20-operation/129-plane and populated-town regression passes, retaining its speed and deletion/save checks. Final Nature stamp timings57.39ms cold first large cut, then19.65–32.21ms (individual smaller operations); coast finish roughly13–17ms. These are isolated Editor timings, not FPS. City median from final regression is recorded in outputs/Nature-Terraforming/City-Regression.txt.

Work/nature-terraform contains guarded baselines, source scripts, scratch snapshots, logs, tests and manifest. Current chat outputs/Nature-Terraforming includes verification and renders. Sixteen guarded existing source files plus NatureTerraform, TerrainCoastEdits and NatureTerrainModal helpers/meta synced while main confirmed outside Play at its menu. No private art, normal player saves, commits or pushes. Test-only region edits remain in scratch.
Final main reload verified: compiling=false, scriptCompilationFailed=false, ArmTerraform present. All22 synced file hashes match QA, scoped whitespace checks pass, and main remains outside Play as found. Final City median28.765ms.


## Terrain Import v1 — 2026-11-22

Implemented on feature/terrain-import in the isolated Modal Windows project. Creation → Terrain → Region Shape → Terrain Import opens metadata/height/tidal-mask selection and asynchronous native 3D preview. Strict grayscale16/RGB8 PNG decoding preserves U16 values, validates checksums/dimensions/palette, and reports shoreline inconsistencies. Repair is default OFF, localized and reversible; the original samples remain immutable.

Imports center their physical footprint in the supported Large 32.768km square/16×16 grid without stretching. Smooth generated padding and an orange footprint boundary distinguish source geography. Per-import vertical datum, full original raster and mask are embedded in native saves. Existing Terrain, Crest, region opening, biomes and resource systems are reused. Procedural Remake is hidden for imports. Freshwater/AI interpretation remain deferred. See Documentation/03_Technical/Terrain_Import.md for format and integration details.

Private Manhattan starter is synthetic illustrative geography, not measured DEM data; no starter images or private assets added to Git. Source warning counts remain 358 water-above-sea and 3386 land-below-sea. Isolated Editor integration and actual Play-mode wizard/create/cancel/save/fresh-open tests pass. Native height grids and source rasters round-trip identically, dry below-sea mask land remains dry, and legacy v3/current v13 temperate+tropical procedural generation/saves are covered. Evidence and captures: /Users/joelinstrum/Documents/Codex/2026-11-22/i/outputs. Temporary scene-specific QA harnesses live outside Assets after validation; a portable embedded-fixture decoder check remains under CityForge → Checks. Primary project and normal player saves were not modified by this implementation. Existing ProjectSettings productName difference is local isolation configuration, not part of the feature.

Terrain import UI refresh: matched the user's dark olive wizard reference with a serif title, readable basename file selections backed by full local paths, validation checkmarks, framed/aspect-correct preview, footprint/padding legend, separate summary/elevation/warning columns, repair helper copy and gold create action. Dimensions come from actual metadata (Manhattan padded region32.768km, not the reference image's28km). Preview is still explicit after file/option edits. No terrain generation or persistence behavior changed.

Import window fit correction: the1600×1000 design frame now reuses GrowableModalFit, the existing UI fit component. It scales uniformly against both available parent dimensions with40 UI units of total margin, updates after viewport resizing, and never enlarges past its designed scale. This keeps the title/footer inside short/wide and docked Game views without changing the global canvas or other modals.

Pre-built map choice: Region Shape now offers Pre-built Maps (Manhattan only) separately from Terrain Import. Manhattan opens the common responsive preview with all file fields hidden, loads the packaged triplet automatically, and has Create Region plus orientation/elevation/optional repair controls. Actual synthetic provenance remains visible. Source inputs are privately installed under Assets/StreamingAssets/TerrainPresets/Manhattan, so they ship in desktop builds without requiring Downloads or JSON knowledge. Source-only Git excludes those files: copy the triplet into other worktrees from the private asset workflow; native created saves remain independent.

Repair checkbox report: immediate freeze was not reproduced in isolated Editor or real Play pointer-click checks. Added automatic next-frame preview refresh for preset option changes and an input-key guard so repeated unchanged Toggle/Dropdown callbacks cannot queue regeneration. Old/cancelled preview jobs cannot overwrite newer state. Real Play pointer click returned in1.18ms, frames continued, repaired preview completed, Create re-enabled, and repair OFF rebuilt original geometry. Evidence: current chat outputs/Terrain-Repair-Play-QA.txt. No save/generation schema change. Maximized Game layout was not changed: CUA still binds the primary Unity process instead of this isolated editor.


### November 24 — reconstructed Art Deco offices imported

Continental Insurance, Art Deco Office001 and Marquee Tower are registered through
RebuiltOfficeCatalog, with private assets in Resources/Lots/RebuiltBuildings. Stable
IDs begin cf-building-; commercial/office, modern, C2/Medium wealth. Authored metres
and reserved tile footprints preserved; Near0–2/reduced Medium3–5.
AuthoredBuildingLighting uses the parent PrototypeRegionView clock for recessed
room emission and visible-tier fixture pools; DistrictBuildingDetail preserves these
prefabs' own alternate materials. URP emission materials must retain a valid emission
keyword; daytime darkness is applied per renderer. Original preparation masters and
true far authoring meshes remain in the private library. Native bounds, LOD switches,
lighting states and isolated native lot save/load checks passed. No city-scale
performance or native three-tier routing claim. Detailed handoff and native renders:
/Users/joelinstrum/Documents/Codex/2026-11-22/be/outputs/Building-Game-Import/Handoff.txt


### November24 — repeatable prepared building workflow

RebuiltOfficeCatalog now loads private UnityCatalog.json records (three original
registrations remain fallback); prepared metadata changes update source defaults
without overwriting the separate saved user-property store. Private preparation
harness supports one stable asset ID per request; empty means full-set regression.
The cityforge-buildings skill owns the portable-package contract and checksum-based
unity_package.py. All three current library kits are sealed; repeat staging copies
zero files, runs zero bakes (0.05–0.12s each; excludes Unity import time). Missing/stale
outputs and changed installed GUIDs fail preflight. Read
Documentation/03_Technical/Prepared_Building_Imports.md. Helper checks and isolated
Unity Roslyn editor-assembly compilation passed. No Play session was stopped to
validate this workflow change.


### November24 — Lot Editor and rebuilt building quality repair

Actual LotEditorViewport checks exposed missing back-facing Continental fans and
awnings, blurred atlas lettering, and imported detail-albedo colour-space issues.
Private rebuilt assets now preserve source detail maps/UVs, repair the affected
faces, retain deliberate two-sided thin surfaces and use R-metal/A-smoothness body
maps. Detail material recipes are per-tier *-Materials.json; albedos are sRGB, data
maps linear. Native prep validates detail recipes and preserves asset GUIDs.
LotEditorViewport uses a neutral editor-only day rig, improved soft shadows, SMAA
and aspect-preserving 1.5x sampling capped at4096; zoning previews retain authored
self shadows. AuthoredBuildingLighting.SetPreviewTime connects the editor clock and
isolates lamp pools to layer31. World district lighting remains unchanged.
All three buildings passed native bounds, LOD and lot roundtrip checks; actual
viewport day/night, lamp masks, zoning shadows, capped-target aspect and legacy
house/tree checks passed. Installed source/data match the tested isolated QA copy.
User authorized discarding the unsaved Play session before installation. Original
masters remain untouched; prior kits are archived in each library ReviewArchive;
updated kits are resealed. Evidence and handoff:
/Users/joelinstrum/Documents/Codex/2026-11-22/be/outputs/Lot-Editor-Quality/


### November24 — asset grooming next

User accepts Marquee Tower architecture; next pass is green foliage and less excessive black, preserving bracing and editable olive trim. This grooming remains pending. See Documentation/03_Technical/Asset_Grooming_Handoff.md and the originating chat outputs/Asset-Grooming-Handoff/Handoff.md for private continuation details.

### November 24 — approved Marquee / Majestic fixed sign and materials

Approved perpendicular double-sided Majestic sign is installed on Marquee Tower.
AuthoredSignLighting shares one clock across native tiers, respects district/zoning
pauses and preserves original emission strengths and cumulative sequence.
AuthoredBuildingLighting forwards preview time and excludes sign slots from room
emission. Lot Editor glow is conditional on sign-bearing previews; world lighting
is unchanged. Private grooming restores green shrubs, green spandrels and raised
gold fan accents without changing geometry/counts/UVs/pivots or stable IDs/GUIDs.
179 isolated native Play assertions passed; both-tier day/night and close-ups
reviewed, existing native lot roundtrip passed. Main installed files match QA.
Approved private package is versioned through the shared repository as Marquee
Tower/v003. Source only is committed here; art remains private. See
Documentation/03_Technical/Marquee_Fixed_Sign.md. Reuse its sealed native kit;
older converter/import harness would overwrite the fixed sign/material pass.

## Townhouse / building customization checkpoint — November 25, 2026

See `Documentation/03_Technical/Townhouse_Checkpoint.md` before continuing
the approved modular Green Cornice Bay or the all-era R2 $$ roster reset.
The approved v005 composition still needs native preparation; roster retirement
and procedural townhouse fences are not installed by this checkpoint.

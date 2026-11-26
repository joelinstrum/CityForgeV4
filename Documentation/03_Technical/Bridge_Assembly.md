# Bridge assembly prototype

Implemented November 23, 2026. This supersedes the implementation status in
Bridge_Assembly_Handoff.md; that file remains the original design handoff.

## Player workflow

In an established town, choose City → Transport → Bridges → Build Timber trestle.
The built-in style is available from the district's Art Deco appearance era.
Click a dry road/bank, hold and drag across water to a dry far bank, then release.
Leave enough dry land at both ends for the inclined approaches. A 20% maximum
grade permits shorter approaches; drag closer to each bank. The selected endpoints
remain authoritative, so existing long crossings do not automatically shrink. PointWire owns the
markers, drag/release input, UI/focus/release guards and cursor/collider restoration.
The bridge adapter keeps just the straight start/end alignment.

The cyan ghost shows the deck, railings, supports and bracing. Yellow surface strips
show proposed approach grading and its lateral feather. Nothing is excavated or
saved during dragging or adjustment. Release opens the minimal designer. Adjust
height, carriageway width, lane count, sidewalks, required water clearance and the
pier-free opening. The style button cycles installed styles. Red previews/errors
must be resolved before confirmation. Cancel/Escape discards the draft. Confirm
creates the instance and applies only dry, bounded approach grading. Save District
persists the result using the existing native save flow.

Right-click the physical elevated deck to reopen it, or use the paginated Bridges
catalog. Edits keep the instance ID. Cancel retains the old bridge and grades.
Demolish removes the bridge and restores its owned approach samples. Roads joined
at its endpoints remain ordinary road records. Whole-area demolition removes whole
bridges; partial selections preserve the bridge. Destructive terrain brushes remove
any touched bridge immediately, including its route surface, before sculpting.

Clear trees from the construction corridor first. This prototype rejects overlap
with lots, camps, other bridges and intermediate ground roads. Existing roads can
connect at the two dry endpoints. Keep all construction/feather widths within the
active district union, including combined-district gaps.

## Geometry and navigation

BridgeAssembly samples actual ground and water separately. The level elevated deck
covers the wetted interval plus bank margin; approaches occupy the remainder of the
straight alignment. Bents are repeated outside the designated navigation opening.
The two legs of every bent store separate sampled bed heights. Piers never use the
water plane as a foundation. Transverse X braces connect the bent legs; longitudinal
braces stop on either side of the pier-free opening.

BridgePresentation combines plain box parts into three material groups: deck/ramps,
railings/curbs and support/bracing. The deck/ramps have one combined MeshCollider.
Custom deck art retains the canonical box envelope as a solid transport surface.
No textures, imported artwork or final structural models are required.

Bridges remain separate from DistrictRoadRecord and LotDefinition. TransportRoadRoutes
adds bridge centerlines to freight and immigrant routing. CaptureRouteRoads includes
bridges for established-route invalidation. The pedestrian network has explicit
sidewalk routes, while ambient vehicle routes include the bridge centerline.
TransportHeight supplies deck/ramp height to walkers, ambient vehicles and resource
vehicles without changing terrain or lot height queries. ActorRouteClear permits
water only inside a usable bridge's clearance-inset surface; ordinary water remains
blocked. Road revisions invalidate cached route sections after edits/removal.

The existing graph is planar. The prototype rejects intermediate road crossings
rather than manufacturing connections between different elevations. Lane count is
saved and checked against width; traffic still uses the existing centerline movement
model, not a new multi-lane simulation. There are no curved/movable/suspension bridges
or multilevel interchanges.

Harbor vessel routing checks actual under-deck clearance and the opening/pier envelope
in addition to existing draft/depth checks. Air clearance uses a conservative envelope
from installed vessel renderer bounds, with a safety margin. Already-planned vessels
also check new bridges during movement; a blocked visit ends without sailing through
the structure. Confirmation waits if a vessel currently occupies the proposed crossing.
The designer's required clearance alone is not a promise that every vessel can fit:
large/tall ships also need enough actual headroom and opening width.

## Earthwork and replay

BridgeEarthwork owns sparse heightmap samples inside dry approach rectangles plus a
3 m lateral feather. It retains original and applied samples, blends from the original
baseline on repeated application, and restores only samples still matching its own
applied result. SourceHeight separates the pre-ramp ground from the constructed fill,
so editing cannot mistake the old ramp for a natural bank. Restoration batches samples
in 32-sample tiles. No unrelated heightfield or waterbody is regenerated.

The region stores construction settings and sampled support beds, not private import
paths. Native terrain is reconstructed from the existing region data, existing planes
and lot cuts; bridge approach grading then replays. Lot presentation refreshes reapply
ramps without accumulating the feather. Creating/editing a bridge does not rebuild
existing lot roots or replace their identities. Native ocean, forest, shoreline and
material/shader baselines remain authoritative.

## Style JSON / mods

Built-in: Assets/CityForge/Resources/Config/BridgeStyles/Trestle.json.
Additional runtime JSON catalogs: Application.persistentDataPath/Mods/BridgeStyles/*.json.
Both use the same envelope with schemaVersion 1 and a styles array. Each style has:

- schemaVersion, stable id, label and firstEraId.
- widths: supported carriageway widths in meters (4–16).
- supportSpacing, maxSpan, maxSlope (rise/run), minClearance, maxLength,
  maxSupportHeight, deckThickness and railingHeight.
- deckPart, railingPart, supportPart, bracePart: primitive:box or a Resources prefab
  path, with its authored unit envelope scaled to each module.

All dimensions use meters. IDs must be unique. Invalid schema versions, missing parts,
nonfinite dimensions and unsupported values produce actionable catalog errors.
External JSON does not load arbitrary model files: referenced style assets must
already be installed as normal Resources assets. A full asset-bundle/Workshop loader
is outside this prototype.

The built-in timber trestle supports 4/8/12 m carriageways, 8 m nominal support spacing,
32 m maximum free span, 20% maximum approach slope, 2 m minimum water clearance,
512 m maximum alignment and 40 m maximum bent height. Defaults: 8 m carriageway,
two lanes, 1 m sidewalks each side, 4 m required water clearance, 16 m pier-free opening.

## Saved instances and compatibility

SavedRegionInfo.bridges is additive; null/missing in older saves means no bridges.
Each BridgeInstance keeps schema/version, stable ID, original district, style ID,
start/end coordinates and heights, deck elevation/stations, carriageway/lane/sidewalk
settings, opening/clearance, supports with bed samples, and approach grading settings.
Endpoint coordinates define connections to the saved ordinary road network.

Structural record validation is independent of installed styles. A missing/invalid
style preserves the entire saved crossing and reports repair in Transport → Bridges.
It contributes no unsafe vehicle/walking surface. Reinstall the style, choose another
installed style in the designer, or explicitly demolish it. Missing styles are never
silently discarded on capture/save/load. Installed referenced part assets remain
necessary to render a style; original import/source files do not.

## Verification

Native isolated Unity acceptance uses a disposable river/Art Deco town fixture and
scratch binary save, never a normal player save. It covers PointWire release behavior,
full-heightfield cancellation, dimension validation, real bed supports, idempotent
bounded grading, live vehicle/pedestrian movement to the far bank, connected freight,
physical deck picking, vessel clearance/movement, edits, native save/load and fresh
reconstruction, missing-style repair, catalog pagination, demolition and destructive
terrain integration. Existing freight and established-route suites also pass.

Evidence: /Users/joelinstrum/Documents/Codex/2026-11-23/b/outputs/Bridge-Assembly.
The temporary scene-specific acceptance harness and logs remain under that chat's
work/bridge-assembly and the isolated CityForgeQA project, outside production source.
These are prototype correctness checks, not a whole-city performance benchmark.

Portable replay: `Unity -batchmode -projectPath <disposable checkout> -executeMethod
CityForge.World.BridgePrototypeQA.StartPlay -logFile <log>`. It creates an empty
fixture scene and scratch output under Library/TransportChecks/Bridge; supply the
project's normal private landscape dependencies. Never run mutation tests in a live
player session. Road_Shapes_Handoff.md describes this combined transport checkpoint.

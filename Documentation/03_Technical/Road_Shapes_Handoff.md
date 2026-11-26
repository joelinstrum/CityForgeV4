# Transport checkpoint handoff

Date: November 26,2026. Branch: `feature/road-shapes-and-diagonal-snapping`.
Base: `origin/main` at `f4115d9` (includes the already-merged townhouse checkpoint).

## Player behavior

Select a road type, or click-drag an existing road. Lowercase r/d/t/x/s/i selects
roundabout/D loop/T junction/diagonal crossing/gentle S-curve/square intersection.
Shift holds the larger stamp; Tab rotates90°; click places/repeats. The same shape
key or Escape returns to ordinary drawing. Cancel exits. Shape changes discard only
an unfinished guide; switching mid-drag detaches its old mouse-release commit.
Release the held drag, then click to place. Continuation hints retain the shortcuts.

Freehand roads infer one of eight headings (45° increments) from raw mouse motion.
Four-metre spacing applies in the rotated frame. Cardinal alignment is preserved;
extensions keep their exact original endpoint. Existing road record schema is reused.

## Implementation and boundaries

- `DistrictRoadAlignment.cs`: four undirected normals, line intersections and rotated
  grid/edge snapping. `DistrictStreetJunctions.cs` reuses the same mouse snapping.
- `DistrictRoads.cs`: stamp keyboard routing/lifecycle/hints plus diagonal extensions.
- `DistrictRoundabouts.cs`: all six shape generators, mode transitions and one atomic
  multi-road commit. T/X/i have independent open arms; D/r are closed. S is a sampled
  broad offset connector with near-horizontal end tangents, not a tight hairpin.
- `PointWireTool.cs`: bounded single/multi-path stamps share UI/focus/raycast/release
  gating, transparent overlay, cursor/collider restoration and cancellation. Paths
  preview independently. Consumer validates every road ribbon before any mutation.

Road stamps add no toolbar buttons, road save schema, island props or traffic priority/yield rules;
bridges have their own Transport catalog and additive instance records. No
automatic old-road demolition, private art, generated catalogs or player saves.
This checkpoint also includes the previously uncommitted avenue/boulevard and
bridge integrations. Concrete avenues retain 12 m carriageways; divided boulevards
retain 20 m roadway width, grass/curb medians and middle planting. Vehicle routes use
the paved-side alignment and return to endpoint/junction openings. The standalone
motor fleet, drivers and following work is not part of this checkpoint.

Transport → Bridges builds the versioned timber trestle using plain code-generated
parts. Dry endpoint dragging opens the designer; confirmation validates full geometry,
water clearance, support beds, planar road conflicts and active-district containment.
Approaches use the approved 20% maximum grade. Previews/cancel do not mutate ground.
Elevated surfaces support vehicles, walkers, freight and immigrant routes. Vessel
planning/live movement check actual opening/headroom. Native bridge records are an
additive saved-region field; missing styles preserve records with repair feedback.
Editing preserves IDs; demolition and destructive terrain restore bounded owned grading.

Runtime files: BridgeDefinition/BridgeAssembly/BridgeEarthwork/BridgePresentation,
DistrictBridges/DistrictBoulevards, and focused hooks in road selection, saved regions,
lot clearance, area demolition, destructive earthwork, transport/harbor movement.
Read Bridge_Assembly.md and Art_Deco_Avenues_Boulevards.md for complete contracts.
No private art, generated catalogs or player saves are included.

## Validation and replay

Use Unity 6000.1.12f1 with this project's existing private dependencies installed.
Each native harness can run independently in a disposable checkout:

```
Unity -batchmode -projectPath <checkout> -executeMethod RoadShapeChecks.Run -logFile <log>
Unity -batchmode -projectPath <checkout> -executeMethod RoundaboutChecks.Run -logFile <log>
Unity -batchmode -projectPath <checkout> -executeMethod DirectRoadStampChecks.Run -logFile <log>
Unity -batchmode -projectPath <checkout> -executeMethod DiagonalRoadChecks.Run -logFile <log>
```

Results and native renders go to ignored `Library/RoadShapeChecks/`. These helpers
never write a normal player save. The committed LegacyRoadAlignment fixture is the
pre-change cardinal algorithm used to check regression behavior, editor-only.

Native validation on this expanded PR branch:

| Suite | Checks |
| --- | ---: |
| Shapes, native records/geometry and routing | 1,492 |
| Roundabout regression | 60 |
| Direct drag and continuation | 27 |
| Cardinal/diagonal regression | 899 |
| Bridge construction, movement, save/fresh-open, vessels, repair and removal | 69 |
| Wide-road widths/restore, medians, planting and vehicle alignment | 14 |
| Total | 2,561 |

Bridge replay ran headlessly with vegetation presentation disabled in its disposable
fixture; it verifies the production bridge UI callbacks, physics, grading and native
save/navigation paths. It does not claim a new visual review of the private landscape.
Actual translucent road-stamp previews were rendered and inspected. Both 12 m avenue
and 20 m boulevard records are validated. Standard private dependencies were supplied
locally and excluded from Git. No normal player saves or live Unity were changed.

## Continuing safely

The shared working checkout remains on `feature/era-content-profiles` and contains
other sessions' uncommitted work. Do not switch/reset/clean/stash it to resume this PR.
Use this isolated branch/worktree. The other session owns the latest townhouse install.
Do not add licensed resources, native local catalog, saved regions or packages to Git.
Do not reload the live Unity session during another session's installation. Checkpoint
any live region under ignored Library before future gameplay script edits.

## Expanded transport validation

The portable full bridge replay is `CityForge.World.BridgePrototypeQA.StartPlay`,
compiled only in the Editor and intended for a disposable project. It constructs
its own river/town, saves only under ignored Library/TransportChecks/Bridge, and
checks movement, grading, save/fresh-open, vessels, repairs and destruction. Standard
private landscape dependencies must be installed; none are shipped in this PR.
`WideRoadChecks.Run` checks native widths, restore/persistence, median/tree planning,
paved-side vehicle routes and median meshes/openings. Results go under Library/TransportChecks.
Replay results are recorded above and in the PR description.
A plain native shader/terrain kit is still required to open the disposable river fixture;
-nographics can be used for the bridge replay. Its optional vegetation presentation
is disabled in the test only; production biome/rendering code is unchanged.

# Road shapes and diagonal snapping handoff

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

No new toolbar buttons, save schema, island props, traffic priority/yield rules,
automatic old-road demolition, private art, generated catalogs or player saves.
The live checkout's uncommitted avenue/boulevard/bridge integrations are intentionally
excluded. Wide generic geometry is supported, but this PR does not introduce new
road types. Resolve future merges by retaining both this tool and those integrations.

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

Prior integrated-checkout evidence: 1492 shape +60 roundabout +27 direct-drag +899
cardinal/diagonal checks (2478 total), inspected actual translucent native previews,
main Unity compile/installed hashes checked. The isolated PR branch also compiled and passed all 2,478 native checks; its actual
translucent preview render was inspected. Standard private Crest/MapMagic/wind
dependencies were supplied locally and are excluded from Git.20 m width is generic geometry coverage here;
full road-record validation runs for widths supported by the base road catalog.

## Continuing safely

The shared working checkout remains on `feature/era-content-profiles` and contains
other sessions' uncommitted work. Do not switch/reset/clean/stash it to resume this PR.
Use this isolated branch/worktree. The other session owns the latest townhouse install.
Do not add licensed resources, native local catalog, saved regions or packages to Git.
Do not reload the live Unity session during another session's installation. Checkpoint
any live region under ignored Library before future gameplay script edits.

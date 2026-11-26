# Road shape stamps

After selecting a road surface, press a single unmodified letter:

| Key | Stamp |
| --- | --- |
| r | Roundabout |
| d | D loop, with a flat side |
| t | Three-way T junction |
| x | Diagonal four-way crossing |
| s | Broad S-curve, with gentle parallel end tangents |
| i | Square four-way intersection |

Shift holds the larger size. Tab rotates the complete stamp clockwise90°.
Click places all arms once, and the stamp stays armed for repeated placements.
Press the current shape key again, or Escape, to return to ordinary road drawing.
Another shape key switches directly. Cancel ends the tool. Shift is never required
for selecting a letter. Cmd/Ctrl/Alt combinations and focused text fields are ignored;
Cmd-S remains save, and i retains its ordinary behavior outside road placement.

Centers use the existing4m tile-centre grid. Individual curve vertices are never
freehand-grid aligned. Sizes scale with road width using RoundaboutRadius; small
r=max(12m,1.5*width) rounded up to4m, large doubles it. Arms reach r; the gentle
S spans4r horizontally with2r of offset and horizontal end tangents. Rotation is
about the snapped center. Existing surface, width, sidewalk and street-tree options
apply. No new toolbar buttons, save schema or traffic/yield rules.

PointWireStampPath carries independent open/closed paths. Optional StampPaths and
StampPathsCompletionRequested extend the shared PointWire input/overlay rather than
copying it. Open paths need2 points, closed paths3; each path and the overall shape
are bounded and finite. Preview draws each path independently, without phantom lines
between junction arms or across an S-curve. Existing focus/UI/release/save guards,
cursor/collider ownership and cancellation remain shared. Legacy single-loop stamps
remain supported. Preview never mutates terrain/road records.

PlaceRoadStamp validates every guide, then calls CommitNewRoadPieces once, which
checks the region's total road limit and ALL full-width site meshes before mutation.
Thus a blocked arm cannot leave a partial junction. Results use ordinary open or
closed DistrictRoadRecords; existing native persistence, path editing, deletion,
sidewalks, junctions and transport routing apply. Existing roads are preserved;
stamps do not demolish an old road through a proposed island.

1492 isolated native checks cover all6 shapes, widths3/4/12/20, two sizes/four
rotations, saved record validation, native meshes, composite rejection/repetition,
adapter transitions and every pair of T/X/intersection arms in the transport graph.
Actual translucent PointWire previews were rendered and visually inspected. Existing
60 roundabout checks and899 cardinal/diagonal road checks are regression gates.
Evidence: outputs/Road-Shapes in the ar chat. No normal player saves are written.

Direct road gestures use the same placement tool: click/drag an existing road,
then press a shape key without opening Transport. Continuing an endpoint now
keeps every shortcut visible in its hint. Switching mid-drag clears the transient
guide and detaches the old release gesture, leaving the stamp attached to the
mouse; release that drag, then click to place.27 targeted native checks passed
for all six shapes, hint variants and stray-release prevention.

Portable validation and continuation notes: Road_Shapes_Handoff.md.

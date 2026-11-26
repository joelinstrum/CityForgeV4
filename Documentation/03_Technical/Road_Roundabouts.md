# Road roundabout stamps

Select a road surface, press R, then click the translucent ring to place it.
Hold either Shift key for the larger circle. Release Shift for the smaller one.
R or Escape returns to normal road drawing; Cancel exits road placement.
Stamps repeat after placement. Enter/Place Road does not submit a stamp.
Switching modes discards only the unfinished freehand guide.

The centre snaps to the existing 4m tile-centre grid. Individual circle vertices
are not snapped or freehand-aligned. Small centreline radius is
ceil(max(12m, 1.5*roadWidth)/4m)*4m; large radius doubles it. Standard 4m roads
therefore have outer diameters28m/52m. Wide avenues/boulevards keep a clear island.
The selected road surface, width, street tree option and ordinary sidewalks apply.
No island prop or special traffic-priority/yield simulation is introduced.
Existing roads are preserved; place the ring on clear ground and connect approaches.

PointWireTool supplies optional StampGuide/StampCompletionRequested callbacks and
shares existing UI/focus, terrain-raycast, release gating, transparent overlay,
cursor/collider ownership and cancellation. The callback validates finite bounded
closed guides before the road adapter uses the existing full-ribbon site validation
and CommitNewRoadPieces. Preview does not mutate terrain or road records.
Errors retain the active stamp. A successful ring is an ordinary closed, curved,
48-guide-point DistrictRoadRecord with gridAligned=false. Existing persistence,
deletion, road editing, sidewalks, junctions and transport routing apply; no save
schema or automatic demolition. Connected corners preserve approach joins.

The filled PointWire overlay now projects with its actual canvas camera (null for
ScreenSpaceOverlay); this also supports ScreenSpaceCamera canvases correctly.

Validation: isolated native geometry/save validation for widths3/4/12/20, both sizes,
actual PointWire repeat/rejection/cancel and road-adapter mode transitions, native
transport routing around the island, plus inspected solid and translucent native
renders. Main project refresh/compile is checked after installation. No player
region or lot saves are written for testing.

Additional d/t/x/s/i stamps and Tab rotation now share this tool. See Road_Shapes.md.

Portable validation and continuation notes: Road_Shapes_Handoff.md.

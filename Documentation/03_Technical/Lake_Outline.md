# Current implementation

LakeOutlineTool now adapts reusable PointWire and commits a rounded lake on valid
closure. See PointWire.md and Player_Lakes.md. The transient-only description below
is historical; Escape clears the guide but does not undo an accepted lake.

# Lake / pond outline tool

Water > Add Water Bodies > Lake / Pond arms the transient outline utility and hides
the popup. Click terrain for markers. A blue arrow replaces the normal ground
cursor; the blue string follows the pointer. After three markers, clicking within
24 screen pixels of the first closes the outline. The first marker is larger.
Backspace undoes a marker or reopens a closed loop. Escape, changing tools or
leaving the district clears the sketch. UI clicks do not place markers. Focus loss
hides the pointer/string and retains placed markers.

LakeOutlineTool exposes Armed, Closed, Points, PlaceMarker and UndoMarker. The
future basin/water implementation can consume the completed footprint. Current
sketches are temporary and are not saved. No terrain, trees or water are changed.
No automatic polygon intersection validation is implemented in this first utility.
Streams, rivers and removal remain selection-only.

TerrainCollider state and cursor visibility are restored on cancellation/destruction.
Up to256 markers and32 terrain samples per segment bound the UI mesh. The overlay
projects ground samples through the district camera into the HUD; its Graphic
requires a CanvasRenderer and full HUD bounds. All graphics ignore UI raycasts.
Marker rings use the existing blue-tinted overlay shader; no imported art is added.

Runtime checks: actual menu callback arms; first marker, duplicate rejection, early
closure rejection, closed-loop marker count, undo/reopen, cleanup and unchanged
forest pass. Closed boundary and live string rendered over grass/trees and visually
inspected. Automated mouse-button injection was not used as evidence of clicks.

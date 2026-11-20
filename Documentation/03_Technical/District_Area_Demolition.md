# District area demolition

Clicking Detonate keeps the existing single-lot/road-run action. Dragging more than
6 screen pixels starts a world-axis rectangle from the original ground point.
A red terrain-following outline follows the pointer; release commits, Escape or
focus loss cancels, and release over UI cancels. Dragging back to a degenerate box
also cancels rather than deleting the original click target. No mutation occurs
while previewing. The tool stays armed for another selection.

The selected area removes fully enclosed lots, placed actors/trees/streetscape by
origin, zoning cells by center, natural forest by trunk base, and painted ground
pixels by center. Existing terrain, lakes/rivers and regional geography remain.
All point edits are scoped to active original district members, including L-shaped
combined districts. Forest clearing was explicitly requested by the user.

Roads crossing the rectangle retain their outside centerline sections. Closed and
curved roads are supported: curves use the rendered sampled centerline and retained
pieces are stored as straight polylines to preserve that shape. Collinear/short
interior markers are simplified; a sub-meter remnant is discarded. Pieces exceeding
the64-marker limit are split into valid records. Road count/guide validation occurs
before any scene mutation; an unrepresentable fragment rejects the selection with
a visible message. Crossings beyond the selection are preserved. New road runs,
sidewalks, junctions, recess and route caches rebuild through existing code.

ForestRemovalStroke adds optional rectangleSize. Missing/zero means the original
circle; positive width/depth means a rectangle centered at x/z. Contains is shared
by cache/fresh generation through IsForestRemoved. Validation rejects nonfinite,
negative, one-sided zero and oversized rectangles. The existing radius/district
fields remain valid, and original circular masks retain their behavior. Rectangles
consume one of the existing8192 edit slots. Matching explicit forest additions
are removed, then native/mixed renderers and geography caches invalidate through
the existing APIs. Later tree planting continues to override older removal masks.

Ground paint clears all eight channels inside the box at the existing.25m pixel
resolution, retains outside bytes, flushes modified tiles and prunes empty tiles.
Deleting zoned lots through area destruction also removes selected zoning so the
area does not immediately regrow. Existing single-lot Detonate keeps its growth
behavior. Save District/Region persists edits normally; destruction does not save
or modify a library lot definition.

Private MaintenanceQA verifies pure road clipping, old/new forest metadata, an
actual area commit, outside paint preservation, outline cancellation and save
validation. Its scene and data are disposable; player region files are untouched.

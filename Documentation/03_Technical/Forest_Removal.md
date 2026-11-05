# Forest removal

Forests now offers Remove, Add Woodlands and Add trees. Only Remove changes the map;
the add choices remain configuration panels. All nature popup windows use twice the
previous dimensions. Explicitly clicking Remove hides the popup and arms the brush.
Opening Forests alone does not arm it. Reopening the category cancels painting.

The software cursor is a tree with a red X. A red ground-following circle indicates
the area. Hold left mouse and drag to clear tree bases along the circle path. Tab
switches circumference200m /400m (radius31.831m /63.662m). Escape, other categories,
region overview and teardown cancel safely. UI clicks do not edit the map. Cursor
visibility and any temporarily enabled terrain collider restore on exit. Rendering
uses the main camera only, excluding reflection captures.

Fast motion interpolates samples every quarter-radius; holding still adds no edits.
Release stamps the endpoint and ends the drag. Only valid landscape mouse-down starts
a stroke. Passing over UI/invalid terrain resets the interpolation anchor, so no
clearing connects across blocked regions. Cancel/focus loss ends painting. Samples
for each frame use one batched mutation and renderer invalidation instead of repeated
terrain refreshes. Existing saved circles keep their original radii.

MeadowDistrictLandscape.RemoveForest removes woody native instances, preserves shrubs
and ground cover, rebuilds mature/native arrays and the canopy index, and removes
matching MixedDistrictForest roots. SpatialForestRenderer invalidates cached batches
and drops removed captured entries immediately. Placement signatures are recomputed
from the remaining original records. Seasonal presentations use the edited canonical
arrays. Legacy authored and tropical palm submissions also respect removal masks.

ForestRemovalStroke metadata stores world x/z, radius and original district members.
SavedRegionStore.Capture/Write/Load and PrototypeRegionView.OpenStages preserve it.
Missing metadata in older saves means no edits. Validation bounds size/count and
checks finite values and district indices. Scoped masks avoid deleting an unselected
neighbor when a brush crosses the active district boundary. Masks apply to restored
caches and fresh planting, after the normal seeded coverage selection, preventing
regrowth or replacement candidates filling the cleared area. Existing terrain and
control maps stay unchanged. Save District/Region persists edits normally; the brush
does not auto-save. Undo and adding foliage are future work.

ForestRemovalChecks.Begin(checkpointPath) runs on a disposable copy from Library.
It verifies both sizes, exact removal/outside preservation, batch invalidation,
seasons, private disk round trip, deliberately stale cache and no-cache regeneration,
then restores the untouched original checkpoint. No normal player save is written.
The latest November3 temperate test removed36 then84 trees and7 more along a
fast drag, and passed the data-path checks. Native mouse automation produced no
Unity mouse-button events, so that extra driver check was inconclusive. Legacy/palm
paths are implemented but were not independently visually tested in that run.

## Planting order

Explicit forestAdditions override older removal masks at their positions. A later
removal deletes matching addition records as well as visible trees, so stale caches
cannot resurrect them. Filter base removals before merging additions. See
Forest_Planting.md.

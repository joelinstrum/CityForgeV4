# Woodland painting

Add Woodlands arms the shared ForestRemovalBrush in adding mode. The green circle
and tree-plus cursor use200/400m circumference; Tab toggles and hold/drag paints.
UI, focus, cancellation and distance sampling match Remove. Repeated overlap fills
available lattice sites without stacking trunks. Species choices retain their exact
approved summer variants across reopening.

ForestAdditionEdits uses the original district9.48m jittered lattice, minimum4.2m
trunk separation and water/road/slope/camp eligibility guards. It appends canonical
instances and MixedDistrictForest roots incrementally, retaining existing objects.
SpatialForestRenderer.Append captures only new LODs; canopy/native refresh batches
once per frame. Legacy native families use their existing seasonal presentation.

SavedRegionInfo.forestAdditions carries position, original district, prototype,
variant, width, height and rotation. Explicit positions override older removal
masks; later removal deletes these records. Cache merging removes matching cached
instances then appends records, giving idempotent stale-cache and fresh generation.
Validation bounds all values and district membership and rejects duplicates. Limits
are65,536 additions,200,000 total canonical trees and16MiB metadata. Existing saves
without records remain compatible. No base ecology re-ranking occurs.

ForestAdditionChecks verifies two sizes (36+105 trees), dragging (76), no repeated
stacking/stationary edits, existing object identity, habitat/canonical/render parity,
remove/replant ordering, Larch selection, seasons, disk reopening, stale caches and
fresh generation. Disposable fixture only; restore the latest user checkpoint.

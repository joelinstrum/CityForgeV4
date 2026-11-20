# Zoning parcel sizes and grid guides

Preset 2×2, 3×3 and 4×4 blocks reserve complete square lots using saved
`TownZoneCell` size and anchor metadata. Growth requires a saved zoning layout
with exactly that frontage/depth, zone and wealth. A 3×3 reservation stays empty
when only 2×2 or 2×3 layouts are available. `HasGrowthZoning` checks candidate
world dimensions, per-cell metadata and shared anchor ownership. Rectangular
frontage/depth swap world axes for east/west. Materializing a layout preserves its
dimensions. Diagonal blocks select exact-size layouts and place native rotated
records rather than rasterizing their lot footprint.

Auto zoning intentionally permits different saved layout sizes. Existing zoning
is skipped by overlapping preset stamps; placing a new block does not convert
old Auto cells. The November19 read-only Guilford inspection found no fixed-size
mismatch: smaller lots mostly occupied Auto cells. Existing lots are preserved.

Press G in a district for the construction grid. Each complete reserved lot is
one combined grid cell: internal 4m lattice edges disappear, leaving only the
parcel boundary. Existing Auto-grown zoning lots and native diagonal lots likewise
show their actual footprint. Unreserved ground retains its fine 4m grid. Partially
erased reservations retain the fine grid until a complete parcel exists again.
There are no floating size labels or separate colored parcel-outline renderers.

`DistrictParcelGrid` collects reserved, grown and valid preview footprints as
`ParcelGridCell` rectangles. `DistrictGrid` spatially indexes these footprints in
4m buckets, clips the ordinary lattice against their open interiors, and appends
native diagonal edges into the same mesh/material. Boundary-coincident lines are
preserved. Terrain sampling, G toggle, active combined-district membership and the
existing subtle grid appearance remain authoritative. Cardinal parcel edges
already coincide with the surviving lattice. Shared diagonal edges are deduplicated.

Block placement temporarily merges the grid within available valid preview
parcels. Pointer/UI focus, cancellation and placement update that temporary mask;
leaving the preview restores the surrounding lattice. Grid geometry rebuilds on
zoning/grid/preview changes only, never every frame for a stationary preview.
The zone color surfaces join within each reservation, retaining a gap only at the
outer parcel edges. Cardinal frontage arrows appear once per parcel instead of
once per underlying tile. Diagonal preview colors also have no internal gaps.

This changes presentation only. Saved zoning dimensions, growth restrictions,
terrain, road routing and the underlying 4m placement lattice are preserved.
There is no save schema change or migration of player lots.

Verification: ResidentialGrowthChecks rejects smaller candidates inside reserved
parcels and retains zone/wealth/anchor/save checks. Private MergedGridQA exercises
2/3/4 square blocks at four cardinal rotations, absence of interior grid edges,
retained surrounding lattice, native rotated edges, preview/cancel/G visibility,
partial erasure, combined-district exterior borders and excluded L-shaped corners,
and unchanged serialized zoning. An isolated top-down Unity render compares
3×3 and 4×4 cells in the actual grid/zone meshes.

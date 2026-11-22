# Geography-based terrain surfaces

Detailed districts retain the existing Unity Terrain renderer and URP PBR lighting.
The geography pass blends only into existing meadow coverage. Road, shoreline,
and other authored terrain paint retains its existing rendering path. The
region relief map uses its existing materials.

## Control maps

`TerrainGeography` generates two linear RGBA8 maps per active terrain, on a
world-aligned lattice with inclusive boundary samples. The first map stores
meadow, dry grass, woodland, and a reserved zero channel. The second stores disturbed soil
and rock in red and green. Weights are normalized when generated and again
after sampling to compensate for quantization and filtering.

The rules use regional moisture and elevation, mature canopy coverage, lot footprints, and terrain slope. World-space noise
softens transitions. District perimeter samples use shared regional heights
and woodland data, with local canopy, slope, and lot influence fading into
the perimeter. Within each terrain, dirty chunks write into the same texture
lattice, so they share filtering with their neighbors.

Forest edits, lake or channel edits, and changes to lot footprints invalidate
the affected control-map chunks. The component recalculates one dirty chunk
per frame and uploads the two small control textures. The map is derived
presentation data and does not enter the save schema. A future painting tool
can supply authored weights before the normalization step.

## Palette and fallback

Edit the six slots in `Resources/Region/TerrainGeographyPalette.asset`.
Slots support albedo, a world-meter repeat scale, an optional linear RGB
tangent-space normal map, and smoothness. Five active slots use the supplied local CityTerrainAlbedos textures; slot 3 is retired and reserved. The
existing RegionPBR normal maps supply restrained detail where available.
The optional linear albedo mean softens mottling at distant zoom while
preserving the geography blend, and per-surface gain and bias tune the
palette without editing source images. A missing palette or texture falls back to the existing
terrain for the corresponding area.

The runtime terrain component has Inspector context menu commands for
diagnostic colors and for disabling the blend. Diagnostic colors are green
for meadow, yellow for dry grass, purple for woodland,
orange for disturbed soil, and gray for rock.

The shader adds no passes or draw calls. It reads two control maps, skips
surface textures with negligible weights, and fades out additional normal
sampling once it is too fine to resolve. The existing distant terrain
basemap beyond gameplay district zooms uses its original appearance.

## Manual terraforming refresh

Nature → Terrain offers Update Terrain beside Sculpt Terrain. Sculpting changes
heights and clears content immediately; coastal water and beach repainting wait
for Update Terrain. Reopening the Terrain panel or cancelling a stroke does not
flush that batch. Update Terrain stops the brush, refreshes connected ocean
clipping, shoreline distances, existing sand/harbor layers, water levels and depth
probes. The update includes existing saved Nature edits, allowing older raised
seabed to be repaired. Loading saved edits still refreshes the coast automatically.

Dry edited cells explicitly exclude ocean, even if their original clip admitted
water. Beach repainting uses current shoreline distances and heights, reuses the
existing layers and original base weights, and stays within edited bounds plus
a shoreline margin. Canonical geography, authored water records, ocean datum and
materials remain unchanged. Regional overview/resource surveys retain original
geography. No save schema change; saving heights before pressing Update Terrain
retains edits and the derived coast is reconstructed on reopening.

Edited dry coastal ground now replaces both appended sand and inherited canonical
beach soil with local ecological grass weights. Sparse brush coverage builds on
Update Terrain/replay with an 8m feather at the bank edge; height strokes do not
repaint. Submerged cells retain sand and untouched beaches retain their original
treatment. Water level inputs reset to the ocean datum across dry edited cells
as well as connected wet cells; old stranded shoreline plants hide locally.

Default texture continuity: edited dry coasts extend eligible nearby canonical
dry-ground coverage weights, including the fifth coarse layer, rather than
filling with a single grass layer. A lazy nearest-source field is derived from
the original base maps, excluding the old beach/soil-dominant samples. Texture
references, tiling, grading and world coordinates are unchanged. Geography
uses the same source ecology/canopy with the existing brush-edge feather.

Final continuity refinement: prefer the grass/coarse mixture retained underneath
the old canonical shoreline paint at each original world coordinate. Removing
shore soil and normalizing those weights preserves the existing 2D distribution.
Nearest dry meadow is only a fallback when those original grass weights are
absent; its 2D reflected swath avoids stretching a reference row across a shelf.
Texture assets, UV coordinates, grading, tiling and palette remain unchanged.
Coast clip values encode terrain height near the crossing, with one dry cell of
under-terrain overlap as on native harbor banks. Ground-cover invalidation is
limited to edited bounds. Level targets/flat cores and native grid precision
remain unchanged.

Grass overlay correction: the geographic material layer must refresh alongside
the terrain alphamap. Old freshwater geometry can otherwise mark new dry land
as smooth moist grass. Edited grass now extends the complete classification
from eligible, unmodified dry-grass references, excluding wet grass/forest floor;
native classification and real exposed rock remain authoritative outside that
blend. Eligibility reads native inputs directly for stable repeated updates and
save replay. The reference grid is capped at 257-square and warmed during the
explicit update/loading operation. No texture, tiling, palette or canonical water
metadata change is required.

## Wet-grass surface retirement

The smooth moist-grass surface is retired globally at the user's direction.
Runtime controls always write zero to geography channel A. Freshwater proximity
no longer paints a broad smooth-green halo or suppresses woodland cover. The
shared shader folds old wet coverage into detailed meadow and does not sample
the retired texture. Serialized palette slot 3 remains reserved, without binding
its texture. Authored terrain layer 3 (leaf litter) is independent and retained.

The earlier edited-coast geographic donor workaround is superseded and removed.
Coastal base texture restoration still preserves its canonical grass/coarse
mixture. Lake/channel edits still invalidate changed slopes and surfaces; water
shapes, levels, wetness queries, shore paint and ecological data are unaffected.

## Shore material controls — November22,2026

The second existing RGBA8 control map keeps disturbed soil/rock in RG. BA now hold
clamp(.5 + signed shore distance /64) and coastal sand coverage. Only primary RGB
and extra RG normalize as material weights; primary A remains retired zero.

Irregular sand/grass coverage is independent of the smooth geometric bank profile.
The shared shader darkens sand near the actual shoreline and preserves its grain,
including the hidden add pass that draws later terrain layers. Base geography
reuses its control read; add/basemap passes read the same existing control map once.
There are no new render objects, geometry, texture allocations or draw passes.

Owned coastal water scales absorption/scattering together for shallower optical
density and more readable sand below water, retaining their deep-color ratio and
existing depth/clipping inputs. Foam is reduced; calm inland renderer routing is
unchanged. The user's concept swatches remain reference only.

## Continuous native estuary floor — November22,2026

All native wet river/coastal cells retain normal negative beach distance. Removing
the former river-only sentinel prevents cached native grass/bed paint meeting
updated coastal sand at a rectangular boundary. Riverbank edge seeds remain
excluded to avoid applying a wide ocean beach along upstream banks.

Sea-level river/bay beds share sand continuously. Graded rivers restore the native
bed palette with a smooth12–96m landward blend, shared by initial paint and refresh.
Sculpted underwater paint blends toward sand through the actual ground-edit mask;
a rectangular pending refresh is never itself a material footprint. Existing depth
fields, authored heights, water levels and shader/draw counts are unchanged.

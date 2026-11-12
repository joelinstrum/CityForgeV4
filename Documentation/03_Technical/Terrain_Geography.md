# Geography-based terrain surfaces

Detailed districts retain the existing Unity Terrain renderer and URP PBR lighting.
The geography pass blends only into existing meadow coverage. Road, shoreline,
and other authored terrain paint retains its existing rendering path. The
region relief map uses its existing materials.

## Control maps

`TerrainGeography` generates two linear RGBA8 maps per active terrain, on a
world-aligned lattice with inclusive boundary samples. The first map stores
meadow, dry grass, woodland, and moist grass. The second stores disturbed soil
and rock in red and green. Weights are normalized when generated and again
after sampling to compensate for quantization and filtering.

The rules use regional moisture and elevation, mature canopy coverage, distance
to mapped freshwater, lot footprints, and terrain slope. World-space noise
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
tangent-space normal map, and smoothness. The six slots use the supplied local CityTerrainAlbedos textures. The
existing RegionPBR normal maps supply restrained detail where available.
The optional linear albedo mean softens mottling at distant zoom while
preserving the geography blend, and per-surface gain and bias tune the
palette without editing source images. A missing palette or texture falls back to the existing
terrain for the corresponding area.

The runtime terrain component has Inspector context menu commands for
diagnostic colors and for disabling the blend. Diagnostic colors are green
for meadow, yellow for dry grass, purple for woodland, cyan for moist grass,
orange for disturbed soil, and gray for rock.

The shader adds no passes or draw calls. It reads two control maps, skips
surface textures with negligible weights, and fades out additional normal
sampling once it is too fine to resolve. The existing distant terrain
basemap beyond gameplay district zooms uses its original appearance.

# Terrain Import v1

## Pre-built maps

Creation → Terrain → Region Shape → Pre-built Maps opens Lower Manhattan circa1609 directly, with
automatic preview, orientation/elevation options and Create Region. No JSON or file
selection is exposed in that flow. Terrain Import remains the separate advanced
file workflow. Both use the same decoder, preview, generation and embedded native
persistence. Manhattan is explicitly labeled synthetic/illustrative.

The new private default is installed at
Assets/StreamingAssets/TerrainPresets/Manhattan1609/ with
Manhattan_1609_CityForge.json, Manhattan_1609_Height.png and
Manhattan_1609_Water.png. It is a12×12km,2048² synthetic historical-inspired
schematic, not GIS or a verified1609 reconstruction. The supplied metadata uses
a different schema; the separate CityForge JSON maps field names only and includes
all original metadata values. Original JSON/PNGs are retained unchanged. Select the
CityForge JSON for advanced imports. The former17×28km private Manhattan folder
remains available through advanced import; existing native saves retain their own
embedded rasters and settings. StreamingAssets and private .meta GUIDs are excluded
from the public source repository; copy them separately to another installation.


Creation → Terrain → Region Shape → Terrain Import → Import terrain files opens
an import panel with metadata, height PNG and optional water PNG paths. Choosing
metadata fills matching PNG paths. Browse is supported inside the Unity Editor;
standalone builds accept pasted absolute paths without an additional file-dialog
plugin. Preview validates and prepares data off the main thread. The existing wizard shows a flat north-up texture derived from the actual import,
with elevation color, authoritative tidal footprint, an orange source boundary,
source/encoding elevations, datum offset, source provenance and warnings. Preview
pixels are prepared on the existing worker; no separate3D scene/camera is created.
The crop includes nearby generated padding; the summary states the full32.768km
region size. Actual playable districts remain native3D Terrain/Crest. No source file
is modified. Create Imported Region uses the normal generation/open pipeline.
Cancel is available during preview and imported-region creation, including Escape
on the loading overlay. The import panel is also cleaned up when its parent closes.

## Input contract

Metadata schema cityforge.terrain-import.v1, north-up, with pixel_width/height,
approximate_extent_m.width/height and elevation_encoding.black_m/white_m/sea_level_m.
The reader accepts non-interlaced grayscale16 PNG for heights and optional RGB8 PNG
for water. It verifies PNG chunk CRCs, zlib checksum, dimensions, exact scanline size,
all row filters0–4 and big-endian U16 samples. It never uses Texture2D.LoadImage for
source decoding, so it performs no gamma/color-space conversion or bit-depth loss.

Height = black + U16/65535 × (white−black), in metres, including seabed heights.
Water: black #000000 land; blue #0000FF tidal. Cyan/green freshwater labels are
recognized but explicitly rejected as deferred v1 features; all other colors are
validation errors. Dimensions must match metadata and each other. Inputs are bounded
at4096×4096 and64MB per PNG; metadata is bounded at64KB. Footprints are1–32768m per
axis. Unsupported formats fail clearly without generating a partial map.

## Geographic coordinates, scale and padding

Original pixels are north-up, row-major NW origin. SourceToWorld and SourceUV form
one paired orientation transform; world +Z is north before optional90° rotation.
Samples use endpoint/vertex alignment: spacing width/(W−1), height/(H−1), with no
half-pixel shift. Heights use bilinear interpolation; labels use nearest-neighbor.

For v1 imports, the existing Large-size/16×16-district infrastructure supplies a
32.768km square; each district remains2048m. Each input footprint (currently12×12km for the1609 schematic) is centered without
rescaling. Original footprint dimensions and rotation persist in metadata and the
orange boundary appears in preview and regional overview. Padding matches edge
heights and slopes, easing the slope to a flat apron over500m; it does not create a
cliff or new categorical transition at the source boundary. Water classes outside
the footprint continue the nearest source edge. Padding is generated continuation,
not claimed geographic data. Inputs larger than32.768km are rejected, never shrunk.

An import-specific positive vertical offset keeps both seabed and sea in the native
Terrain coordinate range; the original elevation range and sea datum remain recorded.
The Manhattan fixture uses +41m, so physical sea0m renders at game Y41m. Vertical
exaggeration, if explicitly selected, scales relief around source sea level before
the offset. It does not alter labels. Original U16 samples remain unchanged.

## Shoreline warnings and optional repair

The preview counts water-labelled ground above source sea and land-labelled ground
below source sea. These are warnings. Mask land below sea does not become ocean.
Repair Shoreline defaults OFF. When enabled, only inconsistent samples within a
two-pixel Manhattan-distance shoreline band are clamped to sea±0.02m before selected
relief scaling. It is deterministic, localized and derived. Turning it off and
previewing again reproduces original geometry; original samples are embedded in
saves even when repair is enabled. Imported samples are not silently raised to the
procedural version13 one-metre bank-clearance rule.

## Existing terrain, water and ecology integration

TerrainImportData generates the existing513×513 regional height/distance grids and
SavedRegionSnapshot. PrototypeRegionData retains the full-resolution import sampler;
detailed districts bypass procedural river smoothing and use that sampler directly.
No procedural geography graph, estuary shaping or coastal landform recipe reshapes
imported heights. The normal biomes, resources, districts/towns and approved landscape
renderers still run. Resource/camp water classification also uses the authoritative mask.
The procedural Remake Map action is hidden for imports; revise import settings by
creating another imported region. Existing procedural generation branches remain unchanged.

One normal Ocean RegionWaterBody descriptor delegates footprint queries to the exact
imported raster. The existing Crest coastal-water renderer uses the same labels for
its clipping mask with point sampling. Ocean material, bathymetry/depth probes and
shore painting remain the established systems. The regional relief renderer uses
mask classification, preserves below-sea dry ground, and avoids procedural bank lift
or additional cartographic height scaling for imports. Preview-only water is a
categorical surface mesh; it does not replace gameplay Crest water. The preview
limits native Terrain simplification so shallow channels cannot disappear at map
scale. These preview quality settings are local to its temporary Terrain.

## Native persistence and compatibility

Small TerrainImportSettings metadata lives in the existing creation recipe.
New imports additionally retain the complete import JSON in optional
sourceMetadataJson (including source_metadata in an adapted dataset); older saves
with this field absent remain valid. A tagged
binary tail after the existing terrain/meadow data embeds original U16 samples and
water labels in the native compressed .cfregion. No original path is needed on load.
The saved native513 grids remain byte-identical; loaded raster data rebinds the ocean
footprint before region/district setup. Missing/corrupt payloads are errors, not a
fallback to procedural geography. Activity outside the importer is unaffected.

Procedural saves retain the existing binary layout with no import tail. An inactive
or absent import field does not opt in. The existing recipe-less-save workaround
now balances nested JSON objects instead of using a flat regex, preserving old null
recipes after adding nested import metadata.

## Verification

CityForge → Checks → Terrain Import Decoder checks tiny embedded synthetic PNG
fixtures with known0/32768/65535 and adjacent-U16 values through all five row filters,
metre decoding and corruption rejection. Local integration evidence covers the full
2048² Manhattan synthetic fixture, exact358/3386 warning counts, dimensions/palette/
missing/8-bit/corruption validation, cancellation, repair reversal, native save and
fresh district reopening, existing Crest water, below-sea dry land, and procedural
legacy/current-climate regressions. Source images in the starter kit are synthetic,
not a measured Manhattan DEM/coastline/bathymetry and are not shipped as production
geographic presets. They remain in the private import workflow.


## Lower Manhattan1609 validation and water coverage

Source encoding -40…80m, sea0m, actual -20.63264…25.96292m; game offset+41m.
Zero original water-above-sea / land-below-sea warnings; repair OFF, north-up,1×.
Original U16/labels/metadata round trip exactly in native payloads while both input
and installed-preset directories are absent. Real wizard Create and native menu
reopen passed;625 native combined-district height samples differed by at most
0.26683m, with77 mask-land samples dry and548 tidal samples. This is a sampled
check, not a full-raster terrain interpolation error bound. Native cached district
save/load passed. Procedural v3/v13 × temperate/tropical generation/save tests passed.

Visual QA caught incomplete ocean coverage at wide district zoom: the small river
prefab range and camera-centered simulation left seabed exposed. Imported coasts
now reuse CoastalWaterClock's ground-focus helper and set ScaleRange's maximum to
max(32, Side/32). Rides use the actual viewer position. Existing procedural-water
configuration, source heights, ocean material/waves and categorical clipping remain.
Rendered before/after inspection confirmed the tidal surface covers the viewed area.

The dataset has long straight coast segments, an angular island and source shoreline
height steps up to about10.05m over5.86m. Preserve and disclose these limitations;
do not add fictitious docks or smooth the input into an implied accurate map.
Outside the12km source footprint, padding is generated continuation. Accurate future
inputs need explicit CRS/georeferencing, a vertical/tidal datum, documented historical
shoreline and DEM/bathymetry provenance/uncertainty, and registration conventions.
Freshwater import remains deferred; this kit uses no cyan/green labels.

# Polygon-constrained texture paint

Texturize is available from Parks and from the Lot Editor's Textures picker. It uses
existing LotPathMaterial textures. The All tab shows every available path texture in
a scrolling thumbnail grid; Natural/Paving filter that same collection. Click markers around an area and close by clicking
within24 screen pixels of the first marker. PointWirePath owns marker/closure state;
PointWireGeometry rejects intersecting outlines and supplies concave containment.
The first marker is larger. Backspace removes an unclosed marker. Hold the left mouse
button to paint or erase after closure. Right/middle camera navigation stays available.
The diameter slider covers1–32m; Feather controls a symmetric0–8m selection-edge blend (0m is a hard edge). Erasing fades
all paint weights back toward the underlying ground. New boundary retains paint;
Done/Esc removes editing guides. Undo stroke retains the last12 paint edits in this
editing session. Lot Editor strokes also enter the normal lot undo history.

## Data and rendering

GroundPaintData stores sparse32m tiles, each with a128×128 map (25cm texels), at most
eight path-material IDs and eight normalized byte weights per texel. The polygon
mask uses PointWireGeometry signed distance for a symmetric smooth coverage ramp
across the selection boundary. The mask caps incoming texture coverage so repeated
strokes preserve feathered edges. Brush edges soften over the selected feather
distance; zero uses a hard brush and hard selection. District/lot eligibility
still clips painting to allowed ground. Polygons themselves are transient. Maps are deflate/base64
encoded on stroke completion, never as individual persistent brush strokes. Data
validation checks sizes, duplicate tiles/palettes, known IDs, coverage and bounded
compressed payloads. Empty tiles are removed after erasing. A document caps at256
tiles; lot parsing caps at64. Palette slots can be reclaimed when a texture is fully
erased from a tile. Regions retain the existing overall metadata-size guard.

GroundPaintRenderer creates one terrain-conforming1m mesh per tile, shared by all
painted polygons and materials in that tile. Its URP shader blends eight source
textures using two linear RGBA weight maps and receives the main sun/shadows and
ambient SH. Clean instances with identical encoded maps share refcounted GPU weight
textures, including repeated reusable lots. Editing maps update only affected tiles.
Meshes and textures are released on teardown. There is no per-stroke/polygon mesh.
Paint sits at8.25cm and renders after legacy ground overlays, before blended paths;
opaque paths and buildings retain depth priority.

## Coordinate ownership and persistence

DistrictTexturePaint stores world-XZ data in SavedRegionInfo.groundPaint, captured
and restored by SavedRegionStore/PrototypeRegionView. Only active district/union
chunks render; brush eligibility checks district containment, actual dry ground and
camp exclusion. Existing terrain/biome alphamaps are not rewritten. Missing paint
metadata remains empty on old maps. Entering overview clears the presentation.

LotDefinition.groundPaint uses lot-local XZ coordinates. The Lot Editor supplies its
viewport-specific mouse ray and screen projection to the shared TexturePaintTool;
painting does not hijack object placement or camera panning. Four quarter-turns of
the lot restore original paint coordinates/weights. Runtime lot roots provide full
native placement rotation, including45 degrees. Painted lots can be saved as normal,
template or growable lots. Player customization mode retains its existing restrictions.

## Verification

Isolated Unity QA covers concave hard masks, brush/erase, multiple materials, normalized
weights, eligibility exclusions, eight-layer limit, corrupt payload rejection, compressed
maps, four rotations, lot undo/redo, actual .cflot library and .cfregion file roundtrips,
Parks/Textures menu entrypoints, lot-tool arm/close callbacks, shader compilation, fixed
mesh counts, rendered controls, and reconstruction with no visible boundary. Fixtures
use an isolated product/library and scratch snapshots, never the player's saves.

## Existing grass textures in Natural

The shared palette now includes Lush Grass, Patchy Grass, Tileable Grass and Worn
Grass, reusing Lots/GroundTextures resources directly. LotPathMaterial accepts an
optional explicit textureResource; entries require that texture to be available.
Grass IDs are appended, preserving earlier catalog ordering. Lush/Patchy/Tileable
repeat at8m and Worn at16m, matching their established ground-texture scales.
Maintained Grass is byte-identical to Lush Grass, so it is not a duplicate swatch.
Both Parks and Lot Editor Texturize expose these through Natural and All. Current
private content has26 available textures. Original artwork and saves remain unchanged.

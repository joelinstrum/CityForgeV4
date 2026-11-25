# Modern paving and automatic street trees

This pass adds paving and automatic sidewalk planting to existing roads/streets. Divided carriageways, planted medians, lane-direction rules, lane markings and highways are separate follow-up work; existing routing and widths are preserved.

## Surfaces

Four appended road IDs preserve all existing IDs and indexes: weathered-concrete-road, weathered-asphalt-road, weathered-concrete-street and weathered-asphalt-street. The two supplied private PNGs are shared through LotPathMaterial weathered-concrete-pavement and weathered-charcoal-asphalt, at a four-metre repeat. Road and street choices use the same texture resources. They appear from Art Deco onward. Existing brick/stone choices remain available; advancing eras does not repave saved roads. Existing blacktop/paved-street saves retain their former materials.

Street variants retain the existing four-metre carriageway, sidewalk builder, frontage eligibility, junction masking, PointWire placement, road validation and editing. The shared paving palette also makes the two surfaces usable by existing path tools. Textures are copied unchanged; repeat wrapping, mipmaps, sRGB and anisotropic filtering are configured through the local importer. These are supplied albedo images, not new normal/roughness maps or concrete slab-joint geometry.

## Designated planting styles

Roads > Streets offers No Trees, Narrow Cypress, Small Elm and Palms. Cypress reuses pencil-cypress-small at authored scale; Small Elm uses the retained siberian-elm-young at 70% scale (approximately3.4m crown footprint). Palms reuse TropicalCoconut/Palms variant0 at8m height, with all four authored LOD meshes, original material and deterministic heading. They are an explicit visual style, not an automatically selected climate or historical claim. No retired tree or new licensed model is introduced.

New streets store the optional streetTreeStyle ID on DistrictRoadRecord. Empty/missing means no automatic trees, preserving older saves. Automatic instances are derived from saved road geometry and are not duplicated in placed-actor records. The shared palette is separate from era availability and can support median-specific choices later.

Rows are sampled along the rendered/rounded centerline, 0.9m outside the carriageway, with1.1m soil pits. Cypress spacing is10m; elm/palm spacing12m. Open endpoints retain8m gaps. Planting skips carriageways, other street approaches, water, outside-active-district points, building crown-clearance envelopes, authored entrance/driveway corridors and lot paths, existing placed trees and streetscape props. Missing space yields a gap, not forced placement. Authored building entries are resolved through the existing entrance helper. Clearance remains conservative and does not simulate roots or pruning.

Road context menu Trees cycles the style for existing streets. Repaving through the palette can choose a planting style; the quick repave action preserves its existing style. Extensions and demolition splits retain style IDs. Derived trees update on completed sidewalk/road/lot mutations, prop placement/movement/deletion and season changes; manual park-tree placement avoids automatic trees. No recurring idle scene scan is added. Unchanged planting retains its instances; queued sidewalk work yields during tree creation. Bounds are128 trees per street and1024 per active district presentation, leaving excess positions unplanted. Removing roads and clearing a district dispose derived trees and owned pit assets.

## Validation

Isolated Unity Play checks cover supplied resources, stable historical road IDs/frontage, current saved-record validation, optional-field compatibility, deterministic spacing/end clearance, real automatic model/pit rendering, instance retention, native palm LOD reuse, intersection exclusions, last-road cleanup, Art Deco menu availability alongside brick, tree-style selection and road roundtrip. Native screenshots include the road menu, both surfaces, palms and an Art Deco townhome frontage. These fixtures do not write normal player saves and are not a city-scale performance benchmark.

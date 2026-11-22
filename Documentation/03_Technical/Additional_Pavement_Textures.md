# Additional pavement textures · November 20, 2026

Six supplied PNG albedos from Downloads/textures are registered in the shared
LotPathMaterial Paving palette and Transport → Roads → Streets. All six street
choices are available from the Founders era onward, without wealth restrictions.
They support commercial frontage, standard 4m placement/repaving, sidewalks,
street snapping, junction blending and saved road IDs. Existing road IDs, indices,
placed surfaces and zoning defaults are preserved. These are optional surfaces.

| Street | Material ID | Repeat span |
|---|---|---|
| Macadam Gravel | `early-macadam-gravel` | 4m |
| Mossy Fieldstone | `mossy-fieldstone` | 3m |
| Herringbone Brick | `weathered-herringbone-pavement` | 3m |
| Irregular Flagstone | `irregular-flagstone-pavement` | 3m |
| Weathered Red Brick | `weathered-red-brick-pavement` | 3m |
| Rustic Wood Planks | `rustic-wood-pavement` | 4m |

Street record IDs are `founders-<material ID>-street`. Private images retain the
supplied 1254×1254 RGB pixels at Resources/Lots/PathMaterials/<material ID>.png.
Import settings: sRGB, Repeat, trilinear mipmaps, anisotropy8, NPOT scale None,
max2048, high-quality compression, CPU readability off. Only albedos were supplied;
no inferred normal/roughness maps or baked lights are added. Shared path rendering
uses its existing matte response. Source-only checkouts retain fallback colors.

The street picker now uses four columns and three compact rows with fitting
captions. It shows nine Founders street options, plus the existing Paved Street
in its existing later eras. StreetJunctions uses the same Founders classification
as frontage and the picker, avoiding a separate list of accepted surface IDs.

Isolated QA imports all six textures, checks availability/dimensions, full-edge
commercial frontage beside each actual road mesh, 4m width, JSON identity,
record validation and preserved historical indices. Rendered road strips and
Founders/later-era menus were inspected. Evidence: November20 ple/outputs/New-Pavement.
Targeted mixed-surface T/cross/angled junction rendering, gestures, snapping and
binary save/load also passed. Main Unity loads all six textures and recognizes
their commercial frontage. This is targeted validation, not an exhaustive
district-authoring regression pass. Player maps and lot definitions are not written. Artwork remains private/ignored.

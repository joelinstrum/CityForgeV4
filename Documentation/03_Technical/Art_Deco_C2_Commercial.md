# Art Deco C2 medium-wealth commercial buildings

Six supplied commercial FBXs use the existing private V3Buildings/catalog workflow.
All are Commercial2 / Medium ($$), timeline id `modern` (Art Deco Era).

| Building | Private folder | Width x depth x height (m) | Near / distant triangles |
| --- | --- | --- | --- |
| Hotel | C2ArtDecoHotel | 21.68 x 35.43 x 31.50 | 26,303 / 24,131 |
| Theater | C2ArtDecoTheater | 12.47 x 17.67 x 18.00 | 27,473 / 22,887 |
| Kingsley Department Store | C2ArtDecoKingsley | 17.16 x 16.00 x 23.50 | 24,614 / 21,790 |
| Dry Goods | C2ArtDecoDryGoods | 13.52 x 28.09 x 23.00 | 26,759 / 24,119 |
| Cartier Corner | C2ArtDecoCartierCorner | 12.50 x 12.14 x 19.00 | 26,126 / 23,608 |
| Metro Bank | C2ArtDecoMetroBank | 10.97 x 23.55 x 21.50 | 26,426 / 22,564 |

## Colored sign lighting

`LotAssetCatalogEntry.preserveEmissionColors` is an optional, default-false flag. Only these six registrations opt in. `ResidentialWindowLights` keeps the existing time-of-day controller and renderer property blocks, using a neutral scalar for opted-in RGB maps rather than multiplying the entire map by warm window color. Existing assets retain their original warm/high-wealth behavior. Catalog setup merges the flag into existing private entries. No new shader, runtime image processing, per-sign update loop, texture sampler or lighting pass is introduced.

The private `Windows.png` convention now contains an RGB linear emission atlas for these assets: warm values for selected upper windows, warm luminance-modulated display panes to retain contents, and original sign/fixture color converted from source sRGB to linear RGB. Sign values are scaled to half strength to limit clipping; lettering and artwork are not redrawn. Evening/night scalar brightness changes preserve color ratios. Dynamic emission is off by day. Original color/normal artwork remains unchanged, including any illustrated/baked lighting already in the supplied diffuse.

Signs are authored as explicit per-facade regions, baked through source mesh triangles/UV islands. A prototype projection lit stone behind the projecting theatre blade sign and was rejected. Final sign baking requires a surface-axis normal component above0.65 and proximity to the visible source depth (512px depth reference, three-pixel minimum filter, tolerance0.007 times source height). Tight blade/marquee boundaries prevent lit collars/stone; glass keeps its separate pane-sampling rules. Numeric regions, source mapping and lamp anchors remain in private Authoring/WindowGrid/ImportInfo files. The final authoring entry points are bake-final.py and prepare.py under this chat work/artdeco-commercial; intermediate prototype scripts are not the final recipes.

Signs retain red, gold, green and white source artwork. Exterior fixtures/canopy bulbs receive authored emission and bounded warm local pools where appropriate; no lamp is invented for the corner furniture building. Near/far meshes share materials, pivots, UVs and source footprint. Model.fbx serves Zoom0-2 and Distant.fbx Zoom3-5; reduction protects front facades, signs, illuminated panes, roof and footprint extremes. Sizes are doorway-proportion estimates. The supplied corner shape is preserved; this does not add a corner-lot solver or fabricate playable lot templates.

## Validation and private dependencies

Targeted Unity checks cover six registrations, C2/wealth/era eligibility and exclusions, properties/row pool/packing, isolated lot roundtrip, grounded pivots/matching bounds, reduced triangles, actual zoom0/2/3/5/2 switching, both-tier noon/evening/night emission, neutral RGB multipliers, lamp count/UV alignment/phase behavior and unchanged warm lighting for all twelve R2 Art Deco homes. Four-corner day and near/far night captures were visually reviewed. Theatre sign probes retain a red glyph and neutral white marquee, with zero emission on tested stone controls. This is import/lighting QA, not an FPS benchmark or full city regression. No player saves are written.

All FBXs, textures, masks, materials, authoring metadata and generated LocalCatalog remain private/ignored. Receiving sessions need the private C2 asset ZIP as well as source changes; let catalog setup rebuild LocalCatalog rather than copying it across projects. Evidence and transfer instructions are in outputs/ArtDeco-Commercial.

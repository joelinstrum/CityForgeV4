# Lot Editor Buildings gallery

The Buildings picker uses the supplied three-column concept: era/category/use/wealth and history filters on the left, searchable paged grid/list in the center, selected model and catalog details on the right. Thumbnail size changes page capacity. A single click previews the card without replacing its GameObject, preserving Unity’s pointer click-count tracking. Double-clicking a card in grid or list view performs Select Building, which calls the original ArmAsset placement flow. Favorite buttons keep their own independent click target. Close/Back/Escape leave existing lot edits intact. Expanded preview supports the same eight rotation steps as the detail preview.

Favorites persist stable asset IDs under CityForge.Buildings.Favorites. New placement/use history persists IDs under CityForge.Buildings.Used; existing recent assets seed the current view, the unused-tracking filter has since been removed. Recently Used retains the existing shared 32-item history. Clear Filters resets the active era, category, use, wealth, search and history filters, without deleting saved favorites.

All counts, thumbnails, descriptions, wealth, availability and footprint measurements come from the current private catalog. The gallery does not invent costs, resident capacity, story counts or requirements. The existing Building Properties modal remains reachable and returns to the previous gallery state. Existing metadata overrides continue to drive thumbnails, default scale and placement.

The harbor panorama and header emblem are private resources under UI/LotBuildingGallery. The gallery uses CityForge modal skins, fonts and vector glyphs; image resources remain ignored by Git. Missing private header artwork simply leaves the code-native header and text usable. Native building thumbnails are rendered with an optional tighter projected-bounds framing mode; other thumbnail consumers retain the prior defaults. Detail caches are released when selection changes; all gallery caches are disposed when the modal/editor closes.

Play QA covers real catalog cards and preview textures, card selection versus placement, persisted favorites, favorite filtering, empty-result selection guards, list mode, era selection without changing authored lot era, clearing filters, pagination, expanded preview, Building Properties roundtrip and arming the existing placement tool. Grid and list screenshots are visually reviewed. Temporary QA harnesses remain outside the installed primary sources.

Gallery dropdown rows are 36 UI units tall and allow full-height text, preventing the chosen font from truncating every option in DefaultControls’ short default rows. Actual opened wealth/use lists and option selection are verified in Play, including opaque popup rendering.

## Shared thumbnail daylight

Catalog previews use a key light positioned relative to the preview camera and the existing LotDaylightProbe as per-renderer custom SH fill. URP's preferred sun is scoped to the preview light during the single render request. Other active directional lights temporarily exclude only the preview layer; their masks and the original sun are restored in finally. This prevents the district/viewport sun from stealing or adding to thumbnail lighting. Post-processing and volume sampling are disabled for this catalog camera. The main world, its materials, and the full Lot Editor viewport retain their existing lighting behavior.

Controlled native tests compare a building and tree against the prior renderer, vary the scene sun and ambient conditions using fresh thumbnail caches, assert scene sun/mask restoration and unchanged material palettes/texture bindings, and rerun both gallery interaction suites. Small tree-pixel differences from animated foliage are tolerated.

Current era UI: LOT ERA is read-only lot context. FILTER BY ERA below Favorites controls visible assets only. A fresh lot context seeds era/category from the lot; manual browsing filters persist for the same context. Blank lot eras default to All eras. Filtering never edits the lot first era.

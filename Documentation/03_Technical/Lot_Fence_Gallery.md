# Lot Editor Fences gallery

The Fences modal shares the Building/Tree gallery shell, current daylight thumbnail renderer, preview rotation/expansion, search, sorting, pagination, grid/list controls, favorites and usage history. It uses the supplied private fence emblem and countryside header under UI/LotFenceGallery. The existing twenty fence prefabs remain the only placeable assets; no decorative header image becomes a new fence model.

## Choose the placement action

- **Place individual piece** arms the original placement tool, including gates, corners and decorative panels. Selecting this action clears any unbuilt guide; the details say so while a guide is ready.
- **Draw fence run** chooses a span first and starts the original PointWire path. Enter completes an open run; clicking the first marker closes a loop. Existing completion and release/input guards remain authoritative.
- **Surround lot** uses the existing sidewalk-inset rectangle and the original LotFenceLayout builder, applying the chosen style directly. The entire loop is one undoable lot edit.
- **Fill current guide** appears when reopening a completed guide. It retains the original markers and open/closed state. Only run-compatible spans are offered for drawing, surrounding and filling; gates/corners remain individual assets.

Double-click uses the currently selected placement action. Single-click previews without rebuilding its card. Clear Filters preserves the chosen tool. Select Fences on This Lot reuses the original editable-fence selection handler. Clear Unbuilt Guide discards only transient markers, never committed fence objects.

Family, piece type and wealth/finish filters use LotPropStyles metadata. Wealth categories aid browsing and do not restrict lot or growth eligibility. Native span lengths and existing straight/partial/full/end-post rules remain unchanged. Preview lots use the actual placement span length and end-post flag, so cards show the same dimensions as a single placed piece. Height stays authored; run spans stretch only through the existing layout builder to meet route endpoints.

Fence favorites and usage history are stored separately under CityForge.Fences.Favorites/Used. The shared recent list remains compatible. Persistent lot geometry continues to use the existing object IDs, asset IDs, fenceLength and fenceEndPost fields; there is no schema change. Growable fence variation, era rules and private asset preparation are unchanged.

## Validation

Isolated native Play QA first runs the Buildings and Trees regression suites. Fence checks cover actual available assets/counts, live family/piece/finish filters, visible dropdown options, native lit previews, independent favorites/history, original style-first PointWire open-run completion, endpoint posts, exact inset surrounding, one-step undo, save/reload of fence objects, select-all, individual gate double-click, existing-guide fill and cleanup, and retained guides/no partial object commits after capacity failure. Only disposable work fixtures are written; normal user saves are untouched. Native modal and surrounded-lot screenshots are inspected before installation.

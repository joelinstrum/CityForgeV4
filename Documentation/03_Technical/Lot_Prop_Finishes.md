# Lot fence and sidewalk finishes

The source catalog `Resources/Config/LotPropStyles.json` classifies the current twenty
fence assets by wealth, family, shape, and era. Wealth chooses decoration only; these
rules add no growth eligibility constraint. All growable lots automatically choose
finishes when populated. There is no authoring toggle or editor-time substitution.
Other props remain authored.

| Wealth | Fence families | Example path finishes |
| --- | --- | --- |
| $ | Rustic, weathered picket, Founders timber, native meadow wood variants | Dirt, garden soil, gravel |
| $$ | White picket, flowered picket | Plain brick, cobblestone, bluestone, plankboard |
| $$$ | Wrought iron, stone-column iron, beige stone iron | Herringbone, garden flagstone, medallions, diamond stonework, mosaics |

Fence pools are Founders-era only. Unsupported eras, unclassified fences, missing
private art, and missing fence wealth retain authored fences. Every fence piece,
including corners, gates and decorative panels, participates in complete-family
compatibility checks. Missing like-shaped pieces disqualify the entire candidate family.

## Texture wealth categories

All path materials and nonempty base/overlay texture entries have a wealth category.
Fancy decorative paving is high wealth; plain brick, cobblestone, bluestone, timber and
concrete are medium wealth; dirt, soil, gravel and worn/patchy ground are low wealth.
Lush/maintained grass and plain sidewalk overlays are medium wealth. None/clear remains
neutral. Shared path/overlay IDs use the same wealth. Texture pickers expose All / Dirt
and worn ground / Plain / Fancy filters and searchable wealth groups. These are browsing
categories, not placement restrictions. Growable path pools use the corrected source
texture wealth at construction. Ground alpha masks, painting and existing lots are unchanged.

## Authoring

The fence picker has an All / $ / $$ / $$$ filter and family/shape groups. New full
sections use 2.4m spans and partials use 1.2m spans. Both single placement and PointWire
fills use these lengths; the final path subdivisions can stretch to meet the exact
route endpoints. Fence random-size placement is disabled. Native model dimensions
remain catalog metadata, and only the span axis is stretched; height stays unchanged.
Standalone straight partials are registered as spans by catalog preparation.

The Lot Editor keeps the authored fence and path assets visible. Wealth and shape
categories aid browsing; there are no finish opt-in/opt-out controls. Every path material
is classified by wealth, surface role and era. Paths automatically choose a like replacement
at build time, matching the source material's wealth rather than the lot's wealth. One
choice per compatible pool makes repeated paths uniform across the lot. Walk paving,
natural ground, garden soil, timber decks and road paving have separate pools; decks
cannot turn into gravel and garden soil cannot become brick. Vehicle paths can vary
within their matching road pool. A pool with only one available finish keeps that finish.

Path points, width, curvature, blending, rotation, depth, border and pedestrian/vehicle
use are preserved. Generated dirt entrance paths and district road sidewalks retain their
surfaces. Ground overlays such as the alpha-shaped Worn Sidewalk remain authored.
Concrete is a Victorian finish; the other current path pools support the Founders era.
Compacted Gravel Pebble declares wealths [Low, Medium, High] and is available under all
three texture filters. Shared textures use the lot wealth when it is in their allowed
set, while exclusive textures keep their source wealth. Replacement pools still require
the same surface role and era.
Build choices are stored as material IDs with the populated lot; editor preview, rendering,
save/reopen and moves never reroll them.

## Construction and compatibility

Town growth materializes a detached layout, appends its building, assigns a persistent
instance ID, and applies the finishes once. Playable/template lots do not substitute
finishes. Legacy optional settings are ignored. Moving, reopening, rendering, and
changing surrounding wealth do not reroll a built lot. The saved object asset IDs, span lengths and path material IDs
are authoritative. The source layout and existing placed lots are not rewritten.

One family is selected for the complete fence set. Candidates must match the authored
fences' wealth (a low-wealth fence stays low-wealth even on a high-wealth lot), era, and
every required piece shape. Full sections replace full sections; partials replace actual
partials; ends, corners, gates and decorative pieces require their matching shapes.
There is no full-panel fallback for a missing partial. The end-post flag is retained.
Mixed source wealth or an unclassified fence prevents the whole-set substitution.

Legacy route lengths, positions, scale, IDs and headings are preserved, including
standalone fences whose source axis runs along Z. A candidate family is skipped if
it would leave placement bounds or increase overlap with a building, fixed prop, or
a parallel/nonadjacent replacement fence. Shared route endpoints can share posts.
Commit is atomic for all fence objects: if no complete family fits, the complete
authored fence set remains. Some wealth/shape combinations currently have only one
complete family; those retain their appearance until more matching pieces are imported.

Near and distant models share the span transform. Distant straight-fence meshes receive
a small endpoint/ground alignment correction for reduction losses, without changing
height. Fence detail switches at Zoom 3 and returns to the original at Zoom 2. Other
building/prop detail thresholds remain unchanged. Existing authored native LODs are reused.

All choices and geometry checks run at construction, with no per-frame selection or
resource scanning. There is no new simulation component per prop. Private source art,
reduced models and the generated LocalCatalog stay ignored; the wealth catalog is public.

## Verification

`CityForge.Editor.LotPropStyleChecks.Run` measures every classified straight model at
its normalized length and center, tests path fill counts/endpoints and distant model
alignment in both zoom directions, builds forty variants at each wealth, checks stable
save/reopen and source isolation, growable-only/unsupported-era behavior, legacy disabled settings,
strict source wealth and shape matching, whole-set fallback for missing pieces,
all path material pools, consistent path replacements and preserved path geometry,
legacy lengths/axes, collision rejection, and absence of optional controls and actual editor wealth-filter callbacks.
`Regressions` also runs extraction, lumberjack, freight, cargo and vehicle checks.
`Render` and `RenderUi` produce disposable private-art preview images under Library.
These are synthetic/editor checks; player-authored lots and maps are not saved.

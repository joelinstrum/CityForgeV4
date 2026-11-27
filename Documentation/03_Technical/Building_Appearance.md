# Building appearance

The shared Edit Building modal supports trim tint on Marquee Tower
(`cf-building-marquee-tower-001`) and independent trim/awning tints on Kingsley
(`v3-building-c2-art-deco-kingsley-department-store`). The six stable palette IDs are `cream`, `sand`,
`red`, `green`, `blue` and `charcoal`; empty/missing means the approved original.
Siding, material replacement and height remain deferred. The Signage browser now
supports per-mount native sign selection with live preview, Original and No sign.

`LotObjectDefinition.buildingAppearance` is additive, per-object data. Validation is
pure and safe on background save/load threads. Lot Editor previews keep draft values
outside the session; Apply records one undoable session edit. Existing Save writes
the appearance into the lot. Cancel, close and Escape discard the draft.

In game, Apply clones only the selected placed lot definition and repaints existing
objects through `lotBuildingViews`. It does not mutate a library/template, rebuild a
lot, alter another placement, or reset the live sign clock. Save Region persists the
additive data through the existing compressed region format. Existing landscaping
composition and protected-data checks retain building appearance.

`BuildingPaint` attaches after native near/medium presentation is created. Marquee uses
the opted-in `Near-Trim` and `Medium-Trim` slots. Kingsley uses exact material
references and authored neutral albedos in `BuildingTintProfile` on both tiers. Body
atlases, windows, gold ornament and sign surfaces are excluded. Property blocks retain
all unrelated lighting properties. Normals, metallic/smoothness and emission remain
authored. Original restores the exact source base texture and color.

The rendering-only BuildingTrimNeutralize shader removes the approved green panel
hue while retaining baked tonal detail. Neutral maps are linear ARGB32, capped at
2048 pixels per dimension, generated on demand and shared by source texture. Leases
release them when the last painted instance resets or is destroyed. Source textures,
materials and prefabs are never modified. No map is generated for an original building.

Adding another building requires explicit prepared, independently mapped paintable
material regions on both native tiers and a reviewed normalization reference; never
infer paint regions from a generic body atlas or paint window/sign materials. Update
the capability registry and renderer binding together, then review both tiers and
check save scopes. Use the explicit `BuildingTintProfile` contract for additional reviewed regions.

Sign choices persist in buildingAppearance.signs as stable mountId/signId pairs.
Missing pairs retain Original. The reserved sign ID none removes the sign face while
retaining the authored mounting hardware. The browser offers six native 8.5m tower
blade variants; the measured Marquee mount accepts five and rejects the wider
Paramount. Runtime renderer refresh preserves per-building objects and clocks across
Near0-2/Medium3-5. Missing/unavailable saved signs fall back visually to Original
without deleting the saved choice. Additional native sizes need distinct sign IDs.


## Kingsley front fascia signs

Kingsley keeps native ID `v3-building-c2-art-deco-kingsley-department-store` and now uses reviewed Near/Medium semantic-material prefabs. Its `fascia-front` mount is FrontFacing with `cf-wall-fascia-v1`, bottom-centre pivot `(0,4.475,6.30)` and envelope `13.1 × 1.65 × .25m`. All six horizontal department-store signs are compatible; projecting blade signs are filtered out. Original restores the approved blank fascia. Source dimensions, day albedo and selective night emission remain unchanged (static night intensity4). The browser preserves wide thumbnail proportions. Lot/instance selection persistence uses the existing mount/sign ID contract. Trim and awning tint support is described below. Native sign acceptance:242 Play checks plus actual Lot Editor day/night renders.


## Independent tint regions

`buildingAppearance.trimColor` retains its original semantics. The optional
`awningColor` uses the same stable palette IDs; missing/empty means Original. Both
fields participate in validation, dirty detection, undo/redo and lot/region saves.

`BuildingTintProfile.bindings` explicitly links a `trim` or `awnings` region to
its exact native material and authored `neutralAlbedo`. Each enabled region must
resolve on both native tiers. Kingsley Near includes separate awning seam paint;
Medium has no seam material. Fabric and seams share the awning selection.
Kingsley uses existing shared neutral textures directly; Marquee retains its
reviewed cached neutralization shader. Runtime changes only per-instance BaseMap
and BaseColor property blocks, preserving the original shader/PBR/emission values.
Original restores each region independently. No wall tint or arbitrary texture
replacement is enabled. Native acceptance:477 Kingsley checks,83 Marquee regression
checks, and actual Lot Editor day/night views with red trim/cream awnings.


## Experimental wall tint — v007

At the user's explicit request, wall tint is installed on Kingsley without QA.
Optional `wallColor` uses the same stable palette and per-object save path.
Both native profiles bind only Masonry and StoneTrim to their original albedo
textures, so tint multiplies the original warm stone color and preserves detail.
Original restores exact inputs. Other material properties and surfaces are untouched.
This supersedes the earlier wall-disabled status; previous acceptance checks do
not cover wall tint. The v006 accepted package is preserved; v007 is an untested
candidate for user experimentation.


### Wall saturation

Wall tint includes a0–100% saturation slider, default50%. It scales the selected
palette color’s HSV saturation while retaining its hue/value and the source wall
texture. `wallSaturationSet` distinguishes an explicit0% from an old/missing value;
`wallSaturation` stores0–1. Missing values resolve to.5, including existing wall
choices from the experimental pilot. Saturation participates in dirty detection,
validation and per-lot/instance persistence. Original clears the tint and resets
the default. No additional QA run for this experimental iteration.


### Reviewed wall paint (2026-11-25)

The editor labels buildings with explicit reviewed paint regions **Paintable**. Green Cornice Bay exposes Brick walls; Sandstone Art Deco Townhouse exposes Stone walls (ashlar and foundation). Near and Medium bind the same wall materials to neutral albedos. White and Yellow join the wall palette; Original restores the exact original finish. Windows, doors, bay frames, copper and carved ornament are excluded. Unmapped buildings cannot open appearance customization unless they have explicit reviewed sign mounts. Existing wallColor and saturation save fields persist these choices. The authored sandstone Far atlas is not paint-enabled and is not used by current native tier routing.


### Expanded Art Deco Wall Paint (2026-11-26)

All five active Art Deco townhouse entries have explicit Wall Paint mappings on
Near and Medium: Green Cornice Bay, Sandstone Townhouse, Brick Townhouse 2,
Stepped Brick Townhouse, and Limestone Townhouse. Limestone binds only its aged
ashlar wall and canonical brick skin. Stepped Brick binds only its authored
warm-weathered-brick material; its architectural-weathering shader is retained.
Kingsley retains its existing wall mappings. Metro Bank, Dry Goods, Cartier
Corner and Gothic Theater additionally expose their separate masonry materials.
Carved ornament, windows, doors, signs, roofs and unrelated materials are excluded
from the new mappings. Original restores the original texture and base color.

`ArtDecoWallPaintSetup.Install` is an explicit Editor menu command to reinstall
these six new building mappings after a native prefab reimport. It preserves
existing trim/awning bindings, requires exact material paths and opaque,
non-emissive, actually-used materials, and rejects ambiguous nested profiles.
It never runs automatically and does not edit shared materials or authoring kits.
The new bindings retain the source wall albedo under palette multiplication;
underlying red brick may produce darker/more muted blues than a neutral paint map.
The existing saturation slider and per-object lot/region fields are reused.

This is not universal atlas painting. Art Deco Office 001, both Continental
Insurance registrations, Marquee's body, the newer single-atlas office imports,
Riverside Gas Station and other unmapped atlas buildings require selectively
prepared wall masks or material separation before Wall Paint can safely cover
them. Do not enable their full body atlas as a wall region or relax the emission
and transparency checks. Retired townhomes remain retired.

Building Look now uses its house icon and opens for any selected building in the
Lot Editor. Unsupported controls explain their unavailable state; opening the
modal does not grant paint capability. Limestone is exempted from the legacy
retirement filter while retaining its stable installed ID and existing 2x5 fit.


### Apply while a preview stage is hidden

Paint discovery now resolves parent tint and sign profiles with includeInactive.
The Lot Editor may construct its refreshed instance beneath a disabled preview
stage. The former active-parent lookup could leave the painter with zero slots
although Apply correctly stored wallColor in the session. BuildingPaint also keeps
its current appearance and reapplies it once in LateUpdate following OnEnable,
after prepared child preview controls have initialized their material blocks.
This does not scan the scene or repaint every frame. The real Lot Editor modal
ChooseWall/Accept callback, session edit, preview rebuild and stage reopen are
covered by isolated Play checks on Brick Townhouse 2, Stepped Brick and Limestone.
The earlier direct renderer checks did not cover this lifecycle.


### Fixed brick texture finishes (2026-11-26)

The user replaced unpredictable brick tinting with five fixed finishes: Original
red, Blue-gray, Yellow, Beige and White. Green Cornice Bay, Brick Townhouse 2 and
Stepped Brick Townhouse retain their own brick bond, UVs and original maps. Each
brick wall binding has four optional BuildingWallFinish records with actual baked
albedo textures. Original red uses the exact source texture and base color.
The existing wallColor IDs blue/yellow/sand/white persist the choice; no save-field
change is needed. Fixed brick finishes ignore wall saturation and use a white
_BaseColor, so no red albedo is multiplied by a blue tint. Legacy cream maps to
Beige, red to Original, and green/charcoal to Blue-gray without deleting saved IDs.

BrickFinishSetup explicitly bakes source-derived maps using a red-clay face mask,
retaining neutral mortar and relative luminance detail. It writes private PNGs and
bindings on Near/Medium; shared source albedo, normal, PBR, geometry and UVs remain
unchanged. Stepped Brick also receives matching calm/distance maps and the baked
finish mean color, avoiding a distance-dependent return to its original red map.
ArtDecoWallPaintSetup preserves these finish arrays when reinstalling wall mappings.

Building Look shows Brick Finish with four named texture thumbnails plus Original
red, and hides saturation for these buildings. Stone wall tint, trim, awning and
sign choices retain their existing independent paths. This is per-object appearance;
shared source materials are never edited by a player's selection. Native validation
covers texture identity, no multiplicative tint, both tiers, Original, normal-map
preservation, persistence, and the actual modal Apply/preview reopening path.


### Softer yellow and Stepped Brick front garden (2026-11-26)

The fixed Yellow brick finish uses #FFF6DA (pale, creamy soft yellow; supersedes the earlier #F5E3A6 butter yellow).
BrickFinishSetup.InstallYellow regenerates only the four yellow maps across the
three brick families, including Stepped Brick's calm/distance map. Blue-gray,
Beige and White map bytes are unchanged.

SteppedGardenFlowerSetup installs the prepared cream-white flowering shrubs from
cf-component-art-deco-brownstone-white-flowers-001/v001 into Stepped Brick's fenced
front garden. It removes exactly the original GardenBox's 1,260 triangles on each
native tier using its measured native bounds; the upper window box, soil, fence,
stairs and other geometry remain. Six shrubs sit four centimetres into the soil.
The native Near uses the botanical Medium mesh (1,928 triangles per shrub); the
native Medium uses botanical Far (320 triangles). The unchanged sealed component
and original building authoring kit remain authoritative; this is a native
presentation addition. Prepared flower albedos/materials retain their stable GUIDs.
No player save/schema or catalog registration changes are required.


### Garden color correction

SteppedGardenColorSetup clones three local garden-only materials: lifted green leaf
color, green stems and warm brown matte soil. The dark original earth and foliage
under self-shadow read nearly black at lot scale. Both native tiers reference the
local variants. White flower petals, shared botanical source materials, building
finishes and global lighting remain unchanged. SteppedGardenFlowerSetup invokes
the color setup on reinstallation. Actual Lot Editor day/night and reduced-tier
views are recorded in the November 26 ple outputs/Garden-Color-Correction folder.


### Low-poly green garden infill

SteppedGardenGreenerySetup reuses the prepared shrub Far leaf geometry alone
(120 triangles), normalizes it to a ground-level mound and combines eight nearby
clumps / four distant clumps into one native mesh per tier. It reuses the existing
Garden-Leaves material and adds 960 / 480 triangles. Native totals are now 50,385
Near and 21,216 Medium, compared with 39,117 / 20,076 before the flower replacement
and 49,425 / 20,736 after the white shrubs. Texture finishes retain geometry.
The flower installer also restores this infill after a native reimport.


### Green-first mini gardens across active townhouses (November 26)

TownhouseMiniGardenSetup now installs native Near/Medium gardens for Green Cornice
Bay, Sandstone, Limestone, Brick Townhouse 2 and Stepped Brick. Low-poly green leaf
clumps form the main planting, with two to four white bloom accents per house
(three on Green Cornice). Prepared Far shrub leaves contribute 120 triangles per
clump; white heads use botanical Medium at native Near and botanical Far at native
Medium. Components are combined by material. Shared garden-only green and warm
soil materials are excluded from building paint. No new colliders are added.

Limestone uses its two existing stone planters; Stepped uses its fenced soil bed.
Sandstone has paired beds beside the central stairs. Green Cornice has one stone
bordered bed clear of its entrance and bay. Brick Townhouse 2 has a shallow bordered
bed forward of the projecting bay and existing fence, beside the stairs. All fit
within the existing registered footprints. Original IDs and player schema remain.

This supersedes the earlier full-shrub Stepped installation and green infill budgets.
The three legacy Stepped garden editor menus delegate to the unified installer.
Immutable building and botanical library master kits remain unchanged. Private
generated meshes, materials and prefab edits remain in the ignored native art tree.

Actual Lot Editor Near-day, Medium-day and Near-night checks passed for all five:
footprint vertices, paint exclusions, material validity, colliders and ID roundtrip.
Evidence and current native triangle totals are in the ple outputs/Townhouse-Mini-Gardens folder.


### Front garden iron enclosures (November 26 follow-up)

Green Cornice Bay, Sandstone and Brick Townhouse 2 now contain the front planting
inside iron fencing. TownhouseGardenFenceSetup reuses Brick Townhouse 2's approved
post, collar, shoe, finial, picket, arch and rail meshes, extracted read-only from its
v003/Near.blend source. IronFenceSource.json is private generated component data;
Iron.mat is a garden-only clone of its native iron material. Picket spacing and
profile are retained, and only straight rail lengths adapt to measured bed sizes.
One combined enclosure renderer is generated per native tier.

Green Cornice and Sandstone receive closed borders around their existing beds.
Brick Townhouse 2's former front iron panel is removed by bounded iron-only
triangle selection; matching side rails extend to a new front panel around its
planting. The original stonework and stair access remain. Planting is unchanged.
Limestone and Stepped prefabs are byte-identical to the preceding garden pass.
Both Near and Medium include the matching enclosure. The added Near geometry is
5,936 triangles for Green Cornice and 8,064 for Sandstone. These are detailed reused
parts, not a claim of a new simplified Far tier. Current complete model counts are
recorded in the follow-up Checks.txt.

Twenty isolated actual Lot Editor views/checks passed (five townhouses across
Near-day, Medium-day, Near-night and the opposite front corner), including full
plant-vertex containment in the three enclosures, reserved footprints, materials,
paint exclusions, no added colliders and stable IDs. The six affected native
prefabs and their generated dependencies were byte-verified against the tested
copies after installation. Evidence: ple outputs/Townhouse-Fenced-Gardens.


### Brick Townhouse 2 open garden extension

Brick Townhouse 2 garden extension: installed November 26.

The front soil/green bed is now an L shape: the main planting reaches toward the
stair cheek and bay foundation, and a green-only return fills the narrow recess
beside the stairs. The two white flower accents remain in the main bed.
Removed the stair-side garden iron run and its detached rear post. Retained only
the street-facing rail and outer side extension; the existing original outer
fence joins at the rear. Removed the old low stone front divider so planting
reads as one area. New stone edging stays on the street and outer sides only.
Sixteen 120-triangle leafy clumps provide the expanded coverage.

Actual saved/reloaded native Lot Editor Near-day, Medium-day, Near-night and the
opposite front corner passed footprint, plant-envelope, paint exclusion, material,
collider and stable ID checks. Both front corners were visually reviewed after a
cold reload (the in-process mesh render buffers initially showed the previous
geometry despite updated saved mesh data). No normal player saves were written.
Both native prefabs and generated dependencies were installed and byte-verified.
Other four townhouse prefab pairs remain byte-identical. Immutable library kits
and shared botanical component sources were not changed.


### Supplied brick-and-mortar texture finishes

Six supplied brick-and-mortar finishes are installed for Green Cornice Bay, Brick Townhouse 2 and Stepped Brick Townhouse, Near and Medium.

Choices: Blue-gray, Yellow (supplied golden texture), Beige, White, Colonial red, Slate blue. Original red remains a separate unchanged default. Old blue/yellow/sand/white IDs retain their selection identity and now use the supplied images. Red is an explicit supplied finish; empty still means Original.

Original albedo PNGs were copied byte-for-byte. They are shared between the three families. Wall UV density was measured from actual native mesh UV edges: Green Cornice/Brick2 1.8 x 1.2 metres per UV tile; Stepped 1 x 1 metre. Per-finish transforms fit the supplied brick courses to approximately 75mm height. Matching subtle normal derivatives replace the old mismatched normal maps; finish surfaces use a uniform matte, nonmetallic response. The source pictures include their own visible lighting; these derivatives are not a calibrated scanned PBR material set.

Original restores the effective original textures, UV mapping, source mean/calming maps, normal strength, PBR map switch and surface controls on the same instance. Unity texture defaults are used where the source map was unassigned. Windows, trim, signs, gardens and lighting retain their material bindings. No geometry or polygon counts changed. Player selections remain ID-based; no texture references enter saved lots.

53 isolated native cases passed: all six plus Original on both tiers of all three families; blue day/night/opposite-front samples; original restoration after a finish; save/load JSON roundtrip; and actual Building Look buttons clicked with their bounds checked; unchanged eight-color palettes and bounds verified for Limestone and Sandstone. Tested source, six native prefabs and shared maps were installed and byte-verified. Main live Unity was left to import normally; no player saves were written.

Reusable private package: SharedArchitecture/Materials/cf-material-user-weathered-brick-finishes-001/v001 in CityForgeAssets. Existing immutable architectural/brick master packages remain unchanged.

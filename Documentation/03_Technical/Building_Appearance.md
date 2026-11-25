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

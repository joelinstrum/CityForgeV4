# Growable building rows

Building Row is a layout mode of a normal growable lot. The author saves an empty
buildable rectangle, a frontage and an eligible building pool. Preview instances
are transient; the template's object list never receives them. Existing single
building layouts retain their default behavior.

## Authoring

Open Growable layout and choose Building row. Set use, density, wealth and era as
usual. Width/depth and center define the buildable footprint; the chosen road edge
sets its orientation. The gold line is where bottom front steps meet the sidewalk.
By default it follows that footprint's front edge. Place entry marker moves this
line inward/outward within the footprint (the marker's perpendicular coordinate
sets the line; its lateral coordinate does not constrain each doorway).

Use Eligible buildings to choose the pool. A row needs room for at least two
buildings; incompatible era/zone/wealth and depth/width failures remain excluded.
Row gaps are0–4m;0 means joining walls touch. Rows always face the road, retain
original model scale, and do not generate individual front paths across their
shared frontage. Single-building-only facing/path controls are disabled in row
mode. Commercial1–3 use the same eligibility/frontage/growth pipeline and stored
wealth, requiring actual matching content. Founders/Industrial low-wealth R3
remains unavailable.

Preview Row/Hide Row toggles a temporary arrangement; arrows show other possible
arrangements. Preview layouts illustrate possibilities, not a promise that every
site will receive that exact sequence. Sites use deterministic seeds, and actual
constructed objects (IDs, positions, yaw and assets) are saved without re-packing
on reload. A selected set of manually placed buildings can be converted using
Use selected buildings as footprint; applying removes just those selected models
within the normal undoable editor edit. Save as a growable lot normally. Existing
zoning parcel dimensions still apply: use Auto zoning or a matching fixed parcel.

Building info / Row alignment exposes left/right wall X and front-step Z in metres
with the model rotated to face South. The supplied eight R3 homes have measured
catalog defaults, extracted from wall-height geometry and low horizontal step
ledges without changing art. Per-building overrides persist in the existing private
BuildingInfo files and survive subsequent doorway-info saves. Other models fall
back to bounding dimensions until calibrated; roof overhangs and props can make
those defaults conservative.

## Packing and runtime

LotRowBuildings uses a bounded deterministic packing search (2.5cm quantization,
up to64 candidates/buildings). It maximizes occupied frontage while penalizing
adjacent repeats and excessive reuse. Wall widths set spacing; front-step depths
align entrances. The fitter reserves outer envelope overhang space and validates
lot bounds/fixed buildings. It never scales models to consume a leftover gap.
Small residual space is split at the ends. Insufficient room returns an explicit
message; it does not fall back to a single building or fabricate an asset.

LotGrowableBuildings.Materialize creates all generatedRowBuilding objects in the
ordinary LotDefinition. Normal and diagonal construction supply site-specific
seeds; Change Lot uses the same branch. Change row replaces all generated houses
while retaining authored objects; its rowSeed increments. The existing construction
veil hides/reveals the whole lot and measures the full row's height. Protected
player landscaping validates actual built objects without re-packing a template.

New optional layout fields are buildingRow,rowGap,rowSeed; missing values preserve
legacy single-building layouts. Lot objects carry generatedRowBuilding for whole
row replacement. Deterministic generated IDs remain valid32-digit GUID strings.
No new region schema, runtime dependency, permanent preview buildings, terrain
flattening, curved frontage or corner-building solver is introduced. Begin with
straight frontage and reasonably level ground.

## Verification

RowLotsQA in the isolated QA project covers deterministic layouts and site variety,
measured joins/front steps, unscaled models, all four road orientations, explicit
gaps, insufficient room, commercial pools, exact materialized JSON roundtrip,
empty saved templates, region object-ID validation, actual district presentation,
whole-lot construction reveal, Change row, selected-building conversion, protected
player landscaping and the existing residential parcel/growth checks. Rendered
editor mode and row previews are inspected. No player lot/region saves are written.

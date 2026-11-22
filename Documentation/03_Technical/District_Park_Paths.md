# District park paths

Parks → Paths shares the lot editor path materials, widths, curved-angle geometry, freehand marker interaction and four presets. Freehand uses PointWireTool: Enter finishes an open path, clicking the first marker creates a loop, Backspace undoes a marker, Escape cancels the sketch. Presets place around one clicked center.

DistrictParkPathRecord stores a world-coordinate LotPathDefinition and original district ID in the additive SavedRegionInfo.parkPaths array. Missing arrays in older maps mean no district paths. Capture, open and binary region save validation retain the records. Validation caps paths at 512 and uses road guide bounds/count checks. Player lot-library files are not changed by placing district paths.

Paths avoid physical buildings and wet ground; empty lot ground and sidewalk overlap are allowed. StreetSurfaceMask fades/clips their surface under street sidewalks. RefreshDistrictSidewalks refreshes this presentation after road edits. Area demolition cuts crossing paths into surviving outside pieces, as it does roads.

RebuildPedestrianRoutes registers both district park paths and pedestrian paths authored inside saved lots as preferred sidewalk routes, with their actual ribbon width. Vehicle routes continue to use roads only. Building clearance still applies to pedestrian movement.

2026-11-20 QA: actual saved C2 medium-wealth 4x4/8x4 layouts and rows, cardinal stamp/save restoration and physical street frontage; unbounded spray and building-mask/input ownership; park creation, walking surface registration and binary save/load; UI and day/7pm horizon shader renders. See current chat outputs/District-Refinements. QA uses disposable scenes/saves; no player region is written.

## Pedestrian path modal and sunken borders — 2026-11-21

The in-game path editor now uses PedestrianPathModal, with the supplied town banner, surface thumbnails/search, border textures and side placement, widths1/2/3/6/9/12m, feathering, texture facing, curved/straight corners and existing PointWire shapes. Selected paths use the same dialog for appearance edits; existing domain undo remains authoritative. Lot Editor selected-path materials/borders use the same appearance controls with its own session undo. Search fields keep text-edit undo; Ctrl/Cmd Z in the Lot Editor appearance modal invokes session undo.

LotPathBorder keeps None=0/Curb=1/Brick=2, then appends Fieldstone, Timber, Limestone and Earth. LotPathBorderSides is additive Both=0/Left=1/Right=2; left/right follow the original drawing direction. Older paths retain their flat/both-side defaults. Flat paths never draw borders. Depth controls expose the existing0.1524m/0.3048m visual recess, sharing LotPathBlend with the lot editor rather than excavating authoritative terrain. The six supplied private textures load from Lots/PathBorders. Border rims and exposed side lining share the chosen material. Street road materials explicitly retain legacy curb treatment.

New depth/lining/sides survive lot JSON and binary region metadata round trips. Existing clipping, preferred pedestrian routes, path selection, material changes, area demolition and create/edit undo remain in their existing consumers. Tested in a disposable copy of actual Candlewick, including both terrain placement choices before construction, path create/edit/save/load/undo and Lot Editor appearance application/closure.

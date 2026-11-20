# District park paths

Parks → Paths shares the lot editor path materials, widths, curved-angle geometry, freehand marker interaction and four presets. Freehand uses PointWireTool: Enter finishes an open path, clicking the first marker creates a loop, Backspace undoes a marker, Escape cancels the sketch. Presets place around one clicked center.

DistrictParkPathRecord stores a world-coordinate LotPathDefinition and original district ID in the additive SavedRegionInfo.parkPaths array. Missing arrays in older maps mean no district paths. Capture, open and binary region save validation retain the records. Validation caps paths at 512 and uses road guide bounds/count checks. Player lot-library files are not changed by placing district paths.

Paths avoid physical buildings and wet ground; empty lot ground and sidewalk overlap are allowed. StreetSurfaceMask fades/clips their surface under street sidewalks. RefreshDistrictSidewalks refreshes this presentation after road edits. Area demolition cuts crossing paths into surviving outside pieces, as it does roads.

RebuildPedestrianRoutes registers both district park paths and pedestrian paths authored inside saved lots as preferred sidewalk routes, with their actual ribbon width. Vehicle routes continue to use roads only. Building clearance still applies to pedestrian movement.

2026-11-20 QA: actual saved C2 medium-wealth 4x4/8x4 layouts and rows, cardinal stamp/save restoration and physical street frontage; unbounded spray and building-mask/input ownership; park creation, walking surface registration and binary save/load; UI and day/7pm horizon shader renders. See current chat outputs/District-Refinements. QA uses disposable scenes/saves; no player region is written.

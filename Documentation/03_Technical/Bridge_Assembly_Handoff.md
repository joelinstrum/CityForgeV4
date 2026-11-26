Implementation status update, November 23, 2026: the first functional prototype is
now installed. Read Bridge_Assembly.md and the latest HANDOFF.md for current behavior
and verification. The following is the original pre-implementation design handoff.

# CityForge V4 — era content checkpoint and bridge assembly handoff

## Start here

Primary project: `/Users/joelinstrum/dev/CityForge - V4`.
Branch: `feature/era-content-profiles`.
Unity: 6000.1.12f1. Read AGENTS.md and the latest HANDOFF.md first.
This checkpoint commits source and configuration only. Private models, artwork,
textures, generated LocalCatalog and saved regions stay local and ignored.
For a new chat using the same project, those private assets are already installed.
A new checkout on another machine requires the private assets as well as Git source.
Do not overwrite the generated LocalCatalog from a different project.

## Completed and installed

- Era appearance profiles: district appearance overrides, lot-specific prop policy
  and paving overrides, while preserving the simulation calendar and existing saves.
- Large, consistent Buildings, Trees, Fences, Landscaping and Characters galleries.
  Shared brighter thumbnail rendering, search, favorites, pagination and double-click
  selection. Buildings supports density, wealth and multi-era availability.
- Building gallery now starts from the lot's first era and category. LOT ERA at top
  is read-only context. FILTER BY ERA below Favorites changes visible assets only.
  Manual filter choices persist for the same lot context; switching lot/era resets
  defaults. Removed "Not used since tracking began" from shared galleries.
- Art Deco imports: 12 R2 medium-wealth homes, six C2 medium-wealth commercial models,
  and the Art Deco City Center building asset. Retain authored/private LODs and lighting.
- Art Deco start popup: existing welcome frame, supplied harbor illustration, text
  in ContentCatalog.json, era-specific persistent acknowledgment. Startup selects
  the recipe's era rather than always Founder.
- Modern street paving: concrete/asphalt plus retained brick; opt-in automatic street
  planting. New path materials have first-era rules, excluding them from older eras.
- City tree collection: four sizes × with/without planter = eight gallery entries,
  each with supplied summer, autumn and bare winter models and reduced distant meshes.
  Small/medium variants are automatic street presets. Planters omit extra soil pits.
- Eight Art Deco NPCs: Characters → Art Deco Era. R2/R3/C2/C3, all wealth levels.
  People per building: 2/3/4/6 respectively, so commercial is 2× matching residential
  density. Staggered 40-second active-simulation cycles: 30 walking, 10 idle, where
  safe routes exist. Black/tan suits lacked clips and use humanoid-retargeted clips
  from the supplied brown suit. Existing actor identities survive unrelated refreshes.
- Civic lots now determine town-center function. Author Civics → Government, choose
  Lot role… → Town Center, configure first era/requirements, then save. Founding and
  Civics menus select saved lots; no generated timber or Art Deco fallback lot.
  Art Deco building identity alone no longer implies Town Center. Explicit None is
  respected. Existing placed centers with explicit roles and legacy Founder-category
  lots remain compatible. No saved user lots were rewritten.

Earlier gameplay/terrain fixes (sidewalk zoning, warmer persistent night lighting,
lenient path completion, save-district staying in place, Manhattan1609 import) are
already recorded in HANDOFF.md and the prior commit f7dfccb. Preserve them.

## Important files and private assets

- `Scripts/UI/LotEditorBuildingGallery.cs`: shared galleries, current era filter.
- `Scripts/World/CivicBuildingChoices.cs`, `DistrictTownCenter.cs`,
  `FounderPlacement.cs`, `TownCivicsPopup.cs`: saved civic lot selection and roles.
- `Scripts/Lots/EraAppearance.cs`, `Config/EraAppearanceProfiles.json`: era policy.
- `Config/ArtDecoCharacters.json`, `Config/PedestrianProfiles.json`:
  NPC import metadata and editable era/zone/activity rules.
- `Scripts/Editor/ArtDecoCharacterPreparation.cs`, `CityTreePreparation.cs`:
  private asset preparation and registration; LotCatalogSetup builds LocalCatalog.
- `Scripts/World/DistrictPlacedActors.cs`, `EraPedestrianProfile.cs`: traffic rules.
- `Scripts/World/StreetTreePalette.cs`, `DistrictStreetTrees.cs`: street planting.
- Private NPC runtime assets: `Assets/CityForge/Resources/Lots/ArtDecoCharacters/`.
  Originals: `SourceAssets/ArtDecoCharacters/`.
- Private tree runtime assets: `Assets/CityForge/Resources/Lots/Trees/CityPack/`.
  Originals: `SourceAssets/CityTrees/`.
- Other art/LOD locations and evidence: corresponding Documentation/03_Technical
  notes and this chat's sibling outputs folders under `be/outputs`.

## Verification checkpoint

Native isolated Unity checks and inspected screenshots cover the galleries/imports,
era restrictions, lighting/LOD behavior and source-independent saved terrain.
Recent suites: 216 city-tree/welcome checks, 109 NPC checks, 14 civic-role checks,
and seven building-era-filter checks. Last primary compile succeeded in 5.49s.
Evidence folders include ArtDeco-Characters, ArtDeco-Welcome-City-Trees,
Civic-Lot-Rules and Building-Era-Filter. These are targeted checks, not a whole-city
bridge or traffic performance benchmark. Temporary QA scripts remain outside the
production commit; isolated project is under
`/Users/joelinstrum/Documents/Codex/2026-11-15/we/work/gazebos/CityForgeQA`.

## Next task: bridge assembly prototype — agreed design, NOT implemented

The user wants configurable assembled bridges, not complete fixed bridge models and
not lots. Place them under Transport → Bridges with their own in-game designer.
Built-in styles and future modded styles should use the same versioned JSON format.

First prototype: a trestle-style bridge for an Art Deco-era crossing, using only
plain shapes and flat colors. No textures, decorative art, final meshes or polished
modal required. Timber trestle is the proposed first structural style; final material
and additional styles are later work, not a prerequisite for functional geometry.

Interaction:
1. Click a starting road/bank and drag across water, consistent with road construction.
2. Preview deck, inclined approaches and ghost support/cross-bracing geometry.
   Preview proposed far-bank grading, without modifying terrain yet.
3. Release to open a minimal designer; adjust deck height, width and style preview.
4. Confirm to create the bridge and apply bounded approach earthwork. Cancel leaves
   terrain and roads unchanged. Do not commit earthwork during dragging.
5. Reopen an existing bridge to edit its saved construction settings.

Separate these concepts:
- Bridge STYLE definition: stable ID/schema version, era availability, supported
  widths, span/support-spacing/slope/clearance rules, and modular part references
  (deck, railings, trestle bents/piers and braces; optional arches/towers later).
- Bridge INSTANCE saved with the region: stable ID, style ID, alignment, deck heights,
  width/lane/sidewalk settings, approach connections, support positions and earthwork
  settings needed for deterministic reconstruction. No dependency on original import
  files at runtime; normal referenced installed style assets remain required.
- Bridge DESIGNER: ghost preview, adjustment, validation and confirmation UI.

Prototype priorities:
- Straight fixed bridges with a level elevated deck and sloped road approaches.
- Simple deck/rail/support primitives assembled repeatedly along the alignment.
- Supports sampled to actual bed/ground; water is not the foundation elevation.
- Correct road topology, elevated driving/walking surfaces, and safe bank transitions.
  Current road/route assumptions may be planar: inspect rather than assuming a mesh
  alone gives vehicles/pedestrians a navigable elevated crossing.
- Width and clearance validation. Keep piers out of navigable channels; boats need
  actual under-deck clearance, not just visual intersection avoidance.
- Region save/reload, editing, cancellation, demolition and compatibility with existing
  roads and saves. Do not regenerate or flatten unrelated terrain or water bodies.
- Explicit limits initially: no curved, movable, suspension or multi-level interchanges.
- JSON validation gives actionable missing-part/unsupported-dimension errors. A missing
  mod must preserve instance data and flag repair, not silently delete the crossing.

These are architecture/acceptance targets, not claims that they already work.
Choose the smallest complete vertical slice before expanding structural styles.

## Integration guidance for the next session

Read the cityforge-pointwire skill before extending marker/string interactions;
AGENTS.md requires PointWirePath/PointWireSurface/PointWireTool and PointWireGeometry
for reusable construction guides. User's preferred bridge gesture is road-like drag
and release: preserve that interaction while reusing applicable infrastructure.
Inspect DistrictRoads, StreetJunctions, DistrictRoadContext, TownRoadsPopup,
SavedRegionStore, EstablishedRoute and pedestrian/freight elevation handling.
Bridge styles should not be forced into LotDefinition/LotLibrary merely to reuse UI.
Keep the existing flat Region Creation preview and procedural terrain compatibility.
Do not alter approved forest/ocean/shoreline rendering to implement bridges.

Do not interrupt a new unsaved Unity Play session for a compile without checking.
Prior discard approvals applied to earlier sessions, not all future work.
No bridge asset creation or bridge implementation has started at this checkpoint.

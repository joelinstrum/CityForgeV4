# Native settlements

Accepted local implementation, November 1, 2026. Geography remains recipe12; settlement content is independently versioned.

## Creation and persistence

The new-region wizard sets `nativeCampVersion=1`. The existing worker generation calls NativeCampPlanner after geography/biomes. Its independent seeded random stream targets round(original district count ×0.20) unique cells (13 of64 by default). It prefers naturally dry sites within120m of water; unsuitable/water-only terrain can leave the target unmet. No water or terrain is reshaped for camps.

NativeCampRecord persists original cell ID, center, heading, seed,5–9 teepees, nearby-water point and preference result. Metadata validation rejects invalid counts, duplicate IDs, nonfinite values and footprints outside their original cells. Missing/zero content version in older recipes produces no new settlements; opening a save never backfills camps. Saved records remain authoritative. Combined districts filter by ActiveDistrictMembers, preserving original ownership and excluded L-shaped corners.

Individual NPC positions/activities are transient and restart when the district loads. This is ambient content, without economic simulation, combat, diplomacy or a settlement editor.

## Layout and residents

Five through nine inward-facing teepees occupy a260-degree horseshoe, opening toward water where available. Radius grows10.5–15.3m with teepee count. A5m totem stands centrally; its neutral wrapper preserves the imported model child's up-axis conversion. A campfire sits4.3m toward the opening. Buildings use sampled ground; native trees leave buffered clearings. Actual Camp markers on the overview open the relevant district.

Total population is teepees+3, including one chief. Half of the remaining count, rounded down, uses the warrior model; the rest uses the female resident model. A nine-teepee camp has one chief, five warriors and six female residents. These are supplied model roles, rather than a demographic or historical reconstruction.

Residents share NpcBrain/actions/profiles for idle, walking, wandering and paired conversations. A65×65 one-metre grid samples terrain, tree trunks and conservative structure/fire obstacles, retaining one connected component. The circular leash is radius+7m. Water, steep ground and occupied points are rejected. Movement selects the actual Walk animation immediately; blocked/stopped actors idle. Paired conversations use fold_arms where available and Idle for the warrior. Zoom0–2 presents actors; farther views pause/hide them. See Npc_Ambient_Study.md for action/state architecture.

## Ground and fire

The courtyard uses a runtime dirt TerrainLayer with irregular feathered coverage and entrance paths. Inner coverage is94–99.5% dirt. The canonical pre-camp map is saved, so derived painting never accumulates or adds layers to a save cache. Earlier lake canonical maps take precedence when present. Heights/water remain unchanged.

SharedDirtSurface uses optional private Resources/NativeCamps/Courtyard-Dirt.png at2.5m tiling for courtyard/base soil, generated riverbank soil, dedicated stream/river soil and wet lake soil. Wet variants use a darker neutral tint. Sand, gravel, rock and grass retain separate textures. TerrainLayers are runtime copies; source assets are unchanged. Without the supplied PNG, the earlier procedural courtyard fallback remains.

NativeCampGrass draws up to64 seeded short clumps from existing meadow plant types0/1 around worn margins. It avoids the bare core and expanded structure/fire bounds, uses shared meshes/materials in instanced batches, and renders only at Zoom0–2. Existing streamed groundcover still excludes camp clearings.

NativeCampfire reuses12 non-moss river stones scaled0.43–0.56m wide, with five procedural charred logs. Bounded flame, ember and widening smoke particles use owned runtime materials and a source shader; smoke emits7/sec with up to7sec lifetime and gentle drift/turbulence. A5m warm point light flickers without shadow rendering. Effects/light stop at Zoom3+, while static logs/stones remain. There is no audio, seating or dedicated gathering behavior yet.

## Private artwork and reproduction

The public repository is source-only. Source downloads, FBXs, textures, materials, controllers, definitions, prefabs and saved regions are excluded. Local input archives are in Downloads/indian stuff; the extensionless totem is an FBX. Its1,417,346-vertex original remains untouched; the prepared sculpt has about15,002 vertices/30,000 triangles before Unity splits. Supplied vertex colors are effectively white, so the runtime material uses a matte wood tint.

1. Set CITYFORGE_PROJECT and run Tools/Npc/prepare_npc_study.py with the download directory; prepare the resident through CityForge > NPC > Prepare local resident.
2. Run Tools/Camps/prepare_native_camp_sources.py with the same directory. An optional second argument supplies the dirt PNG path. Different existing files are never overwritten.
3. Run Tools/Camps/prepare_camp_models.py and bake_totem_colors.py in Blender batch mode with CITYFORGE_PROJECT. Generated output guards prevent replacement.
4. In Unity Edit mode run CityForge > NPC > Prepare native camp assets. The builder refuses an existing prepared asset set. Use Configure local dirt texture when adding a PNG later: sRGB/Repeat/Trilinear/aniso8/mipmaps/max2048/NPOT None/high-quality compression/no alpha. Alpha must not introduce glossy terrain.

Missing required local camp assets raises an explicit preparation error when entering a populated district. Source alone does not reproduce licensed artwork.

## Persistent rendering

CityForge_URP stores the approved4096 main-light atlas with four cascades and splits(.50,.68,.85). Directional sun shadows remain Hard through Summer/Fall/Winter refreshes; seasonal shadow strength is retained. Camera shadow-distance fitting and navigation quality throttling remain active. No live-preview clone is needed. Four cascades add rendering work; this is visually accepted, without a standalone hardware performance qualification.

The three closest zoom levels0–2 use half their previous visible width/height;3–5 retain their previous spans. Explicit saved camera poses retain their saved size until a new zoom action.

## Verification

- NativeCampChecks.Unit: deterministic metadata/count/spacing, water preference, invalid water-only sites, legacy defaults, save roundtrip and unchanged geography with the content flag.
- NativeCampChecks.Run: normal new-region worker generation, unique temporary saved fixture, overview markers, normal saved reload/district entry, upright totem, character mix, tree clearing and30-second safe movement. Successful runs remove only their own unique fixture.
- NativeCampRefinementChecks.Run: existing local trial native-camp-study-2026-11-01/district28, closer layout, dirt weights/cache exclusion, meadow tufts/shared soil texture, character mix, persistent source shadow settings and seasonal refresh, fire emission/zoom policy and safe movement.
- NpcLocomotionChecks.Run: loaded camp's actual movement versus real Animator state and walk clip for all three appearances across close zoom, far hide/show and a forced idle reset.

Existing user save files are hash-checked separately. These checks require locally prepared art and a running Editor; no whole-city/standalone performance claim is implied.

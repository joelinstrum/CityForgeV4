# Optional district wildlife

October 20, 2026. Animal Pack Deluxe and Living Birds are integrated through a local, curated resource library. Licensed models, animation clips, textures, materials, controllers and derived prefabs remain ignored by Git. The public repository contains CityForge integration code only. Rebuild locally with **CityForge → Animals → Build Local Temperate Wildlife**, or `Library/WildlifeImport.request` while stopped.

## Region controls and persistence

The new-region terrain step has **Animals: None / Temperate wildlife**. Wildlife is initially selected in the wizard. The recipe records the creation choice; saved metadata records the current enabled/disabled state. Existing saves default to off. The district **ANIMALS: ON/OFF** button supports trying wildlife in existing regions; it applies across that region's districts and persists only when the user saves normally. No player save is regenerated or rewritten by importing or previewing animals. Turning wildlife off unloads/deactivates the active population immediately. Returning to overview unloads the district population; no animals are drawn in the regional relief view.

Animals render and animate only at close camera levels0–2; levels3–5 pause wandering and hide renderers, including shadows. The toggle shows the total district population, and FIND ANIMAL centers a land animal at zoom1 without changing the region setting. The population list survives Unity script reloads; trunk clearance indexing is rebuilt after reload.

Population and initial positions derive from region seed, district ID and habitat suitability. Re-entering a district restarts its ambient animals from those positions; individual animal movement, health, breeding, ownership and population history are not saved. Farm animals need actual farm/pasture data before placement; settlement and farm markers are not invented.

## Temperate habitat policy

| Species | Initial placement and movement |
| --- | --- |
| Deer, wild boar | Dry woodland, outside roads and tree trunks, moderate slopes |
| Wild rabbit | Woodland edges / meadow margins |
| Wolf, brown bear | Woodland; at most one of each per district; no combat or hunting |
| Common frog | Dry ground near a semantic freshwater bank; idle animation |
| Perch, pike | Typed rivers, lakes or ponds, with enough actual terrain depth |
| Salmon | Rivers only |
| Crab | User-requested river banks and shallow riverbeds: within6m of shoreline, at most0.65m underwater or0.9m above water, slope<=30°, outside roads |
| Sparrow, chickadee, robin, goldfinch, cardinal, blue jay, crow | Ground birds in woodland openings/edges; separate flight variants above local terrain/canopy |

Cattle, chickens, goats, domestic pigs, crocodiles, sharks, scorpions and other unselected animals are excluded from the automatic library. Fish swim0.22m below the surface. Generated river rendering samples scene depth so near-surface fish and shallow crabs are not obscured by absorption calculated all the way to the deep bed. FIND RIVER LIFE focuses the closest bank animal/fish at close zoom.

Fish require a freshwater semantic body as well as sufficient bed/surface clearance; blue water alone never qualifies. Oceans do not receive freshwater animals. Terrain height, roads, shoreline, slope and forest density constrain land placements; mature tree trunks receive a2m clearance. Frogs require a freshwater body nearby, preventing placement along an ocean-only coast.

Land population targets one animal per12,000m², clamped to12–96 attempts per district, plus24–80 fish attempts (one per15,000m²) and24 crab attempts. Living Birds adds one bird attempt per5,000m², clamped to24–220; seven species have ground and flying variants (crows less common). Ground birds require sparse canopy; flight stays32m above the sampled terrain. Birds use the same regional option and close-zoom visibility policy. No squirrel model is present in the installed packages. Unsuitable habitat yields fewer animals rather than forcing spawns. Land animals alternate idle and short local wandering; fish swim. Every movement step is checked against habitat and district bounds. Source pack demo controllers that cycle attacks/death have been replaced with local looping idle/walk controllers; no autonomous root motion, colliders or vendor demo scripts are used. Animators cull offscreen transform updates. Tree proximity uses a spatial index. No navmesh, combat, farming or full ecological simulation is introduced.

## Local assets and verification

`WildlifeImporter` makes URP material copies, safe looping animation/controller copies and sensibly scaled wrappers. Source artwork remains unchanged. Unity's API updater migrated the two imported Living Birds behaviour scripts for Unity6; runtime clones remove vendor behaviours and use the bounded district controller. The generated `Resources/Wildlife/TemperateWildlife` library supplies the runtime. The lightweight availability marker avoids loading animal models merely to display the disabled toggle. Missing local assets show an unavailable control and a warning if an enabled region attempts to populate.

Write `Library/Wildlife.request` to run `WildlifeChecks`; reports and screenshots go to `Library/WildlifeChecks/`. Checks cover the species allowlist, bird abundance/ground+flight presence, textured URP materials, looping safe animation states, wizard choices, legacy default-off, overview isolation, habitat placement, deterministic re-enable, advancing animation and15 seconds of bounded wandering, temporary on/off save round trips and opt-in for the original meadow test save. Player save hashes are audited separately. The forest preview remains unsaved for user visual review.

Legacy loading also explicitly retains `creation = null` when the recipe field was absent/null in old JSON. Unity's missing nested-object initialization otherwise supplies current recipe defaults, incorrectly changing the old grid/vertical-water interpretation. Existing stored heightfields are retained verbatim.

Latest expanded-population checks passed: the seed730421 woodland trial contained210 birds and85 other animals, including ground and flight variants of all seven birds. Zoom0/1/2 render and animate; zoom3/5 renderer checks passed. Player save hashes remained unchanged. Local rebuilds preserve animator-controller GUIDs and strip source animation events that require vendor scripts.

River-life verification passed with70 fish (37 perch,15 pike,18 salmon) and24 crabs in the temporary river district. Fifteen seconds of habitat checks and actual crab crawling passed. Isolated URP camera comparison confirms submerged fish contribute visible pixels through the river surface. The river trial is left unsaved with animals enabled and close shoreline framing. Player save hashes remain unchanged.

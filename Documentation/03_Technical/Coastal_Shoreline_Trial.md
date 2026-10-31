# Coastal shoreline trial — 2026-10-30

The new local `coastal-shoreline-test` save is the sole active temporary test region. Five `pan-test-*` saves were removed through SavedRegionStore.Delete, retaining recoverable files under Regions/Deleted. The original Meadow Test Valley remains unchanged. This new test uses the existing 1,024 m northeast coastal district, 50% mature/native and authored tree populations, summer, animals and optional small plants off. The approved forest and navigation optimization remain intact.

## First swash candidate

Only `Coastline && CoastVersion >= 3` opts in; the original coastVersion2 ocean and generic saved oceans retain their prior setup. The trial replaces the old incoming foam-strip input with an authored shallow wave packet and synchronized foam input. Both feed the existing Crest renderer rather than drawing a visible overlay. Required Crest input shader category names are used; the code and shaders are CityForge-authored, with no copied commercial package source.

A fixed coastline strip has 16,929 vertices sampled once against terrain, spanning40 m seaward to20 m inland. GPU phase shifts vary along the coast. The8-second packet advances then recedes, adding at most0.44 m vertical displacement. Foam is restricted by estimated ground clearance and broken along the coast. This is an artistic wave input, not a fluid or sediment simulation. Native offshore waves, reflections, ocean lighting/material palette and terrain heights remain unchanged. Two material time values update per frame; mesh/terrain data are not rebuilt during animation.

The coastal exclusion mask permits the low beach below seaLevel+2 m, leaving physical terrain/wave intersection to define the advancing surface instead of imposing the old12 m inland mask. The district perimeter remains bounded. Uniform shoreline foam emission is disabled in this trial, wave-generated foam strength is0.25, and foam fade rate2.5. The authored edge input currently uses emission gain4; visible foam is still restrained. No wet-sand material response has been implemented yet.

## Verification and limits

Both shaders compile; fresh runtime input registration and hidden input-renderer checks pass. Motion captures at zoom1 cover more than one8-second packet cycle. Initial capture revealed an incorrect shader-category registration; that capture was discarded, shader names corrected and a fresh load validated. Runtime input renderers must remain forceRenderingOff so displacement colors cannot leak into the normal camera.

This first candidate removes the broad white stripe and introduces a moving shallow ridge; wave shape, edge foam and subsequent wet-sand appearance still require user visual assessment. It is not promoted to other coasts. Standalone performance qualification against the previous coast is pending; prior panning improvements are not evidence of this new input's cost. Source commit1423c12 remains the last accepted environment baseline. HUD cleanup from the preceding request remains in place.

## Visual acceptance

The user approved this first coastal swash appearance on2026-10-30 and requested commit/push/PR. Preserve this version as the accepted starting point for subsequent shore refinement. Wet-sand response and a standalone cost comparison remain future work; visual approval does not qualify performance.

## Tropical climate option — October 30

Climate now offers the three existing Temperate moisture choices plus Tropical. `RegionCreationSettings.climate` is independent of moisture; Tropical uses Balanced moisture and leaves the approved vegetation and terrain generation intact. Missing climate metadata defaults to Temperate. Recipes persist climate; legacy maps can store it in `SavedRegionInfo.climate` without adding a recipe or changing their two-by-two grid. `SavedRegionStore.Write` explicitly retains `creation:null` for those maps because Unity otherwise materializes a default inline recipe.

Only coastal Crest materials opt into the Tropical palette, applied to the district-owned material before `CoastalWaterPresentation` captures its perspective baseline. Absorption is `(0.32,0.065,0.035)` and scattering `(0.035,0.24,0.30)`, giving light blue/aqua water under the approved lighting. Existing Temperate materials are untouched. Waves, swash, foam, reflection settings, lighting, terrain and tree assets are unchanged; rivers/lakes/ponds retain their existing profiles. Ocean/river districts still share the coastal surface, so an estuary inherits this palette. The wizard still creates inland geography; climate does not add a coastline generator or tropical vegetation.

Unity compilation passed with zero errors/warnings. Live dropdown mapping, old-recipe Temperate default, recipe and legacy binary save round trips, legacy null-recipe preservation, unchanged Temperate material and Tropical isometric/perspective color restoration passed. The existing coastal test was rebuilt with Tropical in memory for visual inspection; original saves were not rewritten. Player-build qualification was not run for this color-only path.

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

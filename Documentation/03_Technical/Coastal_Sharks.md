# Coastal great white patrols

This first pass contains shark patrols only. The user explicitly deferred swimmers
and swimmer encounters. No visitor population, targeting, scare or flee behavior
is included.

Coastal districts with Animals enabled can host one supplied great white when the
ocean has a navigable patrol band. The private prefab is approximately five meters
long, with the complete looping swim action. CoastalOceanHabitat checks actual ocean
polygons, displayed terrain depth, active combined-district footprints and a2.8m
clearance footprint. Routes are sampled at one-meter intervals and require at least
2.2m water depth. Region sea level is authoritative; rivers, streams, lakes and dry
land do not qualify. No fixture coordinates or fixed12m datum are used.

CoastalSharkPopulation belongs to the district wildlife holder and follows the
Animals toggle. It cruises at1.6m/s and turns toward checked patrol targets. Activity
is transient and budgeted at distant views. Normal region saves, beach paint,
canonical trees and geography remain unchanged. Swimmer interactions can be designed
when the swimmer system is introduced later.

## Private assets

Preserve the supplied great_white_02.blend and its texture archive. Export the mesh,
rig and full1–117 frame action at24fps to the ignored directory:
Assets/CityForge/Resources/CoastalWildlife/GreatWhite/GreatWhite.fbx.

Put COLORMAP.png and NORMAL.jpg beneath that directory in Textures/. The local setup
also preserves the supplied gloss/reflection images. From the main menu run
CityForge / Wildlife / Prepare supplied great white. The builder creates the private
URP material, looping controller, orientation/scale wrapper and GreatWhiteShark.prefab.
Missing private art skips the patrol with an explicit warning. Do not publish the
supplied or derived meshes, textures, materials, controllers or prefabs to public Git.

## Inspection and checks

CityForge / Wildlife / Find coastal shark frames the active animal at close zoom.
CityForge / Wildlife / Check coastal shark patrol resets only transient wildlife and
checks Animals Off/On, dry/exterior rejection, contained movement and absence of
spawned swimmers. It writes Library/CoastalSharkChecks.txt without saving the region.

No standalone-player performance or full coastal/seasonal visual guarantee has been
established. Preserve the approved beach, tree and water appearance during refinement.

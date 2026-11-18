# Lumberjack workers

In the Lot Editor, place **Axeman (Lumberjack)**, select the character, and choose
**Character Script > Scripted > Lumberjack > Apply**. Save the playable camp lot.
Put each worker on open ground with a clear route out of camp. The initial role is
restricted to the local Axeman asset, which supplies Idle, Walk, and Chop states.
The editor keeps the worker at home; harvesting starts when the lot is placed in an
open district.

DistrictLumberjack claims the nearest reachable canonical forest tree within 80m of
its authored home. Ground cover is excluded. Claims are owned per landscape and
released when the actor returns, is disabled, or is destroyed. Workers walk at
1.15m/s, stop 1.2m from the trunk, chop for six seconds, let the tree fall, then return
home before seeking another. Trees fall away from the worker. Approved summer mixed
forest uses its actual visible species and transform for the falling copy.

Routes are direct segments. Unreachable trees are skipped; this is not a detouring
pathfinder. Route sampling checks district boundaries, water, sea level, abrupt
height changes, building/fence footprints, and initial tree-trunk intersections.
Workers remember sorted nearby candidates and rejected approaches between passes;
removed and claimed trees are skipped. Validated out-and-back routes are retained,
with changed local sections checked before entry. Workers retry when no reachable
tree remains. See Established_Actor_Routes.md. Place workers outside structures and
leave exits through fences. Temporary actor activity and claims are not saved.

Harvesting calls the existing RemoveForest path with a small trunk-base mask, so
normal district/region saves preserve tree removal and prevent regrowth from caches.
It respects save/loading guards and the existing forest-edit limit. Existing dock
worker/shopkeeper serialized enum values are preserved.

Successful felling credits one timber unit to the placed camp lot. Assignable vehicle
freight consumes that stock; see Lumber_Wagon_Scripts.md. Log collection/carry
animations and construction resource spending remain pending.

Editor-only check entry point: CityForge.Editor.LumberjackChecks.Run, intended for
batch mode in an isolated local QA project. It checks exclusive claims, ownership,
release, leash, unreachable targets, trunk safety, settings serialization and local
Axeman animation states. Live district animation/visual acceptance remains needed.

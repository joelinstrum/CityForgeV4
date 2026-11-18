# Established actor routes

Workers, scripted delivery wagons and street traffic keep their waypoint routes. An unrelated world edit does not trigger a global route search or recreate existing ambient actors.

`EstablishedRoute` certifies an initially validated path against a `NavigationState` containing lot presentation/count, road and woodland revisions, canonical tree-array identity, water-array identity and district landscape identity. Unchanged movement performs no clearance probes or route allocations. Camera movement and elapsed time do not invalidate routes.

After a mutation, the actor inspects only the approximately one-metre sections it is about to enter. Clear sections are remembered for that world state, in either travel direction. New blockers stop the actor before entry. Water, grade, buildings, fences and trunks remain barriers. Road and sidewalk snapshots distinguish removed travel surfaces from intentional off-road lot access. There is no repeated scan of the entire remaining path. A later edit invalidates these local certificates again.

## Delivery vehicles

A vehicle retains its last receiver and certified outbound path. It reverses the outbound geometry for the return journey and reuses that round trip on subsequent deliveries. New roads or receivers do not automatically send an established trip searching for a different destination. Replanning occurs after an encountered blocker, a missing/moved/ineligible receiver, or when no reusable route exists. Blocked attempts retain cargo and retry with the existing delay. Existing schedules, loading, unloading and corner smoothing remain authoritative.

## Axemen

Each worker remembers an ordered list of nearby tree positions within the existing capture sphere. The list comes from the spatial tree index rather than a scan of every district tree for every assignment. Trees farther down the list are retained for later harvests, with reachability checked lazily. Rejected approaches remain rejected until a relevant world mutation. Claimed and removed trees are skipped; a successful claim remains exclusive. Remembered alternatives survive canonical-array replacement when another tree is harvested. Once exhausted, a changed forest permits gathering new candidates. Empty unchanged forests do not repeatedly gather candidates.

The validated straight approach also certifies the return path. Workers inspect newly changed local sections before entry. Existing straight-line navigation remains: a worker can abandon a blocked approach and retreat, or wait at a blocked return; this change does not introduce a general detour pathfinder.

## Street traffic

Pedestrians retain their block/sidewalk waypoint arrays and progress. Refreshing layout data adds population for new lots and removes population belonging to removed lots, while preserving existing actors. Stationary characters can attach when a new route appears. Authored parked vehicles remain parked. New local blockers or removed travel surfaces stop movement and allow a bounded nearby route attachment on retry. Connectors are checked before any repositioning. Temporary vehicle avoidance remains local steering rather than a route search.

Caches are runtime-only. Opening a save reconstructs the path once from the saved job position and destination; cargo and job state retain their existing schema.

## Verification

`CityForge.Editor.EstablishedRouteChecks.Run()` covers 10,000 unchanged movements without geometry probes or per-tick allocations, distant edits, entered-section reuse, new blockers, second-edit invalidation, reverse and closed paths, removed roads and sidewalks, and existing pedestrian identity/circuit preservation. It also runs the lumberjack/freight suite, including remembered alternatives, exclusive claims, a real wagon stopped by a newly inserted tree and resumed after removal, repeated round-trip reuse, receiver fallback, schedules, cargo preservation and in-flight save/reload. Fixtures are disposable and do not write player saves.

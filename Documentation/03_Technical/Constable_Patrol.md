# Constable Patrol

## Authoring

Place the Constable character outside the entrance on a lot containing the Constable
Station model. Select the character, open Character Script → Scripted, choose Patrol,
then Apply and save the lot. That authored character position is its home station
point. A station building alone does not create a constable. Existing lots retain
all older script IDs; Patrol appends enum value5.

## Response

1. The constable walks the nearby street/road walking network.
2. A successful Harass scare registers the hooligan as a crime incident.
3. The nearest reachable available constable claims it and runs to intercept.
4. Capture stops harassment and clears that victim's fright. The constable leads
   the unarmed hooligan back, retaining a gap on their shared certified path.
5. They pause at home; the hooligan disappears inside for the evening/night and the
   constable resumes patrol. The normal hooligan returns next daytime. Temporary
   test hooligans disappear permanently and release camera follow after delivery.

The existing Hooligan harass district command exercises the same response during
daytime. It still requires a visible walking pedestrian. Place and script the
constable yourself before testing; the command never generates an officer/station.
Esc ends the temporary test and releases response/custody safely.

## Navigation and lifetime

PatrolCrimes owns per-view registries, single-owner dispatch and a NavigationState
cached PatrolStreetGraph. Walking offsets follow the existing shoulder geometry;
explicit street junction/end links allow crossings. Initial/final connectors are
limited to12m and must pass the same clearance tests as the network. Normal beat
points stay within80m of the station. Routing breaks geometric intersections and
retains original deterministic point order when path costs tie.

Graph edges cache dry-ground, structure/tree and active-district clearance until an
edit. PatrolJourney interpolates certified paths cheaply. Changed navigation stops
travel until replanning succeeds. Captured pairs wait if home is inaccessible;
no actors jump through obstructions. Interception uses the supplied Run state,
escort/patrol Walk, and stationary phases Idle. Offscreen actors use coarse cadence;
close-zoom gating pauses activity budgets. Response/escort budgets bound irrecoverable
cases, and disabling an officer releases ownership. Opening/rebuilding a district
reconstructs transient behavior from saved assignments; arrests are not new persistent
population/criminal records.

## Rotated building access

Patrol uses oriented object footprints for movement clearance. After converting a
point into lot coordinates, it subtracts each building/fence object position and
applies inverse authored yaw before checking its unrotated scaled footprint.
The cached AABB is only a broadphase. A rotated building's empty bounding-box
corners must remain walkable: the saved135-degree Constable Station is a regression
case. Existing freight/lumberjack clients retain their prior conservative checks.

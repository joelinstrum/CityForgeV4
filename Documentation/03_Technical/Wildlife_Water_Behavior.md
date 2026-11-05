# Wildlife water barriers and deer drinking

DistrictWildlife uses WildlifeWaterAccess for the displayed water footprints and
levels: ocean/lake/pond datums, generated ground-following rivers and player-channel
station grades. Dry checks include a0.5m ground footprint; land paths are sampled at
no more than0.4m spacing. Both proposed destinations and individual movement steps
are checked. Land mammals and ground birds cannot cross water. Fish, flying birds,
and the existing river-bank/shallow crab habitat retain their separate movement rules.
Player-channel fish/crab height checks now use the channel's real saved grades.

A deer with the local Drink controller state occasionally searches up to64m for a
reachable freshwater bank. The bounded32-ray search accepts only a dry, road/tree-safe
approach and a bank less than0.8m above/below the surface. It can temporarily leave
its woodland ecology for this trip, while physical water/obstacle rules remain active.
It stops about0.58m short of the edge, faces the water, lowers its head for6–10 seconds,
and walks back to its prior roaming position. Saltwater is never a drinking target.
After drinking the cooldown is65–135 seconds; unsuccessful searches retry20–45 seconds
later. Initial timers are staggered12–45 seconds. At most one bank search runs per
wildlife tick. No thirst/economy/health system or general pathfinding is implied.
Unreachable or obstructed banks are skipped; animals never swim to reach one.

The existing private Animal Pack Deluxe Deer_Eat animation provides the head-down
motion. WildlifeImporter.PrepareDeerDrinking adds a local looping deer-drink clip and
Drink/Drinking controller state/parameter, without modifying the source FBX. The normal
wildlife builder also includes it. Assets stay in ignored Resources/Wildlife. Other
species remain on their existing Idle/Walk controllers; the public repository ships
integration code only. Missing Drink support simply leaves a deer on ordinary wandering.

Animal activity is transient: Idle, Wandering, ApproachingWater, Drinking, ReturningHome.
It does not alter region saves or persistent population metadata. Existing close-only
visibility and far-view simulation pause remain. Drinking animation resumes on return
to close zoom. If a player creates water beneath a land animal, it is reseated on a
nearby safe dry bank rather than walking through water. If no usable dry ground is
found in the bounded search, it stays hidden and retries rather than displaying an
animal underwater. Tree indices refresh after woodland/water edits.

Verification uses disposable snapshots under Library. Checks cover wet lake/channel
samples, stream crossing with a dry-path control, saltwater rejection, recovery after
water appears under an animal, full-population dry positions, natural-deer approach /
stationary head lowering / return, actual Walk animation and zoom hide/show. The user's
original map, camera, season and animal setting are restored; no ordinary save is made.

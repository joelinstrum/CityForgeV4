# Nature animal placement and lightweight dispositions

Nature → Animals uses the local WildlifeLibrary, grouped into woodland, birds,
water/banks and domesticated animals. Random is the first card, left of Deer.
It samples suitable species from the displayed group on each placement click.
Explicit species selection exits Random; Ctrl+click toggles additional species.
Count is per species for explicit picks and the total count for Random.

Place Animals closes the modal and shows a dark-brown, terrain-draped
25m-radius outline under the pointer. The actual spawn samples stay inside that same
radius. Holding either Shift key doubles the outline and actual placement radius
to50m; releasing Shift restores25m. There is no filled disc. Up to32 local candidate samples per requested animal validate the active
district, water/habitat, roads/buildings, spacing and development rules. A failed
batch adds no animals. Escape, tool changes and district changes release the preview
and restore the terrain collider's previous enabled state. The512 placed-actor limit
and existing saved actor format remain authoritative.

WildlifeSpecies.disposition assigns Wild (zero/default) or Domesticated; there is
no player behavior selector. Wild land animals and birds evaluate at most six nearby
destinations after resting, prefer woodland and reject destinations in/near player
zoning or development. Water species keep their aquatic habitat movement. Existing
water barriers, deer drinking, tree clearance and close-zoom-only simulation remain.

Domesticated animals walk within an8m-radius home area and alternate with rest/grazing.
The optional grazingState uses an authored Animator state; missing support falls back
to idle. This is lightweight visual behavior: no needs/economy, combat or pathfinding.
The wildlife development obstacle grid is rebuilt only when NavigationState changes;
zoning checks use the existing cell dictionary. There are no per-animal scene scans.

WildlifeImporter.PrepareDomesticGoat adds the licensed local goat with Idle, Walk
and Graze (Goat_Eat) clips, a Domesticated assignment and zero ambient-spawn weight.
The normal local wildlife build retains this setup. Generated models, clips, controller,
materials and catalog stay private/ignored. DomesticFarmImporter also registers the supplied twelve-model farm collection.
The species ID and original placement location persist through existing region saves;
activity and movement remain transient and restart from that location when reloaded.

Disposable-region verification covers actual goat walking/grazing within8m, wild deer
leaving newly zoned ground, group-limited Random selection, explicit selection overriding
Random, the original25m preview geometry, actual screen-ray placement within the circle,
cancellation/collider release and placed-goat binary persistence. Evidence is in the
November22 i/outputs Animal-Behavior-QA report and modal/circle renders.

## Domestic farm models and icons

The private Domesticated.png atlas supplies Bull, Chick, Hen, Cow, Goose, Horse,
Pig, Rabbit, Ram, Rooster, Sheep and Turkey cards. All twelve supplied FBX models
are now installed and placeable alongside the existing goat, including Random.
The domestic rabbit uses domestic-rabbit to preserve the existing wild rabbit.
Chicken.fbx maps to hen. Every imported entry has Domesticated disposition and
zero ambient-spawn weight.

DomesticFarmImporter.Build (CityForge/Animals/Import Local Domestic Farm Animals)
imports from Assets/CityForge/Art/Animals/DomesticatedFarm/Source and generates
prefabs, URP materials, looping Idle/Walk/Graze controllers and clips under private
Resources/Wildlife/DomesticatedFarm. Eat Loop is preferred; Goose uses its Eat clip.
Generic skinned rigs and authored material colors remain; no source FBX is modified.
A model-alignment wrapper corrects the source -X forward to game +Z without changing
animation bindings. Adult heights range0.4–1.7m (chick0.18m). Meshes range960–2472
triangles and27–41 skinned bones. Existing wildlife rebuilds preserve these entries.

Verification checks actual baked skin deformation during Walk and Graze for every
model; all twelve can be placed together through the normal screen-ray path within
the25m circle. Over300 simulated seconds each walked and grazed inside its8m home
radius, then all twelve survived binary save and fresh region/district reopen.
Only an isolated scratch region was used. Import, placement and render evidence:
November22 i/outputs Domestic-Farm-Import.txt, Domestic-Farm-Placement-QA.txt and
Domestic-Farm-Models.png.

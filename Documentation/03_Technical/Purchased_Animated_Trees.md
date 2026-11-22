# Purchased animated tree package · November 20, 2026

The user purchased Animated_Trees_Package.unitypackage (CGTrader Animated Trees
Package) and requested replacing the old maple/oak models with its maple/oak models.
All 30 tree shapes are prepared as optional placeable trees: five acacias, five
pines, four firs, three birches, two beeches, two dry trees, two junipers, two maples,
two oaks, two spruces and one sycamore. The supplier's materials are URP/Lit already.
Vendor demo code, Object Painter and nested HDRP package are excluded.

Private source and prepared data live under Resources/Lots/Trees/
PurchasedAnimatedTrees. Original meshes, skeletal animations, textures and materials
are retained. Materials enable instancing; source pixel colors are unchanged.
Animated prefabs are used for placeable trees. Static prefabs are used for dense
forest species slots, keeping those submissions compatible with existing batching.
The approved chestnut/locust/larch mixed forest and native poplar biome are retained.

Maple 1/2 replace silver-maple/silver-maple-b; Oak 1/2 replace oak-pilot-a/
oak-pilot-b. Saved lot object IDs, positions, rotations and explicit scales retain
their meaning. The old source artwork is retained for historical references, but
catalog selections resolve to the new prepared models when installed. The other
26 trees use animated-tree-<supplier name> IDs. Optional forest broadleaf slots
19–26 retain their stored indices; maple slots map to Maple 1/2, and the six oak
slots alternate Oak 1/2. Positions/density/preferences are not regenerated. If the
private prepared art is absent, the existing source-only fallback is retained.

Prepared Summer/Fall/Winter prefabs preserve original supplied summer colors.
Deciduous maple/oak/birch/beech/sycamore variants tint leaves for fall and remove
leaf submeshes for winter, including skin weights/bind poses on retained branches.
Conifers, acacias and dry trees keep their supplied seasonal appearance. The
supplier's Maple 1 artwork is orange-leaved even in its default model; it is not
a runtime teal/olive palette override. The source summer billboards are excluded
from prepared LOD groups so winter cannot acquire leafy distant silhouettes.
Authored reduced 3D tiers are retained and selected at Zoom3–5. Near tiers retain
native LOD selection through Zoom0–2. Animator culling stops hidden near skins.

LotAssetCatalogEntry.preserveTreeMaterials bypasses the previous maple-specific
olive/brightness tint, bark recolor, 25% tall-tree reduction and old maple bounds
floor correction for these prepared assets. A static-pose ground reference avoids
using inflated skinned animation bounds as a placement base. Other trees retain
their existing treatment. No player region or lot schema changes are required.

PurchasedTreePreparation exposes CityForge/Import/Prepare purchased animated trees;
PurchasedTreeCatalog supplies ready entries. Catalog setup updates only matching
IDs and adds the new entries rather than replacing the entire catalog asset.
It skips the obsolete cached seasonal maple/oak outputs when replacements exist.

QA: all 30 materials/prefabs, sampled skeletal animation, near/far triangle reduction,
nonempty mesh LODs, winter leaf reduction, instancing, saved object transforms,
unique registration and all eight static forest aliases passed in CityForgeQA.
Near/far summer/fall/winter renders and all-tree overview are in November20
ple/outputs/Animated-Trees. No whole-district performance benchmark was claimed.
Private art stays ignored; no commit/push/merge is requested.

Main synchronization: user explicitly approved script reload during Play. Source
and private prepared assets were copied, the existing catalog was merged in place,
and main Unity verified all30 seasonal entries plus the eight forest aliases.
No existing catalog IDs were removed. No player maps or lots were written.

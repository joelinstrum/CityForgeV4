# Lot Editor Trees gallery

Trees now shares the Buildings gallery layout and input handling: searchable paged grid/list, adjustable thumbnail size, persistent preview, preview rotation/expansion, favorites, recent filters, single-click preview and double-click selection. The existing ArmAsset placement flow remains authoritative.

Only existing valid tree catalog prefabs are listed. IsRetiredTree exclusions and Player Mode cosmetic permissions are preserved. Groups and counts come from catalog metadata; current groups are Deadwood, Deciduous, Evergreen and Tropical. No tree meshes or species from the concept artwork were introduced into the placeable catalog.

Summer/Autumn/Winter is a preview-only control. Season-aware thumbnail cache keys and InstantiateObject's existing season parameter use the approved seasonal prefabs/material behavior. Trees in a saved lot still follow the district season. The details show measured placement height, actual placement footprint and whether seasonal models are supplied; no ecological requirements or costs are fabricated.

Vary placement size retains the existing ±15% flag. Randomize within group retains the existing varyTreeOnPlacement behavior when the lot is placed in game; it does not reroll the catalog on every UI click. Both options survive closing/reopening the modal. Building options and gallery state remain independent.

Tree favorites and usage history use CityForge.Trees.Favorites/Used, separate from Buildings. The shared recent-assets history remains compatible. Unknown or retired IDs in history cannot reappear as gallery items. Clear Filters clears filters without deleting stored favorites.

The supplied landscape, decorative tree emblem and icon sheet are private resources under UI/LotTreeGallery and stay ignored by Git. Their decorative tree is not a placeable asset. Glyph UVs use source-image normalized coordinates so Unity's NPOT texture resizing cannot shift icon crops. Other content falls back to the existing vector glyphs if the private atlas is unavailable.

Thumbnails retain existing material/prefab scale rules. The gallery's optional tight framing uses near LOD0 renderer bounds instead of hidden distant billboard bounds. Tree UI images additionally cache a square UV frame around the rendered alpha silhouette, removing transparent leaf-card/wind padding without changing source models or materials. Existing consumers without tight framing keep their previous framing. Preview caches release on selection/season changes and modal closure.

Isolated Play validation runs the existing Buildings interaction/dropdown suite before Trees tests. Tree checks cover real catalog availability, retirement exclusions, dynamic groups/counts, seasonal model previews without lot mutation, favorites/history, empty-result guards, existing randomization/size controls, grid/list single- and double-click behavior, reopening, and explicit Select Tree. Native screenshots are inspected before installation. No region geography, vegetation generation, tree shaders or source model assets are changed.

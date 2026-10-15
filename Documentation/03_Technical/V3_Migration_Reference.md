# V3 Migration Reference

## Relationship between versions

**V3 is a working reference and specification; V4 is a clean design and project.** V3 demonstrates existing mechanics, art, camera behavior, regional ideas, and hard-earned lessons. It does not define what V4 must retain. Treat its Git history and documentation as an archive that can answer specific questions.

For each V4 need, inspect the relevant V3 behavior or asset, decide whether it supports the new vision, and then either reuse deliberately, adapt, or rebuild. Record the reason and any dependencies before moving material. Avoid copying the V3 project wholesale, especially caches, experiments, packages, and obsolete rendering assumptions.

## Reference checklist

- **Behavior:** what does the V3 feature achieve for players, and does that goal still fit?
- **Visual quality:** is the asset complete from rotated and lower views, does it hold up in V4 terrain and lighting, and can its materials fit the URP/PBR workflow?
- **Complexity:** does its mechanic add meaningful decisions or repeated chores?
- **Dependencies:** what else would have to move with it?
- **Provenance:** where did the asset or code come from, and is reuse permitted?
- **Verification:** what comparison or prototype demonstrates that the migrated piece works?

The first rendering milestone should be evaluated against the concept target before simulation code is brought over. Later migration can be selective: a building, tree, camera behavior, district concept, or other piece only when V4 needs it.

## Open questions

- Which V3 systems are most useful as behavior references?
- Which art assets should be tested first in V4's landscape?
- What old assumptions should be retired instead of translated?

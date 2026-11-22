# Animals modal integration handoff — November 22, 2026

This change adds Nature → Animals as a centered modal with catalog groups,
Ctrl+click multi-selection, group-limited Random, and count controls. Placement uses
a brown circular OUTLINE,25m radius; holding either Shift key expands preview and
actual scatter to50m. Escape/tool/district changes cancel placement.

Wild species favor undeveloped woodland. Domesticated species walk/graze within8m
of their saved placement location. Existing water barriers, deer drinking and
far-zoom simulation pause remain. Activities are transient; existing saved actor
records persist species and original position with no save-format change.

Twelve supplied rigged farm models are supported: Bull, Chick, Chicken→Hen, Cow,
Goose, Horse, Pig, Rabbit→domestic-rabbit, Ram, Rooster, Sheep, Turkey. They have
looping Idle/Walk/Graze controllers, Domesticated disposition and zero ambient
spawn weight. Source -X forward is corrected to +Z using a separate alignment
wrapper. Existing wild rabbit remains distinct. The existing local goat is retained.

## Source integration

Branch: feature/modal-windows. The PR targets main and includes only this animal
work. It deliberately excludes the frozen terrain baseline supplied at worktree
creation and excludes the modal workspace's local productName/save-path setting.
Terrain follow-up is independently tracked by PR #29:
https://github.com/joelinstrum/CityForgeV4/pull/29

Fetch/review the animal PR and merge or cherry-pick its commit into the destination's
own branch/worktree. Preserve destination edits, especially NatureToolbar.cs,
NatureToolPopup.cs, NatureWaterModal.cs and PrototypeRegionView.cs. Do not reset a
checkout or check out a branch already used by another worktree. Terrain PR #29's
TerrainUpdateRequested subscription and this PR's AnimalsPlacementRequested
subscription must both survive integration when combining the two changes.

## Private Unity assets — Git is insufficient

The repository is public/source-only. No FBXs, imported/generated materials,
controllers/clips/prefabs, icon PNGs, player saves or caches are included in Git.
A local private asset bundle and installer accompany the handoff; the originating
chat provides their exact paths. Transfer only to the user's other local project
checkouts. Preserve .meta GUIDs and inspect conflicts before overwriting assets.

Private paths:
- Assets/CityForge/Art/Animals/DomesticatedFarm/Source/*.fbx (+.meta)
- Assets/CityForge/Resources/Wildlife/DomesticatedFarm/ (+.meta)
- Assets/CityForge/Resources/UI/Animals/ (+.meta)

The bundle does NOT overwrite TemperateWildlife.asset. In the destination Unity
6000.1.12f1 Editor, after source compilation and asset import, run:
1. CityForge → Animals → Add Local Domesticated Goat (existing local licensed
   Animal Pack Deluxe goat source is required for this existing optional entry).
2. CityForge → Animals → Import Local Domestic Farm Animals.

The farm importer preserves unrelated existing species and registers the twelve
farm entries in the destination's own private library. The normal wildlife rebuild
preserves installed domestic farm entries. Do not publish the private bundle or
commit licensed/generated art. If private farm models are absent, the UI retains
Model pending cards, disabled for placement and excluded from Random.

## Verification and review state

Unity6000.1.12f1 compiled successfully. All twelve FBXs have skinned rigs,960–2472
triangles and27–41 bones. Baked mesh checks confirmed actual Walk and Graze
deformation for each. Actual screen-ray placement created all twelve inside25m;
over300 simulated seconds each walked/grazed within8m (maximum measured7.859m).
A binary save and fresh region/district reopen restored all twelve domestic prefabs
and grazing support. These tests used only an isolated disposable region. Modal
pages and model renders were visually reviewed. The outline/Shift correction was
compiled and uses one shared radius expression for preview and placement; synthetic
Shift input was not exercised by the disposable-region suite.

The originating Unity instance/project is isolated from the primary Terrain Editor.
Source remains unmerged until this PR is integrated. See Animal_Placement.md for
implementation details and the local handoff evidence files for test results.

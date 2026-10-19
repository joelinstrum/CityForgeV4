# Opening saved regions with progress

The main Open Region list calls `MainMenuController.OpenSavedRegion`. It creates an independent screen overlay immediately and blocks duplicate Open actions. The overlay owns its loading coroutine so hiding the menu Canvas cannot interrupt loading.

Saved-file decompression, metadata/array parsing and validation run on a background Task through `SavedRegionStore.Load`; this path uses value data and JsonUtility rather than scene objects or resource loading. All Unity terrain, mesh, material, vegetation, water and camera work stays on the main thread.

`PrototypeRegionView.OpenStages` and `MeadowDistrictLandscape.BuildStages` yield between setup stages. Native height sampling and terrain painting yield after batches of rows when the batch timer reaches about 12 ms. Terrain calculations and seeded placement rules are unchanged. Existing synchronous editor/test entry points drain these same enumerators through `RegionBuildSteps.Complete`.

The overlay shows the region name, current stage, a progress bar, and animated activity dots. Bar positions are weighted stage milestones, not a file-byte percentage or an ETA. The final complete state is displayed before the overlay closes. A failed read/build disposes the staged enumerators, removes the partial region, restores the menu and resets the Open guard. Saves are read-only throughout loading.

Some indivisible Unity operations (resource/shader import, native terrain uploads and GPU/water initialization) can still stall a frame, particularly on a cold editor cache. This change reduces long application-side blocking; it cannot guarantee that macOS will never show a wait cursor. The application does not hide or override the OS busy indication.

Verification trigger: `Library/RegionLoading.request`. The editor check exercises a missing-file recovery, cached and uncached district opening, progress completion, frame advances during loading, duplicate-open protection, preserved firs/native terrain and an untouched original save. The uncached check uses a disposable file. Maximum observed editor-update gaps are diagnostic observations, not a standalone performance guarantee.

## Verified editor results (2026-10-17)

Cached and uncached Hills and Mountain Test loads passed, with 119 / 291 rendered-frame advances respectively during loading. Largest observed editor-update gaps were 531 / 640 ms; longest measured main-thread build steps were 449 / 604 ms. Before removing an unintended synchronous overview build from HUD setup, the same checks observed about 4.1–4.5 seconds without an editor update. The HUD now defers landscape construction until the actual saved view is selected, and the region controls stay hidden while the overlay is active.

Missing-file recovery, duplicate Open prevention, monotonic stage progress, completion, native terrain/fir presence and read-only save behavior passed. These are short editor observations on this machine, not a guarantee for cold imports or every map/device. Historical editor asset-study commands that require an immediate result use the explicit editor-only synchronous compatibility entry point; normal player Open actions always use the progress overlay.

Follow-up compatibility checks passed: the existing regional-biome wizard/four-district/edge/save-load integration suite, plus the mountain evergreen suite (1,422 evergreens, all four evergreen types, seasonal slots, unchanged placements and disk round trip). Unity was left in the Hills and Mountain Test district for interactive testing.

## District entry progress

Map cells and district buttons now use RequestDistrict and the same RegionLoadingOverlay as menu Open. The overlay renders before work starts; EnterDistrictStages yields through the native landscape builder. Camera/input and duplicate entry are locked until completion. Failed entry returns to overview. Immediate EnterDistrict remains an editor/tool compatibility path. Verify with Library/DistrictLoading.request. Native asset/GPU work can still stall individual frames.

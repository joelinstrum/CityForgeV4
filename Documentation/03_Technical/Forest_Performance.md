# Forest performance isolation — October 17, 2026

The eight new maple/oak variants are visually approved. They default OFF while performance is evaluated. Open a district, click **NEW TREES: OFF**, then enable species individually or cumulatively. NONE restores native poplar presentation; firs are retained. Switching does not change saved species or tree placements. Preferences apply globally, including cached landscapes.

## Fixed-camera editor measurements

3434 × 1728 Game view, Summer, trees on, small plants off, frame cap/vsync temporarily disabled. Each row uses the same canonical population and camera. Disabled additions use native poplar substitutes. Each sample covers 75 rendered frames following warmup. These are editor observations, not standalone GPU timings or guaranteed FPS. Camera and pacing settings were restored.

| Enabled additions | Wide mean ms/frame | Wide draw calls |
|---|---:|---:|
| None | 16.11 | 14,928 |
| All eight | 30.91 | 34,855 |
| Maple A | 17.11 | 16,268 |
| Maple B | 16.93 | 16,298 |
| Canyon Live Oak A | 18.83 | 18,757 |
| Canyon Live Oak B | 18.29 | 17,996 |
| Southern Live Oak | 18.27 | 18,162 |
| Angel Oak | 16.50 | 15,678 |
| OakPilot A | 18.33 | 18,096 |
| OakPilot B | 18.40 | 18,096 |
| None, repeat | 15.89 | 14,928 |

Close camera: none 17.44 ms / all 33.56 ms; repeat none 17.77 ms. Wide UnityStats triangle counts rose from 19.8M to 36.6M; these are rendered-pass totals, not unique model polygon counts. Maples have five material slots per LOD; Canyon/Southern/Pilot oaks have 12–13, Angel has three. All have three genuine mesh LODs. Individual additions cost less than their combined mix; no single runaway species was found. The measurements implicate aggregate rendering load, but do not isolate CPU versus GPU cost. Material consolidation is a possible later optimization requiring visual verification, not part of this change.

## Implementation and checks

`ForestTreeMix` owns the preference and presentation mapping. Canonical slots 19–26 remain stable; disabled display instances map to native slots. Runtime loads enabled new prefabs only. `MeadowDistrictLandscape.RefreshForestSpecies` updates active/cached landscapes. `ForestSpeciesControls` supplies the district HUD panel. All original art and seasonal material settings remain unchanged.

`ForestPerformanceChecks` verifies canonical data and absent disabled display species while comparing each mask. `ForestSwitchChecks` exercises UI raycasts, cumulative selection, seasons, preference persistence and a temporary disk save while disabled. `ForestTreeChecks` temporarily enables all additions to check seasonal assets and save/load, restoring the prior mask afterward. Player saves are never rewritten by these checks.

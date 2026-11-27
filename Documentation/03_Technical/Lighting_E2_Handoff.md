# E2 production lighting handoff — November 27, 2026

The user selected E as the immutable approved reference, reviewed E2, then explicitly requested installing E2 in-game. Active native district afternoon lighting is E2. E remains archived in `Lighting_Baselines/E-Archived-Study.json` and implemented by `AfternoonLightingBaseline` with `e2=false`. Do not rename or alter E when experimenting.

## What changed

- New `DistrictAfternoonLighting.cs`: exact E/E2 sun and SH definitions, afternoon Neutral/zero-exposure volume, lightweight local environment reflection.
- Two hooks in `DistrictDaylight.cs`: apply after native daylight setup and after clock interpolation. The latter prevents the old native clock path from overwriting the approved afternoon direction/intensity.
- URP HDR grading; existing contact AO tuned to the common validated candidate setup (1.7/.29, Depth source unchanged).
- E2 vs E remains only ambient .60→.69 and soft-shadow Medium→High. No building, texture, glass, grass, water or geometry edits.

This intentionally supersedes the older hard-shadow afternoon policy for detailed gameplay districts. Keep existing4096/four-cascade settings, camera shadow fitting and navigation throttling. Other day phases retain native lighting; the existing late-afternoon transition blends to evening. Overview and Lot Editor are not given a new lighting rig.

## Validation and benchmark

See `Lighting_Baselines/README.md` and `GPU-Summary.txt`. Earlier controlled QA Editor medians: C12.67ms, E12.43ms, E2 12.69ms on M1Max/Metal1920x1200;909 unique samples each, three counterbalanced rounds. These are hardware GPU times including editor overhead. They are not a standalone-player or newly integrated runtime benchmark. The timed steady state excludes probe captures.

Native-path validation checks exact4PM sunlight/direction/color/bias/SH, effective Neutral tone mapping/exposure, night deactivation and afternoon reentry in an actual loaded private district. Private captures/raw evidence remain outside this public source-only repository. No saved maps or artwork are committed.

## Next phase

1. Review active E2 in ordinary gameplay, including pausing/scrubbing the clock, district exit/reentry, camera movement, driving, seasons and night fixtures.
2. Establish the permanent six-building test scene: limestone, red brick, painted brick, brownstone, timber and concrete, with grass/roads/trees/neighbors. Preserve fixed camera and exposure for comparisons.
3. Profile a standalone player and reflection refresh spikes in a large town. The128px160m local probe checks movement/phase every2s; it refreshes only after64m movement or phase change. Continuous within-phase sun movement is not recaptured every frame.
4. Treat material weathering, window optics and geometry as separate work. Do not “fix” lighting by baking shadows/highlights into building textures.

The shared development checkout has extensive unrelated uncommitted work from another session. This branch was created in an isolated worktree from origin/main; no unrelated changes were staged. If merging into the live checkout, preserve the unrelated `RigidVehiclePresentation.ApplyTime` addition in DistrictDaylight and any later edits. Never reset or clean that checkout. Private QA depends on local assets absent from source-only Git.

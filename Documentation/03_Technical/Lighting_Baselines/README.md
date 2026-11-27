# Afternoon lighting: E baseline and active E2

E is the immutable, user-approved lighting reference. E2 is now selected for native district afternoon gameplay. E2 changes only the SH sky-fill multiplier from 0.60 to 0.69 and soft-shadow quality Medium to High. `AfternoonLightingBaseline` preserves both choices without material changes.

The archived study JSON uses an upstream sunlight multiplier (`direct: 2` for E), not the effective Unity light intensity. The validated effective E/E2 light intensity is **2.6**. Do not feed archived JSON directly into a runtime Light. Archived softness 2/3 means Medium/High in URP17.1.

## Active implementation

`DistrictAfternoonLighting.cs` applies E2 through the existing `ApplyDistrictDaylight` and `InterpolateDistrictSun` hooks, independent of era/building. Sun rotation is (32,290,0), color (1,.98,.945), strength .97, depth/normal bias .015/.035 with per-light settings. Neutral tone mapping; exposure/contrast/saturation zero; white filter. Ambient SH starts with ambient (.22,.24,.28), adds upward directional (.8,.87,1) at .12, then scales channels by (.92,.99,1.12) times .69 (E is .60). These are the actual gameplay comparison values, not the old calibrated afternoon sliders.

Native morning/noon/night and the clock remain authoritative. Afternoon E2 holds through the existing early/central afternoon period, then blends into the native evening endpoint over the existing final 20% of the afternoon phase. The E2 volume is disabled outside afternoon. Navigation render-scale/cascade fitting is unchanged. Lot Editor and regional overview are not retuned by this feature.

The common validated rendering setup is also installed: HDR grading and existing Depth-source contact AO intensity1.7/radius.29. Shadow map4096, four cascades/splits remain unchanged. A local128px reflection probe uses gain.3 and160m coverage; refresh only after moving64m or phase change, checked every2s, with no overlapping captures. No water/material/geometry changes. Reflection-capture spikes are excluded from the steady-state benchmark and require later profiling in large towns.

## Evidence and limits

Same actual saved district, four-townhouse row, approved trees, camera, frozen4PM,1920x1200 SMAA, one world-camera render/frame. Apple M1Max/Metal, Unity6000.1.12f1. Three counterbalanced rounds;120 warmup and300 sampling frames/condition/round;909 unique positive hardware GPU samples each. Medians C12.6719ms, E12.4263ms, E2 12.6914ms. E2-E +.2651ms (+2.13%); distributions overlap. These are **QA Editor hardware GPU frame timings including Editor overhead, not standalone-player performance**. Zero/one/two-render controls1.7032/12.6914/15.0066ms confirm scene-workload response. No CPU-time substitution.

Representative shadow-interior mean linear luminance increased12.0%. This is not a uniform guarantee across every shaded pixel. No image color correction was applied.

Private comparison captures, raw CSV, complete QA harness, pipeline audit and original E source/image SHA256 archive are intentionally excluded from this public source-only repository. They remain in the local Lighting-E2 output package listed in the private handoff. The integration receives separate native-path lifecycle validation; the above timings are the earlier controlled candidate comparison, not a newly measured integrated-player benchmark.

## Next phase

Preserve E exactly. Start future changes from active E2; do not retune sunlight, grade or materials as incidental cleanup. Establish the six-material permanent test scene (limestone, red brick, painted brick, brownstone, timber, concrete), then assess ordinary town scale, seasonal foliage, terrain, interior streets, night transitions and probe recapture cost. Measure a standalone player before making performance guarantees. Architectural material/geometry revisions are a separate experiment.

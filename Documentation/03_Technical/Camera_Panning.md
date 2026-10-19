# Panning investigation — October 19, 2026

A repeatable sideways sinusoidal pan in Hills and Mountain Test sampled 240 frames per mode after 60 warmup frames, at a close orthographic view. Same terrain and camera path; optional broadleaf additions remained off. The input controller was disabled during the scripted pan, with its pose application used once per observed rendered frame. Tests run in the Unity editor, not a standalone player. CPU frame intervals are not measurements of display presentation or tearing. Test order and vegetation warming can affect comparisons. No saved maps were written.

| Mode | Mean ms | Median ms | p95 ms | p99 ms | Max ms | GC collections |
|---|---:|---:|---:|---:|---:|---:|
| motion_on_cap60 | 16.66 | 18.08 | 22.16 | 23.55 | 31.42 | 1 |
| motion_off_cap60 | 16.75 | 15.70 | 27.43 | 29.98 | 32.77 | 0 |
| stream_off_cap60 | 16.67 | 18.33 | 22.43 | 23.29 | 34.47 | 1 |
| trees_off_cap60 | 16.66 | 16.67 | 18.95 | 21.59 | 23.62 | 0 |
| vsync_motion_on | 8.15 | 8.07 | 14.72 | 16.89 | 26.27 | 1 |
| uncapped_motion_on | 8.14 | 8.01 | 14.87 | 16.81 | 16.99 | 0 |
| motion_on_repeat | 16.67 | 18.62 | 22.57 | 24.48 | 25.50 | 0 |

## Finding and bounded correction

A 60 FPS average masked uneven frame intervals. Movement quality reduced rendering cost and slower-frame tails; freezing vegetation streaming did not substantially improve this run. Trees still add cost, but removing them did not eliminate uneven software-capped timing. Few GC collections occurred, so collections alone do not explain continuous unevenness. This test did not isolate main-thread versus GPU stalls.

The existing desktop pacing policy explicitly used vSyncCount=0 and Application.targetFrameRate=60. Unity documents this software timing method as subject to microstuttering and recommends VSync for smooth desktop pacing: https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Application-targetFrameRate.html

The synchronized option now sets vSyncCount=1 and targetFrameRate=-1, with the HUD label FRAME PACE: VSYNC. It follows display refresh, rather than promising 60 FPS. Uncapped remains vSyncCount=0, target=-1. The old preference key and SetFramePacing60 API remain for compatibility. Editor VSync/uncapped rows had similar intervals, so the editor result does not prove display synchronization fixed the user's perception. User comparison and a standalone presentation/frame-time capture remain necessary if the issue persists. Movement quality, tree placement and approved visual settings are unchanged by this correction.

`Library/CameraPan.request` runs the seven-mode comparison; writing `verify` checks synchronized/uncapped policy and final HUD caption, leaving VSync enabled. Raw frames are in Camera-Pan-Frames.csv. The original camera, quality, visibility and pacing were restored after comparison; verification then explicitly selected synchronized mode.

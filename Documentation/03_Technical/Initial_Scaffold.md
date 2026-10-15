# Initial Project Scaffold

## Implemented baseline

The V4 folder is a Unity **6000.1.12f1** project with **URP 17.1.0** declared in `Packages/manifest.json`. Unity has generated and assigned a URP pipeline asset and a Universal Renderer asset. The first materials use URP/Lit with PBR color, metallic, and smoothness values. This setup establishes a working foundation; it does not claim that the final landscape target or metropolitan performance has been achieved.

The project-owned files live under `Assets/CityForge/`:

```text
Art/{Buildings,Environment,Vegetation}/
Materials/
Prefabs/
Rendering/
Scenes/{Development,Gameplay}/
Scripts/{Camera,Editor}/
Textures/
```

Only folders needed by the current scaffold or imminent asset imports exist. Add characters, vehicles, UI, simulation, and other categories when actual work begins. Unity-generated caches are excluded by `.gitignore`; the project source consists of Assets, Packages, ProjectSettings, and Documentation.

## VisualTest scene

Open `Assets/CityForge/Scenes/Development/VisualTest.unity`. It contains a 100 m ground plane, a reusable mass-study block prefab, three basic URP/Lit material samples (dry stone, wet stone, and metal), one directional light, and the CityForge camera. These are neutral scale and material references, not game assets. The scene is the **permanent development sandbox** for checking imports, proportions, lighting, shadows, camera views, LOD changes, and later rendering performance. It can grow as real assets arrive.

The initial scene is listed in Build Settings. Its creation script, `InitialProjectSetup`, is available from Unity's **CityForge → Create Initial Visual Test** menu and preserves an existing VisualTest scene instead of overwriting it.

### Camera controls in Play mode

| Control | Action |
|---|---|
| W/A/S/D | Pan across the ground plane. |
| Shift + movement | Pan faster. |
| Right mouse drag | Rotate and lower or raise the camera. |
| Q / E | Rotate left or right. |
| Middle mouse drag | Pan. |
| Mouse wheel | Zoom from neighborhood toward region scale. |
| P | Switch between orthographic and perspective framing. |
| Home | Return to the default elevated orthographic view. |

The default is an elevated orthographic/dimetric view. The camera rig has a movable focus, yaw, pitch, distance, and projection mode; no world asset should depend on its initial direction. Lower views and broader zooms are possible, though controls and limits should be tuned with real city content.

## Explicitly deferred

Water, traffic, citizens, procedural cities, gameplay simulation, streaming, and bespoke LOD systems are not in this scaffold. The first asset imports should test the [asset conventions](Asset_Conventions.md), then add complexity to VisualTest in response to actual visual and profiling results.

# V4 Architecture

## Architectural intent

V4 starts as a clean project guided by the desired game and visual target. It should not inherit V3's code, assets, packages, or generated files by default. This document defines boundaries for investigation rather than a settled class diagram.

**Established foundation:** Unity 6, URP, PBR materials, and a true 3D world. The default elevated orthographic/dimetric camera is a presentation choice. World geometry, materials, lighting, and placement must work from rotated and lower views. The [initial scaffold](Initial_Scaffold.md) implements a small VisualTest scene and camera; individual world systems remain future work.

**Scale rule:** the simulation state of a city is distinct from its visual representation. A building can continue to exist and operate when its detailed renderer is culled or replaced by a distant LOD. A citizen's simulation state should not require an animated GameObject to exist at all times. This is an architectural boundary to preserve as systems are introduced, not a request to build those systems now.

**Working rule:** design for scale, implement for current need, and optimize based on profiling. The [scalability plan](Scalability_Plan.md) names likely problems, candidate solutions, and the point at which they should be considered. No streaming or procedural-city framework is justified in an empty project.

The game concept suggests several separable responsibilities:

| Concern | Responsibility to preserve |
|---|---|
| World and rendering | Make geography, water, vegetation, buildings, seasons, and change legible at close, normal, and metropolitan views. |
| Calendar and historical context | Bound plausible content by date and setting. |
| Local development | Let city conditions determine adoption, growth, and character. |
| Economy and connectivity | Translate resources and access into prices, opportunity, employment, and trade. |
| Services | Track coverage, capacity, and quality without pipe-level chores. |
| Place memory | Record events tied to buildings, sites, neighborhoods, and the region. |
| Presentation | Surface useful decisions, Chronicle stories, and Governor identity without overloading the main view. |
| Content packages | Allow history, architecture, and reputation content to vary. |

These are conceptual boundaries. They do not prescribe assemblies, classes, storage formats, or a particular event system. The VisualTest scene is a permanent development sandbox within this project, not a separate proof-of-concept. Later vertical slices can test whether economy, historical context, and memory combine into a coherent city-building loop.

## Open questions

- What is the minimum persistent state needed for centuries of city and place history?
- Which simulation cadence gives stable results at the chosen game speed?
- How can historical packages remain data-driven without making every mechanic configurable?
- Which platform, performance, and save-size targets should constrain architecture?

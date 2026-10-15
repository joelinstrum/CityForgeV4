# Scalability Plan

## Representation changes with distance

The underlying city remains the same while the visual representation changes. At close range, buildings may show full geometry, PBR detail, props, vehicles, pedestrians, local lights, animation, and richer shadows. At the normal elevated gameplay view, smaller features can disappear and LODs, vegetation, agents, and shadow quality can be simplified. At metropolitan distance, the essentials are city shape, neighborhoods, streets, skyline, terrain, water, land use, historical core, and major infrastructure. Window trim or individual pedestrians are not useful at that scale.

This is a design target, not three rigid distance thresholds. Transitions should be judged in motion and based on on-screen size, camera angle, density, and measured cost. A distant billboard or impostor is acceptable if it represents a complete underlying 3D asset and works for the allowed camera range.

## Problems and likely responses

| Problem as content grows | Candidate response | Introduce when |
|---|---|---|
| Thousands of detailed buildings | LODGroups, shared materials, instancing where compatible, frustum/distance culling, eventual spatial sectors. | Representative dense-building scene shows cost or poor transitions. |
| Huge numbers of trees | Vegetation instancing, species-specific LODs, distant clusters or impostors, less distant shadow work. | Real forest density is profiled at normal and far zoom. |
| Expensive metropolitan shadows | Shadow-distance and cascade tuning, selective casters, simpler or no distant shadows. | Lighting tests show visible cost. |
| Many citizens and vehicles | Keep simulation state separate from rendered agents; pool nearby visuals, simplify or aggregate far traffic. | Actual agent counts and viewing needs are known. |
| Too many small props | Cull by screen size and distance; combine or instance repeated objects when useful. | Props create measurable draw or CPU overhead. |
| PBR material cost and variation | Shared material families, texture budgets, atlases or arrays where beneficial, simpler distant shaders. | Material profiling identifies a bottleneck. |
| Free camera exposes missing sides | Complete 3D geometry and materials; review from rotated and lower views. | Required from each asset's first import. |
| Very large city or region | Spatial partitioning and later asynchronous loading/unloading if profiling requires it. | World size and memory pressure justify streaming. |

Possible tools include Unity LODGroups, GPU instancing, SRP Batcher-compatible materials and shaders, frustum and distance culling, occlusion culling where it fits city layouts, pooled dynamic objects, and chunk or sector management. None is mandatory simply because it appears here. Avoid a streaming architecture before there is a world large enough to measure.

## Boundary to protect

Simulation state is not a set of always-rendered GameObjects. A building can keep operating while its close model is absent; a citizen can exist in simulation while no animated citizen is drawn. Future systems should make visual proxies replaceable by distance or visibility without destroying the underlying city record.

**Rule:** design for scale, implement for current need, and optimize based on profiling. The VisualTest scene should gradually become a repeatable rendering stress scene as real assets and density arrive.

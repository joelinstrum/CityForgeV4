# Rendering Vision

## Prototype for the desired image

The target is a rendered world with terrain depth, convincing riverbanks and shallow water, varied vegetation scatter, meaningful shadows, and atmosphere that supports a contemporary city builder. The confluence concept image should be translated into observable acceptance points, then compared with screenshots from an actual running V4 prototype at similar scale and camera framing.

Use the [goal-image gallery](../Goal_Images/README.md) for side-by-side reviews. Begin with the two landscape images; revisit the same rendering choices against the later town and city images before deciding they scale well.

**V4 direction:** a clean Unity 6 project using the Universal Render Pipeline (URP), physically based rendering (PBR) materials, and complete 3D world assets. CityForge V3 uses Unity's Built-in Render Pipeline; its camera-dependent rendering assumptions should not dictate V4's look. The initial URP project and VisualTest scene are established, while the visual quality and performance of future city content still need to be proven.

URP is the **rendering architecture**. PBR describes how materials respond to light. A future riverbank should read as material covering shaped terrain rather than a gray rocky image painted onto a flat surface. Where useful, its material may include base color, normal detail, smoothness or roughness variation between wet stone and dry soil, and height information. Metallic response is generally irrelevant for ordinary terrain but useful for suitable built surfaces. These are available material properties, not a requirement to author every map for every asset. Highlights should come from lighting and material response rather than only being painted into the color image.

Eventually combine those materials with actual terrain geometry, depressed riverbeds, transparent shallows, appropriate material blending, scattered 3D rocks, reeds and vegetation, stronger lighting and shadows, atmospheric treatment, and sensible LOD or instancing. The exact terrain, water, shader, and vegetation techniques remain experiments to compare for visual quality, performance, asset compatibility, and development cost. **Water is not part of the initial scaffold** and should be tackled as its own rendering problem.

A proposed first milestone is intentionally narrow: camera, terrain, a river confluence, shoreline detail, trees and low vegetation, and lighting. No city simulation is required to judge whether the landscape is moving toward the goal image. After that, test one building, roads, seasons, and denser settlement scenes to ensure the approach scales visually.

## Questions to test

- Can the river read as a depression with a visible bed and believable shallow-to-deep transition?
- Can irregular banks avoid a repeated texture ribbon?
- Can vegetation density and detail hold up at the intended near and far zoom levels?
- Do buildings sit naturally on terrain and remain legible under the chosen lighting?
- What performance budget works for the intended platforms and region size?

No particular shader, terrain package, or optimization technique is mandated here. Use side-by-side game screenshots and performance trials against the concept images to refine the established URP/PBR approach. See the [scalability plan](Scalability_Plan.md) for distance-based representations and profiling triggers.

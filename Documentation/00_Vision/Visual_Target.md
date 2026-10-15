# Visual Target

## Direction

V4 should look like a **contemporary, rich living landscape** viewed as a city builder, with enough physical depth that the player believes a river cuts through terrain and a city grows out of it. The concept images discussed for the river confluence are visual goals, not literal asset or performance specifications. Future images should use a coherent visual language so eras and locations can be compared.

The [goal-image gallery](../Goal_Images/README.md) contains the first ten references, from river wilderness to mature city. Its image notes identify qualities to inspect; the concept art should not be treated as verified historical content.

The V4 foundation is a **clean Unity 6 project using URP and a PBR material workflow**. Terrain, buildings, roads, stone, vegetation, and other surfaces should respond coherently to light and material properties. PBR is an art direction, not a demand for photographic realism or one material technique everywhere; readability at city-builder scale remains essential. The underlying assets should also hold up when the camera rotates or moves lower than the default view.

The first target is a natural river landscape: convincing terrain relief; irregular banks; visible shallow water and riverbed; transitions through gravel, mud, stone, reeds, and vegetation; forests with varied density; scattered environmental detail; and lighting and atmosphere that reveal form. A uniform grass-to-bank-to-water stripe would miss the target even if the textures were high resolution.

Buildings and roads should eventually sit within that landscape rather than appear pasted onto it. The same camera and world should make the passage from settlement to mature city visually legible. Architecture, transport, vegetation, clothing, and street life should evolve without turning eras into hard visual switches.

## Concept-image workflow

1. Select a small set of goal images: natural landscape, early settlement, nineteenth-century town, dense later city, and at least one seasonal or lighting variation.
2. Annotate observable qualities in each image: composition, terrain, water, shoreline, vegetation, lighting, architecture, scale, and density.
3. Build a V4 prototype and compare actual game screenshots against the target at the same view and scale.
4. Turn visible gaps into focused work. Accept the result based on what the game actually renders.

The permanent [VisualTest scene](../03_Technical/Initial_Scaffold.md) starts with a camera, scale reference, and material samples. It will grow into a visual and performance testbed as real assets arrive. The river confluence remains a future visual milestone; water is deliberately separate work. Compare actual Unity screenshots with the concept images as each environment feature is added.

## Open questions

- Which existing CityForge art assets hold up in the new landscape?
- What rendering approach achieves shoreline depth and environmental density at the required camera distance and hardware targets?
- What level of realism best preserves clarity when the region fills with buildings?

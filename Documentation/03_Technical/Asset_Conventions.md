# Asset and Prefab Conventions

These are the starting rules for V4 imports. Revise them when real assets expose a problem; apply changes consistently rather than making each asset an exception.

## World space

- Use **1 Unity unit = 1 meter**. Model and import at real-world scale where practical.
- Use **Y up**. Use X and Z for the ground plane; +Z is the local forward direction for an asset that has a front.
- Put a placeable building's pivot at ground level, near the center of its footprint unless placement requires a documented exception. A tree's pivot is at the trunk base. Keep origins predictable for rotation, snapping, and future LOD swaps.
- Build complete 3D geometry where an object can be seen from multiple directions. The default isometric view does not justify camera-facing-only façades. Distant impostors may still be valid representation levels when their transitions are unobtrusive.

## Naming and ownership

- Keep game-owned content under `Assets/CityForge/`. Use `Art` for source meshes and textures, `Materials` for reusable material assets, `Prefabs` for scene-ready objects, and `Scenes/Development` for test scenes.
- Start material names with `M_`, prefab names with `PF_`, and texture names with `T_`. Use readable feature names, such as `M_Brick_Red` or `PF_Townhouse_01`. The sample block is `PF_MassStudy_Block`.
- Share material assets across instances when the surfaces are genuinely the same. Do not create a unique material for every placed building merely to change one small property.

## PBR and lighting

- Author for URP/Lit or another deliberate URP-compatible shader. Use base color, normal, metallic, and smoothness inputs according to the material; add height or other maps only where they improve the result at target viewing distances.
- Keep baked highlights and shadows out of base-color textures when lighting should create them. Compare materials in VisualTest under both the default elevated camera and a rotated or lower view.
- Set texture sizes according to visible screen use and profiling rather than a single universal resolution. Record texture and material families when assets are introduced.

## Scene-ready prefabs

- A prefab should contain the visible model, appropriate colliders, materials, and any required behavior, with predictable transform scale. Use colliders only where interaction or physical behavior needs them; avoid detailed mesh colliders by default.
- Mark static objects appropriately after their behavior is known. Do not apply lightmap/static flags indiscriminately before the lighting strategy is tested.
- Consider LODs, instancing compatibility, and shadow casting during import. Create LODs when an asset will appear in enough instances or at enough distances to justify them; measure transitions in VisualTest.
- Distinguish static scenery from dynamic actors. Dynamic visuals should be suitable for pooling or other distance-based simplification if their count grows.

The [scalability plan](Scalability_Plan.md) records where additional optimization conventions may become necessary.

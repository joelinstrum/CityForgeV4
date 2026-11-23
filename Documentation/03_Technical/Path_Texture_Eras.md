# Path texture registration and era availability

New private textures are registered through `Resources/Config/LotPropStyles.json` under `paths`. A record can supply:

```json
{
  "materialId": "artdeco-concrete-sidewalk-tiles",
  "label": "Concrete Sidewalk Tiles",
  "textureResource": "Lots/PathMaterials/artdeco-concrete-sidewalk-tiles",
  "tileSizeMeters": 3,
  "firstEra": "modern",
  "era": "modern",
  "family": "Concrete",
  "wealth": 2,
  "wealths": [1, 2, 3]
}
```

`firstEra` is the earliest selectable era. `modern` is the configured Art Deco era (1921–1945). Omit optional `lastEra` for availability in every later configured era, including future timeline additions. A lastEra, if supplied, is inclusive. Unknown eras fail closed for restricted materials. Records without either boundary preserve unrestricted legacy availability; old `era` values describe variation pools and are not retroactively treated as exclusive visibility rules. Brick therefore remains selectable in later eras.

`textureResource` registers a material without another C# catalog row. Built-in material IDs remain authoritative on duplicate IDs. Stable materialId references persist in paths and paint data; private source PNGs are installed once under Resources/Lots/PathMaterials. Import settings retain unchanged source bytes, repeat wrapping, sRGB, mipmaps and anisotropic filtering. Tiled sidewalk artwork uses a3m repeat (three approximately1m panels); the other five surfaces repeat at4m. These are supplied albedo images; no normal maps or geometry are fabricated.

## Shared rule

LotPathMaterial.IsAvailableInEra delegates to the metadata boundary. It is used by the lot path material picker, the shared pedestrian-path appearance modal, texture-paint palettes, recent-material/Enter completion, custom era-paving choices, district path creation and material changes, and runtime era-paving substitutions. The modal displays the actual effective era rather than a fixed Founder label. Lot authoring uses appearance overrides/preview era, otherwise its first era; district tools use the district's effective appearance era.

Variation retains existing legacy pool behavior and allows the new concrete family in Art Deco and later, respecting wealth membership and era bounds. Material locks do not permit new future-era material selection in older eras. Existing saved data continues to deserialize and render as authored; this is a content-selection rule, not an automatic destructive conversion of saved paving. Draft edits may choose an allowed replacement without changing the original until accepted.

Six supplied textures are registered: Light Gray Concrete, Weathered Gray Concrete, Warm Tan-Gray Concrete, Concrete Sidewalk Tiles, Cracked Concrete and Vintage Gray Concrete. All carry firstEra modern and shared wealths1/2/3. The previously added weathered-concrete-pavement and weathered-charcoal-asphalt also receive their Art Deco boundary for path use. Existing road menu era gating is preserved.

The data-driven material registry is warmed on the main thread before scene loading and during local editor texture preparation so saved-data validation can use the stable ID registry in background work.

## Verification

Isolated Play checks cover all six resources across all seven eras, legacy brick availability, unknown-era exclusion, background registry lookup, modern/Postwar/Contemporary variation at all three wealth levels, old-era variation exclusion, native Founder/Art Deco modal contents, texture-paint filtering, direct lot/district mutation rejection, Enter/recent-material filtering, material ID serialization and era-profile substitution. Existing lenient-path and Enter-completion regressions also pass. No normal player save is used for these checks.

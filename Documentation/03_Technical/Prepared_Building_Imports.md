# Prepared building imports

Building creation owns portable materials, LOD exports and reviewed native prefab preparation. Installation of a reviewed package should copy files and register metadata; it should not re-bake the building.

The reconstructed-building adapter lives in RebuiltOfficeCatalog and AuthoredBuildingLighting. Its private assets live under Assets/CityForge/Resources/Lots/RebuiltBuildings/<stable-folder>/. UnityCatalog.json supplies the stable ID, full label, era, category/subcategory, density/wealth enums and authored metre footprint. The original three tuples remain a compatibility fallback. Further compatible buildings use JSON records; no new hard-coded tuple is required.

Example private catalog record:

```json
{
  "folder": "ExampleOffice",
  "id": "cf-building-example-office-001",
  "label": "Art Deco · Example Office",
  "eraId": "modern",
  "additionalEraIds": ["postwar", "contemporary"],
  "group": "commercial",
  "subcategory": "office",
  "zone": "Commercial2",
  "wealth": "Medium",
  "width": 20,
  "depth": 20,
  "height": 24
}
```

The adapter expects Near.prefab and Medium.prefab with their own material mappings and clock component. Keep authored metres, stable GUIDs and IDs. Native policy remains Near Zoom0–2 / reduced Medium Zoom3–5. Do not replace a true authoring Far export with the native Medium alternate.

During authorized preparation, the private RebuiltOfficeImport harness can build/check one JSON-defined building from its portable FBXs/maps: put its stable ID in Library/RebuiltOfficeImport.request while out of Play. An empty request intentionally checks the whole set. Do not trigger it for unchanged prepared-package installation. This harness is private local tooling, not shipped public art or a player runtime dependency.

The cityforge-buildings skill contains the maintained package contract and unity_package.py helper. It seals reviewed runtime outputs against explicit authoring inputs, verifies stale/missing dependencies, previews staging changes and copies only changed files. It does not create portable materials or native prefabs, inspect Play mode, control Unity, or edit saved games. Installation still requires an authorized task and safe editor session. After changed assets import, use CityForge > Lots > Prepare local catalog when registration needs refreshing.

Metadata-driven registrations update their source catalog defaults; LotBuildingPropertiesStore applies saved user overrides separately. Invalid manifests log errors rather than inventing classification or silently resizing a building.

Verify affected material/geometry changes with native bounds, LOD switching, representative day/night views and lot roundtrip. Reuse valid previous evidence for unchanged assets. Runtime-adapter changes require broader checks; a routine new compatible asset does not justify re-baking/re-rendering every existing building.

Measured November24: repeat Python verification/staging for the three reconstructed offices took 0.05–0.12 seconds each, copied zero files and ran zero bakes. This measures staging only; it is not an end-to-end Unity import timing guarantee.

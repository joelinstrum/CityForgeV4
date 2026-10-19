# CityForge V4

CityForge's Unity 6 / URP prototype: region creation, continuous terrain and district views, water presentation, seasonal vegetation, and camera controls.

This is a **public source-only repository**, not a complete playable Unity project or a backup of the local asset library. Imported artwork, scenes, prefabs, materials, terrain data, images, and commercial packages are intentionally excluded. A fresh clone will need the local assets and third-party dependencies restored before it can compile and run. Some editor utilities also contain machine-specific paths from development studies.

Read [the main development handoff](HANDOFF.md) before continuing implementation. It identifies the approved meadow biome, seasonal tree library, retired prototype assets, and visual preservation requirements.

## Project configuration

- Unity **6000.1.12f1**
- Universal Render Pipeline **17.1.0**
- Development platform: macOS / Metal
- Registry dependencies: `Packages/manifest.json` and `Packages/packages-lock.json`
- Local dependencies include **WaveHarmonic Crest**, **MapMagic / Den.Tools**, and imported environment content. Obtain these separately with the appropriate licenses. Crest lives locally at `Packages/com.waveharmonic.crest`; the lock file records that embedded package, but its source is excluded here.

## Included

- CityForge runtime and editor C# code under `Assets/CityForge/Scripts`
- CityForge rendering shaders, shader graph, URP configuration, and custom environment integration shaders
- Project settings, registry package manifests, text design/technical documentation, and Python authoring tools
- Unity `.meta` files associated with included assets

## Excluded

- Unity caches and temporary state: `Library`, `Temp`, `Obj`, logs, and user settings
- Builds, player apps, debug-symbol output, captures, and local saved regions
- Third-party package source, imported asset libraries, customized copies of commercial foliage shaders, binary authoring files, screenshots, and reference artwork
- Asset installer archives, IDE caches, Python caches, local secrets, and signing files

The `.gitignore` deliberately allows only selected source paths inside the asset directories. Adding a new source directory may require a corresponding allow rule. Do not force-add imported asset packs to this public repository.

## Current development state

The first inland region-generation slice connects wizard topography, river count, streams, and seed to generated terrain and water descriptors. Created maps open in overview with four equal districts; creation settings persist with saved terrain. Temperate moisture and vegetation choices now affect the shared ecology field; alternate land shapes remain pending. Both generated-region views use the approved meadow renderer.

The grass/quilt rendering experiments were rolled back. The approved mountain and meadow shader sources are retained. Oceans and their approved surf settings remain separate from the region-generation work.

See `Documentation/README.md` for project documentation.

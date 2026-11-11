# Lot Editor

## Future developer and mod authoring workflow

Design notes recorded for later implementation. The final way players access the
Lot Editor remains undecided.

The original purpose of the Lot Editor is developer-mode content authoring, with
mod authoring exposed later. Editing content can affect gameplay balance, so the
proposed workflow separates editable drafts from published content used by saves.

- Draft packages allow editing and adding buildings, lots, classifications, and
  gameplay properties.
- Publishing creates an immutable, versioned package with a unique package ID
  and a content fingerprint.
- Players select published mods when starting a brand-new game. Adding a mod to
  an existing game would be disallowed under this proposal.
- Loaded mod packages are read-only. Altering or extending a published package
  creates a new draft and subsequently a new published version.
- Each save records its selected package IDs, exact versions, and fingerprints.
  Continuing a game uses those versions. Older published versions must remain
  available for the saves that depend on them.
- Fingerprint checks can detect local content changes. Recovery behavior for
  missing or altered packages still needs to be designed.

These restrictions are intended to prevent authoring tools from changing an
existing city's balance through the normal editing workflow. Package immutability
and fingerprints do not provide complete protection against deliberate local
file modification.

The current catalog extension mechanism is documented in
[Content Catalog Mods](../03_Technical/Content_Catalog_Mods.md). The package
lifecycle above is a future proposal and requires implementation.

## Model importing during authoring

A future model import tool could live inside the Lot Editor's draft authoring
workflow. The user selects a model and its textures, then assigns its era,
category, residential density, and wealth level where applicable.

Automated preparation could set up materials, register the building in the asset
catalog, and display its dimensions and a placement preview. Models vary in
scale, orientation, ground alignment, texture conventions, and polygon count.
A visual review should confirm the entrance direction, foundation position,
materials, and plausible dimensions, with flags for missing textures or unusually
dense meshes.

Growable buildings should retain their authored dimensions. Compatibility uses
the placement envelope, actual footprint, and entrance alignment. The Lot Editor
can show a selected compatible building at 70% opacity over the building slot to
help authors judge its size. The authoring preview is distinct from the engine's
building selection when the lot grows.

## Residential variety target

Aim for 10–15 houses per residential density and wealth combination. Three
densities and three wealth levels imply 90–135 buildings per era if every
combination receives the full target. Build toward that gradually, beginning with
Founders-era R1 $ homes. Shared compatible footprints and entrance positions let
multiple houses reuse authored lot layouts.

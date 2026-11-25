# Approved Marquee Tower / Majestic checkpoint

Marquee Tower (`cf-building-marquee-tower-001`) now includes the approved fixed
Majestic Hotel sign (`cf-sign-art-deco-majestic`) in its private native prefabs.
The double-sided blade projects perpendicular to the facade at 8.5 m height,
with about 1.3 m of elevated projection beyond the front of the reserved lot.
The building keeps its commercial/office classification, 20 × 20 m reservation,
stable IDs/GUIDs, opening pivots and original architectural geometry.

`AuthoredSignLighting` shares a clock between native tiers through the lot-object
wrapper. It follows district time and clock/zoning pauses without restarting at
LOD switches. The original 16-second cumulative MAJESTIC sequence, steady HOTEL
and border illumination, and source emission strengths are retained.
`AuthoredBuildingLighting` forwards preview time and excludes sign surfaces from
its recessed-room emission writer. Lot Editor enables the reviewed glow only
for sign-bearing previews. The world lighting profile is unchanged.

The approved material pass restores green entrance shrubs, green spandrel panels
and readable raised gold fan bars. Gold uses its own material/submesh assignment;
vertices, UVs, normals, bounds and triangle counts are unchanged. The native
package includes serialized mesh assets, so the local prepared-package staging
helper must include `.asset` payloads. Near remains Zoom 0–2 and reduced Medium
Zoom 3–5; true authoring Far is retained offline. No general sign picker,
repainting/siding system or persistent appearance-choice schema is introduced.

## Private asset checkpoint

The approved package is published through the local shared-library reservation
workflow as `Buildings/Era - Art Deco/Marquee Tower/v003`, with the stable building
ID above. Private models, textures, prefabs, serialized meshes and validation
captures stay outside this public source repository. Source masters and earlier
native kits are preserved; the current runtime appearance is represented by the
prepared native kit and its material/sign authoring stages.

Use the sealed kit for repeat imports. Do not rerun the older building converter
or native prefab preparation harness over this revision: those paths predate
its fixed sign and gold material regions. The published snapshot freezes its
explicit authoring inputs and includes its staging helper.

## Validation

179 isolated native Play assertions passed, including actual district and zoning
pause/resume, shared tier phase, bidirectional Zoom 0/2/3/5 visibility, exact source
emission strengths/sequence, preview time and native lot save/load/reinstantiate.
Both tiers were reviewed in the actual LotEditorViewport by day and night,
including shrub/panel close-ups. An ordinary house preview disables sign glow.
Installed runtime files match the reviewed private package; player saves and the
generated catalog were not copied or modified. No city-scale performance claim.

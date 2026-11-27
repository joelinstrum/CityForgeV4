# CityForge — editable buildings and sign mounts

Give this to the session creating the new building. Date: November 25, 2026.

## What to prepare now

Preserve the approved architecture. Prepare independently editable regions and sign
mounts during authoring, before combining meshes or baking atlases. Do not change
another chat's building or the live Unity/Blender session to satisfy this handoff.
Use the CityForge building skill, shared library reservation/version workflow, and
existing approved components/materials.

### Editable appearance regions

- Give regions stable semantic IDs: `siding`, `trim`, `metal`, `shutters`, etc. Only
  expose regions that make architectural sense for this building.
- Put each paintable region in its own material/submesh or provide a precise mask.
  Keep window glass, frames, lit interiors, gold ornament, signs, door hardware and
  unrelated surfaces out of that region. A whole-building Tripo atlas is not a
  paintable siding region.
- Prefer neutral albedo for regions intended for paint; keep tonal detail and the
  original normal, roughness/metallic and emission data. Record the approved default
  palette and original finish so Original restores the exact authored appearance.
- Name material slots consistently across all tiers, or supply an explicit per-tier
  mapping. Keep UV density and physical texture scale consistent. Preserve the same
  surface IDs in Near, Medium and offline Far.
- Prepare optional facade parts as separate identified meshes. Do not merge future
  awnings/shutters/signs into structural geometry or bake interchangeable lettering
  into wall textures. Height editing is deferred; do not stretch the model for it.

Current native implementation: Marquee's separate `Near-Trim` / `Medium-Trim` slots
are paintable. Other buildings require explicit integration of their prepared region
mapping. Arbitrarily named slots are not automatically made editable by the game.

### Two initial sign types

1. `FrontFacing`: fascia/wall sign. Face lies parallel to the wall; its visible front
   points outward from the facade.
2. `ProjectingBlade`: face stands perpendicular to the wall, readable from along the
   sidewalk on both sides. Require correct front/back geometry and UVs, plus a real
   mounting bracket/interface. Do not rotate a one-sided fascia sign and call it a
   blade sign.

Prepare actual named mounts, not an unrestricted position slider. Derive their
positions from the finished facade and move them with any tile-fit revision.

## Native mount coordinate contract (version 1)

Store all native values in **Unity building-local metres**, after export-axis/unit
conversion. Blender masters remain Z-up; do not copy Blender coordinates directly.
Use a stable building ID, stable mount ID and the same logical placement in every
native tier. Position is the attachment pivot; Euler rotation is Unity XYZ degrees.
Sign local +Y is up and local +Z is the visible face normal.

- FrontFacing: pivot at the bottom-centre of the wall/back attachment plane; local
  +X runs across the facade, local +Z points outward from the wall.
- ProjectingBlade: pivot at the bottom of the wall-side attachment edge, centred
  through thickness; local +X projects outward from the wall, and +/-Z are the two
  readable faces. Include hardware in the declared geometry envelope.
- `wallOutward` is the facade outward direction in building-local coordinates.
  It must agree with rotated +Z for FrontFacing or rotated +X for ProjectingBlade.
- `maxSignSize` is the allowable sign-local X/Y/Z envelope (face width, height,
  thickness), including hardware. `maxProjection` separately limits how far the
  complete sign/hardware extends along the facade's outward direction.
- `heightAboveWalkway` measures the attachment's bottom above the actual pedestrian
  surface. Record required ground clearance on the sign. These are asset constraints,
  not a claim of compliance with a legal clearance standard.
- Use a stable `mountingInterface` string shared by the socket and compatible sign
  hardware. Different interfaces are incompatible even if the boxes fit.
- Mark `clearanceReviewed` only after checking facade/window/door collisions,
  outward-opening entrance swings, sidewalk space, roof edges, brackets and the lot
  boundary. Record deliberate elevated overhangs explicitly for review.
- `allowedSignIds` can restrict landmark/tenant identity. An empty list permits any
  otherwise compatible sign. An existing default sign must have a stable sign ID.

Runtime component types: `BuildingSignMountProfile`, `BuildingSignMount`,
`BuildingSignAsset`, `BuildingSignCompatibility`, under CityForge.Lots. The profile
belongs on each prepared native building tier; sign assets are separate reusable
native prefabs and a descriptor. Put reusable native sign descriptors under a
Resources/Signs folder so the Signage compatibility browser can discover them. Keep
the building mount descriptors identical across tiers.

## Preparation metadata example

Include an authoring section like this in Building.json. Replace every example value
with measurements. This is preparation metadata; the existing importer does not
currently auto-convert this section into native components.

```json
{
  "customization": {
    "contractVersion": 1,
    "appearanceRegions": [
      {
        "id": "trim",
        "defaultFinishId": "original",
        "materialsByTier": {
          "Near": ["Example-Trim__Near"],
          "Medium": ["Example-Trim__Medium"],
          "Far": ["Example-Trim__Far"]
        },
        "neutralAlbedo": true,
        "excludedRegions": ["glass", "gold", "signage", "interiors"]
      }
    ],
    "signMounts": [
      {
        "id": "front-fascia-01",
        "label": "Entrance fascia",
        "kind": "FrontFacing",
        "position": {"x": 0, "y": 3.5, "z": 8},
        "eulerAngles": {"x": 0, "y": 0, "z": 0},
        "wallOutward": {"x": 0, "y": 0, "z": 1},
        "maxSignSize": {"x": 3, "y": 0.8, "z": 0.2},
        "maxProjection": 0.2,
        "heightAboveWalkway": 3.5,
        "mountingInterface": "cf-wall-fascia-v1",
        "clearanceReviewed": false,
        "allowedSignIds": []
      }
    ]
  }
}
```

For a facade facing building +Z, a blade mount can use Unity rotation Y=-90 degrees:
its local +X then points outward +Z. Verify the axes after export. Do not use this as
a guessed attachment position.

## Sign package requirements

Keep signs separate from buildings. Record stable ID, label, FrontFacing or
ProjectingBlade, actual sign-local size, measured outward projection, minimum ground
clearance, mounting interface, double-sided status, default size and provenance.
Provide aligned Near and Medium native prefabs; retain offline Far. Avoid a competing
LODGroup: CityForge owns Near at Zoom 0–2 and Medium at Zoom 3–5.

Carry original emissive regions and Animation.json into the native lighting path.
Keep original timing/brightness; both sides must stay in phase. Integrate with the
existing district clock/pause and shared near/medium sign clock, not a replacement
lighting rig. Check day, dusk, night and both LOD transition directions.

All six supplied Deco signs now have reviewed native tower-sized blade descriptors.
Five fit Marquee’s front blade mount; Paramount is listed as incompatible because
it exceeds that mount’s available width. Original preserves the approved fixed
Majestic. Source 6m authoring packages remain unchanged; native variants are 8.5m.
Front-facing signs require their own reviewed descriptors and compatible mounts.

## Persistence and delivery

Future selections must persist by building-object ID + mount ID + sign/finish ID.
Lot Editor saves into the lot; in-game saves only the placed instance. Preserve
original/default behavior for older saves. Never edit a shared prefab/material as a
response to a player's per-instance choice, and never rerandomize on reload or LOD.

Deliver: versioned master, portable textures/materials, three authoring tiers,
per-tier native mappings, stable region/mount metadata, default appearance/sign IDs,
measured bounds and clearance notes, and matched day/night/corner review renders.
Use the shared repository reservation and registration workflow; preserve prior
approved versions and other sessions' reservations. Before actual attachment, recheck the transformed world envelope, instance scale,
site elevation and pedestrian/door clearance; static metadata fit alone is not
placement acceptance. Native integration and collision acceptance remain separate
from offline authoring approval.

## Browser integration additions

The native browser uses one reviewed building mount profile per native tier. Set
`mediumTier=false` on Near and `mediumTier=true` on Medium; all mount descriptors
otherwise match. When an original sign is already part of a building, record
`originalSignRootPath` relative to that tier's profile root. Use a common logical
root name in both tiers. `retainedHardwarePaths` lists children of that original
root that must remain when changing or removing its sign. Blank sockets leave
`defaultSignId` and `originalSignRootPath` empty, with no retained paths.

Runtime choices are stored in `buildingAppearance.signs`, an array of stable
`mountId` / `signId` pairs. An absent pair means Original; the reserved sign ID
`none` removes the face while retaining the prepared mounting hardware. Selection
previews are drafts until Apply. Lot and placed-instance save scopes remain distinct.

Native sign descriptors include a thumbnail and actual native size. The first six
blade descriptors use an 8.5 m tower-size variant, preserving source proportions;
the original 6 m authoring packages remain unchanged. Additional native sizes need
distinct descriptor IDs. Do not silently rescale a descriptor already used by saves.
The Marquee bracket keeps the original sign as its default; replacement signs must
fit its measured mount envelope. Wider signs remain visible through the browser's
Show incompatible option, with selection disabled and a reason.


## Native tint binding contract (Kingsley pilot)

Use reviewed `BuildingTintProfile` components on Near and Medium. Each binding
contains stable region `trim` or `awnings`, an exact native material reference,
and an authored neutral albedo preserving tonal/fabric detail and UV layout.
Never infer regions from generic slot names or neutralize a whole facade atlas.
Keep normal, roughness/metallic, glass, gold, sign artwork and emission intact.
Optional saved `awningColor` complements existing `trimColor`; empty restores
the exact Original inputs independently per region. Native support is checked
on both tiers. Kingsley now supports trim/awning tints; wall tint remains deferred.


### Reviewed wall paint (2026-11-25)

The editor labels buildings with explicit reviewed paint regions **Paintable**. Green Cornice Bay exposes Brick walls; Sandstone Art Deco Townhouse exposes Stone walls (ashlar and foundation). Near and Medium bind the same wall materials to neutral albedos. White and Yellow join the wall palette; Original restores the exact original finish. Windows, doors, bay frames, copper and carved ornament are excluded. Unmapped buildings cannot open appearance customization unless they have explicit reviewed sign mounts. Existing wallColor and saturation save fields persist these choices. The authored sandstone Far atlas is not paint-enabled and is not used by current native tier routing.

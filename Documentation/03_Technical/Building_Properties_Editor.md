# Building properties editor

Open **Properties…** beside a selected building in the Lot Editor, or enable
**Edit building properties instead of placing** in the building browser and choose
a model. Player landscaping mode does not expose catalog authoring.

The editor reuses the Civic/Ploppable browser's sunset artwork, serif headings,
rounded panel and section skins, cream text and gold action/selection states.
It includes a rotating daylight preview and effective dimensions, with responsive
fitting for smaller screens. The entrance/dimension and row-alignment dialogs
share the same styling.

Editable catalog defaults:

- Building name and optional description; description is also searchable.
- First era, category, use/subcategory, wealth and growable type.
- Uniform default scale, 10–500%, with five-percentage-point steppers.

Entrances & dimensions opens the existing doorway marker, entrance type/width,
model facing, base measurements and measured-door-height editor. Row alignment
opens the existing joining-wall and front-step tools. These measurements remain
in base-model coordinates at 100% scale. Pending catalog drafts survive a return
from these tools. Their own Save actions remain independent of the catalog form.

Save Properties persists the catalog form. Cancel/close discards its pending
changes. Restore Defaults stages the imported catalog values and removes that
model's catalog override when saved; it does not erase the separate entrance or
row measurements. Invalid names, eras, category/use combinations, growth types,
wealth choices and nonfinite/out-of-range scales cannot be applied. Existing
Founders/Industrial low-wealth R3 restrictions still apply.

## Persistence and placement

Overrides are local application data in `BuildingProperties/catalog.json`, keyed
by the immutable catalog ID, with a versioned document and atomic replacement.
They are separate from the generated private catalog, BuildingInfo files, lot
library and region saves. Catalog loading applies cached overrides to a session
clone, leaving the generated Resources asset unchanged. Regenerating that asset
does not erase overrides. A read failure is reported rather than silently
overwriting existing settings on the next save.

New manual placements use DefaultPlacementScale. Single growth stores that scale
on its new lot object and scales its entrance anchor and physical-fit checks.
Rows scale canonical joining-wall/step coordinates and the physical envelope,
then store the same scale on each new object. Packing never stretches buildings
to consume a leftover gap. Row-alignment editing explicitly reads unscaled
coordinates to prevent repeated scaling when saving markers.

Existing saved objects retain their explicit scale. Existing saved lots retain
their own category/wealth/era; editing a model does not rewrite those lot records.
Catalog identity/classification changes affect future browsing and growth pools.
The existing Harbor House placement multiplier is preserved when no new building
scale is configured. No region/lot schema migration or imported artwork changes
are required.

## Targeted verification

BuildingPropertiesQA in the isolated project covers persisted JSON reload into a
fresh catalog session; source Resources immutability; edited name, description,
era/category/wealth/growth eligibility; invalid numeric/R3 wealth rejection;
new single-growth scale; scaled row packing, joining walls and cardinal frontage
fit; unchanged saved-instance/open-lot scale; selected-building entry point;
UI save/cancel/reset and steppers; era dropdown; and both advanced dialogs.
1920×1080 and 1366×768 captures were visually inspected. All property writes use
an isolated temporary root, and no player lot/region/BuildingInfo saves are written.
Evidence: November20 `ple/outputs/Building-Properties`.

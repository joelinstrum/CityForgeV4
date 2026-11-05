# Streams and rivers

Add Water Bodies / Stream or River now uses an open PointWire ribbon. Click points,
press Enter to create, finish at a district edge to carry into its neighbor. Start
at the downstream end in that neighbor to extend the saved route. Hint, Backspace
and Escape supported. See Player_Waterways.md. Remove water remains menu-only.
Earlier notes below saying streams/rivers are menu-only are historical.

# Lake filling

Closing Add Water Bodies > Lake / Pond now creates curved banks, a basin and calm
water. See Player_Lakes.md and the reusable PointWire.md. Other water tools remain
menu-only. Earlier transient-only lake descriptions below are historical.

# Lake / pond outline utility

Add Water Bodies > Lake / Pond now opens the blue-arrow marker/string outline tool.
Click the first marker to close after at least three markers; Backspace undoes,
Escape cancels. This is a transient sketch only. See Lake_Outline.md. Other water
choices remain menu-only; the earlier all-menu-only description below is historical.

# Water menu foundation

Water opens Add Water Bodies / Remove Water Bodies. Each leads to Lake / Stream /
River choices and Back. Switching mode/category resets the water subtype selection.
WaterBodySelected(bool adding,int kind) is the future integration hook (0 lake,
1 stream,2 river); WaterTypesOpen and WaterBody expose transient navigation state.
No water editing backend is armed and no map geometry is changed in this first pass.
Oceans are excluded. Selection feedback explicitly says editing tools come next.
This supersedes historical Lake/Pond/River slider content below.

# Forest Remove and Add Woodlands are live

Popups are doubled in size. Forests offers Add Woodlands / Remove Woodlands / Add trees, in that order.
Remove arms a tree-with-X cursor and red200m-circumference circle; Tab toggles400m.
Click/hold and drag removes trees, and normal Save District/Region persists the clearing. Opening
the category alone does not arm it; reselecting the category, Escape or other tools
cancel it. Add Woodlands uses the same two brush sizes and drag path with a green
tree-plus cursor. It plants the approved mixed trees or selected species. Individual
Add trees and other editing categories remain configuration-only.
See Forest_Removal.md for persistence, cache, rendering and validation contracts.
See Forest_Planting.md for planting. This supersedes the historical settings-only
descriptions below for Remove and Add Woodlands.

# Category popups and configuration

Each toolbar category opens one NatureToolPopup above the icons. The shared window
has a small heading, left option icons, and a right-side title, description and
settings. Category switching replaces the contents; repeated toolbar click, X or
Escape dismisses it. The main toolbar remains icon-only and transparent.

- Terraforming: Raise / Lower / Level; brush size and strength/blend.
- Water: Lake / Pond / River; area or width, depth.
- Foliage: Individual tree / Woodland brush / Ground foliage; type, brush size.
- Animals: Individual / Herd; available wildlife species, herd size.
- Sun: Seasons / Weather; existing season and precipitation actions.

Settings persist per category/option for the current view session. Value and
ChoiceIndex expose selections; SettingChanged and OptionChanged are integration
hooks. They do not modify terrain, create water, plant foliage or spawn animals.
Those editing backends, undo and persistent per-district editing remain future work.
Sun actions use the existing live APIs and retain climate constraints.

The updated toolbar check verifies popup content, option switching, setting retention,
Sun callbacks, close behavior and unchanged canonical planting. Menu: CityForge /
Nature / Check icon toolbar and popups. Runtime screenshots verified each category.

The earlier toolbar foundation notes below describe the prior icon-only flyout;
category popups now supersede that Sun layout.

# District nature toolbar

The district HUD uses NatureToolbar: five icon-only categories in the order Terrain,
Water, Trees, Animals, Sun. The background is transparent. Hover highlights and a
short gold underline provide feedback. Selection is transient UI state, not map
content. ToolSelected exposes the current NatureTool for future editing backends.
The first four categories currently select only; brushes/placement are not yet built.

Sun opens an icon-only season/weather flyout bound to PrototypeRegionView.SetSeason
and DistrictWeather.SetWeather. Summer/Fall/Winter and Clear/Rain/Snow use existing
climate rules. Weather remains temporary; season follows normal region persistence.
Old season/weather HUD objects stay hidden for legacy integration tools. The bottom
instruction text is hidden in districts. Overview hides the toolbar; district entry
restores it. Escape dismisses selection before normal district-to-region navigation.

Runtime art is the private generated Resources/UI/NatureToolbarIcons.png atlas:
4 columns × 3 rows, ordered mountains/waves/trees/deer, sun/summer/fall/winter,
clear/rain/snow/unused close. Keep transparent alpha, clamp/bilinear, no mipmaps,
no NPOT scaling and uncompressed import. This source-only repo excludes UI bitmaps;
restore the local atlas along with other private runtime artwork.

CityForge / Nature / Check icon toolbar runs live callback, availability, layout and
population-preservation checks in an open district. It writes Library/NatureToolbarChecks.txt
and restores the original season/weather. No normal player save is written.

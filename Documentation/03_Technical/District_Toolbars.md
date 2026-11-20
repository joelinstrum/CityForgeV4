# District toolbars

The main district Nature and City menus share category-card dimensions, spacing,
vertical position and the Nature/City switch position. Nature has five centered
cards: Terrain, Water, Forests, Animals, Sun & Sky. Category cards use the existing
dark City frame, gold selected frame, gold icon and serif caption. Hover and pressed
colors match City. Nature/City switching replaces the category row without moving
the mode switch. Nature remains district-only and hides during district loading.

NatureToolbar keeps its existing selection, dismissal, tool notifications and
NatureToolPopup. The original popup contents, season/weather icons, forest/water
and actor tools are unchanged. Clicking a category again dismisses its popup;
reopening an armed category still follows the previous tool behavior.

DistrictCategoryButtonArtwork is a small MaskableGraphic which draws three strips
from the City's existing Zones card crop. Left and right preserve the actual
rounded frame; a narrow empty side column supplies the center without sampling
baked category text/icons. It shares the TownButtonArtwork shader for matte removal.
No image editing or generated art is required. The original Nature icon atlas and
separate captions supply category content. The graphic swaps dark/gold textures
only when selection changes, preserving ordinary Button hover/press transitions.
NatureToolbar owns and disposes its runtime artwork material and dynamic serif font.

Private NatureToolbarQA renders Nature normal, selected/hover and City comparison.
It checks identical card dimensions/row placement, the matching hover/press palette,
gold state, repeated-click dismissal, all category routes, both mode switches and
shader errors. It uses an isolated disposable canvas and writes no player files.


## November19 placement and script controls

Cmd+S → Help opens scoped, scrollable, executable script choices. District commands
include Place lot, Let it rain/snow, seasons and existing encounters; region choices
include era messages and seasons. Persistent lot placement strip is hidden; Tab
rotates placements and selected lots offer Customize lot. Clock is top right beside
Back/Save and toggles pause on click (orange hand while paused). Legacy lower-right
controls are hidden; weather/time remain in Nature. New districts can open Civics
to place the4x4 Town Center under Government after the capital is established.


## Wealth and shared brass actions · November19

Editor/development builds expand Zones vertically with Low/Medium/High wealth
radio buttons; normal builds omit the row. Both district and region reuse the
53px brass Save/Back art and hover glow. District saves show the same animated
progress strip as region saves, alongside the existing status message.


## Water Features and available layouts · November19

Nature Water now uses a centered modal with independently layered supplied state
frames and feature illustrations. Select Stream/Canal/Pond/Lake/Marsh then Draw
Feature; River and Remove remain accessible. Shape editing is visibly unavailable.
Zone cards show block spaces and matching buildable saved layouts separately.
Counts refresh for type, density, wealth and road-facing dimensions.

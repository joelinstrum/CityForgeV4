# Civic Buildings popup

Open City → Civic Lots after founding a town. The browser lists only user-created
saved Civics lots, with category and historical-era filtering. Imported building
models are available in the Lot Editor; the game does not synthesize playable lots
from them. Saved lots retain their authored names, dimensions, objects, requirements
and references. Distinct saved lots may use the same building without deduplication.

The eight service categories reuse ContentCatalog IDs; Founder's Buildings map to
Government. Chapel and Constable Station map to Culture and Public Safety. Empty
categories remain available with a clear message and disabled Place button. Saved lots use the civic subcategory IDs from ContentCatalog.

TownCivicsPopup shares the existing transportModal input gate and close path.
Camera navigation pauses while open and restores its prior state on dismissal.
Select a card, rotate in45-degree steps, then Place Building to arm the standard
lot placement tool. Existing site, water, district, overlap and save rules apply.
Esc or Cancel dismisses the browser; Esc also cancels armed placement normally.

The supplied Icons.png and Sunset.png remain under ignored private UI resources.
Source-coordinate atlas crops normalize independently of texture import size.
LotAssetThumbnails supports optional resolution, transparent daylight backgrounds,
front-view framing and rotation; default callers retain their original behavior.
Only visited categories/angles render, results cache until close, and all render
textures/stage objects are disposed. Actual lot metadata is shown; coverage and
service/economic simulation are not implemented by this browser.

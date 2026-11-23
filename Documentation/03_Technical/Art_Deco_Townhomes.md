# Art Deco R2 medium-wealth townhomes

Twelve supplied FBX archives are prepared in the existing private V3Buildings workflow.
Registrations use Residential2, Medium ($$), and timeline id `modern` (Art Deco Era).
These are building models for the Lot Editor and growable/row pools; they do not fabricate saved playable lots.

| Model | Folder | Width x depth x height (m) | Lit front windows | Near / distant triangles |
| --- | --- | --- | --- | --- |
| Green Cornice Bay | R2ArtDecoTownhome01 | 7.32 x 17.88 x 16.80 | 8/21 | 21,658 / 19,584 |
| Brownstone Bay | R2ArtDecoTownhome02 | 6.13 x 17.60 x 16.00 | 7/17 | 21,767 / 19,571 |
| Brick Three Bay | R2ArtDecoTownhome03 | 6.13 x 17.58 x 15.80 | 5/13 | 23,754 / 18,530 |
| Green Cornice Wide Bay | R2ArtDecoTownhome04 | 5.90 x 16.50 x 13.40 | 5/14 | 22,500 / 19,344 |
| Brownstone Three Story | R2ArtDecoTownhome05 | 5.91 x 17.67 x 13.50 | 5/13 | 21,092 / 18,778 |
| Limestone Shutters | R2ArtDecoTownhome06 | 4.96 x 14.96 x 13.60 | 5/13 | 21,611 / 19,273 |
| Brick Paired Bays | R2ArtDecoTownhome07 | 5.22 x 15.37 x 13.80 | 4/11 | 23,044 / 21,354 |
| Brick Three Bay Steps | R2ArtDecoTownhome08 | 5.75 x 16.65 x 13.50 | 4/11 | 22,677 / 19,739 |
| Limestone Deco Crown | R2ArtDecoTownhome09 | 5.29 x 16.85 x 13.80 | 3/8 | 19,735 / 18,391 |
| Brick Projecting Bay | R2ArtDecoTownhome10 | 5.64 x 13.93 x 13.80 | 7/18 | 21,771 / 19,547 |
| Brick Entry Bay | R2ArtDecoTownhome11 | 5.10 x 12.16 x 12.20 | 5/14 | 22,786 / 19,684 |
| Green Cornice Double Bay | R2ArtDecoTownhome12 | 6.12 x 13.72 x 12.40 | 5/14 | 21,961 / 19,443 |

Source color/normal artwork and original proportions are retained. Physical sizes are uniform doorway-proportion estimates, not measured building dimensions. Models are centered at ground level and retain the existing +Z-front import convention; row wall/front-step defaults are derived from source bounds. Building Properties can adjust them.

Window masks select whole windows on all four facades: approximately36-41% of front openings and a minority of side/rear glazing, and retain sash gaps through triangle/UV-island baking. Unselected windows remain unlit. Every identified entrance fixture has a mask and a ray-picked warm local light pool; models without fixtures do not receive invented lamps. Existing ResidentialWindowLights and LamppostLight supply noon/evening/night behavior. No per-window simulation or new lighting system is added.

Model.fbx serves Zoom0-2, Distant.fbx Zoom3-5. Reduction preserves the full front, roofs, foundation, footprint extremes, selected window geometry on all facades and UV seams. Rear/side short-edge collapses use manifold and face-orientation checks. Packed source metallic/roughness uses the existing restrained0.35 smoothness material setup. All source art, masks, FBXs, materials, ImportInfo/WindowGrid and LocalCatalog remain private/ignored.

Verification covers catalog/era/wealth/row eligibility, pivots and bounds, reduced triangles, actual bidirectional zoom changes, both-tier time-of-day emission, lamp count/UV alignment, isolated lot roundtrip and four-corner/front near/far renders. Final evidence is in the chat outputs/ArtDeco-Townhomes folder. No player save is modified.

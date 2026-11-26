# Road diagonal snapping

With Snap to Tile Centers enabled, road drawing selects the nearest horizontal, vertical or 45-degree heading from the mouse displacement relative to the last marker. Longitudinal steps use the existing 4m tile pitch in that heading. Final edge alignment projects onto the corresponding rotated grid; adjoining runs intersect at a shared corner. The map grid overlay itself remains axis aligned.

Street junction and existing-road endpoint snaps retain priority. Disabling snapping retains free drawing. Existing cardinal road edge alignment is unchanged. Dragged points preserve the original incident run orientations, including diagonals. Extensions translate their newly aligned guide to keep the saved endpoint fixed. Saved points and the existing gridAligned flag carry the result; no save schema change or stored-road migration.

Verified in isolated Unity: 899 assertions covering old cardinal behavior across 3/4/12/20m widths, eight headings, mixed corners, diagonal loops, point dragging, mouse spacing, extension continuity and record serialization. Native road ribbon geometry rendered and inspected. No live map or player save used for testing.

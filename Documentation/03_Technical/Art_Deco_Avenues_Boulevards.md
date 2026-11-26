# Art Deco avenues and boulevards

Art Deco avenues and divided boulevards

Transport → Roads → Avenues now offers Concrete Avenue (12m roadway with sidewalks) and Divided Boulevard (20m total road width: two8m paved carriageways around a4m median, plus sidewalks). Available from Art Deco onward; earlier-era menus retain their existing choices. Both use the existing weathered concrete surface.

A new boulevard defaults to city-tree-small planting in its middle. Existing tree-style choices work for median trees; an explicit No trees choice is honored. Avenue planting remains curbside. Median grass uses the existing textured tileable-grass material and concrete curb ribbon. Owned meshes/materials follow the existing sidewalk rebuild/cleanup path, with no idle scans. Medians stop before endpoints/intersections; automatic tree clearance protects intersection openings and lot entrances. Closed median circuits join at their seam.

Vehicle routes use a paved-side alignment, offset6m from a20m boulevard's center. It rejoins original road endpoints and crossing centers through the median openings; bridge routing and existing vehicle safety checks are retained. The separate motor-fleet/driver/following implementation is outside this checkpoint. Road authors' saved centerlines are unchanged.

Wide road validation and restore preserve12m/20m widths; repaving chooses the matching width. Stable surface IDs are art-deco-concrete-avenue and art-deco-divided-boulevard. No new save fields or player-save migration. Existing road IDs/width behavior remain unchanged.

19 native QA checks passed on a Burlington copy: actual median geometry/tree models, curbside versus middle planting, lane/end/junction alignment, closed circuit, intersection openings, serialization/native region binary roundtrip, restore widths, Art Deco menu/earlier-era exclusion, actual placement width selection and default planted-tree choice. Native scene and modal previews inspected. No original save was written. Seven tested source files installed outside Play; no new artwork required.

# Asset grooming checkpoint — November24,2026

The reconstructed Continental Insurance, Art Deco Office001 and Marquee Tower are integrated through RebuiltOfficeCatalog and AuthoredBuildingLighting. See Prepared_Building_Imports.md for the private JSON-driven package contract. Stable IDs, metres, reserved footprints and native Near Zoom0–2 / reduced Medium Zoom3–5 are retained. Private art and generated catalogs remain outside the public repository.

The latest user direction for Marquee Tower is deliberately narrow: keep its current reconstructed architecture, make the planter foliage green, and reduce excessive black. A wholesale reconstruction to match the concept was not requested. Preserve the sign bracing/socket, independently editable olive trim, recessed rooms and opening pivots. This color grooming remains pending; technical import checks do not mean the current appearance has final visual approval.

Compare the source materials with the actual native Lot Editor before adjusting lighting. Some simplification predates Unity conversion. Preserve intentional dark sign recesses, base and contrasting trim while checking foliage, bronze/gold, glass and interior readability separately. The current Marquee detail-material recipe is empty; most opaque materials were collapsed to a body atlas plus olive trim. Investigate this mapping rather than assuming exposure is the only cause.

The Lot Editor now has its own neutral day rig, authored zoning self shadows, soft shadows, SMAA, aspect-preserving1.5x sampling capped at4096, and preview-clock/fixture-layer integration. Approved world lighting is unchanged. Keep all future asset-specific corrections local.

Existing isolated native validation covered bounds/catalog classification, bidirectionalLOD switching, both-tier day/evening/night emission, native lot save/load, actual viewport clock and lamp masks, zoning shadows, render-target aspect and representative legacy house/tree previews. These checks establish functionality, not exact concept fidelity or city-scale performance.

For subsequent grooming, retain a before image, change only the affected asset, review both tiers/day/night in the real viewport, preserve stable GUIDs/IDs/footprints, and reseal its prepared package after visual review. Verify and stage unchanged kits without re-baking. An empty private preparation request intentionally processes all buildings; use a selected stable ID for focused work. Check current unsaved Play state before installation; earlier discard permission does not cover a new session.

A detailed local handoff, private library paths, QA project/tool locations, current limitations and evidence are retained in the originating chat outputs/Asset-Grooming-Handoff/Handoff.md. The source-only PR deliberately excludes models, textures, prepared native prefabs, local skill files and unrelated changes belonging to other sessions.

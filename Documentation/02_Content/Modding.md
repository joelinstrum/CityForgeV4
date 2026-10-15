# Modding

## Design intent

CityForge should be designed so its historical and cultural identity can change. The default American 1750-to-present package is one setting. Other packages should be able to provide timelines, regional context, historical headlines, names, architecture, and relevant cultural sites. Governor category names, score bands, overall titles, and distinctions should also be moddable.

Modding should preserve the core game contract: calendar bounds plausible availability; local conditions determine adoption; players build places with histories; economic and infrastructure systems stay understandable. A different history package may change content and emphasis without requiring a different game engine.

Possible extension points include:

- Historical events and headlines, with place, date, and source metadata.
- Era labels and availability windows for assets or services.
- Region and culture definitions, including pre-existing places.
- Architecture and other visible content.
- Governor classifications, descriptions, distinctions, and criteria.
- Procedural local-news templates and names.

These are **candidate interfaces**, not a finalized schema or promise that every extension point will ship in the first version. Factual content needs validation; sensitive topics need editorial standards. Mods should be able to distinguish historical claims from fictional local flavor.

## Open questions

- What is the smallest stable content schema for an initial mod package?
- How are conflicting packages, dates, and localization handled?
- Which criteria for reputation can be data-defined safely and clearly?

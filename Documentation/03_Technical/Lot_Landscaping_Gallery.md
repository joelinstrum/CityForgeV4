# Lot Editor Landscaping Gallery

Landscaping uses the shared responsive 1640 × 900 catalog gallery alongside Buildings, Trees and Fences. Its catalog is the existing private `Landscaping` category, filtered by actual group metadata. No generated or reference-image plants are registered as new models.

The gallery includes search, group filtering, footprint/name/history sorting, grid/list views, thumbnail sizing and pagination. Single-click updates selection without destroying the card; native double-click arms the existing placement flow. Rotation and expanded previews use the shared daylight thumbnail renderer with silhouette framing. Size variation retains the existing placement flag.

Favorites and used tracking are separated into `CityForge.Landscaping.Favorites` and `CityForge.Landscaping.Used`. Recent placement uses existing recent asset IDs. Browse and preview operations do not edit the lot. Player customization retains the existing cosmetic asset permissions.

Private artwork: `Resources/UI/LotLandscapingGallery/Garden.png` and `Landscape.png`, copied from user-supplied artwork. These remain ignored by the source-only repository. Existing lot definitions and procedural region generation are unchanged.

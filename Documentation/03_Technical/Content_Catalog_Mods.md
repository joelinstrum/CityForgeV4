# Modding the content catalog

The built-in eras, historical periods, lot categories, and subcategories live in
`Assets/CityForge/Resources/Config/ContentCatalog.json`. The region creation
timeline and Lot Editor use the same era definitions. The Buildings picker and
lot inspector share the parent category definitions.

To extend the catalog without editing the built-in file, add a JSON file under
`Mods/ContentCatalog` inside Unity's `Application.persistentDataPath` directory.
Create the folders if needed. The game reads the catalog on first use. Restart
the game or re-enter Play Mode to pick up changes.

```json
{
  "schema": 1,
  "categoryTypes": [
    { "id": "community-use", "conditions": [] }
  ],
  "categories": [
    {
      "id": "community",
      "name": "Community",
      "categoryType": "community-use",
      "subcategories": [
        { "id": "meeting-hall", "name": "Meeting Hall" }
      ]
    },
    {
      "id": "civics",
      "subcategories": [
        { "id": "library", "name": "Library" }
      ]
    }
  ],
  "eras": [
    {
      "id": "future",
      "name": "Future Era",
      "periods": [
        { "name": "Future Settlement", "firstYear": 2027, "lastYear": 2049 }
      ]
    }
  ]
}
```

Use lowercase letters, digits, and hyphens in IDs. Keep IDs stable after a mod
ships so saves continue to resolve even if display names change. IDs are unique
within their list. Subcategory IDs are local to a parent. Existing saves that use
the original display names can resolve them through the built-in legacy aliases.

The game applies mod files in alphabetical filename order. An existing ID
updates the fields supplied by that file. Subcategories merge by ID, while an
explicit `periods` or `conditions` array replaces the previous array. Era and
period year ranges must be ordered and nonoverlapping. A rejected file is logged
and the previous catalog stays active.

`categoryType` references a reusable type by ID. Its `conditions` array is reserved
for future availability rules and can be left empty. The current game reads this
data without evaluating conditions or running scripts. Adding a category makes it
available in the editor. Models for that category still need catalog entries.

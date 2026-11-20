using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using CityForge.UI;

namespace CityForge.World
{
    internal static class CivicBuildingChoices
    {
        internal static readonly (string Id, string Name, string Description)[] Categories =
        {
            ("government", "Government", "Public halls, councils, and courts for your town."),
            ("education", "Education", "Schools and places of learning."),
            ("healthcare", "Health", "Buildings for healthcare and public wellbeing."),
            ("public-safety", "Safety", "Constable stations and public safety buildings."),
            ("culture", "Culture", "Chapels and places for worship, arts, and community life."),
            ("parks", "Parks & Recreation", "Public gardens and places for recreation."),
            ("maritime", "Maritime", "Civic buildings serving the waterfront."),
            ("infrastructure", "Infrastructure", "Public works and town infrastructure.")
        };

        internal static List<LotDefinition> Collect(IEnumerable<LotDefinition> saved) =>
            (saved ?? Enumerable.Empty<LotDefinition>())
                .Where(lot => lot != null && LotClassification.FindCategory(lot.category)?.id == "civics" && Category(lot) != null)
                .OrderBy(lot => lot.name, StringComparer.CurrentCultureIgnoreCase).ToList();

        internal static string Category(LotDefinition lot)
        {
            var child = Array.Find(LotClassification.ForCategory(lot.category), c =>
                c.id == lot.subcategory || c.name == lot.subcategory || c.legacyName == lot.subcategory);
            string id = child?.id ?? lot.subcategory;
            if (id == "founders-buildings") return "government";
            return Categories.Any(c => c.Id == id) ? id : null;
        }

        internal static string CardDescription(LotDefinition lot) => $"{lot.width} × {lot.length} tiles\n" + (Category(lot) switch
        {
            "culture" => "Worship and gatherings.",
            "public-safety" => "Local constable station.",
            "education" => "A town schoolhouse.",
            _ => "Civic halls and courts."
        });

        internal static string Era(LotDefinition lot) => LotClassification.Era(lot)?.id ?? "founders";
        internal static string Description(LotDefinition lot) => lot.objects.Any(o => o.assetId == "v3-building-chapel")
            ? "A neighborhood chapel for worship and community gatherings."
            : lot.objects.Any(o => o.assetId == "v3-building-constable-station")
                ? "A local station for the town’s constable."
                : lot.objects.Any(o => o.assetId == "v3-building-small-school")
                    ? "A small schoolhouse for the town’s children."
                    : Categories.FirstOrDefault(c => c.Id == Category(lot)).Description ?? "A saved civic lot for your town.";
    }
}

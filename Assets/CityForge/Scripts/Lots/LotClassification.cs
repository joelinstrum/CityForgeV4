using System;
using CityForge.UI;

namespace CityForge.Lots
{
    internal static class LotClassification
    {
        public static LotContentCatalog.Category[] Categories => LotContentCatalog.Current.categories;

        public static LotContentCatalog.Category FindCategory(string value) =>
            string.IsNullOrEmpty(value) ? null : Array.Find(Categories, category => category.id == value) ??
                Array.Find(Categories, category => category.legacyName == value || category.name == value);

        public static LotContentCatalog.Subcategory[] ForCategory(string value) =>
            FindCategory(value)?.subcategories ?? Array.Empty<LotContentCatalog.Subcategory>();

        public static string SubcategoryName(string category, string value) =>
            string.IsNullOrEmpty(value) ? value : Array.Find(ForCategory(category),
                child => child.id == value || child.name == value || child.legacyName == value)?.name ?? value;

        public static CityForgeTimeline.Era Era(LotDefinition lot) =>
            !string.IsNullOrEmpty(lot.firstEraId) ? CityForgeTimeline.Find(lot.firstEraId)
                : Array.Find(CityForgeTimeline.Eras, era => era.FirstYear == lot.firstEraYear);

        // Saves can outlive their mods and may be parsed on a background thread.
        public static bool IsValid(string category, string subcategory, string era, int year) =>
            IsReference(category) && IsReference(subcategory) && IsReference(era) && year >= 0;

        private static bool IsReference(string value) =>
            value.Length <= 80 && Array.TrueForAll(value.ToCharArray(), character => !char.IsControl(character));
    }
}

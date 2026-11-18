using System;

namespace CityForge.Lots
{
    internal static class LotExtraction
    {
        internal const float CaptureRadius = 80f;
        internal const int MinimumTrees = 10;
        internal static readonly LotRequiredResource[] Resources = { LotRequiredResource.Timber, LotRequiredResource.Stone,
            LotRequiredResource.Iron, LotRequiredResource.Coal, LotRequiredResource.Clay, LotRequiredResource.Fish };
        internal static bool Applies(LotDefinition lot) => lot != null && LotClassification.FindCategory(lot.category)?.id == "industrial" &&
            (string.Equals(lot.subcategory,"extraction",StringComparison.OrdinalIgnoreCase) ||
             string.Equals(LotClassification.SubcategoryName(lot.category,lot.subcategory),"Extraction",StringComparison.OrdinalIgnoreCase));
        internal static LotRequiredResource Resource(LotDefinition lot)
        {
            if (lot.resourceCaptureConfigured) return lot.captureResource;
            foreach(var resource in lot.requiredResources)
                if(Array.IndexOf(Resources,resource)>=0)return resource;
            return LotRequiredResource.Timber;
        }
        internal static string Key(LotRequiredResource resource) => resource.ToString().ToLowerInvariant();
    }
}

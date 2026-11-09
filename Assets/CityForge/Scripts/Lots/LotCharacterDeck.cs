using UnityEngine;

namespace CityForge.Lots
{
    internal static class LotCharacterDeck
    {
        internal const string HarborHouse = "harbor-council-house";
        private const float PorchHeight = .94f;

        // Measured walkable strips on the normalized Harbor Council House model.
        // The enclosed building and the front steps are outside these strips.
        private static readonly Rect[] Porch =
        {
            Rect.MinMaxRect(-2.2f, 2.25f, 5.45f, 2.67f),
            Rect.MinMaxRect(-5.45f, -2.65f, -3.1f, 2.18f),
            Rect.MinMaxRect(4.62f, -2.65f, 5.53f, 2.58f),
            Rect.MinMaxRect(-2.55f, -3.13f, 4.45f, -2.78f)
        };

        internal static bool TryHeight(LotDefinition lot, Vector2 point, out float height)
        {
            height = 0;
            if (lot == null) return false;
            foreach (var item in lot.objects)
                if (TryHeight(item, point, out float deck)) height = Mathf.Max(height, deck);
            return height > 0;
        }

        internal static bool TryHeight(LotObjectDefinition item, Vector2 point, out float height)
        {
            height = 0;
            if (item.assetId != HarborHouse) return false;
            var offset = (point - item.position) / item.scale;
            var local = Quaternion.Euler(0, -item.yaw, 0) * new Vector3(offset.x, 0, offset.y);
            foreach (var strip in Porch)
            {
                if (!strip.Contains(new Vector2(local.x, local.z))) continue;
                height = PorchHeight * item.scale;
                return true;
            }
            return false;
        }

        internal static bool TryPick(LotDefinition lot, Ray ray, Vector3 origin, out Vector2 point)
        {
            point = default;
            if (lot == null) return false;
            float nearest = float.PositiveInfinity;
            foreach (var item in lot.objects)
            {
                if (item.assetId != HarborHouse) continue;
                var plane = new Plane(Vector3.up, origin + Vector3.up * (PorchHeight * item.scale));
                if (!plane.Raycast(ray, out float distance) || distance >= nearest) continue;
                var hit = ray.GetPoint(distance) - origin;
                var candidate = new Vector2(hit.x, hit.z);
                if (!TryHeight(item, candidate, out _)) continue;
                point = candidate;
                nearest = distance;
            }
            return float.IsFinite(nearest);
        }
    }
}

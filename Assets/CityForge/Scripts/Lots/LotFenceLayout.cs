using System.Collections.Generic;
using UnityEngine;

namespace CityForge.Lots
{
    internal static class LotFenceLayout
    {
        public const float EdgeCaptureMeters = 1f;

        public static bool IsNearLot(LotDefinition lot, Vector2 point)
        {
            var half = lot.Size * .5f + Vector2.one * EdgeCaptureMeters;
            return Mathf.Abs(point.x) <= half.x && Mathf.Abs(point.y) <= half.y;
        }

        public static Vector2 ClampToLot(Vector2 point, Vector2 half)
        {
            half = Vector2.Max(Vector2.zero, half);
            return new Vector2(Mathf.Clamp(point.x, -half.x, half.x), Mathf.Clamp(point.y, -half.y, half.y));
        }

        public static Vector2 ClampPiece(LotDefinition lot, LotObjectDefinition item, LotAssetCatalogEntry asset,
            Vector2 point)
        {
            return ClampToLot(point, (lot.Size - LotRuntimeLoader.Footprint(item, asset).size) * .5f);
        }

        public static bool TryBuild(LotDefinition lot, IReadOnlyList<Vector2> points, bool closed,
            LotAssetCatalogEntry asset, out List<LotObjectDefinition> pieces, out string error)
            => TryBuild(points, closed, asset, 512 - lot.objects.Count, lot, out pieces, out error);

        internal static bool TryBuild(IReadOnlyList<Vector2> points, bool closed, LotAssetCatalogEntry asset,
            int capacity, LotDefinition lot, out List<LotObjectDefinition> pieces, out string error)
        {
            pieces = new List<LotObjectDefinition>();
            error = null;
            if (asset.category != "Fences" || asset.fenceSpan < .1f)
            {
                error = "Choose a fence span to fill this path.";
                return false;
            }

            // Keep one shared, inset corner for both adjoining spans. Allow for the
            // projected width of a post when the fence runs diagonally.
            float thickness = Mathf.Max(asset.fenceDepth, asset.fencePostWidth);
            float inset = new Vector2(asset.fencePostWidth, thickness).magnitude * .5f;
            var pathBounds = lot != null ? lot.Size * .5f - Vector2.one * inset : Vector2.zero;
            for (int edge = 0; edge < points.Count - (closed ? 0 : 1); edge++)
            {
                var start = lot == null ? points[edge] : ClampToLot(points[edge], pathBounds);
                var next = points[(edge + 1) % points.Count];
                var end = lot == null ? next : ClampToLot(next, pathBounds);
                var delta = end - start;
                float distance = delta.magnitude;
                if (distance < .05f)
                {
                    error = "Two fence markers are too close after fitting the path to the lot edge.";
                    return false;
                }
                int count = Mathf.Max(1, Mathf.RoundToInt(distance / asset.fenceSpan));
                if (count > capacity - pieces.Count)
                {
                    error = "This path would exceed the lot’s object limit.";
                    return false;
                }

                float yaw = Mathf.Atan2(-delta.y, delta.x) * Mathf.Rad2Deg;
                for (int index = 0; index < count; index++)
                {
                    var item = new LotObjectDefinition
                    {
                        assetId = asset.id,
                        position = start + delta * ((index + .5f) / count),
                        yaw = yaw,
                        fenceLength = distance / count,
                        fenceEndPost = !closed && edge == points.Count - 2 && index == count - 1
                    };
                    if (lot != null && !LotRuntimeLoader.Fits(lot, item, asset))
                    {
                        error = "The fence is too close to the lot edge. Draw the path a little farther inside.";
                        return false;
                    }
                    pieces.Add(item);
                }
            }
            return pieces.Count > 0;
        }
    }
}

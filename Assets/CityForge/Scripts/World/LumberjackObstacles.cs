using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private sealed class WorkObstacleLot
        {
            internal Vector2 center;
            internal Quaternion rotation;
            internal Rect bounds;
            internal Rect[] pieces;
        }
        private readonly List<WorkObstacleLot> workObstacleLots = new();
        private int workObstacleRevision = -1, workObstacleCount = -1;
        private LotAssetCatalog workObstacleCatalog;
        private LotAssetCatalogEntry[] workObstacleEntries;
        internal int WorkObstacleBuilds { get; private set; }

        private void EnsureWorkObstacles()
        {
            var catalog = LotAssetCatalog.Load();
            if (workObstacleRevision == LotPresentationBuilds && workObstacleCount == PlacedLots.Count &&
                workObstacleCatalog == catalog && ReferenceEquals(workObstacleEntries, catalog ? catalog.entries : null)) return;
            workObstacleLots.Clear();
            workObstacleRevision = LotPresentationBuilds; workObstacleCount = PlacedLots.Count;
            workObstacleCatalog = catalog; workObstacleEntries = catalog ? catalog.entries : null;
            WorkObstacleBuilds++;
            if (!catalog) return;
            foreach (var lot in PlacedLots)
            {
                var pieces = new List<Rect>(); var bounds = new Rect();
                foreach (var item in lot.definition.objects)
                {
                    var asset = catalog.Find(item.assetId);
                    if (asset == null || (asset.category != "Buildings" && asset.category != "Fences")) continue;
                    var piece = LotRuntimeLoader.Footprint(item, asset);
                    bounds = pieces.Count == 0 ? piece : Rect.MinMaxRect(Mathf.Min(bounds.xMin, piece.xMin),
                        Mathf.Min(bounds.yMin, piece.yMin), Mathf.Max(bounds.xMax, piece.xMax), Mathf.Max(bounds.yMax, piece.yMax));
                    pieces.Add(piece);
                }
                if (pieces.Count > 0) workObstacleLots.Add(new WorkObstacleLot { center = lot.center,
                    rotation = Quaternion.Euler(0, -lot.FacingDegrees, 0), bounds = bounds, pieces = pieces.ToArray() });
            }
        }

        internal bool LumberjackObstacle(Vector2 point, float clearance = .4f)
        {
            EnsureWorkObstacles();
            foreach (var lot in workObstacleLots)
            {
                var delta = point - lot.center;
                var local3 = lot.rotation * new Vector3(delta.x, 0, delta.y);
                var local = new Vector2(local3.x, local3.z);
                if (!ExpandedContains(lot.bounds, local, clearance)) continue;
                foreach (var piece in lot.pieces) if (ExpandedContains(piece, local, clearance)) return true;
            }
            return false;
        }

        private static bool ExpandedContains(Rect bounds, Vector2 point, float clearance) =>
            point.x >= bounds.xMin - clearance && point.x < bounds.xMax + clearance &&
            point.y >= bounds.yMin - clearance && point.y < bounds.yMax + clearance;
    }
}

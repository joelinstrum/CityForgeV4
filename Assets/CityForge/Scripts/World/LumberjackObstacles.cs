using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private readonly struct WorkObstaclePiece
        {
            internal readonly Rect bounds;
            internal readonly Vector2 center, halfSize;
            internal readonly Quaternion inverseYaw;
            internal WorkObstaclePiece(LotObjectDefinition item, LotAssetCatalogEntry asset)
            {
                bounds = LotRuntimeLoader.Footprint(item, asset);
                center = item.position;
                halfSize = LotRuntimeLoader.ObjectFootprintSize(item, asset) * .5f;
                inverseYaw = Quaternion.Euler(0, -item.yaw, 0);
            }
            internal bool Contains(Vector2 point, float clearance, bool oriented)
            {
                if (!oriented) return ExpandedContains(bounds, point, clearance);
                var delta = point - center;
                var local = inverseYaw * new Vector3(delta.x, 0, delta.y);
                return Mathf.Abs(local.x) <= halfSize.x + clearance && Mathf.Abs(local.z) <= halfSize.y + clearance;
            }
        }
        private sealed class WorkObstacleLot
        {
            internal Vector2 center;
            internal Quaternion rotation;
            internal Rect bounds;
            internal WorkObstaclePiece[] pieces;
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
                var pieces = new List<WorkObstaclePiece>(); var bounds = new Rect();
                foreach (var item in lot.definition.objects)
                {
                    var asset = catalog.Find(item.assetId);
                    if (asset == null || (asset.category != "Buildings" && asset.category != "Fences")) continue;
                    var piece = LotRuntimeLoader.Footprint(item, asset);
                    bounds = pieces.Count == 0 ? piece : Rect.MinMaxRect(Mathf.Min(bounds.xMin, piece.xMin),
                        Mathf.Min(bounds.yMin, piece.yMin), Mathf.Max(bounds.xMax, piece.xMax), Mathf.Max(bounds.yMax, piece.yMax));
                    pieces.Add(new WorkObstaclePiece(item, asset));
                }
                if (pieces.Count > 0) workObstacleLots.Add(new WorkObstacleLot { center = lot.center,
                    rotation = Quaternion.Euler(0, -lot.FacingDegrees, 0), bounds = bounds, pieces = pieces.ToArray() });
            }
        }

        internal bool LumberjackObstacle(Vector2 point, float clearance = .4f) => WorkObstacle(point, clearance, false);
        private bool OrientedWorkObstacle(Vector2 point, float clearance) => WorkObstacle(point, clearance, true);
        private bool WorkObstacle(Vector2 point, float clearance, bool oriented)
        {
            EnsureWorkObstacles();
            foreach (var lot in workObstacleLots)
            {
                var delta = point - lot.center;
                var local3 = lot.rotation * new Vector3(delta.x, 0, delta.y);
                var local = new Vector2(local3.x, local3.z);
                if (!ExpandedContains(lot.bounds, local, oriented ? clearance * 1.414214f : clearance)) continue;
                foreach (var piece in lot.pieces) if (piece.Contains(local, clearance, oriented)) return true;
            }
            return false;
        }

        private static bool ExpandedContains(Rect bounds, Vector2 point, float clearance) =>
            point.x >= bounds.xMin - clearance && point.x < bounds.xMax + clearance &&
            point.y >= bounds.yMin - clearance && point.y < bounds.yMax + clearance;
    }
}

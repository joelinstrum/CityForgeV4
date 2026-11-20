using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    // A visual grid cell. The saved zoning and placement lattice remain at 4m.
    internal readonly struct ParcelGridCell
    {
        internal readonly Vector2 Center, Half, Right, Forward;
        internal readonly bool Diagonal;
        internal ParcelGridCell(Vector2 center, Vector2 size, float degrees = 0)
        {
            Center = center; Half = size * .5f;
            float angle = degrees * Mathf.Deg2Rad;
            Right = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle));
            Forward = new Vector2(-Right.y, Right.x);
            Diagonal = Mathf.Abs(Mathf.Sin(angle * 2)) > .001f;
        }
        internal Vector2 Corner(int index) => Center + Right * (index >= 2 ? Half.x : -Half.x) +
            Forward * (index == 1 || index == 2 ? Half.y : -Half.y);
        internal Rect Bounds
        {
            get
            {
                var extent = new Vector2(Mathf.Abs(Right.x) * Half.x + Mathf.Abs(Forward.x) * Half.y,
                    Mathf.Abs(Right.y) * Half.x + Mathf.Abs(Forward.y) * Half.y);
                return new Rect(Center - extent, extent * 2);
            }
        }
        // Clip only the open interior: a line on a parcel boundary must survive.
        internal bool InteriorInterval(Vector2 a, Vector2 b, out Vector2 interval)
        {
            float low = 0, high = 1;
            bool hit = Slab(Vector2.Dot(a - Center, Right), Vector2.Dot(b - a, Right), Half.x, ref low, ref high) &&
                Slab(Vector2.Dot(a - Center, Forward), Vector2.Dot(b - a, Forward), Half.y, ref low, ref high);
            interval = new Vector2(low, high);
            return hit && high - low > .00001f;
        }
        private static bool Slab(float start, float delta, float half, ref float low, ref float high)
        {
            if (Mathf.Abs(delta) < .00001f) return Mathf.Abs(start) < half - .001f;
            float a = (-half - start) / delta, b = (half - start) / delta;
            low = Mathf.Max(low, Mathf.Min(a, b)); high = Mathf.Min(high, Mathf.Max(a, b));
            return high > low;
        }
    }

    public sealed partial class PrototypeRegionView
    {
        private readonly List<ParcelGridCell> parcelPreviewCells = new();
        private bool parcelPreviewVisible;
        private static Vector2Int ParcelWorldSize(Vector2Int size, TownZoneFacing facing) =>
            facing == TownZoneFacing.East || facing == TownZoneFacing.West ? new Vector2Int(size.y, size.x) : size;

        private ParcelGridCell GridParcel(Vector2Int anchor, Vector2Int size, TownZoneFacing facing)
        {
            var min = ZoneCorner(anchor);
            var worldSize = (Vector2)ParcelWorldSize(size, facing) * DistrictGridCellMeters;
            return new ParcelGridCell(min + worldSize * .5f, worldSize);
        }
        private List<ParcelGridCell> CollectGridParcels()
        {
            var result = new List<ParcelGridCell>();
            var anchors = new HashSet<Vector2Int>();
            foreach (var pair in zoneLotAnchors)
            {
                var anchor = pair.Value;
                if (!anchors.Add(anchor) || !townZones.ContainsKey(anchor)) continue;
                var size = ZonedLotSize(anchor);
                if (size.x <= 0 || size.y <= 0) continue;
                // Do not join partially erased parcels: surviving cells keep their fine grid.
                var bounds = new RectInt(anchor, ParcelWorldSize(size, residentialFacing.GetValueOrDefault(anchor)));
                bool complete = true;
                foreach (var cell in bounds.allPositionsWithin)
                    if (!townZones.ContainsKey(cell) || !zoneLotAnchors.TryGetValue(cell, out var owner) || owner != anchor)
                    { complete = false; break; }
                var parcel = GridParcel(anchor, size, residentialFacing.GetValueOrDefault(anchor));
                if (complete && InActiveDistrict(parcel.Center)) result.Add(parcel);
            }
            foreach (var record in PlacedLots)
            {
                if (record.definition.type != LotType.Zoning || !InActiveDistrict(record.center)) continue;
                if (zoneLotAnchors.ContainsKey(DistrictGridCell(record.center))) continue;
                result.Add(new ParcelGridCell(record.center, record.definition.Size, record.FacingDegrees));
            }
            if (parcelPreviewVisible) result.AddRange(parcelPreviewCells);
            return result;
        }
        private void PreviewParcels(IEnumerable<TownZoneCell> cells, List<DistrictLotRecord> diagonalLots, bool valid)
        {
            parcelPreviewCells.Clear();
            if (valid)
            {
                if (diagonalLots != null)
                    foreach (var record in diagonalLots)
                        parcelPreviewCells.Add(new ParcelGridCell(record.center, record.definition.Size, record.FacingDegrees));
                else
                    foreach (var cell in cells.GroupBy(c => new Vector2Int(c.lotAnchorX, c.lotAnchorZ)).Select(group => group.First()))
                        parcelPreviewCells.Add(GridParcel(new Vector2Int(cell.lotAnchorX, cell.lotAnchorZ),
                            new Vector2Int(cell.lotWidth, cell.lotDepth), cell.facing));
            }
            if (parcelPreviewVisible) RefreshDistrictGrid();
        }
        private void SetParcelPreviewVisible(bool visible)
        {
            if (parcelPreviewVisible == visible) return;
            parcelPreviewVisible = visible;
            RefreshDistrictGrid();
        }
    }
}

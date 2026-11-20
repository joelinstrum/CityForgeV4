using System;
using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    internal sealed class ZonedBlockPlan
    {
        internal const int RowTiles = 12;
        internal readonly TownZone Zone;
        internal readonly LotWealth Wealth;
        internal readonly int LotSize, LotDepth, RowLength, Turns;
        internal readonly RectInt Bounds;
        internal readonly TownZoneCell[] Cells;
        internal readonly Dictionary<Vector2Int, TownZoneFacing> Facings = new();
        internal int LotCount => RowLength / LotSize * 2;
        internal RectInt OuterBounds => new(Bounds.min - Vector2Int.one, Bounds.size + Vector2Int.one * 2);

        internal ZonedBlockPlan(Vector2Int origin, int lotSize, int turns, TownZone zone, LotWealth wealth)
            : this(origin,new Vector2Int(lotSize,lotSize),turns,zone,wealth) { }

        internal ZonedBlockPlan(Vector2Int origin,Vector2Int lotSize,int turns,TownZone zone,LotWealth wealth)
        {
            if(lotSize.x<2||lotSize.y<2||lotSize.x>LotDefinition.MaxTiles||lotSize.y>LotDefinition.MaxTiles)throw new ArgumentOutOfRangeException(nameof(lotSize));
            Zone=zone;Wealth=wealth;
            LotSize=lotSize.x;LotDepth=lotSize.y;RowLength=Mathf.CeilToInt(RowTiles/(float)LotSize)*LotSize; Turns = (turns % 4 + 4) % 4;
            var rotated = LotGroundOverlay.QuarterTurn(new Rect(Vector2.zero, new Vector2(RowLength, LotDepth * 2)), Turns);
            var offset = -rotated.min;
            Bounds = new RectInt(origin, Vector2Int.RoundToInt(rotated.size));
            var cells = new List<TownZoneCell>(RowLength * LotDepth * 2);
            for (int row = 0; row < 2; row++)
            for (int parcel = 0; parcel < RowLength / LotSize; parcel++)
            {
                var rectangle = LotGroundOverlay.QuarterTurn(new Rect(parcel * LotSize, row * LotDepth, LotSize, LotDepth), Turns);
                var anchor = origin + Vector2Int.RoundToInt(rectangle.min + offset);
                var direction = LotGroundOverlay.QuarterTurn(row == 0 ? Vector2.down : Vector2.up, Turns);
                var facing = Mathf.Abs(direction.x) > .5f ? direction.x > 0 ? TownZoneFacing.East : TownZoneFacing.West
                    : direction.y > 0 ? TownZoneFacing.North : TownZoneFacing.South;
                for (int z = 0; z < Mathf.RoundToInt(rectangle.height); z++)
                for (int x = 0; x < Mathf.RoundToInt(rectangle.width); x++)
                {
                    var cell = anchor + new Vector2Int(x, z);
                    cells.Add(new TownZoneCell { x = cell.x, z = cell.y, zone = zone, wealth = wealth, facing = facing,
                        lotWidth = Mathf.RoundToInt(rectangle.width), lotDepth = Mathf.RoundToInt(rectangle.height), hasLotAnchor = true, lotAnchorX = anchor.x, lotAnchorZ = anchor.y });
                    Facings.Add(cell, facing);
                }
            }
            Cells = cells.ToArray();
        }

        internal DistrictRoadRecord Street(Vector2 worldMinimum, float cellMeters, int district)
        {
            var max = worldMinimum + (Vector2)Bounds.size * cellMeters;
            float half = cellMeters * .5f;
            return new DistrictRoadRecord { district = district, surfaceId = Wealth==LotWealth.Medium && (Zone==TownZone.Residential2||Zone==TownZone.Residential3||Zone==TownZone.Commercial2) ? "founders-cobblestone-street" : "founders-dirt-street", width = Mathf.RoundToInt(cellMeters), gridAligned = true, closed = true, curved = true,
                points = new[] { worldMinimum - Vector2.one * half, new Vector2(max.x + half, worldMinimum.y - half),
                    max + Vector2.one * half, new Vector2(worldMinimum.x - half, max.y + half) } };
        }
    }
}

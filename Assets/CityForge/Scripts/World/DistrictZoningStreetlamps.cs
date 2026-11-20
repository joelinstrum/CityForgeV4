using System.Linq;
using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private void AddZoningStreetlamps()
        {
            foreach (var pair in townZones)
            {
                if (pair.Value != TownZone.Residential2 || zoneWealth.GetValueOrDefault(pair.Key) != LotWealth.Low) continue;
                var direction = residentialFacing.GetValueOrDefault(pair.Key) switch
                {
                    TownZoneFacing.East => Vector2Int.right, TownZoneFacing.West => Vector2Int.left,
                    TownZoneFacing.North => Vector2Int.up, TownZoneFacing.South => Vector2Int.down,
                    _ => Vector2Int.zero
                };
                if (direction == Vector2Int.zero || townZones.ContainsKey(pair.Key + direction)) continue;
                int along = direction.x == 0 ? pair.Key.x : pair.Key.y;
                if ((along % 3 + 3) % 3 != 0) continue;
                AddZoningStreetlamp(ZoneCorner(pair.Key) + Vector2.one * 2f + (Vector2)direction * 2.5f);
            }
        }

        private void AddLotStreetlamps(DistrictLotRecord record)
        {
            var lot = record.definition;
            if (lot.zoningLayout.zone != TownZone.Residential2 || lot.zoningLayout.wealth != LotWealth.Low) return;
            var direction = lot.zoningLayout.frontage switch
            {
                LotFrontage.East => Vector2.right, LotFrontage.West => Vector2.left,
                LotFrontage.North => Vector2.up, _ => Vector2.down
            };
            var tangent = Vector2.Perpendicular(direction);
            float width = direction.x == 0 ? lot.Size.x : lot.Size.y;
            float depth = direction.x == 0 ? lot.Size.y : lot.Size.x;
            var footprint = new DistrictLotFootprint(record);
            var center = footprint.WorldPoint(direction * (depth * .5f + .5f));
            var worldTangent = (footprint.WorldPoint(tangent) - record.center).normalized;
            float projection = Vector2.Dot(center, worldTangent);
            float first = Mathf.Ceil((projection - width * .5f + .1f) / 12f) * 12f;
            for (float station = first; station < projection + width * .5f; station += 12)
                AddZoningStreetlamp(center + worldTangent * (station - projection));
        }

        private void AddZoningStreetlamp(Vector2 point)
        {
            if (!InActiveDistrict(point) || PlacedActors.Count >= MaxPlacedActors ||
                PlacedActors.Any(actor => actor.streetscape != null && actor.assetId.Contains("lamppost") &&
                    (actor.position - point).sqrMagnitude < 25f)) return;
            const string id = "v3-simple-street-lamppost";
            var catalog = LotAssetCatalog.Load();
            var asset = catalog ? catalog.Find(id) : null;
            if (asset == null || !asset.prefab) return;
            var record = new DistrictPlacedActor
            {
                assetId = id, position = point, zoningStreetlamp = true,
                district = DistrictAtWorld(new Vector3(point.x, 0, point.y)),
                streetscape = new LotObjectDefinition { assetId = id, position = point }
            };
            PlacedActors.Add(record);
            PresentStreetscape(record, asset);
        }
    }
}

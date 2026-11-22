using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private WildlifeWaterAccess rideWater;
        internal void PrepareRideTools()
        {
            CancelTexturePaint(); CancelParkPath(); CancelParkTreeBrush(); CancelStreetscape();
            CancelZoneDrag(); CancelZonedBlock(false); zoneToolArmed = false;
            CancelRoadPlacement(); CancelLotPlacement(); CancelDistrictActor();
            CloseRoadContext(); SetRoadPaths(false); SelectDistrictLot(null);
            GetComponent<CityForge.UI.NatureToolbar>()?.Dismiss();
        }
        internal bool TryStartRide(Vector3 focus, float yaw, out Vector3 position, out float heading)
        {
            rideWater = DistrictLotWater();
            position = default;
            heading = yaw;
            float best = float.PositiveInfinity;
            var origin = new Vector2(focus.x, focus.z);
            var facing = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            foreach (var road in districtRoadMeshes.Keys)
            {
                var line = RoadCenterline(road);
                for (int i = 0; i < line.Count - (road.closed ? 0 : 1); i++)
                {
                    var a = line[i]; var b = line[(i + 1) % line.Count];
                    var edge = b - a;
                    if (edge.sqrMagnitude < .01f) continue;
                    var point = a + edge * Mathf.Clamp01(Vector2.Dot(origin - a, edge) / edge.sqrMagnitude);
                    float distance = (point - origin).sqrMagnitude;
                    if (distance >= best || !TryRideGround(point, out float ground)) continue;
                    var direction = new Vector3(edge.x, 0f, edge.y).normalized;
                    if (Vector3.Dot(direction, facing) < 0f) direction = -direction;
                    best = distance;
                    position = new Vector3(point.x, ground + 1.7f, point.y);
                    heading = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                }
            }
            if (!float.IsPositiveInfinity(best)) return true;
            if (TryRideGround(origin, out float fallback))
            { position = new Vector3(origin.x, fallback + 1.7f, origin.y); return true; }
            ShowDistrictMessage("No safe street nearby. Move the camera over a street, then press ⌘D.");
            return false;
        }

        internal bool TryRideGround(Vector2 point, out float height)
        {
            height = 0f;
            if (!ApprovedDistrict || !ApprovedDistrict.DisplayTerrain || !InActiveDistrict(point)) return false;
            if (rideWater != null && rideWater.Wet(point, out _, out _)) return false;
            foreach (var lot in PlacedLots)
                if (new DistrictLotFootprint(lot).OverlapsCircle(point, .3f)) return false;
            height = LotHeight(point) + .15f;
            return true;
        }
    }
}

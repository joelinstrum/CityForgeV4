using System.Collections.Generic;
using CityForge.Lots;
using CityForge.CameraSystem;
using UnityEngine;

namespace CityForge.World
{
    internal readonly struct CommercialSoundArea
    {
        private readonly Vector2 center, half;
        private readonly Quaternion rotation;
        internal CommercialSoundArea(DistrictLotRecord lot)
        { center = lot.center; half = lot.definition.Size * .5f; rotation = Quaternion.Euler(0, lot.FacingDegrees, 0); }
        internal Vector2 Closest(Vector2 from)
        {
            var delta = from - center;
            var local = Quaternion.Inverse(rotation) * new Vector3(delta.x, 0, delta.y);
            var closest = rotation * new Vector3(Mathf.Clamp(local.x, -half.x, half.x), 0, Mathf.Clamp(local.z, -half.y, half.y));
            return center + new Vector2(closest.x, closest.z);
        }
    }
    public sealed partial class PrototypeRegionView
    {
        internal Vector2 SoundListeningPoint(CityForgeCameraController rig, Camera camera)
        {
            if (rig.Riding) return new Vector2(camera.transform.position.x, camera.transform.position.z);
            var ground = ApprovedDistrict && ApprovedDistrict.DisplayTerrain ? ApprovedDistrict.DisplayTerrain : terrain;
            var collider = ground ? ground.GetComponent<TerrainCollider>() : null;
            if (collider && collider.enabled && collider.Raycast(camera.ViewportPointToRay(new Vector3(.5f, .5f, 0)), out var hit, 100000f))
                return new Vector2(hit.point.x, hit.point.z);
            var focus = rig.CaptureRegionState().focus;
            return new Vector2(focus.x, focus.z);
        }

        internal void CollectCommercialSoundAreas(List<CommercialSoundArea> areas, LotAssetCatalog catalog)
        {
            areas.Clear();
            if (!catalog) return;
            foreach (var pair in districtLotRoots)
            {
                if (!pair.Value || !pair.Value.gameObject.activeInHierarchy) continue;
                var lot = pair.Key;
                if (!IsCommercialZone(lot.definition.zoningLayout.zone) &&
                    LotClassification.FindCategory(lot.definition.category)?.id != "commercial") continue;
                bool built = false;
                foreach (var item in lot.definition.objects)
                    if (catalog.Find(item.assetId) is { category: "Buildings", prefab: { } model } && model)
                    { built = true; break; }
                if (built) areas.Add(new CommercialSoundArea(lot));
            }
        }

        internal float AudioStreamDistance(Vector2 point, float reach)
        {
            if (Data?.WaterBodies == null) return float.PositiveInfinity;
            var uv = point / MapSide + Vector2.one * .5f;
            float nearest = float.PositiveInfinity;
            foreach (var body in Data.WaterBodies)
            {
                if (!(body.isStream || body.playerChannel || body.kind == WaterBodyKind.River) || body.centerline == null || body.centerline.Length < 2) continue;
                var bounds = body.Bounds;
                float margin = reach / MapSide;
                if (uv.x < bounds.xMin - margin || uv.x > bounds.xMax + margin || uv.y < bounds.yMin - margin || uv.y > bounds.yMax + margin) continue;
                float distance = Mathf.Max(0f, (body.IndexedCenterlineDistance(uv) - body.halfWidthNormalized) * MapSide);
                nearest = Mathf.Min(nearest, distance);
            }
            return nearest;
        }
    }
}

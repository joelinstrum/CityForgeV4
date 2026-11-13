using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    internal sealed class LamppostLight : MonoBehaviour
    {
        private Light pool;
        private float intensityScale = 1f;

        internal static void Attach(GameObject instance, LotAssetCatalogEntry asset, RegionTimeOfDay time,
            int cullingMask = -1)
        {
            if (asset.id == "v3-building-low-wealth-vertical-siding-front-gable")
            {
                AddWallLantern(instance, new Vector3(.90f, 1.90f, -2.49f) * 1.28f, cullingMask, time);
                AddWallLantern(instance, new Vector3(.90f, 1.89f, 2.55f) * 1.28f, cullingMask, time);
                return;
            }
            if (asset.category != LotAssetCatalog.StreetFurniture || asset.group != "Lighting") return;

            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);

            var source = new GameObject("Lantern light");
            source.layer = instance.layer;
            source.transform.SetParent(instance.transform, false);
            source.transform.SetPositionAndRotation(
                new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * .88f, bounds.center.z),
                Quaternion.Euler(90f, 0f, 0f));

            var lamp = instance.AddComponent<LamppostLight>();
            lamp.pool = source.AddComponent<Light>();
            lamp.pool.type = LightType.Spot;
            lamp.pool.innerSpotAngle = 55f;
            lamp.pool.spotAngle = 110f;
            lamp.pool.range = Mathf.Clamp(bounds.size.y * 2.6f, 5f, 13f);
            lamp.pool.color = new Color(1f, .75f, .41f);
            lamp.pool.cullingMask = cullingMask;
            lamp.pool.shadows = LightShadows.None;
            lamp.SetTime(time);
        }

        private static void AddWallLantern(GameObject instance, Vector3 position, int mask, RegionTimeOfDay time)
        {
            var lantern = new GameObject("Wall lantern light");
            lantern.layer = instance.layer;
            lantern.transform.SetParent(instance.transform, false);
            lantern.transform.localPosition = position;
            var light = lantern.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 2.8f;
            light.color = new Color(1f, .72f, .37f);
            light.cullingMask = mask;
            var controller = lantern.AddComponent<LamppostLight>();
            controller.pool = light;
            controller.intensityScale = .16f;
            controller.SetTime(time);
        }

        internal static void ApplyTime(Transform root, RegionTimeOfDay time)
        {
            if (!root) return;
            foreach (var lamp in root.GetComponentsInChildren<LamppostLight>(true))
                lamp.SetTime(time);
        }

        private void SetTime(RegionTimeOfDay time)
        {
            pool.enabled = time == RegionTimeOfDay.Evening || time == RegionTimeOfDay.Night;
            pool.intensity = (time == RegionTimeOfDay.Night ? 11f : 8f) * intensityScale;
        }
    }
}

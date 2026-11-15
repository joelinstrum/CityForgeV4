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
            if (asset.id is "v3-building-medium-wealth-brick-colonial" or "v3-building-medium-wealth-gray-colonial")
            {
                // Authored in the fitted model's local axes, just outside the front lantern glass.
                float span = Mathf.Max(asset.footprint.x, asset.footprint.y);
                AddWallLantern(instance, new Vector3(.11f, .32f, .37f) * span, cullingMask, time);
                AddWallLantern(instance, new Vector3(.36f, .32f, .37f) * span, cullingMask, time);
                return;
            }
            if (asset.id == "v3-building-high-wealth-mediterranean-villa")
            {
                float span = Mathf.Max(asset.footprint.x, asset.footprint.y);
                AddWallLantern(instance, new Vector3(.258f, .243f, .370f) * span, cullingMask, time);
                AddWallLantern(instance, new Vector3(.002f, .243f, .370f) * span, cullingMask, time);
                AddWallLantern(instance, new Vector3(-.263f, .243f, .370f) * span, cullingMask, time);
                return;
            }
            if (asset.id == "v3-building-high-wealth-southern-mansion-slate")
            {
                float span = Mathf.Max(asset.footprint.x, asset.footprint.y);
                AddWallLantern(instance, new Vector3(0f, .588f, .273f) * span, cullingMask, time);
                AddWallLantern(instance, new Vector3(0f, .326f, .282f) * span, cullingMask, time);
                return;
            }
            if (asset.id == "v3-building-high-wealth-regency-southern-mansion")
            {
                float span = Mathf.Max(asset.footprint.x, asset.footprint.y);
                AddWallLantern(instance, new Vector3(.152f, .273f, .299f) * span, cullingMask, time);
                AddWallLantern(instance, new Vector3(.102f, .273f, .143f) * span, cullingMask, time);
                return;
            }
            if (asset.id == "v3-building-high-wealth-grand-stucco-manor")
            {
                float span = Mathf.Max(asset.footprint.x, asset.footprint.y);
                AddWallLantern(instance, new Vector3(.120f, .295f, .308f) * span, cullingMask, time);
                AddWallLantern(instance, new Vector3(-.117f, .295f, .308f) * span, cullingMask, time);
                return;
            }
            if (asset.id == "v3-building-high-wealth-georgian-stone-green-trim")
            {
                float span = Mathf.Max(asset.footprint.x, asset.footprint.y);
                AddWallLantern(instance, new Vector3(.1f, .226f, .278f) * span, cullingMask, time);
                AddWallLantern(instance, new Vector3(-.1f, .226f, .278f) * span, cullingMask, time);
                return;
            }
            if (asset.id == "v3-building-high-wealth-georgian-stone")
            {
                float span = Mathf.Max(asset.footprint.x, asset.footprint.y);
                AddWallLantern(instance, new Vector3(.09f, .232f, .247f) * span, cullingMask, time);
                AddWallLantern(instance, new Vector3(-.091f, .232f, .247f) * span, cullingMask, time);
                return;
            }
            if (asset.id == "v3-building-high-wealth-vernon")
            {
                float span = Mathf.Max(asset.footprint.x, asset.footprint.y);
                AddWallLantern(instance, new Vector3(.051f, .158f, .15f) * span, cullingMask, time);
                AddWallLantern(instance, new Vector3(-.048f, .158f, .165f) * span, cullingMask, time);
                return;
            }
            if (asset.id == "v3-building-high-wealth-red-brick-georgian")
            {
                float span = Mathf.Max(asset.footprint.x, asset.footprint.y);
                AddWallLantern(instance, new Vector3(.062f, .262f, .237f) * span, cullingMask, time);
                AddWallLantern(instance, new Vector3(-.158f, .262f, .237f) * span, cullingMask, time);
                return;
            }
            if (asset.id == "v3-building-high-wealth-federal-mansion")
            {
                float span = Mathf.Max(asset.footprint.x, asset.footprint.y);
                AddWallLantern(instance, new Vector3(-.172f, .31f, .176f) * span, cullingMask, time);
                AddWallLantern(instance, new Vector3(.172f, .31f, .176f) * span, cullingMask, time);
                return;
            }
            if (asset.id == "v3-building-high-wealth-grand-colonial")
            {
                float span = Mathf.Max(asset.footprint.x, asset.footprint.y);
                AddWallLantern(instance, new Vector3(-.14f, .255f, .26f) * span, cullingMask, time);
                AddWallLantern(instance, new Vector3(.14f, .255f, .26f) * span, cullingMask, time);
                return;
            }
            if (asset.id == "v3-building-medium-wealth-red-house")
            {
                float span = Mathf.Max(asset.footprint.x, asset.footprint.y);
                AddWallLantern(instance, new Vector3(.12f, .29f, .46f) * span, cullingMask, time);
                AddWallLantern(instance, new Vector3(.33f, .29f, .46f) * span, cullingMask, time);
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

using CityForge.Lots;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    internal sealed class LamppostLight : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private Light[] pools;
        private Renderer[] lanternRenderers;
        private readonly List<Material> emissionMaterials = new();
        private float intensityScale = 1f;
        private float glowScale = 1f;

        internal static void Attach(GameObject instance, LotAssetCatalogEntry asset, RegionTimeOfDay time,
            int cullingMask = -1)
        {
            Vector3[] c2Lanterns=asset.id switch
            {
                "v3-building-c2-mid-wealth-ye-olde-inn" => new[]{new Vector3(-4.12108f,4.21636f,1.86818f),new Vector3(-0.08342f,2.08075f,3.33081f),new Vector3(1.61840f,2.08075f,3.32119f)},
                "v3-building-c2-mid-wealth-kings-arms" => new[]{new Vector3(-1.07831f,2.35856f,2.92385f),new Vector3(1.04794f,2.35856f,2.92265f),new Vector3(-3.06735f,3.51281f,3.29569f)},
                "v3-building-c2-mid-wealth-apothecary-3-stories" => new[]{new Vector3(-2.04036f,3.03758f,3.55342f)},
                "v3-building-c2-mid-wealth-apothecary" => new[]{new Vector3(-2.50594f,2.05481f,4.70776f)},
                "v3-building-c2-mid-wealth-books" => new[]{new Vector3(-4.12198f,4.11113f,5.74098f),new Vector3(-3.94174f,4.40261f,3.79839f)},
                "v3-building-c2-mid-wealth-ceramics" => new[]{new Vector3(-2.26294f,3.63431f,3.83334f)},
                "v3-building-c2-mid-wealth-chevron-store" => new[]{new Vector3(-2.71856f,2.38894f,3.11293f)},
                "v3-building-c2-mid-wealth-coffee" => new[]{new Vector3(-1.62506f,2.29781f,3.52144f)},
                "v3-building-c2-mid-wealth-commerican-merchants-shop" => new[]{new Vector3(-2.50594f,3.81656f,6.66313f)},
                "v3-building-c2-mid-wealth-dry-goods" => new[]{new Vector3(-2.38444f,3.78619f,5.44987f),new Vector3(-2.34847f,2.47911f,4.24573f)},
                "v3-building-c2-mid-wealth-pottery-barn" => new[]{new Vector3(-2.44519f,3.66469f,3.09769f)},
                "v3-building-c2-mid-wealth-sea-urchin" => new[]{new Vector3(-2.44519f,2.54081f,3.34214f)},
                _=>null
            };
            if(c2Lanterns!=null){foreach(var anchor in c2Lanterns)AddWallLantern(instance,anchor,cullingMask,time);return;}
            // Lantern glass anchors ray-picked from each supplied front facade.
            Vector3? r3Lantern = asset.id switch
            {
                "v3-building-r3-mid-wealth-3-window-brick" => new Vector3(0.05721f,2.77062f,2.89506f),
                "v3-building-r3-mid-wealth-painted-white-brick" => new Vector3(-0.05721f,2.57993f,2.49899f),
                "v3-building-r3-mid-wealth-3-story-brick-2" => new Vector3(-0.05721f,2.35111f,2.01402f),
                "v3-building-r3-mid-wealth-3-story-yellow" => new Vector3(0.01907f,2.27483f,2.72165f),
                "v3-building-r3-mid-wealth-3-story-brick" => new Vector3(-0.17314f,2.17954f,2.56679f),
                "v3-building-r3-mid-wealth-3-story-yellow-stucco" => new Vector3(0.24131f,2.10306f,2.79017f),
                "v3-building-r3-mid-wealth-2-story-brick" => new Vector3(0.51975f,2.36555f,2.02720f),
                "v3-building-r3-mid-wealth-2-story-tan-wood" => new Vector3(0.12454f,2.17659f,2.82730f),
                "v3-building-r3-mid-wealth-2-story-yellow-house" => new Vector3(0.31826f,2.34264f,2.54996f),
                _ => null
            };
            if(r3Lantern.HasValue){AddWallLantern(instance,r3Lantern.Value,cullingMask,time);return;}
            if (asset.id == "v3-building-constable-station")
            {
                // Just outside the two glass lanterns on the fitted front facade.
                AddWallLantern(instance, new Vector3(1.81f, 3.04f, 4.82f), cullingMask, time);
                AddWallLantern(instance, new Vector3(-1.59f, 3.04f, 4.82f), cullingMask, time);
                return;
            }
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

            var lamp = instance.AddComponent<LamppostLight>();
            // Imported/validated materials may have lost the emission keyword while their
            // daytime color was black. Restore it on owned runtime copies, on both LODs.
            var copies = new Dictionary<Material, Material>();
            var glowing = new List<Renderer>();
            foreach (var renderer in renderers)
            {
                var slots = renderer.sharedMaterials;
                bool hasGlow = false;
                for (int slot = 0; slot < slots.Length; slot++)
                {
                    var source = slots[slot];
                    if (!source || !source.HasProperty("_EmissionMap") || !source.GetTexture("_EmissionMap")) continue;
                    if (!copies.TryGetValue(source, out var litMaterial))
                    {
                        litMaterial = new Material(source) { name = source.name + " · lantern emission" };
                        litMaterial.SetColor(EmissionColor, Color.white);
                        litMaterial.EnableKeyword("_EMISSION");
                        copies.Add(source, litMaterial);
                        lamp.emissionMaterials.Add(litMaterial);
                    }
                    slots[slot] = litMaterial;
                    hasGlow = true;
                }
                if (!hasGlow) continue;
                renderer.sharedMaterials = slots;
                glowing.Add(renderer);
            }
            lamp.lanternRenderers = glowing.ToArray();

            if (asset.id == "v3-simple-street-lamppost")
            {
                lamp.glowScale = 1.6f;
                lamp.intensityScale = 1.3f;
            }

            var fittedHeight = bounds.size.y / instance.transform.lossyScale.y;
            float width = asset.footprint.x;
            var center = instance.transform.InverseTransformPoint(bounds.center);
            float baseHeight = center.y - fittedHeight * .5f;
            if (asset.id == "v3-three-lantern-lamppost")
            {
                lamp.pools = new[]
                {
                    AddPool(0f, .88f), AddPool(-width * .40f, .74f), AddPool(width * .40f, .74f)
                };
                lamp.intensityScale = .4f;
            }
            else
            {
                lamp.pools = new[] { asset.id == "v3-18th-century-lamppost"
                    ? AddPool(-width * .29f, .715f) : AddPool(0f, .80f) };
            }
            lamp.SetTime(time);

            Light AddPool(float offset, float height)
            {
                var source = new GameObject("Lantern light");
                source.layer = instance.layer;
                source.transform.SetParent(instance.transform, false);
                source.transform.localPosition = new Vector3(center.x + offset, baseHeight + fittedHeight * height, center.z);
                source.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var light = source.AddComponent<Light>();
                light.type = LightType.Spot;
                light.spotAngle = 130f;
                light.innerSpotAngle = 75f;
                light.range = Mathf.Clamp(bounds.size.y * 3.25f, 6.25f, 16.25f);
                light.color = new Color(1f, .75f, .41f);
                light.cullingMask = cullingMask;
                light.shadows = LightShadows.None;
                return light;
            }
        }

        private static void AddWallLantern(GameObject instance, Vector3 position, int mask, RegionTimeOfDay time)
        {
            var lantern = new GameObject("Wall lantern light");
            lantern.layer = instance.layer;
            lantern.transform.SetParent(instance.transform, false);
            lantern.transform.localPosition = position;
            var light = lantern.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3.8f;
            light.color = new Color(1f, .72f, .37f);
            light.cullingMask = mask;
            var controller = lantern.AddComponent<LamppostLight>();
            controller.pools = new[] { light };
            controller.intensityScale = .16f;
            controller.SetTime(time);
        }

        internal static void ApplyTime(Transform root, RegionTimeOfDay time)
        {
            if (!root) return;
            foreach (var lamp in root.GetComponentsInChildren<LamppostLight>(true))
                lamp.SetTime(time);
        }

        private void OnDestroy()
        {
            foreach (var material in emissionMaterials) if (material) Destroy(material);
        }

        private void SetTime(RegionTimeOfDay time)
        {
            bool night = time == RegionTimeOfDay.Night;
            bool lit = night || time == RegionTimeOfDay.Evening;
            foreach (var pool in pools)
            {
                pool.enabled = lit;
                pool.intensity = (night ? 11f : 8f) * intensityScale;
            }

            if (lanternRenderers == null) return;
            var glow = lit ? (night ? new Color(2.1f, 1.13f, .46f) : new Color(1.3f, .72f, .31f)) : Color.black;
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in lanternRenderers)
            {
                renderer.GetPropertyBlock(properties);
                properties.SetColor(EmissionColor, glow * glowScale);
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}

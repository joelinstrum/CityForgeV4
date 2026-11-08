using System;
using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityForge.UI
{
    internal sealed class LotAssetThumbnails : IDisposable
    {
        private const int PreviewLayer = 31;
        private readonly Dictionary<string, RenderTexture> cache = new Dictionary<string, RenderTexture>();
        private GameObject stage;
        private Camera camera;
        private Light light;

        public Texture Get(LotAssetCatalogEntry asset) => Render(asset.id, asset.label, parent =>
            LotRuntimeLoader.InstantiateObject(new LotObjectDefinition { assetId = asset.id }, asset, parent));

        public Texture GetLot(LotDefinition lot, LotAssetCatalog catalog) => Render(lot.id, lot.name, parent =>
        {
            var instance = new GameObject(lot.name);
            instance.transform.SetParent(parent, false);
            foreach (var item in lot.objects)
            {
                var asset = catalog ? catalog.Find(item.assetId) : null;
                if (asset != null && asset.prefab) LotRuntimeLoader.InstantiateObject(item, asset, instance.transform);
            }
            return instance;
        });

        private Texture Render(string id, string label, Func<Transform, GameObject> instantiate)
        {
            if (cache.TryGetValue(id, out var cached)) return cached;
            if (!stage) CreateStage();
            var instance = instantiate(stage.transform);
            RenderTexture target = null;
            try
            {
                foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = PreviewLayer;
                foreach (var lod in instance.GetComponentsInChildren<LODGroup>()) lod.ForceLOD(0);

                var renderers = instance.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return null;
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float radius = Mathf.Max(.1f, bounds.extents.magnitude);
                var rotation = Quaternion.Euler(22, 330, 0);
                camera.transform.SetPositionAndRotation(bounds.center - rotation * Vector3.forward * (radius * 3), rotation);
                camera.orthographicSize = radius * 1.1f;
                camera.nearClipPlane = radius;
                camera.farClipPlane = radius * 5;

                target = new RenderTexture(96, 96, 24) { name = label + " thumbnail" };
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request)) return null;
                light.enabled = true;
                RenderPipeline.SubmitRenderRequest(camera, request);
                cache.Add(id, target);
                target = null;
                return cache[id];
            }
            finally
            {
                light.enabled = false;
                instance.SetActive(false);
                UnityEngine.Object.Destroy(instance);
                if (target) UnityEngine.Object.Destroy(target);
            }
        }

        private void CreateStage()
        {
            stage = new GameObject("Lot thumbnail stage");
            stage.transform.position = new Vector3(8000, -4000, 0);
            camera = new GameObject("Thumbnail camera").AddComponent<Camera>();
            camera.transform.SetParent(stage.transform, false);
            camera.enabled = false;
            camera.orthographic = true;
            camera.aspect = 1;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(30, 43, 54, 255);
            camera.cullingMask = 1 << PreviewLayer;
            camera.allowHDR = false;

            light = new GameObject("Thumbnail light").AddComponent<Light>();
            light.transform.SetParent(stage.transform, false);
            light.transform.rotation = Quaternion.Euler(42, -30, 0);
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.enabled = false;
            light.cullingMask = 1 << PreviewLayer;
        }

        public void Dispose()
        {
            foreach (var thumbnail in cache.Values)
            {
                thumbnail.Release();
                UnityEngine.Object.Destroy(thumbnail);
            }
            cache.Clear();
            if (stage) UnityEngine.Object.Destroy(stage);
        }
    }
}

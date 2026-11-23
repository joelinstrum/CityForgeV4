using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using CityForge.World;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityForge.UI
{
    internal sealed class LotAssetThumbnails : IDisposable
    {
        private const int PreviewLayer = 31;
        private readonly Dictionary<string, RenderTexture> cache = new Dictionary<string, RenderTexture>();
        private readonly int thumbnailResolution;
        private readonly bool transparent, frontView, tightFraming;
        public LotAssetThumbnails(int resolution = 96, bool transparent = false, bool frontView = false, bool tightFraming = false)
        {
            thumbnailResolution = Mathf.Clamp(resolution, 64, 512);
            this.transparent = transparent;
            this.frontView = frontView;
            this.tightFraming = tightFraming;
        }
        private GameObject stage;
        private Camera camera;
        private Light light;

        public Texture Get(LotAssetCatalogEntry asset) => Get(asset, RegionSeason.Summer);
        public Texture Get(LotAssetCatalogEntry asset, RegionSeason season) => Render(season == RegionSeason.Summer ? asset.id : asset.id + ":season:" + season, asset.label, parent =>
            LotRuntimeLoader.InstantiateObject(new LotObjectDefinition { assetId = asset.id }, asset, parent, season));

        public Texture GetPlan(LotAssetCatalogEntry asset, float halfSpan) =>
            Render($"plan:{asset.id}:{halfSpan:R}", asset.label, parent => LotRuntimeLoader.InstantiateObject(
                new LotObjectDefinition { assetId = asset.id, scale = 1f }, asset, parent), halfSpan);

        public Texture GetLot(LotDefinition lot, LotAssetCatalog catalog, int rotationSteps = 0) => GetLot(lot, catalog, rotationSteps, RegionSeason.Summer);
        public Texture GetLot(LotDefinition lot, LotAssetCatalog catalog, int rotationSteps, RegionSeason season) =>
            Render($"{lot.id}:rotation:{rotationSteps % 8}:season:{season}", lot.name, parent =>
        {
            var instance = new GameObject(lot.name);
            instance.transform.SetParent(parent, false);
            foreach (var item in lot.objects)
            {
                var asset = catalog ? catalog.Find(item.assetId) : null;
                if (asset != null && asset.prefab) LotRuntimeLoader.InstantiateObject(item, asset, instance.transform, season);
            }
            instance.transform.localRotation = Quaternion.Euler(0, rotationSteps * 45, 0);
            return instance;
        });

        private readonly Dictionary<Texture, Rect> visibleFrames = new();
        internal Rect VisibleUV(Texture texture)
        {
            if (texture is not RenderTexture target || !target.IsCreated()) return new Rect(0,0,1,1);
            if (visibleFrames.TryGetValue(texture, out var cached)) return cached;
            // Leaf cards and wind bounds can include large transparent margins. Frame
            // the rendered silhouette without changing the mesh, palette or materials.
            var previous = RenderTexture.active;
            var sample = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target; sample.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                var pixels = sample.GetPixels32(); int minX=target.width,minY=target.height,maxX=-1,maxY=-1;
                for(int y=0;y<target.height;y++)for(int x=0;x<target.width;x++)
                    if(pixels[y*target.width+x].a>24){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                var frame = new Rect(0,0,1,1);
                if(maxX>=minX&&maxY>=minY)
                {
                    float side=Mathf.Min(1,Mathf.Max((maxX-minX+1f)/target.width,(maxY-minY+1f)/target.height)*1.12f);
                    float cx=(minX+maxX+1f)/(2*target.width),cy=(minY+maxY+1f)/(2*target.height);
                    frame=new Rect(Mathf.Clamp(cx-side*.5f,0,1-side),Mathf.Clamp(cy-side*.5f,0,1-side),side,side);
                }
                visibleFrames[texture]=frame;return frame;
            }
            finally { RenderTexture.active=previous;UnityEngine.Object.Destroy(sample); }
        }

        private Texture Render(string id, string label, Func<Transform, GameObject> instantiate, float planHalfSpan = 0f)
        {
            if (cache.TryGetValue(id, out var cached)) return cached;
            if (!stage) CreateStage();
            var instance = instantiate(stage.transform);
            RenderTexture target = null;
            var previousSun = RenderSettings.sun;
            var sceneLightMasks = new List<(Light Source, int Mask)>();
            try
            {
                foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = PreviewLayer;
                foreach (var lod in instance.GetComponentsInChildren<LODGroup>()) lod.ForceLOD(0);

                var renderers = instance.GetComponentsInChildren<Renderer>();
                if (tightFraming)
                {
                    // Hidden billboard/distant bounds must not shrink the visible LOD0 tree.
                    var near = new HashSet<Renderer>(); var distant = new HashSet<Renderer>();
                    foreach (var group in instance.GetComponentsInChildren<LODGroup>())
                    {
                        var levels = group.GetLODs();
                        for (int level = 0; level < levels.Length; level++)
                            foreach (var renderer in levels[level].renderers)
                                if (renderer) (level == 0 ? near : distant).Add(renderer);
                    }
                    renderers = renderers.Where(renderer => !distant.Contains(renderer) || near.Contains(renderer)).ToArray();
                }
                if (transparent)
                    foreach (var renderer in renderers)
                    {
                        var properties = new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(properties);
                        properties.SetColor("_EmissionColor", Color.black);
                        renderer.SetPropertyBlock(properties);
                    }
                var probe = new[] { PrototypeRegionView.LotDaylightProbe() };
                var lighting = new MaterialPropertyBlock();
                foreach (var renderer in renderers)
                {
                    renderer.GetPropertyBlock(lighting);
                    lighting.CopySHCoefficientArraysFrom(probe);
                    renderer.lightProbeUsage = LightProbeUsage.CustomProvided;
                    renderer.SetPropertyBlock(lighting);
                    for (int slot = 0; slot < renderer.sharedMaterials.Length; slot++)
                    {
                        renderer.GetPropertyBlock(lighting, slot);
                        if (lighting.isEmpty) continue;
                        lighting.CopySHCoefficientArraysFrom(probe);
                        renderer.SetPropertyBlock(lighting, slot);
                    }
                }
                if (renderers.Length == 0) return null;
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float radius = Mathf.Max(.1f, bounds.extents.magnitude);
                var rotation = Quaternion.Euler(22, frontView ? 150 : 330, 0);
                camera.transform.SetPositionAndRotation(bounds.center - rotation * Vector3.forward * (radius * 3), rotation);
                camera.orthographicSize = radius * 1.1f;
                if (tightFraming)
                {
                    float extent = .1f; var inverse = Quaternion.Inverse(rotation);
                    for (int corner = 0; corner < 8; corner++)
                    {
                        var offset = new Vector3((corner & 1) == 0 ? -bounds.extents.x : bounds.extents.x,
                            (corner & 2) == 0 ? -bounds.extents.y : bounds.extents.y,
                            (corner & 4) == 0 ? -bounds.extents.z : bounds.extents.z);
                        var projected = inverse * offset;
                        extent = Mathf.Max(extent, Mathf.Abs(projected.x), Mathf.Abs(projected.y));
                    }
                    camera.orthographicSize = extent * 1.06f;
                }
                camera.nearClipPlane = radius;
                camera.farClipPlane = radius * 5;

                if (planHalfSpan > 0f)
                {
                    camera.transform.SetPositionAndRotation(
                        stage.transform.position + Vector3.up * (bounds.size.y + 10f), Quaternion.Euler(90, 0, 0));
                    camera.orthographicSize = planHalfSpan;
                    camera.nearClipPlane = .1f;
                    camera.farClipPlane = bounds.size.y + 20f;
                }
                int resolution = planHalfSpan > 0f ? 640 : thumbnailResolution;
                target = new RenderTexture(resolution, resolution, 24) { name = label + " thumbnail" };
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request)) return null;
                // Keep the key on the camera-facing side for either thumbnail angle.
                light.transform.rotation = Quaternion.Euler(40, camera.transform.eulerAngles.y + 30, 0);
                light.enabled = true;
                // URP otherwise prefers the world's sun over this isolated preview light.
                foreach (var source in Light.GetLights(LightType.Directional, PreviewLayer))
                {
                    if (!source || source == light) continue;
                    sceneLightMasks.Add((source, source.cullingMask));
                    source.cullingMask &= ~(1 << PreviewLayer);
                }
                RenderSettings.sun = light;
                RenderPipeline.SubmitRenderRequest(camera, request);
                cache.Add(id, target);
                target = null;
                return cache[id];
            }
            finally
            {
                foreach (var source in sceneLightMasks) if (source.Source) source.Source.cullingMask = source.Mask;
                RenderSettings.sun = previousSun;
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
            camera.backgroundColor = transparent ? Color.clear : new Color32(30, 43, 54, 255);
            camera.cullingMask = 1 << PreviewLayer;
            camera.allowHDR = false;
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = false;
            cameraData.volumeLayerMask = 0;

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
            cache.Clear(); visibleFrames.Clear();
            if (stage) UnityEngine.Object.Destroy(stage);
        }
    }
}

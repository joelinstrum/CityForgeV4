#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.CameraSystem;
using CityForge.Lots;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CityForge.World
{
    internal static class ResidentialGroundChecks
    {
        [MenuItem("CityForge/Tests/Render Residential Ground")]
        private static void Run() => Render("Library/ResidentialGroundChecks");

        internal static void Render(string output)
        {
            Directory.CreateDirectory(output);
            var scene = EditorSceneManager.NewPreviewScene();
            var owned = new List<Object>();
            var previousTarget = RenderTexture.active;
            try
            {
                const string era = "founders";
                var lot = new LotDefinition
                {
                    width = 4, length = 4, firstEraId = era, type = LotType.Zoning,
                    zoningLayout = new LotZoningLayout
                    {
                        zone = TownZone.Residential1, wealth = LotWealth.Low, buildingSize = new Vector2(8, 8)
                    }
                };
                var record = new DistrictLotRecord { id = "residential-ground-check", definition = lot };
                var restored = JsonUtility.FromJson<DistrictLotRecord>(JsonUtility.ToJson(record));
                Require(ResidentialGround.TrySelect(lot, record.id, era, out var original) &&
                    ResidentialGround.TrySelect(restored.definition, restored.id, era, out var loaded) &&
                    original.Variation.id == loaded.Variation.id && original.Turns == loaded.Turns &&
                    original.FlipX == loaded.FlipX && original.FlipY == loaded.FlipY &&
                    original.Strength == loaded.Strength && original.Tint == loaded.Tint, "Stable placement after serialization");
                restored.definition.zoningLayout.wealth = LotWealth.Medium;
                Require(ResidentialGround.TrySelect(restored.definition, record.id, era, out var medium) &&
                    medium.Variation.id == "maintained-grass" && medium.Variation.mask == "maintained-grass-mask",
                    "Medium wealth uses its own maintained-grass mask");
                restored.definition.zoningLayout.wealth = LotWealth.High;
                Require(!ResidentialGround.TrySelect(restored.definition, record.id, era, out _), "No high-wealth fallback");
                Require(!ResidentialGround.TrySelect(lot, record.id, "unsupported-era", out _), "No era fallback");
                lot.residentialGround.mode = ResidentialGroundMode.Disabled;
                Require(!ResidentialGround.TrySelect(lot, record.id, era, out _), "Disabled overlay");
                lot.residentialGround.mode = ResidentialGroundMode.Automatic;

                var variants = ResidentialGround.FindLibrary(lot, era).variations;
                var root = CreateObject("Residential ground test");
                var patches = root.AddComponent<ResidentialGroundBatches>();
                var terrainData = Own(new TerrainData { heightmapResolution = 129, size = new Vector3(128, 8, 128) });
                var heights = new float[129, 129];
                for (int z = 0; z < 129; z++)
                for (int x = 0; x < 129; x++)
                    heights[z, x] = .12f + .04f * Mathf.Sin(x * .13f) * Mathf.Cos(z * .1f);
                terrainData.SetHeights(0, 0, heights);
                var grass = Own(new TerrainLayer
                {
                    diffuseTexture = LotGroundTexture.Find(false, "lush-grass").Texture, tileSize = Vector2.one * 8
                });
                terrainData.terrainLayers = new[] { grass };
                var terrainObject = Terrain.CreateTerrainGameObject(terrainData);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(terrainObject, scene);
                var terrain = terrainObject.GetComponent<Terrain>();
                terrain.heightmapPixelError = 1;
                terrain.materialTemplate = Own(new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")));

                var catalog = LotAssetCatalog.Load();
                var houses = catalog ? catalog.entries.Where(entry => entry != null &&
                    entry.CanPreviewGrowable(TownZone.Residential1, LotWealth.Low, era)).Take(3).ToArray() : Array.Empty<LotAssetCatalogEntry>();
                for (int i = 0; i < variants.Length; i++)
                {
                    var variant = variants[i];
                    var placement = CreateObject(variant.label).transform;
                    placement.SetParent(root.transform);
                    var center = new Vector2(48 + i % 3 * 16, i < 3 ? 60 : 79);
                    placement.position = new Vector3(center.x, 0, center.y);
                    lot.residentialGround.fixedVariation = variant.id;
                    patches.Add(lot, "fixture-" + i, era, placement, Height);
                    CheckImporter(variant.albedo, true);
                    CheckImporter(variant.mask, false);
                    if (houses.Length > 0)
                    {
                        var asset = houses[i % houses.Length];
                        var item = new LotObjectDefinition { assetId = asset.id, position = new Vector2(-1.5f, 1.5f) };
                        var house = LotRuntimeLoader.InstantiateObject(item, asset, placement);
                        var position = house.transform.position;
                        position.y = Height(new Vector2(position.x, position.z));
                        house.transform.position = position;
                    }
                }
                patches.Build();
                var renderers = patches.GetComponentsInChildren<MeshFilter>()
                    .Where(filter => filter.gameObject.name.StartsWith("Residential ground ·", StringComparison.Ordinal)).ToArray();
                Require(renderers.Length == variants.Length, "One batch per variation within the fixture chunk");
                foreach (var filter in renderers)
                {
                    var mesh = filter.sharedMesh;
                    Require(Mathf.Approximately(mesh.bounds.size.x, 4 * LotDefinition.TileMeters) &&
                        Mathf.Approximately(mesh.bounds.size.z, 4 * LotDefinition.TileMeters), "Exact 4×4 tile footprint");
                    Require(mesh.bounds.size.y > .01f, "Conforms to sloped terrain");
                }

                var road = CreateObject("Dirt road");
                var style = LotPathMaterial.Find("packed-earth");
                var path = new LotPathDefinition { width = 3, points = new[] { new Vector2(34, 69), new Vector2(95, 69) } };
                road.AddComponent<MeshFilter>().sharedMesh = Own(LotPathSurface.BuildMesh(path, style.TileSize, .13f, Height));
                var roadMaterial = Own(style.CreateMaterial());
                LotPathMaterial.ApplyEdgeTreatment(roadMaterial, path, 8, 0);
                road.AddComponent<MeshRenderer>().sharedMaterial = roadMaterial;
                Require(renderers.All(filter => filter.GetComponent<Renderer>().sharedMaterial.renderQueue < roadMaterial.renderQueue),
                    "Residential pass draws before transparent roads");

                var sun = CreateObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.35f;
                sun.shadows = LightShadows.Hard;
                sun.transform.rotation = Quaternion.Euler(52, -40, 0);
                var camera = CreateObject("Camera").AddComponent<Camera>();
                camera.cameraType = CameraType.Game;
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.17f, .21f, .23f);
                camera.farClipPlane = 1000;
                var rig = camera.gameObject.AddComponent<CityForgeCameraController>();
                var target = Own(new RenderTexture(1280, 800, 24));
                var read = Own(new Texture2D(target.width, target.height, TextureFormat.RGB24, false));
                rig.FrameArea(128, 8, new Vector3(64, 1, 69));
                for (int zoom = 0; zoom <= 5; zoom++)
                {
                    rig.ConfigureTerrainStudy(new Vector3(64, 1, 69), 12.8f, zoom);
                    RenderFrame($"Zoom-{zoom}");
                }
                rig.ConfigureTerrainStudy(new Vector3(64, 1, 69), 12.8f, 2);
                foreach (var filter in renderers) filter.gameObject.SetActive(false);
                RenderFrame("Zoom-2-without-overlays");
                var shader = Resources.Load<Shader>("Region/ResidentialGround");
                Require(shader && shader.isSupported && !ShaderUtil.ShaderHasError(shader), "Supported shader with no compile errors");
                File.WriteAllText(Path.Combine(output, "checks.txt"),
                    $"PASS · {variants.Length} variations, {renderers.Length} batches, {houses.Length} house models. " +
                    "Stable save roundtrip, wealth/era isolation, texture import settings, terrain-conforming 4×4 footprints, and road draw order. " +
                    "Captured zoom levels 0–5 and a no-overlay comparison.\n");

                float Height(Vector2 point) => terrain.SampleHeight(new Vector3(point.x, 0, point.y));
                void RenderFrame(string name)
                {
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target;
                    read.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    read.Apply();
                    File.WriteAllBytes(Path.Combine(output, name + ".png"), read.EncodeToPNG());
                }
            }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(output, "checks.txt"), error.ToString());
                Debug.LogException(error);
            }
            finally
            {
                RenderTexture.active = previousTarget;
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var asset in owned) Object.DestroyImmediate(asset);
            }

            GameObject CreateObject(string name)
            {
                var item = new GameObject(name);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(item, scene);
                return item;
            }

            T Own<T>(T asset) where T : Object
            {
                owned.Add(asset);
                return asset;
            }
        }

        private static void CheckImporter(string name, bool srgb)
        {
            var path = "Assets/CityForge/Resources/Lots/GroundTextures/Residential/" + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer && importer.sRGBTexture == srgb && importer.mipmapEnabled &&
                importer.filterMode == FilterMode.Trilinear && importer.wrapMode == TextureWrapMode.Clamp, path);
        }

        private static void Require(bool condition, string check)
        {
            if (!condition) throw new InvalidOperationException(check);
        }
    }
}
#endif

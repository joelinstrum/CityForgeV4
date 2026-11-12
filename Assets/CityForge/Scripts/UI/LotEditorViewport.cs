using System;
using System.Collections.Generic;
using CityForge.CameraSystem;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityForge.UI
{
    internal sealed partial class LotEditorViewport : MonoBehaviour, IPointerClickHandler, IDragHandler,
        IScrollHandler, IBeginDragHandler, IEndDragHandler, IPointerMoveHandler, IPointerExitHandler, IPointerEnterHandler
    {
        private const int PreviewLayer = 31;
        private static readonly Vector3 Origin = new Vector3(0f, -4000f, 0f);
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Mesh> pathMeshes = new List<Mesh>();
        private readonly List<(LotPathDefinition path, Vector3[] vertices)> pathTargets =
            new List<(LotPathDefinition, Vector3[])>();
        private Transform stage, content;
        private Camera cameraView;
        private Light previewLight;
        private Material draftingMaterial;
        private Transform overlayPreview;
        private Material overlayPreviewMaterial;
        private bool night;
        private bool animationsPaused;
        private readonly LotCharacterPreview characterPreview = new LotCharacterPreview();
        private string draggedObject, selectedObject;
        private bool movingSelection, characterFocus;
        private readonly Dictionary<Renderer, Material[]> buildingMaterials = new();
        private readonly Dictionary<Material, Material> fadedBuildingMaterials = new();
        private readonly Dictionary<Animator, float> pausedSpeeds = new Dictionary<Animator, float>();
        private RawImage image;
        private RenderTexture target;
        private Vector3 focus;
        private float yaw = 315f, pitch = CityForgeCameraController.IsometricPitch, zoom = 1f, span;
        private Texture2D draftingTexture;
        private readonly Dictionary<string, Transform> instances = new Dictionary<string, Transform>();
        private readonly List<(string id, Renderer renderer)> pickTargets = new List<(string, Renderer)>();
        private readonly Dictionary<string, Transform> selectionFootprints = new();
        private Action<Vector2, string, LotPathDefinition> click;
        private Action<string, Vector2> beginMove;
        private Action<Vector2> updateMove;
        private Action endMove;
        private PointWirePath repeatPath;
        private PointWireGraphic repeatGuide;
        private Func<Vector2, Vector2> snapRepeatPoint;
        private PointWirePath fencePath;
        private PointWireGraphic fenceGuide;
        private PointWirePath groundPath;
        private PointWireGraphic groundGuide;
        private PointWireGraphic pathSelectionGuide;
        private Vector3[] selectedPathEdges;
        private bool selectedPathClosed;
        private MeshRenderer selectedPathRenderer;
        private int selectedPathOrder;
        private float groundPathWidth;
        private bool curveGroundPath;
        private readonly List<Vector2> roundedGuide = new List<Vector2>();
        private Func<Vector2, Vector2?> snapGroundPoint;
        private Vector2? guidePointer;
        private Camera guideEventCamera;
        private LotAssetCatalogEntry ghostAsset;
        private LotDefinition previewLot;
        private readonly HashSet<string> previewCharacters = new();
        private GameObject placementGhost;
        private readonly Dictionary<Material, Material> ghostMaterials = new Dictionary<Material, Material>();
        private Func<Vector2, Vector2?> ghostPosition;
        private Vector2? ghostPointer;
        private Camera ghostEventCamera;

        public void Initialize(RawImage surface, Action<Vector2, string, LotPathDefinition> handleClick,
            Action<string, Vector2> beginDrag, Action<Vector2> drag, Action endDrag)
        {
            image = surface;
            surface.gameObject.AddComponent<RectMask2D>();
            pathSelectionGuide = CreateGuide("Selected path outline", new Color32(255, 179, 68, 255));
            groundGuide = CreateGuide("Ground path guide", new Color32(255, 179, 68, 255));
            repeatGuide = CreateGuide("Asset repeat guide", new Color32(199, 118, 255, 255));
            fenceGuide = CreateGuide("Fence path guide", new Color32(199, 118, 255, 255));
            click = handleClick;
            beginMove = beginDrag;
            updateMove = drag;
            endMove = endDrag;
            stage = new GameObject("Lot preview stage").transform;
            stage.position = Origin;
            cameraView = new GameObject("Lot preview camera").AddComponent<Camera>();
            cameraView.transform.SetParent(stage, false);
            cameraView.orthographic = true;
            cameraView.clearFlags = CameraClearFlags.SolidColor;
            cameraView.backgroundColor = new Color32(28, 37, 46, 255);
            cameraView.cullingMask = 1 << PreviewLayer;
            cameraView.allowHDR = false;
            var cameraData = cameraView.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (cameraData) cameraData.renderPostProcessing = false;
            previewLight = new GameObject("Lot preview light").AddComponent<Light>();
            previewLight.transform.SetParent(stage, false);
            previewLight.type = LightType.Directional;
            previewLight.cullingMask = 1 << PreviewLayer;
            previewLight.shadows = LightShadows.Hard;
            previewLight.shadowBias = .05f;
            previewLight.shadowNormalBias = .15f;
            ApplyLighting();
            draftingTexture = DraftingTexture();
        }

        public bool ToggleNight()
        {
            night = !night;
            ApplyLighting();
            return night;
        }

        public bool ToggleAnimations()
        {
            animationsPaused = !animationsPaused;
            if (animationsPaused)
            {
                PauseAnimations(content);
                if (placementGhost) PauseAnimations(placementGhost.transform);
            }
            else
            {
                foreach (var pair in pausedSpeeds)
                    if (pair.Key) pair.Key.speed = pair.Value;
                pausedSpeeds.Clear();
            }
            return animationsPaused;
        }

        private void PauseAnimations(Transform root)
        {
            if (!animationsPaused || !root) return;
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                if (pausedSpeeds.ContainsKey(animator)) continue;
                pausedSpeeds.Add(animator, animator.speed);
                animator.speed = 0;
            }
        }

        private void ForgetAnimations(Transform root)
        {
            if (!animationsPaused) return;
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                pausedSpeeds.Remove(animator);
        }

        private Dictionary<string, (float speed, AnimatorStateInfo[] states)[]> CapturePausedPoses()
        {
            if (!animationsPaused) return null;
            var poses = new Dictionary<string, (float, AnimatorStateInfo[])[]>();
            foreach (var pair in instances)
            {
                var animators = pair.Value.GetComponentsInChildren<Animator>(true);
                if (animators.Length == 0) continue;
                var modelPoses = new (float, AnimatorStateInfo[])[animators.Length];
                for (int i = 0; i < animators.Length; i++)
                {
                    var animator = animators[i];
                    var states = new AnimatorStateInfo[animator.layerCount];
                    for (int layer = 0; layer < states.Length; layer++)
                        states[layer] = animator.GetCurrentAnimatorStateInfo(layer);
                    modelPoses[i] = (pausedSpeeds.TryGetValue(animator, out float speed) ? speed : animator.speed, states);
                }
                poses.Add(pair.Key, modelPoses);
            }
            return poses;
        }

        private static void RestorePausedPose(Transform model, (float speed, AnimatorStateInfo[] states)[] poses)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < Mathf.Min(animators.Length, poses.Length); i++)
            {
                var animator = animators[i];
                var (speed, states) = poses[i];
                animator.speed = speed;
                if (!animator.isActiveAndEnabled || !animator.runtimeAnimatorController) continue;
                for (int layer = 0; layer < Mathf.Min(animator.layerCount, states.Length); layer++)
                {
                    var state = states[layer];
                    if (state.fullPathHash != 0 && animator.HasState(layer, state.fullPathHash))
                        animator.Play(state.fullPathHash, layer, state.normalizedTime);
                }
                animator.Update(0);
            }
        }

        private readonly List<Renderer> previewTrees = new();
        private readonly List<Renderer> previewPaths = new();
        private MaterialPropertyBlock previewLighting;
        private readonly UnityEngine.Rendering.SphericalHarmonicsL2[] previewProbe = new UnityEngine.Rendering.SphericalHarmonicsL2[1];

        private void LightPreviewTree(Renderer renderer) =>
            ApplyPreviewProbe(renderer, World.PrototypeRegionView.LotDaylightProbe());

        private void LightPreviewPath(Renderer renderer)
        {
            // The lot stage has its own sun. Keep its path fill independent of the scene's sky probe.
            var fill = new UnityEngine.Rendering.SphericalHarmonicsL2();
            fill.AddAmbientLight(new Color(.12f, .13f, .14f));
            ApplyPreviewProbe(renderer, fill);
        }

        private void ApplyPreviewProbe(Renderer renderer, UnityEngine.Rendering.SphericalHarmonicsL2 probe)
        {
            previewProbe[0] = night ? probe * .18f : probe;
            previewLighting ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(previewLighting);
            previewLighting.CopySHCoefficientArraysFrom(previewProbe);
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.CustomProvided;
            renderer.SetPropertyBlock(previewLighting);
        }

        private void ApplyLighting()
        {
            previewLight.transform.rotation = night ? Quaternion.Euler(32f, 115f, 0f) : Quaternion.Euler(50f, -35f, 0f);
            previewLight.color = night ? new Color(.59f, .73f, 1f) : Color.white;
            previewLight.intensity = night ? .09f : .90f;
            cameraView.backgroundColor = night ? new Color32(10, 16, 27, 255) : new Color32(28, 37, 46, 255);
            UpdateDraftingLighting();
            foreach (var tree in previewTrees)
                if (tree) LightPreviewTree(tree);
            foreach (var path in previewPaths)
                if (path) LightPreviewPath(path);
        }

        private void UpdateDraftingLighting()
        {
            if (!draftingMaterial) return;
            bool grid = draftingMaterial.GetTexture("_BaseMap") == draftingTexture;
            var color = grid ? Color.white : (Color)new Color32(33, 40, 47, 255);
            if (night) color *= new Color(.30f, .37f, .52f, 1f);
            draftingMaterial.SetColor("_BaseColor", color);
        }

        public void Refresh(LotDefinition lot, LotAssetCatalog catalog, string selected, HashSet<string> selectedGroup,
            LotPathDefinition selectedPath, bool grid, Rect? selectedTile, bool showGrowablePreview)
        {
            selectedObject = selected;
            footprintRoot = null;
            footprintPreview = null;
            previewLot = lot;
            previewCharacters.Clear();
            characterPreview.BeginRefresh(lot, catalog);
            var pausedPoses = CapturePausedPoses();
            ClearOverlayPreview();
            if (content)
            {
                ForgetAnimations(content);
                content.gameObject.SetActive(false);
                Destroy(content.gameObject);
            }
            foreach (var material in materials) Destroy(material);
            materials.Clear();
            buildingMaterials.Clear();
            fadedBuildingMaterials.Clear();
            foreach (var mesh in pathMeshes) Destroy(mesh);
            pathMeshes.Clear();
            pathTargets.Clear();
            selectedPathEdges = null;
            selectedPathRenderer = null;
            pathSelectionGuide.gameObject.SetActive(false);
            instances.Clear();
            pickTargets.Clear();
            previewTrees.Clear();
            previewPaths.Clear();
            selectionFootprints.Clear();
            entryMarker = null;
            draggingEntryMarker = false;

            content = new GameObject("Lot preview contents").transform;
            content.SetParent(stage, false);
            var size = lot.Size;
            var ground = LotGroundTexture.Find(false, lot.baseTexture) ?? LotGroundTexture.Base[0];
            var groundTexture = ground.Texture;
            var groundSize = lot.SizeForRotation(lot.baseTextureRotation);
            var groundMaterial = Material(groundTexture ? Color.white : ground.Fallback, groundTexture,
                LotGroundTexture.Repeat(groundSize, lot.baseTextureSpan));
            var surroundings = Material(new Color32(108, 108, 108, 255));
            draftingMaterial = grid
                ? Material(Color.white, draftingTexture, Vector2.one * 96f, true)
                : Material(new Color32(33, 40, 47, 255), unlit: true);
            UpdateDraftingLighting();
            var border = Material(new Color32(234, 215, 154, 255), unlit: true);
            Box("Drafting surface", new Vector3(0, -.92f, 0), new Vector3(768, .1f, 768), draftingMaterial);
            Box("Gray surroundings", new Vector3(0, -.38f, 0), new Vector3(size.x + 8, .35f, size.y + 8), surroundings);
            Box("Ground", new Vector3(0, -.2f, 0), new Vector3(groundSize.x, .4f, groundSize.y), groundMaterial)
                .transform.localRotation = Quaternion.Euler(0, lot.baseTextureRotation * 90f, 0);
            if (lot.IsWaterfront)
            {
                var water = lot.WaterfrontBounds;
                Box("Waterfront rows", new Vector3(water.center.x, .009f, water.center.y),
                    new Vector3(water.width, .01f, water.height), Material(new Color32(54, 112, 150, 255), unlit: true));
            }
            var wholeLot = new Rect(-size * .5f, size);
            DrawGroundOverlay(lot.overlayTexture, lot.overlayTextureSpan, wholeLot, 0, rotation: lot.overlayTextureRotation,
                visibleArea: lot.IsWaterfront ? lot.LandBounds : null);
            for (int i = 0; i < lot.overlays.Count; i++)
            {
                var overlay = lot.overlays[i];
                DrawGroundOverlay(overlay.texture, overlay.span, overlay.Bounds, i + 1, overlay.TextureCenter, overlay.rotation, lot.IsWaterfront ? lot.LandBounds : null);
            }

            if (grid)
            {
                var line = Material(new Color32(191, 200, 147, 255), unlit: true);
                var minor = Material(new Color32(136, 152, 104, 128), unlit: true);
                LotGroundTexture.ConfigureTransparentLayer(minor, LotGroundOverlay.MaxCount + 2);
                for (int x = 1; x < lot.width * 2; x++)
                    Box("Width line", new Vector3(x * LotDefinition.TileMeters / 2 - size.x / 2, .045f, 0),
                        new Vector3(x % 2 == 0 ? .12f : .065f, .018f, size.y), x % 2 == 0 ? line : minor);
                for (int z = 1; z < lot.length * 2; z++)
                    Box("Length line", new Vector3(0, .048f, z * LotDefinition.TileMeters / 2 - size.y / 2),
                        new Vector3(size.x, .018f, z % 2 == 0 ? .12f : .065f), z % 2 == 0 ? line : minor);
            }
            if (selectedTile.HasValue)
                DrawTileOutline(selectedTile.Value, Material(Color.white, unlit: true), content);

            var zoning = lot.zoningLayout;
            if (zoning.Configured)
            {
                footprintMaterial = Material(new Color32(96, 219, 241, 255), unlit: true);
                DrawBuildingFootprint(zoning.BuildingSlot);
                var building = showGrowablePreview && catalog && !string.IsNullOrEmpty(zoning.previewBuildingId)
                    ? catalog.Find(zoning.previewBuildingId) : null;
                if (building != null && building.CanPreviewGrowable(zoning.zone, zoning.wealth, lot.firstEraId))
                {
                    var placement = zoning.hasEntrance
                        ? LotGrowableBuildings.PlaceInSlot(zoning, building) : zoning.BuildingPreview();
                    if (placement != null) DrawBuildingPreview(placement, building);
                }

                void DrawBuildingPreview(LotObjectDefinition placement, LotAssetCatalogEntry building)
                {
                    var preview = LotRuntimeLoader.InstantiateObject(placement, building, content);
                    footprintPreview = preview.transform;
                    foreach (var child in preview.GetComponentsInChildren<Transform>(true))
                        child.gameObject.layer = PreviewLayer;
                    foreach (var collider in preview.GetComponentsInChildren<Collider>(true))
                        collider.enabled = false;
                    foreach (var renderer in preview.GetComponentsInChildren<Renderer>())
                    {
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        renderer.sharedMaterials = FadeBuildingMaterials(renderer.sharedMaterials);
                    }
                }
                if (zoning.hasEntrance)
                    entryMarker = DrawAnchor("House entrance", zoning.entrance, new Color32(255, 211, 109, 255));
                if (zoning.hasDriveway)
                    DrawAnchor("Driveway entrance", zoning.driveway, new Color32(202, 158, 255, 255));
            }

            Transform DrawAnchor(string name, Vector2 point, Color color)
            {
                var marker = Box(name, new Vector3(point.x, .21f, point.y), new Vector3(.55f, .12f, .55f),
                    Material(color, unlit: true));
                marker.transform.SetParent(content, false);
                return marker.transform;
            }

            foreach (var item in lot.objects)
            {
                var asset = catalog ? catalog.Find(item.assetId) : null;
                if (asset == null || !asset.prefab) continue;
                var instance = LotRuntimeLoader.InstantiateObject(item, asset, content);
                foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = PreviewLayer;
                instances.Add(item.id, instance.transform);
                if (pausedPoses != null && pausedPoses.TryGetValue(item.id, out var pose))
                    RestorePausedPose(instance.transform, pose);
                if (asset.IsCharacter)
                {
                    previewCharacters.Add(item.id);
                    characterPreview.Bind(item, asset, instance.transform, animationsPaused);
                }
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    if (asset.category == "Buildings")
                    {
                        var original = renderer.sharedMaterials;
                        buildingMaterials.Add(renderer, original);
                        if (characterFocus) renderer.sharedMaterials = FadeBuildingMaterials(original);
                    }
                    if (asset.category == "Trees" || asset.category == "Landscaping" && asset.group == "Trees")
                    {
                        previewTrees.Add(renderer);
                        LightPreviewTree(renderer);
                    }
                    if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer || renderer is BillboardRenderer)
                        pickTargets.Add((item.id, renderer));
                }
                if (item.id != selected && !selectedGroup.Contains(item.id)) continue;
                var footprint = LotRuntimeLoader.Footprint(item, asset);
                var position = instance.transform.localPosition;
                selectionFootprints[item.id] = Box("Selected footprint", new Vector3(position.x, position.y + .075f, position.z),
                    new Vector3(footprint.width, .025f, footprint.height), border).transform;
            }

            for (int i = 0; i < lot.paths.Count; i++)
            {
                var path = lot.paths[i];
                var style = LotPathMaterial.Find(path.materialId);
                var mesh = LotPathSurface.BuildMesh(path, style.TileSize, .085f + i * .0005f);
                pathMeshes.Add(mesh);
                var edges = mesh.vertices;
                pathTargets.Add((path, edges));
                if (path == selectedPath)
                {
                    selectedPathEdges = edges;
                    selectedPathClosed = path.closed;
                    pathSelectionGuide.gameObject.SetActive(true);
                }
                var surface = new GameObject(style.Label + " path", typeof(MeshFilter), typeof(MeshRenderer));
                surface.layer = PreviewLayer;
                surface.transform.SetParent(content, false);
                surface.GetComponent<MeshFilter>().sharedMesh = mesh;
                var material = style.CreateMaterial();
                if (path.edgeBlend > 0 || path.recessed) LotPathMaterial.ApplyEdgeTreatment(material, path, path.edgeBlend, i);
                materials.Add(material);
                var renderer = surface.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                if (path == selectedPath)
                {
                    selectedPathRenderer = renderer;
                    selectedPathOrder = i;
                }
                previewPaths.Add(renderer);
                LightPreviewPath(renderer);
            }

            characterPreview.EndRefresh();
            PauseAnimations(content);
            span = Mathf.Max(size.x, size.y);
            ApplyPose();
        }

        internal void PreviewPathEdgeBlend(LotPathDefinition path, int value)
        {
            if (!selectedPathRenderer) return;
            var material = selectedPathRenderer.sharedMaterial;
            if (value > 0 || path.recessed)
            {
                LotPathMaterial.ApplyEdgeTreatment(material, path, value, selectedPathOrder);
            }
            else if (material.HasProperty("_EdgeBlend"))
            {
                var opaque = LotPathMaterial.Find(path.materialId).CreateMaterial();
                selectedPathRenderer.sharedMaterial = opaque;
                materials.Remove(material);
                materials.Add(opaque);
                Destroy(material);
            }
        }

        private void DrawGroundOverlay(string id, int span, Rect bounds, int order, Vector2? textureCenter = null,
            int rotation = 0, Rect? visibleArea = null)
        {
            var texture = LotGroundTexture.Find(true, id)?.Texture;
            if (!texture) return;
            if (visibleArea is Rect land)
            {
                var clipped = Rect.MinMaxRect(Mathf.Max(land.xMin, bounds.xMin), Mathf.Max(land.yMin, bounds.yMin),
                    Mathf.Min(land.xMax, bounds.xMax), Mathf.Min(land.yMax, bounds.yMax));
                if (clipped.width <= 0 || clipped.height <= 0) return;
                if (clipped != bounds)
                {
                    textureCenter ??= bounds.center + LotGroundOverlay.FullTextureAnchor(bounds.size, span, rotation);
                    bounds = clipped;
                }
            }
            var localSize = rotation % 2 == 0 ? bounds.size : new Vector2(bounds.height, bounds.width);
            var repeat = LotGroundTexture.Repeat(localSize, span);
            var material = Material(Color.white, texture, repeat);
            if (textureCenter.HasValue)
                material.SetTextureOffset("_BaseMap", (Vector2.one - repeat) * .5f +
                    LotGroundTexture.Repeat(LotGroundOverlay.QuarterTurn(bounds.center - textureCenter.Value, -rotation), span));
            LotGroundTexture.ConfigureTransparentLayer(material, order);
            var overlay = Box("Ground overlay", new Vector3(bounds.center.x, .01f, bounds.center.y),
                new Vector3(localSize.x, .005f, localSize.y), material).GetComponent<Renderer>();
            overlay.transform.localRotation = Quaternion.Euler(0, rotation * 90, 0);
            overlay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void PreviewOverlayTile(Rect tile)
        {
            if (!overlayPreview)
            {
                overlayPreview = new GameObject("Overlay drag preview").transform;
                overlayPreview.SetParent(content, false);
                overlayPreviewMaterial = Material(new Color32(255, 194, 83, 255), unlit: true);
            }
            DrawTileOutline(tile, overlayPreviewMaterial, overlayPreview);
        }

        public void ClearOverlayPreview()
        {
            if (!overlayPreview) return;
            overlayPreview.gameObject.SetActive(false);
            materials.Remove(overlayPreviewMaterial);
            Destroy(overlayPreviewMaterial);
            Destroy(overlayPreview.gameObject);
            overlayPreview = null;
            overlayPreviewMaterial = null;
        }

        private void DrawTileOutline(Rect bounds, Material material, Transform parent, string name = "Tile outline")
        {
            const float height = .16f;
            const float width = .12f;
            Line(bounds.center.x, bounds.yMin, bounds.width, width);
            Line(bounds.center.x, bounds.yMax, bounds.width, width);
            Line(bounds.xMin, bounds.center.y, width, bounds.height);
            Line(bounds.xMax, bounds.center.y, width, bounds.height);

            void Line(float x, float z, float sizeX, float sizeZ)
            {
                var line = Box(name, new Vector3(x, height, z), new Vector3(sizeX, .01f, sizeZ), material);
                line.transform.SetParent(parent, false);
            }
        }

        private Material Material(Color color, Texture2D texture = null, Vector2 tiling = default, bool unlit = false)
        {
            var material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            if (texture)
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }
            if (!unlit) material.SetFloat("_Smoothness", .08f);
            materials.Add(material);
            return material;
        }

        private static Texture2D DraftingTexture()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, true) { name = "Lot drafting grid" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = x == 0 || y == 0 ? new Color32(49, 57, 63, 255) : new Color32(33, 40, 47, 255);
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private GameObject Box(string name, Vector3 center, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.layer = PreviewLayer;
            box.transform.SetParent(content, false);
            box.transform.localPosition = center;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(box.GetComponent<Collider>());
            return box;
        }

        private PointWireGraphic CreateGuide(string name, Color color)
        {
            var overlay = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            overlay.transform.SetParent(image.transform, false);
            var guide = overlay.AddComponent<PointWireGraphic>();
            guide.rectTransform.anchorMin = Vector2.zero;
            guide.rectTransform.anchorMax = Vector2.one;
            guide.rectTransform.offsetMin = guide.rectTransform.offsetMax = Vector2.zero;
            guide.raycastTarget = false;
            guide.color = color;
            overlay.SetActive(false);
            return guide;
        }

        public void ShowRepeatPath(PointWirePath path, Func<Vector2, Vector2> snapPoint)
        {
            repeatPath = path;
            snapRepeatPoint = snapPoint;
            repeatGuide.gameObject.SetActive(path != null);
            DrawGuide(repeatPath, repeatGuide, .36f, false, snapRepeatPoint);
        }

        public void ShowFencePath(PointWirePath path)
        {
            fencePath = path;
            guidePointer = null;
            fenceGuide.gameObject.SetActive(path != null);
            DrawGuide(fencePath, fenceGuide, .36f, false);
        }

        public void ShowGroundPath(PointWirePath path, float width, bool curved, Func<Vector2, Vector2?> snapPoint)
        {
            if (groundPath != path) guidePointer = null;
            groundPath = path;
            groundPathWidth = width;
            curveGroundPath = curved;
            snapGroundPoint = snapPoint;
            groundGuide.gameObject.SetActive(path != null);
        }

        public bool NearFenceStart(Vector2 point) => NearStart(fencePath, fenceGuide, point);
        public bool NearGroundPathStart(Vector2 point) => NearStart(groundPath, groundGuide, point);

        private bool NearStart(PointWirePath path, PointWireGraphic guide, Vector2 point)
        {
            return path != null && path.Points.Count >= 3 &&
                Vector2.Distance(ProjectGuidePoint(guide, point), ProjectGuidePoint(guide, path.Points[0])) *
                image.canvas.scaleFactor <= 24;
        }

        public void SetPlacementGhost(LotAssetCatalogEntry asset, Func<Vector2, Vector2?> position, float yaw = 0f, float scale = 1f)
        {
            ghostPosition = position;
            if (ghostAsset == asset)
            {
                if (placementGhost)
                {
                    placementGhost.transform.localRotation = Quaternion.Euler(0, yaw, 0);
                    placementGhost.transform.localScale = Vector3.one * scale;
                    UpdatePlacementGhost();
                }
                return;
            }
            ClearPlacementGhost();
            if (asset == null) return;
            ghostAsset = asset;
            placementGhost = LotRuntimeLoader.InstantiateObject(new LotObjectDefinition { assetId = asset.id, yaw = yaw, scale = scale }, asset, stage);
            placementGhost.name = asset.label + " placement preview";
            foreach (var child in placementGhost.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = PreviewLayer;
            foreach (var collider in placementGhost.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var renderer in placementGhost.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    var original = slots[i];
                    if (!original) continue;
                    if (!ghostMaterials.TryGetValue(original, out var transparent))
                    {
                        transparent = new Material(original);
                        var color = transparent.GetColor("_BaseColor");
                        color.a *= .48f;
                        transparent.SetColor("_BaseColor", color);
                        transparent.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        LotGroundTexture.ConfigureTransparentLayer(transparent, LotGroundOverlay.MaxCount + 3);
                        ghostMaterials.Add(original, transparent);
                    }
                    slots[i] = transparent;
                }
                renderer.sharedMaterials = slots;
            }
            PauseAnimations(placementGhost.transform);
            placementGhost.SetActive(false);
        }

        private void ClearPlacementGhost()
        {
            if (placementGhost)
            {
                ForgetAnimations(placementGhost.transform);
                placementGhost.SetActive(false);
                Destroy(placementGhost);
            }
            placementGhost = null;
            ghostAsset = null;
            ghostPointer = null;
            foreach (var material in ghostMaterials.Values) Destroy(material);
            ghostMaterials.Clear();
        }

        private void UpdatePlacementGhost()
        {
            if (!placementGhost) return;
            Vector2? position = null;
            if (ghostPointer.HasValue && TryRay(ghostPointer.Value, ghostEventCamera, out var ray) &&
                TryPlacementPoint(ray, ghostAsset?.IsCharacter == true, out var point)) position = ghostPosition(point);
            if (position.HasValue)
            {
                float height = ghostAsset?.IsCharacter == true ? CharacterDeckHeight(position.Value) : 0;
                placementGhost.transform.localPosition = new Vector3(position.Value.x, height, position.Value.y);
            }
            placementGhost.SetActive(position.HasValue);
        }

        public void OnPointerEnter(PointerEventData eventData) => OnPointerMove(eventData);

        public void OnPointerMove(PointerEventData eventData)
        {
            footprintPointerInside = true;
            footprintPointer = eventData.position;
            footprintEventCamera = eventData.enterEventCamera;
            UpdateFootprintCursor();
            if (placementGhost)
            {
                ghostPointer = eventData.position;
                ghostEventCamera = eventData.enterEventCamera;
                UpdatePlacementGhost();
            }
            guidePointer = eventData.position;
            guideEventCamera = eventData.enterEventCamera;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            footprintPointerInside = false;
            if (!footprintDragging) SetFootprintCursor(-1);
            guidePointer = null;
            ghostPointer = null;
            UpdatePlacementGhost();
        }

        private Vector2 ProjectGuidePoint(PointWireGraphic guide, Vector2 point) =>
            ProjectGuidePoint(guide, new Vector3(point.x, .1f, point.y));

        private Vector2 ProjectGuidePoint(PointWireGraphic guide, Vector3 point)
        {
            var projected = cameraView.WorldToViewportPoint(Origin + point);
            var rect = guide.rectTransform.rect;
            return rect.min + Vector2.Scale(rect.size, new Vector2(projected.x, projected.y));
        }

        private void DrawGuide(PointWirePath path, PointWireGraphic guide, float width, bool ground,
            Func<Vector2, Vector2> snapEnd = null)
        {
            if (path == null) return;
            guide.Clear();
            var points = path.Points;
            Vector2? end = null;
            bool loop = path.Closed;
            if (!path.Finished && guidePointer.HasValue &&
                TryRay(guidePointer.Value, guideEventCamera, out var ray) && TryPoint(ray, out var hover))
            {
                end = ground ? snapGroundPoint(hover) : snapEnd?.Invoke(hover) ?? hover;
                if (points.Count == 0)
                {
                    if (ground && end.HasValue) DrawPathStart(guide, end.Value, width);
                    return;
                }
                if (end.HasValue && NearStart(path, guide, hover)) loop = true;
                if (loop || end.HasValue && (end.Value - points[points.Count - 1]).sqrMagnitude < .0001f)
                    end = null;
            }
            if (points.Count == 0) return;
            if ((ground || snapEnd != null) && end.HasValue) DrawGuideMarker(guide, end.Value, 4f);

            IReadOnlyList<Vector2> ribbon = points;
            if (ground && curveGroundPath)
            {
                LotPathSurface.RoundCorners(points, end, loop, width, roundedGuide);
                ribbon = roundedGuide;
                end = null;
            }

            int count = ribbon.Count + (end.HasValue ? 1 : 0);
            int segments = loop ? count : count - 1;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % count;
                var a = PointAt(i);
                var b = PointAt(next);
                var side = Vector2.Perpendicular((b - a).normalized);
                var sideA = ground ? LotPathSurface.RibbonSide(ribbon, end, loop, i, width) : side * (width * .5f);
                var sideB = ground ? LotPathSurface.RibbonSide(ribbon, end, loop, next, width) : side * (width * .5f);
                var leftA = ProjectGuidePoint(guide, a - sideA);
                var leftB = ProjectGuidePoint(guide, b - sideB);
                var rightA = ProjectGuidePoint(guide, a + sideA);
                var rightB = ProjectGuidePoint(guide, b + sideB);
                if (ground)
                {
                    guide.AddFill(leftA, rightA, rightB, leftB);
                    if (!loop && i == 0) guide.Add(leftA, rightA);
                    if (!loop && i == segments - 1) guide.Add(leftB, rightB);
                }
                guide.Add(leftA, leftB);
                guide.Add(rightA, rightB);
            }

            for (int i = 0; i < points.Count; i++)
                DrawGuideMarker(guide, points[i], i == 0 ? 5f : 3f);

            Vector2 PointAt(int i) => i < ribbon.Count ? ribbon[i] : end.GetValueOrDefault();
        }

        private void DrawPathStart(PointWireGraphic guide, Vector2 point, float width)
        {
            // The initial gauge spans the lot X axis until the second point defines a heading.
            var halfWidth = Vector2.right * (width * .5f);
            var left = ProjectGuidePoint(guide, point - halfWidth);
            var right = ProjectGuidePoint(guide, point + halfWidth);
            var tick = Vector2.Perpendicular((right - left).normalized) * 5f;
            guide.Add(left, right);
            guide.Add(left - tick, left + tick);
            guide.Add(right - tick, right + tick);
            DrawGuideMarker(guide, point, 5f);
        }

        private void DrawGuideMarker(PointWireGraphic guide, Vector2 point, float radius)
        {
            var center = ProjectGuidePoint(guide, point);
            var x = Vector2.right * radius;
            var y = Vector2.up * radius;
            guide.Add(center - x, center + y);
            guide.Add(center + y, center + x);
            guide.Add(center + x, center - y);
            guide.Add(center - y, center - x);
        }

        private void DrawSelectedPathOutline()
        {
            if (selectedPathEdges == null || selectedPathEdges.Length < 4) return;
            var guide = pathSelectionGuide;
            guide.Clear();
            int count = selectedPathEdges.Length;
            int end = selectedPathClosed ? count : count - 2;
            for (int i = 0; i < end; i += 2)
            {
                int next = (i + 2) % count;
                guide.Add(Project(i), Project(next));
                guide.Add(Project(i + 1), Project(next + 1));
            }
            if (!selectedPathClosed)
            {
                guide.Add(Project(0), Project(1));
                guide.Add(Project(count - 2), Project(count - 1));
            }

            Vector2 Project(int index) => ProjectGuidePoint(guide, selectedPathEdges[index]);
        }

        private void LateUpdate()
        {
            if (!image) return;
            if (!animationsPaused)
            {
                characterPreview.Tick(Time.unscaledDeltaTime, draggedObject == null ? null : selectionFootprints.Keys);
                foreach (var pair in selectionFootprints)
                    if (instances.TryGetValue(pair.Key, out var selectedView))
                        pair.Value.localPosition = selectedView.localPosition + Vector3.up * .075f;
            }
            UpdatePlacementGhost();
            var rect = image.rectTransform.rect;
            var canvasScale = image.canvas.scaleFactor;
            int width = Mathf.Clamp(Mathf.RoundToInt(rect.width * canvasScale), 1, 4096);
            int height = Mathf.Clamp(Mathf.RoundToInt(rect.height * canvasScale), 1, 4096);
            if (!target || target.width != width || target.height != height)
            {
                ReleaseTarget();
                target = new RenderTexture(width, height, 24) { name = "Lot editor viewport" };
                cameraView.targetTexture = target;
                cameraView.aspect = (float)width / height;
                image.texture = target;
                ApplyPose();
            }
            UpdateFootprintCursor();
            DrawSelectedPathOutline();
            DrawGuide(repeatPath, repeatGuide, .36f, false, snapRepeatPoint);
            DrawGuide(fencePath, fenceGuide, .36f, false);
            DrawGuide(groundPath, groundGuide, groundPathWidth, true);
        }

        private void ApplyPose()
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            cameraView.orthographicSize = (span * .60f + 3f) * zoom / Mathf.Min(1, cameraView.aspect);
            // Fit the depth range around the visible ground so shadow cascades resolve small models.
            float depth = Mathf.Max(12f, cameraView.orthographicSize / Mathf.Sin(pitch * Mathf.Deg2Rad) + 8f);
            float distance = depth + 16f;
            cameraView.nearClipPlane = distance - depth;
            cameraView.farClipPlane = distance + depth;
            cameraView.transform.SetPositionAndRotation(Origin + focus - rotation * Vector3.forward * distance, rotation);
        }

        public void TopDown()
        {
            pitch = pitch == 90f ? CityForgeCameraController.IsometricPitch : 90f;
            focus = Vector3.zero;
            ApplyPose();
        }

        internal bool PickingEntryMarker { get; set; }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (PickingEntryMarker || eventData.button != PointerEventData.InputButton.Left ||
                !TryRay(eventData.pressPosition, eventData.pressEventCamera, out var ray)) return;
            if (HitsEntryMarker(eventData.pressPosition, eventData.pressEventCamera))
            {
                draggingEntryMarker = true;
                entryDragStart = previewLot.zoningLayout.entrance;
                entryDragOffset = TryPoint(ray, out var pressed) ? entryDragStart - pressed : Vector2.zero;
                eventData.eligibleForClick = false;
                SetFootprintCursor(EntryMarkerCursor);
                return;
            }
            var hit = Pick(ray);
            if (TryBeginFootprintDrag(eventData, ray, hit)) return;
            footprintSelected = false;
            if (previewLot.zoningLayout.Configured) DrawBuildingFootprint(previewLot.zoningLayout.BuildingSlot);
            if (!TryPlacementPoint(ray, hit != null && previewCharacters.Contains(hit), out var point)) return;
            eventData.eligibleForClick = false;
            beginMove(hit, point);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (draggingEntryMarker)
            {
                draggingEntryMarker = false;
                var point = entryMarker.localPosition;
                var position = new Vector2(point.x, point.z);
                if ((position - entryDragStart).sqrMagnitude > .000001f)
                    CommitEntryMarker?.Invoke(position);
                UpdateFootprintCursor();
                return;
            }
            if (footprintDragging)
            {
                footprintDragging = false;
                commitFootprint(footprintDraft);
                UpdateFootprintCursor();
                return;
            }
            endMove();
        }

        public void SetMoveState(string id, bool moving)
        {
            draggedObject = id;
            movingSelection = moving;
        }

        public Vector2? PreviewPosition(string id)
        {
            if (id == null || !instances.TryGetValue(id, out var instance)) return null;
            var point = instance.localPosition;
            return new Vector2(point.x, point.z);
        }

        public void RestartCharacterPreview(string id) => characterPreview.Restart(id);

        public void PreviewMove(string id, Vector2 point)
        {
            if (!instances.TryGetValue(id, out var instance)) return;
            float height = previewCharacters.Contains(id) ? CharacterDeckHeight(point) : 0;
            instance.localPosition = new Vector3(point.x, height, point.y);
            if (selectionFootprints.TryGetValue(id, out var selectionFootprint))
                selectionFootprint.localPosition = new Vector3(point.x, height + .075f, point.y);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (draggingEntryMarker)
                {
                    if (TryRay(eventData.position, eventData.pressEventCamera, out var entryRay) &&
                        TryPoint(entryRay, out var entryPoint))
                    {
                        var slot = previewLot.zoningLayout.BuildingSlot;
                        entryPoint += entryDragOffset;
                        entryMarker.localPosition = new Vector3(Mathf.Clamp(entryPoint.x, slot.xMin, slot.xMax),
                            entryMarker.localPosition.y, Mathf.Clamp(entryPoint.y, slot.yMin, slot.yMax));
                    }
                    return;
                }
                if (footprintDragging)
                {
                    if (TryRay(eventData.position, eventData.pressEventCamera, out var footprintRay) &&
                        TryPoint(footprintRay, out var footprintPoint)) DragBuildingFootprint(footprintPoint);
                    return;
                }
                if (TryRay(eventData.position, eventData.pressEventCamera, out var ray) &&
                    TryPlacementPoint(ray, draggedObject != null && previewCharacters.Contains(draggedObject), out var point))
                    updateMove(point);
                return;
            }
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                yaw += eventData.delta.x * .3f;
                pitch = Mathf.Clamp(pitch - eventData.delta.y * .25f, 18f, 90f);
            }
            else if (eventData.button == PointerEventData.InputButton.Middle)
            {
                float step = 2 * cameraView.orthographicSize / Mathf.Max(1, target ? target.height : Screen.height);
                var right = cameraView.transform.right;
                var forward = Vector3.Cross(right, Vector3.up);
                focus -= (right * eventData.delta.x + forward * eventData.delta.y) * step;
            }
            ApplyPose();
        }

        public void OnScroll(PointerEventData eventData) => ZoomBy(eventData.scrollDelta.y);

        public void ZoomBy(float steps)
        {
            zoom = Mathf.Clamp(zoom * Mathf.Pow(.88f, steps), .22f, 3f);
            ApplyPose();
        }

        public bool TryScreenNudge(Vector2 pixels, out Vector2 offset)
        {
            offset = default;
            var canvas = image.canvas;
            var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var center = RectTransformUtility.WorldToScreenPoint(eventCamera,
                image.rectTransform.TransformPoint(image.rectTransform.rect.center));
            if (!TryRay(center, eventCamera, out var startRay) ||
                !TryRay(center + pixels, eventCamera, out var endRay) ||
                !TryPoint(startRay, out var start) || !TryPoint(endRay, out var end)) return false;
            offset = end - start;
            return true;
        }

        private bool TryRay(Vector2 screenPoint, Camera eventCamera, out Ray ray)
        {
            ray = default;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, screenPoint,
                    eventCamera, out var local)) return false;
            var rect = image.rectTransform.rect;
            var normalized = new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
            ray = cameraView.ViewportPointToRay(normalized);
            return true;
        }

        private float CharacterDeckHeight(Vector2 point) =>
            LotCharacterDeck.TryHeight(previewLot, point, out float height) ? height : 0;

        private bool TryPlacementPoint(Ray ray, bool character, out Vector2 point) =>
            (character && LotCharacterDeck.TryPick(previewLot, ray, Origin, out point)) || TryPoint(ray, out point);

        private static bool TryPoint(Ray ray, out Vector2 point)
        {
            point = default;
            if (!new Plane(Vector3.up, Origin).Raycast(ray, out var distance)) return false;
            var hit = ray.GetPoint(distance) - Origin;
            point = new Vector2(hit.x, hit.z);
            return true;
        }

        public void SetCharacterFocus(bool enabled)
        {
            if (characterFocus == enabled) return;
            characterFocus = enabled;
            foreach (var (renderer, original) in buildingMaterials)
                if (renderer) renderer.sharedMaterials = enabled ? FadeBuildingMaterials(original) : original;
        }

        private Material[] FadeBuildingMaterials(Material[] original)
        {
            var slots = (Material[])original.Clone();
            for (int i = 0; i < slots.Length; i++)
            {
                var source = slots[i];
                if (!source || !source.HasProperty("_BaseColor")) continue;
                if (!fadedBuildingMaterials.TryGetValue(source, out var faded))
                {
                    faded = new Material(source);
                    var color = source.GetColor("_BaseColor");
                    color.a *= .70f;
                    faded.SetColor("_BaseColor", color);
                    faded.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    LotGroundTexture.ConfigureTransparentLayer(faded, LotGroundOverlay.MaxCount + 3);
                    fadedBuildingMaterials.Add(source, faded);
                    materials.Add(faded);
                }
                slots[i] = faded;
            }
            return slots;
        }

        private string Pick(Ray ray)
        {
            string id = null;
            float nearest = float.PositiveInfinity;
            bool pickedCharacter = false;
            foreach (var target in pickTargets)
            {
                if (!target.renderer || !target.renderer.enabled) continue;
                bool character = characterFocus && previewCharacters.Contains(target.id);
                if (pickedCharacter && !character) continue;
                var matrix = target.renderer.transform.worldToLocalMatrix;
                var direction = matrix.MultiplyVector(ray.direction);
                float scale = direction.magnitude;
                if (scale <= Mathf.Epsilon) continue;
                var local = new Ray(matrix.MultiplyPoint3x4(ray.origin), direction / scale);
                if (!target.renderer.localBounds.IntersectRay(local, out var distance) ||
                    (character == pickedCharacter && distance / scale >= nearest)) continue;
                nearest = distance / scale;
                id = target.id;
                pickedCharacter = character;
            }
            return id;
        }

        private LotPathDefinition PickPath(Ray ray)
        {
            // Paths are layered in draw order. Test the same triangles used for the visible surface.
            for (int i = pathTargets.Count - 1; i >= 0; i--)
            {
                var (path, vertices) = pathTargets[i];
                var plane = new Plane(Vector3.up, Origin + Vector3.up * vertices[0].y);
                if (!plane.Raycast(ray, out var distance)) continue;
                var point = ray.GetPoint(distance) - Origin;
                int segments = vertices.Length / 2 - (path.closed ? 0 : 1);
                for (int j = 0; j < segments; j++)
                {
                    int a = j * 2;
                    int b = (a + 2) % vertices.Length;
                    if (Inside(vertices[a], vertices[a + 1], vertices[b + 1], point) ||
                        Inside(vertices[a], vertices[b + 1], vertices[b], point))
                        return path;
                }
            }
            return null;

            static bool Inside(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
            {
                return Side(a, b, c) < -.0001f &&
                    Side(a, b, p) <= 0 && Side(b, c, p) <= 0 && Side(c, a, p) <= 0;
            }

            static float Side(Vector3 a, Vector3 b, Vector3 p) =>
                (b.x - a.x) * (p.z - a.z) - (b.z - a.z) * (p.x - a.x);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            bool character = ghostAsset?.IsCharacter == true ||
                (movingSelection && selectedObject != null && previewCharacters.Contains(selectedObject));
            if (eventData.button == PointerEventData.InputButton.Left &&
                TryRay(eventData.position, eventData.pressEventCamera, out var ray) &&
                TryPlacementPoint(ray, character, out var point))
            {
                if (PickingEntryMarker)
                {
                    click(point, null, null);
                    return;
                }
                if (HitsEntryMarker(eventData.position, eventData.pressEventCamera)) return;
                var objectHit = Pick(ray);
                if (CanEditFootprint && TryHitFootprint(eventData.position, eventData.pressEventCamera, point, objectHit, out _))
                {
                    footprintSelected = true;
                    selectFootprint();
                    DrawBuildingFootprint(previewLot.zoningLayout.BuildingSlot);
                    return;
                }
                footprintSelected = false;
                click(point, objectHit, objectHit == null ? PickPath(ray) : null);
            }
        }

        private void ReleaseTarget()
        {
            if (!target) return;
            if (cameraView) cameraView.targetTexture = null;
            target.Release();
            Destroy(target);
        }

        private void OnDisable()
        {
            footprintPointerInside = false;
            SetFootprintCursor(-1);
            if (stage) stage.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            SetFootprintCursor(-1);
            foreach (var cursor in footprintCursors)
                if (cursor) Destroy(cursor);
            ReleaseTarget();
            ClearPlacementGhost();
            if (stage) Destroy(stage.gameObject);
            Destroy(draftingTexture);
            foreach (var mesh in pathMeshes) Destroy(mesh);
            foreach (var material in materials) Destroy(material);
        }
    }
}

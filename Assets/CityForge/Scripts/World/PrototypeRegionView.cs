using System.Collections.Generic;
using CityForge.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CityForge.World
{
    public enum RegionSize { Small, Medium, Large }

    /// <summary>Temporary region prototype opened by Terrain's Next button.</summary>
    public sealed class PrototypeRegionView : MonoBehaviour
    {
        private readonly List<Object> generatedAssets = new List<Object>();
        private Canvas menuCanvas;
        private Camera menuCamera;
        private Terrain terrain;
        private Color previousAmbient;
        private AmbientMode previousAmbientMode;
        private Color previousSky, previousEquator, previousGround;
        private float previousAmbientIntensity;
        private SphericalHarmonicsL2 previousAmbientProbe;
        private bool previousFog;
        private bool returning;
        private Color previousFogColor;
        private FogMode previousFogMode;
        private float previousFogStart, previousFogEnd;
        private UniversalRenderPipelineAsset pipeline;
        private float previousShadowDistance;
        private Mesh wholeRegionMesh;
        private readonly Mesh[] districtMeshes = new Mesh[4];
        private readonly List<GameObject> trees = new List<GameObject>();
        private Text regionTitle, regionNote;
        private System.Random treeRandom;
        private GameObject[] treeVariants;
        private ForestPresentationSettings forestSettings;
        private Transform forestRoot;
        private Texture2D woodlandField;
        private readonly Dictionary<Vector2Int, List<Vector2>> canopyCells = new();
        private readonly List<Vector2> treePositions = new List<Vector2>();
        private float sampleStart, sampleElapsed;
        private int sampleFrames;
        private bool sampling;
        private string performanceReadout = "Measure at a fixed zoom for comparison.";
        private GameObject overviewButton;
        private bool testControlsVisible;
        public RegionSeason Season {get;private set;}
        public void SetSeason(RegionSeason season)
        {
            if(!System.Enum.IsDefined(typeof(RegionSeason),season))throw new System.ArgumentOutOfRangeException(nameof(season));
            Season=season;if(approvedDistrict)approvedDistrict.SetSeason(season);
            RefreshSeasonButtons();
        }
        void RefreshSeasonButtons()
        {
            var hud=transform.Find("RegionHud");if(!hud)return;
            foreach(RegionSeason season in System.Enum.GetValues(typeof(RegionSeason)))
            {
                var button=hud.Find("Season"+season);if(!button)continue;
                button.GetComponent<Button>().interactable=approvedDistrict&&approvedDistrict.gameObject.activeSelf;
                button.GetComponent<Image>().color=season==Season?new Color(.38f,.36f,.29f,.98f):new Color(.13f,.15f,.13f,.95f);
            }
        }

        // Retain the diagnostic controls for future terrain and performance tests.
        public void SetTestControlsVisible(bool visible)
        {
            testControlsVisible=visible;
            var hud=transform.Find("RegionHud");if(!hud)return;
            foreach(Transform child in hud)
            {
                if(child.name.StartsWith("ZoomLevel")||child.name=="ZoomLabel")child.gameObject.SetActive(visible);
                if(child.name=="Trees"||child.name=="SmallPlants")child.gameObject.SetActive(visible&&approvedDistrict&&approvedDistrict.gameObject.activeSelf);
                if(child.name.StartsWith("Season"))child.gameObject.SetActive(!visible);
            }
        }
        private Transform waterSurface;
        public LandscapeProfile Landscape { get; private set; }
        public bool NaturalLandscape { get; private set; }
        public bool PrefersMeadow=>wantsMeadow;
        public bool Waterless {get;private set;}
        public bool Coastline {get;private set;}
        public int CoastVersion {get;private set;}
        public bool StillWater {get;private set;} // Legacy trial flag retained for old saves.
        public int LakeWaterVersion {get;private set;}=1;
        public bool MinimalRiverCrest {get;private set;}
        public int RiverBedVersion {get;private set;}
        private Text landscapeCaption;
        private MeadowDistrictLandscape approvedDistrict;
        Transform overviewLakes;
        private bool wantsMeadow=true;
        private bool buildingRegion,deferLegacyTrees,legacyTreesReady;
        public MeadowDistrictLandscape ApprovedDistrict => approvedDistrict;
        public void SetNaturalLandscape(bool enabled)
        {
            wantsMeadow=enabled;
            UpdateLandscapeMode();
        }
        public void RegenerateApprovedDistrict()
        {
            SetNaturalLandscape(false);
            if(approvedDistrict){Destroy(approvedDistrict.gameObject);approvedDistrict=null;}
            SetNaturalLandscape(true);
        }
        private void UpdateLandscapeMode()
        {
            var baseline=Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline");
            bool lakeDistrict=false;
            if(SelectedDistrict>=0&&Data?.WaterBodies!=null){float side=MapSide*.5f;var area=new Rect(new Vector2(SelectedDistrict%2*.5f,SelectedDistrict/2*.5f),Vector2.one*.5f);
                foreach(var body in Data.WaterBodies)if((body.kind==WaterBodyKind.Lake||body.kind==WaterBodyKind.Pond)&&body.Bounds.Overlaps(area)){lakeDistrict=true;break;}}
            bool active=wantsMeadow && baseline!=null && ((CreationSettings!=null&&SelectedDistrict>=0)||SelectedDistrict==3||lakeDistrict||(MountainTest&&SelectedDistrict>=0));
            if(active&&approvedDistrict!=null&&approvedDistrict.DistrictIndex!=SelectedDistrict){approvedDistrict.ApplyPresentation(false);Destroy(approvedDistrict.gameObject);approvedDistrict=null;}
            Shader.SetGlobalFloat("_LandscapeEnabled",0); // Retire the rejected mesh approximation.
            if(active && approvedDistrict==null){
                var go=new GameObject("Approved meadow runtime district");go.transform.SetParent(transform,false);
                approvedDistrict=go.AddComponent<MeadowDistrictLandscape>();
                approvedDistrict.Build(this,baseline);
                approvedDistrict.SetSeason(Season);
            }
            if(!active&&!buildingRegion&&!legacyTreesReady){legacyTreesReady=true;BuildTrees(Data.Seed);}
            NaturalLandscape=active;
            var continuousCollider=transform.Find("ContinuousRegionTerrain");if(continuousCollider)continuousCollider.GetComponent<TerrainCollider>().enabled=!(active&&RiverBedVersion>=4);
            if(approvedDistrict!=null){approvedDistrict.ApplyPresentation(active);approvedDistrict.gameObject.SetActive(active);}
            var land=transform.Find("RegionLandCover");if(land)land.GetComponent<MeshRenderer>().enabled=!active;
            var forest=transform.Find("RandomTrees");if(forest)forest.gameObject.SetActive(!active);
            var sun=transform.Find("RegionSun");if(sun)sun.gameObject.SetActive(!active);
            var volume=transform.Find("RegionLightingVolume");if(volume)volume.gameObject.SetActive(!active);
            if(waterSurface)waterSurface.gameObject.SetActive(!active);
            foreach(Transform child in transform)if(child.name.StartsWith("Meadow tile ")||child.name.StartsWith("Distant tree"))child.gameObject.SetActive(!active);
            var grass=GetComponent<MeadowGrassRenderer>();if(grass)grass.enabled=!active;
            var distant=GetComponent<DistantTreeRenderer>();if(distant)distant.enabled=!active;
            if(active)sampling=false;
            var hud=transform.Find("RegionHud");if(hud)foreach(Transform child in hud)if(child.name.StartsWith("Relief"))child.gameObject.SetActive(!active);
            var pacingButton=transform.Find("RegionHud/FramePacing");if(pacingButton)pacingButton.gameObject.SetActive(active);
            var treesButton=transform.Find("RegionHud/Trees");if(treesButton)treesButton.gameObject.SetActive(active&&testControlsVisible);
            var plantsButton=transform.Find("RegionHud/SmallPlants");if(plantsButton)plantsButton.gameObject.SetActive(active&&testControlsVisible);
            if(!buildingRegion&&SelectedDistrict<0&&wantsMeadow&&!Waterless&&LakeWaterRenderer.UsesCalmWater(this)){
                if(!overviewLakes){var preview=new GameObject("Lakes and ponds overview");preview.transform.SetParent(transform,false);overviewLakes=preview.transform;
                    foreach(var body in Data.WaterBodies)if(LakeWaterRenderer.Applies(body,this)&&body.Bounds.width>0&&body.Bounds.height>0){var surface=new GameObject(body.kind+" overview — "+body.id);surface.transform.SetParent(overviewLakes,false);surface.AddComponent<LakeWaterRenderer>().InitializeOverview(this,body);}}
            }else if(overviewLakes){Destroy(overviewLakes.gameObject);overviewLakes=null;}
            RefreshSeasonButtons();
            if(landscapeCaption)landscapeCaption.text=active?"LANDSCAPE: MEADOW":CreationSettings!=null?"MEADOW: SELECT A DISTRICT":wantsMeadow&&SelectedDistrict!=3?"MEADOW: SELECT A LAKE DISTRICT":"LANDSCAPE: LEGACY";
        }
        public RegionCreationSettings CreationSettings {get;private set;}
        public int SelectedDistrict { get; private set; } = -1;
        public int TreeCount => trees.Count;
        public int WoodlandRevision { get; private set; }
        private static readonly string[] DistrictNames = { "Southwest", "Southeast", "Northwest", "Northeast" };

        public PrototypeRegionData Data { get; private set; }
        public string SavedRegionId {get;private set;}
        public MeadowRegionSnapshot LoadedMeadow {get;private set;}
        public int LoadedMeadowDistrict {get;private set;}=-1;
        public void ReleaseLoadedMeadow(){LoadedMeadow=null;}
        public bool LoadedFromSnapshot {get;private set;}
        public void SaveCurrentRegion()
        {
            var button=transform.Find("RegionHud/SaveRegion/Caption").GetComponent<Text>();
            try{var snapshot=SavedRegionStore.Capture(this);SavedRegionStore.Save(snapshot);SavedRegionId=snapshot.info.id;button.text="REGION SAVED";}
            catch(System.Exception e){button.text="SAVE FAILED";Debug.LogException(e);}
        }
        public string RegionName { get; private set; }
        public RegionSize Size { get; private set; }
        public float MapSide => 1024f * Mathf.Pow(2f, (int)Size);
        public float MapScale => MapSide / PrototypeRegionData.Side;
        public float SeaLevel => Coastline?CoastlineTestRegion.Sea:12f * MapScale;
        public float TerrainHeightRange {get;private set;}
        public bool MountainTest => SavedRegionId==HillsMountainTestRegion.Id;
        private float VerticalHeight => TerrainHeightRange>0?TerrainHeightRange:PrototypeRegionData.Height * MapScale;

        public static void Open(string regionName, Canvas canvas, int seed = PrototypeRegionData.DefaultSeed, RegionSize size = RegionSize.Medium, SavedRegionSnapshot snapshot = null)
        {
            if (FindFirstObjectByType<PrototypeRegionView>() != null) return;
            var view = new GameObject("PrototypeRegion").AddComponent<PrototypeRegionView>();
            view.menuCanvas = canvas;
            view.menuCamera = canvas != null ? canvas.worldCamera : Camera.main;
            view.previousAmbient = RenderSettings.ambientLight;
            view.previousAmbientMode = RenderSettings.ambientMode;
            view.previousSky = RenderSettings.ambientSkyColor;
            view.previousEquator = RenderSettings.ambientEquatorColor;
            view.previousGround = RenderSettings.ambientGroundColor;
            view.previousAmbientIntensity = RenderSettings.ambientIntensity;
            view.previousAmbientProbe = RenderSettings.ambientProbe;
            view.previousFog = RenderSettings.fog;
            view.previousFogColor = RenderSettings.fogColor;
            view.previousFogMode = RenderSettings.fogMode;
            view.previousFogStart = RenderSettings.fogStartDistance;
            view.previousFogEnd = RenderSettings.fogEndDistance;
            view.pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (view.pipeline != null) view.previousShadowDistance = view.pipeline.shadowDistance;
            try
            {
                if(snapshot!=null){view.CreationSettings=snapshot.info.creation;size=(RegionSize)snapshot.info.size;seed=snapshot.info.seed;regionName=snapshot.info.name;view.SavedRegionId=snapshot.info.id;view.Season=(RegionSeason)snapshot.info.season;view.TerrainHeightRange=snapshot.info.terrainHeightRange;view.LoadedMeadow=snapshot.meadow;view.LoadedMeadowDistrict=snapshot.info.meadowCacheDistrict;view.LoadedFromSnapshot=true;view.wantsMeadow=snapshot.info.meadow;view.Waterless=snapshot.info.waterless;view.Coastline=snapshot.info.coastline;view.CoastVersion=snapshot.info.coastVersion;view.MinimalRiverCrest=snapshot.info.minimalRiverCrest;view.StillWater=snapshot.info.stillWater;view.LakeWaterVersion=snapshot.info.lakeWaterVersion<0?-1:1;view.RiverBedVersion=snapshot.info.riverBedVersion;}
                view.Size = size;
                view.RegionName = string.IsNullOrWhiteSpace(regionName) ? "Test Region" : regionName.Trim();
                view.deferLegacyTrees=view.wantsMeadow&&Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline")!=null&&(snapshot==null||snapshot.info.district>=0);
                view.buildingRegion=true;view.Build(view.RegionName, seed,snapshot?.data);view.buildingRegion=false;
                if(snapshot!=null){if(snapshot.info.district>=0)view.EnterDistrict(snapshot.info.district);view.GetComponentInChildren<CityForgeCameraController>().RestoreRegionState(snapshot.info.camera);}
                else if(Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline")!=null)view.EnterDistrict(3);
                view.UpdateLandscapeMode();
                if (canvas != null) canvas.gameObject.SetActive(false);
                if (view.menuCamera != null) view.menuCamera.enabled = false;
            }
            catch
            {
                view.ReturnToMenu();
                throw;
            }
        }

        private void Build(string regionName, int seed, PrototypeRegionData savedData=null)
        {
            var buildTimer = System.Diagnostics.Stopwatch.StartNew();
            Data = savedData??new PrototypeRegionData(seed);if(Data.WaterBodies==null)Data.WaterBodies=RegionWaterBody.Generate(Data,RiverBedVersion,Waterless,MapSide);Data.SmoothRiverHeights=RiverBedVersion>=4;if(RiverBedVersion>=5)Data.ConfigureContinuousRiver(MapSide);
            var dataMs = buildTimer.ElapsedMilliseconds;
            Shader.SetGlobalFloat("_RegionSide", MapSide);
            var terrainData = Own(new TerrainData
            {
                name = "Prototype continuous regional heightfield",
                heightmapResolution = PrototypeRegionData.Resolution,
                alphamapResolution = 256,
                size = new Vector3(MapSide, VerticalHeight, MapSide)
            });
            terrainData.SetHeights(0, 0, Data.Heights);
            var terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "ContinuousRegionTerrain";
            terrainObject.transform.SetParent(transform);
            terrainObject.transform.position = new Vector3(-MapSide / 2f, 0f, -MapSide / 2f);
            terrain = terrainObject.GetComponent<Terrain>();
            terrain.drawHeightmap = false;
            Landscape = Resources.Load<LandscapeProfile>("Region/ApprovedLandscapeProfile");
            Shader.SetGlobalFloat("_LandscapeEnabled",0);
            BuildLandSurface();

            if(!Waterless){
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "SeaAndConnectedRiverWater";
            water.transform.SetParent(transform);
            water.transform.position = new Vector3(0f, SeaLevel, 0f);
            water.transform.localScale = Vector3.one * (MapSide / 10f);
            waterSurface = water.transform;
            Destroy(water.GetComponent<Collider>());
            var waterMaterial = Own(new Material(Shader.Find("CityForge/Region Water")));
            water.GetComponent<Renderer>().sharedMaterial = waterMaterial;
            }

            var sun = new GameObject("RegionSun").AddComponent<Light>();
            sun.transform.SetParent(transform);
            sun.type = LightType.Directional;
            sun.intensity = 1.55f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .64f;
            sun.shadowBias = .015f;
            sun.shadowNormalBias = .1f;
            sun.color = new Color(1f, .94f, .82f);
            sun.transform.rotation = Quaternion.Euler(32f, 205f, 0f);
            // Directional sky fill keeps shaded foliage readable without flattening sunlit turf.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.32f, .38f, .46f);
            RenderSettings.ambientEquatorColor = new Color(.22f, .245f, .25f);
            RenderSettings.ambientGroundColor = new Color(.105f, .095f, .08f);
            RenderSettings.ambientIntensity = 1f;
            // Runtime worlds have no baked sky probe; provide diffuse sky fill explicitly.
            var skyFill = new SphericalHarmonicsL2();
            skyFill.AddAmbientLight(new Color(.32f, .35f, .39f));
            skyFill.AddDirectionalLight(Vector3.up, new Color(.30f, .38f, .48f), .65f);
            skyFill.AddDirectionalLight(Vector3.down, new Color(.22f, .19f, .14f), .35f);
            RenderSettings.ambientProbe = skyFill;
            RenderSettings.fog = false;

            var cameraObject = new GameObject("RegionCamera");
            cameraObject.transform.SetParent(transform);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.allowHDR = true;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var volume = new GameObject("RegionLightingVolume").AddComponent<Volume>();
            volume.transform.SetParent(transform);
            volume.isGlobal = true;
            volume.priority = 20;
            var profile = Own(ScriptableObject.CreateInstance<VolumeProfile>());
            profile.name = "Region daylight presentation";
            volume.sharedProfile = profile;
            var tonemapping = Own(profile.Add<Tonemapping>(true));
            tonemapping.mode.Override(TonemappingMode.ACES);
            var color = Own(profile.Add<ColorAdjustments>(true));
            color.postExposure.Override(.35f);
            color.contrast.Override(0f);
            color.saturation.Override(-3f);
            camera.nearClipPlane = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.075f, .105f, .12f);
            cameraObject.AddComponent<CityForgeCameraController>().FrameRegion(MapSide, VerticalHeight);
            BuildDistrictLines();
            var surfaceMs = buildTimer.ElapsedMilliseconds;
            if(!deferLegacyTrees){legacyTreesReady=true;BuildTrees(seed);}
            Debug.Log($"Region build timing: fields/heights {dataMs} ms, terrain/surface {surfaceMs-dataMs} ms, trees {buildTimer.ElapsedMilliseconds-surfaceMs} ms.");
            BuildHud(regionName, seed);
            gameObject.AddComponent<DistantTreeRenderer>();
            gameObject.AddComponent<MeadowGrassRenderer>();
            Debug.Log("CityForge: generated buildable coastal prototype, seed " + seed);
        }

        private void BuildLandSurface()
        {
            var kit = Resources.Load<RegionTerrainKit>("Region/TerrainKit");
            if (kit == null) throw new System.InvalidOperationException("Build the Region PBR Kit from the CityForge editor menu first.");
            const int n = PrototypeRegionData.Resolution;
            var field = Own(new Texture2D(n, n, TextureFormat.RGBAFloat, false, true));
            field.wrapMode = TextureWrapMode.Clamp;
            field.filterMode = FilterMode.Bilinear;
            var pixels = new Color[n * n];
            var vertices = new Vector3[n * n];
            var uv = new Vector2[n * n];
            var triangles = new int[(n - 1) * (n - 1) * 6];
            var k = 0;
            for (var z = 0; z < n; z++) for (var x = 0; x < n; x++)
            {
                var u = x / (float)(n - 1); var v = z / (float)(n - 1);
                var i = z * n + x;
                vertices[i] = new Vector3((u - .5f) * MapSide,
                    Data.Heights[z,x] * VerticalHeight, (v - .5f) * MapSide);
                uv[i] = new Vector2(u,v);
                pixels[i] = new Color(Mathf.Clamp01(Data.WaterDistance[z,x] / 2000f),
                    Mathf.Clamp01(.5f + (v - PrototypeRegionData.Coast(u)) * PrototypeRegionData.Side / 3000f),
                    Mathf.PerlinNoise(13f + u * 7f, 29f + v * 7f),
                    // Preserve the shader field's 600 m encoding as TerrainData's range grows.
                    Data.Heights[z,x] * PrototypeRegionData.Height / 600f);
                if (x == n - 1 || z == n - 1) continue;
                triangles[k++] = i; triangles[k++] = i + n; triangles[k++] = i + 1;
                triangles[k++] = i + 1; triangles[k++] = i + n; triangles[k++] = i + n + 1;
            }
            field.SetPixels(pixels); field.Apply(); kit.Bind(field);
            Shader.SetGlobalTexture("_MeadowCoverField", Own(MeadowCoverFields.Build(MapSide)));
            var mesh = Own(new Mesh { name = "Frozen region PBR surface", indexFormat = IndexFormat.UInt32 });
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            wholeRegionMesh = mesh;
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            var land = new GameObject("RegionLandCover", typeof(MeshFilter), typeof(MeshRenderer));
            land.transform.SetParent(transform, false);
            land.GetComponent<MeshFilter>().sharedMesh = mesh;
            land.GetComponent<MeshRenderer>().sharedMaterial = kit.terrainMaterial;
        }

        private void BuildDistrictLines()
        {
            var material = Own(new Material(Shader.Find("Universal Render Pipeline/Unlit")));
            material.color = new Color(.78f, .71f, .48f);
            for (var axis = 0; axis < 2; axis++)
            {
                var line = new GameObject("DistrictDivision" + axis).AddComponent<LineRenderer>();
                line.transform.SetParent(transform);
                line.sharedMaterial = material;
                line.positionCount = 257;
                line.widthMultiplier = 28f * MapScale;
                line.shadowCastingMode = ShadowCastingMode.Off;
                for (var i = 0; i < 257; i++)
                {
                    var u = axis == 0 ? .5f : i / 256f;
                    var v = axis == 0 ? i / 256f : .5f;
                    line.SetPosition(i, new Vector3((u - .5f) * MapSide,
                        (Mathf.Max(12f, Data.Elevation(u, v)) + 10f) * MapScale, (v - .5f) * MapSide));
                }
            }
        }

        private void BuildHud(string regionName, int seed)
        {
            var root = new GameObject("RegionHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var title = HudText(root.transform, "RegionTitle", font, 26);
            regionTitle = title;
            title.text = (string.IsNullOrWhiteSpace(regionName) ? "TEST REGION" : regionName)
                + "  /  FOUR DISTRICTS";
            Position(title.rectTransform, new Vector2(25f, -20f), new Vector2(1250f, 42f));
            var subtitle = HudText(root.transform, "PrototypeNote", font, 17);
            regionNote = subtitle;
            subtitle.text = Size.ToString().ToUpperInvariant() + "  ·  " + MapSide.ToString("N0") + " × " + MapSide.ToString("N0") + " m  ·  SEED " + seed;
            Position(subtitle.rectTransform, new Vector2(25f, -64f), new Vector2(1250f, 30f));
            var controls = HudText(root.transform, "Controls", font, 17);
            controls.text = "Arrows / WASD: pan   ·   Wheel / + / −: zoom   ·   Q/E: rotate   ·   I: camera angle   ·   Home: reset   ·   Click a district: enter   ·   Esc: back";
            controls.rectTransform.anchorMin = controls.rectTransform.anchorMax = new Vector2(0f, 0f);
            controls.rectTransform.pivot = Vector2.zero;
            controls.rectTransform.anchoredPosition = new Vector2(25f, 20f);
            controls.rectTransform.sizeDelta = new Vector2(1500f, 30f);
            var back = new GameObject("ReturnToMenu", typeof(RectTransform), typeof(Image), typeof(Button));
            back.transform.SetParent(root.transform, false);
            var rect = back.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-25f, -25f);
            rect.sizeDelta = new Vector2(205f, 48f);
            back.GetComponent<Image>().color = new Color(.13f, .15f, .13f, .95f);
            back.GetComponent<Button>().onClick.AddListener(ReturnToMenu);
            var caption = HudText(back.transform, "Caption", font, 19);
            caption.text = "RETURN TO MENU";
            caption.alignment = TextAnchor.MiddleCenter;
            caption.rectTransform.anchorMin = Vector2.zero;
            caption.rectTransform.anchorMax = Vector2.one;
            caption.rectTransform.offsetMin = caption.rectTransform.offsetMax = Vector2.zero;
            var save=new GameObject("SaveRegion",typeof(RectTransform),typeof(Image),typeof(Button));save.transform.SetParent(root.transform,false);
            var saveRect=save.GetComponent<RectTransform>();saveRect.anchorMin=saveRect.anchorMax=saveRect.pivot=new Vector2(1,1);saveRect.anchoredPosition=new Vector2(-245,-25);saveRect.sizeDelta=new Vector2(205,48);
            save.GetComponent<Image>().color=new Color(.13f,.15f,.13f,.95f);save.GetComponent<Button>().onClick.AddListener(SaveCurrentRegion);
            var saveCaption=HudText(save.transform,"Caption",font,19);saveCaption.text="SAVE REGION";saveCaption.alignment=TextAnchor.MiddleCenter;saveCaption.rectTransform.anchorMin=Vector2.zero;saveCaption.rectTransform.anchorMax=Vector2.one;saveCaption.rectTransform.offsetMin=saveCaption.rectTransform.offsetMax=Vector2.zero;
            var rig = GetComponentInChildren<CityForgeCameraController>();
            var zoomLabel = HudText(root.transform, "ZoomLabel", font, 16);
            zoomLabel.rectTransform.anchorMin = zoomLabel.rectTransform.anchorMax = zoomLabel.rectTransform.pivot = new Vector2(1,1);
            zoomLabel.rectTransform.anchoredPosition = new Vector2(-25,-182);
            zoomLabel.rectTransform.sizeDelta = new Vector2(205,28);
            zoomLabel.alignment = TextAnchor.MiddleRight;
            var zoomButtons = new Button[6];
            for (var i=0; i<6; i++)
            {
                var level = i;
                var button = new GameObject("ZoomLevel" + level, typeof(RectTransform), typeof(Image), typeof(Button));
                button.transform.SetParent(root.transform,false);
                var r = button.GetComponent<RectTransform>();
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(1,1);
                r.anchoredPosition = new Vector2(-25-(5-i)*42,-214); r.sizeDelta = new Vector2(37,34);
                zoomButtons[i] = button.GetComponent<Button>();
                zoomButtons[i].onClick.AddListener(() => rig.SetZoomLevel(level));
                var label = HudText(button.transform,"Caption",font,17); label.text = level.ToString();
                label.alignment = TextAnchor.MiddleCenter;
                label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            }
            System.Action<int> refreshZoom = level => {
                zoomLabel.text = "ZOOM " + level + " / 5";
                for (var i=0;i<6;i++) zoomButtons[i].GetComponent<Image>().color = i==level
                    ? new Color(.38f,.36f,.29f,.98f) : new Color(.13f,.15f,.13f,.95f);
            };
            rig.ZoomChanged += refreshZoom; refreshZoom(rig.ZoomLevel);
            overviewButton = new GameObject("RegionOverview",typeof(RectTransform),typeof(Image),typeof(Button));
            overviewButton.transform.SetParent(root.transform,false);
            var overviewRect = overviewButton.GetComponent<RectTransform>();
            overviewRect.anchorMin = overviewRect.anchorMax = overviewRect.pivot = new Vector2(1,1);
            overviewRect.anchoredPosition = new Vector2(-25,-330); overviewRect.sizeDelta = new Vector2(205,40);
            overviewButton.GetComponent<Image>().color = new Color(.13f,.15f,.13f,.95f);
            overviewButton.GetComponent<Button>().onClick.AddListener(ShowOverview);
            var overviewText = HudText(overviewButton.transform,"Caption",font,17); overviewText.text="REGION OVERVIEW";
            overviewText.alignment=TextAnchor.MiddleCenter;
            overviewText.rectTransform.anchorMin=Vector2.zero; overviewText.rectTransform.anchorMax=Vector2.one;
            overviewText.rectTransform.offsetMin=overviewText.rectTransform.offsetMax=Vector2.zero;
            overviewButton.SetActive(false);
            var landscapeButton = new GameObject("LandscapeMode",typeof(RectTransform),typeof(Image),typeof(Button));
            landscapeButton.transform.SetParent(root.transform,false);
            var landscapeRect = landscapeButton.GetComponent<RectTransform>();
            landscapeRect.anchorMin=landscapeRect.anchorMax=landscapeRect.pivot=new Vector2(1,1);
            landscapeRect.anchoredPosition=new Vector2(-25,-380); landscapeRect.sizeDelta=new Vector2(205,40);
            landscapeButton.GetComponent<Image>().color=new Color(.13f,.15f,.13f,.95f);
            landscapeButton.GetComponent<Button>().interactable=Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline")!=null;
            landscapeButton.GetComponent<Button>().onClick.AddListener(()=>SetNaturalLandscape(!wantsMeadow));
            landscapeCaption=HudText(landscapeButton.transform,"Caption",font,17);
            landscapeCaption.alignment=TextAnchor.MiddleCenter;
            landscapeCaption.rectTransform.anchorMin=Vector2.zero; landscapeCaption.rectTransform.anchorMax=Vector2.one;
            landscapeCaption.rectTransform.offsetMin=landscapeCaption.rectTransform.offsetMax=Vector2.zero;
            UpdateLandscapeMode();
            var plantButton=new GameObject("SmallPlants",typeof(RectTransform),typeof(Image),typeof(Button));plantButton.transform.SetParent(root.transform,false);
            var plantRect=plantButton.GetComponent<RectTransform>();plantRect.anchorMin=plantRect.anchorMax=plantRect.pivot=new Vector2(1,1);plantRect.anchoredPosition=new Vector2(-25,-362);plantRect.sizeDelta=new Vector2(205,40);
            plantButton.GetComponent<Image>().color=new Color(.13f,.15f,.13f,.95f);
            var plantCaption=HudText(plantButton.transform,"Caption",font,17);plantCaption.text="SMALL PLANTS: OFF";plantCaption.alignment=TextAnchor.MiddleCenter;plantCaption.rectTransform.anchorMin=Vector2.zero;plantCaption.rectTransform.anchorMax=Vector2.one;plantCaption.rectTransform.offsetMin=plantCaption.rectTransform.offsetMax=Vector2.zero;
            plantButton.GetComponent<Button>().onClick.AddListener(()=>{if(approvedDistrict)approvedDistrict.SetSmallPlantsEnabled(!approvedDistrict.SmallPlantsEnabled,true);});
            var treeButton=new GameObject("Trees",typeof(RectTransform),typeof(Image),typeof(Button));treeButton.transform.SetParent(root.transform,false);
            var treeRect=treeButton.GetComponent<RectTransform>();treeRect.anchorMin=treeRect.anchorMax=treeRect.pivot=new Vector2(1,1);treeRect.anchoredPosition=new Vector2(-25,-408);treeRect.sizeDelta=new Vector2(205,40);
            treeButton.GetComponent<Image>().color=new Color(.13f,.15f,.13f,.95f);
            var treeCaption=HudText(treeButton.transform,"Caption",font,17);treeCaption.text="TREES: "+(approvedDistrict&&approvedDistrict.TreesEnabled?"ON":"OFF");treeCaption.alignment=TextAnchor.MiddleCenter;treeCaption.rectTransform.anchorMin=Vector2.zero;treeCaption.rectTransform.anchorMax=Vector2.one;treeCaption.rectTransform.offsetMin=treeCaption.rectTransform.offsetMax=Vector2.zero;
            treeButton.GetComponent<Button>().onClick.AddListener(()=>{if(approvedDistrict)approvedDistrict.SetTreesEnabled(!approvedDistrict.TreesEnabled,true);});
            var pacingButton=new GameObject("FramePacing",typeof(RectTransform),typeof(Image),typeof(Button));pacingButton.transform.SetParent(root.transform,false);
            var pacingRect=pacingButton.GetComponent<RectTransform>();pacingRect.anchorMin=pacingRect.anchorMax=pacingRect.pivot=new Vector2(1,1);pacingRect.anchoredPosition=new Vector2(-25,-454);pacingRect.sizeDelta=new Vector2(205,40);
            pacingButton.GetComponent<Image>().color=new Color(.13f,.15f,.13f,.95f);
            var pacingCaption=HudText(pacingButton.transform,"Caption",font,15);pacingCaption.text=approvedDistrict&&approvedDistrict.FramePacing60?"FRAME PACE: 60 FPS":"FRAME PACE: UNCAPPED";pacingCaption.alignment=TextAnchor.MiddleCenter;pacingCaption.rectTransform.anchorMin=Vector2.zero;pacingCaption.rectTransform.anchorMax=Vector2.one;pacingCaption.rectTransform.offsetMin=pacingCaption.rectTransform.offsetMax=Vector2.zero;
            pacingButton.GetComponent<Button>().onClick.AddListener(()=>{if(approvedDistrict)approvedDistrict.SetFramePacing60(!approvedDistrict.FramePacing60,true);});
            // Visual placeholders: seasonal terrain, foliage and weather arrive in the next pass.
            var seasons=new[]{"Summer","Fall","Winter"};
            for(int i=0;i<seasons.Length;i++)
            {
                var season=new GameObject("Season"+seasons[i],typeof(RectTransform),typeof(Image),typeof(Button));
                season.transform.SetParent(root.transform,false);
                var seasonRect=season.GetComponent<RectTransform>();
                seasonRect.anchorMin=seasonRect.anchorMax=seasonRect.pivot=new Vector2(1,1);
                seasonRect.anchoredPosition=new Vector2(-25,-182-i*46);seasonRect.sizeDelta=new Vector2(205,40);
                var choice=(RegionSeason)i;
                season.GetComponent<Button>().onClick.AddListener(()=>SetSeason(choice));
                season.GetComponent<Image>().color=new Color(.13f,.15f,.13f,.95f);
                var seasonCaption=HudText(season.transform,"Caption",font,17);seasonCaption.text=seasons[i];
                seasonCaption.alignment=TextAnchor.MiddleCenter;
                seasonCaption.rectTransform.anchorMin=Vector2.zero;seasonCaption.rectTransform.anchorMax=Vector2.one;
                seasonCaption.rectTransform.offsetMin=seasonCaption.rectTransform.offsetMax=Vector2.zero;
            }
            SetTestControlsVisible(testControlsVisible);
            RefreshSeasonButtons();
            var diagnostics = gameObject.AddComponent<RegionPreviewDiagnostics>();
            diagnostics.Initialize(transform.Find("RegionLandCover").GetComponent<MeshRenderer>(),
                GetComponentsInChildren<LineRenderer>(), root.transform, font, SeaLevel, 10f * MapScale);
        }

        private static Text HudText(Transform parent, string name, Font font, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.color = new Color(.93f, .88f, .72f);
            label.raycastTarget = false;
            return label;
        }

        private static void Position(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private T Own<T>(T asset) where T : Object { generatedAssets.Add(asset); return asset; }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            { if (SelectedDistrict >= 0) ShowOverview(); else ReturnToMenu(); }
            if (sampling && Time.realtimeSinceStartup >= sampleStart)
            {
                sampleElapsed += Time.unscaledDeltaTime;
                sampleFrames++;
                if (sampleElapsed >= 5f)
                {
                    sampling = false;
                    var enabledTrees = trees.FindAll(t => t.activeSelf).Count;
                    performanceReadout = $"{sampleFrames/sampleElapsed:F1} FPS · {sampleElapsed*1000/sampleFrames:F1} ms/frame";
                    Debug.Log($"CityForge tree performance (Editor, 5 seconds): {TreeCount} total, {enabledTrees} enabled, district {SelectedDistrict}, zoom {GetComponentInChildren<CityForgeCameraController>().ZoomLevel}: {performanceReadout}");
                }
            }
            // Native mode hides the whole legacy forest. District/diagnostic transitions
            // already update it; do not walk inactive legacy trees every camera frame.
            if(!NaturalLandscape){
                var diagnostic = GetComponent<RegionPreviewDiagnostics>();
                if(diagnostic.IsEnabled!=lastTreeDiagnostic){lastTreeDiagnostic=diagnostic.IsEnabled;RefreshTrees();}
            }
        }

        private void OnGUI()
        {
            if(NaturalLandscape)return;
            var panel = new Rect(24, Screen.height-218, 560, 84);
            GUI.Box(panel, $"TREE PERFORMANCE · {TreeCount} trees · {(GetComponent<DistantTreeRenderer>()?.IsFar == true ? "distant tree silhouettes" : "3D meshes")}");
            if (GUI.Button(new Rect(34, Screen.height-190, 160, 25), "ADD 320 TREES")) AddTrees(TreeCount+320);
            if (GUI.Button(new Rect(204, Screen.height-190, 160, 25), "MEASURE 5s")) BeginTreeSample();
            GUI.Label(new Rect(34, Screen.height-160, 540, 25), sampling ? "Measuring… keep camera still" : "Last sample: "+performanceReadout);
            var pointer = Event.current;
            if (panel.Contains(pointer.mousePosition)) return;
            if (SelectedDistrict >= 0 || pointer.type != EventType.MouseDown || pointer.button != 0) return;
            // Use the event coordinates so a click is picked at its actual location,
            // including editor clicks that arrive without a preceding mouse-move event.
            var position = new Vector2(pointer.mousePosition.x, Screen.height - pointer.mousePosition.y);
            if (EventSystem.current != null)
            {
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
                if (hits.Count > 0) return;
            }
            if (TryEnterDistrict(GetComponentInChildren<Camera>().ScreenPointToRay(position))) pointer.Use();
        }

        public static int DistrictAt(Vector3 point) => (point.x >= 0 ? 1 : 0) + (point.z >= 0 ? 2 : 0);

        public bool TryEnterDistrict(Ray ray)
        {
            if (!terrain.GetComponent<TerrainCollider>().Raycast(ray,out var hit,100000f)) return false;
            EnterDistrict(DistrictAt(hit.point)); return true;
        }

        public void EnterDistrict(int index)
        {
            if (index<0 || index>3) throw new System.ArgumentOutOfRangeException(nameof(index));
            GetComponent<RegionPreviewDiagnostics>().SetDiagnostic(false,1);
            SelectedDistrict=index;
            if (districtMeshes[index]==null)
            {
                var mesh = Own(Instantiate(wholeRegionMesh));
                var indices = new List<int>();
                var startX=(index%2)*256; var startZ=(index/2)*256;
                for (var z=startZ;z<startZ+256;z++) for (var x=startX;x<startX+256;x++)
                {
                    var i=z*513+x;
                    indices.Add(i);indices.Add(i+513);indices.Add(i+1);
                    indices.Add(i+1);indices.Add(i+513);indices.Add(i+514);
                }
                mesh.triangles=indices.ToArray(); mesh.RecalculateBounds(); districtMeshes[index]=mesh;
            }
            transform.Find("RegionLandCover").GetComponent<MeshFilter>().sharedMesh=districtMeshes[index];
            var center = new Vector3(index%2==0 ? -MapSide/4 : MapSide/4,0,index/2==0 ? -MapSide/4 : MapSide/4);
            if(waterSurface){waterSurface.position=center+Vector3.up*SeaLevel;
            waterSurface.localScale=Vector3.one*(MapSide/20);}
            foreach (var line in GetComponentsInChildren<LineRenderer>(true)) line.gameObject.SetActive(false);
            GetComponentInChildren<CityForgeCameraController>().FrameArea(MapSide/2,VerticalHeight,center);
            regionTitle.text=RegionName+"  /  "+DistrictNames[index]+" District";
            regionNote.text=(MapSide/2).ToString("N0")+" × "+(MapSide/2).ToString("N0")+" m  ·  SEED "+Data.Seed;
            overviewButton.SetActive(true);
            RefreshTrees();
            UpdateLandscapeMode();
            if(NaturalLandscape){if(Coastline){var coastalCenter=new Vector3(approvedDistrict.Origin.x+approvedDistrict.Side/2,26,approvedDistrict.Origin.y+approvedDistrict.Side/2);GetComponentInChildren<CityForgeCameraController>().FrameMeadowComparison(MapSide/2,VerticalHeight,coastalCenter,32);GetComponentInChildren<CityForgeCameraController>().SetZoomLevel(5);return;}var focus=approvedDistrict.ComparisonFocus;focus.y=approvedDistrict.Height(new Vector2(focus.x,focus.z))+3;
                GetComponentInChildren<CityForgeCameraController>().FrameMeadowComparison(MapSide/2,VerticalHeight,focus);}
        }

        public void ShowOverview()
        {
            GetComponent<RegionPreviewDiagnostics>().SetDiagnostic(false,1);
            SelectedDistrict=-1;
            transform.Find("RegionLandCover").GetComponent<MeshFilter>().sharedMesh=wholeRegionMesh;
            if(waterSurface){waterSurface.position=Vector3.up*SeaLevel; waterSurface.localScale=Vector3.one*(MapSide/10);}
            foreach (var line in GetComponentsInChildren<LineRenderer>(true)) line.gameObject.SetActive(true);
            GetComponentInChildren<CityForgeCameraController>().FrameRegion(MapSide,VerticalHeight);
            regionTitle.text=RegionName+"  /  FOUR DISTRICTS";
            regionNote.text=Size.ToString().ToUpperInvariant()+"  ·  "+MapSide.ToString("N0")+" × "+MapSide.ToString("N0")+" m  ·  SEED "+Data.Seed;
            overviewButton.SetActive(false); RefreshTrees(); UpdateLandscapeMode();
        }

        private bool lastTreeDiagnostic;
        private void RefreshTrees()
        {
            var diagnostic=GetComponent<RegionPreviewDiagnostics>();
            lastTreeDiagnostic=diagnostic&&diagnostic.IsEnabled;
            foreach (var tree in trees) tree.SetActive(!lastTreeDiagnostic&&(SelectedDistrict<0 || DistrictAt(tree.transform.position)==SelectedDistrict));
        }

        private void BuildTrees(int seed)
        {
            forestSettings=ForestPresentationSettings.Load();
            treeVariants=Resources.LoadAll<GameObject>(forestSettings.resourceFolder);
            if (treeVariants.Length==0) { Debug.LogWarning("CityForge: import the region tree assets first."); return; }
            forestRoot=new GameObject("RandomTrees").transform; forestRoot.SetParent(transform,false);
            treeRandom=new System.Random(seed+9137);
            AddTrees(640);
        }

        public void AddTrees(int target)
        {
            if (treeVariants==null || treeVariants.Length==0) return;
            var timer=System.Diagnostics.Stopwatch.StartNew();
            var before=TreeCount;
            var random=treeRandom;
            for (var attempt=0;attempt<target*30 && trees.Count<target;attempt++)
            {
                var u=.02f+(float)random.NextDouble()*.96f; var v=.02f+(float)random.NextDouble()*.96f;
                var height=terrain.SampleHeight(new Vector3((u-.5f)*MapSide,0,(v-.5f)*MapSide));
                var x=Mathf.RoundToInt(u*512);var z=Mathf.RoundToInt(v*512);
                if (height<SeaLevel+1 || Data.WaterDistance[z,x]*MapScale<12
                    || terrain.terrainData.GetSteepness(u,v)>18) continue;
                var position=new Vector2((u-.5f)*MapSide,(v-.5f)*MapSide);
                var woodland = MeadowCoverFields.WoodlandDensity(position, MapSide);
                if (random.NextDouble() > Mathf.Lerp(.025f, .95f, woodland)) continue;
                var spacing = Mathf.Lerp(12f, 6.5f, woodland);
                if (treePositions.Exists(p => (p-position).sqrMagnitude<spacing*spacing)) continue;
                var tree=Instantiate(treeVariants[random.Next(treeVariants.Length)],forestRoot);
                tree.name=tree.name.Replace("(Clone)","")+"_"+trees.Count;
                tree.transform.position=new Vector3(position.x,height-.08f,position.y);
                tree.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                tree.transform.localScale*=Mathf.Lerp(.75f,1.25f,(float)random.NextDouble());
                trees.Add(tree);treePositions.Add(position);
            }
            // Only new members of the NE test stand are packed; later Add Trees
            // does not compress existing members again. Re-sample actual ground.
            for(var i=before;i<trees.Count;i++)
            {
                var original=treePositions[i]; var packed=forestSettings.PackTestStand(original,MapSide);
                if(packed==original) continue;
                var u=packed.x/MapSide+.5f; var v=packed.y/MapSide+.5f;
                var x=Mathf.RoundToInt(u*512); var z=Mathf.RoundToInt(v*512);
                var height=terrain.SampleHeight(new Vector3(packed.x,0,packed.y));
                if(height<SeaLevel+1 || Data.WaterDistance[z,x]*MapScale<12 || terrain.terrainData.GetSteepness(u,v)>18) continue;
                treePositions[i]=packed; trees[i].transform.position=new Vector3(packed.x,height-.08f,packed.y);
            }
            RebuildWoodlandCover();
            RefreshTrees();
            Debug.Log($"CityForge: added {TreeCount-before} trees in {timer.ElapsedMilliseconds} ms; {TreeCount} total from {treeVariants.Length} variants.");
            BeginTreeSample();
        }

        public Vector2 SampleWoodland(Vector2 position)
        {
            var cell = Vector2Int.FloorToInt(position / 32f);
            var nearest = 32f;
            for (var z=-1; z<=1; z++) for (var x=-1; x<=1; x++)
                if (canopyCells.TryGetValue(cell + new Vector2Int(x,z), out var points))
                    foreach (var point in points) nearest = Mathf.Min(nearest, Vector2.Distance(position,point));
            return new Vector2(1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,12,nearest)),
                1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,30,nearest)));
        }

        private void RebuildWoodlandCover()
        {
            WoodlandRevision++;
            canopyCells.Clear();
            foreach (var position in treePositions)
            {
                var cell=Vector2Int.FloorToInt(position/32f);
                if (!canopyCells.TryGetValue(cell,out var points)) canopyCells[cell]=points=new List<Vector2>();
                points.Add(position);
            }
            if (woodlandField != null) Destroy(woodlandField);
            const int n=513;
            woodlandField=Own(new Texture2D(n,n,TextureFormat.RGBA32,false,true) {
                name="Actual tree canopy and woodland edge",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear });
            var pixels=new Color[n*n];
            for (var z=0;z<n;z++) for (var x=0;x<n;x++)
            {
                var cover=SampleWoodland(new Vector2(x/(float)(n-1)-.5f,z/(float)(n-1)-.5f)*MapSide);
                pixels[z*n+x]=new Color(cover.x,cover.y,0,1);
            }
            woodlandField.SetPixels(pixels);woodlandField.Apply(false,true);
            Shader.SetGlobalTexture("_WoodlandCoverField",woodlandField);
        }

        private void BeginTreeSample()
        {
            sampleStart=Time.realtimeSinceStartup+2f;
            sampleElapsed=0f; sampleFrames=0; sampling=true;
        }
        public void ReturnToMenu()
        {
            if (returning) return;
            returning = true;
            gameObject.SetActive(false);
            if (menuCanvas != null) menuCanvas.gameObject.SetActive(true);
            if (menuCamera != null) menuCamera.enabled = true;
            RestorePresentationSettings();
            Destroy(gameObject);
        }
        private void RestorePresentationSettings()
        {
            RenderSettings.ambientLight = previousAmbient;
            RenderSettings.ambientMode = previousAmbientMode;
            RenderSettings.ambientSkyColor = previousSky;
            RenderSettings.ambientEquatorColor = previousEquator;
            RenderSettings.ambientGroundColor = previousGround;
            RenderSettings.ambientIntensity = previousAmbientIntensity;
            RenderSettings.ambientProbe = previousAmbientProbe;
            RenderSettings.fog = previousFog;
            RenderSettings.fogColor = previousFogColor;
            RenderSettings.fogMode = previousFogMode;
            RenderSettings.fogStartDistance = previousFogStart;
            RenderSettings.fogEndDistance = previousFogEnd;
            if (pipeline != null) pipeline.shadowDistance = previousShadowDistance;
        }
        private void OnDestroy()
        {
            // Stopping Editor Play mode also destroys this view without going through ReturnToMenu.
            RestorePresentationSettings();
            Shader.SetGlobalFloat("_LandscapeEnabled", 0);
            Shader.SetGlobalTexture("_RegionField", null);
            Shader.SetGlobalTexture("_MeadowCoverField", null);
            Shader.SetGlobalTexture("_WoodlandCoverField", null);
            foreach (var asset in generatedAssets) if (asset != null) Destroy(asset);
        }
    }
}

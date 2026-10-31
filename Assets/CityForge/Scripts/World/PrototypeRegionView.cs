using System.Collections.Generic;
using System.Collections;
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
    public sealed partial class PrototypeRegionView : MonoBehaviour
    {
        private readonly List<Object> generatedAssets = new List<Object>();
        private GameObject districtSelection;
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
        private Mesh[] districtMeshes;
        RegionReliefMap relief;
        public int GridSide=>CreationSettings?.GridSide??2;
        public int DistrictCount=>GridSide*GridSide;
        public float DistrictSide=>MapSide/GridSide;
        public Vector2 DistrictOrigin(int index)=>new Vector2(index%GridSide*DistrictSide-MapSide/2,index/GridSide*DistrictSide-MapSide/2);
        public int DistrictAtWorld(Vector3 point){if(point.x< -MapSide/2||point.z< -MapSide/2||point.x>MapSide/2||point.z>MapSide/2)return -1;return Mathf.Min(GridSide-1,Mathf.FloorToInt((point.x+MapSide/2)/DistrictSide))+GridSide*Mathf.Min(GridSide-1,Mathf.FloorToInt((point.z+MapSide/2)/DistrictSide));}
        private readonly List<GameObject> trees = new List<GameObject>();
        private Text regionTitle, regionNote;
        private Texture2D woodlandField;
        private readonly Dictionary<Vector2Int, List<Vector2>> canopyCells = new();
        private readonly List<Vector2> treePositions = new List<Vector2>();
        private float sampleStart, sampleElapsed;
        private int sampleFrames;
        private bool sampling;
        private string performanceReadout = "Measure at a fixed zoom for comparison.";
        private GameObject overviewButton;
        public RegionSeason Season {get;private set;}
        // Calendar state persists independently of a climate's visual seasonal response.
        public RegionSeason VisualSeason=>Climate==RegionClimate.Tropical?RegionSeason.Summer:Season;
        bool? hudDistrictMode;
        public void SetSeason(RegionSeason season)
        {
            if(!System.Enum.IsDefined(typeof(RegionSeason),season))throw new System.ArgumentOutOfRangeException(nameof(season));
            Season=season;if(approvedDistrict)approvedDistrict.SetSeason(VisualSeason);
            RefreshSeasonButtons();
        }
        void RefreshSeasonButtons()
        {
            var hud=transform.Find("RegionHud");if(!hud)return;
            bool district=SelectedDistrict>=0;
            if(hudDistrictMode!=district){hudDistrictMode=district;
                var back=hud.Find("ReturnToMenu/Caption");if(back)back.GetComponent<Text>().text=district?"RETURN TO REGION":"RETURN TO MENU";
                var save=hud.Find("SaveRegion/Caption");if(save)save.GetComponent<Text>().text=district?"SAVE DISTRICT":"SAVE REGION";
            }
            foreach(RegionSeason season in System.Enum.GetValues(typeof(RegionSeason)))
            {
                var button=hud.Find("Season"+season);if(!button)continue;
                button.gameObject.SetActive(SelectedDistrict>=0);
                bool available=Climate!=RegionClimate.Tropical||season==RegionSeason.Summer;
                button.GetComponent<Button>().interactable=available&&approvedDistrict&&approvedDistrict.gameObject.activeSelf;
                button.GetComponentInChildren<Text>().color=available?new Color(.92f,.91f,.82f,1):new Color(.48f,.48f,.43f,.7f);
                button.GetComponent<Image>().color=season==VisualSeason?new Color(.38f,.36f,.29f,.98f):new Color(.13f,.15f,.13f,.95f);
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
        private MeadowDistrictLandscape approvedDistrict;
        private MeadowDistrictLandscape approvedOverview;
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
            sessionMeadows.Remove(SelectedDistrict);LoadedMeadow=null;
            if(approvedDistrict){Destroy(approvedDistrict.gameObject);approvedDistrict=null;}
            SetNaturalLandscape(true);
        }
        private bool deferLandscapeUpdate;
        private System.Action<string,float> loadingProgress;
        private void UpdateLandscapeMode()
        {
            if(deferLandscapeUpdate)return;
            RegionBuildSteps.Complete(UpdateLandscapeModeStages());
        }
        private IEnumerator UpdateLandscapeModeStages()
        {
            var baseline=Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline");
            // Every district uses the approved biome, including original recipe-less saves.
            // The regional overview remains the lightweight relief renderer.
            bool active=SelectedDistrict>=0 && wantsMeadow && baseline!=null;
            if(approvedDistrict!=null&&(!active||approvedDistrict.DistrictIndex!=SelectedDistrict)){
                LoadedMeadow=approvedDistrict.CaptureSnapshot();LoadedMeadowDistrict=approvedDistrict.DistrictIndex;sessionMeadows[LoadedMeadowDistrict]=LoadedMeadow;
                approvedDistrict.ApplyPresentation(false);approvedDistrict.gameObject.SetActive(false);
                if(Data.Biomes!=null&&approvedDistrict.IsRegionOverview)approvedOverview=approvedDistrict;
                else Destroy(approvedDistrict.gameObject);
                approvedDistrict=null;
            }
            if(active&&approvedDistrict==null&&SelectedDistrict<0&&approvedOverview){approvedDistrict=approvedOverview;approvedDistrict.SetSeason(VisualSeason);}
            Shader.SetGlobalFloat("_LandscapeEnabled",0); // Retire the rejected mesh approximation.
            if(active && approvedDistrict==null){
                if(sessionMeadows.TryGetValue(SelectedDistrict,out var cachedMeadow)){LoadedMeadow=cachedMeadow;LoadedMeadowDistrict=SelectedDistrict;}
                var go=new GameObject("Approved meadow runtime district");go.transform.SetParent(transform,false);
                approvedDistrict=go.AddComponent<MeadowDistrictLandscape>();
                yield return approvedDistrict.BuildStages(this,baseline,Data.Biomes!=null&&SelectedDistrict<0,(stage,value)=>loadingProgress?.Invoke(stage,.4f+value*.55f));
                approvedDistrict.SetSeason(VisualSeason);
            }
            if(!active&&!buildingRegion&&!legacyTreesReady){legacyTreesReady=true;BuildTrees(Data.Seed);}
            NaturalLandscape=active;
            var continuousCollider=transform.Find("ContinuousRegionTerrain");if(continuousCollider)continuousCollider.GetComponent<TerrainCollider>().enabled=!(active&&(IsCombinedDistrict||RiverBedVersion>=4||CreationSettings?.version>=5));
            if(approvedDistrict!=null){approvedDistrict.ApplyPresentation(active);approvedDistrict.gameObject.SetActive(active);}
            if(active&&Data.Biomes!=null&&SelectedDistrict<0){var camera=GetComponentInChildren<Camera>();camera.farClipPlane=Mathf.Max(camera.farClipPlane,MapSide*10);}
            if(!relief){var map=new GameObject("Regional relief map");map.transform.SetParent(transform,false);relief=map.AddComponent<RegionReliefMap>();yield return relief.BuildStages(this);}
            relief.gameObject.SetActive(SelectedDistrict<0);
            GetComponent<DistrictCombiner>()?.RefreshBorders();
            var land=transform.Find("RegionLandCover");if(land)land.GetComponent<MeshRenderer>().enabled=!active&&SelectedDistrict>=0;
            var forest=transform.Find("RandomTrees");if(forest)forest.gameObject.SetActive(!active);
            var sun=transform.Find("RegionSun");if(sun)sun.gameObject.SetActive(!active);
            var volume=transform.Find("RegionLightingVolume");if(volume)volume.gameObject.SetActive(!active);
            if(waterSurface)waterSurface.gameObject.SetActive(!active&&SelectedDistrict>=0);
            foreach(Transform child in transform)if(child.name.StartsWith("Meadow tile ")||child.name.StartsWith("Distant tree"))child.gameObject.SetActive(!active);
            var grass=GetComponent<MeadowGrassRenderer>();if(grass)grass.enabled=!active&&SelectedDistrict>=0;
            var distant=GetComponent<DistantTreeRenderer>();if(distant)distant.enabled=!active;
            if(active)sampling=false;
            var hud=transform.Find("RegionHud");if(hud)foreach(Transform child in hud)if(child.name.StartsWith("Relief"))child.gameObject.SetActive(SelectedDistrict>=0&&!active);
            RefreshSeasonButtons();

        }
        public RegionDistrictInfo[] Districts {get;private set;}
        public RegionCreationSettings CreationSettings {get;private set;}
        public RegionClimate Climate=>CreationSettings?.climate??legacyClimate;
        RegionClimate legacyClimate;
        public bool AnimalsEnabled {get;private set;}
        public void SetAnimalsEnabled(bool enabled){AnimalsEnabled=enabled;if(approvedDistrict)approvedDistrict.RefreshWildlife();}
        public int SelectedDistrict { get; private set; } = -1;
        public int TreeCount => trees.Count;
        public int WoodlandRevision { get; private set; }
        private static readonly string[] DistrictNames = { "Southwest", "Southeast", "Northwest", "Northeast" };

        public PrototypeRegionData Data { get; private set; }
        public string SavedRegionId {get;private set;}
        // Session caches preserve the originally loaded district when other cells are visited.
        // Disk persistence still stores one last district; no save schema change.
        private readonly Dictionary<int,MeadowRegionSnapshot> sessionMeadows=new();
        public MeadowRegionSnapshot LoadedMeadow {get;private set;}
        public int LoadedMeadowDistrict {get;private set;}=-1;
        public void ReleaseLoadedMeadow(){LoadedMeadow=null;}
        public bool LoadedFromSnapshot {get;private set;}
        public void SaveCurrentRegion()
        {
            var button=transform.Find("RegionHud/SaveRegion/Caption").GetComponent<Text>();
            try{var snapshot=SavedRegionStore.Capture(this);SavedRegionStore.Save(snapshot);SavedRegionId=snapshot.info.id;button.text=SelectedDistrict>=0?"DISTRICT SAVED":"REGION SAVED";}
            catch(System.Exception e){button.text="SAVE FAILED";Debug.LogException(e);}
        }
        public string RegionName { get; private set; }
        public RegionSize Size { get; private set; }
        public float MapSide => 1024f * Mathf.Pow(2f, (int)Size)*(GridSide/2f);
        public float MapScale => MapSide / PrototypeRegionData.Side;
        public float SeaLevel => Coastline?CoastlineTestRegion.Sea:CreationSettings?.version>=5?(TropicalOceanDepth.DeepRecipe(CreationSettings)?TropicalOceanDepth.Sea:12f):12f * (MapScale/(GridSide/2f));
        public float TerrainHeightRange {get;private set;}
        public bool MountainTest => SavedRegionId==HillsMountainTestRegion.Id;
        private float VerticalHeight => TerrainHeightRange>0?TerrainHeightRange:PrototypeRegionData.Height * (MapScale/(GridSide/2f));

        public static void Open(string regionName, Canvas canvas, int seed = PrototypeRegionData.DefaultSeed, RegionSize size = RegionSize.Medium, SavedRegionSnapshot snapshot = null)
        {
            RegionBuildSteps.Complete(OpenStages(regionName,canvas,seed,size,snapshot));
#if UNITY_EDITOR
            // Historical synchronous asset/check tools explicitly work in a saved district.
            if(snapshot?.info.district>=0){var view=FindFirstObjectByType<PrototypeRegionView>();view.EnterDistrict(snapshot.info.district);view.GetComponentInChildren<CityForgeCameraController>().RestoreRegionState(snapshot.info.camera);}
#endif
        }
        public static IEnumerator OpenStages(string regionName,Canvas canvas,int seed=PrototypeRegionData.DefaultSeed,RegionSize size=RegionSize.Medium,SavedRegionSnapshot snapshot=null,System.Action<string,float> progress=null)
        {
            if(FindFirstObjectByType<PrototypeRegionView>()!=null)throw new System.InvalidOperationException("A region is already open.");
            var view = new GameObject("PrototypeRegion").AddComponent<PrototypeRegionView>();
            view.enabled=false;view.loadingProgress=progress;
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
            bool opened=false;
            try
            {
                progress?.Invoke("Preparing region",.15f);yield return null;
                if(snapshot!=null){view.CombinedDistricts=snapshot.info.combinedDistricts??System.Array.Empty<CombinedDistrictGroup>();view.CreationSettings=snapshot.info.creation;view.legacyClimate=snapshot.info.climate;view.AnimalsEnabled=snapshot.info.animals;size=(RegionSize)snapshot.info.size;seed=snapshot.info.seed;regionName=snapshot.info.name;view.SavedRegionId=snapshot.info.id;view.Season=(RegionSeason)snapshot.info.season;view.TerrainHeightRange=snapshot.info.terrainHeightRange;view.LoadedMeadow=snapshot.meadow;view.LoadedMeadowDistrict=snapshot.info.meadowCacheDistrict;view.LoadedFromSnapshot=true;view.wantsMeadow=snapshot.info.meadow;view.Waterless=snapshot.info.waterless;view.Coastline=snapshot.info.coastline;view.CoastVersion=snapshot.info.coastVersion;view.MinimalRiverCrest=snapshot.info.minimalRiverCrest;view.StillWater=snapshot.info.stillWater;view.LakeWaterVersion=snapshot.info.lakeWaterVersion<0?-1:1;view.RiverBedVersion=snapshot.info.riverBedVersion;}
                if(view.LoadedMeadow!=null&&view.LoadedMeadowDistrict>=0)view.sessionMeadows[view.LoadedMeadowDistrict]=view.LoadedMeadow;
                view.Size = size;
                view.RegionName = string.IsNullOrWhiteSpace(regionName) ? "Test Region" : regionName.Trim();
                view.deferLegacyTrees=view.wantsMeadow&&Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline")!=null&&(snapshot==null||snapshot.info.district>=0);
                view.deferLandscapeUpdate=true;view.buildingRegion=true;yield return view.BuildStages(view.RegionName,seed,snapshot?.data);view.buildingRegion=false;
                progress?.Invoke("Selecting saved district",.38f);yield return null;
                view.deferLandscapeUpdate=true;
                view.ShowOverview();
                view.deferLandscapeUpdate=false;
                var control=view.GetComponentInChildren<CityForgeCameraController>();control.enabled=false;
                var grass=view.GetComponent<MeadowGrassRenderer>();if(grass)grass.enabled=false;
                yield return view.UpdateLandscapeModeStages();
                if(view.SelectedDistrict<0)control.FrameRegion(view.MapSide,view.VerticalHeight);
                progress?.Invoke("Preparing first view",.98f);yield return null;
                view.loadingProgress=null;view.transform.Find("RegionHud").gameObject.SetActive(true);view.enabled=true;control.enabled=true;if(grass)grass.enabled=!view.NaturalLandscape&&view.SelectedDistrict>=0;
                opened=true;
                if (canvas != null) canvas.gameObject.SetActive(false);
                if (view.menuCamera != null) view.menuCamera.enabled = false;
            }
            finally
            {
                if(!opened&&view)view.ReturnToMenu();
            }
        }

        private IEnumerator BuildStages(string regionName,int seed,PrototypeRegionData savedData=null)
        {
            var buildTimer = System.Diagnostics.Stopwatch.StartNew();
            districtMeshes=new Mesh[DistrictCount];
            Data = savedData??new PrototypeRegionData(seed);if(Data.WaterBodies==null)Data.WaterBodies=RegionWaterBody.Generate(Data,RiverBedVersion,Waterless,MapSide);Data.SmoothRiverHeights=RiverBedVersion>=4;if(RiverBedVersion>=5)Data.ConfigureContinuousRiver(MapSide);
            if(CreationSettings!=null&&CreationSettings.version>=2){Data.Biomes=new RegionBiomeMap(CreationSettings,Data);Districts=Data.Biomes.DescribeDistricts();}
            loadingProgress?.Invoke("Creating shared terrain",.19f);yield return null;
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
            loadingProgress?.Invoke("Building regional surface",.24f);yield return null;
            BuildLandSurface();
            loadingProgress?.Invoke("Setting up region lighting",.29f);yield return null;

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
            var control=cameraObject.AddComponent<CityForgeCameraController>();control.FrameRegion(MapSide,VerticalHeight);control.enabled=false;
            BuildDistrictLines();
            var surfaceMs = buildTimer.ElapsedMilliseconds;
            if(!deferLegacyTrees){legacyTreesReady=true;BuildTrees(seed);}
            Debug.Log($"Region build timing: fields/heights {dataMs} ms, terrain/surface {surfaceMs-dataMs} ms, trees {buildTimer.ElapsedMilliseconds-surfaceMs} ms.");
            loadingProgress?.Invoke("Preparing region controls",.34f);yield return null;
            BuildHud(regionName, seed);
            if(Data.Biomes==null)gameObject.AddComponent<MeadowGrassRenderer>().enabled=false;
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
            Shader.SetGlobalTexture("_MeadowCoverField", Own(MeadowCoverFields.Build(MapSide,Data.Biomes)));
            var mesh = Own(new Mesh { name = "Frozen region PBR surface", indexFormat = IndexFormat.UInt32 });
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            wholeRegionMesh = mesh;
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            var land = new GameObject("RegionLandCover", typeof(MeshFilter), typeof(MeshRenderer));
            land.transform.SetParent(transform, false);
            land.GetComponent<MeshFilter>().sharedMesh = mesh;
            land.GetComponent<MeshRenderer>().sharedMaterial = kit.terrainMaterial;
        }

        private void BuildDistrictLines() { }

        private void BuildHud(string regionName, int seed)
        {
            var root = new GameObject("RegionHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            if(loadingProgress!=null)root.SetActive(false);
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildDistrictSelection(root.transform,font);
            gameObject.AddComponent<DistrictCombiner>().Initialize(this);
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
            back.GetComponent<Button>().onClick.AddListener(()=>{if(SelectedDistrict>=0)ShowOverview();else ReturnToMenu();});
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
            zoomLabel.rectTransform.anchoredPosition = new Vector2(-25,-87);
            zoomLabel.rectTransform.sizeDelta = new Vector2(205,28);
            zoomLabel.alignment = TextAnchor.MiddleRight;
            System.Action<int> refreshZoom = level => zoomLabel.text = "ZOOM " + level + " / 5";
            rig.ZoomChanged += refreshZoom; refreshZoom(rig.ZoomLevel);
            overviewButton = new GameObject("RegionOverview",typeof(RectTransform),typeof(Image),typeof(Button));
            overviewButton.transform.SetParent(root.transform,false);
            var overviewRect = overviewButton.GetComponent<RectTransform>();
            overviewRect.anchorMin = overviewRect.anchorMax = overviewRect.pivot = new Vector2(1,1);
            overviewRect.anchoredPosition = new Vector2(-25,-280); overviewRect.sizeDelta = new Vector2(205,40);
            overviewButton.GetComponent<Image>().color = new Color(.13f,.15f,.13f,.95f);
            overviewButton.GetComponent<Button>().onClick.AddListener(ShowOverview);
            var overviewText = HudText(overviewButton.transform,"Caption",font,17); overviewText.text="REGION OVERVIEW";
            overviewText.alignment=TextAnchor.MiddleCenter;
            overviewText.rectTransform.anchorMin=Vector2.zero; overviewText.rectTransform.anchorMax=Vector2.one;
            overviewText.rectTransform.offsetMin=overviewText.rectTransform.offsetMax=Vector2.zero;
            overviewButton.SetActive(false);
            UpdateLandscapeMode();
            var seasons=new[]{"Summer","Fall","Winter"};
            for(int i=0;i<seasons.Length;i++)
            {
                var season=new GameObject("Season"+seasons[i],typeof(RectTransform),typeof(Image),typeof(Button));
                season.transform.SetParent(root.transform,false);
                var seasonRect=season.GetComponent<RectTransform>();
                seasonRect.anchorMin=seasonRect.anchorMax=seasonRect.pivot=new Vector2(1,1);
                seasonRect.anchoredPosition=new Vector2(-25,-132-i*46);seasonRect.sizeDelta=new Vector2(205,40);
                var choice=(RegionSeason)i;
                season.GetComponent<Button>().onClick.AddListener(()=>SetSeason(choice));
                season.GetComponent<Image>().color=new Color(.13f,.15f,.13f,.95f);
                var seasonCaption=HudText(season.transform,"Caption",font,17);seasonCaption.text=seasons[i];
                seasonCaption.alignment=TextAnchor.MiddleCenter;
                seasonCaption.rectTransform.anchorMin=Vector2.zero;seasonCaption.rectTransform.anchorMax=Vector2.one;
                seasonCaption.rectTransform.offsetMin=seasonCaption.rectTransform.offsetMax=Vector2.zero;
            }
            CameraTiltControls.Build(this,root.transform,font);
            RefreshSeasonButtons();
            var diagnostics = gameObject.AddComponent<RegionPreviewDiagnostics>();
            diagnostics.Initialize(transform.Find("RegionLandCover").GetComponent<MeshRenderer>(),
                GetComponentsInChildren<LineRenderer>(), root.transform, font, SeaLevel, 10f * MapScale, false);
        }

        private void BuildDistrictSelection(Transform parent,Font font)
        {
            if(Districts==null)return;
            districtSelection=new GameObject("DistrictSelection",typeof(RectTransform));districtSelection.transform.SetParent(parent,false);
            Position(districtSelection.GetComponent<RectTransform>(),new Vector2(25,-115),new Vector2(430,235));
            var heading=HudText(districtSelection.transform,"Heading",font,17);
            heading.text=DistrictCount+" DISTRICTS · CLICK THE MAP TO ENTER";
            Position(heading.rectTransform,Vector2.zero,new Vector2(430,30));
            foreach(var district in Districts){
                if(GridSide>2)break;
                int index=district.index;
                var button=new GameObject("District_"+index,typeof(RectTransform),typeof(Image),typeof(Button));button.transform.SetParent(districtSelection.transform,false);
                Position(button.GetComponent<RectTransform>(),new Vector2(0,-35-index*45),new Vector2(430,40));
                button.GetComponent<Image>().color=new Color(.08f,.11f,.09f,.88f);
                var label=HudText(button.transform,"Caption",font,17);label.text=district.name+"  ·  "+RegionBiomeMap.Label(district.biome)+"  ·  "+Mathf.RoundToInt(district.woodlandFraction*100)+"% woodland";
                label.alignment=TextAnchor.MiddleLeft;label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(12,0);label.rectTransform.offsetMax=Vector2.zero;
                button.GetComponent<Button>().onClick.AddListener(()=>RequestDistrict(index));
            }
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
            if(SelectedDistrict>=0)return;
            var pointer = Event.current;
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
            if(relief&&relief.gameObject.activeSelf&&relief.TryPick(ray,out var district)){RequestDistrict(district);return true;}
            return false;
        }

        bool districtLoading;
        public bool DistrictLoading=>districtLoading;
        public void RequestDistrict(int index)
        {
            if(index<0||index>=DistrictCount)throw new System.ArgumentOutOfRangeException(nameof(index));
            if(districtLoading||SelectedDistrict>=0)return;
            var combiner=GetComponent<DistrictCombiner>();if(combiner&&combiner.Active){combiner.Select(index);return;}
            index=DistrictAnchor(index);
            districtLoading=true;var camera=GetComponentInChildren<CityForgeCameraController>();
            bool previousControl=camera.enabled;camera.enabled=false;enabled=false;
            string name=GridSide==2?DistrictNames[index]+" District":$"District {index%GridSide+1}–{index/GridSide+1}";
            var overlay=CityForge.UI.RegionLoadingOverlay.Show(RegionName+" / "+name,null,true);
            overlay.BeginDistrict(EnterDistrictStages(index,overlay.Report),()=>{
                if(!this)return;districtLoading=false;enabled=true;camera.enabled=previousControl;
            },error=>{
                if(!this)return;
                if(approvedDistrict){approvedDistrict.ApplyPresentation(false);approvedDistrict.gameObject.SetActive(false);Destroy(approvedDistrict.gameObject);approvedDistrict=null;}
                ShowOverview();regionNote.text="Could not open district. Please try again.";Debug.LogException(error);
            });
        }
        // Immediate entry remains available to existing editor checks and asset tools.
        public void EnterDistrict(int index)=>RegionBuildSteps.Complete(EnterDistrictStages(index));
        public IEnumerator EnterDistrictStages(int index,System.Action<string,float> progress=null)
        {
            if(index<0||index>=DistrictCount)throw new System.ArgumentOutOfRangeException(nameof(index));
            index=DistrictAnchor(index);
            float entryHeading=GetComponentInChildren<CityForgeCameraController>().CaptureRegionState().yaw;
            var previousProgress=loadingProgress;loadingProgress=progress;
            try{
                progress?.Invoke("Selecting district",.08f);yield return null;
            GetComponent<RegionPreviewDiagnostics>().SetDiagnostic(false,1);
            SelectedDistrict=index;if(districtSelection)districtSelection.SetActive(false);
            if (districtMeshes[index]==null)
            {
                var mesh = Own(Instantiate(wholeRegionMesh));
                var indices = new List<int>();
                int cells=512/GridSide;var startX=(index%GridSide)*cells; var startZ=(index/GridSide)*cells;
                for (var z=startZ;z<startZ+cells;z++) for (var x=startX;x<startX+cells;x++)
                {
                    var i=z*513+x;
                    indices.Add(i);indices.Add(i+513);indices.Add(i+1);
                    indices.Add(i+1);indices.Add(i+513);indices.Add(i+514);
                }
                mesh.triangles=indices.ToArray(); mesh.RecalculateBounds(); districtMeshes[index]=mesh;
            }
            transform.Find("RegionLandCover").GetComponent<MeshFilter>().sharedMesh=districtMeshes[index];
            var origin=DistrictOrigin(index);var center = new Vector3(origin.x+DistrictSide/2,0,origin.y+DistrictSide/2);
            if(waterSurface){waterSurface.position=center+Vector3.up*SeaLevel;
            waterSurface.localScale=Vector3.one*(DistrictSide/10);}
            foreach (var line in GetComponentsInChildren<LineRenderer>(true)) line.gameObject.SetActive(false);
            GetComponentInChildren<CityForgeCameraController>().FrameArea(DistrictSide,VerticalHeight,center);
            regionTitle.text=RegionName+"  /  "+(GridSide==2?DistrictNames[index]+" District":$"District {index%GridSide+1}–{index/GridSide+1}");
            regionNote.text=DistrictSide.ToString("N0")+" × "+DistrictSide.ToString("N0")+" m  ·  SEED "+Data.Seed;
            if(Districts!=null)regionNote.text+="  ·  "+RegionBiomeMap.Label(Districts[index].biome);
            overviewButton.SetActive(false);
            RefreshTrees();
            yield return UpdateLandscapeModeStages();
            if(NaturalLandscape&&IsCombinedDistrict){
                var bounds=ActiveDistrictBounds;var rig=GetComponentInChildren<CityForgeCameraController>();float high=0,low=VerticalHeight;var td=approvedDistrict.DisplayTerrain.terrainData;foreach(float h in td.GetHeights(0,0,td.heightmapResolution,td.heightmapResolution)){high=Mathf.Max(high,h*td.size.y);low=Mathf.Min(low,h*td.size.y);}
                float span=high-low;rig.FrameDistrictUnion(Mathf.Max(bounds.width,bounds.height),VerticalHeight,low,high,new Vector3(bounds.center.x,0,bounds.center.y));
                regionTitle.text=RegionName+"  /  COMBINED DISTRICT ("+ActiveDistrictMembers.Length+")";regionNote.text=bounds.width.ToString("N0")+" × "+bounds.height.ToString("N0")+" m · "+ActiveDistrictMembers.Length+" CONNECTED DISTRICTS";
                progress?.Invoke("Ready",1);yield return null;yield break;
            }
            if(NaturalLandscape&&approvedDistrict.RockyMountainBorder){
                var td=approvedDistrict.DisplayTerrain.terrainData;var h=td.GetHeights(0,0,td.heightmapResolution,td.heightmapResolution);float low=float.PositiveInfinity,high=0;
                foreach(float sample in h){low=Mathf.Min(low,sample*td.size.y);high=Mathf.Max(high,sample*td.size.y);}
                float range=high-low;var mountainCenter=new Vector3(approvedDistrict.Origin.x+DistrictSide/2,(low+high)/2-range*.0375f,approvedDistrict.Origin.y+DistrictSide/2);
                GetComponentInChildren<CityForgeCameraController>().FrameArea(DistrictSide,range,mountainCenter);
                progress?.Invoke("Ready",1f);yield return null;yield break;
            }
            if(NaturalLandscape){if(Coastline){var coastalCenter=new Vector3(approvedDistrict.Origin.x+approvedDistrict.Side/2,26,approvedDistrict.Origin.y+approvedDistrict.Side/2);GetComponentInChildren<CityForgeCameraController>().FrameMeadowComparison(DistrictSide,VerticalHeight,coastalCenter,32);GetComponentInChildren<CityForgeCameraController>().SetZoomLevel(5);yield break;}var focus=approvedDistrict.ComparisonFocus;focus.y=approvedDistrict.Height(new Vector2(focus.x,focus.z))+3;
                GetComponentInChildren<CityForgeCameraController>().FrameMeadowComparison(DistrictSide,VerticalHeight,focus);
                GetComponentInChildren<CityForgeCameraController>().SetZoomLevel(5);}
                progress?.Invoke("Ready",1f);yield return null;
            }finally{GetComponentInChildren<CityForgeCameraController>().SetHeading(entryHeading);loadingProgress=previousProgress;}
        }

        public void ShowOverview()
        {
            if(districtLoading)return;
            GetComponent<RegionPreviewDiagnostics>().SetDiagnostic(false,1);
            float returnHeading=GetComponentInChildren<CityForgeCameraController>().CaptureRegionState().yaw;
            SelectedDistrict=-1;if(districtSelection)districtSelection.SetActive(true);
            transform.Find("RegionLandCover").GetComponent<MeshFilter>().sharedMesh=wholeRegionMesh;
            if(waterSurface){waterSurface.position=Vector3.up*SeaLevel; waterSurface.localScale=Vector3.one*(MapSide/10);}
            foreach (var line in GetComponentsInChildren<LineRenderer>(true)) line.gameObject.SetActive(true);
            GetComponentInChildren<CityForgeCameraController>().FrameRegion(MapSide,VerticalHeight);
            GetComponentInChildren<CityForgeCameraController>().SetHeading(returnHeading);
            regionTitle.text=RegionName+"  /  "+DistrictCount+" DISTRICTS";
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

        // Retired: prototype broadleaf trees must never re-enter gameplay, including old saves.
        // Kept as no-op entry points for historical editor diagnostics.
        private void BuildTrees(int seed) { }
        public void AddTrees(int target) { }

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

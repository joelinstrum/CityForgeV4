using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WaveHarmonic.Crest;

namespace CityForge.World
{
    /// <summary>One generated district. Authoritative geographic data/collision remain owned by the region.</summary>
    public sealed partial class MeadowDistrictLandscape : MonoBehaviour
    {
        public PrototypeRegionView View {get;private set;}
        public MeadowRuntimeBaseline Baseline {get;private set;}
        public Terrain DisplayTerrain {get;private set;}
        public WaterRenderer Water {get;private set;}
        public Vector2 Origin {get;private set;}
        public float Side {get;private set;}
        public int DistrictIndex {get;private set;}
        public bool IsRegionOverview {get;private set;}
        public Vector2 SeedOffset {get;private set;}
        public int Seed => View.Data.Seed;
        public readonly List<Vector2> Road = new();
        readonly List<UnityEngine.Object> owned=new();
        readonly Dictionary<Vector2Int,List<Vector2>> canopy=new();
        TerrainData terrainData;float[,] shore;
        CityForge.CameraSystem.CityForgeCameraController navigationCamera;
        const float RoadCellSide=16;
        readonly Dictionary<Vector2Int,List<int>> roadCells=new();
        float roadQueryLimit;
        public string GenerationTimings {get;private set;}
        public bool UsedSavedTerrain {get;private set;}
        public bool SmallPlantsEnabled {get;private set;}
        public bool FramePacing60 {get;private set;}=true;
        const string FramePacingPreference="CityForge.Meadow.FramePacing60";
        int savedFrameRate,savedVSync;
        double pacingRefresh=-1;
        public void SetFramePacing60(bool enabled,bool remember=false)
        {
            FramePacing60=enabled;
            // Keep the legacy preference/API name so existing saves and tools remain compatible.
            // Synchronize near 60 Hz even on 120 Hz displays; keep hardware VSync instead of a software timer.
            pacingRefresh=-1;ApplyFramePacing();
            if(remember){PlayerPrefs.SetInt(FramePacingPreference,enabled?1:0);PlayerPrefs.Save();}
            var caption=View.transform.Find("RegionHud/FramePacing/Caption");if(caption)caption.GetComponent<UnityEngine.UI.Text>().text=enabled?"FRAME PACE: VSYNC":"FRAME PACE: UNCAPPED";
        }
        void ApplyFramePacing()
        {
            if(!settingsApplied)return;
            double refresh=Screen.currentResolution.refreshRateRatio.value;
            if(refresh==pacingRefresh)return;
            pacingRefresh=refresh;int interval=1;
            if(FramePacing60&&!ApprovedForestAssets.CheckpointReplay&&double.IsFinite(refresh)&&refresh>0){
                double error=Math.Abs(refresh-60);
                for(int divisor=2;divisor<=4;divisor++){double candidate=Math.Abs(refresh/divisor-60);if(candidate<error){error=candidate;interval=divisor;}}
            }
            QualitySettings.vSyncCount=FramePacing60?interval:0;Application.targetFrameRate=-1;
        }
        public bool TreesEnabled {get;private set;}
        const string TreesPreference="CityForge.Meadow.Trees";
        public void SetTreesEnabled(bool enabled,bool remember=false)
        {
            bool changed=TreesEnabled!=enabled;TreesEnabled=enabled;
            if(DisplayTerrain)DisplayTerrain.drawTreesAndFoliage=enabled;
            if(remember){PlayerPrefs.SetInt(TreesPreference,enabled?1:0);PlayerPrefs.Save();}
            if(changed)RefreshLakeEnvironments();
            var caption=View.transform.Find("RegionHud/Trees/Caption");if(caption)caption.GetComponent<UnityEngine.UI.Text>().text="TREES: "+(enabled?"ON":"OFF");
        }
        public TreeInstance[] CanonicalTreeInstances=>fullTreeInstances??terrainData.treeInstances;
        public int FullTreeCount=>fullTreeInstances?.Length??0;
        const string SmallPlantsPreference="CityForge.Meadow.SmallPlants";
        TreeInstance[] fullTreeInstances,matureTreeInstances;
        MeadowRuntimeVegetation groundCover;
        Transform shorelinePlants;
        // Juvenile poplars, maple shrubs and small willows in the extracted baseline.
        GameObject[] mountainEvergreens;
        public int EvergreenTreeCount {get;private set;}
        public int BroadleafTreeCount {get;private set;}
        RegionSeason treeSeason;
        bool IsSmallPlant(int prototype)=>prototype<3||(prototype>=11&&prototype<Baseline.treePrefabs.Length);
        TreePrototype[] TreePrototypes(GameObject[] prefabs){
            if(treeSeason==RegionSeason.Summer&&!ApprovedForestAssets.CheckpointReplay){var approved=Resources.Load<ApprovedForestAssets>("ApprovedForest/Forest");if(approved&&approved.nativeSummer!=null&&approved.nativeSummer.Length==prefabs.Length)prefabs=approved.nativeSummer;}
            return ForestTreeMix.Prototypes(prefabs,mountainEvergreens,treeSeason);
        }
        void SetNativePresentation(TreeInstance[] source){
            if(PanningTestDistricts.TryGetPercent(View.SavedRegionId,out int percent)){
                // Keep exactly the same mature trees when understory is toggled for a controlled comparison.
                var selected=new List<TreeInstance>(PanningTestDistricts.Subset(matureTreeInstances,percent));
                if(source==fullTreeInstances)foreach(var tree in fullTreeInstances)if(IsSmallPlant(tree.prototypeIndex))selected.Add(tree);
                source=selected.ToArray();
            }
            terrainData.SetTreeInstances(ForestTreeMix.Presentation(source,Baseline),false);
        }
        public void SetSmallPlantsEnabled(bool enabled,bool remember=false)
        {
            if(remember){PlayerPrefs.SetInt(SmallPlantsPreference,enabled?1:0);PlayerPrefs.Save();}
            if(fullTreeInstances==null)return;
            bool changed=enabled!=SmallPlantsEnabled;
            if(enabled!=SmallPlantsEnabled){SetNativePresentation(enabled?fullTreeInstances:matureTreeInstances);SmallPlantsEnabled=enabled;}
            if(enabled){
                if(!shorelinePlants){BuildShoreline();shorelinePlants=transform.Find("Seeded shoreline and understory");}
                if(!groundCover){var go=new GameObject("Streamed small plants");go.transform.SetParent(transform,false);groundCover=go.AddComponent<MeadowRuntimeVegetation>();groundCover.Initialize(this,View.GetComponentInChildren<Camera>());}
            }
            if(shorelinePlants)shorelinePlants.gameObject.SetActive(enabled);
            if(groundCover)groundCover.gameObject.SetActive(enabled);
            if(changed)RefreshLakeEnvironments();
            var caption=View.transform.Find("RegionHud/SmallPlants/Caption");if(caption)caption.GetComponent<UnityEngine.UI.Text>().text="SMALL PLANTS: "+(enabled?"ON":"OFF");
        }
        public void RefreshForestSpecies(bool refreshEnvironment=true)
        {
            if(!terrainData||fullTreeInstances==null)return;
            var library=Resources.Load<SeasonalTreeLibrary>("Seasons/SeasonalTrees");
            terrainData.treePrototypes=TreePrototypes(library.Prefabs(treeSeason));terrainData.RefreshPrototypes();
            SetNativePresentation(SmallPlantsEnabled?fullTreeInstances:matureTreeInstances);DisplayTerrain.Flush();
            if(refreshEnvironment)RefreshLakeEnvironments();
        }
        public void ApplySeasonShadows(RegionSeason season)
        {
            foreach(var light in GetComponentsInChildren<Light>())
            {
                if(light.type!=LightType.Directional)continue;
                light.shadowStrength=season==RegionSeason.Winter?.52f:.62f;
                // Bare branches retain their silhouette without PCF expansion; leafy seasons stay soft.
                light.shadows=season==RegionSeason.Winter?LightShadows.Hard:LightShadows.Soft;
                light.GetUniversalAdditionalLightData().softShadowQuality=SoftShadowQuality.Low;
            }
        }
        public void SetSeason(RegionSeason season)
        {
            ApplySeasonShadows(season);
            var library=Resources.Load<SeasonalTreeLibrary>("Seasons/SeasonalTrees");
            if(!library)return;
            treeSeason=season;
            var prefabs=library.Prefabs(season);
            if(prefabs==null||prefabs.Length!=Baseline.treePrefabs.Length)throw new Exception("Season prototype count mismatch");
            var prototypes=TreePrototypes(prefabs);
            terrainData.treePrototypes=prototypes;terrainData.RefreshPrototypes();DisplayTerrain.Flush();
            RefreshLakeEnvironments();
        }
        void RefreshLakeEnvironments(){foreach(var lake in GetComponentsInChildren<LakeWaterRenderer>())if(lake.CaptureReady)lake.RefreshEnvironment();}
        public MeadowRegionSnapshot CaptureSnapshot()=>new MeadowRegionSnapshot{version=Baseline.generationVersion,road=Road.ToArray(),trees=fullTreeInstances??terrainData.treeInstances,alphamaps=lakeBaseAlphamaps??terrainData.GetAlphamaps(0,0,terrainData.alphamapResolution,terrainData.alphamapResolution)};

        AmbientMode savedAmbientMode; Color savedSky,savedEquator,savedGround,savedAmbient;
        Material savedSkybox;float savedIntensity,savedReflection;
        SphericalHarmonicsL2 savedProbe;
        bool settingsApplied;
        Camera savedCamera;string savedPipelineSettings;
        T Own<T>(T asset) where T:UnityEngine.Object {owned.Add(asset);return asset;}
        public void Build(PrototypeRegionView view,MeadowRuntimeBaseline baseline,bool regionOverview=false)
        {RegionBuildSteps.Complete(BuildStages(view,baseline,regionOverview));}
        public IEnumerator BuildStages(PrototypeRegionView view,MeadowRuntimeBaseline baseline,bool regionOverview=false,Action<string,float> progress=null)
        {
            progress?.Invoke("Loading landscape assets",0);yield return null;
            IsRegionOverview=regionOverview;
            FramePacing60=PlayerPrefs.GetInt(FramePacingPreference,1)==1;
            // A dry region keeps the approved land assets but holds no water/FFT/flow asset references.
            if(view.Waterless&&!view.MinimalRiverCrest){baseline=Own(Instantiate(baseline));baseline.name="Approved meadow land only";baseline.waterTemplate=null;baseline.fftTemplate=null;baseline.flowMaterial=null;}
            if(view.RiverBedVersion>0){
                baseline=Own(Instantiate(baseline));baseline.name="Approved meadow river district";
                var layers=new TerrainLayer[view.RiverBedVersion>=3?8:7];System.Array.Copy(baseline.layers,layers,4);
                layers[4]=Resources.Load<TerrainLayer>(view.RiverBedVersion>=3?"RiverBeds/RichPebbles":"RiverBeds/Gravel");layers[5]=Resources.Load<TerrainLayer>(view.RiverBedVersion>=3?"RiverBeds/RichSand":"RiverBeds/Sand");layers[6]=Resources.Load<TerrainLayer>(view.RiverBedVersion>=3?"RiverBeds/RichSoil":"RiverBeds/Silt");
                if(layers.Length==8)layers[7]=Resources.Load<TerrainLayer>("RiverBeds/Moss");
                foreach(var layer in layers)if(!layer)throw new System.Exception("Missing dry riverbed terrain layer.");baseline.layers=layers;
            }
            if(view.MountainTest){baseline=Own(Instantiate(baseline));baseline.name="Hills and mountain land";
                if(MountainSurfaceTreatment.Enabled){var template=Resources.Load<Material>("Mountains/MountainTerrain");if(!template)throw new Exception("Missing mountain terrain material");var material=Own(Instantiate(template));material.name="Mountain surface with varied world projection";baseline.terrainMaterial=material;}
                baseline.generationVersion=105;var layers=new TerrainLayer[8];Array.Copy(baseline.layers,layers,5);layers[5]=Resources.Load<TerrainLayer>(MountainSurfaceTreatment.Enabled?"Mountains/VariedCliffDark":"Mountains/CliffDark");layers[6]=Resources.Load<TerrainLayer>(MountainSurfaceTreatment.Enabled?"Mountains/PhotoscannedCliff":"Mountains/CliffBright");layers[7]=Resources.Load<TerrainLayer>("Mountains/Scree");baseline.layers=layers;}
            if(view.Coastline){baseline=Own(Instantiate(baseline));baseline.terrainMaterial=Resources.Load<Material>("Coasts/CoastalTerrain");baseline.generationVersion=201;var coastalLayers=new TerrainLayer[7];Array.Copy(baseline.layers,coastalLayers,5);coastalLayers[5]=Resources.Load<TerrainLayer>("RiverBeds/RichSand");coastalLayers[6]=Resources.Load<TerrainLayer>("Mountains/CliffDark");baseline.layers=coastalLayers;}
            mountainEvergreens=MountainEvergreenMix.LoadPrefabs();
            treeSeason=view.Season;
            navigationCamera=view.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>();
            View=view;DistrictIndex=regionOverview?-1:view.SelectedDistrict;Baseline=baseline;Side=regionOverview?view.MapSide:view.DistrictSide;Origin=regionOverview?Vector2.one*(-view.MapSide/2):view.DistrictOrigin(view.SelectedDistrict);
            var rng=new System.Random(Seed);SeedOffset=new Vector2(rng.Next(100,10000),rng.Next(100,10000));
            terrainData=Own(new TerrainData {name="Seeded approved meadow district",heightmapResolution=baseline.referenceHeightResolution,alphamapResolution=regionOverview?512:view.MountainTest?1024:Mathf.ClosestPowerOfTwo(Mathf.RoundToInt(Side*2)),baseMapResolution=1024,size=new Vector3(Side,view.GetComponentInChildren<Terrain>().terrainData.size.y,Side)});
            int resolution=baseline.referenceHeightResolution;var heights=new float[resolution,resolution];
            progress?.Invoke("Shaping district terrain",.08f);yield return null;
            var batchTimer=System.Diagnostics.Stopwatch.StartNew();
            for(int z=0;z<resolution;z++){for(int x=0;x<resolution;x++){
                float gx=regionOverview?x*512f/(resolution-1):(view.SelectedDistrict%view.GridSide)*512f/view.GridSide+x*512f/view.GridSide/(resolution-1),gz=regionOverview?z*512f/(resolution-1):(view.SelectedDistrict/view.GridSide)*512f/view.GridSide+z*512f/view.GridSide/(resolution-1);int ix=Mathf.Min(511,(int)gx),iz=Mathf.Min(511,(int)gz);float fx=gx-ix,fz=gz-iz;
                heights[z,x]=(view.RiverBedVersion>=4||view.CreationSettings?.version>=5)?view.Data.SmoothHeight(gx/512,gz/512):Mathf.Lerp(Mathf.Lerp(view.Data.Heights[iz,ix],view.Data.Heights[iz,ix+1],fx),Mathf.Lerp(view.Data.Heights[iz+1,ix],view.Data.Heights[iz+1,ix+1],fx),fz);
            }
            if(batchTimer.ElapsedMilliseconds>=12){progress?.Invoke("Shaping district terrain",.08f+.12f*z/resolution);yield return null;batchTimer.Restart();}}
            progress?.Invoke("Applying terrain heights",.21f);yield return null;
            terrainData.SetHeights(0,0,heights);terrainData.terrainLayers=baseline.layers;
            var go=Terrain.CreateTerrainGameObject(terrainData);go.name="Runtime Native Meadow Terrain";go.transform.SetParent(transform,false);go.transform.position=new Vector3(Origin.x,0,Origin.y);go.layer=30;
            DisplayTerrain=go.GetComponent<Terrain>();DisplayTerrain.materialTemplate=baseline.terrainMaterial;DisplayTerrain.drawInstanced=true;DisplayTerrain.basemapDistance=view.MountainTest?10000:2000;DisplayTerrain.heightmapPixelError=view.CreationSettings?.version>=5?1:10;
            DisplayTerrain.treeDistance=regionOverview?Mathf.Max(baseline.treeDistance,view.MapSide*3):baseline.treeDistance;DisplayTerrain.treeBillboardDistance=baseline.treeBillboardDistance;DisplayTerrain.treeCrossFadeLength=baseline.treeCrossFadeLength;DisplayTerrain.treeMaximumFullLODCount=baseline.treeMaximumFullLODCount;
            // Avoid competing ray hits; the continuous original collider remains the authoritative gameplay surface.
            go.GetComponent<TerrainCollider>().enabled=view.RiverBedVersion>=4||view.CreationSettings?.version>=5;
            var timings=new System.Text.StringBuilder();
            void Measure(string name,Action action){var timer=System.Diagnostics.Stopwatch.StartNew();action();timings.Append(name+"="+timer.ElapsedMilliseconds+" ms; ");}
            progress?.Invoke("Preparing shorelines",.25f);yield return null;
            Measure("shore field",BuildShoreField);var cached=View.LoadedMeadowDistrict==View.SelectedDistrict?View.LoadedMeadow:null;
            if(cached!=null&&cached.alphamaps.GetLength(2)==Baseline.layers.Length+3){
                // Migrate experimental lake caches while retaining their saved road and trees.
                int count=cached.alphamaps.GetLength(0),layers=Baseline.layers.Length;var canonical=new float[count,count,layers];
                for(int z=0;z<count;z++)for(int x=0;x<count;x++){float sum=0;for(int l=0;l<layers;l++)sum+=cached.alphamaps[z,x,l];
                    if(sum>.00001f)for(int l=0;l<layers;l++)canonical[z,x,l]=cached.alphamaps[z,x,l]/sum;else canonical[z,x,Mathf.Min(1,layers-1)]=1;}
                cached.alphamaps=canonical;
            }
            if(cached!=null&&cached.version==Baseline.generationVersion&&cached.alphamaps.GetLength(0)==terrainData.alphamapResolution&&cached.alphamaps.GetLength(2)==Baseline.layers.Length){
                progress?.Invoke("Restoring saved ground and trees",.34f);yield return null;
                Measure("saved meadow restore",()=>RestoreSnapshot(cached));UsedSavedTerrain=true;
            }else{
                progress?.Invoke("Laying out roads",.34f);yield return null;Measure("road",BuildRoad);
                progress?.Invoke("Planting woodland",.4f);yield return null;Measure("trees",BuildTrees);
                var paintTimer=System.Diagnostics.Stopwatch.StartNew();
                yield return BuildControlMapStages(value=>progress?.Invoke("Painting meadow and mountain surfaces",.44f+value*.25f));
                timings.Append("control map=").Append(paintTimer.ElapsedMilliseconds).Append(" ms; ");
            }
            if(GeneratedRiverDistrict)yield return PaintGeneratedRiverBanks();
            progress?.Invoke("Building roads and rocks",.7f);yield return null;
            Measure("road mesh",BuildRoadMesh);if(View.MountainTest)Measure("mountain outcrops",()=>gameObject.AddComponent<MountainRockRenderer>().Initialize(this));if(View.RiverBedVersion>=2)Measure("river rocks",()=>gameObject.AddComponent<RiverBedRockRenderer>().Initialize(this));
            progress?.Invoke("Applying landscape lighting",.78f);yield return null;Measure("presentation",BuildPresentation);
            progress?.Invoke("Preparing water",.84f);yield return null;
            if(View.MinimalRiverCrest)Measure("minimal river Crest",BuildMinimalRiverCrest);else if(!View.Waterless)Measure("water setup",BuildWater);
            if(!ApprovedForestAssets.CheckpointReplay&&!Water&&!View.Waterless&&!View.MinimalRiverCrest){
                var approvedWater=ApprovedRiverWater.Initialize(this);while(approvedWater.MoveNext())yield return approvedWater.Current;
            }
            GenerationTimings=timings.ToString();Debug.Log("Meadow generation stages: "+GenerationTimings);
            progress?.Invoke("Preparing seasonal vegetation",.94f);yield return null;
            if(View.MountainTest)EvergreenTreeCount=MountainEvergreenMix.Apply(terrainData,Baseline.treePrefabs.Length,View.Data.Seed);
            BroadleafTreeCount=ForestTreeMix.Apply(terrainData,View.Data.Seed);
            fullTreeInstances=terrainData.treeInstances;
            var mature=new List<TreeInstance>();foreach(var tree in fullTreeInstances)if(!IsSmallPlant(tree.prototypeIndex))mature.Add(tree);matureTreeInstances=mature.ToArray();
            // Initial full data remains canonical for painting, snapshots and comparison.
            SmallPlantsEnabled=true;SetSmallPlantsEnabled(View.SavedRegionId=="pan-test-coast-50"||(!PanningTestDistricts.TryGetPercent(View.SavedRegionId,out _)&&PlayerPrefs.GetInt(SmallPlantsPreference,0)==1));
            RefreshForestSpecies(false);
            SetTreesEnabled(true);
            progress?.Invoke("Preparing approved forest canopy",.97f);yield return null;
            if(!ApprovedForestAssets.CheckpointReplay){var forest=gameObject.AddComponent<ApprovedDistrictForest>();forest.Initialize(this);if(PanningTestDistricts.TryGetPercent(View.SavedRegionId,out int percent))forest.SetDiagnosticDensity(percent);}
            RefreshWildlife();
        }
        DistrictWildlife wildlife;
        public DistrictWildlife Wildlife=>wildlife;
        public void RefreshWildlife(){
            if(wildlife){wildlife.gameObject.SetActive(false);Destroy(wildlife.gameObject);wildlife=null;}
            if(!View.AnimalsEnabled||IsRegionOverview)return;
            var holder=new GameObject("District temperate wildlife");holder.transform.SetParent(transform,false);
            wildlife=holder.AddComponent<DistrictWildlife>();wildlife.Initialize(this);
        }
        float[,,] lakeBaseAlphamaps;
        int lakeBedOffset=-1;
        public int EnsureLakeBedLayers()
        {
            if(lakeBedOffset>=0)return lakeBedOffset;
            lakeBedOffset=terrainData.terrainLayers.Length;
            // Save the unpainted canonical ground. Lake beds are rebuilt deterministically after cache restore.
            lakeBaseAlphamaps=terrainData.GetAlphamaps(0,0,terrainData.alphamapResolution,terrainData.alphamapResolution);
            var layers=new TerrainLayer[lakeBedOffset+3];Array.Copy(terrainData.terrainLayers,layers,lakeBedOffset);
            string[] names={"RichPebbles","RichSand","RichSoil"};
            var palette=new[]{new Vector4(.24f,.26f,.22f,1),new Vector4(.32f,.28f,.19f,1),new Vector4(.22f,.19f,.12f,1)};
            for(int i=0;i<3;i++){var layer=Own(Instantiate(Resources.Load<TerrainLayer>("RiverBeds/"+names[i])));
                layer.name="Lake wet "+names[i];layer.diffuseRemapMax=palette[i];layer.specular=new Color(.035f,.035f,.035f);layer.normalScale=.45f;layers[lakeBedOffset+i]=layer;}
            terrainData.terrainLayers=layers;return lakeBedOffset;
        }
        readonly Dictionary<string,float[,]> bodyShores=new();
        public float LakeShoreDistance(RegionWaterBody body,float level,Vector2 p)
        {
            // Retain the approved single-body shoreline sampling exactly.
            if(View.Data.WaterBodies.Length==1)return ShoreDistance(p);
            if(!bodyShores.TryGetValue(body.id,out var field)){
                const int n=257;field=new float[n,n];var wet=new bool[n,n];float pixel=Side/(n-1);
                for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                    var point=Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*Side;
                    wet[z,x]=body.Contains(point/View.MapSide+Vector2.one*.5f)&&Height(point)<level;field[z,x]=Side;
                }
                for(int z=1;z<n-1;z++)for(int x=1;x<n-1;x++)if(wet[z,x]!=wet[z,x-1]||wet[z,x]!=wet[z,x+1]||wet[z,x]!=wet[z-1,x]||wet[z,x]!=wet[z+1,x])field[z,x]=0;
                for(int z=1;z<n;z++)for(int x=1;x<n;x++)field[z,x]=Mathf.Min(field[z,x],Mathf.Min(field[z,x-1]+pixel,Mathf.Min(field[z-1,x]+pixel,field[z-1,x-1]+pixel*1.41421356f)));
                for(int z=n-2;z>=0;z--)for(int x=n-2;x>=0;x--)field[z,x]=Mathf.Min(field[z,x],Mathf.Min(field[z,x+1]+pixel,Mathf.Min(field[z+1,x]+pixel,field[z+1,x+1]+pixel*1.41421356f)));
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)if(wet[z,x])field[z,x]*=-1;
                bodyShores[body.id]=field;
            }
            float gx=Mathf.Clamp((p.x-Origin.x)/Side*256,0,255.999f),gz=Mathf.Clamp((p.y-Origin.y)/Side*256,0,255.999f);int ix=(int)gx,iz=(int)gz;
            return Mathf.Lerp(Mathf.Lerp(field[iz,ix],field[iz,ix+1],gx-ix),Mathf.Lerp(field[iz+1,ix],field[iz+1,ix+1],gx-ix),gz-iz);
        }
        void RestoreSnapshot(MeadowRegionSnapshot cached)
        {
            Road.AddRange(cached.road);IndexRoad();
            var prototypes=TreePrototypes(Baseline.treePrefabs);terrainData.treePrototypes=prototypes;
            // Keep cached placements/species, but seat them on the displayed smooth surface.
            terrainData.SetTreeInstances(cached.trees,View.CreationSettings?.version>=5);
            foreach(var tree in cached.trees)if(tree.prototypeIndex>=3){
                var p=Origin+new Vector2(tree.position.x,tree.position.z)*Side;var key=Vector2Int.FloorToInt(p/16);
                if(!canopy.TryGetValue(key,out var list))canopy[key]=list=new List<Vector2>();list.Add(p);
            }
            terrainData.SetAlphamaps(0,0,cached.alphamaps);
            View.ReleaseLoadedMeadow();
        }
        public bool ExposedRiverBed(Vector2 p)
        {
            if(View.RiverBedVersion==0)return false;
            DryRiverBedMigration.Channel(View.Data,View.MapSide,p.x/View.MapSide+.5f,p.y/View.MapSide+.5f,out float distance,out float width,out _,View.RiverBedVersion);
            return distance<width*1.12f;
        }
        public float Height(Vector2 p) => DisplayTerrain.SampleHeight(new Vector3(p.x,0,p.y));
        public float WaterDistance(Vector2 p)
        {
            float gx=Mathf.Clamp((p.x/View.MapSide+.5f)*512,0,511.999f),gz=Mathf.Clamp((p.y/View.MapSide+.5f)*512,0,511.999f);int x=(int)gx,z=(int)gz;
            return Mathf.Lerp(Mathf.Lerp(View.Data.WaterDistance[z,x],View.Data.WaterDistance[z,x+1],gx-x),Mathf.Lerp(View.Data.WaterDistance[z+1,x],View.Data.WaterDistance[z+1,x+1],gx-x),gz-z)*View.MapScale;
        }
        float ShoreWaterLevel
        {
            get {foreach(var body in View.Data.WaterBodies)if((body.kind==WaterBodyKind.Lake||body.kind==WaterBodyKind.Pond)&&body.hasSurfaceLevel)return body.surfaceLevelNormalized*terrainData.size.y;return View.SeaLevel;}
        }
        void BuildShoreField()
        {
            const int n=257;shore=new float[n,n];var wet=new bool[n,n];float pixel=Side/(n-1);
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){wet[z,x]=terrainData.GetInterpolatedHeight(x/(float)(n-1),z/(float)(n-1))<ShoreWaterLevel;shore[z,x]=Side;}
            for(int z=1;z<n-1;z++)for(int x=1;x<n-1;x++)if(wet[z,x]!=wet[z,x-1]||wet[z,x]!=wet[z,x+1]||wet[z,x]!=wet[z-1,x]||wet[z,x]!=wet[z+1,x])shore[z,x]=0;
            for(int z=1;z<n;z++)for(int x=1;x<n;x++)shore[z,x]=Mathf.Min(shore[z,x],Mathf.Min(shore[z,x-1]+pixel,Mathf.Min(shore[z-1,x]+pixel,shore[z-1,x-1]+pixel*1.41421356f)));
            for(int z=n-2;z>=0;z--)for(int x=n-2;x>=0;x--)shore[z,x]=Mathf.Min(shore[z,x],Mathf.Min(shore[z,x+1]+pixel,Mathf.Min(shore[z+1,x]+pixel,shore[z+1,x+1]+pixel*1.41421356f)));
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)if(wet[z,x])shore[z,x]*=-1;
        }
        public float ShoreDistance(Vector2 p)
        {
            float gx=Mathf.Clamp((p.x-Origin.x)/Side*256,0,255.999f),gz=Mathf.Clamp((p.y-Origin.y)/Side*256,0,255.999f);int x=(int)gx,z=(int)gz;
            return Mathf.Lerp(Mathf.Lerp(shore[z,x],shore[z,x+1],gx-x),Mathf.Lerp(shore[z+1,x],shore[z+1,x+1],gx-x),gz-z);
        }
        public float Noise(Vector2 p,float scale,int salt=0) => Mathf.PerlinNoise(p.x/scale+SeedOffset.x+salt,p.y/scale+SeedOffset.y-salt);
        public float Woodland(Vector2 p)
        {
            if(View.Data.Biomes!=null)return View.Data.Biomes.At(p/View.MapSide+Vector2.one*.5f).woodland;
            // Seeded continuous field, no demo ribbons or district-local restarts.
            var warp=new Vector2(Noise(p,93,19)-.5f,Noise(p,87,31)-.5f)*35;
            return Mathf.SmoothStep(0,1,Mathf.InverseLerp(.47f,.64f,Noise(p+warp,110,5)));
        }
        // All placement/painting callers only need distances within this radius. The
        // segment AABBs are expanded by that radius, so skipped segments cannot affect
        // any road transition or exclusion. Return a saturated distance outside it.
        public float RoadDistance(Vector2 p)
        {
            if(!roadCells.TryGetValue(Vector2Int.FloorToInt(p/RoadCellSide),out var segments))return roadQueryLimit;
            float best=roadQueryLimit*roadQueryLimit;
            foreach(int i in segments){var a=Road[i-1];var d=Road[i]-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);best=Mathf.Min(best,(p-(a+d*t)).sqrMagnitude);}
            return Mathf.Sqrt(best);
        }
        public float ExactRoadDistance(Vector2 p)
        {
            float best=float.PositiveInfinity;
            for(int i=1;i<Road.Count;i++){var a=Road[i-1];var d=Road[i]-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);best=Mathf.Min(best,Vector2.Distance(p,a+d*t));}
            return best;
        }
        public float RoadQueryLimit=>roadQueryLimit;
        void IndexRoad()
        {
            roadQueryLimit=View.MountainTest?24:Mathf.Max(6,Baseline.roadWidth+2)+1;
            roadCells.Clear();
            for(int i=1;i<Road.Count;i++){
                var padding=Vector2.one*roadQueryLimit;
                var low=Vector2Int.FloorToInt((Vector2.Min(Road[i-1],Road[i])-padding)/RoadCellSide);
                var high=Vector2Int.FloorToInt((Vector2.Max(Road[i-1],Road[i])+padding)/RoadCellSide);
                for(int z=low.y;z<=high.y;z++)for(int x=low.x;x<=high.x;x++){
                    var cell=new Vector2Int(x,z);if(!roadCells.TryGetValue(cell,out var list))roadCells[cell]=list=new List<int>();list.Add(i);
                }
            }
        }
        public float ExactCanopy(Vector2 p)
        {
            var cell=Vector2Int.FloorToInt(p/16);float value=0;
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)if(canopy.TryGetValue(cell+new Vector2Int(dx,dz),out var list))foreach(var tree in list){float d=Vector2.Distance(p,tree)/Baseline.canopyRadius;if(d<1)value+=Mathf.SmoothStep(0,1,1-d)*.6f;}
            return Mathf.Clamp01(value);
        }
        public float Canopy(Vector2 p)
        {
            float radius=Baseline.canopyRadius,radiusSquared=radius*radius,value=0;
            var original=Vector2Int.FloorToInt(p/16);
            var low=Vector2Int.Max(original-Vector2Int.one,Vector2Int.FloorToInt((p-Vector2.one*radius)/16));
            var high=Vector2Int.Min(original+Vector2Int.one,Vector2Int.FloorToInt((p+Vector2.one*radius)/16));
            for(int z=low.y;z<=high.y;z++)for(int x=low.x;x<=high.x;x++)if(canopy.TryGetValue(new Vector2Int(x,z),out var list))foreach(var tree in list){
                if((p-tree).sqrMagnitude>=radiusSquared)continue;
                float d=Vector2.Distance(p,tree)/radius;if(d<1)value+=Mathf.SmoothStep(0,1,1-d)*.6f;
            }
            return Mathf.Clamp01(value);
        }
        void BuildRoad()
        {
            if(View.Coastline){for(int i=0;i<=64;i++){float z=Origin.y+Side*(.08f+.8f*i/64);Road.Add(new Vector2(Mathf.Min(Origin.x+Side-40,CoastlineTestRegion.ShoreX(z)+125),z));}IndexRoad();return;}
            if(View.MountainTest){Road.AddRange(MountainRoad.Build(this));IndexRoad();return;}
            // Pilot road data is generated once from this seed; the exact same centerline drives surface and exclusion.
            for(int i=0;i<=64;i++){float t=i/64f;var p=Origin+new Vector2(Side*(.15f+.035f*Mathf.Sin(t*8+SeedOffset.x)),Side*(.04f+.90f*t));
                // Shift inland when a path would enter the preserved river footprint.
                for(int j=0;j<12&&WaterDistance(p)<8;j++)p.x+=4;
                p.x=Mathf.Min(Origin.x+Side-8,p.x);Road.Add(p);}
            IndexRoad();
        }
        public float MountainSlope(Vector2 p){float dx=(Height(p+Vector2.right*2)-Height(p-Vector2.right*2))/4,dz=(Height(p+Vector2.up*2)-Height(p-Vector2.up*2))/4;return Mathf.Atan(Mathf.Sqrt(dx*dx+dz*dz))*Mathf.Rad2Deg;}
        public float MountainVegetation(Vector2 p)=>!View.MountainTest?1:(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(115,205,Height(p))))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(22,38,MountainSlope(p))));
        public void MountainSurface(Vector2 p,out float rock,out float scree){
            float h=Height(p),slope=MountainSlope(p),high=Mathf.SmoothStep(0,1,Mathf.InverseLerp(100,210,h));
            float breakup=Mathf.Lerp(.83f,1.08f,Noise(p,21,23));
            rock=Mathf.Clamp01((Mathf.SmoothStep(0,1,Mathf.InverseLerp(29,50,slope))*.92f+high*Mathf.SmoothStep(0,1,Mathf.InverseLerp(18,36,slope))*.54f)*breakup*Mathf.SmoothStep(0,1,Mathf.InverseLerp(90,140,h)));
            // Loose stone accumulates on gentler ground directly below exposed faces.
            float dx=(Height(p+Vector2.right*4)-Height(p-Vector2.right*4))/8,dz=(Height(p+Vector2.up*4)-Height(p-Vector2.up*4))/8;
            var uphill=new Vector2(dx,dz).normalized;float above=MountainSlope(p+uphill*18);
            float deposit=Mathf.SmoothStep(0,1,Mathf.InverseLerp(25,44,above))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(65,140,h));
            scree=deposit*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(26,41,slope)))*Mathf.Lerp(.25f,.65f,Noise(p,15,31))*(1-rock);
        }
        void BuildTrees()
        {
            var prototypes=TreePrototypes(Baseline.treePrefabs);terrainData.treePrototypes=prototypes;var trees=new List<TreeInstance>();float step=Baseline.treeSpacing;
            float mean=0;const int samples=48;for(int z=0;z<samples;z++)for(int x=0;x<samples;x++)mean+=Woodland((new Vector2((x+.5f)/samples,(z+.5f)/samples)-Vector2.one*.5f)*View.MapSide);
            mean/=samples*samples;if(View.Data.Biomes!=null)mean=.45f;float densityNormalization=Baseline.referenceTreeDensity*step*step/Mathf.Max(.01f,mean);
            int minX=Mathf.FloorToInt(Origin.x/step),minZ=Mathf.FloorToInt(Origin.y/step),maxX=Mathf.CeilToInt((Origin.x+Side)/step),maxZ=Mathf.CeilToInt((Origin.y+Side)/step);
            for(int z=minZ;z<maxZ;z++)for(int x=minX;x<maxX;x++){
                var random=RandomFor(x,z,37);float R()=>(float)random.NextDouble();var p=new Vector2((x+R())*step,(z+R())*step);
                if(!Contains(p)||WaterDistance(p)<3||RoadDistance(p)<Baseline.roadWidth+2||R()>Mathf.Min(1,Woodland(p)*densityNormalization)||Height(p)<View.SeaLevel+.2f)continue;
                if(View.MountainTest&&R()>MountainVegetation(p))continue;
                if(View.Coastline&&(ShoreDistance(p)<75||MountainSlope(p)>24))continue;
                float pick=R();int prototype=0;for(int j=0;j<Baseline.treeWeights.Length;j++){pick-=Baseline.treeWeights[j];if(pick<=0){prototype=j;break;}}
                float width=Baseline.treeWidths[prototype]*Mathf.Lerp(.85f,1.15f,R()),height=Baseline.treeHeights[prototype]*Mathf.Lerp(.85f,1.15f,R());
                trees.Add(new TreeInstance {position=new Vector3((p.x-Origin.x)/Side,Height(p)/terrainData.size.y,(p.y-Origin.y)/Side),prototypeIndex=prototype,widthScale=width,heightScale=height,rotation=R()*Mathf.PI*2,color=Color.white,lightmapColor=Color.white});
                // In the test all prototypes >=3 influence litter, including woodland shrubs.
                if(prototype>=3){var key=Vector2Int.FloorToInt(p/16);if(!canopy.TryGetValue(key,out var list))canopy[key]=list=new List<Vector2>();list.Add(p);}
            }
            terrainData.SetTreeInstances(trees.ToArray(),true);
        }
        void BuildControlMap(){RegionBuildSteps.Complete(BuildControlMapStages());}
        IEnumerator BuildControlMapStages(Action<float> progress=null)
        {
            int n=terrainData.alphamapResolution;var maps=new float[n,n,Baseline.layers.Length];
            var batch=System.Diagnostics.Stopwatch.StartNew();
            for(int z=0;z<n;z++){for(int x=0;x<n;x++){
                var p=Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*Side;
                float forest=Woodland(p),can=Canopy(p),patch=Noise(p,13,8),road=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(Baseline.roadWidth*.35f,Baseline.roadWidth,RoadDistance(p)));
                float bank=(1-Mathf.SmoothStep(0,1,Mathf.Abs(ShoreDistance(p))/Baseline.shorelineWidth))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.58f,.78f,patch));
                float dry=View.Data.Biomes!=null?View.Data.Biomes.At(p/View.MapSide+Vector2.one*.5f).dryness:0;
                float soil=road*.12f+bank*.23f+dry*.08f;float grass=Mathf.Max(0,1-soil);float litter=can*(.5f+patch*.25f);
                maps[z,x,0]=soil;maps[z,x,1]=grass*(1-forest*.4f)*(1-litter);maps[z,x,2]=grass*forest*.4f*(1-litter);maps[z,x,3]=grass*litter;
            }
            if(batch.ElapsedMilliseconds>=12){progress?.Invoke(.45f*z/n);yield return null;batch.Restart();}}
            if(View.MountainTest)for(int z=0;z<n;z++){for(int x=0;x<n;x++){
                var p=Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*Side;
                MountainSurface(p,out float rock,out float scree);
                float light=Mathf.Lerp(.2f,.75f,Noise(p,55,17));
                for(int l=0;l<5;l++)maps[z,x,l]*=1-rock-scree;maps[z,x,5]=rock*(1-light);maps[z,x,6]=rock*light;maps[z,x,7]=scree;
            }
            if(batch.ElapsedMilliseconds>=12){progress?.Invoke(.45f+.5f*z/n);yield return null;batch.Restart();}}
            if(View.Coastline)for(int z=0;z<n;z++){for(int x=0;x<n;x++){
                var p=Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*Side;
                float rock=Mathf.SmoothStep(0,1,Mathf.InverseLerp(20,42,MountainSlope(p)));
                float sand=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,7,Height(p)-View.SeaLevel));sand*=1-rock;
                for(int l=0;l<5;l++)maps[z,x,l]*=1-sand-rock;maps[z,x,5]=sand;maps[z,x,6]=rock;
            }
            if(batch.ElapsedMilliseconds>=12){progress?.Invoke(.45f+.5f*z/n);yield return null;batch.Restart();}}
            progress?.Invoke(.98f);yield return null;terrainData.SetAlphamaps(0,0,maps);
        }
        void BuildRoadMesh()
        {
            var path=Road;
            bool smoothDistrict=View.CreationSettings?.version>=5;
            if(smoothDistrict&&Road.Count>1){
                path=new List<Vector2>{Road[0]};
                for(int i=1;i<Road.Count;i++){
                    int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(Road[i-1],Road[i])));
                    for(int j=1;j<=steps;j++)path.Add(Vector2.Lerp(Road[i-1],Road[i],j/(float)steps));
                }
            }
            int columns=smoothDistrict?3:2;
            var verts=new Vector3[path.Count*columns];var uv=new Vector2[verts.Length];var tri=new int[Mathf.Max(0,path.Count-1)*(columns-1)*6];float length=0;
            for(int i=0;i<path.Count;i++){
                if(i>0)length+=Vector2.Distance(path[i],path[i-1]);
                var dir=(path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(0,i-1)]).normalized;var normal=new Vector2(-dir.y,dir.x);
                for(int j=0;j<columns;j++){
                    float across=j/(float)(columns-1);var p=path[i]+normal*Mathf.Lerp(-Baseline.roadWidth,Baseline.roadWidth,across);
                    verts[i*columns+j]=new Vector3(p.x,Height(p)+(smoothDistrict?.08f:.012f),p.y);uv[i*columns+j]=new Vector2(length/4,across);
                }
                if(i<path.Count-1)for(int j=0;j<columns-1;j++){
                    int k=(i*(columns-1)+j)*6,v=i*columns+j;
                    tri[k]=v;tri[k+1]=v+1;tri[k+2]=v+columns;tri[k+3]=v+columns;tri[k+4]=v+1;tri[k+5]=v+columns+1;
                }
            }
            var mesh=Own(new Mesh {name="Seeded native road ribbon",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32});mesh.vertices=verts;mesh.uv=uv;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject("Runtime natural road transitions",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=Baseline.roadMaterial;r.shadowCastingMode=ShadowCastingMode.Off;
        }
        void BuildShoreline()
        {
            if(View.Coastline)return;
            var root=new GameObject("Seeded shoreline and understory");root.transform.SetParent(transform,false);int shore=0,under=0;
            if(View.RiverBedVersion>=3){
                var random=RandomFor(0,0,1873);float R()=>(float)random.NextDouble();int bankPlants=0;
                for(float z=Origin.y+3;z<Origin.y+Side-3&&bankPlants<160;z+=6f){
                    if(z/View.MapSide+.5f<.52f)continue;
                    for(int bank=-1;bank<=1;bank+=2){
                        if(R()>.65f)continue;
                        float clusterZ=z+(R()-.5f)*4;
                        int count=1+random.Next(3);
                        for(int k=0;k<count&&bankPlants<160;k++){
                            float pz=clusterZ+(R()-.5f)*2.5f;
                            float center=(View.Data.TributaryRiverCenter(pz/View.MapSide+.5f)-.5f)*View.MapSide;
                            float offset=Mathf.Lerp(8.2f,11.4f,R());var p=new Vector2(center+bank*offset,pz);
                            if(!Contains(p)||RoadDistance(p)<Baseline.roadWidth+1)continue;
                            var prefab=offset<9.3f&&R()<.55f?Baseline.shorelineReeds:Baseline.shorelineGrass;
                            if(!prefab)continue;
                            var plant=Instantiate(prefab,new Vector3(p.x,Height(p)-.03f,p.y),Quaternion.Euler(0,R()*360,0),root.transform);
                            plant.transform.localScale*=Mathf.Lerp(.65f,1f,R());bankPlants++;
                        }
                    }
                }
            }
            for(int z=0;z<Mathf.CeilToInt(Side/4);z++)for(int x=0;x<Mathf.CeilToInt(Side/4);x++){
                var random=RandomFor(x,z,81);float R()=>(float)random.NextDouble();var p=Origin+new Vector2((x+R())*4,(z+R())*4);if(!Contains(p)||ExposedRiverBed(p)||RoadDistance(p)<Baseline.roadWidth+1)continue;float h=Height(p);
                GameObject prefab=null;float d=ShoreDistance(p);
                if(shore<160&&Mathf.Abs(d)<2.8f&&h>ShoreWaterLevel-.35f&&h<ShoreWaterLevel+2.2f&&Noise(p,19,34)>.57f){prefab=h<ShoreWaterLevel+.7f?Baseline.shorelineReeds:Baseline.shorelineGrass;shore++;}
                else if(under<60&&Canopy(p)>.35f&&Noise(p,10,5)>.64f&&R()<.09f&&h>ShoreWaterLevel+.3f){prefab=Baseline.shorelineGrass;under++;}
                if(prefab){var go=Instantiate(prefab,new Vector3(p.x,h-.03f,p.y),Quaternion.Euler(0,R()*360,0),root.transform);go.transform.localScale*=.75f+R()*.45f;}}
        }
        void BuildPresentation()
        {
            if(Baseline.sunTemplate){var sun=Instantiate(Baseline.sunTemplate,transform);if(View.RiverBedVersion>=5)foreach(var light in sun.GetComponentsInChildren<Light>()){light.shadowBias=.15f;light.shadowNormalBias=.5f;}}
            if(Baseline.windTemplate)Instantiate(Baseline.windTemplate,transform);
            var volume=new GameObject("Approved meadow volume").AddComponent<Volume>();volume.transform.SetParent(transform,false);volume.isGlobal=true;volume.priority=30;volume.sharedProfile=Baseline.volume;
        }
        public void ApplyPresentation(bool enabled)
        {
            if(enabled&&!settingsApplied){
                savedFrameRate=Application.targetFrameRate;savedVSync=QualitySettings.vSyncCount;
                var camera=View.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>().GetComponent<Camera>();
                if(Baseline.cameraSettingsTemplate){var backup=new GameObject("Legacy camera settings");backup.transform.SetParent(transform,false);savedCamera=backup.AddComponent<Camera>();savedCamera.CopyFrom(camera);savedCamera.enabled=false;savedPipelineSettings=JsonUtility.ToJson(camera.GetUniversalAdditionalCameraData());var wasEnabled=camera.enabled;var ortho=camera.orthographic;var size=camera.orthographicSize;camera.CopyFrom(Baseline.cameraSettingsTemplate.GetComponent<Camera>());camera.enabled=wasEnabled;camera.orthographic=ortho;camera.orthographicSize=size;JsonUtility.FromJsonOverwrite(Baseline.cameraPipelineSettings,camera.GetUniversalAdditionalCameraData());if(View.Data.Biomes!=null){camera.ResetWorldToCameraMatrix();camera.ResetProjectionMatrix();camera.enabled=true;camera.targetTexture=null;camera.rect=new Rect(0,0,1,1);}}
                savedAmbientMode=RenderSettings.ambientMode;savedSky=RenderSettings.ambientSkyColor;savedEquator=RenderSettings.ambientEquatorColor;savedGround=RenderSettings.ambientGroundColor;savedAmbient=RenderSettings.ambientLight;savedIntensity=RenderSettings.ambientIntensity;savedReflection=RenderSettings.reflectionIntensity;savedSkybox=RenderSettings.skybox;savedProbe=RenderSettings.ambientProbe;settingsApplied=true;SetFramePacing60(FramePacing60);
                RenderSettings.ambientMode=Baseline.ambientMode;RenderSettings.ambientSkyColor=Baseline.ambientSky;RenderSettings.ambientEquatorColor=Baseline.ambientEquator;RenderSettings.ambientGroundColor=Baseline.ambientGround;RenderSettings.ambientLight=Baseline.ambientLight;RenderSettings.ambientIntensity=Baseline.ambientIntensity;RenderSettings.reflectionIntensity=Baseline.reflectionIntensity;RenderSettings.skybox=Baseline.skybox;var irradiance=new SphericalHarmonicsL2();for(int c=0;c<3;c++)for(int i=0;i<9;i++)irradiance[c,i]=Baseline.ambientSH[c*9+i];RenderSettings.ambientProbe=irradiance;RenderSettings.fog=false;
            } else if(!enabled&&settingsApplied){RenderSettings.ambientMode=savedAmbientMode;RenderSettings.ambientSkyColor=savedSky;RenderSettings.ambientEquatorColor=savedEquator;RenderSettings.ambientGroundColor=savedGround;RenderSettings.ambientLight=savedAmbient;RenderSettings.ambientIntensity=savedIntensity;RenderSettings.reflectionIntensity=savedReflection;RenderSettings.skybox=savedSkybox;RenderSettings.ambientProbe=savedProbe;settingsApplied=false;Application.targetFrameRate=savedFrameRate;QualitySettings.vSyncCount=savedVSync;
                if(savedCamera){var camera=View.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>().GetComponent<Camera>();var ortho=camera.orthographic;var size=camera.orthographicSize;camera.CopyFrom(savedCamera);camera.orthographic=ortho;camera.orthographicSize=size;JsonUtility.FromJsonOverwrite(savedPipelineSettings,camera.GetUniversalAdditionalCameraData());if(View.Data.Biomes!=null){camera.ResetWorldToCameraMatrix();camera.ResetProjectionMatrix();camera.enabled=true;camera.targetTexture=null;camera.rect=new Rect(0,0,1,1);}Destroy(savedCamera.gameObject);savedCamera=null;}
            }
        }
        void BuildMinimalRiverCrest()=>BuildTypedWater(false);
        void BuildTypedWater(bool allBodies)
        {
            if(allBodies&&LakeWaterRenderer.UsesCalmWater(View)){
                var districtRect=new Rect(Origin/View.MapSide+Vector2.one*.5f,Vector2.one*(Side/View.MapSide));
                foreach(var body in View.Data.WaterBodies)if(LakeWaterRenderer.Applies(body,View)&&body.Bounds.Overlaps(districtRect)){
                    var surface=new GameObject(body.kind+" — "+body.id);surface.transform.SetParent(transform,false);
                    surface.AddComponent<LakeWaterRenderer>().Initialize(this,body);
                }
            }
            // Configure the cloned renderer while inactive; no approved asset is modified.
            var holder=new GameObject("Minimal narrow-river Crest trial");holder.transform.SetParent(transform,false);holder.SetActive(false);
            var regionRect=new Rect(Origin/View.MapSide+Vector2.one*.5f,Vector2.one*(Side/View.MapSide));
            var activeBodies=new System.Collections.Generic.List<RegionWaterBody>();
            foreach(var candidate in View.Data.WaterBodies)if(!LakeWaterRenderer.Applies(candidate,View)&&candidate.kind!=WaterBodyKind.Unknown&&candidate.Bounds.Overlaps(regionRect)&&(allBodies||candidate.id=="tributary-river"))activeBodies.Add(candidate);
            if(activeBodies.Count==0){Destroy(holder);return;}
            var river=activeBodies[0];var profile=WaterAppearanceProfile.For(river.kind);
            float level=View.SeaLevel;
            if(!allBodies){float v=.78f;var sample=new Vector2((View.Data.TributaryRiverCenter(v)-.5f)*View.MapSide,(v-.5f)*View.MapSide);level=Height(sample)+2.4f;}
            if(allBodies&&river.hasSurfaceLevel)level=river.surfaceLevelNormalized*terrainData.size.y;
            foreach(var descriptor in activeBodies)if(allBodies&&descriptor.hasSurfaceLevel&&Mathf.Abs(descriptor.surfaceLevelNormalized*terrainData.size.y-level)>.01f)throw new InvalidOperationException("Different water elevations require the future bounded level-input path.");
            var root=Instantiate(Baseline.waterTemplate,holder.transform);root.transform.position=Vector3.up*level;root.transform.localScale=Vector3.one;
            Water=root.GetComponent<WaterRenderer>();Water.Viewer=View.GetComponentInChildren<Camera>();Water.Viewpoint=Water.Viewer.transform;
            Water.Reflections.Enabled=profile.reflections;Water.Reflections.Resolution=256;Water.Reflections.AllowMSAA=false;Water.Reflections.DisableShadows=true;Water.Reflections.RenderOnlySingleCamera=true;Water.Reflections.Layers=~(1<<4);Water.Underwater.Enabled=false;
            Water.AnimatedWavesLod.Enabled=false;Water.DynamicWavesLod.Enabled=false;Water.FoamLod.Enabled=profile.foam;bool hasRiverFlow=activeBodies.Exists(b=>WaterAppearanceProfile.For(b.kind).flow);Water.FlowLod.Enabled=hasRiverFlow;Water.FlowLod.Resolution=128;
            Water.DepthLod.Enabled=true;Water.DepthLod.Resolution=128;Water.ShadowLod.Enabled=false;Water.LevelLod.Enabled=false;Water.AlbedoLod.Enabled=false;Water.AbsorptionLod.Enabled=false;
            Water.LodResolution=128;Water.LodLevels=4;Water.GeometryDownSampleFactor=2;
            Water.ClipLod.Enabled=true;Water.ClipLod.Resolution=256;Water.ClipLod.DefaultClippingState=DefaultClippingState.EverythingClipped;
            Water.DefaultExcludes=WaterBodyAffects.Everything;
            Water.Surface.Material=CreateTypedWaterMaterial(river.kind);
            if(allBodies)BuildTypedFootprints(holder.transform,activeBodies,level);
            else {
            // Overlapping local rectangles follow only the tributary. Terrain hides their outer edge.
            for(float z=Origin.y+2;z<Origin.y+Side-2;z+=5f){
                float t=z/View.MapSide+.5f;if(t<.52f)continue;
                Vector2 Point(float zz)=>new Vector2((View.Data.TributaryRiverCenter(zz/View.MapSide+.5f)-.5f)*View.MapSide,zz);
                var a=Point(z-3);var b=Point(z+3);var middle=(a+b)*.5f;if(!Contains(middle))continue;
                var body=new GameObject("Narrow channel water reach").AddComponent<WaterBody>();body.transform.SetParent(holder.transform,false);
                body.transform.position=new Vector3(middle.x,level,middle.y);body.transform.rotation=Quaternion.identity;
                body.transform.localScale=new Vector3(Mathf.Abs(b.x-a.x)+14,8,Mathf.Abs(b.y-a.y)+2);body.Affects=WaterBodyAffects.Surface;body.Exclusion=WaterBodyExclusion.Include;body.Precise=true;
            }
                foreach(var body in holder.GetComponentsInChildren<WaterBody>()){
                    body.OverrideMaterials=true;body.AboveSurfaceMaterial=Water.Surface.Material;
                    var tag=body.gameObject.AddComponent<RegionWaterBodyTag>();tag.bodyId=river.id;tag.kind=river.kind;
                }
            }
            var probe=new GameObject("Cached narrow-river terrain depth").AddComponent<DepthProbe>();probe.transform.SetParent(holder.transform,false);
            probe.transform.position=new Vector3(Origin.x+Side/2,level,Origin.y+Side/2);probe.transform.localScale=new Vector3(Side,1,Side);
            probe.Type=DepthProbeMode.RealTime;probe.RefreshMode=DepthProbeRefreshMode.ViaScripting;probe.Layers=1<<30;probe.Resolution=512;probe.SavedTexture=null;
            probe.CaptureRange=new Vector2(-15,terrainData.size.y+10);probe.GenerateSignedDistanceField=false;
            // Enabled includes runtime validity, which is false until the inactive renderer initializes.
            if(hasRiverFlow){if(allBodies)BuildTypedFlowTexture(holder.transform,level,activeBodies);else BuildNarrowFlowTexture(holder.transform,level,river);}root.SetActive(true);holder.SetActive(true);StartCoroutine(RefreshDepth(probe));
            if(hasRiverFlow){var animation=holder.AddComponent<RiverFlowAnimation>();animation.water=Water;animation.flowMap=riverAnimationFlow;animation.flowRegion=new Vector4(Origin.x,Origin.y,1f/Side,riverAnimationStrength);}
        }
        public Material CreateTypedWaterMaterial(WaterBodyKind kind)
        {
            var material=Own(new Material(Baseline.waterTemplate.GetComponent<WaterRenderer>().Surface.Material));material.name=kind+" water (runtime)";
            var profile=WaterAppearanceProfile.For(kind);
            foreach(var keyword in material.shaderKeywords)if(keyword.ToLowerInvariant().Contains("foam")||keyword.ToLowerInvariant().Contains("reflection"))material.DisableKeyword(keyword);
            if(material.HasProperty("_Crest_Scattering")){
                var color=material.GetColor("_Crest_Scattering");color.r*=profile.scatteringMultiplier;color.g*=profile.scatteringMultiplier;color.b*=profile.scatteringMultiplier;material.SetColor("_Crest_Scattering",color);
            }
            // The visible brightness comes from volume lighting and absorption, not tint alone.
            material.SetFloat("_Crest_PlanarReflectionsEnabled",1);
            material.SetFloat("_Crest_AmbientTerm",material.GetFloat("_Crest_AmbientTerm")*profile.lightingMultiplier);
            material.SetFloat("_Crest_DirectTerm",material.GetFloat("_Crest_DirectTerm")*profile.lightingMultiplier);
            if(material.HasProperty("_Crest_Absorption")){
                var absorption=material.GetColor("_Crest_Absorption");absorption.r*=profile.absorptionMultiplier;absorption.g*=profile.absorptionMultiplier;absorption.b*=profile.absorptionMultiplier;material.SetColor("_Crest_Absorption",absorption);
            }
            material.SetFloat("_Crest_NormalMapEnabled",1);
            material.SetFloat("_Crest_NormalMapStrength",profile.rippleStrength);
            material.SetFloat("_Crest_NormalMapScrollSpeed",profile.rippleSpeed);
            material.SetFloat("_Crest_NormalMapScale",profile.rippleScale);
            material.SetFloat("_Crest_NormalMapTurbulenceEnabled",0);
            if(kind==WaterBodyKind.Lake||kind==WaterBodyKind.Pond){
                // Keep subtle static normals; soften reflected detail using Crest's roughness filtering.
                material.SetFloat("_Crest_PlanarReflectionsApplySmoothness",1);
                material.SetFloat("_Crest_Smoothness",.75f);
                material.SetFloat("_Crest_SmoothnessFar",.75f);
                material.SetFloat("_Crest_PlanarReflectionsIntensity",.75f);
            }
            material.SetFloat("_CREST_FLOW_LOD",profile.flow?1:0);
            if(profile.flow)material.EnableKeyword("_CREST_FLOW_LOD");else material.DisableKeyword("_CREST_FLOW_LOD");
            return material;
        }
        void BuildTypedFootprints(Transform root,System.Collections.Generic.List<RegionWaterBody> bodies,float level)
        {
            // One aggregate mask avoids a separate clip input for every lake shore or river bend.
            const int n=256;var pixels=new Color[n*n];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                var point=(Origin+new Vector2((x+.5f)*Side/n,(z+.5f)*Side/n))/View.MapSide+Vector2.one*.5f;
                pixels[z*n+x]=bodies.Exists(b=>b.Contains(point))?Color.black:Color.white;
            }
            var texture=Own(new Texture2D(n,n,TextureFormat.R8,false,true){name="Typed water footprint mask",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear});texture.SetPixels(pixels);texture.Apply(false,true);
            Water.ClipLod.DefaultClippingState=DefaultClippingState.EverythingClipped;
            var clipObject=new GameObject("Seeded water footprints");clipObject.transform.SetParent(root,false);clipObject.transform.position=new Vector3(Origin.x+Side/2,level,Origin.y+Side/2);clipObject.transform.localScale=new Vector3(Side,1,Side);
            var clip=WaveHarmonic.Crest._Extensions.AddComponent<ClipLodInput>(clipObject,LodInputMode.Texture);clip.GetData<ClipTextureLodInputData>().Texture=texture;
            foreach(var descriptor in bodies){
                var material=CreateTypedWaterMaterial(descriptor.kind);
                void Reach(Rect bounds){var p=(bounds.center-Vector2.one*.5f)*View.MapSide;
                    var body=new GameObject(descriptor.kind+" "+descriptor.id).AddComponent<WaterBody>();body.transform.SetParent(root,false);body.transform.position=new Vector3(p.x,level,p.y);body.transform.localScale=new Vector3(bounds.width*View.MapSide,8,bounds.height*View.MapSide);
                    body.Affects=WaterBodyAffects.Surface;body.Exclusion=WaterBodyExclusion.Include;body.Precise=true;body.OverrideMaterials=true;body.AboveSurfaceMaterial=material;
                    var tag=body.gameObject.AddComponent<RegionWaterBodyTag>();tag.bodyId=descriptor.id;tag.kind=descriptor.kind;
                }
                if(descriptor.centerline!=null&&descriptor.centerline.Length>1)for(int i=1;i<descriptor.centerline.Length;i++){var a=descriptor.centerline[i-1];var b=descriptor.centerline[i];float w=descriptor.halfWidthNormalized;var bounds=Rect.MinMaxRect(Mathf.Min(a.x,b.x)-w,Mathf.Min(a.y,b.y)-w,Mathf.Max(a.x,b.x)+w,Mathf.Max(a.y,b.y)+w);if(bounds.Overlaps(new Rect(Origin/View.MapSide+Vector2.one*.5f,Vector2.one*(Side/View.MapSide))))Reach(bounds);}
                else Reach(descriptor.Bounds);
            }
        }
        void BuildTypedFlowTexture(Transform root,float level,System.Collections.Generic.List<RegionWaterBody> bodies)
        {
            const int n=256;var pixels=new Color[n*n];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                var world=Origin+new Vector2((x+.5f)*Side/n,(z+.5f)*Side/n);var point=world/View.MapSide+Vector2.one*.5f;Vector2 flow=Vector2.zero;float best=float.PositiveInfinity;
                foreach(var body in bodies)if(WaterAppearanceProfile.For(body.kind).flow){float distance=body.DistanceToCenterline(point,out var dir);
                    if(distance<best){best=distance;float strength=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(body.halfWidthNormalized*.7f,body.halfWidthNormalized,distance));flow=dir*body.flowSpeed*strength*Mathf.Lerp(.85f,1.15f,Noise(world,32,67));}}
                pixels[z*n+x]=new Color(.5f+.5f*flow.x,.5f+.5f*flow.y,0,1);
            }
            BuildFlowTextureInput(root,level,pixels,n,1);
        }
        void BuildNarrowFlowTexture(Transform root,float level,RegionWaterBody river)
        {
            const int n=256;var pixels=new Color[n*n];
            for(int z=0;z<n;z++){
                float wz=Origin.y+(z+.5f)*Side/n;float v=wz/View.MapSide+.5f;
                float center=(View.Data.TributaryRiverCenter(v)-.5f)*View.MapSide;
                var direction=new Vector2((View.Data.TributaryRiverCenter(v-.001f)-View.Data.TributaryRiverCenter(v+.001f))*View.MapSide,-.002f*View.MapSide).normalized;
                for(int x=0;x<n;x++){
                    float distance=Mathf.Abs(Origin.x+(x+.5f)*Side/n-center);
                    float weight=v<.52f?0:1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(7,10,distance));
                    float variation=Mathf.Lerp(.85f,1.15f,Noise(new Vector2(center,wz),32,67));
                    pixels[z*n+x]=new Color(.5f+.5f*direction.x*weight*variation,.5f+.5f*direction.y*weight*variation,0,1);
                }
            }
            BuildFlowTextureInput(root,level,pixels,n,river.flowSpeed);
        }
        Texture2D riverAnimationFlow;
        float riverAnimationStrength;
        void BuildFlowTextureInput(Transform root,float level,Color[] pixels,int n,float speed)
        {
            var texture=Own(new Texture2D(n,n,TextureFormat.RGHalf,false,true){name="Seeded narrow-river flow vectors",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear});texture.SetPixels(pixels);texture.Apply(false,true);
            var go=new GameObject("Seeded downstream current");go.transform.SetParent(root,false);
            go.transform.position=new Vector3(Origin.x+Side/2,level,Origin.y+Side/2);go.transform.localScale=new Vector3(Side,1,Side);
            var input=WaveHarmonic.Crest._Extensions.AddComponent<FlowLodInput>(go,LodInputMode.Texture);
            var data=input.GetData<FlowTextureLodInputData>();data.Texture=texture;data.Multiplier=new Vector4(speed,speed,1,1);data.NegativeValues=false;riverAnimationFlow=texture;riverAnimationStrength=speed;
        }
        bool GeneratedRiverDistrict=>!View.Coastline&&View.CreationSettings?.version>=5&&!View.Waterless;
        IEnumerator PaintGeneratedRiverBanks()
        {
            // Also apply to restored caches, without re-laying roads or moving trees.
            int n=terrainData.alphamapResolution;var maps=terrainData.GetAlphamaps(0,0,n,n);
            var timer=System.Diagnostics.Stopwatch.StartNew();
            for(int z=0;z<n;z++){
                for(int x=0;x<n;x++){
                    var p=Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*Side;
                    float distance=ShoreDistance(p);
                    float bank=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,16,Mathf.Max(0,distance))))*.86f;
                    if(bank<=maps[z,x,0])continue;
                    float scale=(1-bank)/Mathf.Max(.0001f,1-maps[z,x,0]);
                    for(int l=1;l<maps.GetLength(2);l++)maps[z,x,l]*=scale;
                    maps[z,x,0]=bank;
                }
                if(timer.ElapsedMilliseconds>12){yield return null;timer.Restart();}
            }
            terrainData.SetAlphamaps(0,0,maps);
        }
        bool HasOcean(){
            var area=new Rect(Origin/View.MapSide+Vector2.one*.5f,Vector2.one*(Side/View.MapSide));
            foreach(var body in View.Data.WaterBodies)if(body.kind==WaterBodyKind.Ocean&&body.Bounds.Overlaps(area))return true;
            return false;
        }
        bool HasRiverWithoutOcean(){
            bool river=false;var area=new Rect(Origin/View.MapSide+Vector2.one*.5f,Vector2.one*(Side/View.MapSide));
            foreach(var body in View.Data.WaterBodies)if(body.Bounds.Overlaps(area)){
                if(body.kind==WaterBodyKind.Ocean)return false;
                if(body.kind==WaterBodyKind.River)river=true;
            }
            return river;
        }
        void BuildWater(){
            if(View.Coastline||(!ApprovedForestAssets.CheckpointReplay&&HasOcean()))BuildCoastalWater();
            else if(GeneratedRiverDistrict||(!ApprovedForestAssets.CheckpointReplay&&HasRiverWithoutOcean())){
                var district=new Rect(Origin/View.MapSide+Vector2.one*.5f,Vector2.one*(Side/View.MapSide));
                bool builtRiver=false;
                foreach(var body in View.Data.WaterBodies){
                    if(!builtRiver&&body.kind==WaterBodyKind.River&&LakeWaterRenderer.GeneratedRiverBounds(body).Overlaps(district)){
                        var surface=new GameObject("Stationary district river — "+body.id);surface.transform.SetParent(transform,false);
                        surface.AddComponent<LakeWaterRenderer>().InitializeGeneratedRiver(this,body);builtRiver=true;
                    }
                    else if(LakeWaterRenderer.Applies(body,View)&&body.Bounds.Overlaps(district)){
                        var surface=new GameObject("District lake/pond — "+body.id);surface.transform.SetParent(transform,false);
                        surface.AddComponent<LakeWaterRenderer>().Initialize(this,body);
                    }
                }
            }
            else BuildTypedWater(true);
        }
        void BuildWaterLegacy()
        {
            var root=Instantiate(Baseline.waterTemplate,transform);root.name="Runtime approved Crest river";root.SetActive(false);root.transform.position=new Vector3(0,View.SeaLevel,0);root.transform.localScale=Vector3.one;
            Water=root.GetComponent<WaterRenderer>();var camera=View.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>().GetComponent<Camera>();Water.Viewer=camera;Water.Viewpoint=camera.transform;
            var body=new GameObject("Generated district water bounds").AddComponent<WaterBody>();body.transform.SetParent(transform,false);body.transform.position=new Vector3(Origin.x+Side/2,View.SeaLevel,Origin.y+Side/2);body.transform.localScale=new Vector3(Side,20,Side);body.Affects=WaterBodyAffects.Everything;body.Exclusion=WaterBodyExclusion.Include;body.Precise=true;
            var probe=new GameObject("Generated terrain depth").AddComponent<DepthProbe>();probe.transform.SetParent(transform,false);probe.transform.position=new Vector3(Origin.x+Side/2,View.SeaLevel,Origin.y+Side/2);probe.transform.localScale=new Vector3(Side,1,Side);probe.Type=DepthProbeMode.RealTime;probe.RefreshMode=DepthProbeRefreshMode.ViaScripting;probe.Layers=1<<30;probe.Resolution=1024;probe.SavedTexture=null;probe.CaptureRange=new Vector2(-15,terrainData.size.y+10);probe.GenerateSignedDistanceField=true;
            if(Baseline.fftTemplate){var fft=Instantiate(Baseline.fftTemplate,root.transform);fft.transform.position=Vector3.up*View.SeaLevel;}
            BuildFlow(transform);root.SetActive(true);StartCoroutine(RefreshDepth(probe));
        }
        IEnumerator RefreshDepth(DepthProbe probe){yield return null;probe.Populate();}
        void BuildFlow(Transform root,bool narrowOnly=false,float level=0)
        {
            for(int branch=narrowOnly?1:0;branch<2;branch++)for(float v=.24f;v<1;v+=.012f){if(branch==1&&v<.52f)continue;float next=Mathf.Min(1,v+.014f);Vector2 Point(float t)=>new Vector2((branch==0?View.Data.MainRiverCenter(t):View.Data.TributaryRiverCenter(t))-.5f,t-.5f)*View.MapSide;
                var a=Point(next);var b=Point(v);var mid=(a+b)*.5f;if(!Contains(mid))continue;var dir=(b-a).normalized;var normal=new Vector2(-dir.y,dir.x);float half=8*View.MapSide/1024;float waterHeight=narrowOnly?level:View.SeaLevel;
                var mesh=Own(new Mesh{name="Generated downstream flow reach"});mesh.vertices=new[]{new Vector3(a.x-normal.x*half,waterHeight,a.y-normal.y*half),new Vector3(a.x+normal.x*half,waterHeight,a.y+normal.y*half),new Vector3(b.x-normal.x*half,waterHeight,b.y-normal.y*half),new Vector3(b.x+normal.x*half,waterHeight,b.y+normal.y*half)};mesh.triangles=new[]{0,1,2,2,1,3};mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.right,Vector2.one};mesh.RecalculateBounds();
                var mat=Own(new Material(Baseline.flowMaterial));mat.SetFloat("_Crest_Direction",Mathf.Repeat(Mathf.Atan2(dir.y,dir.x)/(Mathf.PI*2),1));
                if(narrowOnly)mat.SetFloat("_Crest_Speed",.35f);
                var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;
                var input=WaveHarmonic.Crest._Extensions.AddComponent<FlowLodInput>(go,LodInputMode.Renderer);input.GetData<FlowRendererLodInputData>().Renderer=renderer;renderer.forceRenderingOff=true;
            }
        }
        public bool Contains(Vector2 p)=>p.x>=Origin.x&&p.y>=Origin.y&&p.x<Origin.x+Side&&p.y<Origin.y+Side;
        public System.Random RandomFor(int x,int z,int salt){unchecked{return new System.Random(Seed ^ x*73856093 ^ z*19349663 ^ salt*83492791);}}
        public Vector3 ComparisonFocus {get {
            Vector2 best=Origin+Vector2.one*(Side*.5f);float score=-1;
            for(int i=0;i<80;i++){float v=Mathf.Lerp(.62f,.92f,i/79f);for(int sign=-1;sign<=1;sign+=2){var p=new Vector2((View.Data.TributaryRiverCenter(v)-.5f)*View.MapSide+sign*24,(v-.5f)*View.MapSide);if(!Contains(p)||p.x<Origin.x+80||p.x>Origin.x+Side-80||p.y<Origin.y+80||p.y>Origin.y+Side-80||Height(p)<View.SeaLevel+.5f||RoadDistance(p)<6)continue;float s=Woodland(p)+Canopy(p)*.25f;if(s>score){score=s;best=p;}}}
            return new Vector3(best.x,0,best.y);
        }}
        void LateUpdate()
        {
            ApplyFramePacing();
            if(!DisplayTerrain||!Baseline||!navigationCamera)return;
            // Retreating an orthographic camera must not cull the approved forest.
            float padding=navigationCamera.OrthographicSetback;
            DisplayTerrain.treeDistance=(IsRegionOverview?Mathf.Max(Baseline.treeDistance,View.MapSide*3):Baseline.treeDistance)+padding;
            DisplayTerrain.treeBillboardDistance=Baseline.treeBillboardDistance+padding;
        }
        void OnEnable(){if(Baseline)ApplyPresentation(true);}
        void OnDisable(){ApplyPresentation(false);}
        void OnDestroy(){ApplyPresentation(false);foreach(var asset in owned)if(asset)Destroy(asset);}
    }
}
